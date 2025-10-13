using Microsoft.Data.SqlClient;
using System.Data;
using System.IO;
using System.Runtime.InteropServices;
using DocuSign.Requests;

namespace Source1Solutions.DocuSign.WinForms
{
    public partial class DocuSignForm : Form
    {
        string connectionString = AppSettings.GetConnectionString();
        string _argsString;
        Dictionary<string, string> dicArgs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // List to keep track of dynamically created signer controls
        private List<TextBox> signerEmailTextBoxes = new List<TextBox>();
        private List<TextBox> signerNameTextBoxes = new List<TextBox>();
        private int signerCount = 1; // Starting with 1 (the initial signer)

        private Logger _logger;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern Int32 SendMessage(IntPtr hWnd, int msg, int wParam, [MarshalAs(UnmanagedType.LPWStr)] string lParam);

        private const int EM_SETCUEBANNER = 0x1501;

        public DocuSignForm(string[] args)
        {
            // Initialize logger
            _logger = new Logger(
                AppSettings.GetLogFilePath(),
                AppSettings.GetLogFileName(),
                AppSettings.GetLogLevel()
            );

            _logger.LogInformation("=== DocuSign WinForms Application Starting ===");

            _argsString = string.Join(",", args);
            _logger.LogInformation("Command line arguments: {0}", _argsString);

            InitializeComponent();

            foreach (var arg in args)
            {
                var kv = arg.Split('=', 2); // split into 2 parts only
                if (kv.Length == 2)
                {
                    string key = kv[0].Trim();
                    string value = kv[1].Trim();
                    dicArgs[key] = value;
                    _logger.LogDebug("Parsed argument: {0} = {1}", key, value);
                }
            }

            SetPlaceholder(txtSignerEmail, "Signer Email");
            SetPlaceholder(txtSignerName, "Signer Name");

            // Add the initial textboxes to our lists
            signerEmailTextBoxes.Add(txtSignerEmail);
            signerNameTextBoxes.Add(txtSignerName);

            // Wire up the button click events
            btnMoreSigners.Click += BtnMoreSigners_Click;
            btnRemoveSigner.Click += BtnRemoveSigner_Click;

            // Initially disable Remove Signer button (only one signer)
            btnRemoveSigner.Enabled = false;

            _logger.LogInformation("DocuSignForm initialized successfully");

            // Clean old logs
            try
            {
                _logger.CleanOldLogs(AppSettings.GetLogRetentionDays());
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to clean old logs", ex);
            }
        }

        private void BtnMoreSigners_Click(object sender, EventArgs e)
        {
            _logger.LogMethodEntry("BtnMoreSigners_Click");

            signerCount++;
            int yOffset = 80 + (signerCount - 1) * 35; // 35 pixels spacing between rows

            _logger.LogDebug("Adding signer #{0} at Y offset: {1}", signerCount, yOffset);

            // Create new email textbox
            TextBox newEmailTextBox = new TextBox();
            newEmailTextBox.Name = $"txtSignerEmail{signerCount}";
            newEmailTextBox.Location = new Point(225, yOffset);
            newEmailTextBox.Size = new Size(273, 27);
            newEmailTextBox.TabIndex = 6 + (signerCount - 1) * 2;
            SetPlaceholder(newEmailTextBox, $"Signer {signerCount} Email");

            // Create new name textbox
            TextBox newNameTextBox = new TextBox();
            newNameTextBox.Name = $"txtSignerName{signerCount}";
            newNameTextBox.Location = new Point(544, yOffset);
            newNameTextBox.Size = new Size(324, 27);
            newNameTextBox.TabIndex = 7 + (signerCount - 1) * 2;
            SetPlaceholder(newNameTextBox, $"Signer {signerCount} Name");

            // Add to form
            this.Controls.Add(newEmailTextBox);
            this.Controls.Add(newNameTextBox);

            // Add to our tracking lists
            signerEmailTextBoxes.Add(newEmailTextBox);
            signerNameTextBoxes.Add(newNameTextBox);

            // Move other controls down if needed
            MoveControlsDown(yOffset + 35);

            // Enable Remove Signer button since we have more than one signer
            btnRemoveSigner.Enabled = true;

            _logger.LogInformation("Successfully added signer #{0}. Total signers: {1}", signerCount, signerEmailTextBoxes.Count);
            _logger.LogMethodExit("BtnMoreSigners_Click");
        }

