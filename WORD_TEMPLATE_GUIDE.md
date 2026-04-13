# How to Create DOCX Templates with Content Controls in Microsoft Word

This guide walks you through creating fillable templates in Word that work with the DocxExtractor application.

## What are Content Controls?

Content controls are interactive placeholders in Word documents that users can fill with data. They have:
- **Title**: The machine-readable key (e.g., "full_name")
- **Tag**: Additional identifier
- **Type**: Text, Date, Checkbox, Dropdown, etc.

## Prerequisites

- Microsoft Word 2013 or later (Office 365, Word 2016, 2019, 2021)
- "Developer" tab enabled in Word ribbon

## Step 1: Enable the Developer Tab

If you don't see the Developer tab in Word's ribbon:

### For Windows:

1. Click **File** menu
2. Click **Options**
3. Select **Customize Ribbon**
4. In the list on the right, check **Developer**
5. Click **OK**

### For Mac:

1. Click **Word** menu
2. Click **Preferences**
3. Click **Ribbon & Toolbar**
4. Check **Developer**
5. Click **Save**

## Step 2: Create a Template Document

1. Open Microsoft Word
2. Create a new blank document
3. Type your form structure:

```
APPLICATION FORM
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

PERSONAL INFORMATION

Full Name: _____________________

Email Address: _____________________

Date of Birth: _____________________

Address: _____________________


AGREEMENT

☐ I agree to the terms and conditions

```

## Step 3: Insert Content Controls

### For Each Fillable Field:

1. **Position Cursor**
   - Click where you want to insert the blank field
   - Example: after "Full Name: "

2. **Enter Design Mode**
   - Click **Developer** tab
   - Click **Design Mode** button (toggle it ON - it should appear pressed)
   - The button will show a highlighted/pressed state

3. **Insert Content Control**
   
   **For Plain Text Fields:**
   - Click **Developer** → **Plain Text Content Control**
   - A placeholder will appear: `☐ Click here to enter text.`

   **For Rich Text (multi-line):**
   - Click **Developer** → **Rich Text Content Control**

   **For Dates:**
   - Click **Developer** → **Date Picker Content Control**

   **For Checkboxes:**
   - Click **Developer** → **Checkbox Content Control**

   **For Dropdowns:**
   - Click **Developer** → **Drop-Down List Content Control**
   - Then right-click → **Properties** to add options

4. **Set Control Properties**
   - Right-click the inserted content control
   - Click **Properties**
   - In the dialog:
     - **Title**: Enter the key name (e.g., "full_name", "email_address")
     - **Tag**: (Optional) For display purposes
     - **Style**: Choose visual style
     - Other options as needed
   - Click **OK**

5. **Exit Design Mode**
   - Click **Developer** tab
   - Click **Design Mode** again (to toggle it OFF)

## Step 4: Test Your Template

1. Exit Design Mode
2. Try clicking on the controls to ensure they work
3. Test filling in data
4. Delete the test data
5. Save the template

## Step 5: Save Your Template

1. Click **File** → **Save As**
2. Choose location
3. **File name**: Enter your template name (e.g., "application-form")
4. **Save as type**: Select "Word Document (*.docx)"
5. Click **Save**

## Complete Example: Application Form Template

Here's a complete template structure to follow:

```
═════════════════════════════════════════════════════════════════
                     APPLICATION FORM
═════════════════════════════════════════════════════════════════

Please complete all fields below. Fields marked with * are required.

─────────────────────────────────────────────────────────────────
SECTION 1: PERSONAL INFORMATION
─────────────────────────────────────────────────────────────────

* Full Name:
[Content Control - key: full_name]


* Email Address:
[Content Control - key: email_address]


Date of Birth:
[Content Control - key: birth_date]


─────────────────────────────────────────────────────────────────
SECTION 2: CONTACT INFORMATION
─────────────────────────────────────────────────────────────────

Phone Number:
[Content Control - key: phone_number]


Street Address:
[Content Control - key: street_address]


City, State, ZIP:
[Content Control - key: city_state_zip]


Country:
[Content Control (Combo Box) - key: country]
Options: USA, Canada, UK, Other


─────────────────────────────────────────────────────────────────
SECTION 3: PREFERENCES
─────────────────────────────────────────────────────────────────

Gender:
[Content Control (Dropdown) - key: gender]
Options: Male, Female, Other, Prefer not to say


Preferred Contact Method:
☐ Email  [Checkbox - key: contact_email]
☐ Phone  [Checkbox - key: contact_phone]
☐ Mail   [Checkbox - key: contact_mail]


─────────────────────────────────────────────────────────────────
SECTION 4: AGREEMENT
─────────────────────────────────────────────────────────────────

☐ [Checkbox - key: agree_terms] 
  I have read and agree to the Terms & Conditions


☐ [Checkbox - key: agree_privacy]
  I understand the Privacy Policy


─────────────────────────────────────────────────────────────────

Signature: ___________________________  Date: _______________

═════════════════════════════════════════════════════════════════
```

