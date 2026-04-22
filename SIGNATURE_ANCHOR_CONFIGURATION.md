# Signature Anchor Configuration Guide

## Overview
The DocuSign integration now supports **configurable anchor-based signature positioning** through the `appsettings.json` configuration file. This eliminates the need to recompile code when you want to change signature anchor text or positioning.

---

## Configuration Structure

### Location: `appsettings.json`

Add the following section under the `DocuSign` configuration:

```json
{
  "DocuSign": {
    "ClientId": "your-client-id",
    "AuthServer": "account-d.docusign.com",
    // ... other DocuSign settings ...
    "SignatureAnchors": {
      "PrimaryAnchorText": "Vendor Signature:",
      "SecondaryAnchorPattern": "Signer {0} Signature:",
      "AnchorXOffset": "100",
      "AnchorYOffset": "0",
      "AnchorUnits": "pixels",
      "AnchorIgnoreIfNotPresent": "true",
      "AnchorCaseSensitive": "false"
    }
  }
}
```

---

## Configuration Properties

### 1. **PrimaryAnchorText**
- **Type:** String
- **Default:** `"Vendor Signature:"`
- **Description:** The anchor text used for the **first signer**
- **Example:** If your PDF contains the text "Vendor Signature:", the signature field will be placed relative to this text

### 2. **SecondaryAnchorPattern**
- **Type:** String (with placeholder `{0}`)
- **Default:** `"Signer {0} Signature:"`
- **Description:** Pattern for additional signers (2nd, 3rd, etc.)
- **How it works:** 
  - Signer 2: `"Signer 2 Signature:"`
  - Signer 3: `"Signer 3 Signature:"`
  - etc.

### 3. **AnchorXOffset**
- **Type:** String (numeric)
- **Default:** `"100"`
- **Units:** Pixels (or as specified in `AnchorUnits`)
- **Description:** Horizontal offset from the anchor text
  - **Positive values:** Move signature to the **right**
  - **Negative values:** Move signature to the **left**
- **Example:** `"100"` = 100 pixels to the right of "Vendor Signature:"

### 4. **AnchorYOffset**
- **Type:** String (numeric)
- **Default:** `"0"`
- **Units:** Pixels (or as specified in `AnchorUnits`)
- **Description:** Vertical offset from the anchor text
  - **Positive values:** Move signature **down**
  - **Negative values:** Move signature **up**
- **Example:** `"-10"` = 10 pixels above the anchor text

### 5. **AnchorUnits**
- **Type:** String
- **Default:** `"pixels"`
- **Options:** `"pixels"` or `"mms"` (millimeters)
- **Description:** Units for offset measurements

### 6. **AnchorIgnoreIfNotPresent**
- **Type:** String (boolean)
- **Default:** `"true"`
- **Options:** `"true"` or `"false"`
- **Description:** If `"true"`, DocuSign won't error if the anchor text is not found in the PDF

### 7. **AnchorCaseSensitive**
- **Type:** String (boolean)
- **Default:** `"false"`
- **Options:** `"true"` or `"false"`
- **Description:** Whether anchor text matching is case-sensitive

---

## Configuration Examples

### Example 1: Standard Configuration
```json
"SignatureAnchors": {
  "PrimaryAnchorText": "Vendor Signature:",
  "SecondaryAnchorPattern": "Signer {0} Signature:",
  "AnchorXOffset": "100",
  "AnchorYOffset": "0",
  "AnchorUnits": "pixels",
  "AnchorIgnoreIfNotPresent": "true",
  "AnchorCaseSensitive": "false"
}
```
**Result:** 
- First signer signature appears 100 pixels to the right of "Vendor Signature:"
- Additional signers use "Signer 2 Signature:", "Signer 3 Signature:", etc.

---

### Example 2: Signature Below Anchor Text
```json
"SignatureAnchors": {
  "PrimaryAnchorText": "Sign here:",
  "SecondaryAnchorPattern": "Signer {0}:",
  "AnchorXOffset": "0",
  "AnchorYOffset": "20",
  "AnchorUnits": "pixels",
  "AnchorIgnoreIfNotPresent": "true",
  "AnchorCaseSensitive": "false"
}
```
**Result:** Signature appears 20 pixels **below** the anchor text

---

### Example 3: Multiple Vendors
```json
"SignatureAnchors": {
  "PrimaryAnchorText": "Vendor 1 Signature:",
  "SecondaryAnchorPattern": "Vendor {0} Signature:",
  "AnchorXOffset": "50",
  "AnchorYOffset": "-5",
  "AnchorUnits": "pixels",
  "AnchorIgnoreIfNotPresent": "true",
  "AnchorCaseSensitive": "false"
}
```
**Result:**
- Signer 1: Uses "Vendor 1 Signature:"
- Signer 2: Uses "Vendor 2 Signature:"
- Signature appears 50px to the right and 5px above the text

---

### Example 4: Case-Sensitive Matching
```json
"SignatureAnchors": {
  "PrimaryAnchorText": "VENDOR SIGNATURE:",
  "SecondaryAnchorPattern": "SIGNER {0}:",
  "AnchorXOffset": "75",
  "AnchorYOffset": "0",
  "AnchorUnits": "pixels",
  "AnchorIgnoreIfNotPresent": "true",
  "AnchorCaseSensitive": "true"
}
```
**Result:** Only matches exact case ("VENDOR SIGNATURE:", not "Vendor Signature:")

---

## PDF Requirements

For anchor-based positioning to work, your PDF documents **must contain** the anchor text:

