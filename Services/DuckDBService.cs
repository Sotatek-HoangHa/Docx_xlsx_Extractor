using DocxExtractor.Models;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;

namespace DocxExtractor.Services;

/// <summary>
/// Service for storing and reporting on extracted field data using SQLite.
/// Uses file-based analytics database for easy reporting.
/// </summary>
public class DuckDBService
{
    private readonly string _dbPath;
    private readonly string _connectionString;

    public DuckDBService(string dbPath = "extracted_data.db")
    {
        _dbPath = dbPath;
        _connectionString = $"Data Source={dbPath};Version=3;";
        InitializeDatabase();
    }

    /// <summary>
    /// Initialize database and create tables if they don't exist.
    /// </summary>
    private void InitializeDatabase()
    {
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();

        var createTableSql = @"
            CREATE TABLE IF NOT EXISTS ExtractedFields (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                DocumentName TEXT NOT NULL,
                FilterProfileName TEXT,
                FieldKey TEXT NOT NULL,
                FieldLabel TEXT,
                FieldValue TEXT,
                FieldType TEXT,
                IsValid INTEGER,
                ValidationMessage TEXT,
                ExtractedAt DATETIME DEFAULT CURRENT_TIMESTAMP
            );

            CREATE INDEX IF NOT EXISTS idx_document ON ExtractedFields(DocumentName);
            CREATE INDEX IF NOT EXISTS idx_valid ON ExtractedFields(IsValid);
            CREATE INDEX IF NOT EXISTS idx_type ON ExtractedFields(FieldType);
        ";

        using var command = connection.CreateCommand();
        command.CommandText = createTableSql;
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Save extracted fields to the database.
    /// </summary>
    public void SaveExtractedFields(List<FieldValue> fields, string documentName, string? filterProfileName = null)
    {
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();

        foreach (var field in fields)
        {
            var isValid = !field.Value.Contains(" - ");
            var validationMessage = isValid ? null : ExtractValidationMessage(field.Value);

            var insertSql = @"
                INSERT INTO ExtractedFields
                (DocumentName, FilterProfileName, FieldKey, FieldLabel, FieldValue, FieldType, IsValid, ValidationMessage)
                VALUES (@doc, @profile, @key, @label, @value, @type, @valid, @msg);
            ";

            using var command = connection.CreateCommand();
            command.CommandText = insertSql;
            command.Parameters.AddWithValue("@doc", documentName);
            command.Parameters.AddWithValue("@profile", filterProfileName ?? "none");
            command.Parameters.AddWithValue("@key", field.Key);
            command.Parameters.AddWithValue("@label", field.Label);
            command.Parameters.AddWithValue("@value", field.Value);
            command.Parameters.AddWithValue("@type", field.Type);
            command.Parameters.AddWithValue("@valid", isValid ? 1 : 0);
            command.Parameters.AddWithValue("@msg", (object?)validationMessage ?? DBNull.Value);

            command.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// Get a summary report of all extracted data.
    /// </summary>
    public string GetSummaryReport()
    {
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();

        var reportBuilder = new System.Text.StringBuilder();
        reportBuilder.AppendLine("\n╔════════════════════════════════════════════════════════╗");
        reportBuilder.AppendLine("║         EXTRACTED DATA SUMMARY REPORT                   ║");
        reportBuilder.AppendLine("╚════════════════════════════════════════════════════════╝\n");

        // Total records
        using var totalCommand = connection.CreateCommand();
        totalCommand.CommandText = "SELECT COUNT(*) FROM ExtractedFields;";
        var totalCount = (long)totalCommand.ExecuteScalar();
        reportBuilder.AppendLine($"📊 Total Fields Extracted: {totalCount}");

        // Validity summary
        reportBuilder.AppendLine("\n✓ Validity Summary:");
        using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
                SELECT IsValid, COUNT(*) as Count
                FROM ExtractedFields
                GROUP BY IsValid;
            ";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var isValid = reader.GetInt32(0) == 1;
                var count = reader.GetInt32(1);
                var icon = isValid ? "✓" : "✗";
                reportBuilder.AppendLine($"  {icon} {(isValid ? "Valid" : "Invalid")}: {count}");
            }
        }

        // Fields by type
        reportBuilder.AppendLine("\n📋 Fields by Type:");
        using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
                SELECT FieldType, COUNT(*) as Count,
                       SUM(CASE WHEN IsValid = 1 THEN 1 ELSE 0 END) as ValidCount
                FROM ExtractedFields
                GROUP BY FieldType
                ORDER BY Count DESC;
            ";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var fieldType = reader.GetString(0);
                var count = reader.GetInt32(1);
                var validCount = reader.GetInt32(2);
                var validity = count > 0 ? (validCount * 100 / count) : 0;
                reportBuilder.AppendLine($"  • {fieldType}: {count} fields ({validCount} valid, {validity}%)");
            }
        }

