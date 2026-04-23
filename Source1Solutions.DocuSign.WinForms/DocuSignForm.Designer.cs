namespace Source1Solutions.DocuSign.WinForms
{
    partial class DocuSignForm
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
            label2 = new Label();
            dgvAttachments = new DataGridView();
            label3 = new Label();
            label4 = new Label();
            txtSignerEmail = new TextBox();
            txtSignerName = new TextBox();
            cmbSignerType = new ComboBox();
            btnMoreSigners = new Button();
            btnSendDocuments = new Button();
            btnExit = new Button();
            btnRemoveSigner = new Button();
            lblCarbonCopy1 = new Label();
            txtCarbonCopyEmail1 = new TextBox();
            txtCarbonCopyName1 = new TextBox();
            btnCarbonCopyRemove = new Button();
            btnCarbonCopyAdd = new Button();
            btnPreviousPage = new Button();
            btnNextPage = new Button();
            lblAttachmentPageInfo = new Label();
            ((System.ComponentModel.ISupportInitialize)dgvAttachments).BeginInit();
            SuspendLayout();
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(12, 180);
            label2.Name = "label2";
            label2.Size = new Size(138, 20);
            label2.TabIndex = 2;
            label2.Text = "List Of Attachments";
            // 
            // dgvAttachments
            // 
            dgvAttachments.AllowUserToAddRows = false;
            dgvAttachments.AllowUserToDeleteRows = false;
            dgvAttachments.AllowUserToResizeColumns = false;
            dgvAttachments.AllowUserToResizeRows = false;
            dgvAttachments.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvAttachments.Location = new Point(156, 180);
            dgvAttachments.Name = "dgvAttachments";
            dgvAttachments.RowHeadersWidth = 51;
            dgvAttachments.Size = new Size(1134, 258);
            dgvAttachments.TabIndex = 3;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Segoe UI", 15F, FontStyle.Bold);
            label3.Location = new Point(504, 9);
            label3.Name = "label3";
            label3.Size = new Size(221, 35);
            label3.TabIndex = 4;
            label3.Text = "DocuSign Process";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(12, 82);
            label4.Name = "label4";
            label4.Size = new Size(51, 20);
            label4.TabIndex = 5;
            label4.Text = "Signer";
            // 
            // txtSignerEmail
            // 
            txtSignerEmail.Location = new Point(156, 82);
            txtSignerEmail.Name = "txtSignerEmail";
            txtSignerEmail.Size = new Size(273, 27);
            txtSignerEmail.TabIndex = 6;
            // 
            // txtSignerName
            // 
            txtSignerName.Location = new Point(475, 82);
            txtSignerName.Name = "txtSignerName";
            txtSignerName.Size = new Size(224, 27);
            txtSignerName.TabIndex = 7;
            // 
            // cmbSignerType
            // 
            cmbSignerType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbSignerType.FormattingEnabled = true;
            cmbSignerType.Items.AddRange(new object[] { "Primary", "Secondary", "Tertiary" });
            cmbSignerType.Location = new Point(709, 82);
            cmbSignerType.Name = "cmbSignerType";
            cmbSignerType.Size = new Size(120, 28);
            cmbSignerType.TabIndex = 20;
            // 
            // btnMoreSigners
            // 
            btnMoreSigners.Location = new Point(837, 82);
            btnMoreSigners.Name = "btnMoreSigners";
            btnMoreSigners.Size = new Size(182, 29);
            btnMoreSigners.TabIndex = 8;
            btnMoreSigners.Text = "Add Signers";
            btnMoreSigners.UseVisualStyleBackColor = true;
            // 
            // btnSendDocuments
            // 
            btnSendDocuments.Location = new Point(396, 498);
            btnSendDocuments.Name = "btnSendDocuments";
            btnSendDocuments.Size = new Size(177, 29);
            btnSendDocuments.TabIndex = 9;
            btnSendDocuments.Text = "Send Documents";
            btnSendDocuments.UseVisualStyleBackColor = true;
            btnSendDocuments.Click += btnSendDocuments_Click;
            // 
            // btnExit
            // 
            btnExit.Location = new Point(608, 498);
            btnExit.Name = "btnExit";
            btnExit.Size = new Size(177, 29);
            btnExit.TabIndex = 10;
            btnExit.Text = "Exit Application";
            btnExit.UseVisualStyleBackColor = true;
            btnExit.Click += btnExit_Click;
            // 
            // btnRemoveSigner
            // 
            btnRemoveSigner.Location = new Point(1033, 82);
            btnRemoveSigner.Name = "btnRemoveSigner";
            btnRemoveSigner.Size = new Size(182, 29);
            btnRemoveSigner.TabIndex = 11;
            btnRemoveSigner.Text = "Remove Last Signer";
            btnRemoveSigner.UseVisualStyleBackColor = true;
            // 
            // lblCarbonCopy1
            // 
            lblCarbonCopy1.AutoSize = true;
            lblCarbonCopy1.Location = new Point(12, 133);
            lblCarbonCopy1.Name = "lblCarbonCopy1";
            lblCarbonCopy1.Size = new Size(91, 20);
            lblCarbonCopy1.TabIndex = 12;
            lblCarbonCopy1.Text = "CarbonCopy";
            // 
            // txtCarbonCopyEmail1
            // 
            txtCarbonCopyEmail1.Location = new Point(156, 130);
            txtCarbonCopyEmail1.Name = "txtCarbonCopyEmail1";
            txtCarbonCopyEmail1.Size = new Size(273, 27);
            txtCarbonCopyEmail1.TabIndex = 13;
            // 
            // txtCarbonCopyName1
            // 
            txtCarbonCopyName1.Location = new Point(475, 130);
            txtCarbonCopyName1.Name = "txtCarbonCopyName1";
            txtCarbonCopyName1.Size = new Size(324, 27);
            txtCarbonCopyName1.TabIndex = 14;
            // 
            // btnCarbonCopyRemove
            // 
            btnCarbonCopyRemove.Location = new Point(1033, 129);
            btnCarbonCopyRemove.Name = "btnCarbonCopyRemove";
            btnCarbonCopyRemove.Size = new Size(211, 29);
            btnCarbonCopyRemove.TabIndex = 16;
            btnCarbonCopyRemove.Text = "Remove Last CarbonCopy";
            btnCarbonCopyRemove.UseVisualStyleBackColor = true;
            // 
            // btnCarbonCopyAdd
            // 
            btnCarbonCopyAdd.Location = new Point(837, 129);
            btnCarbonCopyAdd.Name = "btnCarbonCopyAdd";
            btnCarbonCopyAdd.Size = new Size(182, 29);
            btnCarbonCopyAdd.TabIndex = 15;
            btnCarbonCopyAdd.Text = "Add CarbonCopy";
            btnCarbonCopyAdd.UseVisualStyleBackColor = true;
            // 
            // btnPreviousPage
            // 
            btnPreviousPage.Location = new Point(142, 444);
            btnPreviousPage.Name = "btnPreviousPage";
            btnPreviousPage.Size = new Size(120, 29);
            btnPreviousPage.TabIndex = 17;
            btnPreviousPage.Text = "< Previous";
            btnPreviousPage.UseVisualStyleBackColor = true;
            btnPreviousPage.Click += btnPreviousPage_Click;
            // 
            // btnNextPage
            // 
            btnNextPage.Location = new Point(475, 444);
            btnNextPage.Name = "btnNextPage";
            btnNextPage.Size = new Size(120, 29);
            btnNextPage.TabIndex = 18;
            btnNextPage.Text = "Next >";
            btnNextPage.UseVisualStyleBackColor = true;
            btnNextPage.Click += btnNextPage_Click;
            // 
            // lblAttachmentPageInfo
            // 
            lblAttachmentPageInfo.AutoSize = true;
            lblAttachmentPageInfo.Location = new Point(280, 449);
            lblAttachmentPageInfo.Name = "lblAttachmentPageInfo";
            lblAttachmentPageInfo.Size = new Size(158, 20);
            lblAttachmentPageInfo.TabIndex = 19;
            lblAttachmentPageInfo.Text = "Page 1 of 1 (0 records)";
            lblAttachmentPageInfo.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // DocuSignForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoScroll = true;
            ClientSize = new Size(1305, 600);
            Controls.Add(cmbSignerType);
            Controls.Add(lblAttachmentPageInfo);
            Controls.Add(btnNextPage);
            Controls.Add(btnPreviousPage);
            Controls.Add(btnCarbonCopyRemove);
            Controls.Add(btnCarbonCopyAdd);
            Controls.Add(txtCarbonCopyName1);
            Controls.Add(txtCarbonCopyEmail1);
            Controls.Add(lblCarbonCopy1);
            Controls.Add(btnRemoveSigner);
            Controls.Add(btnExit);
            Controls.Add(btnSendDocuments);
            Controls.Add(btnMoreSigners);
            Controls.Add(txtSignerName);
            Controls.Add(txtSignerEmail);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(dgvAttachments);
            Controls.Add(label2);
            Name = "DocuSignForm";
            Text = "DocuSign";
            Load += DocuSignForm_Load_1;
            ((System.ComponentModel.ISupportInitialize)dgvAttachments).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private Label label2;
        private DataGridView dgvAttachments;
        private Label label3;
        private Label label4;
        private TextBox txtSignerEmail;
        private TextBox txtSignerName;
        private ComboBox cmbSignerType;
        private Button btnMoreSigners;
        private Button btnSendDocuments;
        private Button btnExit;
        private Button btnRemoveSigner;
        private Label lblCarbonCopy1;
        private TextBox txtCarbonCopyEmail1;
        private TextBox txtCarbonCopyName1;
        private Button btnCarbonCopyRemove;
        private Button btnCarbonCopyAdd;
        private Button btnPreviousPage;
        private Button btnNextPage;
        private Label lblAttachmentPageInfo;
    }
}
