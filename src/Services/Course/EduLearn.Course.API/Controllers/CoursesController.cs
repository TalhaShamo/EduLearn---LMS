using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EduLearn.Course.API.Application.Commands;
using EduLearn.Course.API.Application.Queries;
using EduLearn.Course.API.Application.DTOs;
using EduLearn.Shared.Models;
using System.Security.Claims;

namespace EduLearn.Course.API.Controllers;

[ApiController]
[Route("api/v1/courses")]
public class CoursesController : ControllerBase
{
    private readonly IMediator _mediator;
    public CoursesController(IMediator mediator) => _mediator = mediator;

    // GET /api/v1/courses?page=1&pageSize=20&search=python&category=Programming
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null, [FromQuery] string? category = null)
    {
        var courses = await _mediator.Send(new GetPublishedCoursesQuery(page, pageSize, search, category));
        return Ok(ApiResponse<IEnumerable<CourseListDto>>.Ok(courses));
    }

    // GET /api/v1/courses/{id}
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id)
    {
        var course = await _mediator.Send(new GetCourseDetailQuery(id));
        return Ok(ApiResponse<CourseDetailDto>.Ok(course));
    }

    // GET /api/v1/courses/my — instructor's own courses
    [HttpGet("my")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> GetMyCourses()
    {
        var instructorId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var courses = await _mediator.Send(new GetInstructorCoursesQuery(instructorId));
        return Ok(ApiResponse<IEnumerable<CourseListDto>>.Ok(courses));
    }

    // POST /api/v1/courses — create draft
    [HttpPost]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> Create([FromBody] CreateCourseRequest req)
    {
        var instructorId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var cmd    = new CreateCourseCommand(instructorId, req.Title, req.Description,
                                             req.Category, req.Level, req.Price, req.Language);
        var course = await _mediator.Send(cmd);
        return StatusCode(201, ApiResponse<CourseDetailDto>.Created(course));
    }

    // PUT /api/v1/courses/{id}
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCourseRequest req)
    {
        var instructorId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var cmd    = new UpdateCourseCommand(id, instructorId, req.Title, req.Description,
                                             req.Category, req.Level, req.Price);
        var course = await _mediator.Send(cmd);
        return Ok(ApiResponse<CourseDetailDto>.Ok(course));
    }

    // PATCH /api/v1/courses/{id}/submit-review
    [HttpPatch("{id:guid}/submit-review")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> SubmitForReview(Guid id)
    {
        var instructorId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        await _mediator.Send(new SubmitForReviewCommand(id, instructorId));
        return Ok(ApiResponse<string>.Ok("", "Course submitted for admin review."));
    }

    // PATCH /api/v1/courses/{id}/approve — Admin only
    [HttpPatch("{id:guid}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Approve(Guid id)
    {
        await _mediator.Send(new ApproveCourseCommand(id));
        return Ok(ApiResponse<string>.Ok("", "Course approved and published."));
    }

    // PATCH /api/v1/courses/{id}/request-changes — Admin only
    [HttpPatch("{id:guid}/request-changes")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RequestChanges(Guid id, [FromBody] RequestChangesRequest req)
    {
        await _mediator.Send(new RequestChangesCommand(id, req.Feedback));
        return Ok(ApiResponse<string>.Ok("", "Changes requested."));
    }

    // GET /api/v1/courses/pending — Admin only
    [HttpGet("pending")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetPending()
    {
        var courses = await _mediator.Send(new GetPendingCoursesQuery());
        return Ok(ApiResponse<IEnumerable<CourseListDto>>.Ok(courses));
    }
}
