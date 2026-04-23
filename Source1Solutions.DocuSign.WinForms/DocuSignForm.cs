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
        private List<ComboBox> signerTypeComboBoxes = new List<ComboBox>();
        private int signerCount = 1; // Starting with 1 (the initial signer)

        // List to keep track of dynamically created carbon copy controls
        private List<TextBox> carbonCopyEmailTextBoxes = new List<TextBox>();
        private List<TextBox> carbonCopyNameTextBoxes = new List<TextBox>();
        private int carbonCopyCount = 1; // Starting with 1 (the initial carbon copy)

        // Existing signers from project
        private ComboBox cmbExistingSigners;
        private Button btnAddExistingSigner;
        private Label lblExistingSigners;
        private DataTable _existingSignersDataTable;

        // Constants for layout - ALIGNED WITH DESIGNER.CS POSITIONS
        private const int SIGNER_START_Y = 82;          // Matches Designer Y position for first signer
        private const int SIGNER_EMAIL_X = 156;         // Matches Designer X position for email textboxes
        private const int SIGNER_NAME_X = 475;          // Matches Designer X position for name textboxes
        private const int SIGNER_TYPE_X = 709;          // Position for signer type combobox
        private const int EXISTING_SIGNERS_Y = 50;      // Position for existing signers section
        private const int CONTROL_SPACING = 35;         // Vertical spacing between rows (35px apart)
        private const int SECTION_SPACING = 15;         // Spacing between sections
        private const int MAX_CARBON_COPIES = 4;
        private const int MAX_SIGNERS = 5;

        // Pagination variables
        private DataTable _fullAttachmentDataTable;
        private int _currentAttachmentPage = 1;
        private const int _attachmentPageSize = 20;
        private int _totalAttachmentPages = 0;
        private int _totalAttachmentRecords = 0;

        private Logger _logger;
        private bool _isAttachmentDataLoaded = false;

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

            // Log parsed parameters for debugging
            _logger.LogInformation("Parsed parameters - component: {0}, Key_1_ID: {1}, Key_2_ID: {2}, projectID: {3}",
                dicArgs.ContainsKey("component") ? dicArgs["component"] : "(not set)",
                dicArgs.ContainsKey("Key_1_ID") ? dicArgs["Key_1_ID"] : "(not set)",
                dicArgs.ContainsKey("Key_2_ID") ? dicArgs["Key_2_ID"] : "(not set)",
                dicArgs.ContainsKey("projectID") ? dicArgs["projectID"] : "(not set)");

            // Initialize existing signers dropdown if ProjectID is provided
            InitializeExistingSignersSection();

            SetPlaceholder(txtSignerEmail, "Signer Email");
            SetPlaceholder(txtSignerName, "Signer Name");

            // Add the initial textboxes to our lists
            signerEmailTextBoxes.Add(txtSignerEmail);
            signerNameTextBoxes.Add(txtSignerName);
            signerTypeComboBoxes.Add(cmbSignerType);
            
            // Set default value for first signer type
            cmbSignerType.SelectedIndex = 0; // Default to "Primary"

            // Wire up the button click events for signers
            btnMoreSigners.Click += BtnMoreSigners_Click;
            btnRemoveSigner.Click += BtnRemoveSigner_Click;

            // Initially disable Remove Signer button (only one signer)
            btnRemoveSigner.Enabled = false;

            // Setup Carbon Copy controls
            SetPlaceholder(txtCarbonCopyEmail1, "Carbon Copy Email");
            SetPlaceholder(txtCarbonCopyName1, "Carbon Copy Name");

            // Add the initial carbon copy textboxes to our lists
            carbonCopyEmailTextBoxes.Add(txtCarbonCopyEmail1);
            carbonCopyNameTextBoxes.Add(txtCarbonCopyName1);

            // Wire up the button click events for carbon copies
            btnCarbonCopyAdd.Click += BtnCarbonCopyAdd_Click;
            btnCarbonCopyRemove.Click += BtnCarbonCopyRemove_Click;

            // Initially disable Remove Carbon Copy button (only one carbon copy)
            btnCarbonCopyRemove.Enabled = false;

            // NOTE: btnSendDocuments and btnExit event handlers are already 
            // registered in the Designer.cs file, so we don't register them here
            // to avoid double-firing the events

            _logger.LogInformation("DocuSignForm initialized successfully");

            // Clean old logs asynchronously to avoid blocking startup
            Task.Run(() =>
            {
                try
                {
                    _logger.CleanOldLogs(AppSettings.GetLogRetentionDays());
                }
                catch (Exception ex)
                {
                    _logger.LogError("Failed to clean old logs", ex);
                }
            });

            // Initialize button positions based on initial layout
            UpdateAttachmentSectionPosition();
        }

        private void BtnMoreSigners_Click(object sender, EventArgs e)
        {
            _logger.LogMethodEntry("BtnMoreSigners_Click");

            // Check if we've reached the maximum number of signers
            if (signerCount >= MAX_SIGNERS)
            {
                _logger.LogWarning("Cannot add more signers - maximum limit of {0} reached", MAX_SIGNERS);
                MessageBox.Show($"Maximum of {MAX_SIGNERS} signers allowed.",
                    "Limit Reached", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            signerCount++;
            int yOffset = SIGNER_START_Y + (signerCount - 1) * CONTROL_SPACING;

            _logger.LogDebug("Adding signer #{0} at Y offset: {1}", signerCount, yOffset);

            // Create new email textbox - ALIGNED WITH DESIGNER POSITIONS
            TextBox newEmailTextBox = new TextBox();
            newEmailTextBox.Name = $"txtSignerEmail{signerCount}";
            newEmailTextBox.Location = new Point(SIGNER_EMAIL_X, yOffset);
            newEmailTextBox.Size = new Size(273, 27);
            newEmailTextBox.TabIndex = 6 + (signerCount - 1) * 3;
            SetPlaceholder(newEmailTextBox, $"Signer {signerCount} Email");

            // Create new name textbox - ALIGNED WITH DESIGNER POSITIONS
            TextBox newNameTextBox = new TextBox();
            newNameTextBox.Name = $"txtSignerName{signerCount}";
            newNameTextBox.Location = new Point(SIGNER_NAME_X, yOffset);
            newNameTextBox.Size = new Size(224, 27);
            newNameTextBox.TabIndex = 7 + (signerCount - 1) * 3;
            SetPlaceholder(newNameTextBox, $"Signer {signerCount} Name");

            // Create new signer type combobox
            ComboBox newTypeComboBox = new ComboBox();
            newTypeComboBox.Name = $"cmbSignerType{signerCount}";
            newTypeComboBox.Location = new Point(SIGNER_TYPE_X, yOffset);
            newTypeComboBox.Size = new Size(120, 28);
            newTypeComboBox.TabIndex = 8 + (signerCount - 1) * 3;
            newTypeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            newTypeComboBox.Items.AddRange(new object[] { "Primary", "Secondary", "Tertiary" });
            newTypeComboBox.SelectedIndex = 0; // Default to "Primary"

            // Add to form
            this.Controls.Add(newEmailTextBox);
            this.Controls.Add(newNameTextBox);
            this.Controls.Add(newTypeComboBox);

            // Add to our tracking lists
            signerEmailTextBoxes.Add(newEmailTextBox);
            signerNameTextBoxes.Add(newNameTextBox);
            signerTypeComboBoxes.Add(newTypeComboBox);

            // Move carbon copy section down
            UpdateCarbonCopySectionPosition();

            // Enable Remove Signer button since we have more than one signer
            btnRemoveSigner.Enabled = true;

            // Disable Add Signer button if we've reached the maximum
            if (signerCount >= MAX_SIGNERS)
            {
                btnMoreSigners.Enabled = false;
                _logger.LogDebug("Disabled Add Signers button - maximum limit of {0} reached", MAX_SIGNERS);
            }

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
                // Get the last email, name textboxes, and type combobox
                TextBox lastEmailTextBox = signerEmailTextBoxes[signerEmailTextBoxes.Count - 1];
                TextBox lastNameTextBox = signerNameTextBoxes[signerNameTextBoxes.Count - 1];
                ComboBox lastTypeComboBox = signerTypeComboBoxes[signerTypeComboBoxes.Count - 1];

                _logger.LogDebug("Removing signer #{0} ({1}, {2}, {3})",
                    signerCount, lastEmailTextBox.Name, lastNameTextBox.Name, lastTypeComboBox.Name);

                // Remove from form
                this.Controls.Remove(lastEmailTextBox);
                this.Controls.Remove(lastNameTextBox);
                this.Controls.Remove(lastTypeComboBox);

                // Dispose the controls
                lastEmailTextBox.Dispose();
                lastNameTextBox.Dispose();
                lastTypeComboBox.Dispose();

                // Remove from tracking lists
                signerEmailTextBoxes.RemoveAt(signerEmailTextBoxes.Count - 1);
                signerNameTextBoxes.RemoveAt(signerNameTextBoxes.Count - 1);
                signerTypeComboBoxes.RemoveAt(signerTypeComboBoxes.Count - 1);

                // Decrement signer count
                signerCount--;

                // Move carbon copy section up
                UpdateCarbonCopySectionPosition();

                // Re-enable Add Signer button if we're below the maximum
                if (signerCount < MAX_SIGNERS)
                {
                    btnMoreSigners.Enabled = true;
                    _logger.LogDebug("Re-enabled Add Signers button - below maximum limit");
                }

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

        private void BtnCarbonCopyAdd_Click(object sender, EventArgs e)
        {
            _logger.LogMethodEntry("BtnCarbonCopyAdd_Click");

            // Check if we've reached the maximum number of carbon copies
            if (carbonCopyCount >= MAX_CARBON_COPIES)
            {
                _logger.LogWarning("Cannot add more carbon copies - maximum limit of {0} reached", MAX_CARBON_COPIES);
                MessageBox.Show($"Maximum of {MAX_CARBON_COPIES} carbon copy recipients allowed.",
                    "Limit Reached", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            carbonCopyCount++;

            // Calculate carbon copy start position based on number of signers
            int carbonCopySectionStart = GetCarbonCopySectionStartY();
            int yOffset = carbonCopySectionStart + (carbonCopyCount - 1) * CONTROL_SPACING;

            _logger.LogDebug("Adding carbon copy #{0} at Y offset: {1}", carbonCopyCount, yOffset);

            // Create new email textbox - ALIGNED WITH DESIGNER POSITIONS
            TextBox newEmailTextBox = new TextBox();
            newEmailTextBox.Name = $"txtCarbonCopyEmail{carbonCopyCount}";
            newEmailTextBox.Location = new Point(SIGNER_EMAIL_X, yOffset);
            newEmailTextBox.Size = new Size(273, 27);
            newEmailTextBox.TabIndex = 13 + (carbonCopyCount - 1) * 2;
            SetPlaceholder(newEmailTextBox, $"Carbon Copy {carbonCopyCount} Email");

            // Create new name textbox - ALIGNED WITH DESIGNER POSITIONS
            TextBox newNameTextBox = new TextBox();
            newNameTextBox.Name = $"txtCarbonCopyName{carbonCopyCount}";
            newNameTextBox.Location = new Point(SIGNER_NAME_X, yOffset);
            newNameTextBox.Size = new Size(324, 27);
            newNameTextBox.TabIndex = 14 + (carbonCopyCount - 1) * 2;
            SetPlaceholder(newNameTextBox, $"Carbon Copy {carbonCopyCount} Name");

            // Add to form
            this.Controls.Add(newEmailTextBox);
            this.Controls.Add(newNameTextBox);

            // Add to our tracking lists
            carbonCopyEmailTextBoxes.Add(newEmailTextBox);
            carbonCopyNameTextBoxes.Add(newNameTextBox);

            // Update attachment section position
            UpdateAttachmentSectionPosition();

            // Enable Remove Carbon Copy button since we have more than one
            btnCarbonCopyRemove.Enabled = true;

            // Disable Add Carbon Copy button if we've reached the maximum
            if (carbonCopyCount >= MAX_CARBON_COPIES)
            {
                btnCarbonCopyAdd.Enabled = false;
                _logger.LogDebug("Disabled Add Carbon Copy button - maximum limit of {0} reached", MAX_CARBON_COPIES);
            }

            _logger.LogInformation("Successfully added carbon copy #{0}. Total carbon copies: {1}", carbonCopyCount, carbonCopyEmailTextBoxes.Count);
            _logger.LogMethodExit("BtnCarbonCopyAdd_Click");
        }

        private void BtnCarbonCopyRemove_Click(object sender, EventArgs e)
        {
            _logger.LogMethodEntry("BtnCarbonCopyRemove_Click");

            // Only remove if we have more than one carbon copy
            if (carbonCopyEmailTextBoxes.Count <= 1)
            {
                _logger.LogWarning("Cannot remove carbon copy - only one carbon copy remains");
                MessageBox.Show("Cannot remove the last carbon copy. At least one carbon copy is required.",
                    "Cannot Remove", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // Get the last email and name textboxes
                TextBox lastEmailTextBox = carbonCopyEmailTextBoxes[carbonCopyEmailTextBoxes.Count - 1];
                TextBox lastNameTextBox = carbonCopyNameTextBoxes[carbonCopyNameTextBoxes.Count - 1];

                _logger.LogDebug("Removing carbon copy #{0} ({1}, {2})",
                    carbonCopyCount, lastEmailTextBox.Name, lastNameTextBox.Name);

                // Remove from form
                this.Controls.Remove(lastEmailTextBox);
                this.Controls.Remove(lastNameTextBox);

                // Dispose the controls
                lastEmailTextBox.Dispose();
                lastNameTextBox.Dispose();

                // Remove from tracking lists
                carbonCopyEmailTextBoxes.RemoveAt(carbonCopyEmailTextBoxes.Count - 1);
                carbonCopyNameTextBoxes.RemoveAt(carbonCopyNameTextBoxes.Count - 1);

                // Decrement carbon copy count
                carbonCopyCount--;

                // Update attachment section position
                UpdateAttachmentSectionPosition();

                // Re-enable Add Carbon Copy button if we're below the maximum
                if (carbonCopyCount < MAX_CARBON_COPIES)
                {
                    btnCarbonCopyAdd.Enabled = true;
                    _logger.LogDebug("Re-enabled Add Carbon Copy button - below maximum limit");
                }

                // Disable Remove Carbon Copy button if only one carbon copy left
                if (carbonCopyEmailTextBoxes.Count == 1)
                {
                    btnCarbonCopyRemove.Enabled = false;
                    _logger.LogDebug("Disabled Remove Carbon Copy button - only one carbon copy remains");
                }

                _logger.LogInformation("Successfully removed carbon copy. Remaining carbon copies: {0}", carbonCopyEmailTextBoxes.Count);
                _logger.LogMethodExit("BtnCarbonCopyRemove_Click");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error removing carbon copy", ex);
                MessageBox.Show($"Error removing carbon copy: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Calculate where the carbon copy section should start based on number of signers
        /// Designer.cs has first CC at Y=130, which is 82 + 1*35 + 13 spacing
        /// </summary>
        private int GetCarbonCopySectionStartY()
        {
            return SIGNER_START_Y + signerCount * CONTROL_SPACING + SECTION_SPACING;
        }

        /// <summary>
        /// Update the position of all carbon copy controls when signer count changes
        /// </summary>
        private void UpdateCarbonCopySectionPosition()
        {
            _logger.LogDebug("Updating carbon copy section position");

            int carbonCopyStartY = GetCarbonCopySectionStartY();

            // Move the carbon copy label (align with textbox top)
            lblCarbonCopy1.Top = carbonCopyStartY + 3; // +3 pixels to center label with textbox

            // Move all carbon copy textboxes
            for (int i = 0; i < carbonCopyEmailTextBoxes.Count; i++)
            {
                int yOffset = carbonCopyStartY + i * CONTROL_SPACING;
                carbonCopyEmailTextBoxes[i].Top = yOffset;
                carbonCopyNameTextBoxes[i].Top = yOffset;
            }

            // Move carbon copy buttons to align with first carbon copy row
            btnCarbonCopyAdd.Top = carbonCopyStartY - 1;      // -1 pixel for visual alignment
            btnCarbonCopyRemove.Top = carbonCopyStartY - 1;

            // Update attachment section position
            UpdateAttachmentSectionPosition();

            _logger.LogDebug("Carbon copy section moved to Y: {0}", carbonCopyStartY);
        }

        /// <summary>
        /// Update the position of attachment section and buttons based on carbon copy count
        /// </summary>
        private void UpdateAttachmentSectionPosition()
        {
            _logger.LogDebug("Updating attachment section position");

            int carbonCopyStartY = GetCarbonCopySectionStartY();
            int carbonCopyEndY = carbonCopyStartY + carbonCopyCount * CONTROL_SPACING;
            int attachmentSectionY = carbonCopyEndY + SECTION_SPACING + 10; // Extra spacing before attachments

            // Move the attachments label and grid
            label2.Top = attachmentSectionY;
            dgvAttachments.Top = attachmentSectionY;

            // Move the pagination controls to be below the attachments grid
            int paginationY = dgvAttachments.Bottom + 6; // 6 pixels spacing from grid
            btnPreviousPage.Top = paginationY;
            btnNextPage.Top = paginationY;
            lblAttachmentPageInfo.Top = paginationY + 5; // Center label vertically with buttons

            // Move the main buttons to be below the pagination controls
            int buttonY = paginationY + 40;
            btnSendDocuments.Top = buttonY;
            btnExit.Top = buttonY;

            // Update form's AutoScroll minimum size to accommodate all controls
            this.AutoScrollMinSize = new Size(1305, buttonY + 60);

            _logger.LogDebug("Attachment section positioned at Y: {0}, Pagination at Y: {1}, Buttons at Y: {2}",
                attachmentSectionY, paginationY, buttonY);
        }

        private void SetPlaceholder(TextBox textBox, string placeholderText)
        {
            SendMessage(textBox.Handle, EM_SETCUEBANNER, 0, placeholderText);
        }

        protected async void DocuSignForm_Load_1(object sender, EventArgs e)
        {
            _logger.LogMethodEntry("DocuSignForm_Load_1");

            try
            {
                // Show loading state
                dgvAttachments.Enabled = false;
                btnSendDocuments.Enabled = false;
                this.Cursor = Cursors.WaitCursor;

                // Load attachments asynchronously
                await LoadAttachmentAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError("Error loading form", ex);
                MessageBox.Show($"Error loading attachments: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Restore UI state
                dgvAttachments.Enabled = true;
                btnSendDocuments.Enabled = true;
                this.Cursor = Cursors.Default;
            }

            _logger.LogMethodExit("DocuSignForm_Load_1");
        }

        public async Task LoadAttachmentAsync()
        {
            _logger.LogMethodEntry("LoadAttachmentAsync");

            try
            {
                // Get parameters from arguments
                string requestFrom = dicArgs.ContainsKey("component") ? dicArgs["component"] : string.Empty;
                string key1 = dicArgs.ContainsKey("Key_1_ID") ? dicArgs["Key_1_ID"] : string.Empty;
                string key2 = dicArgs.ContainsKey("Key_2_ID") ? dicArgs["Key_2_ID"] : string.Empty;

                _logger.LogDebug("Loading attachments - RequestFrom: {0}, Key_1: {1}, Key_2: {2}", requestFrom, key1, key2);

                // Load data asynchronously to avoid blocking the UI
                await Task.Run(() =>
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    using (SqlCommand command = new SqlCommand("dbo.brptGetAttachmentsDocuSign", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        // Add parameters
                        command.Parameters.AddWithValue("@RequestFrom", requestFrom);
                        command.Parameters.AddWithValue("@Key_1", key1);
                        command.Parameters.AddWithValue("@Key_2", key2);

                        connection.Open();
                        _logger.LogDebug("Executing stored procedure: dbo.brptGetAttachmentsDocuSign");

                        using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                        {
                            _fullAttachmentDataTable = new DataTable();
                            adapter.Fill(_fullAttachmentDataTable);

                            // Sort by AddDate descending (latest first)
                            if (_fullAttachmentDataTable.Columns.Contains("AddDate"))
                            {
                                DataView dv = _fullAttachmentDataTable.DefaultView;
                                dv.Sort = "AddDate DESC";
                                _fullAttachmentDataTable = dv.ToTable();
                                _logger.LogDebug("Sorted attachments by AddDate descending");
                            }

                            _totalAttachmentRecords = _fullAttachmentDataTable.Rows.Count;
                            _totalAttachmentPages = (_totalAttachmentRecords + _attachmentPageSize - 1) / _attachmentPageSize; // Ceiling division
                            _currentAttachmentPage = 1;

                            _logger.LogInformation("Loaded {0} attachment(s), {1} pages", _totalAttachmentRecords, _totalAttachmentPages);
                        }
                    }
                });

                // Update UI on the UI thread
                this.Invoke((MethodInvoker)delegate
                {
                    // Initialize DataGridView columns
                    InitializeAttachmentDataGridView();

                    // Display first page
                    DisplayAttachmentPage();

                    _isAttachmentDataLoaded = true;
                });

                _logger.LogMethodExit("LoadAttachmentAsync");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error loading attachments", ex);
                throw;
            }
        }

        public void LoadAttachment()
        {
            // Synchronous wrapper for backward compatibility
            LoadAttachmentAsync().GetAwaiter().GetResult();
        }

        private void InitializeAttachmentDataGridView()
        {
            _logger.LogMethodEntry("InitializeAttachmentDataGridView");
            try
            {
                // Clear existing data
                dgvAttachments.Rows.Clear();
                dgvAttachments.Columns.Clear();
                dgvAttachments.AutoGenerateColumns = false;

                // Add checkbox column as first column
                DataGridViewCheckBoxColumn checkBoxColumn = new DataGridViewCheckBoxColumn();
                checkBoxColumn.Name = "Select";
                checkBoxColumn.HeaderText = "Select";
                checkBoxColumn.Width = 60;
                dgvAttachments.Columns.Add(checkBoxColumn);

                // Add other columns
                dgvAttachments.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "OrigFileName",
                    HeaderText = "Original File Name",
                    DataPropertyName = "OrigFileName",
                    Width = 225
                });

                dgvAttachments.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "AttachmentID",
                    HeaderText = "Attachment ID",
                    DataPropertyName = "AttachmentID",
                    Width = 100
                });

                // Add UniqueAttchID column (hidden)
                dgvAttachments.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "UniqueAttchID",
                    HeaderText = "Unique Attachment ID",
                    DataPropertyName = "UniqueAttchID",
                    Width = 100,
                    Visible = false  // Hidden column
                });

                dgvAttachments.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "FormName",
                    HeaderText = "Form Name",
                    DataPropertyName = "FormName",
                    Width = 150,
                    Visible = false  // Hidden column
                });

                dgvAttachments.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "Description",
                    HeaderText = "Description",
                    DataPropertyName = "Description",
                    Width = 250
                });

                dgvAttachments.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "AddedBy",
                    HeaderText = "Added By",
                    DataPropertyName = "AddedBy",
                    Width = 150
                });

                // Add date column with formatting
                dgvAttachments.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "AddDate",
                    HeaderText = "Add Date",
                    DataPropertyName = "AddDate",
                    Width = 200,
                    DefaultCellStyle = new DataGridViewCellStyle { Format = "MM/dd/yyyy hh:mm:ss tt" }
                });

                // Set grid properties
                dgvAttachments.AllowUserToAddRows = false;
                dgvAttachments.AllowUserToDeleteRows = false;
                dgvAttachments.ReadOnly = false;
                dgvAttachments.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                dgvAttachments.MultiSelect = false;
                dgvAttachments.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

                _logger.LogInformation("Attachment DataGridView initialized with {0} columns", dgvAttachments.Columns.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error initializing attachment DataGridView", ex);
                throw;
            }

            _logger.LogMethodExit("InitializeAttachmentDataGridView");
        }

        private void DisplayAttachmentPage()
        {
            _logger.LogMethodEntry("DisplayAttachmentPage");

            try
            {
                if (_fullAttachmentDataTable == null || _fullAttachmentDataTable.Rows.Count == 0)
                {
                    dgvAttachments.DataSource = null;
                    lblAttachmentPageInfo.Text = "Page 0 of 0 (0 records)";
                    btnPreviousPage.Enabled = false;
                    btnNextPage.Enabled = false;
                    _logger.LogDebug("No attachment data to display");
                    return;
                }

                // Calculate start and end row indices
                int startIndex = (_currentAttachmentPage - 1) * _attachmentPageSize;
                int endIndex = Math.Min(startIndex + _attachmentPageSize, _totalAttachmentRecords);

                // Create a new DataTable for the current page
                DataTable pageTable = _fullAttachmentDataTable.Clone();
                for (int i = startIndex; i < endIndex; i++)
                {
                    DataRow newRow = pageTable.NewRow();
                    foreach (DataColumn column in _fullAttachmentDataTable.Columns)
                    {
                        newRow[column.ColumnName] = _fullAttachmentDataTable.Rows[i][column.ColumnName];
                    }
                    pageTable.Rows.Add(newRow);
                }

                dgvAttachments.DataSource = pageTable;

                // The checkbox column is not data-bound, so it needs to be set separately
                foreach (DataGridViewRow row in dgvAttachments.Rows)
                {
                    if (row.Cells["Select"].Value == null)
                    {
                        row.Cells["Select"].Value = false;
                    }
                }

                // Update page info label
                lblAttachmentPageInfo.Text = $"Page {_currentAttachmentPage} of {_totalAttachmentPages} ({_totalAttachmentRecords} records)";

                // Update button states
                btnPreviousPage.Enabled = _currentAttachmentPage > 1;
                btnNextPage.Enabled = _currentAttachmentPage < _totalAttachmentPages;

                _logger.LogDebug("Displaying attachment page {0} of {1}, showing rows {2} to {3}",
                    _currentAttachmentPage, _totalAttachmentPages, startIndex + 1, endIndex);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error displaying attachment page", ex);
                MessageBox.Show($"Error displaying attachment page: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            _logger.LogMethodExit("DisplayAttachmentPage");
        }

        private void btnPreviousPage_Click(object sender, EventArgs e)
        {
            _logger.LogMethodEntry("btnPreviousPage_Click");

            try
            {
                if (_currentAttachmentPage > 1)
                {
                    _currentAttachmentPage--;
                    DisplayAttachmentPage();
                    _logger.LogInformation("Navigated to previous attachment page: {0}", _currentAttachmentPage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error navigating to previous attachment page", ex);
                MessageBox.Show($"Error navigating to previous page: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            _logger.LogMethodExit("btnPreviousPage_Click");
        }

        private void btnNextPage_Click(object sender, EventArgs e)
        {
            _logger.LogMethodEntry("btnNextPage_Click");

            try
            {
                if (_currentAttachmentPage < _totalAttachmentPages)
                {
                    _currentAttachmentPage++;
                    DisplayAttachmentPage();
                    _logger.LogInformation("Navigated to next attachment page: {0}", _currentAttachmentPage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error navigating to next attachment page", ex);
                MessageBox.Show($"Error navigating to next page: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            _logger.LogMethodExit("btnNextPage_Click");
        }

        private void btnSendDocuments_Click(object sender, EventArgs e)
        {
            _logger.LogMethodEntry("btnSendDocuments_Click");

            // Disable the button immediately to prevent multiple clicks
            btnSendDocuments.Enabled = false;
            string originalButtonText = btnSendDocuments.Text;
            btnSendDocuments.Text = "Processing...";
            this.Cursor = Cursors.WaitCursor;

            _logger.LogDebug("Send Documents button disabled during processing");

            try
            {
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

                // Validate carbon copy fields (optional - only if filled)
                _logger.LogDebug("Validating {0} carbon copy recipient(s)", carbonCopyEmailTextBoxes.Count);

                for (int i = 0; i < carbonCopyEmailTextBoxes.Count; i++)
                {
                    TextBox emailTextBox = carbonCopyEmailTextBoxes[i];
                    TextBox nameTextBox = carbonCopyNameTextBoxes[i];
                    int ccNumber = i + 1;

                    string email = emailTextBox.Text.Trim();
                    string name = nameTextBox.Text.Trim();

                    // If either field is filled, both must be valid
                    if (!string.IsNullOrEmpty(email) || !string.IsNullOrEmpty(name))
                    {
                        if (string.IsNullOrEmpty(email))
                        {
                            validationErrors.Add($"Carbon Copy {ccNumber} email is empty");
                            _logger.LogWarning("Validation failed: Carbon Copy {0} email is empty", ccNumber);
                        }
                        else if (!IsValidEmail(email))
                        {
                            validationErrors.Add($"Carbon Copy {ccNumber} email is not valid");
                            _logger.LogWarning("Validation failed: Carbon Copy {0} email '{1}' is not valid", ccNumber, email);
                        }

                        if (string.IsNullOrEmpty(name))
                        {
                            validationErrors.Add($"Carbon Copy {ccNumber} name is empty");
                            _logger.LogWarning("Validation failed: Carbon Copy {0} name is empty", ccNumber);
                        }
                    }
                }

                // Check if any attachments are selected (check all pages, not just current page)
                bool hasSelectedAttachments = false;
                int selectedCount = 0;

                // Check current visible rows
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

                _logger.LogInformation("Created DocuSignRequestDto with {0} signer(s), {1} carbon copy recipient(s), and {2} attachment(s)",
                    docuSignRequest.Signers.Count, docuSignRequest.CarbonCopies.Count, docuSignRequest.SelectedAttachments.Count);

                var userInputs = new UserInputs()
                {
                    ConnectionString = AppSettings.GetConnectionString(),
                    DocuSignClientId = AppSettings.GetDocuSignClientId(),
                    DocuSignAuthServer = AppSettings.GetDocuSignAuthServer(),
                    DocuSignImpersonatedUserID = AppSettings.GetDocuSignImpersonatedUserID(),
                    DocuSignPrivateKeyFile = AppSettings.GetDocuSignPrivateKeyFile(),
                    DocuSignAccountID = AppSettings.GetDocuSignAccountID(),
                    AttachmentDBConnection = AppSettings.GetAttachmentDBConnectionString(),
                    DocuSignApiBaseUrl = AppSettings.GetDocuSignApiBaseUrl(),
                    SignatureAnchorPrimaryText = AppSettings.GetSignatureAnchorPrimaryText(),
                    SignatureAnchorSecondaryPattern = AppSettings.GetSignatureAnchorSecondaryPattern(),
                    SignatureAnchorXOffset = AppSettings.GetSignatureAnchorXOffset(),
                    SignatureAnchorYOffset = AppSettings.GetSignatureAnchorYOffset(),
                    SignatureAnchorUnits = AppSettings.GetSignatureAnchorUnits(),
                    SignatureAnchorIgnoreIfNotPresent = AppSettings.GetSignatureAnchorIgnoreIfNotPresent(),
                    SignatureAnchorCaseSensitive = AppSettings.GetSignatureAnchorCaseSensitive()
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
                               $"Carbon Copies: {docuSignRequest.CarbonCopies.Count}\n" +
                               $"Selected Attachments: {docuSignRequest.SelectedAttachments.Count}\n" +
                               $"Request Time: {docuSignRequest.RequestDateTime:MM/dd/yyyy HH:mm:ss}";

                MessageBox.Show(summary, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                _logger.LogInformation("DocuSign request completed successfully");
                _logger.LogMethodExit("btnSendDocuments_Click", "Success");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error saving DocuSign request", ex);
                MessageBox.Show($"Error saving DocuSign request: {ex.Message}", "Database Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                _logger.LogMethodExit("btnSendDocuments_Click", "Error");
            }
            finally
            {
                // Re-enable the button and restore UI state
                btnSendDocuments.Enabled = true;
                btnSendDocuments.Text = originalButtonText;
                this.Cursor = Cursors.Default;
                _logger.LogDebug("Send Documents button re-enabled after processing");
            }
        }

        private int SaveDocuSignEntries(DocuSignRequestDto docuSignRequest, string envelopeID)
        {
            _logger.LogMethodEntry("SaveDocuSignEntries");

            // Prepare comma-delimited strings for signers
            string signerEmails = string.Join(",", docuSignRequest.Signers.Select(s => s.Email));
            string signerNames = string.Join(",", docuSignRequest.Signers.Select(s => s.Name));
            string signerTypes = string.Join(",", docuSignRequest.Signers.Select(s => s.SignerType));

            // Prepare comma-delimited strings for carbon copies
            string carbonCopyEmails = string.Join(",", docuSignRequest.CarbonCopies.Select(cc => cc.Email));
            string carbonCopyNames = string.Join(",", docuSignRequest.CarbonCopies.Select(cc => cc.Name));

            // Prepare comma-delimited strings for attachments
            string attachmentIds = string.Join(",", docuSignRequest.SelectedAttachments.Select(a => a.AttachmentID));
            string attachmentNames = string.Join(",", docuSignRequest.SelectedAttachments.Select(a => a.OrigFileName));

            string UniqueAttchID = docuSignRequest.SelectedAttachments.Select(a => a.UniqueAttchID).Distinct().First();

            // Get requestor from args or use a default
            string requestor = dicArgs.ContainsKey("requestor") ? dicArgs["requestor"] : Environment.UserName;

            _logger.LogDebug("Requestor: {0}", requestor);
            _logger.LogDebug("Signer Emails: {0}", signerEmails);
            _logger.LogDebug("Signer Names: {0}", signerNames);
            _logger.LogDebug("Signer Types: {0}", signerTypes);
            _logger.LogDebug("Carbon Copy Emails: {0}", carbonCopyEmails);
            _logger.LogDebug("Carbon Copy Names: {0}", carbonCopyNames);
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
                    command.Parameters.AddWithValue("@EnvelopeID", envelopeID);
                    command.Parameters.AddWithValue("@UniqueAttchID", UniqueAttchID);
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
            docuSignRequest.Key_1 = dicArgs.ContainsKey("Key_1_ID") ? dicArgs["Key_1_ID"] : Environment.UserName;
            docuSignRequest.Key_2 = dicArgs.ContainsKey("Key_2_ID") ? dicArgs["Key_2_ID"] : Environment.UserName;
            docuSignRequest.ProjectID = dicArgs.ContainsKey("projectID") ? dicArgs["projectID"] : string.Empty;

            // Add signers
            for (int i = 0; i < signerEmailTextBoxes.Count; i++)
            {
                var signer = new SignerDto
                {
                    Name = signerNameTextBoxes[i].Text.Trim(),
                    Email = signerEmailTextBoxes[i].Text.Trim(),
                    SignerOrder = i + 1,
                    SignerType = signerTypeComboBoxes[i].SelectedItem?.ToString() ?? "Primary"
                };
                docuSignRequest.Signers.Add(signer);
            }

            // Add carbon copies (only if both email and name are filled)
            for (int i = 0; i < carbonCopyEmailTextBoxes.Count; i++)
            {
                string email = carbonCopyEmailTextBoxes[i].Text.Trim();
                string name = carbonCopyNameTextBoxes[i].Text.Trim();

                if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(name))
                {
                    var carbonCopy = new CarbonCopyDto
                    {
                        Name = name,
                        Email = email,
                        CarbonCopyOrder = i + 1
                    };
                    docuSignRequest.CarbonCopies.Add(carbonCopy);
                }
            }

            // Add selected attachments from current page
            foreach (DataGridViewRow row in dgvAttachments.Rows)
            {
                if (row.Cells["Select"].Value != null && Convert.ToBoolean(row.Cells["Select"].Value))
                {
                    var attachment = new AttachmentDto
                    {
                        AttachmentID = row.Cells["AttachmentID"].Value?.ToString() ?? "",
                        UniqueAttchID = row.Cells["UniqueAttchID"].Value?.ToString() ?? "",
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

        /// <summary>
        /// Initialize the existing signers section if ProjectID is provided
        /// </summary>
        private void InitializeExistingSignersSection()
        {
            _logger.LogMethodEntry("InitializeExistingSignersSection");

            try
            {
                // Check if ProjectID is provided
                string projectID = dicArgs.ContainsKey("projectID") ? dicArgs["projectID"]?.Trim() : string.Empty;

                if (string.IsNullOrEmpty(projectID))
                {
                    _logger.LogDebug("ProjectID not provided, skipping existing signers section");
                    return;
                }

                _logger.LogInformation("ProjectID provided: {0}, initializing existing signers section", projectID);

                // Create label for existing signers
                lblExistingSigners = new Label();
                lblExistingSigners.Name = "lblExistingSigners";
                lblExistingSigners.Location = new Point(12, EXISTING_SIGNERS_Y + 3);
                lblExistingSigners.Size = new Size(200, 20);
                lblExistingSigners.Text = "Existing Signers:";
                lblExistingSigners.AutoSize = true;

                // Create existing signers dropdown
                cmbExistingSigners = new ComboBox();
                cmbExistingSigners.Name = "cmbExistingSigners";
                cmbExistingSigners.Location = new Point(156, EXISTING_SIGNERS_Y);
                cmbExistingSigners.Size = new Size(500, 28);
                cmbExistingSigners.DropDownStyle = ComboBoxStyle.DropDownList;
                cmbExistingSigners.DisplayMember = "DisplayText";
                cmbExistingSigners.ValueMember = "Email";

                // Create button to add existing signer
                btnAddExistingSigner = new Button();
                btnAddExistingSigner.Name = "btnAddExistingSigner";
                btnAddExistingSigner.Location = new Point(670, EXISTING_SIGNERS_Y - 1);
                btnAddExistingSigner.Size = new Size(160, 29);
                btnAddExistingSigner.Text = "Add Existing Signer";
                btnAddExistingSigner.UseVisualStyleBackColor = true;
                btnAddExistingSigner.Click += BtnAddExistingSigner_Click;

                // Add controls to form
                this.Controls.Add(lblExistingSigners);
                this.Controls.Add(cmbExistingSigners);
                this.Controls.Add(btnAddExistingSigner);

                // Load existing signers from database
                LoadExistingSigners(projectID);

                _logger.LogInformation("Existing signers section initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error initializing existing signers section", ex);
                // Don't throw - this is optional functionality
            }

            _logger.LogMethodExit("InitializeExistingSignersSection");
        }

        /// <summary>
        /// Load existing signers from database for the given ProjectID
        /// </summary>
        private void LoadExistingSigners(string projectID)
        {
            _logger.LogMethodEntry("LoadExistingSigners", projectID);

            try
            {
                string query = @"
                    SELECT DISTINCT 
                        PMSL.Project,
                        PMPM.FirstName,
                        PMPM.LastName,
                        PMPM.EMail
                    FROM PMSL PMSL
                    JOIN PMSS PMSS ON PMSL.Project = PMSS.Project AND PMSL.PMCo = PMSS.PMCo
                    JOIN PMPM PMPM ON PMSS.SendToFirm = PMPM.FirmNumber AND PMSS.SendToContact = PMPM.ContactCode
                    WHERE PMSL.PMCo = 101
                        AND PMPM.ExcludeYN = 'N'
                        AND PMPM.EMail IS NOT NULL
                        AND LTRIM(RTRIM(PMSL.Project)) = @ProjectID
                    ORDER BY PMPM.LastName, PMPM.FirstName";

                _logger.LogDebug("Executing query to load existing signers for ProjectID: {0}", projectID);

                using (SqlConnection connection = new SqlConnection(connectionString))
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ProjectID", projectID);

                    connection.Open();

                    using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                    {
                        _existingSignersDataTable = new DataTable();
                        adapter.Fill(_existingSignersDataTable);

                        _logger.LogInformation("Loaded {0} existing signer(s) for ProjectID: {1}", 
                            _existingSignersDataTable.Rows.Count, projectID);

                        // Create display items for dropdown
                        var signerItems = new List<object>();
                        signerItems.Add(new { DisplayText = "-- Select an existing signer --", Email = "", FirstName = "", LastName = "" });

                        foreach (DataRow row in _existingSignersDataTable.Rows)
                        {
                            string firstName = row["FirstName"]?.ToString() ?? "";
                            string lastName = row["LastName"]?.ToString() ?? "";
                            string email = row["EMail"]?.ToString() ?? "";
                            string displayText = $"{lastName}, {firstName} ({email})";

                            signerItems.Add(new { DisplayText = displayText, Email = email, FirstName = firstName, LastName = lastName });

                            _logger.LogDebug("Added existing signer: {0}", displayText);
                        }

                        cmbExistingSigners.DataSource = signerItems;
                        cmbExistingSigners.SelectedIndex = 0;

                // Disable button if no signers found
                if (_existingSignersDataTable.Rows.Count == 0)
                {
                    btnAddExistingSigner.Enabled = false;
                    _logger.LogWarning("No existing signers found for ProjectID: {0}", projectID);
                }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error loading existing signers", ex);
                MessageBox.Show($"Error loading existing signers: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);

                // Disable the controls on error
                if (cmbExistingSigners != null) cmbExistingSigners.Enabled = false;
                if (btnAddExistingSigner != null) btnAddExistingSigner.Enabled = false;
            }

            _logger.LogMethodExit("LoadExistingSigners");
        }

        /// <summary>
        /// Handle adding an existing signer from the dropdown
        /// </summary>
        private void BtnAddExistingSigner_Click(object sender, EventArgs e)
        {
            _logger.LogMethodEntry("BtnAddExistingSigner_Click");

            try
            {
                // Check if a signer is selected
                if (cmbExistingSigners.SelectedIndex <= 0)
                {
                    MessageBox.Show("Please select an existing signer from the dropdown.", "No Selection",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    _logger.LogWarning("No existing signer selected");
                    return;
                }

                // Get selected signer info
                dynamic selectedItem = cmbExistingSigners.SelectedItem;
                string email = selectedItem.Email;
                string firstName = selectedItem.FirstName;
                string lastName = selectedItem.LastName;
                string fullName = $"{firstName} {lastName}";

                _logger.LogDebug("Adding existing signer: {0} ({1})", fullName, email);

                // Check if we've reached the maximum number of signers
                if (signerCount >= MAX_SIGNERS)
                {
                    _logger.LogWarning("Cannot add more signers - maximum limit of {0} reached", MAX_SIGNERS);
                    MessageBox.Show($"Maximum of {MAX_SIGNERS} signers allowed.",
                        "Limit Reached", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Check if this email is already added
                bool emailExists = signerEmailTextBoxes.Any(tb => 
                    tb.Text.Trim().Equals(email, StringComparison.OrdinalIgnoreCase));

                if (emailExists)
                {
                    _logger.LogWarning("Signer with email {0} is already added", email);
                    MessageBox.Show($"This signer ({email}) has already been added to the list.",
                        "Duplicate Signer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Add to first empty signer slot or create new one
                bool added = false;

                // Try to fill an empty slot first
                for (int i = 0; i < signerEmailTextBoxes.Count; i++)
                {
                    if (string.IsNullOrWhiteSpace(signerEmailTextBoxes[i].Text))
                    {
                        signerEmailTextBoxes[i].Text = email;
                        signerNameTextBoxes[i].Text = fullName;
                        signerTypeComboBoxes[i].SelectedIndex = 0; // Primary
                        added = true;
                        _logger.LogInformation("Added existing signer to slot {0}", i + 1);
                        break;
                    }
                }

                // If all slots are filled, add a new one
                if (!added)
                {
                    // Simulate clicking "Add Signers" button
                    BtnMoreSigners_Click(sender, e);

                    // Fill the newly created signer fields
                    int lastIndex = signerEmailTextBoxes.Count - 1;
                    signerEmailTextBoxes[lastIndex].Text = email;
                    signerNameTextBoxes[lastIndex].Text = fullName;
                    signerTypeComboBoxes[lastIndex].SelectedIndex = 0; // Primary

                    _logger.LogInformation("Added existing signer as new slot {0}", signerEmailTextBoxes.Count);
                }

                // Reset dropdown to default selection
                cmbExistingSigners.SelectedIndex = 0;

                MessageBox.Show($"Added existing signer: {fullName}\n{email}", "Signer Added",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                _logger.LogInformation("Successfully added existing signer: {0} ({1})", fullName, email);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error adding existing signer", ex);
                MessageBox.Show($"Error adding existing signer: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            _logger.LogMethodExit("BtnAddExistingSigner_Click");
        }
    }
}
