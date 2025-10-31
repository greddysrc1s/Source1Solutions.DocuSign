using Microsoft.Extensions.Configuration;
using System.IO;

namespace Source1Solutions.DocuSign.WinSync
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
        
        public static string GetDocuSignAccountID()
        {
            return Configuration["DocuSign:AccountID"] ?? string.Empty;
        }

        public static string GetDocuSignPrivateKeyFile()
        {
            return Configuration["DocuSign:PrivateKeyFile"] ?? string.Empty;
        }

        public static string GetDocuSignApiBaseUrl()
        {
            return Configuration["DocuSign:ApiBaseUrl"] ?? "https://demo.docusign.net/restapi";
        }

        public static string GetLogFilePath()
        {
            return Configuration["Logging:LogFilePath"] ?? "C:\\Logs\\DocuSign\\";
        }

        public static string GetLogFileName()
        {
            return Configuration["Logging:LogFileName"] ?? "DocuSign_{date}.log";
        }

        public static string GetLogLevel()
        {
            return Configuration["Logging:LogLevel"] ?? "Information";
        }

        public static int GetLogRetentionDays()
        {
            return int.TryParse(Configuration["Logging:RetentionDays"], out int days) ? days : 30;
        }
    }
}
