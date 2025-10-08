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

        public SyncProcess(string[] args)
        {
            foreach (var arg in args)
            {
                var kv = arg.Split('=', 2); // split into 2 parts only
                if (kv.Length == 2)
                {
                    string key = kv[0].Trim();
                    string value = kv[1].Trim();
                    dicArgs[key] = value;
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

        }
        public bool Sync()
        {
            DocuSignRequestor docuSignRequestor = new(userInputs);

            List<String> lstEnvolpeIDs = GetPendingEnvolpeIDs();

            foreach (var envelopeID in lstEnvolpeIDs)
            {
                try
                {
                    Envelope envelope = docuSignRequestor.GetEnvelopeStatus(AppSettings.GetDocuSignAccountID(),
                                                                            envelopeID);

                    if (envelope != null)
                    {

                        var fileNames = GetFileNamesByEnvolpeID(envelopeID);
                        if (envelope.Status == "completed" && fileNames != null && fileNames.Count > 0)
                        {
                            string fileName = string.Join("_", fileNames);
                            fileName = Path.Combine(@"C:\Users\GuruPrasadReddy\OneDrive - Source One Solutions\Desktop\SourceOne", $"{fileName}_combined.pdf");

                            // Download and save the documents
                            var combinedPdf = docuSignRequestor.DownloadCombinedPdf(AppSettings.GetDocuSignAccountID(),
                                                                                        envelopeID,
                                                                                        fileName);
                            if (combinedPdf != null && combinedPdf.Length > 0)
                            {
                                // Update the status in the database
                                string updateCommand = @"UPDATE udtDocuSignTracking_S1S
                                                 SET Status = @Status
                                                 WHERE EnvolpeID = @EnvolpeID";
                                using (SqlConnection connection = new SqlConnection(AppSettings.GetConnectionString()))
                                using (SqlCommand command = new SqlCommand(updateCommand, connection))
                                {
                                    command.Parameters.AddWithValue("@Status", envelope.Status);
                                    //command.Parameters.AddWithValue("@LastUpdated", DateTime.UtcNow);
                                    command.Parameters.AddWithValue("@EnvolpeID", envelopeID);
                                    connection.Open();
                                    command.ExecuteNonQuery();
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing envelope ID {envelopeID}: {ex.Message}");
                    // Optionally log the error or handle it as needed
                }
            }

            return true;
        }

        private List<String> GetPendingEnvolpeIDs()
        {
            List<String> lstEnvolpeIDs = new List<string>();
            string sqlCommand = @"select EnvolpeID from udtDocuSignTracking_S1S where RequestFrom = @RequestFrom
                                                                    and LTRIM(RTRIM(Key_1)) = LTRIM(RTRIM(@Key_1)) 
                                                                    and LTRIM(RTRIM(Key_2)) = LTRIM(RTRIM(@Key_2))
                                                                    and Status = 'Pending'";

            var RequestFrom = dicArgs.ContainsKey("component") ? dicArgs["component"] : string.Empty;
            var Key1 = dicArgs.ContainsKey("companyID") ? dicArgs["companyID"] : string.Empty;
            var Key2 = dicArgs.ContainsKey("contractID") ? dicArgs["contractID"].Trim() : string.Empty;

            using (SqlConnection connection = new SqlConnection(AppSettings.GetConnectionString()))
            using (SqlCommand command = new SqlCommand(sqlCommand, connection))
            {
                // Add parameters
                command.Parameters.AddWithValue("@RequestFrom", RequestFrom);
                command.Parameters.AddWithValue("@Key_1", Key1);
                command.Parameters.AddWithValue("@Key_2", Key2);
                connection.Open();
                // Execute and get the returned DocuSignID
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lstEnvolpeIDs.Add(reader["EnvolpeID"].ToString());
                    }
                }
            }

            return lstEnvolpeIDs;
        }

        private List<String> GetFileNamesByEnvolpeID(string envolpeID)
        {
            List<String> lstFileNames = new List<string>();
            string sqlCommand = @"select FileName from udtDocuSignTracking_S1S  Track 
                                        inner join udtDocuSignAttachments_S1S  Attach ON Track.DocuSignID = Attach.DocuSignID 
		                                            where EnvolpeID =  @EnvolpeID";

            using (SqlConnection connection = new SqlConnection(AppSettings.GetConnectionString()))
            using (SqlCommand command = new SqlCommand(sqlCommand, connection))
            {
                command.Parameters.AddWithValue("@EnvolpeID", envolpeID);
                connection.Open();
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lstFileNames.Add(reader["FileName"].ToString());
                    }
                }
            }

            return lstFileNames;
        }

        private string GetUniqueAttachmentID()
        {
            string uniqueID = string.Empty;
            var Key1 = dicArgs.ContainsKey("companyID") ? dicArgs["companyID"] : string.Empty;
            var Key2 = dicArgs.ContainsKey("contractID") ? dicArgs["contractID"].Trim() : string.Empty;

            string sqlCommand = @"select UniqueAttchID from JCCM where JCCo  = LTRIM(RTRIM(@Key_1)) 
                                            and LTRIM(RTRIM(Contract)) = LTRIM(RTRIM(@Key_2))";
            using (SqlConnection connection = new SqlConnection(AppSettings.GetConnectionString()))
            using (SqlCommand command = new SqlCommand(sqlCommand, connection))
            {
                command.Parameters.AddWithValue("@Key_1", Key1);
                command.Parameters.AddWithValue("@Key_2", Key2);
                connection.Open();
                uniqueID = command.ExecuteScalar().ToString() ?? string.Empty;
            }
            return uniqueID;
        }
    }
}
