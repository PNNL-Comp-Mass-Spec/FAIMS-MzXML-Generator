using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using PRISM;
using PRISM.FileProcessor;
using ThermoRawFileReader;

namespace WriteFaimsXMLFromRawFile
{
    // ReSharper disable once IdentifierTypo
    public class FAIMStoMzXMLProcessor : EventNotifier
    {
        /// <summary>
        /// This Regex matches scan filters of the form
        /// FTMS + p NSI cv=-45.00 Full ms
        /// ITMS + c NSI cv=-65.00 r d Full ms2 438.7423@cid35.00
        /// </summary>
        private readonly Regex mCvMatcher = new Regex("cv=(?<CV>[0-9.+-]+)");

        /// <summary>
        /// Keys in this dictionary are .raw file names
        /// Values are a list of scans that do not have cv= or have an invalid number after the equals sign
        /// </summary>
        /// <remarks>This is used to limit the number of warnings reported by GetCvValue</remarks>
        private readonly Dictionary<string, List<int>> mCvScanWarnings = new Dictionary<string, List<int>>();

        public ByteVariables ByteTracking { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        // ReSharper disable once IdentifierTypo
        public FAIMStoMzXMLProcessor()
        {
            ByteTracking = new ByteVariables();
        }

        /// <summary>
        /// Convert .raw files to .mzXML files
        /// </summary>
        /// <param name="inputFilePathSpec"></param>
        /// <param name="outputDirectoryPath"></param>
        /// <returns></returns>
        public bool ProcessFiles(string inputFilePathSpec, string outputDirectoryPath)
        {
            if (string.IsNullOrWhiteSpace(inputFilePathSpec))
            {
                OnWarningEvent("Input file path sent to ProcessFiles is empty; unable to continue");
                return false;
            }

            try
            {
                bool success;

                // See if inputFilePath contains a wildcard
                if (inputFilePathSpec.IndexOf('*') >= 0 || inputFilePathSpec.IndexOf('?') >= 0)
                {
                    success = ProcessFilesWildcard(inputFilePathSpec, outputDirectoryPath);
                }
                else
                {
                    success = ProcessSingleFile(inputFilePathSpec, outputDirectoryPath);
                }

                return success;
            }
            catch (Exception ex)
            {
                OnErrorEvent("Error in ProcessFiles", ex);
                return false;
            }
        }

        public bool ProcessSingleFile(string inputFilePath, string outputDirectoryPath)
        {

            var inputFile = new FileInfo(inputFilePath);

            DirectoryInfo outputDirectory;
            if (string.IsNullOrWhiteSpace(outputDirectoryPath))
            {
                outputDirectory = inputFile.Directory ?? new DirectoryInfo(".");
            }
            else
            {
                outputDirectory = new DirectoryInfo(outputDirectoryPath);
            }

            OnStatusEvent(string.Format("Input file path: {0}", inputFilePath));
            OnStatusEvent(string.Format("Output directory: {0}", outputDirectory.FullName));

            if (!outputDirectory.Exists)
            {
                OnStatusEvent("Creating missing output directory");
                outputDirectory.Create();
            }

            var success = ProcessFile(inputFile.FullName, outputDirectory.FullName);
            return success;
        }

        private bool ProcessFilesWildcard(string inputFilePathSpec, string outputDirectoryPath)
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

            OnStatusEvent(string.Format("Input file spec: {0}", inputFilePathSpec));
            OnStatusEvent(string.Format("Output directory: {0}", outputDirectory.FullName));

            if (!outputDirectory.Exists)
            {
                OnStatusEvent("Creating missing output directory");
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
                OnWarningEvent("No match was found for the input file path spec:" + inputFilePathSpec);
                return false;
            }

            return true;
        }

        private bool ProcessFile(string filePath, string outputDirectoryPath)
        {
            try
            {
                OnStatusEvent(string.Format("Processing {0}", filePath));

                // quickly hash the raw file before opening it
                var fileSha1 = HashRawFile(filePath);

                // open up the raw file connection
                var reader = new XRawFileIO(filePath);

                // get all unique CV values from scans
                var cvValues = GetUniqueCvValues(reader);

                var totalScanCount = reader.GetNumScans();
                var scansProcessed = 0;

                var lastProgress = DateTime.UtcNow;

                // now work for each unique CV value (# files we're going to need to split into)
                // get all scans that have the CV that we're currently targeting
                foreach (var cvValue in cvValues)
                {

                    // Reset variables for next iteration
                    ByteTracking.Reset();

                    var baseName = Path.GetFileNameWithoutExtension(filePath);

                    var mzXmlPath = Path.Combine(outputDirectoryPath, baseName + "_" + cvValue + ".mzXML");

                    OnDebugEvent(string.Format("Creating file {0}", mzXmlPath));

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

                                OnDebugEvent(string.Format("... processing: {0:F0}% complete", percentComplete));
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
                                    var outString = currentMS1Scan.ToXML(this);
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
                            writer.WriteLine(currentMS1Scan.ToXML(this));
                        }

                        //finish off msRun
                        writer.WriteLine(" </msRun>");

                        writer.WriteLine(" <index name=\"scan\" >");

                        // add special entry to our indexOffset list for where the offsets start
                        var index = new Index(0, ByteTracking.ByteDepth + ByteTracking.Encoder.GetByteCount(" </msRun>") + 3);
                        ByteTracking.ScanOffsets.Add(index);

                        // write all index offsets
                        for (var i = 0; i < ByteTracking.ScanOffsets.Count - 1; i++)
                        {
                            var offset = ByteTracking.ScanOffsets[i];
                            writer.WriteLine("  <offset id=\"" + offset.ScanNumber + "\" >" + offset.ByteDepth + "</offset>");
                        }

                        writer.WriteLine(" </index>");
                        writer.WriteLine(" <indexOffset>" + ByteTracking.ScanOffsets.Last().ByteDepth + "</indexOffset>");
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

                }

                OnDebugEvent(string.Format("... processing: {0:F0}% complete", 100));

                return true;
            }
            catch (Exception ex)
            {
                OnErrorEvent("Error in ProcessFile", ex);
                return false;
            }
        }

