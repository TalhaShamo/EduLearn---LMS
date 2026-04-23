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
        [FromServices] IEnrollmentRepository repo)
    {
        var studentId   = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var enrollments = await repo.GetByStudentAsync(studentId);
        var dtos = enrollments.Select(e => new EnrollmentDto(
            e.EnrollmentId, e.StudentId, e.CourseId, e.EnrolledAt,
            e.Status.ToString(), e.ProgressPct, e.TotalLessons, e.CompletedLessons, e.CompletedAt));

        return Ok(ApiResponse<IEnumerable<EnrollmentDto>>.Ok(dtos));
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
}
