using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CSMSL;
using System.IO;
using CSMSL.IO.Thermo;
using System.Security.Cryptography;

namespace WriteFaimsXMLFromRawFile
{
    class Program
    {
        static void Main(string[] args)
        {
            var filePath = args[0];
            var baseOutPath = args[1];         
            //var filePath = @"C:\Users\dbrademan\Desktop\ReAdW Test\Multi CV Test\20feb2018_60cv_60min_CV_55_70.raw";
            //var baseOutPath = @"C:\Users\dbrademan\Desktop\ReAdW Test\Multi CV Test";

            //var filePath = @"C:\Users\dbrademan\Desktop\ReAdW Test\Large File\29mar2018_K562_4HR_4CV_6UG_06S_15MS_1.raw";
            //var baseOutPath = @"C:\Users\dbrademan\Desktop\ReAdW Test\Large File";   

            // quickly hash the raw file before opening it
            var fileSha1 = HashRawFile(filePath);

            // open up the raw file connection
            var rawFile = new ThermoRawFile(filePath);
            rawFile.Open();

            // get all unique CV values from scans
            var cvValues = GetUniqueCvValues(rawFile);
            

            // now work for each unique CV value (# files we're going to need to split into)
            // get all scans that have the CV that we're currently targeting
            foreach (var cvValue in cvValues)
            {
                var targetScans = FindAllTargetScans(cvValue, rawFile);
                var t = "";

                var outPath = baseOutPath + "\\" + Path.GetFileNameWithoutExtension(filePath) + "_" + cvValue + ".mzxml";

                var writer = new StreamWriter(outPath);

                var header = WriteHeader(writer, filePath, rawFile, fileSha1, targetScans);

                Ms1Scan currentScan = null;
                // write out our target scans
                for (int i = 0; i < targetScans.Count; i++)
                {
                    var scan = new MsScan(targetScans[i], rawFile);

                    if (scan.msLevel == 1)
                    {
                        if (currentScan == null)
                        {
                            // start condition
                            currentScan = Ms1Scan.Create(scan);
                        }
                        else
                        {
                            // write currentScan to file
                            var outString = currentScan.ToXML();
                            writer.WriteLine(outString);

                            rawFile.ClearCachedScans();
                            currentScan = Ms1Scan.Create(scan);
                        }
                    }
                    else if (scan.msLevel == 2)
                    {
                        var ms2Scan = Ms2Scan.Create(scan);
                        ms2Scan.AddMs2ScanParameters(rawFile);
                        currentScan.AddMs2Scan(ms2Scan);
                    }
                }

                // once we're out, we need to write out our last currentScan
                writer.WriteLine(currentScan.ToXML());
                currentScan = null;

                //finish off msRun
                writer.WriteLine(" </msRun>");
                writer.WriteLine(" <index name=\"scan\" >");

                // add special entry to our indexOffset list for where the offsets start
                var index = new Index(0, ByteVariables.byteDepth + ByteVariables.encoder.GetByteCount(" </msRun>") + 3);
                ByteVariables.scanOffsets.Add(index);

                // write all index offsets
                for (int i = 0; i < ByteVariables.scanOffsets.Count - 1; i++)
                {
                    var offset = ByteVariables.scanOffsets[i];
                    writer.WriteLine("  <offset id=\"" + offset.scanNumber + "\" >" + offset.byteDepth + "</offset>");
                }

                writer.WriteLine(" </index>");
                writer.WriteLine(" <indexOffset>" + ByteVariables.scanOffsets.Last().byteDepth + "</indexOffset>");
                //writer.WriteLine(" <sha1>3aaa100a7ac2cef7a798fbbe9973a6f9c994f208</sha1>");
                writer.Write(" <sha1>");
                writer.Flush();
                writer.Close();
                var mzXmlHash = HashMzXML(writer, outPath);
                writer = new StreamWriter(outPath, true);
                writer.Write(mzXmlHash);
                writer.WriteLine("</sha1>");
                writer.WriteLine("</mzXML>");

                writer.Close();
                writer.Dispose();
                // reset static variables for next iteration
                ByteVariables.byteDepth = 0;
                ByteVariables.scanOffsets.Clear();
            }
        }

        private static string HashRawFile(string filePath)
        {
            var returnString = "";
            using (FileStream fs = new FileStream(filePath, FileMode.Open))
            using (BufferedStream bs = new BufferedStream(fs))
            {
                using (SHA1Managed sha1 = new SHA1Managed())
                {
                    byte[] hash = sha1.ComputeHash(bs);
                    StringBuilder formatted = new StringBuilder(2 * hash.Length);
                    foreach (byte b in hash)
                    {
                        formatted.AppendFormat("{0:X2}", b);
                    }

                    returnString += formatted.ToString().ToLower() + "\" />";
                }
            }

            return returnString;
        }

        private static List<double> GetUniqueCvValues(ThermoRawFile rawFile)
        {
            List<double> returnList = new List<double>();

            var numScans = rawFile.LastSpectrumNumber;

            for (int i = 1; i <= numScans; i++)
            {
                var filterLine = rawFile.GetScanFilter(i).Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var param in filterLine)
                {
                    if (param.Contains("cv="))
                    {
                        var cv = Double.Parse(param.Split('=')[1]);

                        if (!returnList.Contains(cv))
                        {
                            returnList.Add(cv);
                        }
                    }
                }                
            }

