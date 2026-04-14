using DocxExtractor.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DocxExtractor.Services;

/// <summary>
/// Database context for filter profiles and rules.
/// </summary>
public class FilterDbContext : DbContext
{
    public DbSet<FilterProfile> FilterProfiles { get; set; }
    public DbSet<FilterRule> FilterRules { get; set; }
    public DbSet<ExtractedFieldData> ExtractedFields { get; set; }

    private readonly string _connectionString;

    public FilterDbContext(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseNpgsql(_connectionString);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure FilterProfile
        modelBuilder.Entity<FilterProfile>()
            .HasKey(x => x.Id);

        modelBuilder.Entity<FilterProfile>()
            .Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        modelBuilder.Entity<FilterProfile>()
            .Property(x => x.Description)
            .HasMaxLength(500);

        // Configure FilterRule
        modelBuilder.Entity<FilterRule>()
            .HasKey(x => x.Id);

        modelBuilder.Entity<FilterRule>()
            .Property(x => x.FieldType)
            .IsRequired()
            .HasMaxLength(50);

        // Configure relationship from FilterProfile to FilterRule
        // Use HasMany without WithOne to avoid duplicate relationships
        modelBuilder.Entity<FilterProfile>()
            .HasMany(x => x.IncludeRules)
            .WithOne()
            .HasForeignKey(x => x.FilterProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FilterProfile>()
            .HasMany(x => x.ExcludeRules)
            .WithOne()
            .HasForeignKey(x => x.FilterProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        // No navigation property on FilterRule to avoid EF relationship conflicts

        // Create indexes for faster queries
        modelBuilder.Entity<FilterProfile>()
            .HasIndex(x => x.Name)
            .IsUnique();

        modelBuilder.Entity<FilterRule>()
            .HasIndex(x => new { x.FilterProfileId, x.IsIncludeRule });

        // Configure ExtractedFieldData
        modelBuilder.Entity<ExtractedFieldData>()
            .HasKey(x => x.Id);

        modelBuilder.Entity<ExtractedFieldData>()
            .Property(x => x.DocumentName)
            .IsRequired()
            .HasMaxLength(255);

        modelBuilder.Entity<ExtractedFieldData>()
            .Property(x => x.FieldKey)
            .IsRequired()
            .HasMaxLength(100);

        modelBuilder.Entity<ExtractedFieldData>()
            .Property(x => x.FieldLabel)
            .HasMaxLength(255);

        modelBuilder.Entity<ExtractedFieldData>()
            .Property(x => x.FieldType)
            .HasMaxLength(50);

        // Create indexes for efficient querying
        modelBuilder.Entity<ExtractedFieldData>()
            .HasIndex(x => x.DocumentName);

        modelBuilder.Entity<ExtractedFieldData>()
            .HasIndex(x => x.FilterProfileName);

        modelBuilder.Entity<ExtractedFieldData>()
            .HasIndex(x => x.FieldType);

        modelBuilder.Entity<ExtractedFieldData>()
            .HasIndex(x => x.IsValid);

        modelBuilder.Entity<ExtractedFieldData>()
            .HasIndex(x => x.ExtractedAt);
    }
}

/// <summary>
/// Service for managing filter profiles and rules.
/// </summary>
public class FilterService
{
    private readonly string _connectionString;

    public FilterService(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <summary>
    /// Gets a filter profile by name.
    /// </summary>
    public ExtractionFilter? GetFilterProfile(string profileName)
    {
        using var context = new FilterDbContext(_connectionString);

        var profile = context.FilterProfiles
            .FirstOrDefault(x => x.Name == profileName);

        if (profile == null)
            return null;

        // Get all rules for this profile and split by IsIncludeRule flag
        var allRules = context.FilterRules
            .Where(x => x.FilterProfileId == profile.Id)
            .ToList();

        return new ExtractionFilter
        {
            ProfileId = profile.Id,
            ProfileName = profile.Name,
            IncludeFieldTypes = allRules
                .Where(x => x.IsIncludeRule)
                .Select(x => x.FieldType)
                .ToList(),
            ExcludeFieldTypes = allRules
                .Where(x => !x.IsIncludeRule)
                .Select(x => x.FieldType)
                .ToList()
        };
    }

    /// <summary>
    /// Gets the active filter profile.
    /// </summary>
    public ExtractionFilter? GetActiveFilterProfile()
    {
        using var context = new FilterDbContext(_connectionString);

        var profile = context.FilterProfiles
            .FirstOrDefault(x => x.IsActive);

        if (profile == null)
            return null;

        // Get all rules for this profile and split by IsIncludeRule flag
        var allRules = context.FilterRules
            .Where(x => x.FilterProfileId == profile.Id)
            .ToList();

        return new ExtractionFilter
        {
            ProfileId = profile.Id,
            ProfileName = profile.Name,
            IncludeFieldTypes = allRules
                .Where(x => x.IsIncludeRule)
                .Select(x => x.FieldType)
                .ToList(),
            ExcludeFieldTypes = allRules
                .Where(x => !x.IsIncludeRule)
                .Select(x => x.FieldType)
                .ToList()
        };
    }

    /// <summary>
    /// Gets all available filter profiles.
    /// </summary>
    public List<ExtractionFilter> GetAllFilterProfiles()
    {
        using var context = new FilterDbContext(_connectionString);

        var profiles = context.FilterProfiles.ToList();
        var allRules = context.FilterRules.ToList();

        return profiles.Select(p => new ExtractionFilter
        {
            ProfileId = p.Id,
            ProfileName = p.Name,
            IncludeFieldTypes = allRules
                .Where(x => x.FilterProfileId == p.Id && x.IsIncludeRule)
                .Select(x => x.FieldType)
                .ToList(),
            ExcludeFieldTypes = allRules
                .Where(x => x.FilterProfileId == p.Id && !x.IsIncludeRule)
                .Select(x => x.FieldType)
                .ToList()
        }).ToList();
    }

    /// <summary>
    /// Creates a new filter profile with rules.
    /// </summary>
    public int CreateFilterProfile(string name, string description, List<string> includeTypes, List<string> excludeTypes)
    {
        using var context = new FilterDbContext(_connectionString);

        var profile = new FilterProfile
        {
            Name = name,
            Description = description,
            IsActive = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.FilterProfiles.Add(profile);
        context.SaveChanges(); // Save profile first to get the ID

        // Add include rules with explicit FK
        foreach (var fieldType in includeTypes)
        {
            var rule = new FilterRule
            {
                FilterProfileId = profile.Id,
                FieldType = fieldType,
                IsIncludeRule = true,
                CreatedAt = DateTime.UtcNow
            };
            context.FilterRules.Add(rule);
        }

        // Add exclude rules with explicit FK
        foreach (var fieldType in excludeTypes)
        {
            var rule = new FilterRule
            {
                FilterProfileId = profile.Id,
                FieldType = fieldType,
                IsIncludeRule = false,
                CreatedAt = DateTime.UtcNow
            };
            context.FilterRules.Add(rule);
        }

        context.SaveChanges();

        return profile.Id;
    }

    /// <summary>
    /// Activates a filter profile by name.
    /// </summary>
    public void ActivateFilterProfile(string profileName)
    {
        using var context = new FilterDbContext(_connectionString);

        // Deactivate all other profiles
        var allProfiles = context.FilterProfiles.ToList();
        foreach (var profile in allProfiles)
        {
            profile.IsActive = false;
        }

        // Activate the specified profile
        var targetProfile = allProfiles.FirstOrDefault(x => x.Name == profileName);
        if (targetProfile != null)
        {
            targetProfile.IsActive = true;
        }

        context.SaveChanges();
    }

    /// <summary>
    /// Deletes a filter profile.
    /// </summary>
    public void DeleteFilterProfile(string profileName)
    {
        using var context = new FilterDbContext(_connectionString);

        var profile = context.FilterProfiles.FirstOrDefault(x => x.Name == profileName);
        if (profile != null)
        {
            context.FilterProfiles.Remove(profile);
            context.SaveChanges();
        }
    }

    /// <summary>
    /// Initializes the database schema if it doesn't exist.
    /// </summary>
    public void InitializeDatabase()
    {
        using var context = new FilterDbContext(_connectionString);
        // Ensure all tables are created
        context.Database.EnsureCreated();

        // Ensure ExtractedFields table exists
        try
        {
            context.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS ""ExtractedFields"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""DocumentName"" VARCHAR(255) NOT NULL,
                    ""FilterProfileName"" VARCHAR(255),
                    ""FieldKey"" VARCHAR(100) NOT NULL,
                    ""FieldLabel"" VARCHAR(255),
                    ""FieldValue"" TEXT,
                    ""FieldType"" VARCHAR(50),
                    ""IsValid"" BOOLEAN NOT NULL,
                    ""ValidationMessage"" TEXT,
                    ""ExtractedAt"" TIMESTAMP NOT NULL
                );
            ");

            // Create indexes
            context.Database.ExecuteSqlRaw(@"CREATE INDEX IF NOT EXISTS ""idx_ExtractedFields_DocumentName"" ON ""ExtractedFields"" (""DocumentName"");");
            context.Database.ExecuteSqlRaw(@"CREATE INDEX IF NOT EXISTS ""idx_ExtractedFields_FilterProfileName"" ON ""ExtractedFields"" (""FilterProfileName"");");
            context.Database.ExecuteSqlRaw(@"CREATE INDEX IF NOT EXISTS ""idx_ExtractedFields_FieldType"" ON ""ExtractedFields"" (""FieldType"");");
            context.Database.ExecuteSqlRaw(@"CREATE INDEX IF NOT EXISTS ""idx_ExtractedFields_IsValid"" ON ""ExtractedFields"" (""IsValid"");");
            context.Database.ExecuteSqlRaw(@"CREATE INDEX IF NOT EXISTS ""idx_ExtractedFields_ExtractedAt"" ON ""ExtractedFields"" (""ExtractedAt"");");
        }
        catch
        {
            // Table might already exist, continue
        }
    }

    /// <summary>
    /// Saves extracted field data to the database for reporting.
    /// </summary>
    public void SaveExtractedFields(List<FieldValue> fields, string documentName, string? filterProfileName = null)
    {
        using var context = new FilterDbContext(_connectionString);

        foreach (var field in fields)
        {
            var isValid = !field.Value.Contains(" - ");
            var validationMessage = isValid ? null : ExtractValidationMessage(field.Value);

            var extractedField = new ExtractedFieldData
            {
                DocumentName = documentName,
                FilterProfileName = filterProfileName,
                FieldKey = field.Key,
                FieldLabel = field.Label,
                FieldValue = field.Value,
                FieldType = field.Type,
                IsValid = isValid,
                ValidationMessage = validationMessage,
                ExtractedAt = DateTime.UtcNow
            };

            context.ExtractedFields.Add(extractedField);
        }

        context.SaveChanges();
    }

    /// <summary>
    /// Gets a summary report of extracted data.
    /// </summary>
    public string GetExtractedDataSummary()
    {
        using var context = new FilterDbContext(_connectionString);

        var totalFields = context.ExtractedFields.Count();
        var validFields = context.ExtractedFields.Count(x => x.IsValid);
        var invalidFields = totalFields - validFields;

        var fieldsByType = context.ExtractedFields
            .GroupBy(x => x.FieldType)
            .Select(g => new { Type = g.Key, Count = g.Count(), ValidCount = g.Count(x => x.IsValid) })
            .OrderByDescending(x => x.Count)
            .ToList();

        var errorsByDocument = context.ExtractedFields
            .Where(x => !x.IsValid)
            .GroupBy(x => x.DocumentName)
            .Select(g => new { Document = g.Key, ErrorCount = g.Count() })
            .OrderByDescending(x => x.ErrorCount)
            .ToList();

        var report = new System.Text.StringBuilder();
        report.AppendLine("\n╔════════════════════════════════════════════════════════╗");
        report.AppendLine("║         EXTRACTED DATA SUMMARY REPORT                   ║");
        report.AppendLine("╚════════════════════════════════════════════════════════╝\n");

        report.AppendLine($"📊 Total Fields Extracted: {totalFields}");
        report.AppendLine($"   ✓ Valid: {validFields}");
        report.AppendLine($"   ✗ Invalid: {invalidFields}\n");

        report.AppendLine("📋 Fields by Type:");
        foreach (var typeGroup in fieldsByType)
        {
            var validPercent = typeGroup.Count > 0 ? (typeGroup.ValidCount * 100 / typeGroup.Count) : 0;
            report.AppendLine($"   • {typeGroup.Type}: {typeGroup.Count} ({typeGroup.ValidCount} valid, {validPercent}%)");
        }

        if (errorsByDocument.Any())
        {
            report.AppendLine("\n⚠️  Validation Errors by Document:");
            foreach (var docErrors in errorsByDocument)
            {
                report.AppendLine($"   📄 {docErrors.Document}: {docErrors.ErrorCount} errors");
            }
        }

        return report.ToString();
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
