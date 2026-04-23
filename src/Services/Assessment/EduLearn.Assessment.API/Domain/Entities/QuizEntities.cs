using EduLearn.Assessment.API.Domain.Enums;

namespace EduLearn.Assessment.API.Domain.Entities;

// Quiz belongs to a lesson in Course.API (linked by LessonId)
public class Quiz
{
    public Guid   QuizId             { get; private set; } = Guid.NewGuid();
    public Guid   LessonId           { get; private set; }  // FK → Course.API lesson
    public Guid   CourseId           { get; private set; }  // Denormalised for easy filtering
    public string Title              { get; private set; } = string.Empty;
    public int    TimeLimitSeconds   { get; private set; }  // 0 = no limit
    public int    PassingScore       { get; private set; }  // Percentage e.g. 70
    public int    MaxAttempts        { get; private set; }  // 0 = unlimited
    public bool   RandomizeQuestions { get; private set; }
    public bool   ShowAnswersAfter   { get; private set; }  // Show correct answers post-submit?

    public ICollection<Question> Questions { get; private set; } = new List<Question>();

    private Quiz() { }

    public static Quiz Create(Guid lessonId, Guid courseId, string title,
        int timeLimitSeconds, int passingScore, int maxAttempts, bool randomize) =>
        new()
        {
            LessonId           = lessonId, CourseId = courseId,
            Title              = title.Trim(),
            TimeLimitSeconds   = timeLimitSeconds,
            PassingScore       = passingScore,
            MaxAttempts        = maxAttempts,
            RandomizeQuestions = randomize
        };

    public void Update(string title, int timeLimitSeconds, int passingScore,
        int maxAttempts, bool randomize, bool showAnswers)
    {
        Title              = title.Trim();
        TimeLimitSeconds   = timeLimitSeconds;
        PassingScore       = passingScore;
        MaxAttempts        = maxAttempts;
        RandomizeQuestions = randomize;
        ShowAnswersAfter   = showAnswers;
    }
}

// Question belongs to a quiz
public class Question
{
    public Guid         QuestionId   { get; private set; } = Guid.NewGuid();
    public Guid         QuizId       { get; private set; }
    public string       Text         { get; private set; } = string.Empty;
    public QuestionType Type         { get; private set; }
    public string?      CorrectAnswer{ get; private set; }  // Used for auto-grading
    public string?      Explanation  { get; private set; }  // Shown after submit
    public int          Points       { get; private set; } = 1;
    public int          SortOrder    { get; private set; }

    // Options stored as JSON string (MCQ: "A|B|C|D", TrueFalse: "True|False")
    public string? OptionsJson { get; private set; }

    public Quiz Quiz { get; private set; } = null!;

    private Question() { }

    public static Question Create(Guid quizId, string text, QuestionType type,
        string? correctAnswer, int points, int sortOrder, string? optionsJson = null) =>
        new()
        {
            QuizId        = quizId, Text = text.Trim(),
            Type          = type, CorrectAnswer = correctAnswer,
            Points        = points, SortOrder    = sortOrder,
            OptionsJson   = optionsJson
        };
}

// One quiz attempt per student per quiz
public class QuizAttempt
{
    public Guid          AttemptId    { get; private set; } = Guid.NewGuid();
    public Guid          QuizId       { get; private set; }
    public Guid          StudentId    { get; private set; }
    public AttemptStatus Status       { get; private set; } = AttemptStatus.InProgress;
    public decimal       Score        { get; private set; }   // Achieved score
    public decimal       MaxScore     { get; private set; }   // Total possible score
    public bool          IsPassed     { get; private set; }
    public bool          HasPendingManualGrade { get; private set; }
    public DateTime      StartedAt    { get; private set; } = DateTime.UtcNow;
    public DateTime?     SubmittedAt  { get; private set; }

    public ICollection<AttemptAnswer> Answers { get; private set; } = new List<AttemptAnswer>();

    private QuizAttempt() { }

    public static QuizAttempt Start(Guid quizId, Guid studentId) =>
        new() { QuizId = quizId, StudentId = studentId };

    // Called by the GradingService after auto-grading
    public void SetResult(decimal score, decimal maxScore, int passingPct, bool hasPending)
    {
        Score                  = score;
        MaxScore               = maxScore;
        HasPendingManualGrade  = hasPending;
        Status                 = hasPending ? AttemptStatus.Submitted : AttemptStatus.Graded;
        IsPassed               = maxScore > 0 && (score / maxScore * 100) >= passingPct;
        SubmittedAt            = DateTime.UtcNow;
    }

    // Called when instructor completes manual grading of short-answer questions
    public void FinalizeManualGrade(decimal additionalScore)
    {
        Score                 += additionalScore;
        HasPendingManualGrade  = false;
        Status                 = AttemptStatus.Graded;
    }
}

// Student's answer to a single question
public class AttemptAnswer
{
    public Guid   AnswerId    { get; set; } = Guid.NewGuid();
    public Guid   AttemptId   { get; set; }
    public Guid   QuestionId  { get; set; }
    public string AnswerText  { get; set; } = string.Empty;  // Student's answer
    public bool?  IsCorrect   { get; set; }  // Null for short-answer (pending manual grade)
    public decimal PointsEarned { get; set; }

    public QuizAttempt Attempt { get; set; } = null!;
}
