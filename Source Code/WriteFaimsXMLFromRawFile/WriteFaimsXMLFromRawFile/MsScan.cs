using System;
using System.Collections.Generic;
using System.Globalization;
using CSMSL;
using CSMSL.IO.Thermo;
using CSMSL.Spectral;

namespace WriteFaimsXMLFromRawFile
{
    class MsScan
    {
        public int num;
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

        public MsScan(int num, int msLevel, int peaksCount, string polarity, string scanType, string filterLine, string retentionTime, double lowMz, double highMz,
            double basePeakMz, double basePeakIntensity, double totIonCurrent, Peaks peaks)
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

        public MsScan(int num, ThermoRawFile rawFile)
        {
            this.num = num;

            var scan = rawFile.GetMsScan(num);
            var spectrum = scan.MassSpectrum;

            this.msLevel = scan.MsnOrder;
            this.peaksCount = scan.MassSpectrum.Count;

            if (scan.Polarity.Equals(Polarity.Positive))
            {
                this.polarity = "+";
            }
            else
            {
                this.polarity = "-";
            }
            this.filterLine = rawFile.GetScanFilter(num);
            this.scanType = GetScanType();

            this.retentionTime = "PT" + Math.Round(rawFile.GetRetentionTime(num) * 60, 8) + "S";
            this.totIonCurrent = spectrum.TotalIonCurrent;

            string encodedPeaks;
            if (peaksCount == 0)
            {
                this.lowMz = 0;
                this.highMz = 0;
                this.basePeakMz = 0;
                this.basePeakIntensity = 0;

                encodedPeaks = Base64EncodeMsData(spectrum);
            }
            else
            {
                this.lowMz = spectrum.FirstMZ;
                this.highMz = spectrum.LastMZ;
                this.basePeakMz = spectrum.GetBasePeak().MZ;
                this.basePeakIntensity = spectrum.GetBasePeakIntensity();

                encodedPeaks = Base64EncodeMsData(spectrum);
            }

            this.peaks = new Peaks(32, "network", "m/z-int", "none", 0, encodedPeaks);
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

        private static string Base64EncodeMsData(ISpectrum spectrum)
        {
            var byteList = new List<byte>();

            var mZs = spectrum.GetMasses();
            var intensities = spectrum.GetIntensities();

            for (var i = 0; i < mZs.Length; i++)
            {
                var floatValue = Convert.ToSingle(mZs[i]);
                var bigEndianFloat = FloatToBigEndian(floatValue);
                foreach (var chunk in bigEndianFloat)
                {
                    byteList.Add(chunk);
                }

                floatValue = Convert.ToSingle(intensities[i]);
                bigEndianFloat = FloatToBigEndian(floatValue);
                foreach (var chunk in bigEndianFloat)
                {
                    byteList.Add(chunk);
                }
            }

            var byteArray = byteList.ToArray();

            return Convert.ToBase64String(byteArray);
        }

        private static byte[] FloatToBigEndian(Single floatValue)
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