        // Documents processed
        reportBuilder.AppendLine("\n📄 Documents Processed:");
        using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
                SELECT DocumentName, COUNT(*) as FieldCount,
                       COUNT(DISTINCT FilterProfileName) as ProfilesUsed
                FROM ExtractedFields
                GROUP BY DocumentName;
            ";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var docName = reader.GetString(0);
                var fieldCount = reader.GetInt32(1);
                var profileCount = reader.GetInt32(2);
                reportBuilder.AppendLine($"  📌 {docName}: {fieldCount} fields, {profileCount} profile(s)");
            }
        }

        return reportBuilder.ToString();
    }

    /// <summary>
    /// Get validation errors report.
    /// </summary>
    public string GetValidationErrorsReport()
    {
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();

        var reportBuilder = new System.Text.StringBuilder();
        reportBuilder.AppendLine("\n╔════════════════════════════════════════════════════════╗");
        reportBuilder.AppendLine("║              VALIDATION ERRORS REPORT                   ║");
        reportBuilder.AppendLine("╚════════════════════════════════════════════════════════╝\n");

        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT DocumentName, FieldKey, FieldLabel, FieldType, ValidationMessage, COUNT(*) as ErrorCount
            FROM ExtractedFields
            WHERE IsValid = 0
            GROUP BY DocumentName, FieldKey, FieldLabel, FieldType, ValidationMessage
            ORDER BY ErrorCount DESC;
        ";

        using var reader = command.ExecuteReader();
        int errorCount = 0;
        while (reader.Read())
        {
            errorCount++;
            var docName = reader.GetString(0);
            var fieldKey = reader.GetString(1);
            var fieldLabel = reader.GetString(2);
            var fieldType = reader.GetString(3);
            var message = reader.IsDBNull(4) ? "Unknown error" : reader.GetString(4);
            var count = reader.GetInt32(5);

            reportBuilder.AppendLine($"{errorCount}. {fieldLabel} ({fieldType})");
            reportBuilder.AppendLine($"   📁 Document: {docName}");
            reportBuilder.AppendLine($"   ⚠️  Error: {message}");
            reportBuilder.AppendLine($"   🔢 Count: {count}\n");
        }

        if (errorCount == 0)
        {
            reportBuilder.AppendLine("✓ No validation errors found!");
        }

        return reportBuilder.ToString();
    }

    /// <summary>
    /// Get filter profile usage report.
    /// </summary>
    public string GetFilterUsageReport()
    {
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();

        var reportBuilder = new System.Text.StringBuilder();
        reportBuilder.AppendLine("\n╔════════════════════════════════════════════════════════╗");
        reportBuilder.AppendLine("║           FILTER PROFILE USAGE REPORT                   ║");
        reportBuilder.AppendLine("╚════════════════════════════════════════════════════════╝\n");

        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT FilterProfileName, COUNT(*) as UsageCount,
                   COUNT(DISTINCT DocumentName) as DocumentsUsed
            FROM ExtractedFields
            GROUP BY FilterProfileName
            ORDER BY UsageCount DESC;
        ";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var profileName = reader.GetString(0);
            var usageCount = reader.GetInt32(1);
            var documentCount = reader.GetInt32(2);
            reportBuilder.AppendLine($"🔍 {profileName}:");
            reportBuilder.AppendLine($"   • Usage Count: {usageCount}");
            reportBuilder.AppendLine($"   • Documents: {documentCount}\n");
        }

        return reportBuilder.ToString();
    }

    /// <summary>
    /// Extract validation message from field value.
    /// </summary>
    private string? ExtractValidationMessage(string fieldValue)
    {
        var parts = fieldValue.Split(" - ");
        return parts.Length > 1 ? string.Join(" - ", parts.Skip(1)) : null;
    }
}
