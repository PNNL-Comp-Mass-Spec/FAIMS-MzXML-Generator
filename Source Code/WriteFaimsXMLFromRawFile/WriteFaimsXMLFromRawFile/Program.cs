using System;
using System.IO;
using PRISM;
using PRISM.FileProcessor;
using PRISM.Logging;

namespace WriteFaimsXMLFromRawFile
{
    class Program
    {
        private const string PROGRAM_DATE = "2020-03-26";

        static void Main(string[] args)
        {
            try
            {
                if (args.Length == 0)
                {
                    var exePath = ProcessFilesOrDirectoriesBase.GetAppPath();

                    Console.WriteLine("WriteFaimsXMLFromRawFile version " + GetAppVersion());
                    Console.WriteLine();
                    Console.WriteLine(ConsoleMsgUtils.WrapParagraph(
                        "This program converts a Thermo .raw file with FAIMS scans into a series of .mzXML files, " +
                        "creating one .mzXML file for each FAIMS compensation voltage (CV) value in the .raw file."));
                    Console.WriteLine();
                    Console.WriteLine("Syntax:");
                    Console.WriteLine("{0} InstrumentFile.raw [Output_Directory_Path]", Path.GetFileName(exePath));
                    Console.WriteLine();
                    Console.WriteLine("Wild cards are supported, e.g. *.raw");
                    Console.WriteLine();

                    // ReSharper disable StringLiteralTypo
                    Console.WriteLine("Program written by Dain Brademan for the Joshua Coon Research Group (University of Wisconsin) in 2018");
                    Console.WriteLine("Converted to use ThermoFisher.CommonCore DLLs by Matthew Monroe for PNNL (Richland, WA) in 2020");
                    Console.WriteLine("E-mail: brademan@wisc.edu or matthew.monroe@pnnl.gov or proteomics@pnnl.gov");
                    Console.WriteLine("Website: https://github.com/PNNL-Comp-Mass-Spec/FAIMS-MzXML-Generator/releases or");
                    Console.WriteLine("         https://github.com/coongroup/FAIMS-MzXML-Generator");
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

                var processor = new FAIMStoMzXMLProcessor();
                RegisterEvents(processor);

                processor.QuickCVLookup = false;

                var success = processor.ProcessFiles(inputFilePathSpec, outputDirectoryPath);

                if (success)
                {
                    Console.WriteLine("Processing completed");
                }

                System.Threading.Thread.Sleep(750);

            }
            catch (Exception ex)
            {
                ConsoleMsgUtils.ShowError("Error in Program.Main", ex);
            }
        }

        private static string GetAppVersion()
        {
            return ProcessFilesOrDirectoriesBase.GetAppVersion(PROGRAM_DATE);
        }

        private static void RegisterEvents(IEventNotifier processingClass)
        {
            // processingClass.ProgressUpdate += ProcessingClass_ProgressUpdate;
            processingClass.DebugEvent += ProcessingClass_DebugEvent;
            processingClass.ErrorEvent += ProcessingClass_ErrorEvent;
            processingClass.StatusEvent += ProcessingClass_StatusEvent;
            processingClass.WarningEvent += ProcessingClass_WarningEvent;
        }

        private static void ProcessingClass_DebugEvent(string message)
        {
            ConsoleMsgUtils.ShowDebugCustom(message, emptyLinesBeforeMessage: 0);
        }

        private static void ProcessingClass_ErrorEvent(string message, Exception ex)
        {
            ShowErrorMessage(message, ex);
        }

        private static void ProcessingClass_StatusEvent(string message)
        {
            Console.WriteLine(message);
        }

        private static void ProcessingClass_WarningEvent(string message)
        {
            ShowWarningMessage(message);
        }

        private static void ShowErrorMessage(string message, Exception ex = null)
        {
            ConsoleMsgUtils.ShowError(message, ex);
        }

        private static void ShowWarningMessage(string message, int emptyLinesBeforeMessage = 1)
        {
            ConsoleMsgUtils.ShowWarningCustom(message, emptyLinesBeforeMessage);
        }
    }
}
