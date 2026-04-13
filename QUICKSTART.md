# Quick Start Guide

## 5-Minute Setup

### 1. Build the Project
```bash
cd C:\Users\admin\DocxExtractor
dotnet build
```

### 2. Run the Demo
```bash
dotnet run
```

**Output:**
- `sample-template.docx` — Generated template with 7 content controls
- `sample-template-filled.docx` — Template filled with sample data
- `extracted-fields.json` — Extracted data as JSON

### 3. View Results

Check `extracted-fields.json`:
```json
[
  {"Key": "full_name", "Label": "Full Name", "Value": "John Michael Doe", "Type": "Content Control"},
  {"Key": "email_address", "Label": "Email Address", "Value": "john.doe@example.com", "Type": "Content Control"},
  ...
]
```

## Usage in Your Code

```csharp
using DocxExtractor.Services;
using System.Text.Json;

// Extract from any .docx template
var fields = ContentExtractor.ExtractFields("your-form.docx");

// Export to JSON
var json = JsonSerializer.Serialize(fields, new JsonSerializerOptions { WriteIndented = true });
Console.WriteLine(json);
```

## Create Your Own Template

### Option A: Use the Programmatic API

```csharp
// Creates a template with blank content controls
TemplateCreator.CreateSampleTemplate("my-template.docx");
```

### Option B: Create Manually in Word

See **WORD_TEMPLATE_GUIDE.md** for step-by-step instructions.

## Project Files

| File | Purpose |
|------|---------|
| `Program.cs` | Demo application |
| `Models/FieldValue.cs` | Data model |
| `Services/TemplateCreator.cs` | Creates templates |
| `Services/ContentExtractor.cs` | Extracts data |
| `README.md` | Full documentation |
| `WORD_TEMPLATE_GUIDE.md` | Word template creation guide |

## Next Steps

1. ✅ Build and run demo
2. ✅ Review generated DOCX files
3. ✅ Create your own template using Word
4. ✅ Use `ContentExtractor.ExtractFields()` to read data
5. ✅ Integrate into your application

## Key Features

- ✅ Create templates with content controls
- ✅ Extract key-value pairs from filled templates
- ✅ Export to JSON format
- ✅ Full C# type safety
- ✅ Works with .NET Core 8

## Common Tasks

### Extract from a filled template
```csharp
var fields = ContentExtractor.ExtractFields("form.docx");
foreach (var field in fields)
    Console.WriteLine($"{field.Label}: {field.Value}");
```

### Create template programmatically
```csharp
TemplateCreator.CreateSampleTemplate("template.docx");
```

### Serialize to JSON
```csharp
var json = JsonSerializer.Serialize(fields, 
    new JsonSerializerOptions { WriteIndented = true });
File.WriteAllText("output.json", json);
```

---

**For detailed documentation, see README.md**

**For Word template creation, see WORD_TEMPLATE_GUIDE.md**
