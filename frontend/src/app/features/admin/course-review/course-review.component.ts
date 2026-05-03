import { Component, OnInit, OnDestroy } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Subject } from 'rxjs';
import { takeUntil, catchError, of } from 'rxjs';
import { environment } from '../../../../environments/environment';

interface PendingCourse {
  courseId: string; title: string; subtitle: string;
  instructorName: string; categoryName: string; level: string;
  price: number; submittedAt: string; thumbnailUrl?: string;
  totalLessons: number; totalSections: number;
}

@Component({
  selector: 'app-course-review',
  templateUrl: './course-review.component.html',
  styleUrls: ['./course-review.component.scss']
})
export class CourseReviewComponent implements OnInit, OnDestroy {
  isLoading = true;
  courses: PendingCourse[] = [];
  selectedCourse: PendingCourse | null = null;
  rejectReason = '';
  actionCourseId: string | null = null;
  activeTab: 'pending' | 'approved' | 'rejected' = 'pending';
  private destroy$ = new Subject<void>();

  constructor(private http: HttpClient) {}

  ngOnInit(): void { this.loadCourses(); }

  loadCourses(): void {
    this.isLoading = true;
    this.http.get<any>(`${environment.apiUrl}/courses/pending`)
      .pipe(catchError(() => of({ data: [] })), takeUntil(this.destroy$))
      .subscribe(res => {
        this.courses = Array.isArray(res?.data) ? res.data : [];
        this.isLoading = false;
      });
  }

  selectCourse(c: PendingCourse): void { this.selectedCourse = c; this.rejectReason = ''; }
  closePanel(): void { this.selectedCourse = null; }

  approveCourse(courseId: string): void {
    this.actionCourseId = courseId;
    this.http.patch(`${environment.apiUrl}/courses/${courseId}/approve`, {})
      .pipe(catchError(() => of(null)), takeUntil(this.destroy$))
      .subscribe(() => {
        this.courses = this.courses.filter(c => c.courseId !== courseId);
        if (this.selectedCourse?.courseId === courseId) this.selectedCourse = null;
        this.actionCourseId = null;
      });
  }

  rejectCourse(courseId: string): void {
    if (!this.rejectReason.trim()) { alert('Please provide a rejection reason'); return; }
    this.actionCourseId = courseId;
    this.http.patch(`${environment.apiUrl}/courses/${courseId}/request-changes`, { feedback: this.rejectReason })
      .pipe(catchError(() => of(null)), takeUntil(this.destroy$))
      .subscribe(() => {
        this.courses = this.courses.filter(c => c.courseId !== courseId);
        if (this.selectedCourse?.courseId === courseId) this.selectedCourse = null;
        this.rejectReason = '';
        this.actionCourseId = null;
      });
  }

  getTimeAgo(dateStr: string): string {
    const diff = Date.now() - new Date(dateStr).getTime();
    const h = Math.floor(diff / 3600000);
    if (h < 24) return `${h}h ago`;
    return `${Math.floor(h / 24)}d ago`;
  }



  ngOnDestroy(): void { this.destroy$.next(); this.destroy$.complete(); }
}
