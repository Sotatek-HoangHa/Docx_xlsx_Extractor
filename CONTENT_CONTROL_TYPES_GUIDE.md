# Content Control Types Guide

## Overview

Content controls in Microsoft Word and OpenXML support **explicit type information** that can be used to automatically detect field types for validation.

## Two Approaches to Field Type Detection

### Approach 1: Key/Label-Based Detection (Current)
- **File**: `Services/FieldValidator.cs`
- **How it works**: Analyzes the field key (`birth_date`, `email_address`) and label to guess the type
- **Pros**: Works with any template, no special setup needed
- **Cons**: May make wrong guesses, requires keyword matching

**Example**:
```
Key: "birth_date" → Detected as: Date
Key: "email_address" → Detected as: Email
Key: "agree_terms" → Detected as: Boolean
```

### Approach 2: Explicit Type Detection (Enhanced)
- **File**: `Services/FieldValidatorWithTypeDetection.cs`
- **How it works**: Reads the actual control type stored in the DOCX file
- **Pros**: Accurate detection, no guessing, explicit intent
- **Cons**: Requires creating templates with typed controls

## Content Control Types in Word

| Type | OpenXML Element | Use in Word | Example |
|------|---|---|---|
| **Plain Text** | `<w:plainText>` | Simple text input | Name, Address |
| **Rich Text** | `<w:richText>` | Formatted text with bold/italic | Description, Comments |
| **Date Picker** | `<w:date>` | Calendar date selection | Birth Date, Event Date |
| **Dropdown List** | `<w:dropdownList>` | Fixed list (read-only) | Gender, Status |
| **Combo Box** | `<w:comboBox>` | List + custom values | Country, Category |
| **Checkbox** | `<w:checkbox>` | Toggle on/off | Agreement, Confirmation |
| **Picture** | `<w:picture>` | Image/photo upload | Photo, Signature |
| **Repeating Section** | `<w:repeatingSection>` | Repeatable blocks | Multiple addresses, Items |

## How to Set Control Types in Microsoft Word

### Manual Method (UI)

1. **Open sample-template.docx** in Microsoft Word
2. **Enable Developer Tab** (if not visible):
   - File → Options → Customize Ribbon
   - Check "Developer"
   - Click OK

3. **Enter Design Mode**:
   - Developer tab → Design Mode (turn ON)

4. **Insert a Content Control**:
   - Position cursor where you want control
   - Developer tab → Choose control type:
     - **Plain Text Control** (Abc)
     - **Rich Text Control** (Abc with formatting)
     - **Picture Control** 📷
     - **Dropdown List** 🔽
     - **Combo Box** (editable list)
     - **Date Picker** 📅
     - **Checkbox** ☑️
     - **Building Block Gallery**
     - **Repeating Section**

5. **Configure Properties**:
   - Right-click control → Properties
   - Set **Title**: "Birth Date"
   - Set **Tag**: "birth_date" (for extraction)
   - For Dropdown/Combo: Add list items
   - Click OK

6. **Exit Design Mode**:
   - Developer tab → Design Mode (turn OFF)

7. **Save** the template

### Programmatic Method (C#)

Use the new `TemplateCreatorTyped.cs` class:

```csharp
// Create a template with typed controls
TemplateCreatorTyped.CreateSampleTemplateWithTypes("typed-template.docx");
```

This creates controls like:
- **Plain Text**: Full Name, Email
- **Date Picker**: Birth Date
- **Dropdown**: Gender (Male/Female/Other)
- **Combo Box**: Country (with custom values allowed)
- **Rich Text**: Address (supports formatting)
- **Checkbox**: Agreements

## Using Type Detection in Code

### Option A: Key/Label Detection (Default)
```csharp
// Extract fields from template
List<FieldValue> fields = ContentExtractor.ExtractFields("sample-template-filled.docx");

// Validate using key-based type detection
FieldValidator.ValidateFields(fields);

// Output shows validation errors appended to values
// "invalid-email - invalid email format"
```

### Option B: DOCX Type Detection (Enhanced)
```csharp
// Extract fields from template
List<FieldValue> fields = ContentExtractor.ExtractFields("sample-template-filled.docx");

// Validate using explicit type information from DOCX
FieldValidatorWithTypeDetection.ValidateFieldsFromDocx(
    "sample-template-filled.docx", 
    fields
);

// Output shows validation errors based on actual control types
```

## Comparison

