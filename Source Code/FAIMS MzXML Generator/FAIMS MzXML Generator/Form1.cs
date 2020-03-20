using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;

namespace FAIMS_MzXML_Generator
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
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

        private void button2_Click(object sender, EventArgs e)
        {
            SelectFolder();
        }

        private void SelectFolder()
        {
            var browser = new ShFolderBrowser.FolderBrowser.FolderBrowser();

            var success = browser.BrowseForFolder(txtOutputDirectory.Text);

            if (!success)
                return;

            txtOutputDirectory.Text = browser.FolderPath;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            lstInputFiles.Items.Clear();
        }

        private void button5_Click(object sender, EventArgs e)
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

        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
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
                    var firstFile = (string)lstInputFiles.Items[0];

                    var firstFileInfo = new FileInfo(firstFile);
                    txtOutputDirectory.Text = firstFileInfo.DirectoryName;
                }

                var filesToBeSplit = lstInputFiles.Items;

                var processor = new WriteFaimsXMLFromRawFile.FAIMStoMzXMLProcessor();

                var successCount = 0;
                var failureCount = 0;

                foreach (string item in filesToBeSplit)
                {
                    var success = processor.Process(item, txtOutputDirectory.Text);

                    if (success)
                        successCount++;
                    else
                        failureCount++;
                }

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

                var failureMessage = string.Format("Error encountered while processing {0} {1}", failureCount, failureCount == 1 ? "file" : "files"); ;
                MessageBox.Show(failureMessage, "Errors Encountered", MessageBoxButtons.OK);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error converting .raw files: " + ex.Message);
            }
        }
    }
}
