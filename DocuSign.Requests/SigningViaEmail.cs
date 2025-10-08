using System;
using System.Collections.Generic;
using System.Text;
using DocuSign.eSign.Api;
using DocuSign.eSign.Client;
using DocuSign.eSign.Model;
using Microsoft.Data.SqlClient;

namespace DocuSign.Requests
{
    public static class SigningViaEmail
    {
        private static string AttachmentDBConnection = "Server=WAP-sql.viewpointdata.cloud,4316;Database=VPAttachments;User Id=ReportBuilder;Password=SourceOne@20230816;";

        public static string SendEnvelopeViaEmail(string accessToken,
                                                    string basePath,
                                                    string accountId,
                                                    List<SignerDto> signers,
                                                    List<AttachmentDto> selectedAttachments,
                                                    string envStatus)
        {
            try
            {
                EnvelopeDefinition env = MakeEnvelope(signers, selectedAttachments, envStatus);

                var docuSignClient = new DocuSignClient(basePath);
                docuSignClient.Configuration.DefaultHeader.Add("Authorization", "Bearer " + accessToken);

                EnvelopesApi envelopesApi = new EnvelopesApi(docuSignClient);
                EnvelopeSummary results = envelopesApi.CreateEnvelope(accountId, env);
                return results.EnvelopeId;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static EnvelopeDefinition MakeEnvelope(List<SignerDto> signers,
                                                    List<AttachmentDto> selectedAttachments,
                                                    string envStatus)
        {
            SignHere signHere = new SignHere
            {
                AnchorString = "/sn1/",
                AnchorUnits = "pixels",
                AnchorYOffset = "10",
                AnchorXOffset = "20",
            };

            List<Signer> dsSigners = new List<Signer>();
            foreach (var signer in signers)
            {
                Signer dsSigner = new Signer
                {
                    Email = signer.Email,
                    Name = signer.Name,
                    RecipientId = signer.SignerOrder.ToString(),
                    RoutingOrder = signer.SignerOrder.ToString()
                };

                // Tabs are set per recipient / signer
                Tabs signer1Tabs = new Tabs
                {
                    SignHereTabs = new List<SignHere> { signHere },
                };
                dsSigner.Tabs = signer1Tabs;

                dsSigners.Add(dsSigner);
            }

            List<Document> documents = new List<Document>();

            int docIndex = 1;

            foreach (var doc in selectedAttachments)
            {
                // Get attachment data from database
                byte[] attachmentData = GetAttachmentDataFromDatabase(doc.AttachmentID);
                string fileExtension = GetAttachmentFileType(doc.AttachmentID);

                Document document = new Document
                {
                    DocumentBase64 = Convert.ToBase64String(attachmentData),
                    Name = doc.OrigFileName,
                    FileExtension = string.IsNullOrEmpty(fileExtension) ?
                        System.IO.Path.GetExtension(doc.OrigFileName).TrimStart('.') : fileExtension,
                    DocumentId = docIndex.ToString()
                };
                documents.Add(document);
            }

            EnvelopeDefinition env = new EnvelopeDefinition();
            env.EmailSubject = "Please sign this document set";

            // The order in the docs array determines the order in the envelope
            env.Documents = documents;

            // Add the recipients to the envelope object
            Recipients recipients = new Recipients
            {
                Signers = dsSigners
            };
            env.Recipients = recipients;

            // Request that the envelope be sent by setting |status| to "sent".
            // To request that the envelope be created as a draft, set to "created"
            env.Status = envStatus;

            return env;
        }

        private static byte[] GetAttachmentDataFromDatabase(string attachmentId)
        {
            string query = "SELECT AttachmentData, AttachmentID, AttachmentFileType " +
                          "FROM [VPAttachments].[dbo].[bHQAF] WHERE AttachmentID = @AttachmentID";

            using (SqlConnection connection = new SqlConnection(AttachmentDBConnection))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@AttachmentID", attachmentId);
                connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        if (reader["AttachmentData"] != DBNull.Value)
                        {
                            return (byte[])reader["AttachmentData"];
                        }
                        else
                        {
                            throw new Exception($"AttachmentData is null for AttachmentID: {attachmentId}");
                        }
                    }
                    else
                    {
                        throw new Exception($"No attachment found with AttachmentID: {attachmentId}");
                    }
                }
            }
        }

        private static string GetAttachmentFileType(string attachmentId)
        {
            string query = "SELECT TOP 1 AttachmentFileType " +
                          "FROM [VPAttachments].[dbo].[bHQAF] WHERE AttachmentID = @AttachmentID";

            using (SqlConnection connection = new SqlConnection(AttachmentDBConnection))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@AttachmentID", attachmentId);
                connection.Open();

                var result = command.ExecuteScalar();
                return result?.ToString()?.TrimStart('.') ?? string.Empty;
            }
        }

        public static byte[] Document1(string signerEmail, string signerName, string ccEmail, string ccName)
        {
            // Data for this method
            // signerEmail
            // signerName
            // ccEmail
            // ccName
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
