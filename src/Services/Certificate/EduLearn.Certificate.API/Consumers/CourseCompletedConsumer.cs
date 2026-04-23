using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CertificateEntity = EduLearn.Certificate.API.Domain.Entities.Certificate;
using EduLearn.Certificate.API.Domain.Entities;
using EduLearn.Certificate.API.Infrastructure.Data;
using EduLearn.Certificate.API.Services;
using EduLearn.Shared.Events;
using Microsoft.Extensions.Logging;

namespace EduLearn.Certificate.API.Consumers;

// Consumes CourseCompletedEvent from Enrollment.API
// Generates the PDF certificate and publishes CertificateIssuedEvent
public class CourseCompletedConsumer : IConsumer<CourseCompletedEvent>
{
    private readonly CertificateDbContext   _db;
    private readonly CertificatePdfService  _pdf;
    private readonly IPublishEndpoint       _bus;
    private readonly IConfiguration         _config;
    private readonly ILogger<CourseCompletedConsumer> _logger;

    public CourseCompletedConsumer(CertificateDbContext db, CertificatePdfService pdf,
        IPublishEndpoint bus, IConfiguration config, ILogger<CourseCompletedConsumer> logger)
    { _db = db; _pdf = pdf; _bus = bus; _config = config; _logger = logger; }

    public async Task Consume(ConsumeContext<CourseCompletedEvent> ctx)
    {
        var e = ctx.Message;
        _logger.LogInformation("Generating certificate for Student {StudentId}, Course {CourseId}",
            e.StudentId, e.CourseId);

        // Idempotency: skip if certificate already issued for this student+course
        bool exists = await _db.Certificates
            .AnyAsync(c => c.StudentId == e.StudentId && c.CourseId == e.CourseId);
        if (exists)
        {
            _logger.LogWarning("Certificate already exists for Student {StudentId}", e.StudentId);
            return;
        }

        // Build certificate record with a unique verification code
        var cert = new CertificateEntity
        {
            StudentId        = e.StudentId,
            CourseId         = e.CourseId,
            StudentName      = e.StudentName,
            CourseTitle      = e.CourseTitle,
            InstructorName   = e.InstructorName,
            VerificationCode = GenerateVerificationCode(),
            IssuedAt         = e.CompletedAt
        };

        // Generate the PDF — File I/O
        cert.FilePath = await _pdf.GenerateAsync(cert);

        _db.Certificates.Add(cert);
        await _db.SaveChangesAsync();

        // Build public URLs for download and verification
        var baseUrl     = _config["BaseUrl"]!;
        var downloadUrl = $"{baseUrl}/api/v1/certificates/{cert.CertificateId}/download";
        var verifyUrl   = $"{baseUrl}/api/v1/certificates/verify/{cert.VerificationCode}";

        // Publish event → Notification.API sends certificate email to student
        await _bus.Publish(new CertificateIssuedEvent(
            cert.CertificateId, cert.StudentId, cert.CourseId,
            e.StudentEmail, cert.StudentName, cert.CourseTitle,
            downloadUrl, verifyUrl, cert.IssuedAt));
    }

    // Generate a human-readable 10-char verification code (e.g., "EDU-A3X9KP")
    private static string GenerateVerificationCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var code = new char[6];
        var rng  = new Random();
        for (int i = 0; i < 6; i++)
            code[i] = chars[rng.Next(chars.Length)];
        return $"EDU-{new string(code)}";
    }
}
