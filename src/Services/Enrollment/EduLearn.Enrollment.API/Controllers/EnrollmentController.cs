using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EduLearn.Enrollment.API.Application.Commands;
using EduLearn.Enrollment.API.Application.DTOs;
using EduLearn.Enrollment.API.Application.Interfaces;
using EduLearn.Shared.Models;
using System.Security.Claims;

namespace EduLearn.Enrollment.API.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize]
public class EnrollmentController : ControllerBase
{
    private readonly IMediator _mediator;
    public EnrollmentController(IMediator mediator) => _mediator = mediator;

    // POST /api/v1/enrollments — enroll in a free course
    [HttpPost("enrollments")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Enroll([FromBody] EnrollRequest req)
    {
        var studentId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var cmd = new EnrollFreeCommand(studentId, req.CourseId, req.TotalLessons,
            User.FindFirst(ClaimTypes.Email)?.Value ?? "",
            User.FindFirst(ClaimTypes.Name)?.Value ?? "Student",
            "Course Title");   // Angular passes full name; simplified here

        var enrollment = await _mediator.Send(cmd);
        return StatusCode(201, ApiResponse<EnrollmentDto>.Created(enrollment, "Enrolled successfully!"));
    }

    // GET /api/v1/enrollments — student's enrolled courses
    [HttpGet("enrollments")]
    public async Task<IActionResult> GetMyEnrollments(
        [FromServices] IEnrollmentRepository repo,
        [FromServices] IHttpClientFactory httpFactory)
    {
        var studentId   = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var enrollments = await repo.GetByStudentAsync(studentId);
        
        // Fetch course details for each enrollment
        var httpClient = httpFactory.CreateClient();
        var enrichedEnrollments = new List<object>();
        
        foreach (var e in enrollments)
        {
            try
            {
                // Call Course API to get course details
                var courseResponse = await httpClient.GetAsync($"http://course-api:8080/api/v1/courses/{e.CourseId}");
                if (courseResponse.IsSuccessStatusCode)
                {
                    var courseJson = await courseResponse.Content.ReadAsStringAsync();
                    var courseData = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(courseJson);
                    var course = courseData.GetProperty("data");
                    
                    enrichedEnrollments.Add(new
                    {
                        enrollmentId = e.EnrollmentId,
                        studentId = e.StudentId,
                        courseId = e.CourseId,
                        enrolledAt = e.EnrolledAt,
                        status = e.Status.ToString(),
                        progressPct = e.ProgressPct,
                        totalLessons = e.TotalLessons,
                        completedLessons = e.CompletedLessons,
                        completedAt = e.CompletedAt,
                        isCompleted = e.Status == Domain.Enums.EnrollmentStatus.Completed,
                        completionPercentage = e.ProgressPct,
                        // Course details
                        courseTitle = course.GetProperty("title").GetString(),
                        courseSlug = course.GetProperty("slug").GetString(),
                        courseThumbnailUrl = course.TryGetProperty("thumbnailUrl", out var thumb) ? thumb.GetString() : null,
                        courseLevel = course.GetProperty("level").GetString(),
                        courseCategory = course.GetProperty("category").GetString()
                    });
                }
                else
                {
                    // Course not found, return basic enrollment info
                    enrichedEnrollments.Add(new
                    {
                        enrollmentId = e.EnrollmentId,
                        studentId = e.StudentId,
                        courseId = e.CourseId,
                        enrolledAt = e.EnrolledAt,
                        status = e.Status.ToString(),
                        progressPct = e.ProgressPct,
                        totalLessons = e.TotalLessons,
                        completedLessons = e.CompletedLessons,
                        completedAt = e.CompletedAt,
                        isCompleted = e.Status == Domain.Enums.EnrollmentStatus.Completed,
                        completionPercentage = e.ProgressPct,
                        courseTitle = "Course Not Found",
                        courseSlug = "",
                        courseThumbnailUrl = (string?)null,
                        courseLevel = "",
                        courseCategory = ""
                    });
                }
            }
            catch
            {
                // Error fetching course, return basic enrollment info
                enrichedEnrollments.Add(new
                {
                    enrollmentId = e.EnrollmentId,
                    studentId = e.StudentId,
                    courseId = e.CourseId,
                    enrolledAt = e.EnrolledAt,
                    status = e.Status.ToString(),
                    progressPct = e.ProgressPct,
                    totalLessons = e.TotalLessons,
                    completedLessons = e.CompletedLessons,
                    completedAt = e.CompletedAt,
                    isCompleted = e.Status == Domain.Enums.EnrollmentStatus.Completed,
                    completionPercentage = e.ProgressPct,
                    courseTitle = "Loading...",
                    courseSlug = "",
                    courseThumbnailUrl = (string?)null,
                    courseLevel = "",
                    courseCategory = ""
                });
            }
        }

        return Ok(ApiResponse<IEnumerable<object>>.Ok(enrichedEnrollments));
    }

    // GET /api/v1/enrollments/{courseId}/status
    [HttpGet("enrollments/{courseId:guid}/status")]
    public async Task<IActionResult> GetStatus(Guid courseId,
        [FromServices] IEnrollmentRepository repo)
    {
        var studentId  = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var enrollment = await repo.GetByStudentAndCourseAsync(studentId, courseId);
        var isEnrolled = enrollment is not null;

        return Ok(ApiResponse<object>.Ok(new { isEnrolled, enrollment?.ProgressPct, enrollment?.Status }));
    }

    // POST /api/v1/progress/update — video watch heartbeat (every 10 seconds)
    [HttpPost("progress/update")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> UpdateProgress([FromBody] UpdateProgressRequest req,
        [FromQuery] Guid courseId)
    {
        var studentId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result    = await _mediator.Send(new UpdateProgressCommand(
            studentId, courseId, req.LessonId, req.WatchedSeconds, req.TotalSeconds));

        return Ok(ApiResponse<LessonProgressDto>.Ok(result));
    }

    // POST /api/v1/progress/complete — mark article/quiz lesson complete
    [HttpPost("progress/complete")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> CompleteLesson([FromBody] CompleteLessonRequest req,
        [FromQuery] Guid courseId)
    {
        var studentId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result    = await _mediator.Send(new CompleteLessonCommand(studentId, courseId, req.LessonId));

        return Ok(ApiResponse<LessonProgressDto>.Ok(result, "Lesson marked complete."));
    }

    // GET /api/v1/progress/{courseId} — full course progress summary
    [HttpGet("progress/{courseId:guid}")]
    public async Task<IActionResult> GetCourseProgress(Guid courseId,
        [FromServices] IEnrollmentRepository repo)
    {
        var studentId  = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var enrollment = await repo.GetByStudentAndCourseAsync(studentId, courseId)
                         ?? throw new Shared.Exceptions.NotFoundException("Enrollment", courseId);

        var full = await repo.GetWithProgressAsync(enrollment.EnrollmentId);
        var progressDtos = full!.LessonProgresses.Select(p =>
            new LessonProgressDto(p.LessonId, p.Status.ToString(), p.WatchedSeconds, p.CompletedAt));

        var dto = new CourseProgressDto(enrollment.EnrollmentId, enrollment.ProgressPct,
                                        enrollment.Status.ToString(), progressDtos);

        return Ok(ApiResponse<CourseProgressDto>.Ok(dto));
    }

    // GET /api/v1/enrollments/all — Admin only, get all enrollments
    [HttpGet("enrollments/all")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllEnrollments([FromServices] IEnrollmentRepository repo)
    {
        var enrollments = await repo.GetAllAsync();
        var dtos = enrollments.Select(e => new
        {
            enrollmentId = e.EnrollmentId,
            studentId = e.StudentId,
            courseId = e.CourseId,
            enrolledAt = e.EnrolledAt,
            status = e.Status.ToString(),
            progressPct = e.ProgressPct,
            totalLessons = e.TotalLessons,
            completedLessons = e.CompletedLessons
        });
        return Ok(ApiResponse<IEnumerable<object>>.Ok(dtos));
    }

    // GET /api/v1/enrollments/course/{courseId}/count — Get enrollment count for a course
    [HttpGet("enrollments/course/{courseId:guid}/count")]
    public async Task<IActionResult> GetCourseEnrollmentCount(Guid courseId,
        [FromServices] IEnrollmentRepository repo)
    {
        var enrollments = await repo.GetByCourseAsync(courseId);
        var count = enrollments.Count();
        return Ok(ApiResponse<object>.Ok(new { count }));
    }
}
