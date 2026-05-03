using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.IdentityModel.Tokens;
using EduLearn.Course.API.Application.Commands;
using EduLearn.Course.API.Application.DTOs;
using EduLearn.Course.API.Application.Interfaces;
using EduLearn.Shared.Models;
using System.Security.Claims;
using System.Text;

namespace EduLearn.Course.API.Controllers;

[ApiController]
[Route("api/v1")]
public class LessonsController : ControllerBase
{
    private readonly IMediator            _mediator;
    private readonly IVideoStorageService _storage;

    public LessonsController(IMediator mediator, IVideoStorageService storage)
    {
        _mediator = mediator;
        _storage  = storage;
    }

    // POST /api/v1/courses/{courseId}/sections
    [HttpPost("courses/{courseId:guid}/sections")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> AddSection(Guid courseId, [FromBody] CreateSectionRequest req)
    {
        var instructorId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var section = await _mediator.Send(new AddSectionCommand(courseId, instructorId, req.Title, req.SortOrder));
        return StatusCode(201, ApiResponse<SectionDto>.Created(section));
    }

    // POST /api/v1/sections/{sectionId}/lessons
    [HttpPost("sections/{sectionId:guid}/lessons")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> AddLesson(Guid sectionId, [FromQuery] Guid courseId, [FromBody] CreateLessonRequest req)
    {
        var instructorId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var lesson = await _mediator.Send(new AddLessonCommand(
            sectionId, courseId, instructorId, req.Title, req.GetLessonType(), req.SortOrder, req.IsFreePreview));
        return StatusCode(201, ApiResponse<LessonDto>.Created(lesson));
    }

    // POST /api/v1/lessons/{lessonId}/upload-video
    // Uses IFormFile — Angular will send multipart/form-data
    [HttpPost("lessons/{lessonId:guid}/upload-video")]
    [Authorize(Roles = "Instructor")]
    [RequestSizeLimit(4L * 1024 * 1024 * 1024)] // 4 GB max
    public async Task<IActionResult> UploadVideo(Guid lessonId, [FromQuery] Guid courseId,
        IFormFile file, [FromQuery] int durationSeconds = 0)
    {
        if (file is null || file.Length == 0)
            return BadRequest(ApiResponse<string>.Fail("No video file provided."));

        var instructorId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        await using var stream = file.OpenReadStream();
        var path = await _mediator.Send(new UploadVideoCommand(
            lessonId, courseId, instructorId, stream, file.FileName, durationSeconds));

        return Ok(ApiResponse<string>.Ok(path, "Video uploaded successfully."));
    }

    // GET /api/v1/lessons/{lessonId}/stream?token=xxx
    // Byte-range video streaming — Angular <video> tag hits this endpoint
    // Token passed as query param since <video> tag cannot send Authorization header
    [HttpGet("lessons/{lessonId:guid}/stream")]
    [AllowAnonymous] // We'll manually validate token from query string
    public async Task<IActionResult> StreamVideo(
        Guid lessonId,
        [FromQuery] string? token,
        [FromServices] ILessonRepository lessonRepo,
        [FromServices] IConfiguration config)
    {
        // Validate JWT token from query parameter
        if (string.IsNullOrEmpty(token))
            return Unauthorized(ApiResponse<string>.Fail("Authentication token required."));

        try
        {
            var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(config["Jwt:SecretKey"]!);
            
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = config["Jwt:Issuer"],
                ValidAudience = config["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.Zero
            }, out _);
        }
        catch
        {
            return Unauthorized(ApiResponse<string>.Fail("Invalid or expired token."));
        }

        var lesson = await lessonRepo.GetByIdAsync(lessonId);
        if (lesson?.VideoPath is null)
            return NotFound(ApiResponse<string>.Fail("Video not found."));

        var fullPath = _storage.GetVideoPath(lesson.VideoPath);
        if (!System.IO.File.Exists(fullPath))
            return NotFound(ApiResponse<string>.Fail("Video file missing from storage."));

        // Serve with byte-range support so video player can seek
        var provider    = new FileExtensionContentTypeProvider();
        var contentType = provider.TryGetContentType(fullPath, out var ct) ? ct : "video/mp4";

        return PhysicalFile(fullPath, contentType, enableRangeProcessing: true);
    }
}
