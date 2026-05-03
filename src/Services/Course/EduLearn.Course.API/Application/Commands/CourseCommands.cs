using MediatR;
using EduLearn.Course.API.Application.DTOs;
using EduLearn.Course.API.Application.Interfaces;
using CourseEntity = EduLearn.Course.API.Domain.Entities.Course;
using EduLearn.Course.API.Domain.Entities;
using EduLearn.Course.API.Domain.Enums;
using EduLearn.Shared.Exceptions;
using EduLearn.Course.API.Infrastructure.Data;

namespace EduLearn.Course.API.Application.Commands;

// ── CREATE COURSE ─────────────────────────────────────────────
public record CreateCourseCommand(
    Guid   InstructorId,
    string Title, string? Subtitle, string Description,
    string Category, string Level,
    decimal Price, string Language,
    List<string>? Tags, List<string>? LearningObjectives,
    List<CreateSectionRequest>? Sections
) : IRequest<CourseDetailDto>;

public class CreateCourseCommandHandler : IRequestHandler<CreateCourseCommand, CourseDetailDto>
{
    private readonly ICourseRepository _repo;
    public CreateCourseCommandHandler(ICourseRepository repo) => _repo = repo;

    public async Task<CourseDetailDto> Handle(CreateCourseCommand cmd, CancellationToken ct)
    {
        var level  = Enum.Parse<CourseLevel>(cmd.Level, ignoreCase: true);
        var course = CourseEntity.Create(cmd.InstructorId, cmd.Title, cmd.Description,
                                   cmd.Category, level, cmd.Price, cmd.Language);

        // Set additional metadata
        course.Update(cmd.Title, cmd.Description, cmd.Category, level, cmd.Price, cmd.Subtitle);
        course.SetMetadata(tags: cmd.Tags ?? new List<string>(), learningObjectives: cmd.LearningObjectives ?? new List<string>());

        // Add sections and lessons if provided
        if (cmd.Sections != null && cmd.Sections.Any())
        {
            foreach (var sectionReq in cmd.Sections)
            {
                var section = Section.Create(course.CourseId, sectionReq.Title, sectionReq.SortOrder);
                course.Sections.Add(section);

                if (sectionReq.Lessons != null && sectionReq.Lessons.Any())
                {
                    foreach (var lessonReq in sectionReq.Lessons)
                    {
                        var lessonType = Enum.Parse<LessonType>(lessonReq.GetLessonType(), ignoreCase: true);
                        var lesson = Lesson.Create(section.SectionId, lessonReq.Title, lessonType, lessonReq.SortOrder);
                        lesson.SetFreePreview(lessonReq.IsFreePreview);
                        section.Lessons.Add(lesson);
                    }
                }
            }
        }

        await _repo.AddAsync(course);
        await _repo.SaveChangesAsync();
        return MapToDetail(course);
    }

    internal static CourseDetailDto MapToDetail(CourseEntity c) => new(
        c.CourseId, c.Title, c.Subtitle, c.Slug, c.Description, c.Category, c.Category,
        c.Level.ToString(), c.Price, c.Language, c.Status.ToString(),
        c.ThumbnailUrl, c.AdminFeedback, c.InstructorId, c.InstructorName,
        c.EnrollmentCount, c.AverageRating, c.ReviewCount, c.DurationMinutes,
        c.Tags, c.LearningObjectives, c.CreatedAt, c.UpdatedAt,
        c.Sections.Select(s => new SectionDto(s.SectionId, s.Title, s.SortOrder,
            s.Lessons.Select(l => new LessonDto(l.LessonId, l.Title, l.Type.ToString(),
                l.VideoPath, l.DurationSeconds, l.IsFreePreview, l.SortOrder, l.IsPublished)))));
}

// ── UPDATE COURSE ─────────────────────────────────────────────
public record UpdateCourseCommand(
    Guid CourseId, Guid InstructorId,
    string Title, string? Subtitle, string Description,
    string Category, string Level, decimal Price,
    List<string>? Tags, List<string>? LearningObjectives,
    List<CreateSectionRequest>? Sections
) : IRequest<CourseDetailDto>;

public class UpdateCourseCommandHandler : IRequestHandler<UpdateCourseCommand, CourseDetailDto>
{
    private readonly ICourseRepository _repo;

    public UpdateCourseCommandHandler(ICourseRepository repo)
    { 
        _repo = repo; 
    }

