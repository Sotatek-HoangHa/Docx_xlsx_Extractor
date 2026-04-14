using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocxExtractor.Models;
using DocxExtractor.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

const string OUTPUT_TEMPLATE_PATH = "sample-template.docx";
const string FILLED_TEMPLATE_PATH = "sample-template-filled.docx";

// Database connection string
const string POSTGRES_CONNECTION = "Host=localhost;Port=5432;Database=postgres;Username=admin;Password=admin123";

Console.WriteLine("=== DOCX Content Controls Extractor with Database Filtering ===\n");

// Step 0: Initialize database and filter profiles
Console.WriteLine("Step 0: Initializing database and filter profiles...");
try
{
    InitializeDatabase(POSTGRES_CONNECTION);
    Console.WriteLine($"✓ Database initialized\n");
}
catch (Exception ex)
{
    Console.WriteLine($"⚠ Database connection failed: {ex.GetBaseException().Message}");
    Console.WriteLine($"✓ Continuing without database filtering\n");
}

// Step 1: Create a sample template with content controls
Console.WriteLine("Step 1: Creating sample template with content controls...");
TemplateCreator.CreateSampleTemplate(OUTPUT_TEMPLATE_PATH);
Console.WriteLine($"✓ Template created: {OUTPUT_TEMPLATE_PATH}\n");

// Step 2: Simulate user filling the template
Console.WriteLine("Step 2: Simulating user filling in the template...");
FillTemplateWithSampleData(OUTPUT_TEMPLATE_PATH, FILLED_TEMPLATE_PATH);
Console.WriteLine($"✓ Filled template created: {FILLED_TEMPLATE_PATH}\n");

// Step 3: Extract all filled fields as JSON
Console.WriteLine("Step 3: Extracting fields from filled template...");
List<FieldValue> extractedFields = ContentExtractor.ExtractFields(FILLED_TEMPLATE_PATH);
Console.WriteLine($"✓ Extracted {extractedFields.Count} fields\n");

// Step 4: Validate all fields
Console.WriteLine("Step 4: Validating field values...");
FieldValidator.ValidateFields(extractedFields);
Console.WriteLine($"✓ Validation completed\n");

// Step 5: Apply filter
Console.WriteLine("Step 5: Applying filter from database...");
List<FieldValue> filteredFields = extractedFields;
try
{
    var filterService = new FilterService(POSTGRES_CONNECTION);
    var activeFilter = filterService.GetActiveFilterProfile();
    var fieldFilterService = new FieldFilterService(activeFilter);
    filteredFields = fieldFilterService.ApplyFilter(extractedFields);

    if (activeFilter != null)
    {
        Console.WriteLine($"✓ Filter '{activeFilter.ProfileName}' applied");
        Console.WriteLine($"  Original fields: {extractedFields.Count}");
        Console.WriteLine($"  Filtered fields: {filteredFields.Count}\n");
        Console.WriteLine(fieldFilterService.GetFilterInfo());
    }
    else
    {
        Console.WriteLine("✓ No active filter - all fields extracted\n");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"⚠ Could not apply database filter: {ex.GetBaseException().Message}");
    Console.WriteLine("✓ Using all extracted fields without filtering\n");
}

// Step 6: Display results in JSON format
Console.WriteLine("=== EXTRACTED DATA (JSON FORMAT - AFTER FILTERING) ===\n");
DisplayAsJson(filteredFields);

// Step 7: Display as table for better readability
Console.WriteLine("\n=== EXTRACTED DATA (TABLE FORMAT - AFTER FILTERING) ===\n");
DisplayAsTable(filteredFields);

Console.WriteLine("\n✓ Complete! Check the generated DOCX files in the current directory.");

/// <summary>
/// Initializes the database and creates sample filter profiles if they don't exist.
/// </summary>
static void InitializeDatabase(string connectionString)
{
    var filterService = new FilterService(connectionString);
    filterService.InitializeDatabase();

    // Create sample filter profiles if they don't exist
    var existingProfiles = filterService.GetAllFilterProfiles();

    if (existingProfiles.Count == 0)
    {
        // Profile 1: Include only contact information (email, phone)
        var includeContact = new System.Collections.Generic.List<string> { "Email", "Phone" };
        var excludeEmpty1 = new System.Collections.Generic.List<string>();
        filterService.CreateFilterProfile(
            name: "contact_info",
            description: "Extract only email and phone fields",
            includeTypes: includeContact,
            excludeTypes: excludeEmpty1
        );

        // Profile 2: Exclude sensitive data (email, phone)
        var includeEmpty2 = new System.Collections.Generic.List<string>();
        var excludeSensitive = new System.Collections.Generic.List<string> { "Email", "Phone" };
        filterService.CreateFilterProfile(
            name: "no_sensitive",
            description: "Extract all fields except email and phone",
            includeTypes: includeEmpty2,
            excludeTypes: excludeSensitive
        );

        // Profile 3: Date and personal info only
        var includeDate = new System.Collections.Generic.List<string> { "Date" };
        var excludeEmpty3 = new System.Collections.Generic.List<string>();
        filterService.CreateFilterProfile(
            name: "dates_only",
            description: "Extract only date fields",
            includeTypes: includeDate,
            excludeTypes: excludeEmpty3
        );

        // Activate the first profile by default
        filterService.ActivateFilterProfile("contact_info");
    }
}

