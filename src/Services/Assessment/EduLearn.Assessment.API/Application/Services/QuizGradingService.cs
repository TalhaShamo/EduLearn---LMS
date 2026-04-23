using EduLearn.Assessment.API.Domain.Entities;
using EduLearn.Assessment.API.Domain.Enums;

namespace EduLearn.Assessment.API.Application.Services;

// Quiz auto-grading service — OOP single-responsibility
// Grades objective question types; flags short-answer for manual review
public class QuizGradingService
{
    // Grade all answers for an attempt; returns (score, maxScore, hasPending)
    public (decimal score, decimal maxScore, bool hasPendingManualGrade)
        Grade(IEnumerable<Question> questions, IEnumerable<AttemptAnswer> answers)
    {
        decimal score    = 0;
        decimal maxScore = 0;
        bool    pending  = false;

        // Build a dictionary for O(1) lookup: questionId → question (Collections)
        var questionMap = questions.ToDictionary(q => q.QuestionId);

        foreach (var answer in answers)
        {
            if (!questionMap.TryGetValue(answer.QuestionId, out var question))
                continue;

            maxScore += question.Points;

            switch (question.Type)
            {
                // Auto-grade: compare student answer to correct answer (case-insensitive)
                case QuestionType.MultipleChoice:
                case QuestionType.TrueFalse:
                case QuestionType.FillInBlank:
                    bool correct = string.Equals(
                        answer.AnswerText.Trim(),
                        question.CorrectAnswer?.Trim(),
                        StringComparison.OrdinalIgnoreCase);

                    answer.IsCorrect     = correct;
                    answer.PointsEarned  = correct ? question.Points : 0;
                    score               += answer.PointsEarned;
                    break;

                // Short answer: flag for instructor review — no auto-grade
                case QuestionType.ShortAnswer:
                    answer.IsCorrect = null;  // Null = pending
                    pending          = true;
                    break;
            }
        }

        return (score, maxScore, pending);
    }
}
