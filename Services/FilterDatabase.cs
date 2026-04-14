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
        context.Database.EnsureCreated();
    }
}
