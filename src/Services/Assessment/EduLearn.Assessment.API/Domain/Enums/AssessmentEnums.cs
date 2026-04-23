namespace EduLearn.Assessment.API.Domain.Enums;

public enum QuestionType
{
    MultipleChoice = 1,   // One correct answer from options
    TrueFalse      = 2,   // Boolean answer
    FillInBlank    = 3,   // Short exact-match text
    ShortAnswer    = 4    // Open text — requires manual grading
}

public enum AttemptStatus
{
    InProgress = 1,   // Student is currently answering
    Submitted  = 2,   // Submitted, auto-grading done, may have pending manual grades
    Graded     = 3    // All questions graded (including manual ones)
}

public enum SubmissionStatus
{
    Submitted   = 1,
    UnderReview = 2,
    Graded      = 3
}
