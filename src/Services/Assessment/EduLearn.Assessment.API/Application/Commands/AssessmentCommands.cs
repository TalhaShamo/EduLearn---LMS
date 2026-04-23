using MediatR;
using MassTransit;
using EduLearn.Assessment.API.Application.Services;
using EduLearn.Assessment.API.Domain.Entities;
using EduLearn.Assessment.API.Domain.Enums;
using EduLearn.Assessment.API.Infrastructure.Data;
using EduLearn.Shared.Events;
using EduLearn.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace EduLearn.Assessment.API.Application.Commands;

// ── START QUIZ ATTEMPT ────────────────────────────────────────
public record StartAttemptCommand(Guid QuizId, Guid StudentId) : IRequest<Guid>;

public class StartAttemptCommandHandler : IRequestHandler<StartAttemptCommand, Guid>
{
    private readonly AssessmentDbContext _db;
    public StartAttemptCommandHandler(AssessmentDbContext db) => _db = db;

    public async Task<Guid> Handle(StartAttemptCommand cmd, CancellationToken ct)
    {
        // Prevent starting a new attempt while one is still in progress
        bool inProgress = await _db.Attempts.AnyAsync(
            a => a.QuizId == cmd.QuizId && a.StudentId == cmd.StudentId
              && a.Status == AttemptStatus.InProgress, ct);

        if (inProgress)
            throw new ConflictException("You already have an attempt in progress for this quiz.");

        // Check max attempts policy
        var quiz = await _db.Quizzes.FindAsync([cmd.QuizId], ct)
                   ?? throw new NotFoundException("Quiz", cmd.QuizId);

        if (quiz.MaxAttempts > 0)
        {
            int attemptCount = await _db.Attempts.CountAsync(
                a => a.QuizId == cmd.QuizId && a.StudentId == cmd.StudentId, ct);

            if (attemptCount >= quiz.MaxAttempts)
                throw new BusinessRuleException($"Maximum attempts ({quiz.MaxAttempts}) reached for this quiz.");
        }

        var attempt = QuizAttempt.Start(cmd.QuizId, cmd.StudentId);
        _db.Attempts.Add(attempt);
        await _db.SaveChangesAsync(ct);

        return attempt.AttemptId;
    }
}

// ── SUBMIT QUIZ ATTEMPT ───────────────────────────────────────
public record SubmitAttemptCommand(
    Guid AttemptId,
    Guid StudentId,
    Dictionary<Guid, string> Answers  // questionId → student's answer text
) : IRequest<AttemptResultDto>;

public record AttemptResultDto(
    Guid    AttemptId,
    decimal Score,
    decimal MaxScore,
    bool    IsPassed,
    bool    HasPendingManualGrade,
    IEnumerable<AnswerResultDto> AnswerResults
);

public record AnswerResultDto(
    Guid    QuestionId,
    string  AnswerText,
    bool?   IsCorrect,
    decimal PointsEarned,
    string? Explanation
);

public class SubmitAttemptCommandHandler : IRequestHandler<SubmitAttemptCommand, AttemptResultDto>
{
    private readonly AssessmentDbContext _db;
    private readonly QuizGradingService  _grader;
    private readonly IPublishEndpoint    _bus;

    public SubmitAttemptCommandHandler(AssessmentDbContext db, QuizGradingService grader, IPublishEndpoint bus)
    { _db = db; _grader = grader; _bus = bus; }

    public async Task<AttemptResultDto> Handle(SubmitAttemptCommand cmd, CancellationToken ct)
    {
        // Load attempt + quiz questions
        var attempt = await _db.Attempts
                               .Include(a => a.Answers)
                               .FirstOrDefaultAsync(a => a.AttemptId == cmd.AttemptId, ct)
                     ?? throw new NotFoundException("Attempt", cmd.AttemptId);

        if (attempt.StudentId != cmd.StudentId)
            throw new ForbiddenException("This attempt does not belong to you.");

        if (attempt.Status != AttemptStatus.InProgress)
            throw new BusinessRuleException("This attempt has already been submitted.");

        var quiz      = await _db.Quizzes.Include(q => q.Questions)
                                         .FirstOrDefaultAsync(q => q.QuizId == attempt.QuizId, ct)
                        ?? throw new NotFoundException("Quiz", attempt.QuizId);

        // Build AttemptAnswer records from the submitted dictionary
        var answerEntities = cmd.Answers.Select(kv => new AttemptAnswer
        {
            AttemptId  = attempt.AttemptId,
            QuestionId = kv.Key,
            AnswerText = kv.Value
        }).ToList();

        _db.Answers.AddRange(answerEntities);

        // Auto-grade using the grading service (OOP separation of concerns)
        var (score, maxScore, hasPending) = _grader.Grade(quiz.Questions, answerEntities);
        attempt.SetResult(score, maxScore, quiz.PassingScore, hasPending);

        await _db.SaveChangesAsync(ct);

        // Publish graded event → Notification.API sends result email
        if (!hasPending)
            await _bus.Publish(new QuizGradedEvent(
                attempt.AttemptId, cmd.StudentId, quiz.QuizId,
                "student@email.com", "Student", quiz.Title,
                score, maxScore, attempt.IsPassed, DateTime.UtcNow), ct);

        // Build result DTO with per-answer breakdown
        var questionMap  = quiz.Questions.ToDictionary(q => q.QuestionId);
        var answerResult = answerEntities.Select(a =>
        {
            questionMap.TryGetValue(a.QuestionId, out var q);
            return new AnswerResultDto(a.QuestionId, a.AnswerText, a.IsCorrect, a.PointsEarned, q?.Explanation);
        });

        return new AttemptResultDto(attempt.AttemptId, score, maxScore, attempt.IsPassed, hasPending, answerResult);
    }
}