    public async Task<CourseDetailDto> Handle(UpdateCourseCommand cmd, CancellationToken ct)
    {
        var course = await _repo.GetByIdAsync(cmd.CourseId)
                     ?? throw new NotFoundException("Course", cmd.CourseId);

        // Only the owning instructor can update their course
        if (course.InstructorId != cmd.InstructorId)
            throw new ForbiddenException("You can only update your own courses.");

        var level = Enum.Parse<CourseLevel>(cmd.Level, ignoreCase: true);
        course.Update(cmd.Title, cmd.Description, cmd.Category, level, cmd.Price, cmd.Subtitle);
        course.SetMetadata(tags: cmd.Tags ?? new List<string>(), learningObjectives: cmd.LearningObjectives ?? new List<string>());

        // Don't update sections - they are managed separately via video upload
        // This avoids the concurrency exception when updating after video upload

        _repo.Update(course);
        await _repo.SaveChangesAsync();
        
        // Reload with sections for response
        var updatedCourse = await _repo.GetWithSectionsAsync(cmd.CourseId);
        return CreateCourseCommandHandler.MapToDetail(updatedCourse!);
    }
}

// ── SUBMIT FOR REVIEW ─────────────────────────────────────────
public record SubmitForReviewCommand(Guid CourseId, Guid InstructorId) : IRequest;

public class SubmitForReviewCommandHandler : IRequestHandler<SubmitForReviewCommand>
{
    private readonly ICourseRepository _repo;
    public SubmitForReviewCommandHandler(ICourseRepository repo) => _repo = repo;

    public async Task Handle(SubmitForReviewCommand cmd, CancellationToken ct)
    {
        var course = await _repo.GetByIdAsync(cmd.CourseId)
                     ?? throw new NotFoundException("Course", cmd.CourseId);

        if (course.InstructorId != cmd.InstructorId)
            throw new ForbiddenException("You can only submit your own courses.");

        course.SubmitForReview();
        _repo.Update(course);
        await _repo.SaveChangesAsync();
    }
}

// ── ADMIN: APPROVE COURSE ─────────────────────────────────────
public record ApproveCourseCommand(Guid CourseId) : IRequest;

public class ApproveCourseCommandHandler : IRequestHandler<ApproveCourseCommand>
{
    private readonly ICourseRepository _repo;
    public ApproveCourseCommandHandler(ICourseRepository repo) => _repo = repo;

    public async Task Handle(ApproveCourseCommand cmd, CancellationToken ct)
    {
        var course = await _repo.GetByIdAsync(cmd.CourseId)
                     ?? throw new NotFoundException("Course", cmd.CourseId);
        course.Publish();
        _repo.Update(course);
        await _repo.SaveChangesAsync();
    }
}

// ── ADMIN: REQUEST CHANGES ────────────────────────────────────
public record RequestChangesCommand(Guid CourseId, string Feedback) : IRequest;

public class RequestChangesCommandHandler : IRequestHandler<RequestChangesCommand>
{
    private readonly ICourseRepository _repo;
    public RequestChangesCommandHandler(ICourseRepository repo) => _repo = repo;

    public async Task Handle(RequestChangesCommand cmd, CancellationToken ct)
    {
        var course = await _repo.GetByIdAsync(cmd.CourseId)
                     ?? throw new NotFoundException("Course", cmd.CourseId);
        course.RequestChanges(cmd.Feedback);
        _repo.Update(course);
        await _repo.SaveChangesAsync();
    }
}

// ── ADD SECTION ───────────────────────────────────────────────
public record AddSectionCommand(Guid CourseId, Guid InstructorId, string Title, int SortOrder) : IRequest<SectionDto>;

public class AddSectionCommandHandler : IRequestHandler<AddSectionCommand, SectionDto>
{
    private readonly ICourseRepository   _courseRepo;
    private readonly ISectionRepository  _sectionRepo;

    public AddSectionCommandHandler(ICourseRepository c, ISectionRepository s)
    { _courseRepo = c; _sectionRepo = s; }

    public async Task<SectionDto> Handle(AddSectionCommand cmd, CancellationToken ct)
    {
        var course = await _courseRepo.GetByIdAsync(cmd.CourseId)
                     ?? throw new NotFoundException("Course", cmd.CourseId);

        if (course.InstructorId != cmd.InstructorId)
            throw new ForbiddenException("You can only edit your own courses.");

        var section = Section.Create(cmd.CourseId, cmd.Title, cmd.SortOrder);
        await _sectionRepo.AddAsync(section);
        await _sectionRepo.SaveChangesAsync();

        return new SectionDto(section.SectionId, section.Title, section.SortOrder, []);
    }
}

