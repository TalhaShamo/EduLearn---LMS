using FluentAssertions;
using EduLearn.Assessment.API.Application.Services;
using EduLearn.Assessment.API.Domain.Entities;
using EduLearn.Assessment.API.Domain.Enums;

namespace EduLearn.Assessment.Tests;

// Unit tests for QuizGradingService — pure logic, no DB or mocks needed
public class QuizGradingServiceTests
{
    private readonly QuizGradingService _grader = new();

    private static Question MakeQuestion(QuestionType type, string correct, int points = 2) =>
        Question.Create(Guid.NewGuid(), "Q?", type, correct, points, 1);

    [Fact]
    public void Grade_AllCorrectMCQ_ShouldReturnFullScore()
    {
        var q1 = MakeQuestion(QuestionType.MultipleChoice, "A");
        var q2 = MakeQuestion(QuestionType.MultipleChoice, "C");

        var answers = new List<AttemptAnswer>
        {
            new() { QuestionId = q1.QuestionId, AnswerText = "A" },
            new() { QuestionId = q2.QuestionId, AnswerText = "C" }
        };

        var (score, maxScore, pending) = _grader.Grade([q1, q2], answers);

        score.Should().Be(4);       // 2 + 2
        maxScore.Should().Be(4);
        pending.Should().BeFalse();
    }

    [Fact]
    public void Grade_AllWrongAnswers_ShouldReturnZeroScore()
    {
        var q1 = MakeQuestion(QuestionType.MultipleChoice, "A");

        var answers = new List<AttemptAnswer>
        {
            new() { QuestionId = q1.QuestionId, AnswerText = "B" }
        };

        var (score, _, _) = _grader.Grade([q1], answers);
        score.Should().Be(0);
    }

    [Fact]
    public void Grade_TrueFalse_CaseInsensitive_ShouldMatch()
    {
        var q = MakeQuestion(QuestionType.TrueFalse, "True");

        var answers = new List<AttemptAnswer>
        {
            new() { QuestionId = q.QuestionId, AnswerText = "true" } // lowercase
        };

        var (score, _, _) = _grader.Grade([q], answers);
        score.Should().Be(2); // Case-insensitive match
    }

    [Fact]
    public void Grade_ShortAnswer_ShouldBeMarkedPending()
    {
        var q = MakeQuestion(QuestionType.ShortAnswer, "any");

        var answers = new List<AttemptAnswer>
        {
            new() { QuestionId = q.QuestionId, AnswerText = "some answer" }
        };

        var (score, _, pending) = _grader.Grade([q], answers);

        score.Should().Be(0);      // No auto-grade for short answer
        pending.Should().BeTrue();  // Must be flagged for manual review
        answers[0].IsCorrect.Should().BeNull(); // Pending = null
    }

    [Fact]
    public void Grade_MixedQuestions_ShouldPartiallyAutoGrade()
    {
        var mcq   = MakeQuestion(QuestionType.MultipleChoice, "B", points: 2);
        var short_ = MakeQuestion(QuestionType.ShortAnswer, null, points: 5);

        var answers = new List<AttemptAnswer>
        {
            new() { QuestionId = mcq.QuestionId,    AnswerText = "B" },
            new() { QuestionId = short_.QuestionId, AnswerText = "explain..." }
        };

        var (score, maxScore, pending) = _grader.Grade([mcq, short_], answers);

        score.Should().Be(2);      // Only MCQ auto-graded
        maxScore.Should().Be(7);   // 2 + 5
        pending.Should().BeTrue();
    }
}
