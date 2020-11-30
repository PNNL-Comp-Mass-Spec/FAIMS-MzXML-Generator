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

        public int CollisionEnergy { get; set; }
        public PrecursorMz PrecursorMz { get; set; }

        private Ms2Scan(int num, int msLevel, int peaksCount, string polarity, string scanType, string filterLine, string retentionTime, double lowMz, double highMz,
            double basePeakMz, double basePeakIntensity, double totIonCurrent, Peaks peakData) : base(num, msLevel, peaksCount, polarity, scanType, filterLine,
            retentionTime, lowMz, highMz, basePeakMz, basePeakIntensity, totIonCurrent, peakData)
        {
            ScanNumber = num;
            MsLevel = msLevel;
            PeaksCount = peaksCount;
            Polarity = polarity;
            ScanType = scanType;
            FilterLine = filterLine;
            RetentionTime = retentionTime;
            LowMz = lowMz;
            HighMz = highMz;
            BasePeakMz = basePeakMz;
            BasePeakIntensity = basePeakIntensity;
            TotIonCurrent = totIonCurrent;
            PeakData = peakData;
        }

        public static Ms2Scan Create(MsScan scan)
        {
            var ms2 = new Ms2Scan(
                scan.ScanNumber, scan.MsLevel, scan.PeaksCount, scan.Polarity, scan.ScanType, scan.FilterLine, scan.RetentionTime, scan.LowMz,
                scan.HighMz, scan.BasePeakMz, scan.BasePeakIntensity, scan.TotIonCurrent, scan.PeakData);
            return ms2;
        }

        public void AddMs2ScanParameters(XRawFileIO reader)
        {
            // grab precursorMz from raw file
            var success = reader.GetScanInfo(ScanNumber, out var scanInfo);
            if (!success)
            {
                ConsoleMsgUtils.ShowWarning("Scan {0} not found by AddMs2ScanParameters", ScanNumber);
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

            var filterLineParts = FilterLine.Split(' ');

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

                    CollisionEnergy = Convert.ToInt32(double.Parse(activationArray[1]));
                }
            }

            PrecursorMz = new PrecursorMz(precursorIntensity, activationType, precursorMzValue);
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

            FilterLine = FixFilterLine();

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

            sb.AppendLine(PrecursorMz.ToXML());
            sb.AppendLine(PeakData.ToXML(4));
            sb.Append("   </scan>");

            processor.ByteTracking.CurrentScan++;
            return sb.ToString();
        }

        private string FixFilterLine()
        {
            // split FilterLine by spaces. remove empty entries
            var filterLineParts = this.FilterLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();

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
