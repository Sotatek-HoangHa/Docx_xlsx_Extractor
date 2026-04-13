using DocxExtractor.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace DocxExtractor.Services;

/// <summary>
/// Validates content control field values using explicit type tags or fallback detection.
/// Supports tag format: "fieldName:fieldType:additionalInfo"
/// Example: "birth_date:datetime", "email_address:email", "gender:enum:Male|Female|Other"
/// </summary>
public class FieldValidator
{
    // Regex patterns for validation
    private static class ValidationPatterns
    {
        // Date: YYYY-MM-DD, MM/DD/YYYY, DD.MM.YYYY, etc.
        public const string DatePattern = @"^(\d{4}[-/]\d{2}[-/]\d{2}|\d{1,2}[-/.]\d{1,2}[-/.]\d{2,4}|[A-Za-z]+\s+\d{1,2},?\s+\d{4})$";

        // Email: user@domain.com format
        public const string EmailPattern = @"^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$";

        // Phone: 10+ digits with optional formatting
        public const string PhonePattern = @"^\+?[\d\s\-()]+$";

        // URL: http:// or https://
        public const string UrlPattern = @"^https?://[^\s/$.?#].[^\s]*$";

        // Gender: common gender values
        public const string GenderPattern = @"^(male|female|other|n/a|prefer\s+not\s+to\s+say|non-binary)$";

        // Country: 2+ letters (not pure numbers)
        public const string CountryPattern = @"^[a-zA-Z\s]{2,}$";

        // Boolean: yes, no, true, false, 1, 0, checked, unchecked
        public const string BooleanPattern = @"^(yes|no|true|false|1|0|checked|unchecked)$";

        // Text: any non-empty value
        public const string TextPattern = @".+";
    }

    /// <summary>
    /// Validates a list of extracted fields and appends validation messages to values.
    /// Supports explicit type tags in key format: "fieldName:fieldType"
    /// </summary>
    public static void ValidateFields(List<FieldValue> fields)
    {
        foreach (var field in fields)
        {
            ValidateField(field);
        }
    }

    /// <summary>
    /// Validates a single field and appends validation message if invalid.
    /// Parses explicit type from tag format: "fieldName:fieldType:additionalInfo"
    /// Falls back to key/label detection if no explicit type specified.
    /// </summary>
    private static void ValidateField(FieldValue field)
    {
        if (string.IsNullOrWhiteSpace(field.Value))
        {
            return; // Skip empty fields
        }

        // Try to extract explicit type from key (tag format)
        var fieldType = ExtractFieldTypeFromTag(field.Key);

        // Fallback to detecting type from key/label if no explicit type
        if (fieldType == FieldType.Text && !IsExplicitlyTagged(field.Key))
        {
            fieldType = DetectFieldTypeFromKeyAndLabel(field.Key, field.Label);
        }

        var validationResult = ValidateByType(fieldType, field.Value);

        if (!validationResult.IsValid)
        {
            field.Value = $"{field.Value} - {validationResult.Message}";
        }
    }

    /// <summary>
    /// Extracts field type from tag format: "fieldName:fieldType:additionalInfo"
    /// Example: "birth_date:datetime" → FieldType.Date
    /// </summary>
    private static FieldType ExtractFieldTypeFromTag(string tag)
    {
        if (string.IsNullOrEmpty(tag) || !tag.Contains(':'))
            return FieldType.Text; // No explicit type

        var parts = tag.Split(':');
        if (parts.Length < 2)
            return FieldType.Text;

        string typeString = parts[1].ToLower().Trim();

        return typeString switch
        {
            "date" or "datetime" => FieldType.Date,
            "email" => FieldType.Email,
            "phone" => FieldType.Phone,
            "url" or "website" => FieldType.Url,
            "gender" => FieldType.Gender,
            "country" or "region" => FieldType.Country,
            "boolean" or "checkbox" => FieldType.Boolean,
            _ => FieldType.Text
        };
    }

    /// <summary>
    /// Checks if the tag has explicit type specification (contains ':').
    /// </summary>
    private static bool IsExplicitlyTagged(string tag)
    {
        return !string.IsNullOrEmpty(tag) && tag.Contains(':');
    }

