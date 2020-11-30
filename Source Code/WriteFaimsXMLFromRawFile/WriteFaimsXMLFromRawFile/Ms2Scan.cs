using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using PRISM;
using ThermoRawFileReader;

namespace WriteFaimsXMLFromRawFile
{
    /// <summary>
    /// MS2 specific info
    /// </summary>
    internal sealed class Ms2Scan : MsScan
    {
        // Ignore Spelling: cv


        private Ms2Scan(int num, int msLevel, int peaksCount, string polarity, string scanType, string filterLine, string retentionTime, double lowMz, double highMz,
            double basePeakMz, double basePeakIntensity, double totIonCurrent, Peaks peaks) : base(num, msLevel, peaksCount, polarity, scanType, filterLine,
            retentionTime, lowMz, highMz, basePeakMz, basePeakIntensity, totIonCurrent, peaks)
        {
            this.ScanNumber = num;
            this.msLevel = msLevel;
            this.peaksCount = peaksCount;
            this.polarity = polarity;
            this.scanType = scanType;
            this.filterLine = filterLine;
            this.retentionTime = retentionTime;
            this.lowMz = lowMz;
            this.highMz = highMz;
            this.basePeakMz = basePeakMz;
            this.basePeakIntensity = basePeakIntensity;
            this.totIonCurrent = totIonCurrent;
            this.peaks = peaks;
        }

        public static Ms2Scan Create(MsScan scan)
        {
            var ms2 = new Ms2Scan(
                scan.ScanNumber, scan.msLevel, scan.peaksCount, scan.polarity, scan.scanType, scan.filterLine, scan.retentionTime, scan.lowMz,
                scan.highMz, scan.basePeakMz, scan.basePeakIntensity, scan.totIonCurrent, scan.peaks);
            return ms2;
        }

        public void AddMs2ScanParameters(XRawFileIO reader)
        {
            // grab precursorMz from raw file
            var success = reader.GetScanInfo(ScanNumber, out var scanInfo);
            if (!success)
            {
                ConsoleMsgUtils.ShowWarning("Scan {0} not found by AddMs2ScanParameters", this.ScanNumber);
                return;
            }

            var precursorMzValue = Math.Round(scanInfo.ParentIonMZ, 8);

            var parentScanInfo = GetParentScan(reader, scanInfo);

            var parentScanDataCount = reader.GetScanData(parentScanInfo.ScanNumber, out var mzList, out var intensityList);

            double precursorIntensity = 0;

            if (parentScanDataCount > 0)
            {
                const double PRECURSOR_TOLERANCE = 0.1;

                var closestDifference = double.MaxValue;
                for (var i = 0; i < mzList.Length; i++)
                {
                    var mzDifference = Math.Abs(mzList[i] - precursorMzValue);
                    if (mzDifference > closestDifference)
                        continue;

                    closestDifference = mzDifference;
                    precursorIntensity = Math.Round(intensityList[i], 2);
                }

                if (closestDifference > PRECURSOR_TOLERANCE)
                {
                    // couldn't find precursor mass in MS1. That's fine i guess.
                    precursorIntensity = 0;
                }
            }

            var filterLineParts = this.filterLine.Split(' ');

            string activationType;
            switch (scanInfo.ActivationType)
            {
                case ActivationTypeConstants.CID:
                case ActivationTypeConstants.MPD:
                case ActivationTypeConstants.ECD:
                case ActivationTypeConstants.PQD:
                case ActivationTypeConstants.ETD:
                case ActivationTypeConstants.HCD:
                case ActivationTypeConstants.SA:
                case ActivationTypeConstants.PTR:
                case ActivationTypeConstants.NETD:
                case ActivationTypeConstants.NPTR:
                case ActivationTypeConstants.UVPD:
                    activationType = scanInfo.ActivationType.ToString();
                    break;

                default:
                    // Includes ActivationTypeConstants.AnyType
                    activationType = "CID";
                    break;
            }

            foreach (var param in filterLineParts)
            {
                if (param.Contains("@"))
                {
                    // HCD25.00 -> [HCD, 25.00]
                    // RegEx alpha from numeric
                    var activationArray = Regex.Matches(param.Split('@')[1].Trim(), @"\D+|\d+").Cast<Match>().Select(m => m.Value).ToArray();

                    this.collisionEnergy = Convert.ToInt32(double.Parse(activationArray[1]));
                }
            }

            this.precursorMz = new PrecursorMz(precursorIntensity, activationType, precursorMzValue);
        }

        private clsScanInfo GetParentScan(XRawFileIO reader, clsScanInfo scanInfo)
        {
            if (scanInfo.MSLevel <= 1)
                return scanInfo;

            var parentScanNumber = scanInfo.ScanNumber - 1;
            while (parentScanNumber > 0)
            {
                var success = reader.GetScanInfo(parentScanNumber, out var parentScanInfo);
                if (success && parentScanInfo.MSLevel <= 1)
                    return parentScanInfo;

                parentScanNumber--;
            }

            reader.GetScanInfo(1, out var scanInfoFirstScan);
            return scanInfoFirstScan;
        }

        public string ToXML(FAIMStoMzXMLProcessor processor)
        {
            // place scan byte depth into our tracking list
            var index = new Index(processor.ByteTracking.CurrentScan, processor.ByteTracking.ByteDepth + 3);
            processor.ByteTracking.ScanOffsets.Add(index);

            this.filterLine = FixFilterLine();

            var sb = new StringBuilder();

            sb.AppendLine("   <scan num=\"" + processor.ByteTracking.CurrentScan + "\"");
            sb.AppendLine("    msLevel=\"" + this.msLevel + "\"");
            sb.AppendLine("    peaksCount=\"" + this.peaksCount + "\"");
            sb.AppendLine("    polarity=\"" + this.polarity + "\"");
            sb.AppendLine("    scanType=\"" + this.scanType + "\"");
            sb.AppendLine("    filterLine=\"" + this.filterLine + "\"");
            sb.AppendLine("    retentionTime=\"" + this.retentionTime + "\"");
            sb.AppendLine("    lowMz=\"" + Math.Round(this.lowMz, 3) + "\"");
            sb.AppendLine("    highMz=\"" + Math.Round(this.highMz, 3) + "\"");
            sb.AppendLine("    basePeakMz=\"" + Math.Round(this.basePeakMz, 3) + "\"");
            sb.AppendLine("    basePeakIntensity=\"" + this.FormatSpecialNumber(this.basePeakIntensity) + "\"");
            sb.AppendLine("    totIonCurrent=\"" + this.FormatSpecialNumber(this.totIonCurrent) + "\"");
            sb.AppendLine("    collisionEnergy=\"" + this.collisionEnergy + "\">");

            sb.AppendLine(this.precursorMz.ToXML());
            sb.AppendLine(this.peaks.ToXML(4));
            sb.Append("   </scan>");

            processor.ByteTracking.CurrentScan++;
            return sb.ToString();
        }

        private string FixFilterLine()
        {
            // split filterLine by spaces. remove empty entries
            var filterLineParts = this.filterLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();

            for (var i = 0; i < filterLineParts.Count; i++)
            {
                var item = filterLineParts[i];
                if (item.Contains("cv=") || item.Equals("t"))
                {
                    filterLineParts.RemoveAt(i);
                    i--;
                }
            }

            return string.Join(" ", filterLineParts.ToArray());
        }
    }
}
