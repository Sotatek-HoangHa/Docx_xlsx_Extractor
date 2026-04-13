# Content Control Tag Format Guide

## Overview

The refactored `FieldValidator` now supports **explicit type tags** in the tag field of content controls. This eliminates the need for key/label string matching and uses **pure regex validation** based on the explicit type.

## Tag Format

```
fieldName:fieldType:additionalInfo
```

### Examples

```
email_address:email
birth_date:datetime
phone:phone
website:url
gender:enum:Male|Female|Other|Prefer not to say
country:country
agree_terms:boolean
full_name:text
```

## Setting Tags in Microsoft Word

### Step-by-Step

1. **Open your DOCX template** in Microsoft Word
2. **Enable Developer Tab** (File → Options → Customize Ribbon → Check "Developer")
3. **Turn ON Design Mode** (Developer tab → Design Mode)
4. **Right-click any content control** → Select **Properties**
5. **In the Properties dialog**, set the **Tag** field:

   ```
   Format: fieldName:fieldType
   
   Examples:
   ✓ email_address:email
   ✓ birth_date:datetime
   ✓ phone_number:phone
   ✓ website_url:url
   ✓ gender:enum:Male|Female|Other
   ```

6. **Click OK**
7. **Turn OFF Design Mode**
8. **Save the template**

## Supported Type Tags

| Tag Type | Example | Validation Rule |
|----------|---------|-----------------|
| **email** | `email_address:email` | RFC-compliant email format |
| **datetime** / **date** | `birth_date:datetime` | Date formats: YYYY-MM-DD, MM/DD/YYYY, DD.MM.YYYY |
| **phone** | `phone_number:phone` | 10+ digits with formatting allowed |
| **url** / **website** | `website_url:url` | HTTP/HTTPS URLs only |
| **gender** | `gender:gender` | male, female, other, n/a, prefer not to say, non-binary |
| **country** / **region** | `country:country` | 2+ letters (no pure numbers) |
| **boolean** / **checkbox** | `agree_terms:boolean` | yes, no, true, false, 1, 0, checked, unchecked |
| **text** | `full_name:text` | Any non-empty value |

## Regex Patterns Used

The validator uses these regex patterns internally:

```csharp
// Email
^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$

// Date
^(\d{4}[-/]\d{2}[-/]\d{2}|\d{1,2}[-/.]\d{1,2}[-/.]\d{2,4}|[A-Za-z]+\s+\d{1,2},?\s+\d{4})$

// Phone
^\+?[\d\s\-()]+$  (with 10+ digit requirement)

// URL
^https?://[^\s/$.?#].[^\s]*$

// Gender
^(male|female|other|n/a|prefer\s+not\s+to\s+say|non-binary)$

// Country
^[a-zA-Z\s]{2,}$

// Boolean
^(yes|no|true|false|1|0|checked|unchecked)$
```

## How It Works

### 1. Backward Compatibility (Old Way)

If you don't use the new tag format, the validator **automatically falls back** to key/label detection:

```
Tag: "email_address"
Label: "Email Address"

→ Detects "email_address" contains "email"
→ Validates as: Email type
```

### 2. Explicit Type Tags (New Way)

If you use the new tag format, the validator **uses explicit type**:

```
Tag: "email_address:email"
Label: "Email Address"

→ Parses tag: splits on ':' → gets "email"
→ Validates as: Email type (pure regex)
```

## Algorithm Flow

```
Field received
    ↓
Check if tag contains ':' 
    ↓
    YES → Parse explicit type from tag
           └─ Extract part after ':' → "email"
    ↓
    NO → Fall back to key/label detection
         └─ Search key/label for keywords → "email_address" → "email"
    ↓
Validate using regex pattern for that type
    ↓
Append error if validation fails
```

## Example: Complete Setup

### Scenario
You want to validate an email field.

### Step 1: Create Control in Word
```
1. Developer → Design Mode (ON)
2. Insert Plain Text Control
3. Right-click → Properties
   - Title: "Email Address"
   - Tag: "user_email:email"  ← Explicit type!
4. Click OK
5. Design Mode (OFF)
```

### Step 2: Fill with Data
```
User enters: "john@example.com"
```

### Step 3: Validation
```
Tag: "user_email:email"
  └─ Extracts: "email"
  └─ Validates with regex: ^[a-zA-Z0-9.!#$%...@...

Result: ✅ Valid
```

### Step 4: Validation Error
```
User enters: "invalid-email"

Tag: "user_email:email"
  └─ Extracts: "email"
  └─ Validates with regex: ^[a-zA-Z0-9.!#$%...@...

Result: ❌ invalid email format
Output: "invalid-email - invalid email format"
```

## Advanced: Custom Enum Values

For enum-like fields, you can optionally specify allowed values:

```
Tag: "status:enum:Active|Inactive|Pending"
```

(Note: Current implementation treats as generic enum, future enhancement can validate against specific values)

## Benefits

### ✅ Before (Key/Label Detection)
- String matching required ("email", "phone", "date" in key/label)
- Fragile - breaks if naming changes
- Multiple fallback patterns needed

### ✅ After (Explicit Type Tags)
- No string matching - pure regex validation
- Self-documenting - type is explicit
- Robust - tag format is independent of naming
- Performance - no need to search keywords

## Migration Guide

### Step 1: Identify existing controls
```
Current: Tag = "email_address"
Updated: Tag = "email_address:email"
```

### Step 2: Update all controls
| Field | Old Tag | New Tag |
|-------|---------|---------|
| Email | email_address | email_address:email |
| Birth Date | birth_date | birth_date:datetime |
| Phone | phone_number | phone_number:phone |
| Website | website_url | website_url:url |

### Step 3: No code changes needed!
The validator automatically handles both formats.

## Testing

Test with various inputs:

```json
{
  "Key": "user_email:email",
  "Value": "john@example.com"
  → ✅ Valid
}

{
  "Key": "user_email:email",
  "Value": "invalid.email"
  → ❌ invalid email format
}

{
  "Key": "birth_date:datetime",
  "Value": "1990-05-15"
  → ✅ Valid
}

{
  "Key": "birth_date:datetime",
  "Value": "not-a-date"
  → ❌ invalid date format
}

{
  "Key": "phone:phone",
  "Value": "+1 (555) 123-4567"
  → ✅ Valid (10+ digits)
}

{
  "Key": "phone:phone",
  "Value": "123"
  → ❌ phone must have at least 10 digits
}
```

## Summary

✅ **Old Way** (Still Works):
- Tag: `email_address`
- Uses key/label matching to detect type
- Backward compatible

✅ **New Way** (Recommended):
- Tag: `email_address:email`
- Uses explicit type specification
- Pure regex validation
- Self-documenting
- More robust

Both approaches work simultaneously. Migrate at your own pace! 🚀
