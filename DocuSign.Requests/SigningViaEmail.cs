using DocuSign.eSign.Api;
using DocuSign.eSign.Client;
using DocuSign.eSign.Model;
using Microsoft.Data.SqlClient;
using Org.BouncyCastle.Crypto.Signers;
using System;
using System.Collections.Generic;
using System.Text;

namespace DocuSign.Requests
{
    public static class SigningViaEmail
    {
        private static string AttachmentDBConnection = "Server=WAP-sql.viewpointdata.cloud,4316;Database=VPAttachments;User Id=ReportBuilder;Password=SourceOne@20230816;";

        public static string SendEnvelopeViaEmail(string accessToken,
                                                    string basePath,
                                                    string accountId,
                                                    List<SignerDto> signers,
                                                    List<CarbonCopyDto> carbonCopies,
                                                    List<AttachmentDto> selectedAttachments,
                                                    string envStatus,
                                                    Logger logger)
        {
            Logger _logger = logger;

            _logger.LogMethodEntry("SendEnvelopeViaEmail", basePath, accountId, envStatus);
            _logger.LogInformation("Sending envelope with {0} signer(s), {1} carbon copy recipient(s), and {2} attachment(s)", 
                signers.Count, carbonCopies.Count, selectedAttachments.Count);

            try
            {
                EnvelopeDefinition env = MakeEnvelope(signers, carbonCopies, selectedAttachments, envStatus, _logger);
                _logger.LogDebug("Envelope definition created successfully");

                var docuSignClient = new DocuSignClient(basePath);
                docuSignClient.Configuration.DefaultHeader.Add("Authorization", "Bearer " + accessToken);
                _logger.LogDebug("DocuSign client configured with authorization");

                EnvelopesApi envelopesApi = new EnvelopesApi(docuSignClient);
                EnvelopeSummary results = envelopesApi.CreateEnvelope(accountId, env);

                _logger.LogInformation("Envelope created successfully with ID: {0}", results.EnvelopeId);
                _logger.LogDebug("Envelope URI: {0}", results.Uri);
                _logger.LogDebug("Envelope Status: {0}", results.Status);
                _logger.LogMethodExit("SendEnvelopeViaEmail", results.EnvelopeId);

                return results.EnvelopeId;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error sending envelope via email", ex);
                throw;
            }
        }

