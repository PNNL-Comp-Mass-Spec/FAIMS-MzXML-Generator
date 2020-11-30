using System;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using PRISM;
using PRISM.Logging;

namespace FAIMS_MzXML_Generator
{
    public partial class Form1 : Form
    {
        private WriteFaimsXMLFromRawFile.FAIMStoMzXMLProcessor mProcessor;
        private string mInputFilePath;
        private string mOutputDirectoryPath;
        private bool mProcessingSuccess;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        private void cmdSelectFiles_Click(object sender, EventArgs e)
        {
            SelectFiles();
        }

        private void SelectFiles()
        {
            var open = new OpenFileDialog
            {
                Filter = "Raw files (*.raw)|*.raw",
                Multiselect = true,
                Title = "Please Select FAIMS File(s) for Conversion"
            };

            if (open.ShowDialog() == DialogResult.OK)
            {
                foreach (var file in open.FileNames)
                {
                    lstInputFiles.Items.Add(file);
                }
            }
        }

        private void cmdSelectOutputDirectory_Click(object sender, EventArgs e)
        {
            SelectFolder();
        }

        private void SelectFolder()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtOutputDirectory.Text))
                {
                    AutoDefineOutputDirectory();
                }

                var browser = new Ookii.Dialogs.WinForms.VistaFolderBrowserDialog
                {
                    SelectedPath = txtOutputDirectory.Text
                };

                var result = browser.ShowDialog();

                if (result == DialogResult.OK)
                {
                    txtOutputDirectory.Text = browser.SelectedPath;
                }
            }
            catch (Exception)
            {
                // Ignore errors here
            }
        }

        private void cmdClearAll_Click(object sender, EventArgs e)
        {
            lstInputFiles.Items.Clear();
        }

        private void cmdRemoveFile_Click(object sender, EventArgs e)
        {
            var selectedItems = lstInputFiles.SelectedItems;

            if (lstInputFiles.SelectedIndex != -1)
            {
                for (var i = selectedItems.Count - 1; i >= 0; i--)
                {
                    lstInputFiles.Items.Remove(selectedItems[i]);
                }
            }
            else
            {
                MessageBox.Show("Select a file to remove.");
            }
        }

        private void cmdProcessFiles_Click(object sender, EventArgs e)
        {
            try
            {
                txtProcessingLog.Clear();

                if (lstInputFiles.Items.Count < 1)
                {
                    var errorMessage = "Please Select one or more FAIMS-based Thermo .raw files to process.";
                    var caption = "Error Detected in Input";
                    var buttons = MessageBoxButtons.OK;

                    // Displays the MessageBox.
                    MessageBox.Show(errorMessage, caption, buttons);
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtOutputDirectory.Text))
                {
                    AutoDefineOutputDirectory();
                }

                var filesToBeSplit = lstInputFiles.Items;

                mProcessor = new WriteFaimsXMLFromRawFile.FAIMStoMzXMLProcessor();
                RegisterEvents(mProcessor);

                var successCount = 0;
                var failureCount = 0;

                foreach (string inputFilePath in filesToBeSplit)
                {
                    var success = ProcessFileThreaded(inputFilePath, txtOutputDirectory.Text);

                    if (success)
                        successCount++;
                    else
                        failureCount++;
                }

                mProcessor = null;

                if (successCount > 0 && failureCount == 0)
                {
                    var message = string.Format("Converted {0} {1} to .mzXML files", successCount, successCount == 1 ? "file" : "files");
                    MessageBox.Show(message, "Processing Complete", MessageBoxButtons.OK);
                    return;
                }

                if (successCount > 0 && failureCount > 0)
                {
                    var mixedSuccessMessage = string.Format("Successfully converted {0} {1} to .mzXML files", successCount, successCount == 1 ? "file" : "files") +
                                              string.Format("; error encountered while processing {0} {1}", failureCount, failureCount == 1 ? "file" : "files");
                    MessageBox.Show(mixedSuccessMessage, "Errors Encountered", MessageBoxButtons.OK);
                    return;
                }

                var failureMessage = string.Format("Error encountered while processing {0} {1}", failureCount, failureCount == 1 ? "file" : "files");
                MessageBox.Show(failureMessage, "Errors Encountered", MessageBoxButtons.OK);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error converting .raw files: " + ex.Message);
            }
        }

        private void AppendProcessingStatus(string message, bool precedeWithBlankLink = false)
        {
            // Cross thread - so you don't get the cross-threading exception
            if (InvokeRequired)
            {
                BeginInvoke((MethodInvoker)delegate
                {
                    AppendProcessingStatus(message, precedeWithBlankLink);
                });
                return;
            }

            if (precedeWithBlankLink)
            {
                txtProcessingLog.AppendText(Environment.NewLine);
            }

            txtProcessingLog.AppendText("  " + message + Environment.NewLine);

            Application.DoEvents();
        }

        private void RegisterEvents(IEventNotifier processingClass)
        {
            // processingClass.ProgressUpdate += ProcessingClass_ProgressUpdate;
            processingClass.DebugEvent += ProcessingClass_DebugEvent;
            processingClass.ErrorEvent += ProcessingClass_ErrorEvent;
            processingClass.StatusEvent += ProcessingClass_StatusEvent;
            processingClass.WarningEvent += ProcessingClass_WarningEvent;
        }

        private void AutoDefineOutputDirectory()
        {
            if (lstInputFiles.Items.Count == 0)
                return;

            var firstFile = (string)lstInputFiles.Items[0];

            var firstFileInfo = new FileInfo(firstFile);
            txtOutputDirectory.Text = firstFileInfo.DirectoryName;
        }

        private bool ProcessFileThreaded(string inputFilePath, string outputDirectoryPath)
        {
            try
            {
                mInputFilePath = inputFilePath;
                mOutputDirectoryPath = outputDirectoryPath;

                var processingThread = new Thread(ProcessFileWork);
                processingThread.Start();

                var threadAborted = false;

                // Loop until URL call finishes, or until timeoutSeconds elapses
                while (processingThread.ThreadState != ThreadState.Stopped)
                {
                    ProgRunner.SleepMilliseconds(25);

                    if (processingThread.ThreadState == ThreadState.Aborted)
                    {
                        threadAborted = true;
                        break;
                    }

                    Application.DoEvents();
                }

                if (threadAborted)
                {
                    ShowErrorMessage("The processing thread was aborted");
                    return false;
                }

                return mProcessingSuccess;
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Error in ProcessFileThreaded", ex);
                return false;
            }
        }

        private void ProcessFileWork()
        {
            mProcessingSuccess = false;
            mProcessingSuccess = mProcessor.ProcessSingleFile(mInputFilePath, mOutputDirectoryPath);
        }

        private void ProcessingClass_DebugEvent(string message)
        {
            AppendProcessingStatus("  " + message);
        }

        private void ProcessingClass_ErrorEvent(string message, Exception ex)
        {
            ShowErrorMessage(message, ex);
        }

        private void ProcessingClass_StatusEvent(string message)
        {
            AppendProcessingStatus(message);
        }

        private void ProcessingClass_WarningEvent(string message)
        {
            AppendProcessingStatus(message, true);
        }

        private void ShowErrorMessage(string message, Exception ex = null)
        {
            if (ex == null || message.Contains(ex.Message))
            {
                AppendProcessingStatus(message, true);
            }
            else
            {
                AppendProcessingStatus(message + ": " + ex.Message, true);
            }

            if (ex == null)
                return;

            var stackTrace = StackTraceFormatter.GetExceptionStackTraceMultiLine(ex, true, false);
            AppendProcessingStatus(stackTrace, true);
        }
    }
}
