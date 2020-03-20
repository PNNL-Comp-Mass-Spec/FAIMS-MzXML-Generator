using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using PRISM;
using PRISM.FileProcessor;
using ThermoRawFileReader;

namespace WriteFaimsXMLFromRawFile
{
    class Program
    {
        /// <summary>
        /// This Regex matches scan filters of the form
        /// FTMS + p NSI cv=-45.00 Full ms
        /// ITMS + c NSI cv=-65.00 r d Full ms2 438.7423@cid35.00
        /// </summary>
        private static readonly Regex mCvMatcher = new Regex("cv=(?<CV>[0-9.+-]+)");

        /// <summary>
        /// Keys in this dictionary are .raw file names
        /// Values are a list of scans that do not have cv= or have an invalid number after the equals sign
        /// </summary>
        /// <remarks>This is used to limit the number of warnings reported by GetCvValue</remarks>
        private static readonly Dictionary<string, List<int>> mCvScanWarnings = new Dictionary<string, List<int>>();

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
                var reader = new XRawFileIO(filePath);

                ConsoleMsgUtils.ShowDebug("Reading CV values");

                // get all unique CV values from scans
                var cvValues = GetUniqueCvValues(reader);

                var totalScanCount = reader.GetNumScans();
                var scansProcessed = 0;

                var lastProgress = DateTime.UtcNow;

                // Change this to true to create a tab-delimited text listing the MS1 and MS2 scans in the .mzXML file
                var createScanMapFile = false;

