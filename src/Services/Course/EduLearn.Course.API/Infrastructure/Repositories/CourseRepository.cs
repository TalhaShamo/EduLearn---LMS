using Microsoft.EntityFrameworkCore;
using EduLearn.Course.API.Application.Interfaces;
using CourseEntity = EduLearn.Course.API.Domain.Entities.Course;
using EduLearn.Course.API.Domain.Entities;
using EduLearn.Course.API.Domain.Enums;
using EduLearn.Course.API.Infrastructure.Data;

namespace EduLearn.Course.API.Infrastructure.Repositories;

// Generic base (same OOP pattern as Identity.API)
public abstract class BaseRepository<T> : IRepository<T> where T : class
{
    protected readonly CourseDbContext _db;
    protected BaseRepository(CourseDbContext db) => _db = db;
    public async Task<T?> GetByIdAsync(Guid id)     => await _db.Set<T>().FindAsync(id);
    public async Task<IEnumerable<T>> GetAllAsync() => await _db.Set<T>().ToListAsync();
    public async Task AddAsync(T entity)             => await _db.Set<T>().AddAsync(entity);
    public void Update(T entity)                     => _db.Set<T>().Update(entity);
    public void Delete(T entity)                     => _db.Set<T>().Remove(entity);
    public async Task SaveChangesAsync()             => await _db.SaveChangesAsync();
}

// ── COURSE REPOSITORY ─────────────────────────────────────────
public class CourseRepository : BaseRepository<CourseEntity>, ICourseRepository
{
    public CourseRepository(CourseDbContext db) : base(db) { }

    // Eagerly load sections and their lessons for full course detail
    public async Task<CourseEntity?> GetWithSectionsAsync(Guid courseId) =>
        await _db.Courses
                 .Include(c => c.Sections.OrderBy(s => s.SortOrder))
                 .ThenInclude(s => s.Lessons.OrderBy(l => l.SortOrder))
                 .FirstOrDefaultAsync(c => c.CourseId == courseId);

    // Catalog: published only, with optional search + category filter
    public async Task<IEnumerable<CourseEntity>> GetPublishedPagedAsync(int page, int pageSize, string? search, string? category)
    {
        // Build query dynamically (LINQ Collections)
        var query = _db.Courses.Where(c => c.Status == CourseStatus.Published);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.Title.Contains(search) || c.Description.Contains(search));

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(c => c.Category == category);

        return await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<CourseEntity>> GetByInstructorAsync(Guid instructorId) =>
        await _db.Courses
                 .Where(c => c.InstructorId == instructorId)
                 .OrderByDescending(c => c.CreatedAt)
                 .ToListAsync();

    public async Task<IEnumerable<CourseEntity>> GetPendingReviewAsync() =>
        await _db.Courses
                 .Where(c => c.Status == CourseStatus.PendingReview)
                 .OrderBy(c => c.CreatedAt)
                 .ToListAsync();

    public async Task<int> CountPublishedAsync() =>
        await _db.Courses.CountAsync(c => c.Status == CourseStatus.Published);
}

// ── SECTION REPOSITORY ────────────────────────────────────────
public class SectionRepository : BaseRepository<Section>, ISectionRepository
{
    public SectionRepository(CourseDbContext db) : base(db) { }

    public async Task<IEnumerable<Section>> GetByCourseAsync(Guid courseId) =>
        await _db.Sections
                 .Where(s => s.CourseId == courseId)
                 .OrderBy(s => s.SortOrder)
                 .Include(s => s.Lessons)
                 .ToListAsync();
}

// ── LESSON REPOSITORY ─────────────────────────────────────────
public class LessonRepository : BaseRepository<Lesson>, ILessonRepository
{
    public LessonRepository(CourseDbContext db) : base(db) { }

    public async Task<IEnumerable<Lesson>> GetBySectionAsync(Guid sectionId) =>
        await _db.Lessons
                 .Where(l => l.SectionId == sectionId)
                 .OrderBy(l => l.SortOrder)
                 .ToListAsync();

    public async Task<Lesson?> GetWithSectionAsync(Guid lessonId) =>
        await _db.Lessons
                 .Include(l => l.Section)
                 .FirstOrDefaultAsync(l => l.LessonId == lessonId);
}
