import { Component, OnInit, OnDestroy } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Subject, takeUntil, forkJoin } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { Course } from '../../../core/models/models';
import { AuthService } from '../../../core/services/auth.service';

interface InstructorStats {
  totalStudents: number;
  totalCourses: number;
  draftCourses: number;
}

@Component({
  selector: 'app-instructor-dashboard',
  templateUrl: './instructor-dashboard.component.html',
  styleUrls: ['./instructor-dashboard.component.scss']
})
export class InstructorDashboardComponent implements OnInit, OnDestroy {
  stats: InstructorStats = { totalStudents: 0, totalCourses: 0, draftCourses: 0 };
  recentCourses: Course[] = [];
  isLoading = true;
  private destroy$ = new Subject<void>();

  get greeting(): string {
    const h = new Date().getHours();
    if (h < 12) return 'Good morning';
    if (h < 17) return 'Good afternoon';
    return 'Good evening';
  }

  get instructorName(): string {
    return this.authService.currentUser?.fullName?.split(' ')[0] ?? 'Instructor';
  }

  statCards = [
    { key: 'totalStudents', label: 'Total Students', icon: 'people', color: '#3949AB', bg: 'rgba(57,73,171,0.1)' },
    { key: 'totalCourses',  label: 'Published Courses', icon: 'library_books', color: '#059669', bg: 'rgba(5,150,105,0.1)' },
    { key: 'draftCourses',  label: 'Draft Courses', icon: 'edit_note', color: '#D97706', bg: 'rgba(217,119,6,0.1)' },
  ];

  constructor(
    private http: HttpClient,
    public authService: AuthService
  ) {}

  ngOnInit(): void {
    forkJoin({
      stats: this.http.get<any>(`${environment.apiUrl}/instructor/stats`),
      courses: this.http.get<Course[]>(`${environment.apiUrl}/courses/my?pageSize=5`)
    }).pipe(takeUntil(this.destroy$))
      .subscribe({
        next: ({ stats, courses }) => {
          this.stats = stats?.data || { totalStudents: 0, totalCourses: 0, draftCourses: 0 };
          this.recentCourses = Array.isArray(courses) ? courses : (courses as any)?.data ?? [];
          this.isLoading = false;
        },
        error: () => {
          this.stats = { totalStudents: 0, totalCourses: 0, draftCourses: 0 };
          this.recentCourses = [];
          this.isLoading = false;
        }
      });
  }

  getStatValue(key: string): string | number {
    const val = (this.stats as any)[key] ?? 0;
    return Number(val).toLocaleString();
  }



  getStatusClass(status: string): string {
    const map: Record<string, string> = { Published: 'published', Draft: 'draft', PendingReview: 'pending', Archived: 'archived' };
    return map[status] ?? 'draft';
  }

  ngOnDestroy(): void { this.destroy$.next(); this.destroy$.complete(); }
}