        public static EnvelopeDefinition MakeEnvelope(List<SignerDto> signers,
                                                    List<CarbonCopyDto> carbonCopies,
                                                    List<AttachmentDto> selectedAttachments,
                                                    string envStatus,
                                                    Logger logger)
        {
            Logger _logger = logger;

            _logger.LogMethodEntry("MakeEnvelope", envStatus);
            _logger.LogDebug("Creating envelope with {0} signers, {1} carbon copy recipients, and {2} attachments", 
                signers.Count, carbonCopies.Count, selectedAttachments.Count);

            try
            {

                List<Document> documents = new List<Document>();

                int docIndex = 1;


                foreach (var doc in selectedAttachments)
                {
                    _logger.LogDebug("Processing attachment ID: {0}, File: {1}", doc.AttachmentID, doc.OrigFileName);

                    // Get attachment data from database
                    byte[] attachmentData = GetAttachmentDataFromDatabase(doc.AttachmentID, _logger);
                    string fileExtension = GetAttachmentFileType(doc.AttachmentID, _logger);

                    _logger.LogDebug("Retrieved {0} bytes for attachment ID: {1}, extension: {2}",
                        attachmentData?.Length ?? 0, doc.AttachmentID, fileExtension);

                    Document document = new Document
                    {
                        DocumentBase64 = Convert.ToBase64String(attachmentData),
                        Name = doc.OrigFileName,
                        FileExtension = string.IsNullOrEmpty(fileExtension) ?
                            System.IO.Path.GetExtension(doc.OrigFileName).TrimStart('.') : fileExtension,
                        DocumentId = docIndex.ToString() //doc.AttachmentID
                    };
                    documents.Add(document);

                    docIndex++;

                    _logger.LogDebug("Added document: {0} (ID: {1})", doc.OrigFileName, doc.AttachmentID);
                }

                // Create signature tabs for each signer with dynamic positioning
                List<Signer> dsSigners = new List<Signer>();

                const int SIGNATURE_START_X = 300;  // Starting X position
                const int SIGNATURE_START_Y = 600;  // Starting Y position for first signer
                const int SIGNATURE_SPACING = 75;   // Vertical spacing between signers

                _logger.LogDebug("Creating {0} signer(s) with signature spacing of {1} pixels", signers.Count, SIGNATURE_SPACING);

                for (int i = 0; i < signers.Count; i++)
                {
                    // Calculate X position for this signer (75 pixels apart)
                    int xPosition = SIGNATURE_START_X - (i * SIGNATURE_SPACING);
                    int yPosition = SIGNATURE_START_Y;

                    _logger.LogDebug("Signer {0} ({1}) will have signature at position X:{2}, Y:{3}",
                        i + 1, signers[i].Name, xPosition, yPosition);

                    // Create signature tabs for this specific signer on all documents
                    List<SignHere> signerSignHereTabs = new List<SignHere>();

                    for (int docIdx = 1; docIdx <= documents.Count; docIdx++)
                    {
                        var signHere = new SignHere
                        {
                            DocumentId = docIdx.ToString(),
                            PageNumber = "1",
                            XPosition = xPosition.ToString(),
                            YPosition = yPosition.ToString()
                        };

                        signerSignHereTabs.Add(signHere);
                    }

                    Tabs signerTabs = new Tabs
                    {
                        SignHereTabs = signerSignHereTabs,
                    };

                    Signer dsSigner = new Signer
                    {
                        Email = signers[i].Email,
                        Name = signers[i].Name,
                        RecipientId = (i + 1).ToString(),
                        RoutingOrder = "1",
                        Tabs = signerTabs
                    };

                    dsSigners.Add(dsSigner);
                    _logger.LogDebug("Added signer: {0} ({1}) with signature position Y:{2}", signers[i].Name, signers[i].Email, xPosition);
                }

                // Add carbon copy recipients
                List<CarbonCopy> dsCarbonCopies = new List<CarbonCopy>();
                int ccRecipientIdStart = signers.Count + 1;

                for (int i = 0; i < carbonCopies.Count; i++)
                {
                    CarbonCopy dsCarbonCopy = new CarbonCopy
                    {
                        Email = carbonCopies[i].Email,
                        Name = carbonCopies[i].Name,
                        RecipientId = (ccRecipientIdStart + i).ToString(),
                        RoutingOrder = "2" // Carbon copies receive after signers
                    };

                    dsCarbonCopies.Add(dsCarbonCopy);
                    _logger.LogDebug("Added carbon copy recipient: {0} ({1})", carbonCopies[i].Name, carbonCopies[i].Email);
                }

                EnvelopeDefinition env = new EnvelopeDefinition();
                env.EmailSubject = "Please sign this document set";

                // The order in the docs array determines the order in the envelope
                env.Documents = documents;

                // Add the recipients to the envelope object
                Recipients recipients = new Recipients
                {
                    Signers = dsSigners,
                    CarbonCopies = dsCarbonCopies.Count > 0 ? dsCarbonCopies : null
                };
                env.Recipients = recipients;

                // Request that the envelope be sent by setting |status| to "sent".
                // To request that the envelope be created as a draft, set to "created"
                env.Status = envStatus;

                _logger.LogInformation("Envelope definition created with {0} document(s), {1} signer(s), and {2} carbon copy recipient(s), status: {3}",
                    documents.Count, dsSigners.Count, dsCarbonCopies.Count, envStatus);
                _logger.LogDebug("Email subject: {0}", env.EmailSubject);
                _logger.LogMethodExit("MakeEnvelope");

                return env;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error making envelope", ex);
                throw;
            }
        }

