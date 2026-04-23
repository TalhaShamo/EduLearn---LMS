using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using EduLearn.Certificate.API.Infrastructure.Data;
using EduLearn.Shared.Models;
using System.Security.Claims;

namespace EduLearn.Certificate.API.Controllers;

[ApiController]
[Route("api/v1/certificates")]
public class CertificatesController : ControllerBase
{
    private readonly CertificateDbContext _db;
    private readonly IWebHostEnvironment  _env;

    public CertificatesController(CertificateDbContext db, IWebHostEnvironment env)
    { _db = db; _env = env; }

    // GET /api/v1/certificates — student's certificates
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetMyCertificates()
    {
        var studentId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var certs     = await _db.Certificates
                                 .Where(c => c.StudentId == studentId)
                                 .ToListAsync();

        return Ok(ApiResponse<IEnumerable<object>>.Ok(certs.Select(c => new
        {
            c.CertificateId, c.CourseTitle, c.StudentName,
            c.InstructorName, c.VerificationCode, c.IssuedAt,
            DownloadUrl = $"/api/v1/certificates/{c.CertificateId}/download"
        })));
    }

    // GET /api/v1/certificates/{id}/download — download PDF
    [HttpGet("{id:guid}/download")]
    [Authorize]
    public async Task<IActionResult> Download(Guid id)
    {
        var cert = await _db.Certificates.FindAsync(id);
        if (cert is null) return NotFound();

        var studentId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        if (cert.StudentId != studentId) return Forbid();

        var fullPath = Path.Combine(_env.WebRootPath, cert.FilePath.Replace("/", Path.DirectorySeparatorChar.ToString()));
        if (!System.IO.File.Exists(fullPath))
            return NotFound(ApiResponse<string>.Fail("Certificate file not found."));

        // Stream the PDF file to the client
        var fileName = $"EduLearn-Certificate-{cert.VerificationCode}.pdf";
        return PhysicalFile(fullPath, "application/pdf", fileName);
    }

    // GET /api/v1/certificates/verify/{code} — public verification (no auth required)
    [HttpGet("verify/{code}")]
    [AllowAnonymous]
    public async Task<IActionResult> Verify(string code)
    {
        var cert = await _db.Certificates
                            .FirstOrDefaultAsync(c => c.VerificationCode == code);

        if (cert is null)
            return NotFound(ApiResponse<string>.Fail("Certificate not found or invalid code."));

        return Ok(ApiResponse<object>.Ok(new
        {
            cert.CertificateId, cert.StudentName, cert.CourseTitle,
            cert.InstructorName, cert.IssuedAt, IsValid = true
        }, "Certificate is authentic."));
    }
}
