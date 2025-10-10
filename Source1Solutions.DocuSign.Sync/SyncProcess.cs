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

namespace Source1Solutions.DocuSign.Sync
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
                AttachmentDBConnection = AppSettings.GetAttachmentDBConnectionString()
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
                                fileName = Path.Combine(@"C:\Users\GuruPrasadReddy\OneDrive - Source One Solutions\Desktop\SourceOne", $"{fileName}_combined.pdf");
                                
                                _logger.LogInformation("Downloading combined PDF to: {0}", fileName);

                                // Download and save the documents
                                var combinedPdf = docuSignRequestor.DownloadCombinedPdf(AppSettings.GetDocuSignAccountID(),
                                                                                            envelopeID,
                                                                                            fileName);
                                                                                            
                                if (combinedPdf != null && combinedPdf.Length > 0)
                                {
                                    _logger.LogInformation("Successfully downloaded {0} bytes for envelope ID: {1}", combinedPdf.Length, envelopeID);
                                    
                                    // Update the status in the database
                                    string updateCommand = @"UPDATE udtDocuSignTracking_S1S
                                                     SET Status = @Status
                                                     WHERE EnvolpeID = @EnvolpeID";
                                    using (SqlConnection connection = new SqlConnection(AppSettings.GetConnectionString()))
                                    using (SqlCommand command = new SqlCommand(updateCommand, connection))
                                    {
                                        command.Parameters.AddWithValue("@Status", envelope.Status);
                                        command.Parameters.AddWithValue("@EnvolpeID", envelopeID);
                                        connection.Open();
                                        int rowsAffected = command.ExecuteNonQuery();
                                        
                                        _logger.LogInformation("Updated status to '{0}' for envelope ID: {1} ({2} row(s) affected)", 
                                            envelope.Status, envelopeID, rowsAffected);
                                    }
                                    
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
            var Key1 = dicArgs.ContainsKey("companyID") ? dicArgs["companyID"] : string.Empty;
            var Key2 = dicArgs.ContainsKey("contractID") ? dicArgs["contractID"].Trim() : string.Empty;

            _logger.LogDebug("Query parameters - RequestFrom: {0}, Key_1: {1}, Key_2: {2}", RequestFrom, Key1, Key2);

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

        private string GetUniqueAttachmentID()
        {
            _logger.LogMethodEntry("GetUniqueAttachmentID");
            
            string uniqueID = string.Empty;
            var Key1 = dicArgs.ContainsKey("companyID") ? dicArgs["companyID"] : string.Empty;
            var Key2 = dicArgs.ContainsKey("contractID") ? dicArgs["contractID"].Trim() : string.Empty;

            _logger.LogDebug("Query parameters - Key_1: {0}, Key_2: {1}", Key1, Key2);

            string sqlCommand = @"select UniqueAttchID from JCCM where JCCo  = LTRIM(RTRIM(@Key_1)) 
                                            and LTRIM(RTRIM(Contract)) = LTRIM(RTRIM(@Key_2))";
            try
            {
                using (SqlConnection connection = new SqlConnection(AppSettings.GetConnectionString()))
                using (SqlCommand command = new SqlCommand(sqlCommand, connection))
                {
                    command.Parameters.AddWithValue("@Key_1", Key1);
                    command.Parameters.AddWithValue("@Key_2", Key2);
                    connection.Open();
                    
                    _logger.LogDebug("Executing query: {0}", sqlCommand);
                    
                    uniqueID = command.ExecuteScalar()?.ToString() ?? string.Empty;
                    _logger.LogInformation("Retrieved unique attachment ID: {0}", uniqueID);
                }
                
                _logger.LogMethodExit("GetUniqueAttachmentID", uniqueID);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error retrieving unique attachment ID", ex);
                throw;
            }
            
            return uniqueID;
        }
    }
}