| Feature | FieldValidator | FieldValidatorWithTypeDetection |
|---------|---|---|
| Type Detection | Key/Label based | Read from DOCX |
| Accuracy | Good (with keywords) | Perfect |
| Setup Required | None | Create typed template |
| Performance | Fast | Slightly slower (reads DOCX) |
| Flexibility | High (any template) | Requires template setup |
| Scalability | Good | Better for many fields |

## Example: Creating a Typed Template

### Step 1: Create Template Programmatically
```csharp
TemplateCreatorTyped.CreateSampleTemplateWithTypes("my-template.docx");
```

### Step 2: Fill Template
```csharp
var data = new Dictionary<string, string>
{
    { "full_name", "Jane Smith" },
    { "email_address", "jane@example.com" },
    { "birth_date", "1995-03-20" },
    { "gender", "Female" },
    { "country", "United States" },
    { "address", "789 Oak Ave, Portland, OR" },
    { "agree_terms", "Yes" }
};

// Fill controls with data...
```

### Step 3: Extract and Validate
```csharp
var fields = ContentExtractor.ExtractFields("filled-template.docx");
FieldValidatorWithTypeDetection.ValidateFieldsFromDocx("filled-template.docx", fields);

// All fields validated based on their explicit control type
```

## Validation Rules by Type

### DatePicker / Date
- Valid formats: YYYY-MM-DD, MM/DD/YYYY, DD.MM.YYYY, etc.
- Error: `"invalid date format"`

### Email
- Must match email format (user@domain.com)
- Error: `"invalid email format"`

### Phone
- Must have 10+ digits
- Allows: spaces, dashes, parentheses, +
- Error: `"invalid phone format"`

### Url
- Must be valid HTTP/HTTPS URL
- Error: `"invalid URL format"`

### Gender
- Valid: Male, Female, Other, N/A, Prefer not to say, Non-binary
- Error: `"invalid gender value"`

### Country
- Must be 2+ characters and not all numbers
- Error: `"invalid country format"`

### Boolean / Checkbox
- Valid: Yes, No, True, False, 1, 0, Checked, Unchecked
- Error: `"invalid boolean value"`

### Text / DropdownList / ComboBox
- Any non-empty value is valid
- Error: `"empty value"`

## XML Structure Examples

### Plain Text Control in DOCX XML
```xml
<w:sdt>
  <w:sdtPr>
    <w:tag w:val="full_name" />
    <w:alias w:val="Full Name" />
    <w:plainText />  <!-- ← Type indicator -->
  </w:sdtPr>
  <w:sdtContent>
    <w:r>
      <w:t>John Doe</w:t>
    </w:r>
  </w:sdtContent>
</w:sdt>
```

### Date Picker Control in DOCX XML
```xml
<w:sdt>
  <w:sdtPr>
    <w:tag w:val="birth_date" />
    <w:alias w:val="Date of Birth" />
    <w:date/>  <!-- ← Type indicator -->
  </w:sdtPr>
  <w:sdtContent>
    <w:r>
      <w:t>1995-03-20</w:t>
    </w:r>
  </w:sdtContent>
</w:sdt>
```

### Checkbox Control in DOCX XML
```xml
<w:sdt>
  <w:sdtPr>
    <w:tag w:val="agree_terms" />
    <w:alias w:val="I agree" />
    <w:checkbox/>  <!-- ← Type indicator -->
  </w:sdtPr>
  <w:sdtContent>
    <w:r>
      <w:t>☑</w:t>
    </w:r>
  </w:sdtContent>
</w:sdt>
```

## Recommendations

1. **For Simple Projects**: Use `FieldValidator` (key/label based)
   - Less setup required
   - Works with existing templates
   - Good enough for most cases

2. **For Enterprise Projects**: Use `FieldValidatorWithTypeDetection`
   - More accurate
   - Clearer intent (explicit types)
   - Better for complex forms with many field types

3. **Best Practice**: Create templates with typed controls
   - Use `TemplateCreatorTyped` or Word UI
   - Explicit control types are self-documenting
   - Validation becomes automatic and reliable
   - Future developers understand your schema instantly

## Next Steps

- [ ] Review `TemplateCreatorTyped.cs` for creating typed templates
- [ ] Review `FieldValidatorWithTypeDetection.cs` for type-aware validation
- [ ] Test with your own templates
- [ ] Consider migrating existing templates to use typed controls
