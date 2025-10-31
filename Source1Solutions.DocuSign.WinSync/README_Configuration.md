# WinSync Project Configuration Setup

## Summary of Changes

This document describes the changes made to the `Source1Solutions.DocuSign.WinSync` project to add configuration management similar to the `Source1Solutions.DocuSign.Sync` and `Source1Solutions.DocuSign.WinForms` projects.

## Files Created/Modified

### 1. **appsettings.json** (New File)
**Location:** `Source1Solutions.DocuSign.WinSync\appsettings.json`

**Purpose:** Configuration file containing all application settings including:
- Database connection strings
- DocuSign API configuration
- Logging settings

**Key Sections:**
- `ConnectionStrings`: Database connection strings for main database and attachments
- `DocuSign`: DocuSign API credentials and settings
- `Logging`: Log file path, filename pattern, log level, and retention days

**Note:** The log file path is set to `C:\Logs\DocuSign\WinSync\` to distinguish it from other projects.

### 2. **AppSettings.cs** (New File)
**Location:** `Source1Solutions.DocuSign.WinSync\AppSettings.cs`

**Purpose:** Static helper class to read configuration values from appsettings.json

**Features:**
- Lazy-loaded IConfiguration instance
- Helper methods for all configuration values
- Default fallback values for optional settings
- Type-safe configuration access

**Available Methods:**
```csharp
AppSettings.GetConnectionString()
AppSettings.GetAttachmentDBConnectionString()
AppSettings.GetDocuSignClientId()
AppSettings.GetDocuSignAuthServer()
AppSettings.GetDocuSignImpersonatedUserID()
AppSettings.GetDocuSignAccountID()
AppSettings.GetDocuSignPrivateKeyFile()
AppSettings.GetDocuSignApiBaseUrl()
AppSettings.GetLogFilePath()
AppSettings.GetLogFileName()
AppSettings.GetLogLevel()
AppSettings.GetLogRetentionDays()
```

### 3. **Source1Solutions.DocuSign.WinSync.csproj** (Modified)
**Changes Made:**
- Added NuGet package references:
  - `Microsoft.Data.SqlClient` (v6.1.1) - For database access
  - `Microsoft.Extensions.Configuration` (v9.0.0) - For configuration support
  - `Microsoft.Extensions.Configuration.Json` (v9.0.0) - For JSON configuration files
  
- Added project reference:
  - `DocuSign.Requests` - For DocuSign integration and logging

- Added build configuration:
  - `appsettings.json` is copied to output directory with `PreserveNewest` setting

### 4. **Program.cs** (Modified)
**Changes Made:**
- Updated `Main` method signature to accept `string[] args` parameter
- Command line arguments are now passed to the `SyncForm` constructor
- Added try-catch block with user-friendly error message display
- Follows the same pattern as `Source1Solutions.DocuSign.WinForms`

**Before:**
```csharp
static void Main()
{
    ApplicationConfiguration.Initialize();
    Application.Run(new SyncForm());
}
```

**After:**
```csharp
static void Main(string[] args)
{
    try
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new SyncForm(args));
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Error starting application: {ex.Message}", 
            "Application Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        throw;
    }
}
```

### 5. **SyncForm.cs** (Modified)
**Changes Made:**
- Added constructor that accepts `string[] args` parameter
- Initialized `Logger` instance using AppSettings
- Added command line argument parsing into `dicArgs` dictionary
- Added connection string field using AppSettings
- Added comprehensive logging for application startup and lifecycle
- Added automatic log cleanup on startup

**Key Features:**
- Parses command line arguments in `key=value` format
- Case-insensitive argument dictionary
- Comprehensive logging of startup and operations
- Error handling with user-friendly messages

**Example Command Line Usage:**
```bash
Source1Solutions.DocuSign.WinSync.exe contractID=12345 companyID=ABC123 requestor=JohnDoe component=ContractModule
```

**Accessing Arguments:**
```csharp
string contractID = dicArgs.ContainsKey("contractID") ? dicArgs["contractID"] : string.Empty;
string companyID = dicArgs.ContainsKey("companyID") ? dicArgs["companyID"] : string.Empty;
string requestor = dicArgs.ContainsKey("requestor") ? dicArgs["requestor"] : Environment.UserName;
```

## Pattern Consistency

The implementation follows the exact same pattern as the other projects:

### Common Pattern Across Projects:
1. **appsettings.json** - Configuration storage
2. **AppSettings.cs** - Configuration access layer
3. **Logger integration** - Comprehensive logging
4. **Command line arguments** - Parse key=value pairs into dictionary
5. **Error handling** - Try-catch with user-friendly messages
6. **Log cleanup** - Automatic cleanup of old logs based on retention days

### Project-Specific Settings:
- **WinForms**: `LogFilePath` = `C:\Logs\DocuSign\WinForms\`
- **Sync**: `LogFilePath` = `C:\Logs\DocuSign\Sync\`
- **WinSync**: `LogFilePath` = `C:\Logs\DocuSign\WinSync\`

## Build Status
? **Build Successful** - All changes compile without errors

## Next Steps

1. **Configure Log Directory**: Ensure the log directory `C:\Logs\DocuSign\WinSync\` exists or has write permissions
2. **Implement Sync Logic**: Add the synchronization logic in the `SyncForm` class
3. **Test Command Line Arguments**: Test the application with various command line arguments
4. **Review Configuration**: Update appsettings.json with environment-specific values if needed

## Usage Example

```csharp
// In SyncForm.cs, you can now use:

// Access configuration
string connectionString = AppSettings.GetConnectionString();
string apiUrl = AppSettings.GetDocuSignApiBaseUrl();

// Access command line arguments
string contractID = dicArgs.ContainsKey("contractID") 
    ? dicArgs["contractID"] 
    : string.Empty;

// Use logger
_logger.LogInformation("Processing contract: {0}", contractID);
_logger.LogError("Error occurred", exception);
```

## Dependencies

The WinSync project now depends on:
- .NET 9.0
- Microsoft.Data.SqlClient
- Microsoft.Extensions.Configuration
- Microsoft.Extensions.Configuration.Json
- DocuSign.Requests (project reference for Logger and DTOs)
