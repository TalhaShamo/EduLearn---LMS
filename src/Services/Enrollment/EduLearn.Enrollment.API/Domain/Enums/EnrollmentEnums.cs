namespace EduLearn.Enrollment.API.Domain.Enums;

public enum EnrollmentStatus
{
    Active    = 1,  // Student is actively learning
    Completed = 2,  // 100% progress achieved
    Expired   = 3   // Access expired (future: subscription model)
}

public enum LessonProgressStatus
{
    NotStarted = 1,
    InProgress = 2,
    Completed  = 3
}
