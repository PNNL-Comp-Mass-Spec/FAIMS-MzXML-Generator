using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WriteFaimsXMLFromRawFile
{
    /// <summary>
    /// MS1 specific info
    /// </summary>
    internal sealed class Ms1Scan : MsScan
    {
        // Ignore Spelling: cv

        public List<Ms2Scan> Ms2s { get; set; }

        private Ms1Scan(int num, int msLevel, int peaksCount, string polarity, string scanType, string filterLine, string retentionTime, double lowMz, double highMz,
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

        public static Ms1Scan Create(MsScan scan)
        {
            var ms1 = new Ms1Scan(
                scan.ScanNumber, scan.MsLevel, scan.PeaksCount,
                scan.Polarity, scan.ScanType, scan.FilterLine,
                scan.RetentionTime, scan.LowMz, scan.HighMz,
                scan.BasePeakMz, scan.BasePeakIntensity,
                scan.TotIonCurrent, scan.PeakData)
            {
                // Initialize list to hold references to Ms2Scans
                Ms2s = new List<Ms2Scan>()
            };

            return ms1;
        }

        public void AddMs2Scan(Ms2Scan scan)
        {
            Ms2s.Add(scan);
        }

        // for outputting valid MzXML strings to file
        public string ToXML(FAIMStoMzXMLProcessor processor)
        {
            // place scan byte depth into our tracking list
            var index = new Index(processor.ByteTracking.CurrentScan, processor.ByteTracking.ByteDepth + 2);
            processor.ByteTracking.ScanOffsets.Add(index);

            FilterLine = FixFilterLine();

            var sb = new StringBuilder();

            sb.AppendFormat("  <scan num=\"{0}\"", processor.ByteTracking.CurrentScan).AppendLine();

            sb.AppendFormat("   msLevel=\"{0}\"", MsLevel).AppendLine();
            sb.AppendFormat("   peaksCount=\"{0}\"", PeaksCount).AppendLine();
            sb.AppendFormat("   polarity=\"{0}\"", Polarity).AppendLine();
            sb.AppendFormat("   scanType=\"{0}\"", ScanType).AppendLine();
            sb.AppendFormat("   filterLine=\"{0}\"", FilterLine).AppendLine();
            sb.AppendFormat("   retentionTime=\"{0}\"", RetentionTime).AppendLine();
            sb.AppendFormat("   lowMz=\"" + Math.Round(LowMz, 3) + "\"").AppendLine();
            sb.AppendFormat("   highMz=\"" + Math.Round(HighMz, 3) + "\"").AppendLine();
            sb.AppendFormat("   basePeakMz=\"" + Math.Round(BasePeakMz, 3) + "\"").AppendLine();
            sb.AppendFormat("   basePeakIntensity=\"" + FormatSpecialNumber(BasePeakIntensity) + "\"").AppendLine();
            sb.AppendFormat("   totIonCurrent=\"" + FormatSpecialNumber(TotIonCurrent) + "\">").AppendLine();
            sb.AppendLine(PeakData.ToXML(3));

            // advance the byteTracker for Ms2 Indices
            processor.ByteTracker(sb.ToString(), true);

            processor.ByteTracking.CurrentScan++;

            foreach (var ms2 in Ms2s)
            {
                var ms2String = ms2.ToXML(processor);
                sb.AppendLine(ms2String);

                // advance byteTracker for the Ms2 Entry
                processor.ByteTracker(ms2String);
            }

            sb.Append("  </scan>");
            processor.ByteTracker("  </scan>");

            return sb.ToString();
        }

        private string FixFilterLine()
        {
            // split FilterLine by spaces. remove empty entries
            var filterLineParts = FilterLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();

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
