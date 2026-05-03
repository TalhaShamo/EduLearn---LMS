using Microsoft.EntityFrameworkCore;
using CourseEntity = EduLearn.Course.API.Domain.Entities.Course;
using EduLearn.Course.API.Domain.Entities;

namespace EduLearn.Course.API.Infrastructure.Data;

// EF Core DbContext for Course service — owns EduLearnCourseDb
public class CourseDbContext : DbContext
{
    public CourseDbContext(DbContextOptions<CourseDbContext> options) : base(options) { }

    public DbSet<CourseEntity> Courses  => Set<CourseEntity>();
    public DbSet<Section>  Sections => Set<Section>();
    public DbSet<Lesson>   Lessons  => Set<Lesson>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        // ── Course ────────────────────────────────────────────
        model.Entity<CourseEntity>(e =>
        {
            e.HasKey(c => c.CourseId);
            e.Property(c => c.Title).IsRequired().HasMaxLength(200);
            e.Property(c => c.Subtitle).HasMaxLength(500);
            e.Property(c => c.Slug).IsRequired().HasMaxLength(250);
            e.HasIndex(c => c.Slug).IsUnique();
            e.Property(c => c.Description).IsRequired();
            e.Property(c => c.Status).HasConversion<string>();
            e.Property(c => c.Level).HasConversion<string>();
            e.Property(c => c.Price).HasColumnType("decimal(18,2)");
            e.Property(c => c.InstructorName).HasMaxLength(200);
            e.Property(c => c.AverageRating).HasColumnType("decimal(3,2)");
            
            // Serialize Lists as JSON - handle NULL values
            e.Property(c => c.Tags)
             .HasConversion(
                 v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                 v => string.IsNullOrEmpty(v) ? new List<string>() : System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>()
             );
            
            e.Property(c => c.LearningObjectives)
             .HasConversion(
                 v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                 v => string.IsNullOrEmpty(v) ? new List<string>() : System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>()
             );

            // One course → many sections
            e.HasMany(c => c.Sections)
             .WithOne(s => s.Course)
             .HasForeignKey(s => s.CourseId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Section ───────────────────────────────────────────
        model.Entity<Section>(e =>
        {
            e.HasKey(s => s.SectionId);
            e.Property(s => s.Title).IsRequired().HasMaxLength(200);

            // One section → many lessons
            e.HasMany(s => s.Lessons)
             .WithOne(l => l.Section)
             .HasForeignKey(l => l.SectionId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Lesson ────────────────────────────────────────────
        model.Entity<Lesson>(e =>
        {
            e.HasKey(l => l.LessonId);
            e.Property(l => l.Title).IsRequired().HasMaxLength(200);
            e.Property(l => l.Type).HasConversion<string>();
            e.Property(l => l.VideoPath).HasMaxLength(500);
        });
    }
}