        private void BtnRemoveSigner_Click(object sender, EventArgs e)
        {
            _logger.LogMethodEntry("BtnRemoveSigner_Click");

            // Only remove if we have more than one signer
            if (signerEmailTextBoxes.Count <= 1)
            {
                _logger.LogWarning("Cannot remove signer - only one signer remains");
                MessageBox.Show("Cannot remove the last signer. At least one signer is required.",
                    "Cannot Remove", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // Get the last email and name textboxes
                TextBox lastEmailTextBox = signerEmailTextBoxes[signerEmailTextBoxes.Count - 1];
                TextBox lastNameTextBox = signerNameTextBoxes[signerNameTextBoxes.Count - 1];

                _logger.LogDebug("Removing signer #{0} ({1}, {2})",
                    signerCount, lastEmailTextBox.Name, lastNameTextBox.Name);

                // Remove from form
                this.Controls.Remove(lastEmailTextBox);
                this.Controls.Remove(lastNameTextBox);

                // Dispose the controls
                lastEmailTextBox.Dispose();
                lastNameTextBox.Dispose();

                // Remove from tracking lists
                signerEmailTextBoxes.RemoveAt(signerEmailTextBoxes.Count - 1);
                signerNameTextBoxes.RemoveAt(signerNameTextBoxes.Count - 1);

                // Decrement signer count
                signerCount--;

                // Move controls up if needed
                int newTopPosition = 80 + (signerCount - 1) * 35 + 35;
                MoveControlsUp(newTopPosition);

                // Disable Remove Signer button if only one signer left
                if (signerEmailTextBoxes.Count == 1)
                {
                    btnRemoveSigner.Enabled = false;
                    _logger.LogDebug("Disabled Remove Signer button - only one signer remains");
                }

                _logger.LogInformation("Successfully removed signer. Remaining signers: {0}", signerEmailTextBoxes.Count);
                _logger.LogMethodExit("BtnRemoveSigner_Click");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error removing signer", ex);
                MessageBox.Show($"Error removing signer: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void MoveControlsUp(int newTopPosition)
        {
            _logger.LogDebug("Moving controls up to position: {0}", newTopPosition);

            // Move the attachments section up
            if (dgvAttachments.Top > newTopPosition + 20)
            {
                int moveDistance = dgvAttachments.Top - (newTopPosition + 20);
                dgvAttachments.Top -= moveDistance;
                label2.Top -= moveDistance;

                // Shrink form height if needed
                int minHeight = dgvAttachments.Bottom + 150; // Minimum height for buttons
                if (this.Height > minHeight)
                {
                    this.Height = Math.Max(minHeight, 450); // Don't go below original height
                }

                _logger.LogDebug("Moved controls up by {0} pixels. New form height: {1}", moveDistance, this.Height);
            }
        }

        private void MoveControlsDown(int newTopPosition)
        {
            // Move the attachments section down
            if (dgvAttachments.Top < newTopPosition + 20)
            {
                int moveDistance = (newTopPosition + 20) - dgvAttachments.Top;
                dgvAttachments.Top += moveDistance;
                label2.Top += moveDistance;

                // Expand form height if needed
                if (dgvAttachments.Bottom > this.ClientSize.Height - 50)
                {
                    this.Height = dgvAttachments.Bottom + 100;
                }
            }
        }

        private void SetPlaceholder(TextBox textBox, string placeholderText)
        {
            SendMessage(textBox.Handle, EM_SETCUEBANNER, 0, placeholderText);
        }

        protected void DocuSignForm_Load_1(object sender, EventArgs e)
        {
            _logger.LogMethodEntry("DocuSignForm_Load_1");
            LoadAttachment();
            _logger.LogMethodExit("DocuSignForm_Load_1");
        }

        public void LoadAttachment()
        {
            _logger.LogMethodEntry("LoadAttachment");

            try
            {
                string insertQuery = "INSERT INTO DocuSign_Testing_S1S (DocuSignName, RequestedDtm) VALUES (@DocuSignName, @RequestedDtm)";

                using (SqlConnection connection = new SqlConnection(connectionString))
                using (SqlCommand command = new SqlCommand(insertQuery, connection))
                {
                    command.Parameters.AddWithValue("@DocuSignName", _argsString);
                    command.Parameters.AddWithValue("@RequestedDtm", DateTime.Now);

                    connection.Open();
                    int rowsAffected = command.ExecuteNonQuery();
                    _logger.LogInformation("Inserted {0} row(s) into DocuSign_Testing_S1S", rowsAffected);
                }

                string contractID = dicArgs.ContainsKey("contractID") ? dicArgs["contractID"] : string.Empty;
                _logger.LogDebug("Loading attachments for Contract ID: {0}", contractID);

                string selectQuery = "select top 1000 HQAT.AttachmentID, HQAT.FormName, HQAT.Description, HQAT.AddedBy, HQAT.AddDate, HQAT.UniqueAttchID, OrigFileName " +
                                              " from dbo.HQAT HQAT with(nolock) inner join dbo.JCCM on HQAT.UniqueAttchID = JCCM.UniqueAttchID " +
                                                " where LTRIM(RTRIM(Contract)) = LTRIM(RTRIM(@Contract))";

                using (SqlConnection connection = new SqlConnection(connectionString))
                using (SqlCommand command = new SqlCommand(selectQuery, connection))
                {
                    command.Parameters.AddWithValue("@Contract", contractID);

                    connection.Open();
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        // Clear existing data
                        dgvAttachments.Rows.Clear();
                        dgvAttachments.Columns.Clear();

                        // Add checkbox column as first column
                        DataGridViewCheckBoxColumn checkBoxColumn = new DataGridViewCheckBoxColumn();
                        checkBoxColumn.Name = "Select";
                        checkBoxColumn.HeaderText = "Select";
                        checkBoxColumn.Width = 60;
                        dgvAttachments.Columns.Add(checkBoxColumn);

                        // Add other columns
                        dgvAttachments.Columns.Add("OrigFileName", "Original File Name");
                        dgvAttachments.Columns["OrigFileName"].Width = 200;
                        dgvAttachments.Columns.Add("AttachmentID", "Attachment ID");
                        dgvAttachments.Columns.Add("FormName", "Form Name");
                        dgvAttachments.Columns.Add("Description", "Description");
                        dgvAttachments.Columns.Add("AddedBy", "Added By");

                        // Add date column with formatting
                        DataGridViewTextBoxColumn dateColumn = new DataGridViewTextBoxColumn();
                        dateColumn.Name = "AddDate";
                        dateColumn.HeaderText = "Add Date";
                        dateColumn.DefaultCellStyle.Format = "MM/dd/yyyy";
                        dgvAttachments.Columns.Add(dateColumn);

                        int rowCount = 0;
                        while (reader.Read())
                        {
                            // Add row with data from reader, formatting date
                            DateTime addDate = reader["AddDate"] != DBNull.Value ? Convert.ToDateTime(reader["AddDate"]) : DateTime.MinValue;

                            dgvAttachments.Rows.Add(
                                false, // Checkbox column - default unchecked
                                reader["OrigFileName"],
                                reader["AttachmentID"],
                                reader["FormName"],
                                reader["Description"],
                                reader["AddedBy"],
                                addDate == DateTime.MinValue ? "" : addDate.ToString("MM/dd/yyyy")
                            );
                            rowCount++;
                        }

                        _logger.LogInformation("Loaded {0} attachment(s) into grid", rowCount);
                    }
                }

                _logger.LogMethodExit("LoadAttachment");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error loading attachments", ex);
                MessageBox.Show($"Error loading attachments: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnSendDocuments_Click(object sender, EventArgs e)
        {
            _logger.LogMethodEntry("btnSendDocuments_Click");

            // Validate all signer fields
            List<string> validationErrors = new List<string>();

            _logger.LogDebug("Validating {0} signer(s)", signerEmailTextBoxes.Count);

            for (int i = 0; i < signerEmailTextBoxes.Count; i++)
            {
                TextBox emailTextBox = signerEmailTextBoxes[i];
                TextBox nameTextBox = signerNameTextBoxes[i];
                int signerNumber = i + 1;

                // Check if email is empty or invalid
                string email = emailTextBox.Text.Trim();
                if (string.IsNullOrEmpty(email))
                {
                    validationErrors.Add($"Signer {signerNumber} email is empty");
                    _logger.LogWarning("Validation failed: Signer {0} email is empty", signerNumber);
                }
                else if (!IsValidEmail(email))
                {
                    validationErrors.Add($"Signer {signerNumber} email is not valid");
                    _logger.LogWarning("Validation failed: Signer {0} email '{1}' is not valid", signerNumber, email);
                }

                // Check if name is empty
                string name = nameTextBox.Text.Trim();
                if (string.IsNullOrEmpty(name))
                {
                    validationErrors.Add($"Signer {signerNumber} name is empty");
                    _logger.LogWarning("Validation failed: Signer {0} name is empty", signerNumber);
                }
            }

            // Check if any attachments are selected
            bool hasSelectedAttachments = false;
            int selectedCount = 0;
            foreach (DataGridViewRow row in dgvAttachments.Rows)
            {
                if (row.Cells["Select"].Value != null && Convert.ToBoolean(row.Cells["Select"].Value))
                {
                    hasSelectedAttachments = true;
                    selectedCount++;
                }
            }

            _logger.LogDebug("Selected {0} attachment(s)", selectedCount);

            if (!hasSelectedAttachments)
            {
                validationErrors.Add("Please select at least one attachment to send");
                _logger.LogWarning("Validation failed: No attachments selected");
            }

            // If there are validation errors, show message box and return
            if (validationErrors.Count > 0)
            {
                string errorMessage = string.Join("\n", validationErrors);
                _logger.LogError("Validation failed with {0} error(s): {1}", validationErrors.Count, errorMessage);
                MessageBox.Show(errorMessage, "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _logger.LogMethodExit("btnSendDocuments_Click", "Validation Failed");
                return;
            }

            _logger.LogInformation("Validation passed successfully");

            // Create DTO object with validated data
            DocuSignRequestDto docuSignRequest = CreateDocuSignRequestDto();

            _logger.LogInformation("Created DocuSignRequestDto with {0} signer(s) and {1} attachment(s)",
                docuSignRequest.Signers.Count, docuSignRequest.SelectedAttachments.Count);

            try
            {
                var userInputs = new UserInputs()
                {
                    ConnectionString = AppSettings.GetConnectionString(),
                    DocuSignClientId = AppSettings.GetDocuSignClientId(),
                    DocuSignAuthServer = AppSettings.GetDocuSignAuthServer(),
                    DocuSignImpersonatedUserID = AppSettings.GetDocuSignImpersonatedUserID(),
                    DocuSignPrivateKeyFile = AppSettings.GetDocuSignPrivateKeyFile(),
                    DocuSignAccountID = AppSettings.GetDocuSignAccountID(),
                    AttachmentDBConnection = AppSettings.GetAttachmentDBConnectionString(),
                    DocuSignApiBaseUrl = AppSettings.GetDocuSignApiBaseUrl()
                };

                _logger.LogInformation("Creating DocuSignRequestor with logger");
                DocuSignRequestor docuSignRequestor = new DocuSignRequestor(userInputs, _logger);

                _logger.LogInformation("Sending envelope to DocuSign");
                var envelopeID = docuSignRequestor.SendEnvelope(docuSignRequest);

                _logger.LogInformation("Envelope sent successfully with ID: {0}", envelopeID);

                // Save to database using stored procedure
                int docuSignId = SaveDocuSignEntries(docuSignRequest, envelopeID);

                // Show success message with summary
                string summary = $"DocuSign Request Saved Successfully!\n\n" +
                               $"Database ID: {docuSignId}\n" +
                               $"Request ID: {docuSignRequest.RequestId}\n" +
                               $"Signers: {docuSignRequest.Signers.Count}\n" +
                               $"Selected Attachments: {docuSignRequest.SelectedAttachments.Count}\n" +
                               $"Request Time: {docuSignRequest.RequestDateTime:MM/dd/yyyy HH:mm:ss}";

                MessageBox.Show(summary, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                _logger.LogInformation("DocuSign request completed successfully");
                _logger.LogMethodExit("btnSendDocuments_Click", "Success");

                // TODO: Send docuSignRequest to DocuSign API
                // ProcessDocuSignRequest(docuSignRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error saving DocuSign request", ex);
                MessageBox.Show($"Error saving DocuSign request: {ex.Message}", "Database Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                _logger.LogMethodExit("btnSendDocuments_Click", "Error");
            }
        }

        private int SaveDocuSignEntries(DocuSignRequestDto docuSignRequest, string envelopeID)
        {
            _logger.LogMethodEntry("SaveDocuSignEntries");

            // Prepare comma-delimited strings for signers
            string signerEmails = string.Join(",", docuSignRequest.Signers.Select(s => s.Email));
            string signerNames = string.Join(",", docuSignRequest.Signers.Select(s => s.Name));

            // Prepare comma-delimited strings for attachments
            string attachmentIds = string.Join(",", docuSignRequest.SelectedAttachments.Select(a => a.AttachmentID));
            string attachmentNames = string.Join(",", docuSignRequest.SelectedAttachments.Select(a => a.OrigFileName));

            // Get requestor from args or use a default
            string requestor = dicArgs.ContainsKey("requestor") ? dicArgs["requestor"] : Environment.UserName;

            _logger.LogDebug("Requestor: {0}", requestor);
            _logger.LogDebug("Signer Emails: {0}", signerEmails);
            _logger.LogDebug("Signer Names: {0}", signerNames);
            _logger.LogDebug("Attachment IDs: {0}", attachmentIds);
            _logger.LogDebug("Attachment Names: {0}", attachmentNames);

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                using (SqlCommand command = new SqlCommand("brptCreateDocuSignEntries_S1S", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    // Add parameters
                    command.Parameters.AddWithValue("@Requestor", requestor);
                    command.Parameters.AddWithValue("@EnvelopeID", envelopeID); // Will be updated after DocuSign API call
                    command.Parameters.AddWithValue("@Status", "Pending");
                    command.Parameters.AddWithValue("@RequestFrom", docuSignRequest.RequestFrom);
                    command.Parameters.AddWithValue("@Key_1", docuSignRequest.Key_1);
                    command.Parameters.AddWithValue("@Key_2", docuSignRequest.Key_2);
                    command.Parameters.AddWithValue("@Error_Msg", DBNull.Value);
                    command.Parameters.AddWithValue("@SignerEmails", signerEmails);
                    command.Parameters.AddWithValue("@SignerNames", signerNames);
                    command.Parameters.AddWithValue("@AttachmentIDs", attachmentIds);
                    command.Parameters.AddWithValue("@AttachmentNames", attachmentNames);

                    connection.Open();

                    _logger.LogDebug("Executing stored procedure: brptCreateDocuSignEntries_S1S");

                    // Execute and get the returned DocuSignID
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            int docuSignId = Convert.ToInt32(reader["DocuSignID"]);
                            _logger.LogInformation("Stored procedure returned DocuSignID: {0}", docuSignId);
                            _logger.LogMethodExit("SaveDocuSignEntries", docuSignId);
                            return docuSignId;
                        }
                        else
                        {
                            _logger.LogError("Failed to retrieve DocuSignID from stored procedure");
                            throw new Exception("Failed to retrieve DocuSignID from stored procedure");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error executing SaveDocuSignEntries", ex);
                throw;
            }
        }

        private DocuSignRequestDto CreateDocuSignRequestDto()
        {
            var docuSignRequest = new DocuSignRequestDto();

            docuSignRequest.RequestId = dicArgs.ContainsKey("requestor") ? dicArgs["requestor"] : Environment.UserName;
            docuSignRequest.RequestFrom = dicArgs.ContainsKey("component") ? dicArgs["component"] : Environment.UserName;
            docuSignRequest.Key_1 = dicArgs.ContainsKey("companyID") ? dicArgs["companyID"] : Environment.UserName;
            docuSignRequest.Key_2 = dicArgs.ContainsKey("contractID") ? dicArgs["contractID"] : Environment.UserName;

            // Add signers
            for (int i = 0; i < signerEmailTextBoxes.Count; i++)
            {
                var signer = new SignerDto
                {
                    Name = signerNameTextBoxes[i].Text.Trim(),
                    Email = signerEmailTextBoxes[i].Text.Trim(),
                    SignerOrder = i + 1
                };
                docuSignRequest.Signers.Add(signer);
            }

            // Add selected attachments
            foreach (DataGridViewRow row in dgvAttachments.Rows)
            {
                if (row.Cells["Select"].Value != null && Convert.ToBoolean(row.Cells["Select"].Value))
                {
                    var attachment = new AttachmentDto
                    {
                        AttachmentID = row.Cells["AttachmentID"].Value?.ToString() ?? "",
                        OrigFileName = row.Cells["OrigFileName"].Value?.ToString() ?? "",
                        FormName = row.Cells["FormName"].Value?.ToString() ?? "",
                        Description = row.Cells["Description"].Value?.ToString() ?? "",
                        AddedBy = row.Cells["AddedBy"].Value?.ToString() ?? "",
                        AddDate = DateTime.TryParse(row.Cells["AddDate"].Value?.ToString(), out DateTime addDate) ? addDate : DateTime.MinValue
                    };
                    docuSignRequest.SelectedAttachments.Add(attachment);
                }
            }

            return docuSignRequest;
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