// ── INSTRUCTOR: GRADE SHORT ANSWER ───────────────────────────
public record GradeShortAnswerCommand(
    Guid    AttemptId,
    Guid    QuestionId,
    decimal PointsAwarded,
    Guid    InstructorId
) : IRequest;

public class GradeShortAnswerCommandHandler : IRequestHandler<GradeShortAnswerCommand>
{
    private readonly AssessmentDbContext _db;
    private readonly IPublishEndpoint    _bus;

    public GradeShortAnswerCommandHandler(AssessmentDbContext db, IPublishEndpoint bus)
    { _db = db; _bus = bus; }

    public async Task Handle(GradeShortAnswerCommand cmd, CancellationToken ct)
    {
        var answer = await _db.Answers
                              .FirstOrDefaultAsync(a => a.AttemptId == cmd.AttemptId && a.QuestionId == cmd.QuestionId, ct)
                     ?? throw new NotFoundException("Answer", $"{cmd.AttemptId}/{cmd.QuestionId}");

        answer.IsCorrect    = cmd.PointsAwarded > 0;
        answer.PointsEarned = cmd.PointsAwarded;

        var attempt = await _db.Attempts.Include(a => a.Answers)
                                        .FirstOrDefaultAsync(a => a.AttemptId == cmd.AttemptId, ct)!;

        // Check if all short-answer questions are now graded
        bool allGraded = !attempt!.Answers.Any(a => a.IsCorrect == null);
        if (allGraded)
        {
            attempt.FinalizeManualGrade(cmd.PointsAwarded);

            // Notify student that final grade is ready
            await _bus.Publish(new QuizGradedEvent(
                attempt.AttemptId, attempt.StudentId, attempt.QuizId,
                "student@email.com", "Student", "Quiz",
                attempt.Score, attempt.MaxScore, attempt.IsPassed, DateTime.UtcNow), ct);
        }

        await _db.SaveChangesAsync(ct);
    }
}

// ── SUBMIT ASSIGNMENT ─────────────────────────────────────────
public record SubmitAssignmentCommand(
    Guid   AssignmentId,
    Guid   StudentId,
    Stream FileStream,
    string FileName
) : IRequest<Guid>;

public class SubmitAssignmentCommandHandler : IRequestHandler<SubmitAssignmentCommand, Guid>
{
    private readonly AssessmentDbContext _db;
    private readonly IWebHostEnvironment _env;

    public SubmitAssignmentCommandHandler(AssessmentDbContext db, IWebHostEnvironment env)
    { _db = db; _env = env; }

    public async Task<Guid> Handle(SubmitAssignmentCommand cmd, CancellationToken ct)
    {
        var assignment = await _db.Assignments.FindAsync([cmd.AssignmentId], ct)
                         ?? throw new NotFoundException("Assignment", cmd.AssignmentId);

        bool isLate = DateTime.UtcNow > assignment.DueDate;

        // File I/O: save submitted file to wwwroot/submissions/
        var dir  = Path.Combine(_env.WebRootPath, "submissions", cmd.AssignmentId.ToString());
        Directory.CreateDirectory(dir);

        var ext      = Path.GetExtension(cmd.FileName).ToLowerInvariant();
        var filePath = Path.Combine(dir, $"{cmd.StudentId}{ext}");

        await using var output = File.Create(filePath);
        await cmd.FileStream.CopyToAsync(output, ct);

        var relativePath = Path.Combine("submissions", cmd.AssignmentId.ToString(), $"{cmd.StudentId}{ext}")
                               .Replace("\\", "/");

        var submission = Submission.Create(cmd.AssignmentId, cmd.StudentId, relativePath, cmd.FileName, isLate);
        _db.Submissions.Add(submission);
        await _db.SaveChangesAsync(ct);

        return submission.SubmissionId;
    }
}

// ── INSTRUCTOR: GRADE ASSIGNMENT ─────────────────────────────
public record GradeAssignmentCommand(
    Guid    SubmissionId,
    Guid    InstructorId,
    decimal Score,
    string  Feedback
) : IRequest;

public class GradeAssignmentCommandHandler : IRequestHandler<GradeAssignmentCommand>
{
    private readonly AssessmentDbContext _db;
    private readonly IPublishEndpoint    _bus;

    public GradeAssignmentCommandHandler(AssessmentDbContext db, IPublishEndpoint bus)
    { _db = db; _bus = bus; }

    public async Task Handle(GradeAssignmentCommand cmd, CancellationToken ct)
    {
        var submission = await _db.Submissions
                                  .Include(s => s.Assignment)
                                  .FirstOrDefaultAsync(s => s.SubmissionId == cmd.SubmissionId, ct)
                         ?? throw new NotFoundException("Submission", cmd.SubmissionId);

        submission.Grade(cmd.Score, cmd.Feedback, cmd.InstructorId);
        await _db.SaveChangesAsync(ct);

        // Publish event → Notification.API sends grading notification
        await _bus.Publish(new AssignmentGradedEvent(
            submission.SubmissionId, submission.StudentId, submission.AssignmentId,
            "student@email.com", "Student", submission.Assignment.Title,
            cmd.Score, submission.Assignment.MaxScore, cmd.Feedback, DateTime.UtcNow), ct);
    }
}
