namespace EduLearn.Enrollment.API.Application.DTOs;

// POST /api/v1/enrollments (free course)
public record EnrollRequest(Guid CourseId, int TotalLessons);

// POST /api/v1/progress/update (video heartbeat)
public record UpdateProgressRequest(
    Guid LessonId,
    int  WatchedSeconds,
    int  TotalSeconds    // Full video duration — needed to compute 80% threshold
);

// POST /api/v1/progress/complete (article or quiz completion)
public record CompleteLessonRequest(Guid LessonId);

// Response DTOs
public record EnrollmentDto(
    Guid     EnrollmentId,
    Guid     StudentId,
    Guid     CourseId,
    DateTime EnrolledAt,
    string   Status,
    decimal  ProgressPct,
    int      TotalLessons,
    int      CompletedLessons,
    DateTime? CompletedAt
);

public record LessonProgressDto(
    Guid     LessonId,
    string   Status,
    int      WatchedSeconds,
    DateTime? CompletedAt
);

public record CourseProgressDto(
    Guid                         EnrollmentId,
    decimal                      ProgressPct,
    string                       Status,
    IEnumerable<LessonProgressDto> LessonProgresses
);
