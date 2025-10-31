namespace Source1Solutions.DocuSign.WinSync
{
    partial class SyncForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            btnRefresh = new Button();
            btnClose = new Button();
            dgvDocuSignTracking = new DataGridView();
            lblTitle = new Label();
            lblRequestFrom = new Label();
            lblKey1 = new Label();
            lblKey2 = new Label();
            txtRequestFrom = new TextBox();
            txtKey1 = new TextBox();
            txtKey2 = new TextBox();
            btnPrevious = new Button();
            btnNext = new Button();
            lblPageInfo = new Label();
            ((System.ComponentModel.ISupportInitialize)dgvDocuSignTracking).BeginInit();
            SuspendLayout();
            // 
            // btnRefresh
            // 
            btnRefresh.Location = new Point(1057, 513);
            btnRefresh.Name = "btnRefresh";
            btnRefresh.Size = new Size(177, 34);
            btnRefresh.TabIndex = 0;
            btnRefresh.Text = "Refresh Sync";
            btnRefresh.UseVisualStyleBackColor = true;
            btnRefresh.Click += btnRefresh_Click;
            // 
            // btnClose
            // 
            btnClose.Location = new Point(1257, 513);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(177, 34);
            btnClose.TabIndex = 1;
            btnClose.Text = "Exit Application";
            btnClose.UseVisualStyleBackColor = true;
            btnClose.Click += btnClose_Click;
            // 
            // dgvDocuSignTracking
            // 
            dgvDocuSignTracking.AllowUserToAddRows = false;
            dgvDocuSignTracking.AllowUserToDeleteRows = false;
            dgvDocuSignTracking.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvDocuSignTracking.Location = new Point(27, 127);
            dgvDocuSignTracking.Name = "dgvDocuSignTracking";
            dgvDocuSignTracking.ReadOnly = true;
            dgvDocuSignTracking.RowHeadersWidth = 51;
            dgvDocuSignTracking.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvDocuSignTracking.Size = new Size(1407, 327);
            dgvDocuSignTracking.TabIndex = 2;
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblTitle.Location = new Point(580, 18);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(320, 37);
            lblTitle.TabIndex = 3;
            lblTitle.Text = "DocuSign Sync Tracking";
            // 
            // lblRequestFrom
            // 
            lblRequestFrom.AutoSize = true;
            lblRequestFrom.Location = new Point(27, 78);
            lblRequestFrom.Name = "lblRequestFrom";
            lblRequestFrom.Size = new Size(103, 20);
            lblRequestFrom.TabIndex = 4;
            lblRequestFrom.Text = "Request From:";
            // 
            // lblKey1
            // 
            lblKey1.AutoSize = true;
            lblKey1.Location = new Point(393, 78);
            lblKey1.Name = "lblKey1";
            lblKey1.Size = new Size(48, 20);
            lblKey1.TabIndex = 5;
            lblKey1.Text = "Key 1:";
            // 
            // lblKey2
            // 
            lblKey2.AutoSize = true;
            lblKey2.Location = new Point(712, 78);
            lblKey2.Name = "lblKey2";
            lblKey2.Size = new Size(48, 20);
            lblKey2.TabIndex = 6;
            lblKey2.Text = "Key 2:";
            // 
            // txtRequestFrom
            // 
            txtRequestFrom.Location = new Point(135, 75);
            txtRequestFrom.Name = "txtRequestFrom";
            txtRequestFrom.ReadOnly = true;
            txtRequestFrom.Size = new Size(230, 27);
            txtRequestFrom.TabIndex = 7;
            // 
            // txtKey1
            // 
            txtKey1.Location = new Point(448, 75);
            txtKey1.Name = "txtKey1";
            txtKey1.ReadOnly = true;
            txtKey1.Size = new Size(230, 27);
            txtKey1.TabIndex = 8;
            // 
            // txtKey2
            // 
            txtKey2.Location = new Point(767, 75);
            txtKey2.Name = "txtKey2";
            txtKey2.ReadOnly = true;
            txtKey2.Size = new Size(230, 27);
            txtKey2.TabIndex = 9;
            // 
            // btnPrevious
            // 
            btnPrevious.Location = new Point(27, 470);
            btnPrevious.Name = "btnPrevious";
            btnPrevious.Size = new Size(120, 34);
            btnPrevious.TabIndex = 10;
            btnPrevious.Text = "< Previous";
            btnPrevious.UseVisualStyleBackColor = true;
            btnPrevious.Click += btnPrevious_Click;
            // 
            // btnNext
            // 
            btnNext.Location = new Point(360, 470);
            btnNext.Name = "btnNext";
            btnNext.Size = new Size(120, 34);
            btnNext.TabIndex = 11;
            btnNext.Text = "Next >";
            btnNext.UseVisualStyleBackColor = true;
            btnNext.Click += btnNext_Click;
            // 
            // lblPageInfo
            // 
            lblPageInfo.AutoSize = true;
            lblPageInfo.Location = new Point(165, 477);
            lblPageInfo.Name = "lblPageInfo";
            lblPageInfo.Size = new Size(170, 20);
            lblPageInfo.TabIndex = 12;
            lblPageInfo.Text = "Page 1 of 1 (0 records)";
            lblPageInfo.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // SyncForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1474, 566);
            Controls.Add(lblPageInfo);
            Controls.Add(btnNext);
            Controls.Add(btnPrevious);
            Controls.Add(txtKey2);
            Controls.Add(txtKey1);
            Controls.Add(txtRequestFrom);
            Controls.Add(lblKey2);
            Controls.Add(lblKey1);
            Controls.Add(lblRequestFrom);
            Controls.Add(lblTitle);
            Controls.Add(dgvDocuSignTracking);
            Controls.Add(btnClose);
            Controls.Add(btnRefresh);
            Name = "SyncForm";
            Text = "DocuSign Sync";
            Load += Form1_Load;
            ((System.ComponentModel.ISupportInitialize)dgvDocuSignTracking).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button btnRefresh;
        private Button btnClose;
        private DataGridView dgvDocuSignTracking;
        private Label lblTitle;
        private Label lblRequestFrom;
        private Label lblKey1;
        private Label lblKey2;
        private TextBox txtRequestFrom;
        private TextBox txtKey1;
        private TextBox txtKey2;
        private Button btnPrevious;
        private Button btnNext;
        private Label lblPageInfo;
    }
}
