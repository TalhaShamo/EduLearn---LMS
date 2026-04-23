using Microsoft.EntityFrameworkCore;
using CertificateEntity = EduLearn.Certificate.API.Domain.Entities.Certificate;
using EduLearn.Certificate.API.Domain.Entities;

namespace EduLearn.Certificate.API.Infrastructure.Data;

public class CertificateDbContext : DbContext
{
    public CertificateDbContext(DbContextOptions<CertificateDbContext> options) : base(options) { }

    public DbSet<CertificateEntity> Certificates => Set<CertificateEntity>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<CertificateEntity>(e =>
        {
            e.HasKey(c => c.CertificateId);
            e.Property(c => c.VerificationCode).IsRequired().HasMaxLength(50);
            e.HasIndex(c => c.VerificationCode).IsUnique();
            e.HasIndex(c => new { c.StudentId, c.CourseId }).IsUnique();
        });
    }
}
