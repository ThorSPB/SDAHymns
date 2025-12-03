using SDAHymns.Core.Data.Models;

namespace SDAHymns.Core.Data;

public class HymnsContext : DbContext
{
    public HymnsContext(DbContextOptions<HymnsContext> options)
        : base(options)
    {
    }

    // DbSets
    public DbSet<Hymn> Hymns => Set<Hymn>();
    public DbSet<Verse> Verses => Set<Verse>();
    public DbSet<HymnCategory> HymnCategories => Set<HymnCategory>();
    public DbSet<AudioRecording> AudioRecordings => Set<AudioRecording>();
    public DbSet<DisplayProfile> DisplayProfiles => Set<DisplayProfile>();
    public DbSet<ServicePlan> ServicePlans => Set<ServicePlan>();
    public DbSet<ServicePlanItem> ServicePlanItems => Set<ServicePlanItem>();
    public DbSet<UsageStatistic> UsageStatistics => Set<UsageStatistic>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureHymn(modelBuilder);
        ConfigureVerse(modelBuilder);
        ConfigureHymnCategory(modelBuilder);
        ConfigureAudioRecording(modelBuilder);
        ConfigureDisplayProfile(modelBuilder);
        ConfigureServicePlan(modelBuilder);
        ConfigureServicePlanItem(modelBuilder);
        ConfigureUsageStatistic(modelBuilder);
        ConfigureAppSetting(modelBuilder);

