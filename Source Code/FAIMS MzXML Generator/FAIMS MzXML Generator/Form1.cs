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
            if (lstInputFiles.Items.Count < 1)
            {
                var message = "Please Select on or more FAIMS-based Thermo .raw files to process.";
                var caption = "Error Detected in Input";
                var buttons = MessageBoxButtons.OK;

                // Displays the MessageBox.
                MessageBox.Show(message, caption, buttons);
                return;
            }

            {
                string message = "Please Select An Output Destination.";
                string caption = "Error Detected in Input";
                MessageBoxButtons buttons = MessageBoxButtons.OK;
                DialogResult result;

                // Displays the MessageBox.

                result = MessageBox.Show(message, caption, buttons);
            }
            else
            {
                var filesToBeSplit = listBox1.Items;

                foreach (string item in filesToBeSplit)
                {
                    Process process = new Process();
                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.CreateNoWindow = false;
                    startInfo.UseShellExecute = false;
                    startInfo.FileName = "Rawfile To MzXML/WriteFaimsXMLFromRawFile.exe";
                    startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    startInfo.Arguments = "\"" + item + "\" \"" + textBox2.Text + "\"";

                    process.StartInfo = startInfo;
                    process.Start();

                    // until we are done
                    process.WaitForExit();
                }

                MessageBox.Show("Done.");
            }
        }
    }
}