    /// <summary>
    /// Detects field type from key and label (fallback method).
    /// </summary>
    private static FieldType DetectFieldTypeFromKeyAndLabel(string key, string label)
    {
        var keyLower = key.Split(':')[0].ToLower(); // Use only field name if tagged
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
    /// Validates a value based on its type using regex patterns and type-specific rules.
    /// </summary>
    private static ValidationResult ValidateByType(FieldType fieldType, string value)
    {
        return fieldType switch
        {
            FieldType.Date => ValidateDateWithRegex(value),
            FieldType.Email => ValidateEmailWithRegex(value),
            FieldType.Phone => ValidatePhoneWithRegex(value),
            FieldType.Url => ValidateUrlWithRegex(value),
            FieldType.Gender => ValidateGenderWithRegex(value),
            FieldType.Country => ValidateCountryWithRegex(value),
            FieldType.Boolean => ValidateBooleanWithRegex(value),
            _ => ValidationResult.Valid()
        };
    }

    /// <summary>
    /// Validates date using regex pattern and DateTime parsing.
    /// Supports: YYYY-MM-DD, MM/DD/YYYY, DD.MM.YYYY, MMM DD, YYYY, etc.
    /// </summary>
    private static ValidationResult ValidateDateWithRegex(string value)
    {
        // First check regex pattern
        if (!Regex.IsMatch(value, ValidationPatterns.DatePattern))
            return ValidationResult.Invalid("invalid date format");

        // Then try parsing to ensure it's a valid date
        var formats = new[]
        {
            "yyyy-MM-dd", "MM/dd/yyyy", "dd.MM.yyyy", "dd/MM/yyyy",
            "yyyy/MM/dd", "dd-MM-yyyy", "MMM dd, yyyy", "MMMM dd, yyyy"
        };

        if (DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            return ValidationResult.Valid();

        return ValidationResult.Invalid("invalid date format");
    }

    /// <summary>
    /// Validates email using regex pattern.
    /// </summary>
    private static ValidationResult ValidateEmailWithRegex(string value)
    {
        if (Regex.IsMatch(value, ValidationPatterns.EmailPattern))
            return ValidationResult.Valid();

        return ValidationResult.Invalid("invalid email format");
    }

    /// <summary>
    /// Validates phone using regex pattern and digit count.
    /// Requires at least 10 digits.
    /// </summary>
    private static ValidationResult ValidatePhoneWithRegex(string value)
    {
        if (!Regex.IsMatch(value, ValidationPatterns.PhonePattern))
            return ValidationResult.Invalid("invalid phone format");

        int digitCount = value.Count(char.IsDigit);
        if (digitCount < 10)
            return ValidationResult.Invalid("phone must have at least 10 digits");

        return ValidationResult.Valid();
    }

    /// <summary>
    /// Validates URL using regex pattern.
    /// Requires http:// or https://
    /// </summary>
    private static ValidationResult ValidateUrlWithRegex(string value)
    {
        if (Regex.IsMatch(value, ValidationPatterns.UrlPattern, RegexOptions.IgnoreCase))
            return ValidationResult.Valid();

        return ValidationResult.Invalid("invalid URL format");
    }

    /// <summary>
    /// Validates gender value using regex pattern.
    /// Valid values: male, female, other, n/a, prefer not to say, non-binary
    /// </summary>
    private static ValidationResult ValidateGenderWithRegex(string value)
    {
        if (Regex.IsMatch(value, ValidationPatterns.GenderPattern, RegexOptions.IgnoreCase))
            return ValidationResult.Valid();

        return ValidationResult.Invalid("invalid gender value");
    }

    /// <summary>
    /// Validates country name using regex pattern.
    /// Must be 2+ characters and contain letters.
    /// </summary>
    private static ValidationResult ValidateCountryWithRegex(string value)
    {
        if (Regex.IsMatch(value, ValidationPatterns.CountryPattern))
            return ValidationResult.Valid();

        return ValidationResult.Invalid("invalid country format");
    }

    /// <summary>
    /// Validates boolean/checkbox values using regex pattern.
    /// Valid values: yes, no, true, false, 1, 0, checked, unchecked
    /// </summary>
    private static ValidationResult ValidateBooleanWithRegex(string value)
    {
        if (Regex.IsMatch(value, ValidationPatterns.BooleanPattern, RegexOptions.IgnoreCase))
            return ValidationResult.Valid();

        return ValidationResult.Invalid("invalid boolean value");
    }
}

/// <summary>
/// Field type enumeration for validation.
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

/// <summary>
/// Validation result for a field.
/// </summary>
internal class ValidationResult
{
    public bool IsValid { get; set; }
    public string Message { get; set; }

    private ValidationResult(bool isValid, string message)
    {
        IsValid = isValid;
        Message = message;
    }

    public static ValidationResult Valid() => new(true, "");
    public static ValidationResult Invalid(string message) => new(false, message);
}