                // now work for each unique CV value (# files we're going to need to split into)
                // get all scans that have the CV that we're currently targeting
                foreach (var cvValue in cvValues)
                {
                    var baseName = Path.GetFileNameWithoutExtension(filePath);

                    var mzXmlPath = Path.Combine(outputDirectoryPath, baseName + "_" + cvValue + ".mzXML");
                    var scanMapFilePath = Path.Combine(outputDirectoryPath, baseName + "_ScanMap_" + cvValue + ".txt");

                    ConsoleMsgUtils.ShowDebug("Creating file {0}", mzXmlPath);

                    var targetScans = FindAllTargetScans(cvValue, reader);

                    var parentScanToMS2Map = new Dictionary<int, List<int>>();

                    using (var writer = new StreamWriter(new FileStream(mzXmlPath, FileMode.Create, FileAccess.Write, FileShare.Read)))
                    {

                        WriteHeader(writer, filePath, reader, fileSha1, targetScans);

                        Ms1Scan currentMS1Scan = null;

                        // write out our target scans
                        foreach (var scanNumber in targetScans)
                        {
                            if (DateTime.UtcNow.Subtract(lastProgress).TotalSeconds >= 3)
                            {
                                var percentComplete = scansProcessed / (double)totalScanCount * 100;

                                ConsoleMsgUtils.ShowDebugCustom(string.Format("... processing: {0:F0}% complete", percentComplete), emptyLinesBeforeMessage: 0);
                                lastProgress = DateTime.UtcNow;
                            }

                            var scan = new MsScan(scanNumber, reader);

                            if (scan.msLevel == 1)
                            {
                                parentScanToMS2Map.Add(scan.ScanNumber, new List<int>());

                                if (currentMS1Scan == null)
                                {
                                    // start condition
                                    currentMS1Scan = Ms1Scan.Create(scan);
                                }
                                else
                                {
                                    // write currentMS1Scan to file
                                    var outString = currentMS1Scan.ToXML();
                                    writer.WriteLine(outString);

                                    currentMS1Scan = Ms1Scan.Create(scan);
                                }
                            }
                            else if (scan.msLevel == 2)
                            {
                                if (currentMS1Scan != null)
                                {
                                    parentScanToMS2Map[currentMS1Scan.ScanNumber].Add(scan.ScanNumber);
                                }

                                var ms2Scan = Ms2Scan.Create(scan);
                                ms2Scan.AddMs2ScanParameters(reader);
                                currentMS1Scan?.AddMs2Scan(ms2Scan);
                            }

                            scansProcessed++;
                        }

                        if (currentMS1Scan != null)
                        {
                            // once we're out, we need to write out our last currentMS1Scan
                            writer.WriteLine(currentMS1Scan.ToXML());
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
                    var mzXmlHash = HashMzXML(mzXmlPath);

                    // Append the hash
                    using (var writer = new StreamWriter(mzXmlPath, true))
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

        private static void AddToScanWarningList(string readerRawFilePath, int scanNumber, string warningMessage)
        {
            if (mCvScanWarnings.TryGetValue(readerRawFilePath, out var scanNumbers))
            {
                scanNumbers.Add(scanNumber);
            }
            else
            {
                scanNumbers = new List<int> { scanNumber };
                mCvScanWarnings.Add(readerRawFilePath, scanNumbers);
            }

            if (scanNumbers.Count < 10 || scanNumber % 100 == 0)
            {
                ConsoleMsgUtils.ShowWarning(warningMessage);
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

        private static bool GetCvValue(XRawFileIO reader, int scanNumber, out float cvValue, out string filterTextMatch, bool showWarnings = false)
        {
            cvValue = 0;
            filterTextMatch = string.Empty;

            if (!reader.GetScanInfo(scanNumber, out clsScanInfo scanInfo))
            {
                if (showWarnings)
                {
                    ConsoleMsgUtils.ShowWarning("Scan {0} not found; skipping", scanNumber);
                }
                return false;
            }

            var filterText = scanInfo.FilterText;

            if (filterText.IndexOf("cv=", StringComparison.OrdinalIgnoreCase) < 0)
            {
                if (showWarnings)
                {
                    AddToScanWarningList(
                        reader.RawFilePath, scanNumber,
                        string.Format("Scan {0} does not contain cv=; skipping", scanNumber));
                }
                return false;
            }

            var match = mCvMatcher.Match(filterText);

            if (!match.Success)
            {
                if (showWarnings)
                {
                    AddToScanWarningList(
                        reader.RawFilePath, scanNumber,
                        string.Format(
                            "Scan {0} has cv= in the filter text, but it is not followed by a number: {1}",
                            scanNumber, filterText));
                }

                return false;
            }

            if (!float.TryParse(match.Groups["CV"].Value, out cvValue))
            {
                if (showWarnings)
                {
                    AddToScanWarningList(
                        reader.RawFilePath, scanNumber,
                        string.Format(
                            "Unable to parse the CV value for scan {0}: {1}",
                            scanNumber, match.Groups["CV"].Value));
                }

                return false;
            }

            filterTextMatch = match.Value;
            return true;
        }

        private static SortedSet<float> GetUniqueCvValues(XRawFileIO reader)
        {

            // Dictionary where keys are CV values and values are the filter text that scans with this CV value will have
            var cvValues = new SortedSet<float>();

            for (var scanNumber = reader.ScanStart; scanNumber <= reader.ScanEnd; scanNumber++)
            {
                var success = GetCvValue(reader, scanNumber, out var cvValue, out _, true);
                if (!success)
                    continue;

                if (cvValues.Contains(cvValue))
                {
                    continue;
                }

                cvValues.Add(cvValue);
            }

            return cvValues;
        }

        private static List<int> FindAllTargetScans(double targetCV, XRawFileIO reader)
        {
            var targetScans = new List<int>();

            var startScan = reader.ScanStart;
            var endScan = reader.ScanEnd;

            for (var scanNumber = startScan; scanNumber <= endScan; scanNumber++)
            {
                var success = GetCvValue(reader, scanNumber, out var cvValue, out _);
                if (!success)
                    continue;

                if (Math.Abs(cvValue - targetCV) < float.Epsilon)
                {
                    targetScans.Add(scanNumber);
                }
            }

            return targetScans;
        }

        private static void WriteHeader(TextWriter writer, string filePath, XRawFileIO reader, string hash, IReadOnlyCollection<int> targetScans)
        {
            var version = ProcessFilesOrDirectoriesBase.GetEntryOrExecutingAssembly().GetName().Version;

            var sb = new StringBuilder();

            sb.AppendLine("<?xml version=\"1.0\" encoding=\"ISO-8859-1\"?>");
            sb.AppendLine("<mzXML xmlns=\"http://sashimi.sourceforge.net/schema_revision/mzXML_3.1\"");
            sb.AppendLine(" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"");
            sb.AppendLine(" xsi:schemaLocation=\"http://sashimi.sourceforge.net/schema_revision/mzXML_3.1 http://sashimi.sourceforge.net/schema_revision/mzXML_3.1/mzXML_idx_3.1.xsd\" >");
            sb.AppendLine(WriteMsRunTag(reader, targetScans));
            sb.AppendLine(WriteParentFileTag(filePath, hash));
            sb.AppendLine("  <msInstrument>");
            sb.AppendLine("   <msManufacturer category=\"msManufacturer\" value=\"Thermo Finnigan\" />");
            sb.AppendLine("   <msModel category=\"msModel\" value=\"unknown\" />");
            sb.AppendLine("   <msIonisation category=\"msIonisation\" value=\"" + GetIonizationSource(reader) + "\" />");
            sb.AppendLine("   <msMassAnalyzer category=\"msMassAnalyzer\" value=\"" + GetMzAnalyzer(reader) + "\" />");
            sb.AppendLine("   <msDetector category=\"msDetector\" value=\"unknown\" />");
            sb.AppendLine("   <software type=\"acquisition\" name=\"Xcalibur\" version=\"3.1.2279\" />");
            sb.AppendLine("  </msInstrument>");
            sb.AppendLine("  <dataProcessing centroided=\"1\" >");
            sb.AppendLine(string.Format(
                "   <software type=\"conversion\" name=\"WriteFaimsXMLFromRawFile\" version=\"{0}\" />", version));
            sb.Append("  </dataProcessing>");

            var headerText = sb.ToString();
            ByteTracker(headerText);

            writer.WriteLine(headerText);
        }

        private static string WriteMsRunTag(XRawFileIO reader, IReadOnlyCollection<int> targetScans)
        {
            if (targetScans.Count == 0)
            {
                ConsoleMsgUtils.ShowWarning("targetScans sent to WriteMsRunTag is empty; cannot create a valid .mzXML file");
                return string.Empty;
            }

            var startScanFound = reader.GetScanInfo(targetScans.First(), out clsScanInfo scanFirst);
            if (!startScanFound)
            {
                ConsoleMsgUtils.ShowWarning("Unable to find scan {0} in WriteMsRunTag", targetScans.First());
                return string.Empty;
            }

            var endScanFound = reader.GetScanInfo(targetScans.Last(), out clsScanInfo scanLast);
            if (!endScanFound)
            {
                ConsoleMsgUtils.ShowWarning("Unable to find scan {0} in WriteMsRunTag", targetScans.Last());
                return string.Empty;
            }

            var returnString = " <msRun scanCount=\"" + targetScans.Count + "\" startTime=\"";
            var startTime = "PT" + Math.Round(scanFirst.RetentionTime * 60, 8) + "S\" ";
            var endTime = "endTime=\"PT" + Math.Round(scanLast.RetentionTime * 60, 8) + "S\" >";

            return returnString + startTime + endTime;
        }

        private static string WriteParentFileTag(string filePath, string hash)
        {
            var returnString = "  <parentFile fileName=\"" + Path.GetFileName(filePath) + "\" fileType=\"RAWData\" fileSha1=\"" + hash;

            return returnString;
        }

        private static string GetIonizationSource(XRawFileIO reader)
        {
            var startScan = reader.ScanStart;

            var success = reader.GetScanInfo(startScan, out clsScanInfo scanInfo);
            if (!success)
            {
                ConsoleMsgUtils.ShowWarning("Scan {0} not found in GetIonizationSource", startScan);
                return "Unknown";
            }

            var filterLineParts = scanInfo.FilterText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

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

            ConsoleMsgUtils.ShowWarning("Unrecognized ionization source; filter line does not contain NSI or ESI: " + scanInfo.FilterText);
            return "Unknown";
        }

        private static string GetMzAnalyzer(XRawFileIO reader)
        {
            var startScan = reader.ScanStart;

            var success = reader.GetScanInfo(startScan, out clsScanInfo scanInfo);
            if (!success)
            {
                ConsoleMsgUtils.ShowWarning("Scan {0} not found in GetIonizationSource", startScan);
                return "Unknown";
            }

            var filterLineParts = scanInfo.FilterText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

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

            ConsoleMsgUtils.ShowWarning("Unrecognized MzAnalyzer; filter line does not contain FTMS or ITMS: " + scanInfo.FilterText);
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
