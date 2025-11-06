using Microsoft.Data.SqlClient;
using System.Data;
using DocuSign.Requests;

namespace Source1Solutions.DocuSign.WinSync
{
    public partial class SyncForm : Form
    {
        string connectionString = AppSettings.GetConnectionString();
        string _argsString;
        Dictionary<string, string> dicArgs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private Logger _logger;
        private DataTable _fullDataTable;
        private int _currentPage = 1;
        private const int _pageSize = 20;
        private int _totalPages = 0;
        SyncProcess _syncProcess = null;
        private int _totalRecords = 0;

        public SyncForm(string[] args)
        {
            // Initialize logger
            _logger = new Logger(
                AppSettings.GetLogFilePath(),
                AppSettings.GetLogFileName(),
                AppSettings.GetLogLevel()
            );

            _logger.LogInformation("=== DocuSign WinSync Application Starting ===");

            _argsString = string.Join(",", args);
            _logger.LogInformation("Command line arguments: {0}", _argsString);

            InitializeComponent();

            // Parse command line arguments
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

            // Set filter values from command line arguments
            txtRequestFrom.Text = dicArgs.ContainsKey("component") ? dicArgs["component"] : string.Empty;
            txtKey1.Text = dicArgs.ContainsKey("Key_1_ID") ? dicArgs["Key_1_ID"] : string.Empty;
            txtKey2.Text = dicArgs.ContainsKey("Key_2_ID") ? dicArgs["Key_2_ID"] : string.Empty;

            _syncProcess = new SyncProcess(args);

            _logger.LogInformation("SyncForm initialized successfully");

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

        private void Form1_Load(object sender, EventArgs e)
        {
            _logger.LogMethodEntry("Form1_Load");

            try
            {
                // Initialize DataGridView
                InitializeDataGridView();

                // Load data
                LoadDocuSignTrackingData();

                _logger.LogInformation("Form loaded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error loading form", ex);
                MessageBox.Show($"Error loading form: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            _logger.LogMethodExit("Form1_Load");
        }

        private void InitializeDataGridView()
        {
            _logger.LogMethodEntry("InitializeDataGridView");

            try
            {
                // Clear existing columns
                dgvDocuSignTracking.Columns.Clear();
                dgvDocuSignTracking.AutoGenerateColumns = false;

                // Add columns matching the stored procedure
                dgvDocuSignTracking.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "DocuSignID",
                    HeaderText = "DocuSign ID",
                    DataPropertyName = "DocuSignID",
                    Width = 100
                });

                dgvDocuSignTracking.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "EnvolpeID",
                    HeaderText = "Envelope ID",
                    DataPropertyName = "EnvolpeID",
                    Width = 200
                });

                dgvDocuSignTracking.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "Requestor",
                    HeaderText = "Requestor",
                    DataPropertyName = "Requestor",
                    Width = 120
                });

