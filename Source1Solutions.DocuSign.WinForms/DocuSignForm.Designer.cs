

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
            btnMoreSigners = new Button();
            btnSendDocuments = new Button();
            btnExit = new Button();
            ((System.ComponentModel.ISupportInitialize)dgvAttachments).BeginInit();
            SuspendLayout();
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(81, 134);
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
            dgvAttachments.Location = new Point(225, 134);
            dgvAttachments.Name = "dgvAttachments";
            dgvAttachments.RowHeadersWidth = 51;
            dgvAttachments.Size = new Size(1059, 155);
            dgvAttachments.TabIndex = 3;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Segoe UI", 15F, FontStyle.Bold);
            label3.Location = new Point(647, 9);
            label3.Name = "label3";
            label3.Size = new Size(221, 35);
            label3.TabIndex = 4;
            label3.Text = "DocuSign Process";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(81, 80);
            label4.Name = "label4";
            label4.Size = new Size(51, 20);
            label4.TabIndex = 5;
            label4.Text = "Signer";
            // 
            // txtSignerEmail
            // 
            txtSignerEmail.Location = new Point(225, 80);
            txtSignerEmail.Name = "txtSignerEmail";
            txtSignerEmail.Size = new Size(273, 27);
            txtSignerEmail.TabIndex = 6;
            // 
            // txtSignerName
            // 
            txtSignerName.Location = new Point(544, 80);
            txtSignerName.Name = "txtSignerName";
            txtSignerName.Size = new Size(324, 27);
            txtSignerName.TabIndex = 7;
            // 
            // btnMoreSigners
            // 
            btnMoreSigners.Location = new Point(906, 80);
            btnMoreSigners.Name = "btnMoreSigners";
            btnMoreSigners.Size = new Size(182, 29);
            btnMoreSigners.TabIndex = 8;
            btnMoreSigners.Text = "Add Signers";
            btnMoreSigners.UseVisualStyleBackColor = true;
            // 
            // btnSendDocuments
            // 
            btnSendDocuments.Location = new Point(513, 349);
            btnSendDocuments.Name = "btnSendDocuments";
            btnSendDocuments.Size = new Size(177, 29);
            btnSendDocuments.TabIndex = 9;
            btnSendDocuments.Text = "Send Documents";
            btnSendDocuments.UseVisualStyleBackColor = true;
            btnSendDocuments.Click += btnSendDocuments_Click;
            // 
            // btnExit
            // 
            btnExit.Location = new Point(746, 349);
            btnExit.Name = "btnExit";
            btnExit.Size = new Size(177, 29);
            btnExit.TabIndex = 10;
            btnExit.Text = "Exit Application";
            btnExit.UseVisualStyleBackColor = true;
            btnExit.Click += btnExit_Click;
            // 
            // DocuSignForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1458, 450);
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
        private Button btnMoreSigners;
        private Button btnSendDocuments;
        private Button btnExit;
    }
}