            return returnList;
        }

        private static List<int> FindAllTargetScans(double CV, ThermoRawFile rawFile)
        {
            var returnList = new List<int>();

            var numScans = rawFile.LastSpectrumNumber;

            for (int i = 1; i <= numScans; i++)
            {
                var filterLine = rawFile.GetScanFilter(i).Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var param in filterLine)
                {
                    if (param.Contains("cv="))
                    {
                        var cv = Double.Parse(param.Split('=')[1]);

                        if (cv == CV)
                        {
                            returnList.Add(i);
                        }
                    }
                }
            }

            return returnList;
        }

        private static string WriteHeader(StreamWriter writer, string filePath, ThermoRawFile rawFile, string hash, List<int> targetScans)
        {
            var sb = new StringBuilder();

            sb.AppendLine("<?xml version=\"1.0\" encoding=\"ISO-8859-1\"?>");
            sb.AppendLine("<mzXML xmlns=\"http://sashimi.sourceforge.net/schema_revision/mzXML_3.1\"");
            sb.AppendLine(" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"");
            sb.AppendLine(" xsi:schemaLocation=\"http://sashimi.sourceforge.net/schema_revision/mzXML_3.1 http://sashimi.sourceforge.net/schema_revision/mzXML_3.1/mzXML_idx_3.1.xsd\" >");
            sb.AppendLine(WriteMsRunTag(rawFile, targetScans));
            sb.AppendLine(WriteParentFileTag(filePath, hash));
            sb.AppendLine("  <msInstrument>");
            sb.AppendLine("   <msManufacturer category=\"msManufacturer\" value=\"Thermo Finnigan\" />");
            sb.AppendLine("   <msModel category=\"msModel\" value=\"unknown\" />");
            sb.AppendLine("   <msIonisation category=\"msIonisation\" value=\"" + GetIonizationSource(rawFile) + "\" />");
            sb.AppendLine("   <msMassAnalyzer category=\"msMassAnalyzer\" value=\"" + GetMzAnalyzer(rawFile) + "\" />");
            sb.AppendLine("   <msDetector category=\"msDetector\" value=\"unknown\" />");
            sb.AppendLine("   <software type=\"acquisition\" name=\"Xcalibur\" version=\"3.1.2279\" />");
            sb.AppendLine("  </msInstrument>");
            sb.AppendLine("  <dataProcessing centroided=\"1\" >");
            sb.AppendLine("   <software type=\"conversion\" name=\"WriteFaimsXMLFromRawFile\" version=\"1.0\" />");
            sb.Append("  </dataProcessing>");

            var returnString = sb.ToString();
            byteTracker(returnString);
            writer.WriteLine(returnString);
            return returnString;
        }

        private static string WriteMsRunTag(ThermoRawFile rawFile, List<int> targetScans)
        {
            var returnString = " <msRun scanCount=\"" + targetScans.Count + "\" startTime=\"";
            var startTime = "PT" + Math.Round(rawFile.GetMsScan(targetScans.First()).RetentionTime * 60, 8) + "S\" ";
            var endTime = "endTime=\"PT" + Math.Round(rawFile.GetMsScan(targetScans.Last()).RetentionTime * 60, 8) + "S\" >";

            returnString += startTime + endTime;
            return returnString;
        }

        private static string WriteParentFileTag(string filePath, string hash)
        { 
            
            var returnString = "  <parentFile fileName=\"" + Path.GetFileName(filePath) + "\" fileType=\"RAWData\" fileSha1=\"" + hash;

            return returnString;
        }

        private static string GetIonizationSource(ThermoRawFile rawFile)
        {
            var filterLine = rawFile.GetScanFilter(1).Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var param in filterLine)
            {
                if (param.Equals("NSI"))
                {
                    return "NSI";
                } 
                else if (param.Equals("ESI"))
                {
                    return "ESI";
                }
            }
            return "Unknown";
        }

        private static string GetMzAnalyzer(ThermoRawFile rawFile)
        {
            var filterLine = rawFile.GetScanFilter(1).Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var param in filterLine)
            {
                if (param.Equals("FTMS"))
                {
                    return "FTMS";
                }
                else if (param.Equals("ITMS"))
                {
                    return "ITMS";
                }
            }
            return "Unknown";
        }

        public static void byteTracker(string writtenString, bool hasNewLineChars = false)
        {
            if (hasNewLineChars)
            {
                ByteVariables.byteDepth += ByteVariables.encoder.GetByteCount(writtenString);
            }
            else
            {
                ByteVariables.byteDepth += ByteVariables.encoder.GetByteCount(writtenString) + 2;
            }
        }

        private static string HashMzXML(StreamWriter writer, string filePath)
        {
            var returnString = "";

            using (FileStream fs = new FileStream(filePath, FileMode.Open))
            using (BufferedStream bs = new BufferedStream(fs))
            {
                using (SHA1Managed sha1 = new SHA1Managed())
                {
                    byte[] hash = sha1.ComputeHash(bs);
                    StringBuilder formatted = new StringBuilder(2 * hash.Length);
                    foreach (byte b in hash)
                    {
                        formatted.AppendFormat("{0:X2}", b);
                    }

                    returnString += formatted.ToString().ToLower();
                }
            }

            return returnString;
        }
    }
}
