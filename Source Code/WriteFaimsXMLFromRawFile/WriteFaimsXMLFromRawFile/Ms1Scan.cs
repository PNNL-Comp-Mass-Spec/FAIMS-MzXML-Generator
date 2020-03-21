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
            ScanNumber = num;
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
            var ms1 = new Ms1Scan(
                scan.ScanNumber, scan.msLevel, scan.peaksCount,
                scan.polarity, scan.scanType, scan.filterLine,
                scan.retentionTime, scan.lowMz, scan.highMz,
                scan.basePeakMz, scan.basePeakIntensity,
                scan.totIonCurrent, scan.peaks)
            {
                // Initialize list to hold references to Ms2Scans
                ms2s = new List<Ms2Scan>()
            };

            return ms1;
        }

        public void AddMs2Scan(Ms2Scan scan)
        {
            this.ms2s.Add(scan);
        }

        // for outputting valid MzXML strings to file
        public string ToXML(FAIMStoMzXMLProcessor processor)
        {
            // place scan byte depth into our tracking list
            var index = new Index(processor.ByteTracking.CurrentScan, processor.ByteTracking.ByteDepth + 2);
            processor.ByteTracking.ScanOffsets.Add(index);

            this.filterLine = FixFilterLine();

            var sb = new StringBuilder();

            sb.AppendLine("  <scan num=\"" + processor.ByteTracking.CurrentScan + "\"");

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
            sb.AppendLine("   totIonCurrent=\"" + this.FormatSpecialNumber(this.totIonCurrent) + "\">");
            sb.AppendLine(this.peaks.ToXML(3));

            // advance the byteTracker for Ms2 Indices
            processor.ByteTracker(sb.ToString(), true);

            foreach (var ms2 in ms2s)
            {
                var ms2String = ms2.ToXML(processor);
                sb.AppendLine(ms2String);

                // advance byteTracker for the Ms2 Entry
                processor.ByteTracker(ms2String);
            }

            sb.Append("  </scan>");
            processor.ByteTracker("  </scan>");

            processor.ByteTracking.CurrentScan++;

            return sb.ToString();
        }

        private string FixFilterLine()
        {
            // split filterLine by spaces. remove empty entries
            var filterLineParts = filterLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();

            for (var i = 0; i < filterLineParts.Count; i++)
            {
                var item = filterLineParts[i];
                if (item.Contains("cv="))
                {
                    filterLineParts.RemoveAt(i);
                    i--;
                }
            }

            return string.Join(" ", filterLineParts.ToArray());
        }
    }
}
