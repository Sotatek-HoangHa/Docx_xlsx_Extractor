using DocxExtractor.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DocxExtractor.Services;

/// <summary>
/// Applies extraction filters to field values based on their detected or explicit type.
/// </summary>
public class FieldFilterService
{
    private readonly ExtractionFilter? _filter;
    private readonly FieldValidator _validator;

    public FieldFilterService(ExtractionFilter? filter = null)
    {
        _filter = filter;
        _validator = new FieldValidator();
    }

    /// <summary>
    /// Filters a list of extracted fields based on the active filter profile.
    /// Returns only fields that pass the filter.
    /// </summary>
    public List<FieldValue> ApplyFilter(List<FieldValue> fields)
    {
        if (_filter == null)
        {
            // No filter applied, return all fields
            return fields;
        }

        return fields
            .Where(field => ShouldIncludeField(field))
            .ToList();
    }

    /// <summary>
    /// Determines if a field should be included based on the filter profile.
    /// </summary>
    private bool ShouldIncludeField(FieldValue field)
    {
        if (_filter == null)
            return true;

        // Detect field type from key/label
        var fieldType = DetectFieldType(field.Key, field.Label);

        // Apply filter logic
        return _filter.ShouldExtractField(fieldType.ToString());
    }

    /// <summary>
    /// Detects field type from key and label (same logic as FieldValidator).
    /// </summary>
    private FieldType DetectFieldType(string key, string label)
    {
        var keyLower = key.Split(':')[0].ToLower();
        var labelLower = label.ToLower();

        if (keyLower.Contains("date") || keyLower.Contains("birth") ||
            labelLower.Contains("date") || labelLower.Contains("birth"))
            return FieldType.Date;

        if (keyLower.Contains("email") || labelLower.Contains("email"))
            return FieldType.Email;

        if (keyLower.Contains("phone") || keyLower.Contains("mobile") ||
            labelLower.Contains("phone") || labelLower.Contains("mobile"))
            return FieldType.Phone;

        if (keyLower.Contains("url") || keyLower.Contains("website") ||
            labelLower.Contains("url") || labelLower.Contains("website"))
            return FieldType.Url;

        if (keyLower.Contains("gender") || keyLower.Contains("sex") ||
            labelLower.Contains("gender") || labelLower.Contains("sex"))
            return FieldType.Gender;

        if (keyLower.Contains("country") || keyLower.Contains("region") ||
            labelLower.Contains("country") || labelLower.Contains("region"))
            return FieldType.Country;

        if (keyLower.Contains("agree") || keyLower.Contains("accept") || keyLower.Contains("confirm") ||
            labelLower.Contains("agree") || labelLower.Contains("accept") || labelLower.Contains("confirm"))
            return FieldType.Boolean;

        return FieldType.Text;
    }

    /// <summary>
    /// Gets information about the active filter.
    /// </summary>
    public string GetFilterInfo()
    {
        if (_filter == null)
            return "No filter applied - all fields will be extracted";

        var info = new System.Text.StringBuilder();
        info.AppendLine($"Filter Profile: {_filter.ProfileName}");

        if (_filter.IncludeFieldTypes.Count > 0)
        {
            info.AppendLine($"Include Types: {string.Join(", ", _filter.IncludeFieldTypes)}");
        }

        if (_filter.ExcludeFieldTypes.Count > 0)
        {
            info.AppendLine($"Exclude Types: {string.Join(", ", _filter.ExcludeFieldTypes)}");
        }

        return info.ToString();
    }
}

/// <summary>
/// Field type enumeration (same as FieldValidator).
/// </summary>
internal enum FieldType
{
    Text,
    Date,
    Email,
    Phone,
    Url,
    Gender,
    Country,
    Boolean
}
