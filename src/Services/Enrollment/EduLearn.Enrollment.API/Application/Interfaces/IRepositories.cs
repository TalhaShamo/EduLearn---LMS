using EduLearn.Enrollment.API.Domain.Entities;

namespace EduLearn.Enrollment.API.Application.Interfaces;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id);
    Task AddAsync(T entity);
    void Update(T entity);
    Task SaveChangesAsync();
}

public interface IEnrollmentRepository : IRepository<Enrollment>
{
    // Check if a student is already enrolled in a course
    Task<Enrollment?> GetByStudentAndCourseAsync(Guid studentId, Guid courseId);
    // Get all courses a student is enrolled in
    Task<IEnumerable<Enrollment>> GetByStudentAsync(Guid studentId);
    // Get enrollment with full lesson progress loaded
    Task<Enrollment?> GetWithProgressAsync(Guid enrollmentId);
}

public interface ILessonProgressRepository : IRepository<LessonProgress>
{
    // Find or null for a specific lesson in a specific enrollment
    Task<LessonProgress?> GetByEnrollmentAndLessonAsync(Guid enrollmentId, Guid lessonId);
    // Count how many lessons are completed in an enrollment
    Task<int> CountCompletedAsync(Guid enrollmentId);
}
