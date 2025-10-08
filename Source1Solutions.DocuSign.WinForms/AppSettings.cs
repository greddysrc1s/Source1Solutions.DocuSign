using Microsoft.Extensions.Configuration;
using System.IO;

namespace Source1Solutions.DocuSign.WinForms
{
    public static class AppSettings
    {
        private static IConfiguration? _configuration;

        public static IConfiguration Configuration
        {
            get
            {
                if (_configuration == null)
                {
                    var builder = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    
                    _configuration = builder.Build();
                }
                return _configuration;
            }
        }

        public static string GetConnectionString()
        {
            return Configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
        }

        public static string GetAttachmentDBConnectionString()
        {
            return Configuration.GetConnectionString("AttachmentDBConnection") ?? string.Empty;
        }

        public static string GetDocuSignClientId()
        {
            return Configuration["DocuSign:ClientId"] ?? string.Empty;
        }

        public static string GetDocuSignAuthServer()
        {
            return Configuration["DocuSign:AuthServer"] ?? string.Empty;
        }

        public static string GetDocuSignImpersonatedUserID()
        {
            return Configuration["DocuSign:ImpersonatedUserID"] ?? string.Empty;
        }

        public static string GetDocuSignPrivateKeyFile()
        {
            return Configuration["DocuSign:PrivateKeyFile"] ?? string.Empty;
        }

        public static string DocuSignAccountID()
        {
            return Configuration["DocuSign:AccountID"] ?? string.Empty;
        }
    }
}
