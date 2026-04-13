# DOCX Content Controls Extractor

A C# (.NET Core 8) solution for creating DOCX templates with content controls (blank fields) and extracting user-filled data as JSON.

## Overview

This project demonstrates how to:
1. **Create** a DOCX template with named content controls (structured document tags)
2. **Fill** the template with user data programmatically
3. **Extract** all filled data and export it as JSON

## Features

- ✅ Programmatically create DOCX templates with content controls
- ✅ Support for inline and block-level content controls
- ✅ Extract data with key-value pairs
- ✅ Export extracted data as JSON
- ✅ Display results in table format
- ✅ Full type safety with C# models

## Project Structure

```
DocxExtractor/
├── DocxExtractor.csproj              # Project configuration
├── Program.cs                        # Entry point & demo
├── Models/
│   └── FieldValue.cs                 # Extracted field model
├── Services/
│   ├── TemplateCreator.cs            # Creates .docx templates
│   └── ContentExtractor.cs           # Extracts filled data
├── sample-template.docx              # Generated template
├── sample-template-filled.docx       # Template with filled data
└── extracted-fields.json             # Extracted data as JSON
```

## Getting Started

### Prerequisites

- .NET Core 8.0 SDK or later
- Any text editor or IDE (VS Code, Visual Studio, etc.)

### Building the Project

```bash
cd C:\Users\admin\DocxExtractor
dotnet build
```

### Running the Demo

```bash
dotnet run
```

This will:
1. Create a sample template: `sample-template.docx`
2. Fill it with sample data: `sample-template-filled.docx`
3. Extract the data and display it in JSON and table format
4. Save JSON output to: `extracted-fields.json`

## Usage

### 1. Creating a Template Programmatically

```csharp
using DocxExtractor.Services;

// Create a template with content controls
TemplateCreator.CreateSampleTemplate("my-template.docx");
```

### 2. Extracting Data from a Filled Template

```csharp
using DocxExtractor.Services;

// Extract all fields from a filled template
List<FieldValue> fields = ContentExtractor.ExtractFields("my-filled-template.docx");

// Convert to JSON
var json = JsonSerializer.Serialize(fields, new JsonSerializerOptions { WriteIndented = true });
Console.WriteLine(json);
```

### 3. Output Format

The extracted data is formatted as JSON:

```json
[
  {
    "Key": "full_name",
    "Label": "Full Name",
    "Value": "John Doe",
    "Type": "Content Control"
  },
  {
    "Key": "email_address",
    "Label": "Email Address",
    "Value": "john@example.com",
    "Type": "Content Control"
  }
]
```

## Creating Templates Manually in Microsoft Word

### Step 1: Create a Document

1. Open Microsoft Word
2. Create a new document with your form layout

### Step 2: Insert Content Controls

For each blank field you want to add:

1. **Position cursor** where you want the blank field
2. **Go to Developer Tab**
   - If you don't see Developer tab: File → Options → Customize Ribbon → Check "Developer"
3. **Click "Design Mode"** (to enable editing mode)
4. **Insert Content Control**
   - Plain Text: Developer → Plain Text Content Control
   - Rich Text: Developer → Rich Text Content Control
   - Combo Box: Developer → Combo Box Content Control
   - Checkbox: Developer → Checkbox Content Control
5. **Set Control Properties**
   - Right-click the control → Properties
   - Set the **Title** (machine key, e.g., "full_name")
   - Set the **Tag** (optional, display label)
6. **Click "Design Mode"** again to exit editing mode

### Step 3: Save the Template

Save the document as `.docx` format.

### Example Template Structure

```
═══════════════════════════════════════
         APPLICATION FORM
═══════════════════════════════════════

Full Name: [Content Control - key: "full_name"]

Email Address: [Content Control - key: "email_address"]

Date of Birth: [Content Control - key: "birth_date"]

Address: [Content Control - key: "address"]

I agree to the terms: [Checkbox - key: "agree_terms"]

═══════════════════════════════════════
```

## API Reference

### FieldValue Class

Represents a single extracted field:

```csharp
public class FieldValue
{
    public string Key { get; set; }        // Machine key (from content control Tag)
    public string Label { get; set; }      // Human label (from content control Alias)
    public string Value { get; set; }      // Filled value
    public string Type { get; set; }       // Control type
}
```

### TemplateCreator.CreateSampleTemplate

Creates a sample DOCX template with multiple content controls:

```csharp
public static void CreateSampleTemplate(string filePath)
```

### ContentExtractor.ExtractFields

Extracts all content control data from a filled DOCX file:

```csharp
public static List<FieldValue> ExtractFields(string filePath)
```

## Integration with Your Application

### 1. Copy to Your Project

Copy the `Models/` and `Services/` folders to your project.

### 2. Add NuGet Package

```bash
dotnet add package DocumentFormat.OpenXml
```

### 3. Use in Your Code

```csharp
using DocxExtractor.Services;
using DocxExtractor.Models;
using System.Text.Json;

// Extract data
List<FieldValue> fields = ContentExtractor.ExtractFields("user-form.docx");

// Convert to JSON
string json = JsonSerializer.Serialize(fields, new JsonSerializerOptions { WriteIndented = true });

// Use the data
foreach (var field in fields)
{
    Console.WriteLine($"{field.Label}: {field.Value}");
}
```

## Advanced Usage

### Handling Different Content Control Types

Content controls can be various types:
- **Plain Text**: Simple text input
- **Rich Text**: Formatted text (bold, italic, etc.)
- **Date Picker**: Calendar date selection
- **Dropdown List**: Fixed list of options
- **Combo Box**: Dropdown + manual input
- **Checkbox**: Boolean checkbox

The extractor identifies the control type and includes it in the output.

### Processing Large Batches

```csharp
// Process multiple DOCX files
var files = Directory.GetFiles(@"C:\forms", "*.docx");

foreach (var file in files)
{
    var fields = ContentExtractor.ExtractFields(file);
    
    // Process fields (save to database, etc.)
    ProcessFields(fields, Path.GetFileNameWithoutExtension(file));
}
```

## Troubleshooting

### Empty Values After Extraction

**Problem**: Extracted values are empty even though the template was filled.

**Solution**: Ensure the content is inside the content control bounds. In Word, the blue outline shows the control boundaries.

### Content Control Not Recognized

**Problem**: Some content controls are not being extracted.

**Solution**: Ensure you're using native Word content controls (not form fields). Form fields and content controls are different in the OpenXML structure.

### File Not Found Error

**Problem**: FileNotFoundException when trying to extract.

**Solution**: Verify the file path is correct and the file exists:
```csharp
if (File.Exists(filePath))
{
    var fields = ContentExtractor.ExtractFields(filePath);
}
```

## NuGet Dependencies

- **DocumentFormat.OpenXml** (v3.1.0+) - Official Microsoft library for working with Office Open XML formats
- **System.Text.Json** - Built-in with .NET 8 for JSON serialization

## License

This example is provided as-is for educational purposes.

## See Also

- [DocumentFormat.OpenXml Documentation](https://github.com/OfficeDev/Open-XML-SDK)
- [Office Open XML Standard](https://www.ecma-international.org/publications-and-standards/standards/ecma-376/)
- [Microsoft Word Content Controls](https://support.microsoft.com/en-us/office/overview-of-content-controls-e2a78dcc-5f76-4fb1-9ab2-73d4a1d37e6a)
