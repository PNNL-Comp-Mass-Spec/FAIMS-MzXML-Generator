using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using PRISM;
using ThermoRawFileReader;

namespace WriteFaimsXMLFromRawFile
{
    internal class MsScan
    {
        public int ScanNumber;
        public int msLevel;
        public int peaksCount;
        public string polarity;
        public string scanType;
        public string filterLine;
        public string retentionTime;
        public double lowMz;
        public double highMz;
        public double basePeakMz;
        public double basePeakIntensity;
        public double totIonCurrent;
        public Peaks peaks;

        public MsScan(int scanNumber, int msLevel, int peaksCount, string polarity, string scanType, string filterLine, string retentionTime, double lowMz, double highMz,
            double basePeakMz, double basePeakIntensity, double totIonCurrent, Peaks peaks)
        {
            ScanNumber = scanNumber;
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

        public MsScan(int scanNumber, XRawFileIO reader)
        {
            ScanNumber = scanNumber;

            if (!reader.GetScanInfo(scanNumber, out clsScanInfo scanInfo))
            {
                ConsoleMsgUtils.ShowWarning("Scan {0} not found in {1}", scanNumber, Path.GetFileName(reader.RawFilePath));
                return;
            }

            // Get centroided data
            var dataPointCount = GetScanData(reader, scanInfo, out var mzList, out var intensityList);

            msLevel = scanInfo.MSLevel;
            peaksCount = dataPointCount;

            switch (scanInfo.IonMode)
            {
                case IonModeConstants.Positive:
                    polarity = "+";
                    break;

                case IonModeConstants.Negative:
                    polarity = "-";
                    break;

                case IonModeConstants.Unknown:
                    polarity = string.Empty;
                    break;
            }

            filterLine = scanInfo.FilterText;
            scanType = GetScanType();

            retentionTime = "PT" + Math.Round(scanInfo.RetentionTime * 60, 8) + "S";
            totIonCurrent = scanInfo.TotalIonCurrent;

            string encodedPeaks;
            if (peaksCount == 0)
            {
                lowMz = 0;
                highMz = 0;
                basePeakMz = 0;
                basePeakIntensity = 0;

                encodedPeaks = Base64EncodeMsData(mzList, intensityList);
            }
            else
            {
                lowMz = mzList.Min();
                highMz = mzList.Max();
                basePeakMz = scanInfo.BasePeakMZ;
                basePeakIntensity = scanInfo.BasePeakIntensity;

                encodedPeaks = Base64EncodeMsData(mzList, intensityList);
            }

            peaks = new Peaks(32, "network", "m/z-int", "none", 0, encodedPeaks);
        }

        private int GetScanData(XRawFileIO reader, clsScanInfo scanInfo, out double[] mzList, out double[] intensityList)
        {
            if (scanInfo.IsFTMS)
            {
                var labelDataCount = reader.GetScanLabelData(scanInfo.ScanNumber, out var labelData);

                if (labelDataCount > 0)
                {
                    mzList = new double[labelDataCount];
                    intensityList = new double[labelDataCount];

                    for (var i = 0; i < labelDataCount; i++)
                    {
                        mzList[i] = labelData[i].Mass;
                        intensityList[i] = labelData[i].Intensity;
                    }

                    return labelDataCount;
                }
            }

            var dataPointCount = reader.GetScanData(scanInfo.ScanNumber, out mzList, out intensityList, 0, true);
            return dataPointCount;
        }

        private string GetScanType()
        {
            var returnString = "Unknown";

            var filterLineParts = this.filterLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var param in filterLineParts)
            {
                if (param.Equals("Full"))
                {
                    return "Full";
                }

                if (param.Equals("SIM"))
                {
                    return "SIM";
                }
            }

            return returnString;
        }

        private static string Base64EncodeMsData(IReadOnlyList<double> mzList, IReadOnlyList<double> intensityList)
        {
            var byteList = new List<byte>();

            for (var i = 0; i < mzList.Count; i++)
            {
                var floatValue = Convert.ToSingle(mzList[i]);
                var bigEndianFloat = FloatToBigEndian(floatValue);
                foreach (var chunk in bigEndianFloat)
                {
                    byteList.Add(chunk);
                }

                floatValue = Convert.ToSingle(intensityList[i]);
                bigEndianFloat = FloatToBigEndian(floatValue);
                foreach (var chunk in bigEndianFloat)
                {
                    byteList.Add(chunk);
                }
            }

            var byteArray = byteList.ToArray();

            return Convert.ToBase64String(byteArray);
        }

        private static byte[] FloatToBigEndian(float floatValue)
        {
            var littleEndian = BitConverter.GetBytes(floatValue);

            return new[] { littleEndian[3], littleEndian[2], littleEndian[1], littleEndian[0] };
        }

        public string FormatSpecialNumber(double number)
        {
            if (number < 1000000)
            {
                return number.ToString(CultureInfo.InvariantCulture);
            }

            var exponent = Math.Floor(Math.Log10(number));
            var prefix = Math.Round(number / Math.Pow(10, exponent), 5);

            if (exponent < 10)
            {
                return "" + prefix + "e+00" + exponent;
            }

            return "" + prefix + "e+0" + exponent;
        }

    }
}
