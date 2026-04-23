using MediatR;
using EduLearn.Course.API.Application.DTOs;
using EduLearn.Course.API.Application.Interfaces;
using CourseEntity = EduLearn.Course.API.Domain.Entities.Course;
using EduLearn.Shared.Exceptions;

namespace EduLearn.Course.API.Application.Queries;

// ── GET ALL PUBLISHED COURSES (Catalog) ───────────────────────
public record GetPublishedCoursesQuery(
    int     Page     = 1,
    int     PageSize = 20,
    string? Search   = null,
    string? Category = null
) : IRequest<IEnumerable<CourseListDto>>;

public class GetPublishedCoursesQueryHandler : IRequestHandler<GetPublishedCoursesQuery, IEnumerable<CourseListDto>>
{
    private readonly ICourseRepository _repo;
    public GetPublishedCoursesQueryHandler(ICourseRepository repo) => _repo = repo;

    public async Task<IEnumerable<CourseListDto>> Handle(GetPublishedCoursesQuery q, CancellationToken ct)
    {
        var courses = await _repo.GetPublishedPagedAsync(q.Page, q.PageSize, q.Search, q.Category);

        // Project to lightweight list DTO using LINQ (Collections)
        return courses.Select(c => new CourseListDto(
            c.CourseId, c.Title, c.Slug, c.Category,
            c.Level.ToString(), c.Price, c.Status.ToString(),
            c.ThumbnailUrl, c.InstructorId, c.CreatedAt));
    }
}

// ── GET COURSE DETAIL ─────────────────────────────────────────
public record GetCourseDetailQuery(Guid CourseId) : IRequest<CourseDetailDto>;

public class GetCourseDetailQueryHandler : IRequestHandler<GetCourseDetailQuery, CourseDetailDto>
{
    private readonly ICourseRepository _repo;
    public GetCourseDetailQueryHandler(ICourseRepository repo) => _repo = repo;

    public async Task<CourseDetailDto> Handle(GetCourseDetailQuery q, CancellationToken ct)
    {
        var course = await _repo.GetWithSectionsAsync(q.CourseId)
                     ?? throw new NotFoundException("Course", q.CourseId);

        return Commands.CreateCourseCommandHandler.MapToDetail(course);
    }
}

// ── GET INSTRUCTOR'S OWN COURSES ──────────────────────────────
public record GetInstructorCoursesQuery(Guid InstructorId) : IRequest<IEnumerable<CourseListDto>>;

public class GetInstructorCoursesQueryHandler : IRequestHandler<GetInstructorCoursesQuery, IEnumerable<CourseListDto>>
{
    private readonly ICourseRepository _repo;
    public GetInstructorCoursesQueryHandler(ICourseRepository repo) => _repo = repo;

    public async Task<IEnumerable<CourseListDto>> Handle(GetInstructorCoursesQuery q, CancellationToken ct)
    {
        var courses = await _repo.GetByInstructorAsync(q.InstructorId);
        return courses.Select(c => new CourseListDto(
            c.CourseId, c.Title, c.Slug, c.Category,
            c.Level.ToString(), c.Price, c.Status.ToString(),
            c.ThumbnailUrl, c.InstructorId, c.CreatedAt));
    }
}

// ── ADMIN: GET PENDING REVIEW COURSES ────────────────────────
public record GetPendingCoursesQuery : IRequest<IEnumerable<CourseListDto>>;

public class GetPendingCoursesQueryHandler : IRequestHandler<GetPendingCoursesQuery, IEnumerable<CourseListDto>>
{
    private readonly ICourseRepository _repo;
    public GetPendingCoursesQueryHandler(ICourseRepository repo) => _repo = repo;

    public async Task<IEnumerable<CourseListDto>> Handle(GetPendingCoursesQuery q, CancellationToken ct)
    {
        var courses = await _repo.GetPendingReviewAsync();
        return courses.Select(c => new CourseListDto(
            c.CourseId, c.Title, c.Slug, c.Category,
            c.Level.ToString(), c.Price, c.Status.ToString(),
            c.ThumbnailUrl, c.InstructorId, c.CreatedAt));
    }
}
