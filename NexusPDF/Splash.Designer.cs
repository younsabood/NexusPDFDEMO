namespace NexusPDF
{
    partial class splash
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(splash));
            this.splashTimer = new System.Windows.Forms.Timer(this.components);
            this.tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.imagePanel = new System.Windows.Forms.Panel();
            this.progressPanel = new System.Windows.Forms.Panel();
            this.progressBar = new Guna.UI2.WinForms.Guna2ProgressBar();
            this.tableLayoutPanel.SuspendLayout();
            this.progressPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // splashTimer
            // 
            this.splashTimer.Interval = 30;
            this.splashTimer.Tick += new System.EventHandler(this.SplashTimer_Tick);
            // 
            // tableLayoutPanel
            // 
            this.tableLayoutPanel.BackColor = System.Drawing.Color.White;
            this.tableLayoutPanel.ColumnCount = 1;
            this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel.Controls.Add(this.imagePanel, 0, 0);
            this.tableLayoutPanel.Controls.Add(this.progressPanel, 0, 1);
            this.tableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel.Location = new System.Drawing.Point(10, 20);
            this.tableLayoutPanel.Name = "tableLayoutPanel";
            this.tableLayoutPanel.RowCount = 2;
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 60F));
            this.tableLayoutPanel.Size = new System.Drawing.Size(880, 420);
            this.tableLayoutPanel.TabIndex = 0;
            // 
            // imagePanel
            // 
            this.imagePanel.BackColor = System.Drawing.Color.White;
            this.imagePanel.BackgroundImage = global::NexusPDF.Properties.Resources.svgviewer_png_output;
            this.imagePanel.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.imagePanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.imagePanel.Location = new System.Drawing.Point(0, 0);
            this.imagePanel.Margin = new System.Windows.Forms.Padding(0);
            this.imagePanel.Name = "imagePanel";
            this.imagePanel.Size = new System.Drawing.Size(880, 360);
            this.imagePanel.TabIndex = 0;
            // 
            // progressPanel
            // 
            this.progressPanel.BackColor = System.Drawing.Color.White;
            this.progressPanel.Controls.Add(this.progressBar);
            this.progressPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.progressPanel.Location = new System.Drawing.Point(0, 360);
            this.progressPanel.Margin = new System.Windows.Forms.Padding(0);
            this.progressPanel.Name = "progressPanel";
            this.progressPanel.Padding = new System.Windows.Forms.Padding(10, 20, 10, 10);
            this.progressPanel.Size = new System.Drawing.Size(880, 60);
            this.progressPanel.TabIndex = 1;
            // 
            // progressBar
            // 
            this.progressBar.AutoRoundedCorners = true;
            this.progressBar.BackColor = System.Drawing.Color.Transparent;
            this.progressBar.BorderColor = System.Drawing.Color.White;
            this.progressBar.BorderRadius = 14;
            this.progressBar.Dock = System.Windows.Forms.DockStyle.Fill;
            this.progressBar.FillColor = System.Drawing.Color.White;
            this.progressBar.ForeColor = System.Drawing.Color.Black;
            this.progressBar.Location = new System.Drawing.Point(10, 20);
            this.progressBar.Name = "progressBar";
            this.progressBar.ProgressColor = System.Drawing.Color.FromArgb(((int)(((byte)(190)))), ((int)(((byte)(158)))), ((int)(((byte)(68)))));
            this.progressBar.ProgressColor2 = System.Drawing.Color.FromArgb(((int)(((byte)(216)))), ((int)(((byte)(198)))), ((int)(((byte)(144)))));
            this.progressBar.ShowText = true;
            this.progressBar.Size = new System.Drawing.Size(860, 30);
            this.progressBar.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.progressBar.TabIndex = 0;
            this.progressBar.Text = "0";
            this.progressBar.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SystemDefault;
            this.progressBar.UseTransparentBackground = true;
            // 
            // splash
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 23F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(900, 450);
            this.Controls.Add(this.tableLayoutPanel);
            this.DoubleBuffered = true;
            this.Font = new System.Drawing.Font("Times New Roman", 15.75F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
            this.Name = "splash";
            this.Padding = new System.Windows.Forms.Padding(10, 20, 10, 10);
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "splash";
            this.tableLayoutPanel.ResumeLayout(false);
            this.progressPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Timer splashTimer;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel;
        private System.Windows.Forms.Panel imagePanel;
        private System.Windows.Forms.Panel progressPanel;
        private Guna.UI2.WinForms.Guna2ProgressBar progressBar;
    }
}
