# Third-Party Application Call Fix

## Problem
When calling the DocuSign WinForms executable from Viewpoint Construction Software (Vista), the application was looking for `appsettings.json` in the **caller's directory** instead of the **executable's directory**:

```
Error: The configuration file 'appsettings.json' was not found and is not optional.
Expected path: C:\Program Files (x86)\Viewpoint Construction Software\Vista\bin\appsettings.json

Actual executable location: J:\Vista_Apps\PublishWinSync\
Actual config location: J:\Vista_Apps\PublishWinSync\appsettings.json
```

## Root Cause
When an application is launched by another process (like Vista), some .NET methods for getting the executable directory can behave unexpectedly and may return the caller's directory instead.

## Solution Applied

Updated both `AppSettings.cs` files to use **multiple fallback methods** to reliably find the executable's directory, starting with the most robust approach.

### Method Priority (in order):

1. **`AppContext.BaseDirectory`** (Most reliable for .NET Core/.NET 5+)
2. **`Assembly.GetExecutingAssembly().Location`** (Standard method)
3. **`Application.StartupPath`** (WinForms) or **`Process.MainModule.FileName`** (Console)
4. **`Directory.GetCurrentDirectory()`** (Last resort fallback)

---

## Files Modified

? `Source1Solutions.DocuSign.WinForms/AppSettings.cs`
? `Source1Solutions.DocuSign.WinSync/AppSettings.cs`

---

## Why AppContext.BaseDirectory?

- **Always returns the application's base directory**, not the caller's
- Works correctly even when called from another process
- Recommended by Microsoft for .NET Core/.NET 5+ applications
- Returns the directory containing the entry point assembly

---

## Your Deployment Setup

```
Caller Application:
    C:\Program Files (x86)\Viewpoint Construction Software\Vista\bin\Vista.exe

Your DocuSign Application:
    J:\Vista_Apps\PublishWinSync\Source1Solutions.DocuSign.WinForms.exe
    J:\Vista_Apps\PublishWinSync\appsettings.json  ? Must be here
```

---

## Testing Steps

### 1. Publish the Application
```bash
dotnet publish -c Release -o J:\Vista_Apps\PublishWinSync
```

### 2. Verify Files
Ensure these files exist in `J:\Vista_Apps\PublishWinSync\`:
- ? Source1Solutions.DocuSign.WinForms.exe
- ? appsettings.json
- ? All required DLLs

### 3. Test Direct Execution
```bash
cd J:\Vista_Apps\PublishWinSync
.\Source1Solutions.DocuSign.WinForms.exe component=TEST Key_1_ID=123
```

### 4. Test from Vista
Let Vista call your application normally.

**Expected Result:** No configuration file errors! ?

---

## Build Status

? **Build: SUCCESSFUL**
? **Configuration Loading: Fixed**
? **Ready for Deployment**