                dgvDocuSignTracking.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "RequestedDtm",
                    HeaderText = "Requested Date",
                    DataPropertyName = "RequestedDtm",
                    Width = 150,
                    DefaultCellStyle = new DataGridViewCellStyle { Format = "MM/dd/yyyy HH:mm:ss" }
                });

                dgvDocuSignTracking.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "Signors",
                    HeaderText = "Signers",
                    DataPropertyName = "Signors",
                    Width = 200
                });

                dgvDocuSignTracking.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "SignorsName",
                    HeaderText = "Signers Name",
                    DataPropertyName = "SignorsName",
                    Width = 200
                });

                dgvDocuSignTracking.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "Attachments",
                    HeaderText = "Attachments",
                    DataPropertyName = "Attachments",
                    Width = 250
                });

                dgvDocuSignTracking.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "CarbonCopies",
                    HeaderText = "Carbon Copies",
                    DataPropertyName = "CarbonCopies",
                    Width = 200
                });

                dgvDocuSignTracking.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "CarbonCopiesName",
                    HeaderText = "Carbon Copies Name",
                    DataPropertyName = "CarbonCopiesName",
                    Width = 200
                });

                dgvDocuSignTracking.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "RequestFrom",
                    HeaderText = "Request From",
                    DataPropertyName = "RequestFrom",
                    Width = 120
                });

                dgvDocuSignTracking.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "Key_1",
                    HeaderText = "Key 1",
                    DataPropertyName = "Key_1",
                    Width = 100
                });

                dgvDocuSignTracking.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "Key_2",
                    HeaderText = "Key 2",
                    DataPropertyName = "Key_2",
                    Width = 100
                });

                dgvDocuSignTracking.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "Status",
                    HeaderText = "Status",
                    DataPropertyName = "Status",
                    Width = 100
                });

                dgvDocuSignTracking.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "Error_Msg",
                    HeaderText = "Error Message",
                    DataPropertyName = "Error_Msg",
                    Width = 250
                });

                // Set grid properties
                dgvDocuSignTracking.AllowUserToAddRows = false;
                dgvDocuSignTracking.AllowUserToDeleteRows = false;
                dgvDocuSignTracking.ReadOnly = true;
                dgvDocuSignTracking.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                dgvDocuSignTracking.MultiSelect = false;
                dgvDocuSignTracking.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
                dgvDocuSignTracking.RowHeadersVisible = true;

                _logger.LogInformation("DataGridView initialized with {0} columns", dgvDocuSignTracking.Columns.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error initializing DataGridView", ex);
                throw;
            }

            _logger.LogMethodExit("InitializeDataGridView");
        }

        private void LoadDocuSignTrackingData()
        {
            _logger.LogMethodEntry("LoadDocuSignTrackingData");

            try
            {
                string requestFrom = txtRequestFrom.Text.Trim();
                string key1 = txtKey1.Text.Trim();
                string key2 = txtKey2.Text.Trim();

                _logger.LogDebug("Loading tracking data with filters - RequestFrom: {0}, Key_1: {1}, Key_2: {2}",
                    requestFrom, key1, key2);

                // First, perform synchronization
                _syncProcess.Sync();

                using (SqlConnection connection = new SqlConnection(connectionString))
                using (SqlCommand command = new SqlCommand("dbo.GetDocuSignTrackingDetails_S1S", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    // Add parameters
                    command.Parameters.AddWithValue("@RequestFrom", string.IsNullOrEmpty(requestFrom) ? (object)DBNull.Value : requestFrom);
                    command.Parameters.AddWithValue("@Key_1", string.IsNullOrEmpty(key1) ? (object)DBNull.Value : key1);
                    command.Parameters.AddWithValue("@Key_2", string.IsNullOrEmpty(key2) ? (object)DBNull.Value : key2);

                    connection.Open();
                    _logger.LogDebug("Executing stored procedure: GetDocuSignTrackingDetails_S1S");

                    using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                    {
                        _fullDataTable = new DataTable();
                        adapter.Fill(_fullDataTable);

                        // Sort by RequestedDtm descending
                        DataView dv = _fullDataTable.DefaultView;
                        dv.Sort = "RequestedDtm DESC";
                        _fullDataTable = dv.ToTable();

                        _totalRecords = _fullDataTable.Rows.Count;
                        _totalPages = (_totalRecords + _pageSize - 1) / _pageSize; // Ceiling division
                        _currentPage = 1;

                        _logger.LogInformation("Loaded {0} tracking record(s), {1} pages", _totalRecords, _totalPages);

                        // Display first page
                        DisplayPage();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error loading DocuSign tracking data", ex);
                MessageBox.Show($"Error loading tracking data: {ex.Message}", "Database Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            _logger.LogMethodExit("LoadDocuSignTrackingData");
        }

        private void DisplayPage()
        {
            _logger.LogMethodEntry("DisplayPage");

            try
            {
                if (_fullDataTable == null || _fullDataTable.Rows.Count == 0)
                {
                    dgvDocuSignTracking.DataSource = null;
                    lblPageInfo.Text = "Page 0 of 0 (0 records)";
                    btnPrevious.Enabled = false;
                    btnNext.Enabled = false;
                    _logger.LogDebug("No data to display");
                    return;
                }

                // Calculate start and end row indices
                int startIndex = (_currentPage - 1) * _pageSize;
                int endIndex = Math.Min(startIndex + _pageSize, _totalRecords);

                // Create a new DataTable for the current page
                DataTable pageTable = _fullDataTable.Clone();
                for (int i = startIndex; i < endIndex; i++)
                {
                    pageTable.ImportRow(_fullDataTable.Rows[i]);
                }

                dgvDocuSignTracking.DataSource = pageTable;

                // Update page info label
                lblPageInfo.Text = $"Page {_currentPage} of {_totalPages} ({_totalRecords} records)";

                // Update button states
                btnPrevious.Enabled = _currentPage > 1;
                btnNext.Enabled = _currentPage < _totalPages;

                _logger.LogDebug("Displaying page {0} of {1}, showing rows {2} to {3}",
                    _currentPage, _totalPages, startIndex + 1, endIndex);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error displaying page", ex);
                MessageBox.Show($"Error displaying page: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            _logger.LogMethodExit("DisplayPage");
        }

        private void btnPrevious_Click(object sender, EventArgs e)
        {
            _logger.LogMethodEntry("btnPrevious_Click");

            try
            {
                if (_currentPage > 1)
                {
                    _currentPage--;
                    DisplayPage();
                    _logger.LogInformation("Navigated to previous page: {0}", _currentPage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error navigating to previous page", ex);
                MessageBox.Show($"Error navigating to previous page: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            _logger.LogMethodExit("btnPrevious_Click");
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            _logger.LogMethodEntry("btnNext_Click");

            try
            {
                if (_currentPage < _totalPages)
                {
                    _currentPage++;
                    DisplayPage();
                    _logger.LogInformation("Navigated to next page: {0}", _currentPage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error navigating to next page", ex);
                MessageBox.Show($"Error navigating to next page: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            _logger.LogMethodExit("btnNext_Click");
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            _logger.LogMethodEntry("btnRefresh_Click");

            try
            {
                // Reload the data
                LoadDocuSignTrackingData();
                _logger.LogInformation("Data refreshed successfully");
                MessageBox.Show("Data refreshed successfully!", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error refreshing data", ex);
                MessageBox.Show($"Error refreshing data: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            _logger.LogMethodExit("btnRefresh_Click");
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            _logger.LogInformation("Application closing");
            this.Close();
        }
    }
}
