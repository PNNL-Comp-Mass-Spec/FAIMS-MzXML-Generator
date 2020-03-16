using System;
using System.Linq;
using System.Text;
using CSMSL.IO.Thermo;
using System.Text.RegularExpressions;

namespace WriteFaimsXMLFromRawFile
{
    class Ms2Scan : MsScan
    {
        // Ms2 specific vars
        public int collisionEnergy;
        public PrecursorMz precursorMz;

        private Ms2Scan(int num, int msLevel, int peaksCount, string polarity, string scanType, string filterLine, string retentionTime, double lowMz, double highMz,
            double basePeakMz, double basePeakIntensity, double totIonCurrent, Peaks peaks) : base(num, msLevel, peaksCount, polarity, scanType, filterLine,
            retentionTime, lowMz, highMz, basePeakMz, basePeakIntensity, totIonCurrent, peaks)
        {
            this.num = num;
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
            var ms2 = new Ms2Scan(scan.num, scan.msLevel, scan.peaksCount, scan.polarity, scan.scanType, scan.filterLine, scan.retentionTime, scan.lowMz,
                scan.highMz, scan.basePeakMz, scan.basePeakIntensity, scan.totIonCurrent, scan.peaks);
            return ms2;
        }

        public void AddMs2ScanParameters(ThermoRawFile rawfile)
        {
            // grab precursorMz from raw file
            var precursorMz = Math.Round(rawfile.GetPrecursorMz(this.num), 8);

            var parentSpectrum = rawfile.GetMsScan(rawfile.GetParentSpectrumNumber(this.num)).MassSpectrum;

            var precursorIntensity = 0;
            try
            {
                Math.Round(parentSpectrum.GetClosestPeak(precursorMz, .1).Intensity, 2);
            }
            catch
            {
                // couldn't find precursor mass in MS1. That's fine i guess. 
            }

            var filterParams = this.filterLine.Split(' ');

            var activationType = "HCD";

            foreach (var param in filterParams)
            {
                if (param.Contains("@"))
                {
                    // HCD25.00 -> [HCD, 25.00]
                    // regex alpha from numeric
                    var activationArray = Regex.Matches(param.Split('@')[1].Trim(), @"\D+|\d+").Cast<Match>().Select(m => m.Value).ToArray();

                    activationType = activationArray[0].ToUpper();

                    this.collisionEnergy = Convert.ToInt32(Double.Parse(activationArray[1]));
                }
            }

            this.precursorMz = new PrecursorMz(precursorIntensity, activationType, precursorMz);
        }

        public string ToXML()
        {
            // place scan byte depth into our tracking list
            var index = new Index(ByteVariables.currentScan, ByteVariables.byteDepth + 3);
            ByteVariables.scanOffsets.Add(index);

            this.filterLine = FixFilterLine();

            var sb = new StringBuilder();

            sb.AppendLine("   <scan num=\"" + ByteVariables.currentScan++ + "\"");
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
            sb.AppendLine("    collisionEnergy=\"" + this.collisionEnergy + "\" >");

            sb.AppendLine(this.precursorMz.ToXML());
            sb.AppendLine(this.peaks.ToXML(4));
            sb.Append("   </scan>");

            return sb.ToString();
        }

        private string FixFilterLine()
        {
            // split filterline by spaces. remove empty entries
            var parameters = this.filterLine.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();

            for (var i = 0; i < parameters.Count; i++)
            {
                var item = parameters[i];
                if (item.Contains("cv=") || item.Equals("t"))
                {
                    parameters.RemoveAt(i);
                    i--;
                }
            }

            return String.Join(" ", parameters.ToArray());
        }
    }
}
