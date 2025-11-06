using DocuSign.eSign.Client;
using DocuSign.eSign.Model;
using DocuSign.Requests;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Source1Solutions.DocuSign.WinSync
{
    public class SyncProcess
    {
        private Dictionary<string, string> dicArgs = new(StringComparer.OrdinalIgnoreCase);
        UserInputs userInputs = new();
        private Logger _logger;

        public SyncProcess(string[] args)
        {
            // Initialize logger first
            _logger = new Logger(
                AppSettings.GetLogFilePath(),
                AppSettings.GetLogFileName(),
                AppSettings.GetLogLevel()
            );

            _logger.LogInformation("=== DocuSign Sync Process Starting ===");
            _logger.LogInformation("Command line arguments: {0}", string.Join(" ", args));

            foreach (var arg in args)
            {
                var kv = arg.Split('=', 2); // split into 2 parts only
                if (kv.Length == 2)
                {
                    string key = kv[0].Trim();
                    string value = kv[1].Trim();
                    dicArgs[key] = value;
                    _logger.LogDebug("Argument parsed: {0} = {1}", key, value);
                }
            }

            userInputs = new UserInputs()
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

            _logger.LogInformation("UserInputs initialized successfully");

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

        public bool Sync()
        {
            _logger.LogMethodEntry("Sync");

            try
            {
                DocuSignRequestor docuSignRequestor = new(userInputs, _logger);
                _logger.LogInformation("DocuSignRequestor initialized");

                List<String> lstEnvolpeIDs = GetPendingEnvolpeIDs();
                _logger.LogInformation("Found {0} pending envelope(s)", lstEnvolpeIDs.Count);

                if (lstEnvolpeIDs.Count == 0)
                {
                    _logger.LogInformation("No pending envelopes to process");
                    return true;
                }

                int successCount = 0;
                int errorCount = 0;

                foreach (var envelopeID in lstEnvolpeIDs)
                {
                    try
                    {
                        _logger.LogInformation("Processing envelope ID: {0}", envelopeID);

                        Envelope envelope = docuSignRequestor.GetEnvelopeStatus(AppSettings.GetDocuSignAccountID(),
                                                                                envelopeID);

                        if (envelope != null)
                        {
                            _logger.LogInformation("Envelope status: {0} for envelope ID: {1}", envelope.Status, envelopeID);

                            var fileNames = GetFileNamesByEnvolpeID(envelopeID);
                            _logger.LogDebug("Retrieved {0} file name(s) for envelope ID: {1}", fileNames?.Count ?? 0, envelopeID);

                            if (envelope.Status == "completed" && fileNames != null && fileNames.Count > 0)
                            {
                                string fileName = string.Join("_", fileNames);
                                fileName = $"{fileName}_combined.pdf";

                                _logger.LogInformation("Downloading combined PDF for envelope: {0}", envelopeID);

                                // Download the PDF as byte array (stream)
                                var pdfBytes = docuSignRequestor.DownloadCombinedPdfAsBytes(AppSettings.GetDocuSignAccountID(), envelopeID);

                                if (pdfBytes != null && pdfBytes.Length > 0)
                                {
                                    _logger.LogInformation("Successfully downloaded {0} bytes for envelope ID: {1}", pdfBytes.Length, envelopeID);

                                    // Save PDF to database using stored procedure
                                    int docuSignID = GetDocuSignIDByEnvelopeID(envelopeID);
                                    SavePdfToDatabase(docuSignID, envelopeID, fileName, pdfBytes, envelope.Status);

                                    _logger.LogInformation("Successfully saved PDF to database for envelope ID: {0}", envelopeID);

                                    successCount++;
                                }
                                else
                                {
                                    _logger.LogWarning("Downloaded PDF is null or empty for envelope ID: {0}", envelopeID);
                                    errorCount++;
                                }
                            }
                            else
                            {
                                _logger.LogInformation("Envelope not ready for download. Status: {0}, FileCount: {1}",
                                    envelope.Status, fileNames?.Count ?? 0);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Envelope object is null for envelope ID: {0}", envelopeID);
                            errorCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error processing envelope ID {envelopeID}", ex);
                        errorCount++;
                    }
                }

                _logger.LogInformation("Sync completed. Success: {0}, Errors: {1}", successCount, errorCount);
                _logger.LogMethodExit("Sync", true);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Fatal error in Sync process", ex);
                _logger.LogMethodExit("Sync", false);
                return false;
            }
        }

        private List<String> GetPendingEnvolpeIDs()
        {
            _logger.LogMethodEntry("GetPendingEnvolpeIDs");

            List<String> lstEnvolpeIDs = new List<string>();
            string sqlCommand = @"select EnvolpeID from udtDocuSignTracking_S1S where RequestFrom = @RequestFrom
                                                                    and LTRIM(RTRIM(Key_1)) = LTRIM(RTRIM(@Key_1)) 
                                                                    and LTRIM(RTRIM(Key_2)) = LTRIM(RTRIM(@Key_2))
                                                                    and Status = 'Pending'";

            var RequestFrom = dicArgs.ContainsKey("component") ? dicArgs["component"] : string.Empty;
            var Key1 = dicArgs.ContainsKey("Key_1_ID") ? dicArgs["Key_1_ID"] : string.Empty;
            var Key2 = dicArgs.ContainsKey("Key_2_ID") ? dicArgs["Key_2_ID"].Trim() : string.Empty;

            _logger.LogDebug("Query parameters - RequestFrom: {0}, Key_1: {1}, Key_2: {2}", RequestFrom, Key1, Key2);
            _logger.LogDebug("SQL Command: {0}", sqlCommand);

            try
            {
                using (SqlConnection connection = new SqlConnection(AppSettings.GetConnectionString()))
                using (SqlCommand command = new SqlCommand(sqlCommand, connection))
                {
                    // Add parameters
                    command.Parameters.AddWithValue("@RequestFrom", RequestFrom);
                    command.Parameters.AddWithValue("@Key_1", Key1);
                    command.Parameters.AddWithValue("@Key_2", Key2);
                    connection.Open();

                    _logger.LogDebug("Executing query: {0}", sqlCommand);

                    // Execute and get the returned DocuSignID
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string envelopeId = reader["EnvolpeID"].ToString();
                            lstEnvolpeIDs.Add(envelopeId);
                            _logger.LogDebug("Found pending envelope ID: {0}", envelopeId);
                        }
                    }
                }

                _logger.LogMethodExit("GetPendingEnvolpeIDs", lstEnvolpeIDs.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error retrieving pending envelope IDs", ex);
                throw;
            }

            return lstEnvolpeIDs;
        }

        private List<String> GetFileNamesByEnvolpeID(string envolpeID)
        {
            _logger.LogMethodEntry("GetFileNamesByEnvolpeID", envolpeID);

            List<String> lstFileNames = new List<string>();
            string sqlCommand = @"select FileName from udtDocuSignTracking_S1S  Track 
                                        inner join udtDocuSignAttachments_S1S  Attach ON Track.DocuSignID = Attach.DocuSignID 
		                                            where EnvolpeID =  @EnvolpeID";

            try
            {
                using (SqlConnection connection = new SqlConnection(AppSettings.GetConnectionString()))
                using (SqlCommand command = new SqlCommand(sqlCommand, connection))
                {
                    command.Parameters.AddWithValue("@EnvolpeID", envolpeID);
                    connection.Open();

                    _logger.LogDebug("Executing query for file names: {0}", sqlCommand);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string fileName = reader["FileName"].ToString();
                            lstFileNames.Add(fileName);
                            _logger.LogDebug("Found file name: {0}", fileName);
                        }
                    }
                }

                _logger.LogMethodExit("GetFileNamesByEnvolpeID", lstFileNames.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving file names for envelope ID: {envolpeID}", ex);
                throw;
            }

            return lstFileNames;
        }

        private int GetDocuSignIDByEnvelopeID(string envelopeID)
        {
            _logger.LogMethodEntry("GetDocuSignIDByEnvelopeID", envelopeID);

            int docuSignID = 0;
            string sqlCommand = @"SELECT DocuSignID, Requestor, RequestFrom 
                                  FROM udtDocuSignTracking_S1S 
                                  WHERE EnvolpeID = @EnvelopeID";

            try
            {
                using (SqlConnection connection = new SqlConnection(AppSettings.GetConnectionString()))
                using (SqlCommand command = new SqlCommand(sqlCommand, connection))
                {
                    command.Parameters.AddWithValue("@EnvelopeID", envelopeID);
                    connection.Open();

                    _logger.LogDebug("Executing query: {0}", sqlCommand);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            docuSignID = Convert.ToInt32(reader["DocuSignID"]);
                            _logger.LogInformation("Retrieved DocuSignID: {0} for envelope: {1}", docuSignID, envelopeID);
                        }
                    }
                }

                _logger.LogMethodExit("GetDocuSignIDByEnvelopeID", docuSignID);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving DocuSignID for envelope ID: {envelopeID}", ex);
                throw;
            }

            return docuSignID;
        }

        private void SavePdfToDatabase(int docuSignID, string envelopeID, string fileName, byte[] pdfBytes, string status)
        {
            _logger.LogMethodEntry("SavePdfToDatabase", docuSignID, envelopeID, fileName, pdfBytes.Length, status);

            try
            {
                // Get tracking info
                string requestor = dicArgs.ContainsKey("currentUser") ? dicArgs["currentUser"] : "greddy@src1s.com_1075";


                using (SqlConnection connection = new SqlConnection(AppSettings.GetConnectionString()))
                {

                    _logger.LogDebug("Requestor: {0}", requestor);

                    // Call stored procedure to save PDF
                    using (SqlCommand command = new SqlCommand("brptUpdateDocuSignFileToDB_S1S", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        // Add parameters
                        command.Parameters.AddWithValue("@Requestor", requestor);
                        command.Parameters.AddWithValue("@Descripption", $"DocuSign completed document - {fileName}");
                        command.Parameters.AddWithValue("@FileName", fileName);
                        command.Parameters.AddWithValue("@FileType", "pdf");
                        command.Parameters.AddWithValue("@FileData", pdfBytes);
                        command.Parameters.AddWithValue("@DocuSignID", docuSignID);
                        command.Parameters.AddWithValue("@Status", status);

                        _logger.LogDebug("Executing stored procedure: brptUpdateDocuSignFileToDB_S1S");
                        _logger.LogDebug("Parameters - DocuSignID: {0}, EnvelopeID: {1}, FileName: {2}, FileSize: {3} bytes",
                            docuSignID, envelopeID, fileName, pdfBytes.Length);

                        connection.Open();

                        command.ExecuteNonQuery();

                        connection.Close();

                        _logger.LogInformation("Successfully saved PDF to database. DocuSignID: {0}, Size: {1} bytes",
                            docuSignID, pdfBytes.Length);
                    }
                }

                _logger.LogMethodExit("SavePdfToDatabase");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error saving PDF to database for DocuSignID: {docuSignID}", ex);
                throw;
            }
        }
    }
}