        private void AddToScanWarningList(string readerRawFilePath, int scanNumber, string warningMessage)
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
                OnWarningEvent(warningMessage);
            }
        }

        private string HashRawFile(string filePath)
        {
            OnDebugEvent("Computing the SHA-1 Hash of the .raw file");

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

                    OnDebugEvent(string.Format("... {0}", formatted));

                    returnString += formatted.ToString().ToLower() + "\" />";
                }
            }

            return returnString;
        }

        private bool GetCvValue(XRawFileIO reader, int scanNumber, out float cvValue, out string filterTextMatch, bool showWarnings = false)
        {
            cvValue = 0;
            filterTextMatch = string.Empty;

            if (!reader.GetScanInfo(scanNumber, out clsScanInfo scanInfo))
            {
                if (showWarnings)
                {
                    OnWarningEvent(string.Format("Scan {0} not found; skipping", scanNumber));
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

        private SortedSet<float> GetUniqueCvValues(XRawFileIO reader)
        {

            OnDebugEvent("Determining FAIMS CV values");

            var lastStatus = DateTime.UtcNow;
            var progressShown = false;

            // Dictionary where keys are CV values and values are the filter text that scans with this CV value will have
            var cvValues = new SortedSet<float>();

            for (var scanNumber = reader.ScanStart; scanNumber <= reader.ScanEnd; scanNumber++)
            {
                if (DateTime.UtcNow.Subtract(lastStatus).TotalSeconds >= 3)
                {
                    var percentComplete = scanNumber / (float)reader.ScanEnd * 100;
                    OnDebugEvent(string.Format(" ... {0:F0}% of scans examined", percentComplete));

                    lastStatus = DateTime.UtcNow;
                    progressShown = true;
                }

                var success = GetCvValue(reader, scanNumber, out var cvValue, out _, true);
                if (!success)
                    continue;

                if (cvValues.Contains(cvValue))
                {
                    continue;
                }

                cvValues.Add(cvValue);
            }

            if (progressShown)
            {
                OnDebugEvent(string.Format(" ... {0:F0}% of scans examined", 100));
            }
            return cvValues;
        }

        private List<int> FindAllTargetScans(double targetCV, XRawFileIO reader)
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

        private void WriteHeader(TextWriter writer, string filePath, XRawFileIO reader, string hash, IReadOnlyCollection<int> targetScans)
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

        private string WriteMsRunTag(XRawFileIO reader, IReadOnlyCollection<int> targetScans)
        {
            if (targetScans.Count == 0)
            {
                OnWarningEvent("targetScans sent to WriteMsRunTag is empty; cannot create a valid .mzXML file");
                return string.Empty;
            }

            var startScanFound = reader.GetScanInfo(targetScans.First(), out clsScanInfo scanFirst);
            if (!startScanFound)
            {
                OnWarningEvent(string.Format("Unable to find scan {0} in WriteMsRunTag", targetScans.First()));
                return string.Empty;
            }

            var endScanFound = reader.GetScanInfo(targetScans.Last(), out clsScanInfo scanLast);
            if (!endScanFound)
            {
                OnWarningEvent(string.Format("Unable to find scan {0} in WriteMsRunTag", targetScans.Last()));
                return string.Empty;
            }

            var returnString = " <msRun scanCount=\"" + targetScans.Count + "\" startTime=\"";
            var startTime = "PT" + Math.Round(scanFirst.RetentionTime * 60, 8) + "S\" ";
            var endTime = "endTime=\"PT" + Math.Round(scanLast.RetentionTime * 60, 8) + "S\" >";

            return returnString + startTime + endTime;
        }

        private string WriteParentFileTag(string filePath, string hash)
        {
            var returnString = "  <parentFile fileName=\"" + Path.GetFileName(filePath) + "\" fileType=\"RAWData\" fileSha1=\"" + hash;

            return returnString;
        }

        private string GetIonizationSource(XRawFileIO reader)
        {
            var startScan = reader.ScanStart;

            var success = reader.GetScanInfo(startScan, out clsScanInfo scanInfo);
            if (!success)
            {
                OnWarningEvent(string.Format("Scan {0} not found in GetIonizationSource", startScan));
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

            OnWarningEvent("Unrecognized ionization source; filter line does not contain NSI or ESI: " + scanInfo.FilterText);
            return "Unknown";
        }

        private string GetMzAnalyzer(XRawFileIO reader)
        {
            var startScan = reader.ScanStart;

            var success = reader.GetScanInfo(startScan, out clsScanInfo scanInfo);
            if (!success)
            {
                OnWarningEvent(string.Format("Scan {0} not found in GetIonizationSource", startScan));
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

            OnWarningEvent("Unrecognized MzAnalyzer; filter line does not contain FTMS or ITMS: " + scanInfo.FilterText);
            return "Unknown";
        }

        public void ByteTracker(string writtenString, bool hasNewLineChars = false)
        {
            if (hasNewLineChars)
            {
                ByteTracking.ByteDepth += ByteTracking.Encoder.GetByteCount(writtenString);
            }
            else
            {
                ByteTracking.ByteDepth += ByteTracking.Encoder.GetByteCount(writtenString) + 2;
            }
        }

        private string HashMzXML(string filePath)
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
