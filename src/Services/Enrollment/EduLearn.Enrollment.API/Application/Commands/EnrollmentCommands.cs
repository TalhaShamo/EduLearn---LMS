using MediatR;
using MassTransit;
using EduLearn.Enrollment.API.Application.DTOs;
using EduLearn.Enrollment.API.Application.Interfaces;
using EduLearn.Enrollment.API.Domain.Entities;
using EduLearn.Shared.Events;
using EduLearn.Shared.Exceptions;
using EnrollmentEntity = EduLearn.Enrollment.API.Domain.Entities.Enrollment;

namespace EduLearn.Enrollment.API.Application.Commands;

// ── ENROLL (FREE COURSE) ──────────────────────────────────────
public record EnrollFreeCommand(Guid StudentId, Guid CourseId, int TotalLessons,
    string StudentEmail, string StudentName, string CourseTitle) : IRequest<EnrollmentDto>;

public class EnrollFreeCommandHandler : IRequestHandler<EnrollFreeCommand, EnrollmentDto>
{
    private readonly IEnrollmentRepository _enrollRepo;
    private readonly IPublishEndpoint      _bus;

    public EnrollFreeCommandHandler(IEnrollmentRepository repo, IPublishEndpoint bus)
    { _enrollRepo = repo; _bus = bus; }

    public async Task<EnrollmentDto> Handle(EnrollFreeCommand cmd, CancellationToken ct)
    {
        // Prevent duplicate enrollment
        var existing = await _enrollRepo.GetByStudentAndCourseAsync(cmd.StudentId, cmd.CourseId);
        if (existing is not null)
            throw new ConflictException("You are already enrolled in this course.");

        // Create enrollment entity
        var enrollment = EnrollmentEntity.CreateFree(cmd.StudentId, cmd.CourseId, cmd.TotalLessons);
        await _enrollRepo.AddAsync(enrollment);
        await _enrollRepo.SaveChangesAsync();

        // Publish event → Notification.API sends confirmation email
        await _bus.Publish(new StudentEnrolledEvent(
            enrollment.EnrollmentId, cmd.StudentId, cmd.CourseId,
            cmd.StudentEmail, cmd.StudentName, cmd.CourseTitle, enrollment.EnrolledAt), ct);

        return MapToDto(enrollment);
    }

    internal static EnrollmentDto MapToDto(EnrollmentEntity e) => new(
        e.EnrollmentId, e.StudentId, e.CourseId, e.EnrolledAt,
        e.Status.ToString(), e.ProgressPct, e.TotalLessons, e.CompletedLessons, e.CompletedAt);
}

// ── CREATE PAID ENROLLMENT (called by Payment saga consumer) ──
public record CreatePaidEnrollmentCommand(Guid StudentId, Guid CourseId,
    int TotalLessons, Guid PaymentId,
    string StudentEmail, string StudentName, string CourseTitle) : IRequest<EnrollmentDto>;

public class CreatePaidEnrollmentCommandHandler : IRequestHandler<CreatePaidEnrollmentCommand, EnrollmentDto>
{
    private readonly IEnrollmentRepository _enrollRepo;
    private readonly IPublishEndpoint      _bus;

    public CreatePaidEnrollmentCommandHandler(IEnrollmentRepository repo, IPublishEndpoint bus)
    { _enrollRepo = repo; _bus = bus; }

    public async Task<EnrollmentDto> Handle(CreatePaidEnrollmentCommand cmd, CancellationToken ct)
    {
        // Idempotency: skip if already enrolled (saga may retry)
        var existing = await _enrollRepo.GetByStudentAndCourseAsync(cmd.StudentId, cmd.CourseId);
        if (existing is not null)
            return EnrollFreeCommandHandler.MapToDto(existing);

        var enrollment = EnrollmentEntity.CreatePaid(cmd.StudentId, cmd.CourseId, cmd.TotalLessons, cmd.PaymentId);
        await _enrollRepo.AddAsync(enrollment);
        await _enrollRepo.SaveChangesAsync();

        // Notify student of enrollment
        await _bus.Publish(new StudentEnrolledEvent(
            enrollment.EnrollmentId, cmd.StudentId, cmd.CourseId,
            cmd.StudentEmail, cmd.StudentName, cmd.CourseTitle, enrollment.EnrolledAt), ct);

        return EnrollFreeCommandHandler.MapToDto(enrollment);
    }
}

// ── UPDATE VIDEO PROGRESS (heartbeat) ────────────────────────
public record UpdateProgressCommand(Guid StudentId, Guid CourseId,
    Guid LessonId, int WatchedSeconds, int TotalSeconds) : IRequest<LessonProgressDto>;

public class UpdateProgressCommandHandler : IRequestHandler<UpdateProgressCommand, LessonProgressDto>
{
    private readonly IEnrollmentRepository    _enrollRepo;
    private readonly ILessonProgressRepository _progressRepo;
    private readonly IPublishEndpoint          _bus;

