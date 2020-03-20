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
            this.button3 = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.lblOutputDirectory = new System.Windows.Forms.Label();
            this.txtOutputDirectory = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.lstInputFiles = new System.Windows.Forms.ListBox();
            this.button4 = new System.Windows.Forms.Button();
            this.button5 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(16, 246);
            this.button3.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(651, 44);
            this.button3.TabIndex = 19;
            this.button3.Text = "Create MzXMLs";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(567, 33);
            this.button1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(100, 28);
            this.button1.TabIndex = 18;
            this.button1.Text = "Browse";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(567, 193);
            this.button2.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(100, 28);
            this.button2.TabIndex = 17;
            this.button2.Text = "Browse";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
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
            this.txtOutputDirectory.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
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
            this.lstInputFiles.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.lstInputFiles.Name = "lstInputFiles";
            this.lstInputFiles.Size = new System.Drawing.Size(536, 132);
            this.lstInputFiles.TabIndex = 20;
            // 
            // button4
            // 
            this.button4.Location = new System.Drawing.Point(567, 138);
            this.button4.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(100, 28);
            this.button4.TabIndex = 21;
            this.button4.Text = "Clear All";
            this.button4.UseVisualStyleBackColor = true;
            this.button4.Click += new System.EventHandler(this.button4_Click);
            // 
            // button5
            // 
            this.button5.Location = new System.Drawing.Point(567, 102);
            this.button5.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.button5.Name = "button5";
            this.button5.Size = new System.Drawing.Size(100, 28);
            this.button5.TabIndex = 22;
            this.button5.Text = "Remove";
            this.button5.UseVisualStyleBackColor = true;
            this.button5.Click += new System.EventHandler(this.button5_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(689, 311);
            this.Controls.Add(this.button5);
            this.Controls.Add(this.button4);
            this.Controls.Add(this.lstInputFiles);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.lblOutputDirectory);
            this.Controls.Add(this.txtOutputDirectory);
            this.Controls.Add(this.label1);
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Name = "Form1";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Label lblOutputDirectory;
        private System.Windows.Forms.TextBox txtOutputDirectory;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ListBox lstInputFiles;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.Button button5;
    }
}

