# Logger Passing Implementation in DocuSign.Requests

## Overview
The DocuSign.Requests library now supports receiving a logger instance from calling applications, enabling unified logging across all components with consistent log paths and formats.

## Architecture

### Logger Flow
```
Source Application (WinForms/Sync)
    ? Creates Logger with app-specific config
    ? Creates UserInputs
    ?
DocuSignRequestor (receives logger)
    ? Passes logger to
    ?
SigningViaEmail static methods
    ? Passes logger to
    ?
GetAttachmentDataFromDatabase
GetAttachmentFileType
```

## Implementation Details

### 1. DocuSignRequestor Constructor

**Updated Signature:**
```csharp
public DocuSignRequestor(UserInputs inputs, Logger logger = null)
```

**Features:**
- Accepts optional logger parameter
- Falls back to default logger if not provided
- Stores logger for use in all methods
- Logs authentication process details

**Usage Example:**
```csharp
var logger = new Logger(
    AppSettings.GetLogFilePath(),
    AppSettings.GetLogFileName(),
    AppSettings.GetLogLevel()
);

var userInputs = new UserInputs() { /* ... */ };
var requestor = new DocuSignRequestor(userInputs, logger);
```

### 2. SigningViaEmail Methods

**Updated Signatures:**
```csharp
public static string SendEnvelopeViaEmail(
    string accessToken,  
    string basePath, 
    string accountId,
    List<SignerDto> signers,
    List<AttachmentDto> selectedAttachments,
    string envStatus,
    Logger logger = null)  // Added parameter

public static EnvelopeDefinition MakeEnvelope(
    List<SignerDto> signers,
    List<AttachmentDto> selectedAttachments,
    string envStatus,
    Logger logger = null)  // Added parameter

private static byte[] GetAttachmentDataFromDatabase(
    string attachmentId, 
    Logger logger = null)  // Added parameter

private static string GetAttachmentFileType(
    string attachmentId, 
    Logger logger = null)  // Added parameter
```

## Logged Information in DocuSign.Requests

### Authentication (DocuSignRequestor Constructor)
```
[INFO] Initializing DocuSignRequestor with ClientId: {clientId}
[DEBUG] Attempting JWT authentication
[DEBUG] Auth Server: {authServer}
[DEBUG] Impersonated User ID: {userId}
[INFO] JWT authentication successful. Token expires at: {expiresIn}
[ERROR] API Exception during authentication (if error occurs)
[WARN] Consent required for impersonation (if consent needed)
```

### Envelope Sending (SendEnvelope)
```
[DEBUG] Entering method: SendEnvelope
[INFO] Sending envelope for Request ID: {requestId}
[INFO] Number of signers: {count}, Number of attachments: {count}
[DEBUG] Getting user info from DocuSign
[INFO] Using account: {accountName} (ID: {accountId})
[DEBUG] Added signer: {name} ({email}) with order {order}
[DEBUG] Calling SigningViaEmail.SendEnvelopeViaEmail
[INFO] Envelope sent successfully with ID: {envelopeId}
```

### Envelope Creation (MakeEnvelope)
```
[DEBUG] Creating envelope with {count} signers and {count} attachments
[DEBUG] Added signer: {name} ({email}) with order {order}
[DEBUG] Processing attachment ID: {id}, File: {filename}
[DEBUG] Retrieved {bytes} bytes for attachment ID: {id}, extension: {ext}
[DEBUG] Added document: {filename} (ID: {id})
[INFO] Envelope definition created with {docCount} document(s) and {signerCount} signer(s)
[DEBUG] Email subject: {subject}
```

### Attachment Retrieval (GetAttachmentDataFromDatabase)
```
[DEBUG] Connecting to AttachmentDB
[DEBUG] Executing query for attachment data: {attachmentId}
[INFO] Retrieved {bytes} bytes for attachment ID: {attachmentId}
[ERROR] AttachmentData is null for AttachmentID: {id} (if null)
[ERROR] No attachment found with AttachmentID: {id} (if not found)
```

### Envelope Status (GetEnvelopeStatus)
```
[DEBUG] API Client initialized for envelope status check
[DEBUG] Retrieving envelope details for ID: {envelopeId}
[INFO] Envelope status retrieved: {status} for envelope ID: {id}
[DEBUG] Envelope created: {created}, sent: {sent}
```

### PDF Download (DownloadCombinedPdf)
```
[DEBUG] API Client initialized for PDF download
[INFO] Downloading combined PDF for envelope ID: {envelopeId}
[DEBUG] Output file path: {path}
[INFO] Successfully downloaded combined PDF. File size: {bytes} bytes
```

## Source Application Integration

### WinForms Application

```csharp
// In DocuSignForm constructor
_logger = new Logger(
    AppSettings.GetLogFilePath(),
    AppSettings.GetLogFileName(),
    AppSettings.GetLogLevel()
);

// When creating DocuSignRequestor
var userInputs = new UserInputs() { /* ... */ };
_logger.LogInformation("Creating DocuSignRequestor with logger");
DocuSignRequestor docuSignRequestor = new DocuSignRequestor(userInputs, _logger);

_logger.LogInformation("Sending envelope to DocuSign");
var envelopeID = docuSignRequestor.SendEnvelope(docuSignRequest);
_logger.LogInformation("Envelope sent successfully with ID: {0}", envelopeID);
```

**Log Output Location:** `C:\Logs\DocuSign\WinForms\DocuSign_WinForms_{date}.log`

### Sync Application

