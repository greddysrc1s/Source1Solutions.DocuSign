using DocuSign.eSign.Api;
using DocuSign.eSign.Client;
using DocuSign.eSign.Client.Auth;
using DocuSign.eSign.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static DocuSign.eSign.Client.Auth.OAuth;
using static DocuSign.eSign.Client.Auth.OAuth.UserInfo;

namespace DocuSign.Requests
{
    public class DocuSignRequestor
    {
        UserInputs _userInputs = null;
        OAuthToken accessToken = null;
        string DevCenterPage = "https://www.docusign.com";
        private Logger _logger;

        public DocuSignRequestor(UserInputs inputs, Logger logger)
        {
            _userInputs = inputs;
            _logger = logger;

            _logger.LogMethodEntry("DocuSignRequestor.Constructor");
            _logger.LogInformation("Initializing DocuSignRequestor with ClientId: {0}", _userInputs.DocuSignClientId);

            try
            {
                _logger.LogDebug("Attempting JWT authentication");
                _logger.LogDebug("Auth Server: {0}", _userInputs.DocuSignAuthServer);
                _logger.LogDebug("Impersonated User ID: {0}", _userInputs.DocuSignImpersonatedUserID);
                
                accessToken = JwtAuth.AuthenticateWithJwt("ESignature", _userInputs.DocuSignClientId,
                                                        _userInputs.DocuSignImpersonatedUserID,
                                                            _userInputs.DocuSignAuthServer,
                                                            DsHelper.ReadFileContent(_userInputs.DocuSignPrivateKeyFile));
                
                _logger.LogInformation("JWT authentication successful. Token expires at: {0}", accessToken.expires_in);
                _logger.LogMethodExit("DocuSignRequestor.Constructor", "Success");
            }
            catch (ApiException apiExp)
            {
                _logger.LogError("API Exception during authentication", apiExp);
                
                // Consent for impersonation must be obtained to use JWT Grant
                if (apiExp.Message.Contains("consent_required"))
                {
                    _logger.LogWarning("Consent required for impersonation");
                    
                    // Caret needed for escaping & in windows URL
                    string caret = "";
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        caret = "^";
                    }

                    // build a URL to provide consent for this Integration Key and this userId
                    string url = $"https://{_userInputs.DocuSignAuthServer}/oauth/auth?response_type=code&scope=impersonation%20signature" +
                                                $"&client_id={_userInputs.DocuSignClientId}&redirect_uri={DevCenterPage}";

                    string consentRequiredMessage = $"Consent is required - launching browser (URL is {url})";
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        consentRequiredMessage = consentRequiredMessage.Replace(caret, "");
                    }

                    _logger.LogInformation(consentRequiredMessage);
                    Console.WriteLine(consentRequiredMessage);

                    // Start new browser window for login and consent to this app by DocuSign user
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = false });
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        Process.Start("xdg-open", url);
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    {
                        Process.Start("open", url);
                    }

                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Unable to send envelope; Exiting. Please rerun the console app once consent was provided");
                    Console.ForegroundColor = ConsoleColor.White;
                    
