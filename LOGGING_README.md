# DocuSign Logging Implementation

## Overview
Comprehensive logging has been implemented across all DocuSign applications to capture detailed execution information, errors, and debugging data.

## Logging Configuration

### appsettings.json
Each application has logging configuration in `appsettings.json`:

```json
"Logging": {
  "LogFilePath": "C:\\Logs\\DocuSign\\[AppName]\\",
  "LogFileName": "DocuSign_[AppName]_{date}.log",
  "LogLevel": "Information",
  "RetentionDays": 30
}
```

### Configuration Properties

| Property | Description | Example |
|----------|-------------|---------|
| `LogFilePath` | Directory where log files are stored | `C:\Logs\DocuSign\WinForms\` |
| `LogFileName` | Log file name pattern. `{date}` is replaced with current date (yyyyMMdd) | `DocuSign_WinForms_20240115.log` |
| `LogLevel` | Minimum log level (Debug, Information, Warning, Error) | `Information` |
| `RetentionDays` | Number of days to keep old log files | `30` |

## Log Levels

| Level | Usage | Example |
|-------|-------|---------|
| **DEBUG** | Detailed diagnostic information | Method entry/exit, parameter values |
| **INFO** | General informational messages | Process start, success messages, counts |
| **WARN** | Warning messages for potential issues | Validation failures, missing optional data |
| **ERROR** | Error messages with exception details | Database errors, API failures, exceptions |

## Logger Methods

### Basic Logging
```csharp
_logger.LogInformation("Message with {0} parameters", count);
_logger.LogWarning("Warning: {0}", warningMessage);
_logger.LogError("Error occurred: {0}", errorMessage);
_logger.LogError("Error with exception", exception);
_logger.LogDebug("Debug info: {0}", debugData);
```

### Method Entry/Exit Tracking
```csharp
_logger.LogMethodEntry("MethodName", param1, param2);
// Method logic here
_logger.LogMethodExit("MethodName", returnValue);
```

## Log File Format

```
2024-01-15 14:30:45.123 [INFO] === DocuSign Sync Process Starting ===
2024-01-15 14:30:45.124 [INFO] Command line arguments: component=JCCM contractID=12345
2024-01-15 14:30:45.125 [DEBUG] Argument parsed: component = JCCM
2024-01-15 14:30:45.126 [DEBUG] Argument parsed: contractID = 12345
2024-01-15 14:30:45.130 [INFO] UserInputs initialized successfully
2024-01-15 14:30:45.135 [DEBUG] Entering method: Sync
2024-01-15 14:30:45.140 [INFO] DocuSignRequestor initialized
2024-01-15 14:30:45.145 [INFO] Found 3 pending envelope(s)
2024-01-15 14:30:45.150 [INFO] Processing envelope ID: ENV123456
2024-01-15 14:30:45.200 [INFO] Envelope status: completed for envelope ID: ENV123456
2024-01-15 14:30:45.250 [INFO] Successfully downloaded 524288 bytes for envelope ID: ENV123456
2024-01-15 14:30:45.300 [INFO] Updated status to 'completed' for envelope ID: ENV123456 (1 row(s) affected)
2024-01-15 14:30:45.350 [ERROR] Error processing envelope ID ENV789012
Exception: SqlException
Message: Connection timeout
StackTrace: at System.Data.SqlClient...
2024-01-15 14:30:45.400 [INFO] Sync completed. Success: 2, Errors: 1
```

## Applications with Logging

### 1. Source1Solutions.DocuSign.WinForms
- **Log Path**: `C:\Logs\DocuSign\WinForms\`
- **Log File**: `DocuSign_WinForms_{date}.log`
- **Logged Events**:
  - Application startup
  - Form initialization
  - Argument parsing
  - Signer additions
  - Attachment loading
  - Validation (success/failure)
  - Database operations
  - DocuSign API calls
  - Errors and exceptions

### 2. Source1Solutions.DocuSign.Sync
- **Log Path**: `C:\Logs\DocuSign\Sync\`
- **Log File**: `DocuSign_Sync_{date}.log`
- **Logged Events**:
  - Sync process start/end
  - Pending envelope retrieval
  - Envelope status checks
  - PDF downloads
  - Database updates
  - Success/error counts
  - All exceptions

### 3. DocuSign.Requests (Library)
- **Log Path**: `C:\Logs\DocuSign\Requests\`
- **Log File**: `DocuSign_Requests_{date}.log`
- **Logged Events**:
  - Envelope creation
  - Signer additions
  - Document processing
  - Database attachment retrieval
  - API calls to DocuSign
  - All errors and exceptions

## Log Retention

Old log files are automatically deleted based on the `RetentionDays` setting. The cleanup runs when each application starts:

```csharp
_logger.CleanOldLogs(AppSettings.GetLogRetentionDays());
```

## Accessing Logs

### Via File System
Navigate to the configured log path:
- WinForms: `C:\Logs\DocuSign\WinForms\`
- Sync: `C:\Logs\DocuSign\Sync\`
- Requests: `C:\Logs\DocuSign\Requests\`

### Via Console
All log entries are also written to the console output for real-time monitoring during development.

## Troubleshooting

### Common Issues

1. **Logs not being created**
   - Check if the log directory exists and has write permissions
   - Verify `appsettings.json` is being copied to output directory
   - Check console output for logging errors

2. **Log files growing too large**
   - Reduce `RetentionDays` setting
   - Change `LogLevel` from `Debug` to `Information`
   - Implement log rotation (currently daily by design)

3. **Performance impact**
   - Debug logging has minimal performance impact
   - File I/O is thread-safe with locks
   - Consider using async logging for high-volume scenarios

## Best Practices

1. **Always log method entry/exit for critical operations**
   ```csharp
   _logger.LogMethodEntry("SendDocuments", documentCount);
   // logic
   _logger.LogMethodExit("SendDocuments", envelopeId);
   ```

2. **Log all exceptions with context**
   ```csharp
   catch (Exception ex)
   {
       _logger.LogError($"Failed to process envelope {envelopeId}", ex);
       throw;
   }
   ```

3. **Use appropriate log levels**
   - DEBUG: Diagnostic info, not needed in production
   - INFO: Important business events, should be concise
   - WARN: Recoverable issues, validation failures
   - ERROR: Exceptions and critical failures

4. **Include relevant context in log messages**
   ```csharp
   _logger.LogInformation("Processing envelope {0} for contract {1}", envelopeId, contractId);
   ```

5. **Sanitize sensitive data**
   - Don't log passwords, tokens, or PII
   - Mask email addresses if needed for privacy

## Configuration Examples

### Development Environment
```json
"Logging": {
  "LogFilePath": "C:\\Logs\\DocuSign\\Dev\\",
  "LogFileName": "DocuSign_Dev_{date}.log",
  "LogLevel": "Debug",
  "RetentionDays": 7
}
```

### Production Environment
```json
"Logging": {
  "LogFilePath": "D:\\Logs\\DocuSign\\Production\\",
  "LogFileName": "DocuSign_Prod_{date}.log",
  "LogLevel": "Information",
  "RetentionDays": 90
}
```

## Future Enhancements

Potential improvements for the logging system:

1. **Structured Logging**: Use JSON format for easier parsing
2. **Centralized Logging**: Send logs to a central server (e.g., Elasticsearch)
3. **Log Rotation**: Implement size-based rotation in addition to daily rotation
4. **Performance Metrics**: Add timing information for performance analysis
5. **Alert Integration**: Send critical errors to monitoring systems
6. **Async Logging**: Implement asynchronous logging for better performance