/// <summary>
/// Fills the template with sample data by opening and modifying content controls.
/// </summary>
static void FillTemplateWithSampleData(string templatePath, string outputPath)
{
    // Copy template to output
    File.Copy(templatePath, outputPath, overwrite: true);

    using (WordprocessingDocument doc = WordprocessingDocument.Open(outputPath, true))
    {
        MainDocumentPart? mainPart = doc.MainDocumentPart;
        if (mainPart?.Document.Body == null)
        {
            return;
        }

        Body body = mainPart.Document.Body;

        // Sample data
        Dictionary<string, string> sampleData = new Dictionary<string, string>
        {
            { "full_name", "John Michael Doe" },
            { "email_address", "john.doe@examplecom" },
            { "birth_date", "195-15" },
            { "gender", "Male" },
            { "country", "United States" },
            { "address", "456 Side Street, Springfield, IL 62701, USA" },
            { "agree_terms", "13124" }
        };

        FillContentControls(body, sampleData);
        mainPart.Document.Save();
    }
}

/// <summary>
/// Fills all content controls with sample data.
/// </summary>
static void FillContentControls(Body body, Dictionary<string, string> data)
{
    foreach (var element in body.Elements().ToList())
    {
        FillElementContent(element, data);
    }
}

/// <summary>
/// Recursively fills content controls.
/// </summary>
static void FillElementContent(OpenXmlElement element, Dictionary<string, string> data)
{
    // Handle block-level SDTs
    if (element is SdtBlock sdtBlock)
    {
        string? key = sdtBlock.SdtProperties?.Elements<Tag>()
            .FirstOrDefault()?.Val?.Value;

        if (key != null && data.TryGetValue(key, out var value))
        {
            FillSdtContent(sdtBlock, value);
        }
    }
    // Handle paragraph SDTs
    else if (element is Paragraph paragraph)
    {
        foreach (var run in paragraph.Elements<OpenXmlElement>().ToList())
        {
            if (run is SdtRun sdtRun)
            {
                string? key = sdtRun.SdtProperties?.Elements<Tag>()
                    .FirstOrDefault()?.Val?.Value;

                if (key != null && data.TryGetValue(key, out var value))
                {
                    FillSdtRunContent(sdtRun, value);
                }
            }
        }
    }
}

/// <summary>
/// Fills a block-level SDT with text.
/// </summary>
static void FillSdtContent(SdtBlock sdtBlock, string text)
{
    // Clear existing content except properties
    foreach (var child in sdtBlock.Elements().ToList())
    {
        if (child is not SdtProperties)
        {
            child.Remove();
        }
    }

    // Add new paragraph with text
    Paragraph para = new Paragraph();
    Run run = new Run();
    run.AppendChild(new Text { Text = text, Space = SpaceProcessingModeValues.Preserve });
    para.AppendChild(run);
    sdtBlock.AppendChild(para);
}

/// <summary>
/// Fills a run-level (inline) SDT with text.
/// </summary>
static void FillSdtRunContent(SdtRun sdtRun, string text)
{
    // Clear existing content except properties
    foreach (var child in sdtRun.Elements().ToList())
    {
        if (child is not SdtProperties)
        {
            child.Remove();
        }
    }

    // Add new run with text, wrapped in SdtContentRun
    SdtContentRun sdtContent = new SdtContentRun();
    Run run = new Run();
    run.AppendChild(new Text { Text = text, Space = SpaceProcessingModeValues.Preserve });
    sdtContent.AppendChild(run);
    sdtRun.AppendChild(sdtContent);
}

/// <summary>
/// Displays extracted fields as JSON.
/// </summary>
static void DisplayAsJson(List<FieldValue> fields)
{
    var options = new JsonSerializerOptions
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    string json = JsonSerializer.Serialize(fields, options);
    Console.WriteLine(json);

    // Save to file
    File.WriteAllText("extracted-fields.json", json);
    Console.WriteLine($"\n✓ JSON saved to: extracted-fields.json");
}

/// <summary>
/// Displays extracted fields in a formatted table.
/// </summary>
static void DisplayAsTable(List<FieldValue> fields)
{
    if (fields.Count == 0)
    {
        Console.WriteLine("No fields extracted.");
        return;
    }

    // Calculate column widths
    int keyWidth = Math.Max(fields.Max(f => f.Key.Length), 10);
    int labelWidth = Math.Max(fields.Max(f => f.Label.Length), 12);
    int valueWidth = Math.Max(fields.Max(f => f.Value.Length), 15);
    int typeWidth = Math.Max(fields.Max(f => f.Type.Length), 10);

    // Print header
    PrintTableRow(
        "Key".PadRight(keyWidth),
        "Label".PadRight(labelWidth),
        "Value".PadRight(valueWidth),
        "Type".PadRight(typeWidth));

    PrintSeparator(keyWidth, labelWidth, valueWidth, typeWidth);

    // Print rows
    foreach (var field in fields)
    {
        PrintTableRow(
            field.Key.PadRight(keyWidth),
            field.Label.PadRight(labelWidth),
            (field.Value.Length > valueWidth ? field.Value.Substring(0, valueWidth - 3) + "..." : field.Value).PadRight(valueWidth),
            field.Type.PadRight(typeWidth));
    }
}

/// <summary>
/// Prints a formatted table row.
/// </summary>
static void PrintTableRow(string col1, string col2, string col3, string col4)
{
    Console.WriteLine($"| {col1} | {col2} | {col3} | {col4} |");
}

/// <summary>
/// Prints a table separator.
/// </summary>
static void PrintSeparator(int col1Width, int col2Width, int col3Width, int col4Width)
{
    Console.WriteLine($"+{new string('-', col1Width + 2)}+{new string('-', col2Width + 2)}+{new string('-', col3Width + 2)}+{new string('-', col4Width + 2)}+");
}