// ── ADD LESSON ────────────────────────────────────────────────
public record AddLessonCommand(
    Guid SectionId, Guid CourseId, Guid InstructorId,
    string Title, string Type, int SortOrder, bool IsFreePreview
) : IRequest<LessonDto>;

public class AddLessonCommandHandler : IRequestHandler<AddLessonCommand, LessonDto>
{
    private readonly ICourseRepository  _courseRepo;
    private readonly ILessonRepository  _lessonRepo;

    public AddLessonCommandHandler(ICourseRepository c, ILessonRepository l)
    { _courseRepo = c; _lessonRepo = l; }

    public async Task<LessonDto> Handle(AddLessonCommand cmd, CancellationToken ct)
    {
        var course = await _courseRepo.GetByIdAsync(cmd.CourseId)
                     ?? throw new NotFoundException("Course", cmd.CourseId);

        if (course.InstructorId != cmd.InstructorId)
            throw new ForbiddenException("You can only edit your own courses.");

        var type   = Enum.Parse<LessonType>(cmd.Type, ignoreCase: true);
        var lesson = Lesson.Create(cmd.SectionId, cmd.Title, type, cmd.SortOrder);
        lesson.SetFreePreview(cmd.IsFreePreview);

        await _lessonRepo.AddAsync(lesson);
        await _lessonRepo.SaveChangesAsync();

        return new LessonDto(lesson.LessonId, lesson.Title, lesson.Type.ToString(),
            lesson.VideoPath, lesson.DurationSeconds, lesson.IsFreePreview, lesson.SortOrder, lesson.IsPublished);
    }
}

// ── UPLOAD VIDEO ──────────────────────────────────────────────
public record UploadVideoCommand(
    Guid   LessonId, Guid CourseId, Guid InstructorId,
    Stream FileStream, string FileName, int DurationSeconds
) : IRequest<string>;   // Returns the relative video path

public class UploadVideoCommandHandler : IRequestHandler<UploadVideoCommand, string>
{
    private readonly ICourseRepository    _courseRepo;
    private readonly ILessonRepository    _lessonRepo;
    private readonly IVideoStorageService _storage;

    public UploadVideoCommandHandler(ICourseRepository c, ILessonRepository l, IVideoStorageService s)
    { _courseRepo = c; _lessonRepo = l; _storage = s; }

    public async Task<string> Handle(UploadVideoCommand cmd, CancellationToken ct)
    {
        var course = await _courseRepo.GetByIdAsync(cmd.CourseId)
                     ?? throw new NotFoundException("Course", cmd.CourseId);

        if (course.InstructorId != cmd.InstructorId)
            throw new ForbiddenException("You can only upload to your own courses.");

        var lesson = await _lessonRepo.GetByIdAsync(cmd.LessonId)
                     ?? throw new NotFoundException("Lesson", cmd.LessonId);

        // File I/O: save video to local storage
        var path = await _storage.SaveVideoAsync(cmd.CourseId, cmd.LessonId, cmd.FileStream, cmd.FileName);
        lesson.SetVideoPath(path, cmd.DurationSeconds);
        lesson.Publish();

        _lessonRepo.Update(lesson);
        await _lessonRepo.SaveChangesAsync();

        return path;
    }
}

// ── DELETE COURSE ─────────────────────────────────────────────
public record DeleteCourseCommand(Guid CourseId, Guid InstructorId) : IRequest;

public class DeleteCourseCommandHandler : IRequestHandler<DeleteCourseCommand>
{
    private readonly ICourseRepository _repo;
    public DeleteCourseCommandHandler(ICourseRepository repo) => _repo = repo;

    public async Task Handle(DeleteCourseCommand cmd, CancellationToken ct)
    {
        var course = await _repo.GetByIdAsync(cmd.CourseId)
                     ?? throw new NotFoundException("Course", cmd.CourseId);

        // Only the owning instructor can delete their course
        if (course.InstructorId != cmd.InstructorId)
            throw new ForbiddenException("You can only delete your own courses.");

        // Allow deletion of courses in any status (Draft, Published, PendingReview, Archived)
        _repo.Delete(course);
        await _repo.SaveChangesAsync();
    }
}
