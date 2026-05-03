import { Component, OnInit, OnDestroy, ViewChild, ElementRef } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { Course, Section, Lesson, LessonProgress } from '../../../core/models/models';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-course-player',
  templateUrl: './course-player.component.html',
  styleUrls: ['./course-player.component.scss']
})
export class CoursePlayerComponent implements OnInit, OnDestroy {
  @ViewChild('videoEl') videoEl!: ElementRef<HTMLVideoElement>;

  course: Course | null = null;
  activeLesson: Lesson | null = null;
  lessonProgress: Map<string, LessonProgress> = new Map();
  isLoading = true;
  sidebarOpen = true;
  expandedSections = new Set<string>();
  completionPercentage = 0;
  private courseId = '';
  private destroy$ = new Subject<void>();

  get totalLessons(): number {
    return this.course?.sections?.reduce((acc, s) => acc + s.lessons.length, 0) ?? 0;
  }

  get completedLessons(): number {
    return Array.from(this.lessonProgress.values()).filter(p => p.status === 'Completed').length;
  }

  constructor(
    private http: HttpClient,
    private route: ActivatedRoute,
    private router: Router,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.route.params.pipe(takeUntil(this.destroy$)).subscribe(params => {
      this.courseId = params['courseId'];
      this.loadCourse();
    });
  }

  loadCourse(): void {
    this.isLoading = true;
    this.http.get<any>(`${environment.apiUrl}/courses/${this.courseId}`)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          // Extract course from wrapped response
          const course = res?.data || res;
          this.setupCourse(course);
        },
        error: () => {
          this.isLoading = false;
          console.error('Failed to load course');
        }
      });
  }

  setupCourse(course: Course): void {
    this.course = course;
    // expand first section, activate first lesson
    if (course.sections && course.sections.length > 0) {
      this.expandedSections.add(course.sections[0].sectionId);
      if (course.sections[0].lessons && course.sections[0].lessons.length > 0) {
        this.setActiveLesson(course.sections[0].lessons[0]);
      }
    }
    this.loadProgress();
    this.isLoading = false;
  }

  loadProgress(): void {
    this.http.get<any>(`${environment.apiUrl}/progress/${this.courseId}`)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          const data = res?.data || res;
          if (data.lessonProgresses) {
            data.lessonProgresses.forEach((p: any) => {
              this.lessonProgress.set(p.lessonId, {
                progressId: '',
                lessonId: p.lessonId,
                status: p.status,
                watchedSeconds: p.watchedSeconds || 0
              });
            });
            this.updateCompletionPercentage();
          }
        },
        error: (err) => {
          console.error('Failed to load progress:', err);
        }
      });
  }

  setActiveLesson(lesson: Lesson): void {
    this.activeLesson = lesson;
  }

  toggleSection(id: string): void {
    this.expandedSections.has(id) ? this.expandedSections.delete(id) : this.expandedSections.add(id);
  }

  markComplete(lesson: Lesson): void {
    const body = { lessonId: lesson.lessonId };
    this.http.post(`${environment.apiUrl}/progress/complete?courseId=${this.courseId}`, body)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.lessonProgress.set(lesson.lessonId, { 
            progressId: '', 
            lessonId: lesson.lessonId, 
            status: 'Completed', 
            watchedSeconds: lesson.durationSeconds 
          });
          this.updateCompletionPercentage();
          this.goToNextLesson();
        },
        error: (err) => {
          console.error('Failed to mark lesson complete:', err);
          // Still update UI optimistically
          this.lessonProgress.set(lesson.lessonId, { 
            progressId: '', 
            lessonId: lesson.lessonId, 
            status: 'Completed', 
            watchedSeconds: lesson.durationSeconds 
          });
          this.updateCompletionPercentage();
          this.goToNextLesson();
        }
      });
  }

  goToNextLesson(): void {
    if (!this.course?.sections) return;
    let found = false;
    for (const section of this.course.sections) {
      for (const lesson of section.lessons) {
        if (found) { this.setActiveLesson(lesson); return; }
        if (lesson.lessonId === this.activeLesson?.lessonId) found = true;
      }
    }
  }

  updateCompletionPercentage(): void {
    const total = this.totalLessons;
    this.completionPercentage = total > 0 ? Math.round((this.completedLessons / total) * 100) : 0;
  }

  isCompleted(lessonId: string): boolean {
    return this.lessonProgress.get(lessonId)?.status === 'Completed';
  }

  getLessonIcon(lesson: Lesson): string {
    const map: Record<string, string> = { Video: 'play_circle', Article: 'article', Quiz: 'quiz', Assignment: 'assignment' };
    return map[lesson.lessonType] ?? 'fiber_manual_record';
  }

  getVideoUrl(lessonId: string): string {
    const token = this.authService.accessToken;
    return `${environment.apiUrl}/lessons/${lessonId}/stream?token=${token}`;
  }

  getMockCourse(): Course {
    return {
      courseId: this.courseId, title: 'Complete Angular 17 Masterclass', slug: 'angular-17',
      subtitle: '', description: '', instructorId: '1', instructorName: 'Sarah Johnson',
      categoryId: '1', categoryName: 'Web Development', level: 'Intermediate', language: 'English',
      price: 2999, status: 'Published', durationMinutes: 1200,
      enrollmentCount: 12400, averageRating: 4.8, reviewCount: 1240, tags: [],
      sections: [
        { sectionId: 's1', courseId: this.courseId, title: 'Getting Started', sortOrder: 1, lessons: [
          { lessonId: 'l1', sectionId: 's1', title: 'Course Introduction', lessonType: 'Video', durationSeconds: 420, isFreePreview: true, sortOrder: 1, isPublished: true },
          { lessonId: 'l2', sectionId: 's1', title: 'Setting up your Dev Environment', lessonType: 'Video', durationSeconds: 840, isFreePreview: false, sortOrder: 2, isPublished: true },
        ]},
        { sectionId: 's2', courseId: this.courseId, title: 'Components & Templates', sortOrder: 2, lessons: [
          { lessonId: 'l3', sectionId: 's2', title: 'Understanding Components', lessonType: 'Video', durationSeconds: 900, isFreePreview: false, sortOrder: 1, isPublished: true },
          { lessonId: 'l4', sectionId: 's2', title: 'Data Binding Fundamentals', lessonType: 'Article', durationSeconds: 600, isFreePreview: false, sortOrder: 2, isPublished: true },
          { lessonId: 'l5', sectionId: 's2', title: 'Section Quiz', lessonType: 'Quiz', durationSeconds: 300, isFreePreview: false, sortOrder: 3, isPublished: true },
        ]},
        { sectionId: 's3', courseId: this.courseId, title: 'State Management with NgRx', sortOrder: 3, lessons: [
          { lessonId: 'l6', sectionId: 's3', title: 'Introduction to NgRx', lessonType: 'Video', durationSeconds: 960, isFreePreview: false, sortOrder: 1, isPublished: true },
          { lessonId: 'l7', sectionId: 's3', title: 'Actions, Reducers & Selectors', lessonType: 'Video', durationSeconds: 1320, isFreePreview: false, sortOrder: 2, isPublished: true },
        ]},
      ],
      createdAt: '', updatedAt: ''
    };
  }

  ngOnDestroy(): void { this.destroy$.next(); this.destroy$.complete(); }
}
