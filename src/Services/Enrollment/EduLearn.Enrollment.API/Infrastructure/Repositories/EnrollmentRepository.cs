using Microsoft.EntityFrameworkCore;
using EduLearn.Enrollment.API.Application.Interfaces;
using EduLearn.Enrollment.API.Domain.Entities;
using EduLearn.Enrollment.API.Infrastructure.Data;
using EnrollmentEntity = EduLearn.Enrollment.API.Domain.Entities.Enrollment;

namespace EduLearn.Enrollment.API.Infrastructure.Repositories;

// Generic base repository (same OOP pattern across all services)
public abstract class BaseRepository<T> : IRepository<T> where T : class
{
    protected readonly EnrollmentDbContext _db;
    protected BaseRepository(EnrollmentDbContext db) => _db = db;
    public async Task<T?> GetByIdAsync(Guid id) => await _db.Set<T>().FindAsync(id);
    public async Task AddAsync(T entity)         => await _db.Set<T>().AddAsync(entity);
    public void Update(T entity)                 => _db.Set<T>().Update(entity);
    public async Task SaveChangesAsync()         => await _db.SaveChangesAsync();
}

// ── ENROLLMENT REPOSITORY ─────────────────────────────────────
public class EnrollmentRepository : BaseRepository<EnrollmentEntity>, IEnrollmentRepository
{
    public EnrollmentRepository(EnrollmentDbContext db) : base(db) { }

    public async Task<EnrollmentEntity?> GetByStudentAndCourseAsync(Guid studentId, Guid courseId) =>
        await _db.Enrollments
                 .FirstOrDefaultAsync(e => e.StudentId == studentId && e.CourseId == courseId);

    public async Task<IEnumerable<EnrollmentEntity>> GetByStudentAsync(Guid studentId) =>
        await _db.Enrollments
                 .Where(e => e.StudentId == studentId)
                 .OrderByDescending(e => e.EnrolledAt)
                 .ToListAsync();

    public async Task<EnrollmentEntity?> GetWithProgressAsync(Guid enrollmentId) =>
        await _db.Enrollments
                 .Include(e => e.LessonProgresses)
                 .FirstOrDefaultAsync(e => e.EnrollmentId == enrollmentId);

    public async Task<IEnumerable<EnrollmentEntity>> GetAllAsync() =>
        await _db.Enrollments.ToListAsync();

    public async Task<IEnumerable<EnrollmentEntity>> GetByCourseAsync(Guid courseId) =>
        await _db.Enrollments
                 .Where(e => e.CourseId == courseId)
                 .ToListAsync();
}

// ── LESSON PROGRESS REPOSITORY ────────────────────────────────
public class LessonProgressRepository : BaseRepository<LessonProgress>, ILessonProgressRepository
{
    public LessonProgressRepository(EnrollmentDbContext db) : base(db) { }

    public async Task<LessonProgress?> GetByEnrollmentAndLessonAsync(Guid enrollmentId, Guid lessonId) =>
        await _db.LessonProgresses
                 .FirstOrDefaultAsync(p => p.EnrollmentId == enrollmentId && p.LessonId == lessonId);

    public async Task<int> CountCompletedAsync(Guid enrollmentId) =>
        await _db.LessonProgresses
                 .CountAsync(p => p.EnrollmentId == enrollmentId &&
                                  p.Status == Domain.Enums.LessonProgressStatus.Completed);
}
