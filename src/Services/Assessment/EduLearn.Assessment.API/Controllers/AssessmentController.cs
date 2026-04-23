using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EduLearn.Assessment.API.Application.Commands;
using EduLearn.Assessment.API.Infrastructure.Data;
using EduLearn.Assessment.API.Domain.Entities;
using EduLearn.Assessment.API.Domain.Enums;
using EduLearn.Shared.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EduLearn.Assessment.API.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize]
public class AssessmentController : ControllerBase
{
    private readonly IMediator           _mediator;
    private readonly AssessmentDbContext _db;

    public AssessmentController(IMediator mediator, AssessmentDbContext db)
    { _mediator = mediator; _db = db; }

    // POST /api/v1/quizzes — instructor creates a quiz
    [HttpPost("quizzes")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> CreateQuiz([FromBody] CreateQuizRequest req)
    {
        var quiz = Quiz.Create(req.LessonId, req.CourseId, req.Title,
            req.TimeLimitSeconds, req.PassingScore, req.MaxAttempts, req.RandomizeQuestions);

        foreach (var q in req.Questions)
        {
            var question = Question.Create(quiz.QuizId, q.Text,
                Enum.Parse<QuestionType>(q.Type, true),
                q.CorrectAnswer, q.Points, q.SortOrder, q.OptionsJson);
            quiz.Questions.Add(question);
        }

        _db.Quizzes.Add(quiz);
        await _db.SaveChangesAsync();
        return StatusCode(201, ApiResponse<object>.Created(new { quiz.QuizId }));
    }

    // GET /api/v1/quizzes/{quizId} — safe DTO (no correct answers exposed)
    [HttpGet("quizzes/{quizId:guid}")]
    public async Task<IActionResult> GetQuiz(Guid quizId)
    {
        var quiz = await _db.Quizzes
            .Include(q => q.Questions.OrderBy(q => q.SortOrder))
            .FirstOrDefaultAsync(q => q.QuizId == quizId);

        if (quiz is null) return NotFound();

        var dto = new
        {
            quiz.QuizId, quiz.Title, quiz.TimeLimitSeconds,
            quiz.PassingScore, quiz.MaxAttempts, quiz.RandomizeQuestions,
            Questions = quiz.Questions.Select(q => new
            {
                q.QuestionId, q.Text, Type = q.Type.ToString(),
                q.Points, q.SortOrder, q.OptionsJson
            })
        };
        return Ok(ApiResponse<object>.Ok(dto));
    }

    // POST /api/v1/quizzes/{quizId}/attempts — start attempt
    [HttpPost("quizzes/{quizId:guid}/attempts")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> StartAttempt(Guid quizId)
    {
        var studentId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var attemptId = await _mediator.Send(new StartAttemptCommand(quizId, studentId));
        return StatusCode(201, ApiResponse<object>.Created(new { attemptId }));
    }

    // POST /api/v1/quizzes/attempts/{attemptId}/submit
    [HttpPost("quizzes/attempts/{attemptId:guid}/submit")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> SubmitAttempt(Guid attemptId,
        [FromBody] Dictionary<Guid, string> answers)
    {
        var studentId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result    = await _mediator.Send(new SubmitAttemptCommand(attemptId, studentId, answers));
        return Ok(ApiResponse<AttemptResultDto>.Ok(result));
    }

    // GET /api/v1/instructor/grading/pending
    [HttpGet("instructor/grading/pending")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> GetPendingGrading()
    {
        var pending = await _db.Attempts
            .Where(a => a.HasPendingManualGrade)
            .Select(a => new { a.AttemptId, a.QuizId, a.StudentId, a.StartedAt })
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(pending));
    }

    // POST /api/v1/instructor/grading/answers/{answerId}
    [HttpPost("instructor/grading/answers/{answerId:guid}")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> GradeShortAnswer(Guid answerId,
        [FromBody] GradeAnswerRequest req)
    {
        var instructorId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        await _mediator.Send(new GradeShortAnswerCommand(
            req.AttemptId, req.QuestionId, req.PointsAwarded, instructorId));
        return Ok(ApiResponse<string>.Ok("", "Answer graded."));
    }

    // POST /api/v1/assignments/{assignmentId}/submit
    [HttpPost("assignments/{assignmentId:guid}/submit")]
    [Authorize(Roles = "Student")]
    [RequestSizeLimit(50L * 1024 * 1024)]
    public async Task<IActionResult> SubmitAssignment(Guid assignmentId, IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(ApiResponse<string>.Fail("No file provided."));

        var studentId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        await using var stream = file.OpenReadStream();
        var submissionId = await _mediator.Send(
            new SubmitAssignmentCommand(assignmentId, studentId, stream, file.FileName));
        return StatusCode(201, ApiResponse<object>.Created(new { submissionId }));
    }

    // POST /api/v1/instructor/grading/submissions/{submissionId}
    [HttpPost("instructor/grading/submissions/{submissionId:guid}")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> GradeSubmission(Guid submissionId,
        [FromBody] GradeSubmissionRequest req)
    {
        var instructorId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        await _mediator.Send(new GradeAssignmentCommand(submissionId, instructorId, req.Score, req.Feedback));
        return Ok(ApiResponse<string>.Ok("", "Assignment graded."));
    }
}

// Request models
public record CreateQuizRequest(Guid LessonId, Guid CourseId, string Title,
    int TimeLimitSeconds, int PassingScore, int MaxAttempts, bool RandomizeQuestions,
    IEnumerable<CreateQuestionRequest> Questions);

public record CreateQuestionRequest(string Text, string Type, string? CorrectAnswer,
    int Points, int SortOrder, string? OptionsJson);

public record GradeAnswerRequest(Guid AttemptId, Guid QuestionId, decimal PointsAwarded);
public record GradeSubmissionRequest(decimal Score, string Feedback);