        private static byte[] GetAttachmentDataFromDatabase(string attachmentId, Logger logger)
        {
            Logger _logger = logger;

            _logger.LogMethodEntry("GetAttachmentDataFromDatabase", attachmentId);

            string query = "SELECT TOP 100 AttachmentData, AttachmentID, AttachmentFileType " +
                          "FROM [VPAttachments].[dbo].[bHQAF] WHERE AttachmentID = @AttachmentID";

            try
            {
                _logger.LogDebug("Connecting to AttachmentDB");
                using (SqlConnection connection = new SqlConnection(AttachmentDBConnection))
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@AttachmentID", attachmentId);
                    connection.Open();

                    _logger.LogDebug("Executing query for attachment data: {0}", attachmentId);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            if (reader["AttachmentData"] != DBNull.Value)
                            {
                                byte[] data = (byte[])reader["AttachmentData"];
                                _logger.LogInformation("Retrieved {0} bytes for attachment ID: {1}", data.Length, attachmentId);
                                _logger.LogMethodExit("GetAttachmentDataFromDatabase", data.Length);
                                return data;
                            }
                            else
                            {
                                _logger.LogError("AttachmentData is null for AttachmentID: {0}", attachmentId);
                                throw new Exception($"AttachmentData is null for AttachmentID: {attachmentId}");
                            }
                        }
                        else
                        {
                            _logger.LogError("No attachment found with AttachmentID: {0}", attachmentId);
                            throw new Exception($"No attachment found with AttachmentID: {attachmentId}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving attachment data for ID: {attachmentId}", ex);
                throw;
            }
        }

        private static string GetAttachmentFileType(string attachmentId, Logger logger)
        {
            Logger _logger = logger;

            _logger.LogMethodEntry("GetAttachmentFileType", attachmentId);

            string query = "SELECT TOP 1 AttachmentFileType " +
                          "FROM [VPAttachments].[dbo].[bHQAF] WHERE AttachmentID = @AttachmentID";

            try
            {
                using (SqlConnection connection = new SqlConnection(AttachmentDBConnection))
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@AttachmentID", attachmentId);
                    connection.Open();

                    var result = command.ExecuteScalar();
                    string fileType = result?.ToString()?.TrimStart('.') ?? string.Empty;

                    _logger.LogDebug("Retrieved file type '{0}' for attachment ID: {1}", fileType, attachmentId);
                    _logger.LogMethodExit("GetAttachmentFileType", fileType);

                    return fileType;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving file type for attachment ID: {attachmentId}", ex);
                throw;
            }
        }

        public static byte[] Document1(string signerEmail, string signerName, string ccEmail, string ccName)
        {
            return Encoding.UTF8.GetBytes(
            " <!DOCTYPE html>\n" +
                "    <html>\n" +
                "        <head>\n" +
                "          <meta charset=\"UTF-8\">\n" +
                "        </head>\n" +
                "        <body style=\"font-family:sans-serif;margin-left:2em;\">\n" +
                "        <h1 style=\"font-family: 'Trebuchet MS', Helvetica, sans-serif;\n" +
                "            color: darkblue;margin-bottom: 0;\">World Wide Corp</h1>\n" +
                "        <h2 style=\"font-family: 'Trebuchet MS', Helvetica, sans-serif;\n" +
                "          margin-top: 0px;margin-bottom: 3.5em;font-size: 1em;\n" +
                "          color: darkblue;\">Order Processing Division</h2>\n" +
                "        <h4>Ordered by " + signerName + "</h4>\n" +
                "        <p style=\"margin-top:0em; margin-bottom:0em;\">Email: " + signerEmail + "</p>\n" +
                "        <p style=\"margin-top:0em; margin-bottom:0em;\">Copy to: " + ccName + ", " + ccEmail + "</p>\n" +
                "        <p style=\"margin-top:3em;\">\n" +
                "  Candy bonbon pastry jujubes lollipop wafer biscuit biscuit. Topping brownie sesame snaps sweet roll pie. Croissant danish biscuit soufflé caramels jujubes jelly. Dragée danish caramels lemon drops dragée. Gummi bears cupcake biscuit tiramisu sugar plum pastry. Dragée gummies applicake pudding liquorice. Donut jujubes oat cake jelly-o. Dessert bear claw chocolate cake gummies lollipop sugar plum ice cream gummies cheesecake.\n" +
                "        </p>\n" +
                "        <!-- Note the anchor tag for the signature field is in white. -->\n" +
                "        <h3 style=\"margin-top:3em;\">Agreed: <span style=\"color:white;\">**signature_1**/</span></h3>\n" +
                "        </body>\n" +
                "    </html>");
        }
    }
}
