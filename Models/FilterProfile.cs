using System;
using System.Collections.Generic;
using System.Linq;

namespace DocxExtractor.Models;

/// <summary>
/// Represents a filter profile that contains include/exclude rules for field extraction.
/// </summary>
public class FilterProfile
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation property
    public ICollection<FilterRule> IncludeRules { get; set; } = new List<FilterRule>();
    public ICollection<FilterRule> ExcludeRules { get; set; } = new List<FilterRule>();
}

/// <summary>
/// Represents a single filter rule (include or exclude a field type).
/// </summary>
public class FilterRule
{
    public int Id { get; set; }
    public int FilterProfileId { get; set; }
    public string FieldType { get; set; } = string.Empty;  // e.g., "email", "datetime", "phone"
    public bool IsIncludeRule { get; set; }  // true = include, false = exclude
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Filter configuration for field extraction.
/// </summary>
public class ExtractionFilter
{
    public int ProfileId { get; set; }
    public string ProfileName { get; set; } = string.Empty;
    public List<string> IncludeFieldTypes { get; set; } = new List<string>();
    public List<string> ExcludeFieldTypes { get; set; } = new List<string>();

    /// <summary>
    /// Determines if a field should be extracted based on the filter rules.
    /// </summary>
    public bool ShouldExtractField(string fieldType)
    {
        var fieldTypeLower = fieldType.ToLower();

        // If include rules exist, field must be in the include list
        if (IncludeFieldTypes.Count > 0)
        {
            if (!IncludeFieldTypes.Any(x => x.ToLower() == fieldTypeLower))
                return false;
        }

        // Field must not be in exclude list
        if (ExcludeFieldTypes.Count > 0)
        {
            if (ExcludeFieldTypes.Any(x => x.ToLower() == fieldTypeLower))
                return false;
        }

        return true;
    }
}
