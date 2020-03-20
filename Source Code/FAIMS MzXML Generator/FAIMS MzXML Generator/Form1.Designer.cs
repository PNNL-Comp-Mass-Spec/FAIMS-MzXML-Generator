namespace FAIMS_MzXML_Generator
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.cmdProcessFiles = new System.Windows.Forms.Button();
            this.cmdSelectFiles = new System.Windows.Forms.Button();
            this.cmdSelectOutputDirectory = new System.Windows.Forms.Button();
            this.lblOutputDirectory = new System.Windows.Forms.Label();
            this.txtOutputDirectory = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.lstInputFiles = new System.Windows.Forms.ListBox();
            this.cmdClearAll = new System.Windows.Forms.Button();
            this.cmdRemoveFile = new System.Windows.Forms.Button();
            this.txtProcessingLog = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // cmdProcessFiles
            // 
            this.cmdProcessFiles.Location = new System.Drawing.Point(16, 246);
            this.cmdProcessFiles.Margin = new System.Windows.Forms.Padding(4);
            this.cmdProcessFiles.Name = "cmdProcessFiles";
            this.cmdProcessFiles.Size = new System.Drawing.Size(651, 44);
            this.cmdProcessFiles.TabIndex = 19;
            this.cmdProcessFiles.Text = "Create MzXMLs";
            this.cmdProcessFiles.UseVisualStyleBackColor = true;
            this.cmdProcessFiles.Click += new System.EventHandler(this.cmdProcessFiles_Click);
            // 
            // cmdSelectFiles
            // 
            this.cmdSelectFiles.Location = new System.Drawing.Point(567, 33);
            this.cmdSelectFiles.Margin = new System.Windows.Forms.Padding(4);
            this.cmdSelectFiles.Name = "cmdSelectFiles";
            this.cmdSelectFiles.Size = new System.Drawing.Size(100, 28);
            this.cmdSelectFiles.TabIndex = 18;
            this.cmdSelectFiles.Text = "Browse";
            this.cmdSelectFiles.UseVisualStyleBackColor = true;
            this.cmdSelectFiles.Click += new System.EventHandler(this.cmsSelectFiles_Click);
            // 
            // cmdSelectOutputDirectory
            // 
            this.cmdSelectOutputDirectory.Location = new System.Drawing.Point(567, 193);
            this.cmdSelectOutputDirectory.Margin = new System.Windows.Forms.Padding(4);
            this.cmdSelectOutputDirectory.Name = "cmdSelectOutputDirectory";
            this.cmdSelectOutputDirectory.Size = new System.Drawing.Size(100, 28);
            this.cmdSelectOutputDirectory.TabIndex = 17;
            this.cmdSelectOutputDirectory.Text = "Browse";
            this.cmdSelectOutputDirectory.UseVisualStyleBackColor = true;
            this.cmdSelectOutputDirectory.Click += new System.EventHandler(this.cmdSelectOutputDirectory_Click);
            // 
            // lblOutputDirectory
            // 
            this.lblOutputDirectory.AutoSize = true;
            this.lblOutputDirectory.Location = new System.Drawing.Point(16, 177);
            this.lblOutputDirectory.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblOutputDirectory.Name = "lblOutputDirectory";
            this.lblOutputDirectory.Size = new System.Drawing.Size(110, 17);
            this.lblOutputDirectory.TabIndex = 16;
            this.lblOutputDirectory.Text = "Output directory";
            // 
            // txtOutputDirectory
            // 
            this.txtOutputDirectory.Location = new System.Drawing.Point(16, 197);
            this.txtOutputDirectory.Margin = new System.Windows.Forms.Padding(4);
            this.txtOutputDirectory.Name = "txtOutputDirectory";
            this.txtOutputDirectory.Size = new System.Drawing.Size(541, 22);
            this.txtOutputDirectory.TabIndex = 15;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(16, 14);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(101, 17);
            this.label1.TabIndex = 13;
            this.label1.Text = "Raw File Paths";
            // 
            // lstInputFiles
            // 
            this.lstInputFiles.FormattingEnabled = true;
            this.lstInputFiles.ItemHeight = 16;
            this.lstInputFiles.Location = new System.Drawing.Point(21, 33);
            this.lstInputFiles.Margin = new System.Windows.Forms.Padding(4);
            this.lstInputFiles.Name = "lstInputFiles";
            this.lstInputFiles.Size = new System.Drawing.Size(536, 132);
            this.lstInputFiles.TabIndex = 20;
            // 
            // cmdClearAll
            // 
            this.cmdClearAll.Location = new System.Drawing.Point(567, 138);
            this.cmdClearAll.Margin = new System.Windows.Forms.Padding(4);
            this.cmdClearAll.Name = "cmdClearAll";
            this.cmdClearAll.Size = new System.Drawing.Size(100, 28);
            this.cmdClearAll.TabIndex = 21;
            this.cmdClearAll.Text = "Clear All";
            this.cmdClearAll.UseVisualStyleBackColor = true;
            this.cmdClearAll.Click += new System.EventHandler(this.cmdClearAll_Click);
            // 
            // cmdRemoveFile
            // 
            this.cmdRemoveFile.Location = new System.Drawing.Point(567, 102);
            this.cmdRemoveFile.Margin = new System.Windows.Forms.Padding(4);
            this.cmdRemoveFile.Name = "cmdRemoveFile";
            this.cmdRemoveFile.Size = new System.Drawing.Size(100, 28);
            this.cmdRemoveFile.TabIndex = 22;
            this.cmdRemoveFile.Text = "Remove";
            this.cmdRemoveFile.UseVisualStyleBackColor = true;
            this.cmdRemoveFile.Click += new System.EventHandler(this.cmdRemoveFile_Click);
            // 
            // txtProcessingLog
            // 
            this.txtProcessingLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtProcessingLog.Location = new System.Drawing.Point(16, 309);
            this.txtProcessingLog.Multiline = true;
            this.txtProcessingLog.Name = "txtProcessingLog";
            this.txtProcessingLog.ReadOnly = true;
            this.txtProcessingLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtProcessingLog.Size = new System.Drawing.Size(651, 164);
            this.txtProcessingLog.TabIndex = 23;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(689, 485);
            this.Controls.Add(this.txtProcessingLog);
            this.Controls.Add(this.cmdRemoveFile);
            this.Controls.Add(this.cmdClearAll);
            this.Controls.Add(this.lstInputFiles);
            this.Controls.Add(this.cmdProcessFiles);
            this.Controls.Add(this.cmdSelectFiles);
            this.Controls.Add(this.cmdSelectOutputDirectory);
            this.Controls.Add(this.lblOutputDirectory);
            this.Controls.Add(this.txtOutputDirectory);
            this.Controls.Add(this.label1);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "Form1";
            this.Text = "FAIMS MzXML Generator";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button cmdProcessFiles;
        private System.Windows.Forms.Button cmdSelectFiles;
        private System.Windows.Forms.Button cmdSelectOutputDirectory;
        private System.Windows.Forms.Label lblOutputDirectory;
        private System.Windows.Forms.TextBox txtOutputDirectory;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ListBox lstInputFiles;
        private System.Windows.Forms.Button cmdClearAll;
        private System.Windows.Forms.Button cmdRemoveFile;
        private System.Windows.Forms.TextBox txtProcessingLog;
    }
}

