namespace XV2SSEdit
{
    partial class Export
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
            this.exportSoulsList = new System.Windows.Forms.CheckedListBox();
            this.checkBurst1 = new System.Windows.Forms.CheckBox();
            this.checkBurst2 = new System.Windows.Forms.CheckBox();
            this.checkBurst3 = new System.Windows.Forms.CheckBox();
            this.btnExport = new System.Windows.Forms.Button();
            this.btnHelp = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // exportSoulsList
            // 
            this.exportSoulsList.CheckOnClick = true;
            this.exportSoulsList.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.exportSoulsList.FormattingEnabled = true;
            this.exportSoulsList.HorizontalScrollbar = true;
            this.exportSoulsList.IntegralHeight = false;
            this.exportSoulsList.Location = new System.Drawing.Point(12, 6);
            this.exportSoulsList.Name = "exportSoulsList";
            this.exportSoulsList.ScrollAlwaysVisible = true;
            this.exportSoulsList.Size = new System.Drawing.Size(391, 417);
            this.exportSoulsList.TabIndex = 0;
            this.exportSoulsList.ThreeDCheckBoxes = true;
            this.exportSoulsList.SelectedIndexChanged += new System.EventHandler(this.exportSoulsList_SelectedIndexChanged);
            // 
            // checkBurst1
            // 
            this.checkBurst1.AutoSize = true;
            this.checkBurst1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.checkBurst1.Location = new System.Drawing.Point(409, 7);
            this.checkBurst1.Name = "checkBurst1";
            this.checkBurst1.Size = new System.Drawing.Size(129, 24);
            this.checkBurst1.TabIndex = 1;
            this.checkBurst1.Text = "Export Burst 1";
            this.checkBurst1.UseVisualStyleBackColor = true;
            this.checkBurst1.CheckedChanged += new System.EventHandler(this.checkBurst1_CheckedChanged);
            // 
            // checkBurst2
            // 
            this.checkBurst2.AutoSize = true;
            this.checkBurst2.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.checkBurst2.Location = new System.Drawing.Point(409, 37);
            this.checkBurst2.Name = "checkBurst2";
            this.checkBurst2.Size = new System.Drawing.Size(129, 24);
            this.checkBurst2.TabIndex = 2;
            this.checkBurst2.Text = "Export Burst 2";
            this.checkBurst2.UseVisualStyleBackColor = true;
            this.checkBurst2.CheckedChanged += new System.EventHandler(this.checkBurst2_CheckedChanged);
            // 
            // checkBurst3
            // 
            this.checkBurst3.AutoSize = true;
            this.checkBurst3.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.checkBurst3.Location = new System.Drawing.Point(409, 67);
            this.checkBurst3.Name = "checkBurst3";
            this.checkBurst3.Size = new System.Drawing.Size(129, 24);
            this.checkBurst3.TabIndex = 3;
            this.checkBurst3.Text = "Export Burst 3";
            this.checkBurst3.UseVisualStyleBackColor = true;
            this.checkBurst3.CheckedChanged += new System.EventHandler(this.checkBurst3_CheckedChanged);
            // 
            // btnExport
            // 
            this.btnExport.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnExport.Location = new System.Drawing.Point(409, 394);
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new System.Drawing.Size(186, 35);
            this.btnExport.TabIndex = 4;
            this.btnExport.Text = "Export";
            this.btnExport.UseVisualStyleBackColor = true;
            this.btnExport.Click += new System.EventHandler(this.btnExport_Click);
            // 
            // btnHelp
            // 
            this.btnHelp.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnHelp.Location = new System.Drawing.Point(409, 353);
            this.btnHelp.Name = "btnHelp";
            this.btnHelp.Size = new System.Drawing.Size(186, 35);
            this.btnHelp.TabIndex = 5;
            this.btnHelp.Text = "Help";
            this.btnHelp.UseVisualStyleBackColor = true;
            this.btnHelp.Click += new System.EventHandler(this.btnHelp_Click);
            // 
            // Export
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(607, 435);
            this.Controls.Add(this.btnHelp);
            this.Controls.Add(this.btnExport);
            this.Controls.Add(this.checkBurst3);
            this.Controls.Add(this.checkBurst2);
            this.Controls.Add(this.checkBurst1);
            this.Controls.Add(this.exportSoulsList);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.MaximizeBox = false;
            this.Name = "Export";
            this.Text = "Export Super Souls";
            this.Load += new System.EventHandler(this.Export_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckedListBox exportSoulsList;
        private System.Windows.Forms.CheckBox checkBurst1;
        private System.Windows.Forms.CheckBox checkBurst2;
        private System.Windows.Forms.CheckBox checkBurst3;
        private System.Windows.Forms.Button btnExport;
        private System.Windows.Forms.Button btnHelp;
    }
}