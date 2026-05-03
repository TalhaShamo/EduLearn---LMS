import { Component, OnInit, OnDestroy } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { Course, CourseStatus } from '../../../core/models/models';

@Component({
  selector: 'app-my-courses',
  templateUrl: './my-courses.component.html',
  styleUrls: ['./my-courses.component.scss']
})
export class MyCoursesComponent implements OnInit, OnDestroy {
  courses: Course[] = [];
  isLoading = true;
  activeFilter: CourseStatus | 'All' = 'All';
  deletingId: string | null = null;
  private destroy$ = new Subject<void>();

  readonly filters: Array<CourseStatus | 'All'> = ['All', 'Published', 'Draft', 'PendingReview', 'Archived'];

  get filtered(): Course[] {
    if (this.activeFilter === 'All') return this.courses;
    return this.courses.filter(c => c.status === this.activeFilter);
  }

  constructor(private http: HttpClient, private router: Router) {}

  ngOnInit(): void {
    this.http.get<Course[]>(`${environment.apiUrl}/courses/my`)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => { this.courses = Array.isArray(res) ? res : (res as any)?.data ?? []; this.isLoading = false; },
        error: () => { this.courses = []; this.isLoading = false; }
      });
  }

  editCourse(id: string): void { this.router.navigate(['/instructor/courses', id, 'edit']); }

  deleteCourse(id: string): void {
    if (!confirm('Are you sure you want to delete this course? This cannot be undone.')) return;
    this.deletingId = id;
    this.http.delete(`${environment.apiUrl}/courses/${id}`)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => { this.courses = this.courses.filter(c => c.courseId !== id); this.deletingId = null; },
        error: () => { this.deletingId = null; alert('Delete failed. Please try again.'); }
      });
  }

  getStatusClass(status: string): string {
    const map: Record<string, string> = { Published: 'published', Draft: 'draft', PendingReview: 'pending', Archived: 'archived', ChangesRequested: 'changes' };
    return map[status] ?? 'draft';
  }

  getStatusLabel(status: string): string {
    return status === 'PendingReview' ? 'Under Review' : status === 'ChangesRequested' ? 'Changes Needed' : status;
  }



  ngOnDestroy(): void { this.destroy$.next(); this.destroy$.complete(); }
}
