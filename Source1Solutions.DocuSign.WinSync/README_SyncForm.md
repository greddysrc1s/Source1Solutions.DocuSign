# SyncForm DataGridView Implementation

## Overview
The SyncForm has been updated to display DocuSign tracking data in a DataGridView using the stored procedure `GetDocuSignTrackingDetails_S1S`.

## Form Layout

```
??????????????????????????????????????????????????????????????????
?                  DocuSign Sync Tracking                         ?
??????????????????????????????????????????????????????????????????
? Request From: [________] Key 1: [________] Key 2: [________]  ?
?                                                                 ?
? ?????????????????????????????????????????????????????????????? ?
? ? DocuSign Tracking Data Grid                                ? ?
? ? ???????????????????????????????????????????????????????    ? ?
? ? ? ID   ?Envelope?Requestor ?Requested   ?Signers      ?    ? ?
? ? ?      ?ID      ?          ?Date        ?             ?... ? ?
? ? ???????????????????????????????????????????????????????    ? ?
? ? ? ...  ? ...    ? ...      ? ...        ? ...         ?    ? ?
? ? ???????????????????????????????????????????????????????    ? ?
? ?????????????????????????????????????????????????????????????? ?
?                                                                 ?
?                          [Refresh Sync] [Exit Application]     ?
??????????????????????????????????????????????????????????????????
```

## Stored Procedure

### Name
`dbo.GetDocuSignTrackingDetails_S1S`

### Parameters
- `@RequestFrom` (VARCHAR(20)) - Filter by request source
- `@Key_1` (VARCHAR(20)) - First key filter
- `@Key_2` (VARCHAR(20)) - Second key filter

### Returned Columns
All columns are displayed in the DataGridView:

| Column Name | Data Type | Description | Width |
|------------|-----------|-------------|-------|
| DocuSignID | INT | Unique identifier | 100px |
| EnvelopeID | VARCHAR | DocuSign envelope ID | 200px |
| Requestor | VARCHAR | Person who requested | 120px |
| RequestedDtm | DATETIME | Request timestamp | 150px |
| Signers | VARCHAR | Comma-separated signer emails | 200px |
| SignersName | VARCHAR | Comma-separated signer names | 200px |
| Attachments | VARCHAR | Comma-separated attachment names | 250px |
| CarbonCopies | VARCHAR | Comma-separated CC emails | 200px |
| CarbonCopiesName | VARCHAR | Comma-separated CC names | 200px |
| RequestFrom | VARCHAR | Request source/component | 120px |
| Key_1 | VARCHAR | First key value | 100px |
| Key_2 | VARCHAR | Second key value | 100px |
| Status | VARCHAR | Current status | 100px |
| Error_Msg | VARCHAR | Error message if any | 250px |

## DataGridView Configuration

### Properties
- **AutoGenerateColumns**: `false` (manual column definition)
- **AllowUserToAddRows**: `false` (read-only)
- **AllowUserToDeleteRows**: `false` (read-only)
- **ReadOnly**: `true`
- **SelectionMode**: `FullRowSelect`
- **MultiSelect**: `false`
- **AutoSizeColumnsMode**: `None` (manual sizing)

### Column Features
- Custom widths for optimal display
- DateTime formatting for `RequestedDtm` column (MM/dd/yyyy HH:mm:ss)
- All columns are sortable by default
- Full row selection for better visibility

## Command Line Arguments

The form accepts command line arguments to pre-populate filter values:

```bash
Source1Solutions.DocuSign.WinSync.exe RequestFrom=WinForms Key_1=ABC123 Key_2=XYZ789
```

### Supported Arguments
- `RequestFrom` - Sets the Request From filter
- `Key_1` - Sets the Key 1 filter
- `Key_2` - Sets the Key 2 filter

### Example
```bash
# Launch with filters pre-populated
Source1Solutions.DocuSign.WinSync.exe RequestFrom=ContractModule Key_1=12345 Key_2=67890

# Launch without filters (will show all records based on SP default behavior)
Source1Solutions.DocuSign.WinSync.exe
```

## Functionality

### 1. **Form Load**
When the form loads:
- Filter textboxes are populated from command line arguments
- DataGridView columns are initialized
- Data is automatically loaded from the stored procedure

### 2. **Refresh Button**
- Reloads data from the database using current filter values
- Shows success message when complete
- Logs all operations