```csharp
// In SyncProcess constructor
_logger = new Logger(
    AppSettings.GetLogFilePath(),
    AppSettings.GetLogFileName(),
    AppSettings.GetLogLevel()
);

// When creating DocuSignRequestor
DocuSignRequestor docuSignRequestor = new(userInputs, _logger);
_logger.LogInformation("DocuSignRequestor initialized");
```

**Log Output Location:** `C:\Logs\DocuSign\Sync\DocuSign_Sync_{date}.log`

## Benefits

### 1. **Unified Logging**
- All DocuSign operations logged to same file as calling application
- Complete trace from UI action to API call
- Easier debugging with consolidated logs

### 2. **Centralized Configuration**
- Log path and level controlled by calling application
- No duplicate configuration in library
- Consistent log format across all layers

### 3. **Better Traceability**
- Can trace entire workflow in single log file
- Request ID flows through all layers
- Exception stack traces include full call chain

### 4. **Backward Compatibility**
- Logger parameter is optional (defaults if not provided)
- Existing code continues to work
- Gradual migration path

## Log Tracing Example

### Complete Flow in Single Log File

```
2024-01-15 14:30:45.123 [INFO] === DocuSign WinForms Application Starting ===
2024-01-15 14:30:45.124 [INFO] Command line arguments: component=JCCM,contractID=12345
2024-01-15 14:30:45.130 [DEBUG] Validating 2 signer(s)
2024-01-15 14:30:45.135 [INFO] Validation passed successfully
2024-01-15 14:30:45.140 [INFO] Created DocuSignRequestDto with 2 signer(s) and 3 attachment(s)
2024-01-15 14:30:45.145 [INFO] Creating DocuSignRequestor with logger
2024-01-15 14:30:45.150 [DEBUG] Entering method: DocuSignRequestor.Constructor
2024-01-15 14:30:45.155 [INFO] Initializing DocuSignRequestor with ClientId: 2e8a7d86...
2024-01-15 14:30:45.160 [DEBUG] Attempting JWT authentication
2024-01-15 14:30:45.165 [DEBUG] Auth Server: account-d.docusign.com
2024-01-15 14:30:45.400 [INFO] JWT authentication successful. Token expires at: 3600
2024-01-15 14:30:45.405 [INFO] Sending envelope to DocuSign
2024-01-15 14:30:45.410 [DEBUG] Entering method: SendEnvelope
2024-01-15 14:30:45.415 [INFO] Sending envelope for Request ID: user@company.com
2024-01-15 14:30:45.420 [INFO] Number of signers: 2, Number of attachments: 3
2024-01-15 14:30:45.425 [DEBUG] Getting user info from DocuSign
2024-01-15 14:30:45.500 [INFO] Using account: Demo Account (ID: 40579370)
2024-01-15 14:30:45.505 [DEBUG] Added signer: John Doe (john@example.com) with order 1
2024-01-15 14:30:45.510 [DEBUG] Added signer: Jane Smith (jane@example.com) with order 2
2024-01-15 14:30:45.515 [DEBUG] Calling SigningViaEmail.SendEnvelopeViaEmail
2024-01-15 14:30:45.520 [DEBUG] Creating envelope with 2 signers and 3 attachments
2024-01-15 14:30:45.525 [DEBUG] Processing attachment ID: 123, File: Contract.pdf
2024-01-15 14:30:45.530 [DEBUG] Connecting to AttachmentDB
2024-01-15 14:30:45.600 [INFO] Retrieved 524288 bytes for attachment ID: 123
2024-01-15 14:30:45.605 [DEBUG] Retrieved 524288 bytes for attachment ID: 123, extension: pdf
2024-01-15 14:30:45.610 [DEBUG] Added document: Contract.pdf (ID: 123)
2024-01-15 14:30:45.750 [INFO] Envelope definition created with 3 document(s) and 2 signer(s), status: sent
2024-01-15 14:30:45.900 [INFO] Envelope created successfully with ID: ENV-12345-67890
2024-01-15 14:30:45.905 [INFO] Envelope sent successfully with ID: ENV-12345-67890
2024-01-15 14:30:45.910 [DEBUG] Executing stored procedure: brptCreateDocuSignEntries_S1S
2024-01-15 14:30:45.950 [INFO] Stored procedure returned DocuSignID: 42
2024-01-15 14:30:45.955 [INFO] DocuSign request completed successfully
```

## Error Handling

All errors in DocuSign.Requests are logged before being thrown:

```csharp
try
{
    // Operation
}
catch (Exception ex)
{
    _logger.LogError("Descriptive error message", ex);
    throw; // Re-throw for caller to handle
}
```

This ensures:
- Full exception details in log
- Stack trace preserved
- Calling application can handle appropriately
- Log file has complete error context

## Testing

### Verify Logger Passing
1. Run WinForms application
2. Complete DocuSign operation
3. Check log file at `C:\Logs\DocuSign\WinForms\DocuSign_WinForms_{date}.log`
4. Verify entries from both WinForms AND DocuSign.Requests in same file

### Verify Sync Application
1. Run Sync application
2. Process envelope
3. Check log file at `C:\Logs\DocuSign\Sync\DocuSign_Sync_{date}.log`
4. Verify DocuSignRequestor logs appear in Sync log file

## Future Enhancements

1. **Correlation IDs**: Add correlation ID to track requests across systems
2. **Performance Metrics**: Log timing information for each operation
3. **Structured Logging**: Use JSON format for machine parsing
4. **Log Aggregation**: Send logs to central logging service
5. **Async Logging**: Implement async logging for better performance