### Required Text in PDF:
- **For 1st Signer:** Include the text specified in `PrimaryAnchorText` (e.g., "Vendor Signature:")
- **For 2nd Signer:** Include the text based on `SecondaryAnchorPattern` (e.g., "Signer 2 Signature:")
- **For 3rd Signer:** Include "Signer 3 Signature:", etc.

### Example PDF Layout:
```
?????????????????????????????????????????
?          CONTRACT AGREEMENT           ?
?????????????????????????????????????????
?                                       ?
?  Terms and conditions...              ?
?                                       ?
?  Vendor Signature: ___________        ?
?                                       ?
?  Signer 2 Signature: ___________      ?
?                                       ?
?????????????????????????????????????????
```

---

## How It Works

1. **Configuration Loading:**
   - `AppSettings.GetSignatureAnchorPrimaryText()` reads `PrimaryAnchorText`
   - `AppSettings.GetSignatureAnchorXOffset()` reads `AnchorXOffset`
   - etc.

2. **Populating UserInputs:**
   ```csharp
   var userInputs = new UserInputs()
   {
       SignatureAnchorPrimaryText = AppSettings.GetSignatureAnchorPrimaryText(),
       SignatureAnchorXOffset = AppSettings.GetSignatureAnchorXOffset(),
       // ...
   };
   ```

3. **Creating Signature Fields:**
   ```csharp
   for (int i = 0; i < signers.Count; i++)
   {
       string anchorString = i == 0 
           ? userInputs.SignatureAnchorPrimaryText 
           : string.Format(userInputs.SignatureAnchorSecondaryPattern, i + 1);
       
       var signHere = new SignHere
       {
           AnchorString = anchorString,
           AnchorXOffset = userInputs.SignatureAnchorXOffset,
           AnchorYOffset = userInputs.SignatureAnchorYOffset,
           // ...
       };
   }
   ```

---

## Benefits

? **No Recompilation Required** - Change settings without rebuilding  
? **Environment-Specific** - Different settings for Dev/Test/Prod  
? **Flexible Positioning** - Adjust X/Y offsets easily  
? **Multiple Anchor Patterns** - Support different PDF layouts  
? **Fail-Safe** - Gracefully handles missing anchor text  
? **Logged** - All anchor settings logged for debugging  

---

## Troubleshooting

### Issue: Signature not appearing in PDF
**Solution:**
1. Verify anchor text exists in PDF (exact match)
2. Check `AnchorCaseSensitive` setting
3. Review logs for anchor string used
4. Try `AnchorIgnoreIfNotPresent = "false"` to see errors

### Issue: Signature in wrong position
**Solution:**
1. Adjust `AnchorXOffset` (positive = right, negative = left)
2. Adjust `AnchorYOffset` (positive = down, negative = up)
3. Check `AnchorUnits` (`pixels` vs `mms`)

### Issue: Multiple signers using same anchor
**Solution:**
1. Ensure PDF has unique anchor text for each signer
2. Verify `SecondaryAnchorPattern` includes `{0}` placeholder
3. Check logs for actual anchor strings generated

---

## Logging

All anchor configuration is logged during envelope creation:

```
2024-01-15 14:30:45.123 [DEBUG] Signature anchor settings - Primary: 'Vendor Signature:', XOffset: 100, YOffset: 0
2024-01-15 14:30:45.124 [DEBUG] Signer 1 (John Doe) will use anchor string: 'Vendor Signature:'
2024-01-15 14:30:45.125 [DEBUG] Signer 2 (Jane Smith) will use anchor string: 'Signer 2 Signature:'
```

Check logs at: `C:\Logs\DocuSign\[App]\DocuSign_[App]_YYYYMMDD.log`

---

## Projects Configured

The following projects now support configurable signature anchors:

| Project | Config File Location |
|---------|---------------------|
| **WinForms** | `Source1Solutions.DocuSign.WinForms\appsettings.json` |
| **Sync** | `Source1Solutions.DocuSign.Sync\appsettings.json` |

Both projects share the same configuration structure and defaults.

---

## Default Values

If any setting is missing from `appsettings.json`, these defaults are used:

```json
{
  "PrimaryAnchorText": "Vendor Signature:",
  "SecondaryAnchorPattern": "Signer {0} Signature:",
  "AnchorXOffset": "100",
  "AnchorYOffset": "0",
  "AnchorUnits": "pixels",
  "AnchorIgnoreIfNotPresent": "true",
  "AnchorCaseSensitive": "false"
}
```

---

## Advanced Scenarios

### Scenario 1: Different Anchor Text Per Signer Type
```json
{
  "PrimaryAnchorText": "Contractor Signature:",
  "SecondaryAnchorPattern": "Witness {0} Signature:",
  //...
}
```

### Scenario 2: Precise Positioning with Millimeters
```json
{
  "AnchorXOffset": "25",
  "AnchorYOffset": "5",
  "AnchorUnits": "mms",
  //...
}
```

### Scenario 3: Strict Matching
```json
{
  "AnchorIgnoreIfNotPresent": "false",
  "AnchorCaseSensitive": "true",
  //...
}
```
**Result:** Will throw error if exact anchor text not found

---

## Summary

You can now easily configure signature anchor positioning by editing `appsettings.json`:

1. **Set anchor text** in `PrimaryAnchorText` and `SecondaryAnchorPattern`
2. **Adjust position** using `AnchorXOffset` and `AnchorYOffset`
3. **Configure behavior** with `AnchorIgnoreIfNotPresent` and `AnchorCaseSensitive`
4. **No code changes required** - just update config and restart application

For questions or issues, check the application logs for detailed anchor information! ??