### 3. **Exit Button**
- Closes the application
- Logs shutdown

## Code Implementation

### Key Methods

#### **InitializeDataGridView()**
```csharp
// Sets up all 14 columns with proper widths and formatting
// Configures grid properties (read-only, selection mode, etc.)
```

#### **LoadDocuSignTrackingData()**
```csharp
// Calls stored procedure: dbo.GetDocuSignTrackingDetails_S1S
// Parameters:
//   @RequestFrom - from txtRequestFrom (or DBNull if empty)
//   @Key_1 - from txtKey1 (or DBNull if empty)
//   @Key_2 - from txtKey2 (or DBNull if empty)
// Binds result to DataGridView using DataTable
```

#### **btnRefresh_Click()**
```csharp
// Refreshes data from database
// Shows success message
// Comprehensive error handling
```

## Logging

All operations are logged with the Logger class:

### Logged Events
- Form initialization
- Command line argument parsing
- DataGridView initialization (including column count)
- Data loading with filter values
- Number of records loaded
- Refresh operations
- All errors with stack traces

### Log Location
`C:\Logs\DocuSign\WinSync\DocuSign_WinSync_{date}.log`

### Log Levels Used
- **Information**: Normal operations, startup, data loaded
- **Debug**: Detailed information (filter values, SQL execution)
- **Warning**: Non-critical issues
- **Error**: Exceptions and failures

## Database Connection

Uses the connection string from `appsettings.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=WAP-sql.viewpointdata.cloud,4316;Database=Viewpoint;..."
}
```

## Error Handling

### User-Friendly Error Messages
All errors are caught and displayed to the user with MessageBox:
- **Form Load Errors**: "Error loading form: {message}"
- **Data Loading Errors**: "Error loading tracking data: {message}"
- **Refresh Errors**: "Error refreshing data: {message}"

### Comprehensive Logging
All errors are logged with:
- Method name
- Error message
- Full stack trace
- Context information (filter values, etc.)

## UI Features

### Filter Display
- Read-only textboxes showing current filter values
- Pre-populated from command line arguments
- Clearly labeled for user reference

### Grid Features
- Horizontal and vertical scrolling for large datasets
- Full row selection highlights entire record
- Sortable columns (click column header)
- Resizable columns (drag column borders)

### Button Placement
- Right-aligned at bottom of form
- "Refresh Sync" button for reloading data
- "Exit Application" button for closing

## Testing

### Test Scenarios

1. **Launch with filters**
   ```bash
   Source1Solutions.DocuSign.WinSync.exe RequestFrom=WinForms Key_1=123 Key_2=456
   ```
   - Verify textboxes are populated
   - Verify data is filtered

2. **Launch without filters**
   ```bash
   Source1Solutions.DocuSign.WinSync.exe
   ```
   - Verify all records are shown (or SP default behavior)

3. **Refresh functionality**
   - Click Refresh Sync button
   - Verify data reloads
   - Verify success message displays

4. **Empty result set**
   - Use filters that return no records
   - Verify grid is empty (no errors)

5. **Database connection failure**
   - Temporarily break connection string
   - Verify error message displays
   - Verify error is logged

## Performance Considerations

- Uses `SqlDataAdapter` for efficient data retrieval
- DataTable binding for optimal grid performance
- Read-only grid prevents accidental edits
- Connection properly disposed with `using` statements

## Future Enhancements

Possible improvements:
1. **Editable Filters** - Make filter textboxes editable for user queries
2. **Export to Excel** - Add button to export grid data
3. **Row Details** - Double-click row to show detailed view
4. **Auto-Refresh** - Timer-based automatic refresh
5. **Status Color Coding** - Color code rows based on status
6. **Column Chooser** - Allow users to show/hide columns
7. **Search Box** - Quick search across all columns

## Build Status
? **Build Successful** - All changes compile without errors

## Dependencies
- .NET 9.0
- Microsoft.Data.SqlClient
- DocuSign.Requests (for Logger)
- Microsoft.Extensions.Configuration

## Summary
The SyncForm now provides a complete view of DocuSign tracking data with:
- 14 columns matching stored procedure output
- Filter support via command line arguments
- Refresh functionality
- Comprehensive logging
- User-friendly error handling
- Professional UI layout
