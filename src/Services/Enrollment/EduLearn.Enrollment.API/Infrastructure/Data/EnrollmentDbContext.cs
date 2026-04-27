using Microsoft.EntityFrameworkCore;
using EduLearn.Enrollment.API.Domain.Entities;
using EnrollmentEntity = EduLearn.Enrollment.API.Domain.Entities.Enrollment;

namespace EduLearn.Enrollment.API.Infrastructure.Data;

// EF Core DbContext for Enrollment service — owns EduLearnEnrollmentDb
public class EnrollmentDbContext : DbContext
{
    public EnrollmentDbContext(DbContextOptions<EnrollmentDbContext> options) : base(options) { }

    public DbSet<EnrollmentEntity>     Enrollments     => Set<EnrollmentEntity>();
    public DbSet<LessonProgress> LessonProgresses => Set<LessonProgress>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<EnrollmentEntity>(e =>
        {
            e.HasKey(x => x.EnrollmentId);
            e.Property(x => x.ProgressPct).HasColumnType("decimal(5,2)");
            e.Property(x => x.Status).HasConversion<string>();

            // Unique constraint: one student per course
            e.HasIndex(x => new { x.StudentId, x.CourseId }).IsUnique();

            // One enrollment → many lesson progress records
            e.HasMany(x => x.LessonProgresses)
             .WithOne(p => p.Enrollment)
             .HasForeignKey(p => p.EnrollmentId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        model.Entity<LessonProgress>(e =>
        {
            e.HasKey(x => x.ProgressId);
            e.Property(x => x.Status).HasConversion<string>();

            // Unique: one progress record per lesson per enrollment
            e.HasIndex(x => new { x.EnrollmentId, x.LessonId }).IsUnique();
        });
    }
}
