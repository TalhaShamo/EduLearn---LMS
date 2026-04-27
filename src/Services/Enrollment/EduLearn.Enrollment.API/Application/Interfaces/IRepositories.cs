using EduLearn.Enrollment.API.Domain.Entities;
using EnrollmentEntity = EduLearn.Enrollment.API.Domain.Entities.Enrollment;

namespace EduLearn.Enrollment.API.Application.Interfaces;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id);
    Task AddAsync(T entity);
    void Update(T entity);
    Task SaveChangesAsync();
}

public interface IEnrollmentRepository : IRepository<EnrollmentEntity>
{
    // Check if a student is already enrolled in a course
    Task<EnrollmentEntity?> GetByStudentAndCourseAsync(Guid studentId, Guid courseId);
    // Get all courses a student is enrolled in
    Task<IEnumerable<EnrollmentEntity>> GetByStudentAsync(Guid studentId);
    // Get enrollment with full lesson progress loaded
    Task<EnrollmentEntity?> GetWithProgressAsync(Guid enrollmentId);
}

public interface ILessonProgressRepository : IRepository<LessonProgress>
{
    // Find or null for a specific lesson in a specific enrollment
    Task<LessonProgress?> GetByEnrollmentAndLessonAsync(Guid enrollmentId, Guid lessonId);
    // Count how many lessons are completed in an enrollment
    Task<int> CountCompletedAsync(Guid enrollmentId);
}
