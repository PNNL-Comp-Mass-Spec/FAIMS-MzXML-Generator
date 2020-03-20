using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

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
            OpenFileDialog open = new OpenFileDialog();

            open.Filter = "Raw files (*.raw)|*.raw";
            open.Multiselect = true;
            open.Title = "Please Select FAIMS File(s) for Conversion";

            if (open.ShowDialog() == DialogResult.OK)
            {
                foreach (String file in open.FileNames)
                {
                    listBox1.Items.Add(file);
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
            listBox1.Items.Clear();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            var selectedItems = listBox1.SelectedItems;

            if (listBox1.SelectedIndex != -1)
            {
                for (int i = selectedItems.Count - 1; i >= 0; i--)
                {
                    listBox1.Items.Remove(selectedItems[i]);
                }
            }
            else
            {
                MessageBox.Show("Select a file to remove.");
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (listBox1.Items.Count < 1)
            {
                string message = "Please Select A FAIMS File(s) For Processing.";
                string caption = "Error Detected in Input";
                MessageBoxButtons buttons = MessageBoxButtons.OK;
                DialogResult result;

                // Displays the MessageBox.

                result = MessageBox.Show(message, caption, buttons);
            } 
            else if (String.IsNullOrEmpty(textBox2.Text))
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
