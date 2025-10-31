# Source1Solutions.DocuSign Application Overview

## ?? Table of Contents
1. [Application Architecture](#application-architecture)
2. [Projects Overview](#projects-overview)
3. [Core Components](#core-components)
4. [Data Flow](#data-flow)
5. [Key Features](#key-features)
6. [Configuration](#configuration)
7. [Database Integration](#database-integration)
8. [Security](#security)
9. [Logging](#logging)
10. [Deployment](#deployment)

---

## ??? Application Architecture

### **Technology Stack**
- **.NET Version**: .NET 9
- **C# Version**: 13.0
- **UI Framework**: Windows Forms (WinForms)
- **DocuSign SDK**: DocuSign.eSign.dll v8.4.0
- **Database**: Microsoft SQL Server
- **Configuration**: Microsoft.Extensions.Configuration (JSON-based)
- **Logging**: Custom file-based logging system

### **Architecture Pattern**
- **3-Tier Architecture**:
  1. **Presentation Layer**: WinForms UI
  2. **Business Logic Layer**: DocuSign.Requests library
  3. **Data Access Layer**: SQL Server database

---

## ?? Projects Overview

### **1. DocuSign.Requests** (Core Library)
**Type**: Class Library  
**Purpose**: Shared business logic and DocuSign API integration

**Key Files**:
- `DocuSignRequestor.cs` - Main DocuSign API orchestrator
- `SigningViaEmail.cs` - Envelope creation and sending
- `JwtAuth.cs` - JWT authentication handler
- `Logger.cs` - Custom logging implementation
- `UserInputs.cs` - Configuration model
- `DocuSignRequestDto.cs` - Data transfer objects
- `DsHelper.cs` - Utility functions

**Responsibilities**:
- ? DocuSign API authentication (JWT)
- ? Envelope creation and management
- ? Document handling
- ? Signer management
- ? PDF download and streaming
- ? Centralized logging

---

### **2. Source1Solutions.DocuSign.WinForms** (UI Application)
**Type**: Windows Forms Application  
**Purpose**: User interface for sending documents for signature

**Key Files**:
- `Program.cs` - Application entry point
- `DocuSignForm.cs` - Main form logic
- `DocuSignForm.Designer.cs` - UI designer code
- `AppSettings.cs` - Configuration accessor
- `appsettings.json` - Configuration file

**Features**:
- ? **Dynamic Signer Management**:
  - Add multiple signers dynamically
  - Remove signers (minimum 1 required)
  - Email and name validation
  - Routing order management

- ? **Attachment Management**:
  - Browse and select attachments from database
  - Multiple attachment selection via checkboxes
  - Attachment metadata display

- ? **Validation**:
  - Email format validation
  - Required field validation
  - Minimum attachment validation

- ? **Database Integration**:
  - Store DocuSign request metadata
  - Track envelope status
  - Link to contract/company data

**User Workflow**:
1. Application launches with command-line arguments (component, contractID, companyID)
2. User enters signer information (email, name)
3. User can add/remove signers
4. User selects attachments from grid
5. User clicks "Send Documents"
6. System validates inputs
7. System creates DocuSign envelope
8. System saves metadata to database
9. User receives success confirmation with envelope ID

---

### **3. Source1Solutions.DocuSign.Sync** (Background Service)
**Type**: Console Application  
**Purpose**: Automated synchronization of completed envelopes

**Key Files**:
- `Program.cs` - Application entry point
- `SyncProcess.cs` - Sync orchestration logic
- `AppSettings.cs` - Configuration accessor
- `appsettings.json` - Configuration file

**Features**:
- ? **Automated Polling**:
  - Query pending envelopes from database
  - Check envelope status with DocuSign
  - Download completed PDFs

- ? **Database Synchronization**:
  - Stream PDFs to database (no file system)
  - Update envelope status
  - Link to contract attachments

- ? **Error Handling**:
  - Per-envelope error isolation
  - Success/error count tracking
  - Comprehensive error logging

**Sync Workflow**:
1. Application starts with command-line arguments
2. Retrieves pending envelopes from database
3. For each envelope:
   - Check status with DocuSign API
   - If completed:
     - Download combined PDF as stream
     - Save to database (bHQAT/bHQAF tables)
     - Update status to "Completed"
4. Log success/error counts
5. Exit

---

### **4. Source1Solutions.DocuSign** (Unknown/Legacy)
**Type**: Project placeholder
**Purpose**: Not actively used in current implementation

---

## ?? Core Components

### **Authentication (JWT)**
```
JwtAuth.AuthenticateWithJwt()
    ?
DocuSign OAuth Server
    ?
Access Token (expires in 1 hour)
    ?
Used for all API calls
```

**Configuration Required**:
- ClientId (Integration Key)
- ImpersonatedUserID
- AuthServer (account-d.docusign.com for demo)
- PrivateKeyFile (.key file path)
- AccountID

### **Envelope Creation Flow**
```
WinForms UI
    ?
DocuSignRequestor.SendEnvelope()
    ?
SigningViaEmail.MakeEnvelope()
    ?
    ?? Create Signers
    ?? Retrieve Attachments from DB
    ?? Convert to Base64
    ?? Add to Envelope
    ?
SigningViaEmail.SendEnvelopeViaEmail()
    ?
DocuSign API
    ?
EnvelopeID returned
```

### **PDF Download & Storage Flow**
```
Sync Process
    ?
Query Pending Envelopes
    ?
Check Status (GetEnvelopeStatus)
    ?
If Completed:
    ?? DownloadCombinedPdfAsBytes()
    ?? Stream to memory
    ?? SavePdfToDatabase()
        ?
        Stored Procedure: brptUpdateDocuSignFileToDB_S1S
        ?
        ?? Insert into bHQAT (metadata)
        ?? Insert into bHQAF (binary data)
        ?? Update udtDocuSignTracking_S1S (status)
```

---

## ?? Data Flow

### **WinForms Application Flow**
```mermaid
User Input ? Validation ? Create DTO ? Authenticate ? Send to DocuSign ? Save to DB ? Confirm
```

**Key Steps**:
1. **Input Collection**: User enters signers and selects attachments
2. **Validation**: Email format, required fields, minimum selections
3. **DTO Creation**: Bundle data into `DocuSignRequestDto`
4. **Authentication**: JWT token retrieval
5. **Envelope Creation**: Create DocuSign envelope with documents
6. **Database Persistence**: Store tracking information via SP
7. **User Confirmation**: Display success message with envelope ID

### **Sync Application Flow**
```mermaid
Read Args ? Query DB ? Check Status ? Download PDF ? Save to DB ? Update Status ? Log Results
```

**Key Steps**:
1. **Initialize**: Parse command-line arguments
2. **Query**: Get pending envelopes filtered by component/company/contract
3. **Status Check**: Call DocuSign API for each envelope
4. **Download**: Stream combined PDF for completed envelopes
5. **Persist**: Save PDF to database via stored procedure
6. **Update**: Mark envelope as completed
7. **Report**: Log success/error counts

---

## ?? Key Features

### **1. Multi-Signer Support**
- Dynamic addition of signers
- Routing order management
- Individual validation per signer
- Minimum 1 signer required

### **2. Attachment Management**
- Database-driven attachment selection
- Support for multiple file types
- Metadata tracking (FormName, Description, AddedBy, AddDate)
- Base64 encoding for DocuSign

### **3. Comprehensive Logging**
- **Log Levels**: DEBUG, INFO, WARN, ERROR
- **Features**:
  - Method entry/exit tracking
  - Parameter logging
  - Exception details with stack traces
  - Daily log rotation
  - Automatic cleanup (retention days)
- **Log Locations**:
  - WinForms: `C:\Logs\DocuSign\WinForms\`
  - Sync: `C:\Logs\DocuSign\Sync\`
  - Requests: `C:\Logs\DocuSign\Requests\`

### **4. Database Integration**

#### **Tables Used**:

**udtDocuSignTracking_S1S** (Tracking Table)
- DocuSignID (PK)
- EnvelopeID (DocuSign)
- Status (Pending, Completed)
- RequestFrom (component)
- Key_1 (Company ID)
- Key_2 (Contract ID)
- Requestor
- RequestedDtm

**udtDocuSignAttachments_S1S** (Attachment Mapping)
- DocuSignID (FK)
- AttachmentID
- FileName

**bHQAT** (Attachment Metadata - Viewpoint)
- AttachmentID
- FormName
- Description
- AddedBy
- AddDate
- UniqueAttchID
- OrigFileName

**bHQAF** (Attachment Binary Data - VPAttachments DB)
- AttachmentID
- AttachmentData (VARBINARY)
- AttachmentFileType

**JCCM** (Contract Master - Viewpoint)
- JCCo (Company)
- Contract
- UniqueAttchID
- KeyID

#### **Stored Procedures**:

**brptCreateDocuSignEntries_S1S**
- Creates tracking record
- Creates attachment mappings
- Returns DocuSignID

**brptUpdateDocuSignFileToDB_S1S**
- Inserts into bHQAT (metadata)
- Inserts into bHQAF (binary)
- Updates tracking status to "Completed"
- Transaction-safe with rollback

---

## ?? Configuration

### **appsettings.json Structure**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "...",
    "AttachmentDBConnection": "..."
  },
  "DocuSign": {
    "ClientId": "Integration Key",
    "AuthServer": "account-d.docusign.com",
    "ImpersonatedUserID": "User GUID",
    "AccountID": "Account Number",
    "PrivateKeyFile": "Path to .key file",
    "ApiBaseUrl": "https://demo.docusign.net/restapi"
  },
  "Logging": {
    "LogFilePath": "C:\\Logs\\DocuSign\\[App]\\",
    "LogFileName": "DocuSign_[App]_{date}.log",
    "LogLevel": "Information",
    "RetentionDays": 30
  }
}
```

### **Environment-Specific Settings**

**Development/Demo**:
- AuthServer: `account-d.docusign.com`
- ApiBaseUrl: `https://demo.docusign.net/restapi`

**Production**:
- AuthServer: `account.docusign.com`
- ApiBaseUrl: `https://www.docusign.net/restapi`

---

## ?? Security

### **Authentication**
- **Method**: JWT (JSON Web Token)
- **Token Lifetime**: 1 hour
- **Scopes**: signature, impersonation
- **Private Key**: RSA key stored locally

### **Consent Requirements**
- First-time use requires admin consent
- Consent URL opened in browser automatically
- Application exits after consent redirect

### **Data Protection**
- Passwords in connection strings (consider Azure Key Vault)
- Private key files secured on file system
- No sensitive data in logs (tokens masked)

---

## ?? Logging

### **Log Format**
```
2024-01-15 14:30:45.123 [LEVEL] Message
```

### **Key Logging Points**

**WinForms**:
- Application startup
- Argument parsing
- Signer addition/removal
- Validation results
- Envelope sending
- Database operations
- Success/failure

**Sync**:
- Process startup
- Envelope retrieval
- Status checks
- PDF downloads
- Database saves
- Success/error counts

**DocuSign.Requests**:
- Authentication
- Envelope creation
- API calls
- Document processing
- Error handling

### **Log Rotation**
- Daily files: `DocuSign_[App]_YYYYMMDD.log`
- Automatic cleanup after retention period
- Thread-safe file writes

---

## ?? Deployment

### **Prerequisites**
- .NET 9 Runtime
- SQL Server access
- DocuSign developer account
- Private key file (.key)
- Windows OS (for WinForms)

### **Configuration Steps**
1. Update `appsettings.json` with:
   - Connection strings
   - DocuSign credentials
   - Log paths
2. Deploy private key file
3. Grant consent (first-time)
4. Test with demo environment
5. Switch to production settings

### **Command-Line Arguments**

**WinForms**:
```bash
DocuSign.WinForms.exe component=JCCM companyID=01 contractID=12345 requestor=user@email.com
```

**Sync**:
```bash
DocuSign.Sync.exe component=JCCM companyID=01 contractID=12345 currentUser=user@email.com
```

---

## ?? Key Workflows

### **Workflow 1: Send Documents for Signature**
1. User launches WinForms app with contract context
2. App loads attachments from database
3. User enters 1+ signers
4. User selects attachments
5. App validates inputs
6. App authenticates with DocuSign (JWT)
7. App creates envelope with documents
8. DocuSign sends emails to signers
9. App saves tracking data to database
10. User sees success message

### **Workflow 2: Sync Completed Envelopes**
1. Sync app runs (scheduled/manual)
2. App queries pending envelopes
3. For each envelope:
   - Check status with DocuSign
   - If completed:
     - Download combined PDF
     - Stream to database
     - Update status
4. App logs summary
5. App exits

### **Workflow 3: Signer Signs Document**
1. Signer receives email from DocuSign
2. Signer clicks link
3. Signer reviews and signs
4. DocuSign completes envelope
5. Sync process detects completion
6. PDF saved to database
7. Status updated

---

## ?? Maintenance

### **Regular Tasks**
- Monitor log files
- Check database growth (bHQAF)
- Rotate logs (automatic)
- Update DocuSign SDK
- Refresh JWT tokens (automatic)

### **Troubleshooting**
- Check logs for errors
- Verify connection strings
- Confirm DocuSign credentials
- Test consent status
- Validate database permissions

---

## ?? Important Notes

1. **No File System Dependency**: PDFs streamed directly to database
2. **Transaction Safety**: Database operations use transactions
3. **Error Isolation**: One envelope failure doesn't stop others
4. **Comprehensive Logging**: Every operation logged
5. **Configurable**: All settings in appsettings.json
6. **Scalable**: Can process multiple envelopes
7. **Secure**: JWT authentication, no password storage

---

## ?? Technical Highlights

### **Design Patterns**
- **Repository Pattern**: Database access
- **DTO Pattern**: Data transfer objects
- **Factory Pattern**: Logger creation
- **Singleton Pattern**: Configuration access

### **Best Practices**
- ? Dependency injection ready
- ? Comprehensive error handling
- ? Logging at all levels
- ? Configuration-driven
- ? Thread-safe logging
- ? Transaction safety
- ? Parameter validation
- ? Resource disposal (using statements)

### **Performance Considerations**
- Stream PDFs (no temp files)
- Connection pooling
- Async-ready architecture
- Efficient memory management

---

## ?? Additional Resources

### **DocuSign Documentation**
- [DocuSign API Reference](https://developers.docusign.com/docs/esign-rest-api/)
- [JWT Authentication](https://developers.docusign.com/platform/auth/jwt/)
- [Envelope API](https://developers.docusign.com/docs/esign-rest-api/reference/envelopes/)

### **Internal Documentation**
- `LOGGING_README.md` - Logging implementation details
- `LOGGER_PASSING_README.md` - Logger architecture
- `APPLICATION_OVERVIEW.md` - This document

---

## ?? Summary

This DocuSign integration application provides a complete solution for:
- Sending documents for electronic signature via WinForms UI
- Automatically syncing completed documents back to database
- Comprehensive tracking and logging
- Secure JWT authentication
- Database-driven attachment management
- Multi-signer support with routing

The application is **production-ready**, **well-documented**, and follows **.NET best practices** with comprehensive error handling and logging throughout.
