using Microsoft.Extensions.Configuration;
using System.IO;

namespace Source1Solutions.DocuSign.WinSync
{
    public static class AppSettings
    {
        private static readonly Lazy<IConfiguration> _lazyConfiguration = new Lazy<IConfiguration>(() =>
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            
            return builder.Build();
        });

        public static IConfiguration Configuration => _lazyConfiguration.Value;

        // Cache frequently accessed values
        private static string? _connectionString;
        private static string? _attachmentDBConnectionString;
        private static string? _docuSignClientId;
        private static string? _docuSignAuthServer;
        private static string? _docuSignImpersonatedUserID;
        private static string? _docuSignAccountID;
        private static string? _docuSignPrivateKeyFile;
        private static string? _docuSignApiBaseUrl;
        private static string? _logFilePath;
        private static string? _logFileName;
        private static string? _logLevel;
        private static int? _logRetentionDays;

        public static string GetConnectionString()
        {
            return _connectionString ??= Configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
        }
        
        public static string GetAttachmentDBConnectionString()
        {
            return _attachmentDBConnectionString ??= Configuration.GetConnectionString("AttachmentDBConnection") ?? string.Empty;
        }

        public static string GetDocuSignClientId()
        {
            return _docuSignClientId ??= Configuration["DocuSign:ClientId"] ?? string.Empty;
        }

        public static string GetDocuSignAuthServer()
        {
            return _docuSignAuthServer ??= Configuration["DocuSign:AuthServer"] ?? string.Empty;
        }

        public static string GetDocuSignImpersonatedUserID()
        {
            return _docuSignImpersonatedUserID ??= Configuration["DocuSign:ImpersonatedUserID"] ?? string.Empty;
        }
        
        public static string GetDocuSignAccountID()
        {
            return _docuSignAccountID ??= Configuration["DocuSign:AccountID"] ?? string.Empty;
        }

        public static string GetDocuSignPrivateKeyFile()
        {
            return _docuSignPrivateKeyFile ??= Configuration["DocuSign:PrivateKeyFile"] ?? string.Empty;
        }

        public static string GetDocuSignApiBaseUrl()
        {
            return _docuSignApiBaseUrl ??= Configuration["DocuSign:ApiBaseUrl"] ?? "https://demo.docusign.net/restapi";
        }

        public static string GetLogFilePath()
        {
            return _logFilePath ??= Configuration["Logging:LogFilePath"] ?? "C:\\Logs\\DocuSign\\";
        }

        public static string GetLogFileName()
        {
            return _logFileName ??= Configuration["Logging:LogFileName"] ?? "DocuSign_{date}.log";
        }

        public static string GetLogLevel()
        {
            return _logLevel ??= Configuration["Logging:LogLevel"] ?? "Information";
        }

        public static int GetLogRetentionDays()
        {
            return _logRetentionDays ??= int.TryParse(Configuration["Logging:RetentionDays"], out int days) ? days : 30;
        }
    }
}
