using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using CertificateEntity = EduLearn.Certificate.API.Domain.Entities.Certificate;
using EduLearn.Certificate.API.Domain.Entities;

namespace EduLearn.Certificate.API.Services;

// Certificate PDF generator using QuestPDF
// QuestPDF community license is free for non-commercial use
public class CertificatePdfService
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<CertificatePdfService> _logger;

    public CertificatePdfService(IWebHostEnvironment env, ILogger<CertificatePdfService> logger)
    {
        _env    = env;
        _logger = logger;
        // Set QuestPDF to community license (free)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    // Generates a PDF certificate and saves it to wwwroot/certificates/
    // Returns the relative path for storage and URL access
    public async Task<string> GenerateAsync(CertificateEntity cert)
    {
        var dir = Path.Combine(_env.WebRootPath, "certificates");
        Directory.CreateDirectory(dir);  // File I/O: ensure directory exists

        var fileName = $"{cert.CertificateId}.pdf";
        var fullPath = Path.Combine(dir, fileName);

        // Build the PDF using QuestPDF's Fluent API
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(50);
                page.Background(Colors.White);

                page.Content()
                    .PaddingVertical(20)
                    .Column(col =>
                    {
                        // Header: decorative top border
                        col.Item().BorderBottom(4).BorderColor("#667eea").PaddingBottom(10)
                           .Row(row =>
                           {
                               row.RelativeItem()
                                  .Text("🎓 EduLearn")
                                  .FontSize(20).Bold().FontColor("#667eea");
                           });

                        col.Item().PaddingTop(30).AlignCenter()
                           .Text("Certificate of Completion")
                           .FontSize(32).Bold().FontColor("#333333");

                        col.Item().PaddingTop(10).AlignCenter()
                           .Text("This is to certify that")
                           .FontSize(14).FontColor("#666666");

                        // Student name — large and prominent
                        col.Item().PaddingTop(16).AlignCenter()
                           .Text(cert.StudentName)
                           .FontSize(28).Bold().FontColor("#764ba2");

                        col.Item().PaddingTop(10).AlignCenter()
                           .Text("has successfully completed the course")
                           .FontSize(14).FontColor("#666666");

                        // Course title
                        col.Item().PaddingTop(12).AlignCenter()
                           .Text(cert.CourseTitle)
                           .FontSize(22).Bold().FontColor("#333333");

                        col.Item().PaddingTop(8).AlignCenter()
                           .Text($"Instructed by: {cert.InstructorName}")
                           .FontSize(12).FontColor("#888888");

                        // Date and verification code
                        col.Item().PaddingTop(40)
                           .Row(row =>
                           {
                               row.RelativeItem()
                                  .Column(c =>
                                  {
                                      c.Item().Text($"Date: {cert.IssuedAt:MMMM dd, yyyy}")
                                             .FontSize(11).FontColor("#555555");
                                      c.Item().Text($"Certificate ID: {cert.VerificationCode}")
                                             .FontSize(10).FontColor("#999999");
                                  });
                           });
                    });
            });
        });

        // File I/O: save PDF asynchronously
        await Task.Run(() => document.GeneratePdf(fullPath));

        _logger.LogInformation("Certificate PDF generated at {Path}", fullPath);

        return Path.Combine("certificates", fileName).Replace("\\", "/");
    }
}
