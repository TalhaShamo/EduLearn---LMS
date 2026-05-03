using CourseEntity = EduLearn.Course.API.Domain.Entities.Course;
using EduLearn.Course.API.Domain.Entities;

namespace EduLearn.Course.API.Application.Interfaces;

// Generic repository — same pattern as Identity.API (OOP reuse)
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id);
    Task<IEnumerable<T>> GetAllAsync();
    Task AddAsync(T entity);
    void Update(T entity);
    void Delete(T entity);
    Task SaveChangesAsync();
}

// Course-specific queries
public interface ICourseRepository : IRepository<CourseEntity>
{
    Task<CourseEntity?> GetWithSectionsAsync(Guid courseId);          // Eagerly load sections + lessons
    Task<CourseEntity?> GetBySlugAsync(string slug);                  // Get course by slug
    Task<IEnumerable<CourseEntity>> GetPublishedPagedAsync(int page, int pageSize, string? search, string? category);
    Task<IEnumerable<CourseEntity>> GetByInstructorAsync(Guid instructorId);
    Task<IEnumerable<CourseEntity>> GetPendingReviewAsync();          // Admin approval queue
    Task<int> CountPublishedAsync();
}

// Section repository
public interface ISectionRepository : IRepository<Section>
{
    Task<IEnumerable<Section>> GetByCourseAsync(Guid courseId);
}

// Lesson repository
public interface ILessonRepository : IRepository<Lesson>
{
    Task<IEnumerable<Lesson>> GetBySectionAsync(Guid sectionId);
    Task<Lesson?> GetWithSectionAsync(Guid lessonId);          // Needed for enrollment validation
}

// Video storage service — abstracts local file I/O
public interface IVideoStorageService
{
    // Save uploaded video file to wwwroot/videos/{courseId}/{lessonId}/
    Task<string> SaveVideoAsync(Guid courseId, Guid lessonId, Stream fileStream, string fileName);

    // Delete video file (when lesson is deleted)
    void DeleteVideo(string filePath);

    // Get full file path for streaming
    string GetVideoPath(string relativePath);
}
