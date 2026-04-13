namespace DocxExtractor.Models;

/// <summary>
/// Represents a single extracted field from a DOCX content control.
/// </summary>
public class FieldValue
{
    /// <summary>
    /// Machine-readable key of the content control (from Tag property)
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable label of the content control (from Alias property)
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// The filled value by the user
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Type of content control (PlainText, RichText, Date, Dropdown, CheckBox, etc.)
    /// </summary>
    public string Type { get; set; } = string.Empty;
}