    public UpdateProgressCommandHandler(IEnrollmentRepository e, ILessonProgressRepository p, IPublishEndpoint bus)
    { _enrollRepo = e; _progressRepo = p; _bus = bus; }

    public async Task<LessonProgressDto> Handle(UpdateProgressCommand cmd, CancellationToken ct)
    {
        // Validate enrollment
        var enrollment = await _enrollRepo.GetByStudentAndCourseAsync(cmd.StudentId, cmd.CourseId)
                         ?? throw new NotFoundException("Enrollment", $"{cmd.StudentId}/{cmd.CourseId}");

        // Upsert: find existing progress or create new record
        var progress = await _progressRepo.GetByEnrollmentAndLessonAsync(enrollment.EnrollmentId, cmd.LessonId);
        bool isNew   = progress is null;

        if (isNew)
            progress = LessonProgress.Create(enrollment.EnrollmentId, cmd.LessonId);

        var wasCompleted = progress!.Status == Domain.Enums.LessonProgressStatus.Completed;
        progress.UpdateVideoProgress(cmd.WatchedSeconds, cmd.TotalSeconds);

        // If lesson just became completed for the first time, update enrollment progress
        if (!wasCompleted && progress.Status == Domain.Enums.LessonProgressStatus.Completed)
        {
            enrollment.RecordLessonCompletion();
            _enrollRepo.Update(enrollment);

            // If course is now 100% complete, publish event → Certificate.API
            if (enrollment.Status == Domain.Enums.EnrollmentStatus.Completed)
                await PublishCourseCompleted(enrollment, cmd, ct);
        }

        if (isNew) await _progressRepo.AddAsync(progress);
        else       _progressRepo.Update(progress);

        await _progressRepo.SaveChangesAsync();

        return new LessonProgressDto(progress.LessonId, progress.Status.ToString(),
                                     progress.WatchedSeconds, progress.CompletedAt);
    }

    private async Task PublishCourseCompleted(EnrollmentEntity enrollment, UpdateProgressCommand cmd, CancellationToken ct)
    {
        // CourseCompletedEvent → Certificate.API will generate the PDF certificate
        await _bus.Publish(new CourseCompletedEvent(
            enrollment.EnrollmentId, cmd.StudentId, cmd.CourseId,
            "Student",        // Angular will send full name — simplified here
            "student@mail",   // Real impl: lookup from Identity.API or include in command
            "Course Title",   // Real impl: lookup from Course.API or pass in command
            "Instructor",
            enrollment.CompletedAt ?? DateTime.UtcNow), ct);
    }
}

// ── COMPLETE LESSON MANUALLY (article / quiz pass) ────────────
public record CompleteLessonCommand(Guid StudentId, Guid CourseId, Guid LessonId) : IRequest<LessonProgressDto>;

public class CompleteLessonCommandHandler : IRequestHandler<CompleteLessonCommand, LessonProgressDto>
{
    private readonly IEnrollmentRepository    _enrollRepo;
    private readonly ILessonProgressRepository _progressRepo;
    private readonly IPublishEndpoint          _bus;

    public CompleteLessonCommandHandler(IEnrollmentRepository e, ILessonProgressRepository p, IPublishEndpoint bus)
    { _enrollRepo = e; _progressRepo = p; _bus = bus; }

    public async Task<LessonProgressDto> Handle(CompleteLessonCommand cmd, CancellationToken ct)
    {
        var enrollment = await _enrollRepo.GetByStudentAndCourseAsync(cmd.StudentId, cmd.CourseId)
                         ?? throw new NotFoundException("Enrollment", $"{cmd.StudentId}/{cmd.CourseId}");

        var progress = await _progressRepo.GetByEnrollmentAndLessonAsync(enrollment.EnrollmentId, cmd.LessonId);
        bool isNew   = progress is null;

        if (isNew)
            progress = LessonProgress.Create(enrollment.EnrollmentId, cmd.LessonId);

        if (progress!.Status != Domain.Enums.LessonProgressStatus.Completed)
        {
            progress.MarkCompleted();
            enrollment.RecordLessonCompletion();
            _enrollRepo.Update(enrollment);

            if (enrollment.Status == Domain.Enums.EnrollmentStatus.Completed)
                await _bus.Publish(new CourseCompletedEvent(
                    enrollment.EnrollmentId, cmd.StudentId, cmd.CourseId,
                    "Student", "student@mail", "Course Title", "Instructor",
                    enrollment.CompletedAt ?? DateTime.UtcNow), ct);
        }

        if (isNew) await _progressRepo.AddAsync(progress);
        else       _progressRepo.Update(progress);

        await _progressRepo.SaveChangesAsync();

        return new LessonProgressDto(progress.LessonId, progress.Status.ToString(),
                                     progress.WatchedSeconds, progress.CompletedAt);
    }
}
