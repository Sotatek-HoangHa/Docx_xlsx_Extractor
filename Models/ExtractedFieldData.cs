using System;

namespace DocxExtractor.Models;

/// <summary>
/// Represents extracted field data stored in the database for reporting.
/// </summary>
public class ExtractedFieldData
{
    public int Id { get; set; }
    public string DocumentName { get; set; } = string.Empty;
    public string? FilterProfileName { get; set; }
    public string FieldKey { get; set; } = string.Empty;
    public string FieldLabel { get; set; } = string.Empty;
    public string FieldValue { get; set; } = string.Empty;
    public string FieldType { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public string? ValidationMessage { get; set; }
    public DateTime ExtractedAt { get; set; }
}
