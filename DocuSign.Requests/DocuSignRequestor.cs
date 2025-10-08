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

        public DocuSignRequestor(UserInputs inputs)
        {
            _userInputs = inputs;

            try
            {
                accessToken = JwtAuth.AuthenticateWithJwt("ESignature", _userInputs.DocuSignClientId,
                                                        _userInputs.DocuSignImpersonatedUserID,
                                                            _userInputs.DocuSignAuthServer,
                                                            DsHelper.ReadFileContent(_userInputs.DocuSignPrivateKeyFile));
            }
            catch (ApiException apiExp)
            {
                // Consent for impersonation must be obtained to use JWT Grant
                if (apiExp.Message.Contains("consent_required"))
                {
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
                    Environment.Exit(-1);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error during authentication: " + ex.Message);
                Environment.Exit(-1);
            }
            finally { }
        }

        public string SendEnvelope(DocuSignRequestDto docuSignRequest)
        {
            string signerEmail = "greddy@src1s.com";
            string signerName = docuSignRequest.RequestId;

            var docuSignClient = new DocuSignClient();
            docuSignClient.SetOAuthBasePath(_userInputs.DocuSignAuthServer);
            OAuth.UserInfo userInfo = docuSignClient.GetUserInfo(accessToken.access_token);
            Account acct = userInfo.Accounts.FirstOrDefault();

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
            }

            string docDocx = Path.Combine(@"..", "..", "..", "..", "launcher-csharp", "World_Wide_Corp_salary.docx");
            string docPdf = Path.Combine(@"..", "..", "..", "..", "launcher-csharp", "World_Wide_Corp_lorem.pdf");
            Console.WriteLine("");
            string envelopeId = SigningViaEmail.SendEnvelopeViaEmail(signerEmail, signerName, "", "", accessToken.access_token, acct.BaseUri + "/restapi", acct.AccountId, docDocx, docPdf, "sent");
            Console.WriteLine("");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Successfully sent envelope with envelopeId {envelopeId}");
            Console.WriteLine("");
            Console.WriteLine("");
            Console.ForegroundColor = ConsoleColor.White;

            return envelopeId;
        }

        public static Envelope GetEnvelopeStatus(string accessToken,
                                        string accountId, string envelopeId)
        {
            // Step 1: Initialize DocuSign client
            var apiClient = new ApiClient("https://demo.docusign.net/restapi");
            apiClient.Configuration.DefaultHeader.Add("Authorization", "Bearer " + accessToken);

            // Step 2: Create Envelopes API instance
            var envelopesApi = new EnvelopesApi(apiClient);

            // Step 3: Get envelope details
            Envelope envelope = envelopesApi.GetEnvelope(accountId, envelopeId);

            // Step 4: Return envelope info
            return envelope;
        }

        public async static void DownloadCombinedPdf(string accessToken, string accountId,
                            string envelopeId, string outputFilePath)
        {
            // Step 1: Initialize API client
            var apiClient = new ApiClient("https://demo.docusign.net/restapi"); // use "https://www.docusign.net/restapi" for production
            apiClient.Configuration.DefaultHeader.Add("Authorization", "Bearer " + accessToken);

            // Step 2: Create EnvelopesApi instance
            var envelopesApi = new EnvelopesApi(apiClient);

            // Step 3: Download combined document (all PDFs merged + certificate)
            using (var stream = envelopesApi.GetDocument(accountId, envelopeId, "combined"))
            {
                using (var fileStream = File.Create(outputFilePath))
                {
                    stream.CopyTo(fileStream);
                }
            }

            Console.WriteLine($"✅ Combined PDF downloaded successfully to: {outputFilePath}");

            //string baseUrl = $"https://demo.docusign.net/restapi/v2.1/accounts/{accountId}";
            //string documentsCombinedUri = $"/envelopes/{envelopeId}/documents/combined";

            //var client = new HttpClient();
            //client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            //var response = await client.GetAsync(baseUrl + documentsCombinedUri);
            //response.EnsureSuccessStatusCode();

            //using (var stream = await response.Content.ReadAsStreamAsync())
            //using (var fileStream = File.Create(outputFilePath))
            //{
            //    await stream.CopyToAsync(fileStream);
            //}

            //Console.WriteLine("Downloaded combined document successfully!");
        }
    }
}