## Content Control Configuration Examples

### Example 1: Plain Text Control

**Key Steps:**
1. Position cursor after "Full Name: "
2. Developer → Plain Text Content Control
3. Right-click → Properties
4. Title: `full_name`
5. Click OK
6. Exit Design Mode

**Result:** Users can type text in this field

---

### Example 2: Date Picker Control

**Key Steps:**
1. Position cursor after "Date of Birth: "
2. Developer → Date Picker Content Control
3. Right-click → Properties
4. Title: `birth_date`
5. Display the date as: Choose format (e.g., M/d/yyyy)
6. Click OK
7. Exit Design Mode

**Result:** Users click to select from a calendar

---

### Example 3: Dropdown List

**Key Steps:**
1. Position cursor after "Gender: "
2. Developer → Drop-Down List Content Control
3. Right-click → Properties
4. Title: `gender`
5. Click **Remove** to delete placeholder items
6. Click **Add** to add options:
   - Display Name: `Male` → Value: `Male`
   - Display Name: `Female` → Value: `Female`
   - Display Name: `Other` → Value: `Other`
7. Click OK
8. Exit Design Mode

**Result:** Users select from predefined options

---

### Example 4: Checkbox Control

**Key Steps:**
1. Position cursor before "I agree to the terms"
2. Developer → Checkbox Content Control
3. Right-click → Properties
4. Title: `agree_terms`
5. Unchecked Symbol: Choose a symbol (default ☐)
6. Checked Symbol: Choose a symbol (default ☒)
7. Click OK
8. Exit Design Mode

**Result:** Users click to check/uncheck

---

## Tips & Best Practices

### ✅ Do's

- ✅ Use meaningful, lowercase keys (e.g., "full_name", "email_address")
- ✅ Use consistent naming conventions (snake_case)
- ✅ Add placeholder text inside controls showing format (e.g., "MM/DD/YYYY")
- ✅ Group related fields in sections
- ✅ Mark required fields with asterisk (*)
- ✅ Test all controls before distribution
- ✅ Keep field names short but descriptive

### ❌ Don'ts

- ❌ Don't use spaces in control keys
- ❌ Don't use special characters in keys (@, #, $, %)
- ❌ Don't nest content controls inside each other
- ❌ Don't use form fields (older technology) - use content controls instead
- ❌ Don't forget to exit Design Mode before saving
- ❌ Don't change keys after collecting data

## Troubleshooting

### Problem: Content controls are not visible

**Solution:** You may be in Design Mode. Click Developer → Design Mode to toggle it.

### Problem: Can't edit content control properties

**Solution:** Exit Design Mode first (Developer → Design Mode toggle), then try again.

### Problem: Content controls appear as gray boxes with text

**Solution:** This is normal. In Design Mode, controls show selection handles. In normal mode, they appear as fields.

### Problem: Users can't fill in the controls

**Solution:** Make sure Design Mode is OFF and the document is not in Read-Only mode.

### Problem: "Developer tab is missing"

**Solution:** See Step 1 section above to enable Developer tab.

## Using the Template with DocxExtractor

Once your template is created:

1. Save it as `.docx` format
2. Use with the C# application:

```csharp
// Extract from your template
var fields = ContentExtractor.ExtractFields("your-template.docx");

// View the extracted keys
foreach (var field in fields)
{
    Console.WriteLine($"Key: {field.Key}, Label: {field.Label}");
}
```

3. The extracted keys will match your control Title values
4. When users fill the document, data is extracted and exported to JSON

## Advanced: Protecting the Template

To prevent users from accidentally deleting controls:

1. Developer → **Design Mode** (OFF)
2. Developer → **Restrict Editing**
3. Check "Limit formatting to a selection of styles"
4. Set password (optional)
5. Click **Start Enforcing Protection**

Users can now only edit inside content controls, not the form structure.

## Additional Resources

- [Microsoft Word Content Controls Documentation](https://support.microsoft.com/en-us/office/overview-of-content-controls-e2a78dcc-5f76-4fb1-9ab2-73d4a1d37e6a)
- [OpenXML Standard Documentation](https://www.ecma-international.org/publications-and-standards/standards/ecma-376/)
- [DocumentFormat.OpenXml GitHub](https://github.com/OfficeDev/Open-XML-SDK)

---

**Next Step:** Once you've created your template, use the `ContentExtractor.ExtractFields()` method to read the filled data!
