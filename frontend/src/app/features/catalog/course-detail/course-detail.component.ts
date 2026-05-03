import { Component, OnInit, OnDestroy } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { Course, Section, AuthResponse } from '../../../core/models/models';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-course-detail',
  templateUrl: './course-detail.component.html',
  styleUrls: ['./course-detail.component.scss']
})
export class CourseDetailComponent implements OnInit, OnDestroy {
  course: Course | null = null;
  isLoading = true;
  isEnrolling = false;
  isEnrolled = false;
  expandedSections = new Set<string>();
  activeTab: 'overview' | 'curriculum' | 'instructor' | 'reviews' = 'overview';
  private destroy$ = new Subject<void>();

  get isLoggedIn(): boolean { return !!this.authService.currentUser; }
  get isStudent(): boolean  { return this.authService.currentUser?.role === 'Student'; }

  get totalLessons(): number {
    return this.course?.sections?.reduce((acc, s) => acc + (s.lessons?.length ?? 0), 0) ?? 0;
  }

  get freeLessons(): Lesson[] {
    return this.course?.sections?.flatMap(s => s.lessons.filter(l => l.isFreePreview)) ?? [];
  }

  constructor(
    private http: HttpClient,
    private route: ActivatedRoute,
    private router: Router,
    public authService: AuthService
  ) {}

  ngOnInit(): void {
    this.route.params.pipe(takeUntil(this.destroy$)).subscribe(params => {
      this.loadCourse(params['slug']);
    });
  }

  loadCourse(slug: string): void {
    this.isLoading = true;
    this.http.get<any>(`${environment.apiUrl}/courses/${slug}`)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.course = res?.data || res;
          if (this.course?.sections?.length) this.expandedSections.add(this.course.sections[0].sectionId);
          if (this.course?.courseId) this.checkEnrollment(this.course.courseId);
          this.isLoading = false;
        },
        error: () => {
          this.isLoading = false;
        }
      });
  }

  checkEnrollment(courseId: string): void {
    if (!this.isLoggedIn) return;
    this.http.get<any>(`${environment.apiUrl}/enrollments/${courseId}/status`)
      .pipe(takeUntil(this.destroy$))
      .subscribe({ next: (res) => this.isEnrolled = res?.data?.isEnrolled || res?.isEnrolled || false, error: () => {} });
  }

  toggleSection(id: string): void {
    this.expandedSections.has(id) ? this.expandedSections.delete(id) : this.expandedSections.add(id);
  }

  expandAll(): void   { this.course?.sections?.forEach(s => this.expandedSections.add(s.sectionId)); }
  collapseAll(): void { this.expandedSections.clear(); }

  onEnrollOrBuy(): void {
    if (!this.isLoggedIn) { this.router.navigate(['/auth/login']); return; }
    if (this.course!.price === 0) {
      this.enrollFree();
    } else {
      this.router.navigate(['/checkout', this.course!.courseId]);
    }
  }

  enrollFree(): void {
    this.isEnrolling = true;
    this.http.post(`${environment.apiUrl}/enrollments`, { courseId: this.course!.courseId })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => { this.isEnrolled = true; this.isEnrolling = false; this.router.navigate(['/learn', this.course!.courseId]); },
        error: () => this.isEnrolling = false
      });
  }

  goToPlayer(): void { this.router.navigate(['/learn', this.course!.courseId]); }

  getInitials(name: string): string {
    if (!name) return 'IN';
    return name.split(' ').map(n => n[0]).join('').substring(0, 2).toUpperCase();
  }

  formatDuration(seconds: number): string {
    if (!seconds) return '0m';
    const m = Math.floor(seconds / 60);
    const h = Math.floor(m / 60);
    return h > 0 ? `${h}h ${m % 60}m` : `${m}m`;
  }

  getLessonIcon(type: string): string {
    const map: Record<string, string> = { Video: 'play_circle', Article: 'article', Quiz: 'quiz', Assignment: 'assignment' };
    return map[type] ?? 'fiber_manual_record';
  }

  ngOnDestroy(): void { this.destroy$.next(); this.destroy$.complete(); }
}

// local type shorthand
type Lesson = import('../../../core/models/models').Lesson;
