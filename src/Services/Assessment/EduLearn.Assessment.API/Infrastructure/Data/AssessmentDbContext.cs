using Microsoft.EntityFrameworkCore;
using EduLearn.Assessment.API.Domain.Entities;

namespace EduLearn.Assessment.API.Infrastructure.Data;

// EF Core DbContext for Assessment service — owns EduLearnAssessmentDb
public class AssessmentDbContext : DbContext
{
    public AssessmentDbContext(DbContextOptions<AssessmentDbContext> options) : base(options) { }

    public DbSet<Quiz>         Quizzes     => Set<Quiz>();
    public DbSet<Question>     Questions   => Set<Question>();
    public DbSet<QuizAttempt>  Attempts    => Set<QuizAttempt>();
    public DbSet<AttemptAnswer> Answers    => Set<AttemptAnswer>();
    public DbSet<Assignment>   Assignments => Set<Assignment>();
    public DbSet<Submission>   Submissions => Set<Submission>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Quiz>(e =>
        {
            e.HasKey(x => x.QuizId);
            e.Property(x => x.Title).IsRequired().HasMaxLength(200);
            e.HasMany(x => x.Questions).WithOne(q => q.Quiz)
             .HasForeignKey(q => q.QuizId).OnDelete(DeleteBehavior.Cascade);
        });

        model.Entity<Question>(e =>
        {
            e.HasKey(x => x.QuestionId);
            e.Property(x => x.Type).HasConversion<string>();
            e.Property(x => x.Text).IsRequired();
        });

        model.Entity<QuizAttempt>(e =>
        {
            e.HasKey(x => x.AttemptId);
            e.Property(x => x.Status).HasConversion<string>();
            e.Property(x => x.Score).HasColumnType("decimal(10,2)");
            e.Property(x => x.MaxScore).HasColumnType("decimal(10,2)");
            e.HasMany(x => x.Answers).WithOne(a => a.Attempt)
             .HasForeignKey(a => a.AttemptId).OnDelete(DeleteBehavior.Cascade);
        });

        model.Entity<AttemptAnswer>(e =>
        {
            e.HasKey(x => x.AnswerId);
            e.Property(x => x.PointsEarned).HasColumnType("decimal(10,2)");
        });

        model.Entity<Assignment>(e =>
        {
            e.HasKey(x => x.AssignmentId);
            e.Property(x => x.MaxScore).HasColumnType("decimal(10,2)");
            e.Property(x => x.LatePenaltyPct).HasColumnType("decimal(5,2)");
            e.HasMany(x => x.Submissions).WithOne(s => s.Assignment)
             .HasForeignKey(s => s.AssignmentId).OnDelete(DeleteBehavior.Cascade);
        });

        model.Entity<Submission>(e =>
        {
            e.HasKey(x => x.SubmissionId);
            e.Property(x => x.Status).HasConversion<string>();
            e.Property(x => x.Score).HasColumnType("decimal(10,2)");
        });
    }
}
