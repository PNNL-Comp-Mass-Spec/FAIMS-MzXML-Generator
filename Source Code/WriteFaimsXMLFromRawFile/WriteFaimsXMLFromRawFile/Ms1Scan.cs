using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WriteFaimsXMLFromRawFile
{
    class Ms1Scan : MsScan
    {
        // Ms1 specific vars
        public List<Ms2Scan> ms2s;

        private Ms1Scan(int num, int msLevel, int peaksCount, string polarity, string scanType, string filterLine, string retentionTime, double lowMz, double highMz,
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

        public static Ms1Scan Create(MsScan scan)
        {
            var ms1 = new Ms1Scan(scan.num, scan.msLevel, scan.peaksCount, scan.polarity, scan.scanType, scan.filterLine, scan.retentionTime, scan.lowMz,
                scan.highMz, scan.basePeakMz, scan.basePeakIntensity, scan.totIonCurrent, scan.peaks);

            // initialize list to hold references to Ms2Scans
            ms1.ms2s = new List<Ms2Scan>();

            return ms1;
        }

        public void AddMs2Scan(Ms2Scan scan)
        {
            this.ms2s.Add(scan);
        }

        // for outputting valid MzXML strings to file
        public string ToXML()
        {
            // place scan byte depth into our tracking list
            var index = new Index(ByteVariables.currentScan, ByteVariables.byteDepth + 2);
            ByteVariables.scanOffsets.Add(index);

            this.filterLine = FixFilterLine();

            var sb = new StringBuilder();
           
            sb.AppendLine("  <scan num=\"" + ByteVariables.currentScan++ + "\"");
            sb.AppendLine("   msLevel=\"" + this.msLevel + "\"");
            sb.AppendLine("   peaksCount=\"" + this.peaksCount + "\"");
            sb.AppendLine("   polarity=\"" + this.polarity + "\"");
            sb.AppendLine("   scanType=\"" + this.scanType + "\"");
            sb.AppendLine("   filterLine=\"" + this.filterLine + "\"");
            sb.AppendLine("   retentionTime=\"" + this.retentionTime + "\"");
            sb.AppendLine("   lowMz=\"" + Math.Round(this.lowMz, 3) + "\"");
            sb.AppendLine("   highMz=\"" + Math.Round(this.highMz, 3) + "\"");
            sb.AppendLine("   basePeakMz=\"" + Math.Round(this.basePeakMz, 3) + "\"");
            sb.AppendLine("   basePeakIntensity=\"" + this.FormatSpecialNumber(this.basePeakIntensity) + "\"");
            sb.AppendLine("   totIonCurrent=\"" + this.FormatSpecialNumber(this.totIonCurrent) + "\" >");
            sb.AppendLine(this.peaks.ToXML(3));

            // advance the byteTracker for Ms2 Indices
            Program.ByteTracker(sb.ToString(), true);

            foreach (var ms2 in ms2s)
            {
                var ms2String = ms2.ToXML();
                // advance byteTracker for the Ms2 Entry
                Program.ByteTracker(ms2String);
                sb.AppendLine(ms2String);
            }

            sb.Append("  </scan>");
            Program.ByteTracker("  </scan>");
            return sb.ToString();
        }

        private string FixFilterLine()
        {
            // split filterline by spaces. remove empty entries
            var parameters = this.filterLine.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();

            for (var i = 0; i < parameters.Count; i++)
            {
                var item = parameters[i];
                if (item.Contains("cv="))
                {
                    parameters.RemoveAt(i);
                    i--;
                }
            }

            return String.Join(" ", parameters.ToArray());
        }
    }
}