                    _logger.LogError("Application exiting due to missing consent");
                    Environment.Exit(-1);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Fatal error during authentication", ex);
                Console.WriteLine("Error during authentication: " + ex.Message);
                Environment.Exit(-1);
            }
        }

        public string SendEnvelope(DocuSignRequestDto docuSignRequest)
        {
            _logger.LogMethodEntry("SendEnvelope");
            _logger.LogInformation("Sending envelope for Request ID: {0}", docuSignRequest.RequestId);
            _logger.LogInformation("Number of signers: {0}, Number of attachments: {1}", 
                docuSignRequest.Signers.Count, docuSignRequest.SelectedAttachments.Count);

            try
            {
                string signerEmail = "greddy@src1s.com";
                string signerName = docuSignRequest.RequestId;

                var docuSignClient = new DocuSignClient();
                docuSignClient.SetOAuthBasePath(_userInputs.DocuSignAuthServer);
                
                _logger.LogDebug("Getting user info from DocuSign");
                OAuth.UserInfo userInfo = docuSignClient.GetUserInfo(accessToken.access_token);
                Account acct = userInfo.Accounts.FirstOrDefault();
                
                _logger.LogInformation("Using account: {0} (ID: {1})", acct.AccountName, acct.AccountId);

                List<Signer> signers = new List<Signer>();

                foreach (var inputSigners in docuSignRequest.Signers)
                {
                    signers.Add(new Signer
                    {
                        Email = inputSigners.Email,
                        Name = inputSigners.Name,
                        RecipientId = inputSigners.SignerOrder.ToString(),
                        RoutingOrder = inputSigners.SignerOrder.ToString(),
                    });
                    _logger.LogDebug("Added signer: {0} ({1}) with order {2}", 
                        inputSigners.Name, inputSigners.Email, inputSigners.SignerOrder);
                }

                _logger.LogDebug("Calling SigningViaEmail.SendEnvelopeViaEmail");
                string envelopeId = SigningViaEmail.SendEnvelopeViaEmail(
                    accessToken.access_token, 
                    acct.BaseUri + "/restapi", 
                    acct.AccountId,
                    docuSignRequest.Signers,
                    docuSignRequest.SelectedAttachments, 
                    "sent",
                    _logger);
                
                _logger.LogInformation("Envelope sent successfully with ID: {0}", envelopeId);
                _logger.LogMethodExit("SendEnvelope", envelopeId);
                
                return envelopeId;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error sending envelope", ex);
                throw;
            }
        }

        public Envelope GetEnvelopeStatus(string accountId, string envelopeId)
        {
            _logger.LogMethodEntry("GetEnvelopeStatus", accountId, envelopeId);
            
            try
            {
                // Step 1: Initialize DocuSign client
                var apiClient = new ApiClient("https://demo.docusign.net/restapi");
                apiClient.Configuration.DefaultHeader.Add("Authorization", "Bearer " + accessToken.access_token);
                
                _logger.LogDebug("API Client initialized for envelope status check");

                // Step 2: Create Envelopes API instance
                var envelopesApi = new EnvelopesApi(apiClient);

                // Step 3: Get envelope details
                _logger.LogDebug("Retrieving envelope details for ID: {0}", envelopeId);
                Envelope envelope = envelopesApi.GetEnvelope(accountId, envelopeId);
                
                _logger.LogInformation("Envelope status retrieved: {0} for envelope ID: {1}", envelope.Status, envelopeId);
                _logger.LogDebug("Envelope created: {0}, sent: {1}", envelope.CreatedDateTime, envelope.SentDateTime);
                _logger.LogMethodExit("GetEnvelopeStatus", envelope.Status);

                // Step 4: Return envelope info
                return envelope;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting envelope status for ID: {envelopeId}", ex);
                throw;
            }
        }

        public string DownloadCombinedPdf(string accountId, string envelopeId, string outputFilePath)
        {
            _logger.LogMethodEntry("DownloadCombinedPdf", accountId, envelopeId, outputFilePath);
            
            try
            {
                // Step 1: Initialize API client
                var apiClient = new ApiClient("https://demo.docusign.net/restapi");
                apiClient.Configuration.DefaultHeader.Add("Authorization", "Bearer " + accessToken.access_token);
                
                _logger.LogDebug("API Client initialized for PDF download");

                // Step 2: Create EnvelopesApi instance
                var envelopesApi = new EnvelopesApi(apiClient);

                // Step 3: Download combined document (all PDFs merged + certificate)
                _logger.LogInformation("Downloading combined PDF for envelope ID: {0}", envelopeId);
                _logger.LogDebug("Output file path: {0}", outputFilePath);
                
                using (var stream = envelopesApi.GetDocument(accountId, envelopeId, "combined"))
                {
                    using (var fileStream = File.Create(outputFilePath))
                    {
                        stream.CopyTo(fileStream);
                    }
                }
                
                FileInfo fileInfo = new FileInfo(outputFilePath);
                _logger.LogInformation("Successfully downloaded combined PDF. File size: {0} bytes", fileInfo.Length);
                _logger.LogMethodExit("DownloadCombinedPdf", outputFilePath);

                return outputFilePath;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error downloading combined PDF for envelope ID: {envelopeId}", ex);
                throw;
            }
        }
    }
}