        SeedData(modelBuilder);
    }

    private void ConfigureHymn(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Hymn>(entity =>
        {
            entity.HasKey(h => h.Id);

            entity.Property(h => h.Title)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(h => h.LegacyPowerPointPath)
                .HasMaxLength(500);

            entity.HasIndex(h => h.Number);
            entity.HasIndex(h => h.Title);
            entity.HasIndex(h => h.CategoryId);
            entity.HasIndex(h => new { h.Number, h.CategoryId })
                .IsUnique();

            entity.HasOne(h => h.Category)
                .WithMany(c => c.Hymns)
                .HasForeignKey(h => h.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureVerse(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Verse>(entity =>
        {
            entity.HasKey(v => v.Id);

            entity.Property(v => v.Content)
                .IsRequired()
                .HasMaxLength(2000);

            entity.Property(v => v.Label)
                .HasMaxLength(50);

            entity.HasIndex(v => v.HymnId);
            entity.HasIndex(v => new { v.HymnId, v.VerseNumber })
                .IsUnique();
            entity.HasIndex(v => new { v.HymnId, v.DisplayOrder });

            entity.HasOne(v => v.Hymn)
                .WithMany(h => h.Verses)
                .HasForeignKey(v => v.HymnId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureHymnCategory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<HymnCategory>(entity =>
        {
            entity.HasKey(c => c.Id);

            entity.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(c => c.Slug)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(c => c.Description)
                .HasMaxLength(500);

            entity.Property(c => c.IconPath)
                .HasMaxLength(500);

            entity.Property(c => c.LegacyFolderPath)
                .HasMaxLength(500);

            entity.HasIndex(c => c.Name)
                .IsUnique();
            entity.HasIndex(c => c.Slug)
                .IsUnique();
            entity.HasIndex(c => c.DisplayOrder);
        });
    }

    private void ConfigureAudioRecording(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AudioRecording>(entity =>
        {
            entity.HasKey(a => a.Id);

            entity.Property(a => a.FilePath)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(a => a.FileFormat)
                .HasMaxLength(10);

            entity.Property(a => a.Tempo)
                .HasMaxLength(50);

            entity.Property(a => a.RecordedBy)
                .HasMaxLength(200);

            entity.Property(a => a.Notes)
                .HasMaxLength(1000);

            entity.HasIndex(a => a.HymnId);
            entity.HasIndex(a => a.FilePath);

            entity.HasOne(a => a.Hymn)
                .WithMany(h => h.AudioRecordings)
                .HasForeignKey(a => a.HymnId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureDisplayProfile(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DisplayProfile>(entity =>
        {
            entity.HasKey(d => d.Id);

            entity.Property(d => d.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(d => d.Description)
                .HasMaxLength(500);

            entity.Property(d => d.FontFamily)
                .HasMaxLength(100);

            entity.Property(d => d.BackgroundColor)
                .HasMaxLength(20);

            entity.Property(d => d.TextColor)
                .HasMaxLength(20);

            entity.Property(d => d.ShadowColor)
                .HasMaxLength(20);

            entity.Property(d => d.TextAlignment)
                .HasMaxLength(20);

            entity.Property(d => d.BackgroundImagePath)
                .HasMaxLength(500);

            entity.HasIndex(d => d.Name)
                .IsUnique();
            entity.HasIndex(d => d.IsDefault);
        });
    }

    private void ConfigureServicePlan(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ServicePlan>(entity =>
        {
            entity.HasKey(s => s.Id);

            entity.Property(s => s.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(s => s.Description)
                .HasMaxLength(1000);

            entity.HasIndex(s => s.ServiceDate);
            entity.HasIndex(s => s.IsActive);
            entity.HasIndex(s => s.Name);
        });
    }

    private void ConfigureServicePlanItem(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ServicePlanItem>(entity =>
        {
            entity.HasKey(i => i.Id);

            entity.Property(i => i.SelectedVerses)
                .HasMaxLength(200);

            entity.Property(i => i.Notes)
                .HasMaxLength(500);

            entity.HasIndex(i => new { i.ServicePlanId, i.DisplayOrder });

            entity.HasOne(i => i.ServicePlan)
                .WithMany(s => s.Items)
                .HasForeignKey(i => i.ServicePlanId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(i => i.Hymn)
                .WithMany(h => h.ServicePlanItems)
                .HasForeignKey(i => i.HymnId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureUsageStatistic(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UsageStatistic>(entity =>
        {
            entity.HasKey(u => u.Id);

            entity.Property(u => u.ServicePlanName)
                .HasMaxLength(200);

            entity.HasIndex(u => u.HymnId);
            entity.HasIndex(u => u.UsedAt);

            entity.HasOne(u => u.Hymn)
                .WithMany(h => h.UsageStatistics)
                .HasForeignKey(u => u.HymnId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureAppSetting(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppSetting>(entity =>
        {
            entity.HasKey(a => a.Id);

            entity.Property(a => a.Key)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(a => a.Value)
                .IsRequired();

            entity.Property(a => a.Description)
                .HasMaxLength(500);

            entity.HasIndex(a => a.Key)
                .IsUnique();
        });
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        // Use static date for seed data (required by EF Core)
        var now = new DateTime(2025, 12, 3, 0, 0, 0, DateTimeKind.Utc);

        // Seed HymnCategories
        modelBuilder.Entity<HymnCategory>().HasData(
            new HymnCategory
            {
                Id = 1,
                Name = "Imnuri crestine",
                Slug = "crestine",
                Description = "Main Christian hymnbook",
                DisplayOrder = 1,
                LegacyFolderPath = "Imnuri Azs/Resurse/Imnuri crestine",
                CreatedAt = now,
                UpdatedAt = now
            },
            new HymnCategory
            {
                Id = 2,
                Name = "Imnuri companioni",
                Slug = "companioni",
                Description = "Pathfinder/Companion hymns",
                DisplayOrder = 2,
                LegacyFolderPath = "Imnuri Azs/Resurse/Imnuri companioni",
                CreatedAt = now,
                UpdatedAt = now
            },
            new HymnCategory
            {
                Id = 3,
                Name = "Imnuri exploratori",
                Slug = "exploratori",
                Description = "Explorer hymns",
                DisplayOrder = 3,
                LegacyFolderPath = "Imnuri Azs/Resurse/Imnuri exploratori",
                CreatedAt = now,
                UpdatedAt = now
            },
            new HymnCategory
            {
                Id = 4,
                Name = "Imnuri licurici",
                Slug = "licurici",
                Description = "Firefly hymns (children's songs)",
                DisplayOrder = 4,
                LegacyFolderPath = "Imnuri Azs/Resurse/Imnuri licurici",
                CreatedAt = now,
                UpdatedAt = now
            },
            new HymnCategory
            {
                Id = 5,
                Name = "Imnuri tineret",
                Slug = "tineret",
                Description = "Youth hymns",
                DisplayOrder = 5,
                LegacyFolderPath = "Imnuri Azs/Resurse/Imnuri tineret",
                CreatedAt = now,
                UpdatedAt = now
            }
        );

        // Seed DisplayProfiles
        modelBuilder.Entity<DisplayProfile>().HasData(
            new DisplayProfile
            {
                Id = 1,
                Name = "Projector",
                Description = "Default projector display settings",
                BackgroundColor = "#000000",
                BackgroundOpacity = 1.0,
                FontFamily = "Arial",
                FontSize = 48,
                TextColor = "#FFFFFF",
                TextAlignment = "Center",
                EnableTextShadow = true,
                ShadowColor = "#000000",
                PaddingHorizontal = 60,
                PaddingVertical = 40,
                LineSpacing = 15,
                TransparentBackground = false,
                IsDefault = true,
                IsSystemProfile = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new DisplayProfile
            {
                Id = 2,
                Name = "OBS Stream",
                Description = "Transparent background for OBS/streaming",
                BackgroundColor = "#000000",
                BackgroundOpacity = 0.0,
                FontFamily = "Arial",
                FontSize = 52,
                TextColor = "#FFFFFF",
                TextAlignment = "Center",
                EnableTextShadow = true,
                ShadowColor = "#000000",
                PaddingHorizontal = 80,
                PaddingVertical = 60,
                LineSpacing = 18,
                TransparentBackground = true,
                IsDefault = false,
                IsSystemProfile = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new DisplayProfile
            {
                Id = 3,
                Name = "Practice Mode",
                Description = "Lower opacity for practice/rehearsal",
                BackgroundColor = "#1a1a1a",
                BackgroundOpacity = 0.7,
                FontFamily = "Arial",
                FontSize = 36,
                TextColor = "#CCCCCC",
                TextAlignment = "Center",
                EnableTextShadow = false,
                PaddingHorizontal = 40,
                PaddingVertical = 30,
                LineSpacing = 12,
                TransparentBackground = false,
                IsDefault = false,
                IsSystemProfile = true,
                CreatedAt = now,
                UpdatedAt = now
            }
        );

        // Seed initial AppSettings
        modelBuilder.Entity<AppSetting>().HasData(
            new AppSetting
            {
                Id = 1,
                Key = "LastDatabaseImportDate",
                Value = "",
                Description = "Last time legacy XML was imported",
                CreatedAt = now,
                UpdatedAt = now
            },
            new AppSetting
            {
                Id = 2,
                Key = "DefaultDisplayProfileId",
                Value = "1",
                Description = "Default display profile ID",
                CreatedAt = now,
                UpdatedAt = now
            },
            new AppSetting
            {
                Id = 3,
                Key = "EnableStatisticsTracking",
                Value = "true",
                Description = "Track hymn usage statistics",
                CreatedAt = now,
                UpdatedAt = now
            }
        );
    }
}
