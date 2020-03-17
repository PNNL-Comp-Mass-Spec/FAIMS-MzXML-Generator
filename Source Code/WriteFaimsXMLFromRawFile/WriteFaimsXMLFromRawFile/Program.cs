using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using CSMSL.IO.Thermo;
using System.Security.Cryptography;
using PRISM;
using PRISM.FileProcessor;

namespace WriteFaimsXMLFromRawFile
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                if (args.Length == 0)
                {
                    var exePath = ProcessFilesOrDirectoriesBase.GetAppPath();

                    Console.WriteLine("Syntax:");
                    Console.WriteLine("{0} InstrumentFile.raw [Output_Directory_Path]", exePath);
                    Console.WriteLine();
                    Console.WriteLine("Wild cards are also supported, e.g. *.raw");
                    Console.WriteLine();
                    // ReSharper disable StringLiteralTypo
                    Console.WriteLine("Written by Dain Brademan for the Joshua Coon Research Group (University of Wisconsin) in 2018");
                    Console.WriteLine("Functionality expanded by Matthew Monroe for PNNL (Richland, WA) in 2020");
                    Console.WriteLine("E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov");
                    Console.WriteLine("Website: https://omics.pnl.gov/ or https://panomics.pnnl.gov/");
                    // ReSharper restore StringLiteralTypo
                    return;
                }

                var inputFilePathSpec = args[0];

                string outputDirectoryPath;

                if (args.Length > 1)
                {
                    outputDirectoryPath = args[1];
                }
                else
                {
                    outputDirectoryPath = string.Empty;
                }

                ProcessFiles(inputFilePathSpec, outputDirectoryPath);
            }
            catch (Exception ex)
            {
                ConsoleMsgUtils.ShowError("Error in Program.Main", ex);
            }
        }

        private static void ProcessFiles(string inputFilePathSpec, string outputDirectoryPath)
        {
            if (string.IsNullOrWhiteSpace(inputFilePathSpec))
            {
                ConsoleMsgUtils.ShowWarning("Input file path sent to ProcessFiles is empty; unable to continue");
                return;
            }

            try
            {

                // See if inputFilePath contains a wildcard
                if (inputFilePathSpec.IndexOf('*') >= 0 || inputFilePathSpec.IndexOf('?') >= 0)
                {
                    // Obtain a list of the matching files

                    // Copy the path into cleanPath and replace any * or ? characters with _
                    var cleanPath = inputFilePathSpec.Replace("*", "_").Replace("?", "_");

                    var datasetFile = new FileInfo(cleanPath);
                    string inputDirectoryPath;

                    if (datasetFile.Directory != null && datasetFile.Directory.Exists)
                    {
                        inputDirectoryPath = datasetFile.DirectoryName;
                    }
                    else
                    {
                        // Use the current working directory
                        inputDirectoryPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    }

                    if (string.IsNullOrEmpty(inputDirectoryPath))
                        inputDirectoryPath = ".";

                    var inputDirectory = new DirectoryInfo(inputDirectoryPath);

                    // Remove any directory information from inputFilePathSpec
                    inputFilePathSpec = Path.GetFileName(inputFilePathSpec);

                    var matchCount = 0;

                    DirectoryInfo outputDirectory;
                    if (string.IsNullOrWhiteSpace(outputDirectoryPath))
                    {
                        outputDirectory = inputDirectory;
                    }
                    else
                    {
                        outputDirectory = new DirectoryInfo(outputDirectoryPath);
                    }

                    Console.WriteLine("Input file spec: {0}", inputFilePathSpec);
                    Console.WriteLine("Output directory: {0}", outputDirectory.FullName);

                    if (!outputDirectory.Exists)
                    {
                        Console.WriteLine("Creating missing output directory");
                        outputDirectory.Create();
                    }

                    foreach (var fileItem in inputDirectory.GetFiles(inputFilePathSpec))
                    {
                        ProcessFile(fileItem.FullName, outputDirectory.FullName);

                        matchCount += 1;

                        if (matchCount % 100 == 0)
                            Console.Write(".");
                    }

                    if (matchCount == 0)
                    {
                        ConsoleMsgUtils.ShowWarning("No match was found for the input file path spec:" + inputFilePathSpec);
                    }
                }
                else
                {
                    var inputFile = new FileInfo(inputFilePathSpec);

                    DirectoryInfo outputDirectory;
                    if (string.IsNullOrWhiteSpace(outputDirectoryPath))
                    {
                        outputDirectory = inputFile.Directory ?? new DirectoryInfo(".");
                    }
                    else
                    {
                        outputDirectory = new DirectoryInfo(outputDirectoryPath);
                    }

                    Console.WriteLine("Input file path: {0}", inputFilePathSpec);
                    Console.WriteLine("Output directory: {0}", outputDirectory.FullName);

                    if (!outputDirectory.Exists)
                    {
                        Console.WriteLine("Creating missing output directory");
                        outputDirectory.Create();
                    }

                    ProcessFile(inputFile.FullName, outputDirectory.FullName);
                }
            }
            catch (Exception ex)
            {
                ConsoleMsgUtils.ShowError("Error in ProcessFiles", ex);
            }
        }

        private static void ProcessFile(string filePath, string outputDirectoryPath)
        {
            try
            {
                Console.WriteLine("Processing {0}", filePath);

                // quickly hash the raw file before opening it
                var fileSha1 = HashRawFile(filePath);

                // open up the raw file connection
                var rawFile = new ThermoRawFile(filePath);
                rawFile.Open();

                ConsoleMsgUtils.ShowDebug("Reading CV values");

                // get all unique CV values from scans
                var cvValues = GetUniqueCvValues(rawFile);

                var totalScanCount = rawFile.GetMsScans().Count();
                var scansProcessed = 0;

                var lastProgress = DateTime.UtcNow;

                // now work for each unique CV value (# files we're going to need to split into)
                // get all scans that have the CV that we're currently targeting
                foreach (var cvValue in cvValues)
                {
                    var outPath = outputDirectoryPath + "\\" + Path.GetFileNameWithoutExtension(filePath) + "_" + cvValue + ".mzXML";
                    ConsoleMsgUtils.ShowDebug("Creating file {0}", outPath);

                    var targetScans = FindAllTargetScans(cvValue, rawFile);

                    using (var writer = new StreamWriter(outPath))
                    {

                        WriteHeader(writer, filePath, rawFile, fileSha1, targetScans);

                        Ms1Scan currentScan = null;

                        // write out our target scans
                        foreach (var scanNumber in targetScans)
                        {
                            if (DateTime.UtcNow.Subtract(lastProgress).TotalSeconds >= 3)
                            {
                                var percentComplete = scansProcessed / (double)totalScanCount * 100;

                                ConsoleMsgUtils.ShowDebugCustom(string.Format("... processing: {0:F0}% complete", percentComplete), emptyLinesBeforeMessage: 0);
                                lastProgress = DateTime.UtcNow;
                            }

                            var scan = new MsScan(scanNumber, rawFile);

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
                                currentScan?.AddMs2Scan(ms2Scan);
                            }

                            scansProcessed++;
                        }

                        if (currentScan != null)
                        {
                            // once we're out, we need to write out our last currentScan
                            writer.WriteLine(currentScan.ToXML());
                        }

                        //finish off msRun
                        writer.WriteLine(" </msRun>");

                        writer.WriteLine(" <index name=\"scan\" >");

                        // add special entry to our indexOffset list for where the offsets start
                        var index = new Index(0, ByteVariables.byteDepth + ByteVariables.encoder.GetByteCount(" </msRun>") + 3);
                        ByteVariables.scanOffsets.Add(index);

                        // write all index offsets
                        for (var i = 0; i < ByteVariables.scanOffsets.Count - 1; i++)
                        {
                            var offset = ByteVariables.scanOffsets[i];
                            writer.WriteLine("  <offset id=\"" + offset.scanNumber + "\" >" + offset.byteDepth + "</offset>");
                        }

                        writer.WriteLine(" </index>");
                        writer.WriteLine(" <indexOffset>" + ByteVariables.scanOffsets.Last().byteDepth + "</indexOffset>");
                        writer.Write(" <sha1>");
                    }

                    // Compute the SHA-1 hash of the file up to this point
                    var mzXmlHash = HashMzXML(outPath);

                    // Append the hash
                    using (var writer = new StreamWriter(outPath, true))
                    {
                        writer.Write(mzXmlHash);
                        writer.WriteLine("</sha1>");
                        writer.WriteLine("</mzXML>");
                    }

                    // Reset static variables for next iteration
                    ByteVariables.byteDepth = 0;
                    ByteVariables.scanOffsets.Clear();
                }

                ConsoleMsgUtils.ShowDebugCustom(string.Format("... processing: {0:F0}% complete", 100), emptyLinesBeforeMessage: 0);
                System.Threading.Thread.Sleep(750);
            }
            catch (Exception ex)
            {
                ConsoleMsgUtils.ShowError("Error in ProcessFile", ex);
            }
        }

        private static string HashRawFile(string filePath)
        {
            ConsoleMsgUtils.ShowDebug("Computing the SHA-1 Hash of the .raw file");

            var returnString = "";
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var bs = new BufferedStream(fs))
            {
                using (var sha1 = new SHA1Managed())
                {
                    var hash = sha1.ComputeHash(bs);
                    var formatted = new StringBuilder(2 * hash.Length);
                    foreach (var b in hash)
                    {
                        formatted.AppendFormat("{0:X2}", b);
                    }

                    ConsoleMsgUtils.ShowDebugCustom(string.Format("... {0}", formatted), emptyLinesBeforeMessage: 0);

                    returnString += formatted.ToString().ToLower() + "\" />";
                }
            }

            return returnString;
        }

        private static List<double> GetUniqueCvValues(ThermoRawFile rawFile)
        {
            var returnList = new List<double>();

            var numScans = rawFile.LastSpectrumNumber;

            for (var i = 1; i <= numScans; i++)
            {
                var filterLine = rawFile.GetScanFilter(i).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var param in filterLine)
                {
                    if (param.Contains("cv="))
                    {
                        var cv = double.Parse(param.Split('=')[1]);

                        if (!returnList.Contains(cv))
                        {
                            returnList.Add(cv);
                        }
                    }
                }
            }

            return returnList;
        }

        private static List<int> FindAllTargetScans(double targetCV, ThermoRawFile rawFile)
        {
            var returnList = new List<int>();

            var numScans = rawFile.LastSpectrumNumber;

            for (var i = 1; i <= numScans; i++)
            {
                var filterLine = rawFile.GetScanFilter(i).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var param in filterLine)
                {
                    if (!param.Contains("cv=") ||
                        !double.TryParse(param.Split('=')[1], out var cv))
                        continue;

                    if (Math.Abs(cv - targetCV) < float.Epsilon)
                    {
                        returnList.Add(i);
                    }
                }
            }

            return returnList;
        }

        private static void WriteHeader(TextWriter writer, string filePath, ThermoRawFile rawFile, string hash, IReadOnlyCollection<int> targetScans)
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

            var headerText = sb.ToString();
            ByteTracker(headerText);

            writer.WriteLine(headerText);
        }

        private static string WriteMsRunTag(ThermoRawFile rawFile, IReadOnlyCollection<int> targetScans)
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
            var filterLine = rawFile.GetScanFilter(1);
            var filterLineParts = filterLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var param in filterLineParts)
            {
                if (param.Equals("NSI"))
                {
                    return param;
                }

                if (param.Equals("ESI"))
                {
                    return param;
                }
            }

            ConsoleMsgUtils.ShowWarning("Unrecognized ionization source; filter line does not contain NSI or ESI: " + filterLine);
            return "Unknown";
        }

        private static string GetMzAnalyzer(ThermoRawFile rawFile)
        {
            var filterLine = rawFile.GetScanFilter(1);
            var filterLineParts = filterLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var param in filterLineParts)
            {
                if (param.Equals("FTMS"))
                {
                    return param;
                }

                if (param.Equals("ITMS"))
                {
                    return param;
                }
            }

            ConsoleMsgUtils.ShowWarning("Unrecognized MzAnalyzer; filter line does not contain FTMS or ITMS: " + filterLine);
            return "Unknown";
        }

        public static void ByteTracker(string writtenString, bool hasNewLineChars = false)
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

        private static string HashMzXML(string filePath)
        {
            var returnString = "";

            using (var fs = new FileStream(filePath, FileMode.Open))
            using (var bs = new BufferedStream(fs))
            {
                using (var sha1 = new SHA1Managed())
                {
                    var hash = sha1.ComputeHash(bs);
                    var formatted = new StringBuilder(2 * hash.Length);
                    foreach (var b in hash)
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
