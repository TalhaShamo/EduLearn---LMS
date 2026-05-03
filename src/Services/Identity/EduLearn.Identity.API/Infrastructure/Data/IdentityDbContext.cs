using Microsoft.EntityFrameworkCore;
using EduLearn.Identity.API.Domain.Entities;

namespace EduLearn.Identity.API.Infrastructure.Data;

// EF Core DbContext for the Identity service
// Each microservice owns its own DbContext and database
public class IdentityDbContext : DbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options) { }

    public DbSet<User>         Users         => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        // ── User table configuration ──────────────────────────
        model.Entity<User>(entity =>
        {
            entity.HasKey(u => u.UserId);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(256);
            entity.HasIndex(u => u.Email).IsUnique();  // Enforce email uniqueness at DB level
            entity.Property(u => u.FullName).IsRequired().HasMaxLength(100);
            entity.Property(u => u.PasswordHash).IsRequired();

            // Store enum as string for readability in DB ("Student", "Instructor", "Admin")
            entity.Property(u => u.Role).HasConversion<string>();

            // One user → many refresh tokens
            entity.HasMany(u => u.RefreshTokens)
                  .WithOne(t => t.User)
                  .HasForeignKey(t => t.UserId)
                  .OnDelete(DeleteBehavior.Cascade); // Tokens deleted when user deleted
        });

        // ── RefreshToken table configuration ─────────────────
        model.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Token).IsRequired().HasMaxLength(512);
            entity.HasIndex(t => t.Token); // Index for fast token lookups
        });

        // ── EmailVerificationToken table configuration ───────
        model.Entity<EmailVerificationToken>(entity =>
        {
            entity.HasKey(t => t.TokenId);
            entity.Property(t => t.Token).IsRequired().HasMaxLength(100);
            entity.HasIndex(t => t.Token); // Index for fast token lookups
            entity.Property(t => t.UserId).IsRequired();
        });

        // ── PasswordResetToken table configuration ───────────
        model.Entity<PasswordResetToken>(entity =>
        {
            entity.HasKey(t => t.TokenId);
            entity.Property(t => t.Token).IsRequired().HasMaxLength(100);
            entity.HasIndex(t => t.Token); // Index for fast token lookups
            entity.Property(t => t.UserId).IsRequired();
        });
    }
}
