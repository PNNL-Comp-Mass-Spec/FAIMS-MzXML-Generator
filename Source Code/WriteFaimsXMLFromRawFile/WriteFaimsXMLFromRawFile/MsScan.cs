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
        public int ScanNumber { get; set; }
        public int MsLevel { get; set; }
        public int PeaksCount { get; set; }
        public string Polarity { get; set; }
        public string ScanType { get; set; }
        public string FilterLine { get; set; }
        public string RetentionTime { get; set; }
        public double LowMz { get; set; }
        public double HighMz { get; set; }
        public double BasePeakMz { get; set; }
        public double BasePeakIntensity { get; set; }
        public double TotIonCurrent { get; set; }
        public Peaks PeakData { get; set; }

        public MsScan(int scanNumber, int msLevel, int peaksCount, string polarity, string scanType, string filterLine, string retentionTime, double lowMz, double highMz,
            double basePeakMz, double basePeakIntensity, double totIonCurrent, Peaks peakData)
        {
            ScanNumber = scanNumber;
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

        public MsScan(int scanNumber, XRawFileIO reader)
        {
            ScanNumber = scanNumber;

            if (!reader.GetScanInfo(scanNumber, out var scanInfo))
            {
                ConsoleMsgUtils.ShowWarning("Scan {0} not found in {1}", scanNumber, Path.GetFileName(reader.RawFilePath));
                return;
            }

            // Get centroided data
            var dataPointCount = GetScanData(reader, scanInfo, out var mzList, out var intensityList);

            MsLevel = scanInfo.MSLevel;
            PeaksCount = dataPointCount;

            switch (scanInfo.IonMode)
            {
                case IonModeConstants.Positive:
                    Polarity = "+";
                    break;

                case IonModeConstants.Negative:
                    Polarity = "-";
                    break;

                case IonModeConstants.Unknown:
                    Polarity = string.Empty;
                    break;
            }

            FilterLine = scanInfo.FilterText;
            ScanType = GetScanType();

            RetentionTime = "PT" + Math.Round(scanInfo.RetentionTime * 60, 8) + "S";
            TotIonCurrent = scanInfo.TotalIonCurrent;

            string encodedPeaks;
            if (PeaksCount == 0)
            {
                LowMz = 0;
                HighMz = 0;
                BasePeakMz = 0;
                BasePeakIntensity = 0;

                encodedPeaks = Base64EncodeMsData(mzList, intensityList);
            }
            else
            {
                LowMz = mzList.Min();
                HighMz = mzList.Max();
                BasePeakMz = scanInfo.BasePeakMZ;
                BasePeakIntensity = scanInfo.BasePeakIntensity;

                encodedPeaks = Base64EncodeMsData(mzList, intensityList);
            }

            PeakData = new Peaks(32, "network", "m/z-int", "none", 0, encodedPeaks);
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
