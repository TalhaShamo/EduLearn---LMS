import { Component, OnInit, OnDestroy } from '@angular/core';
import { FormBuilder, FormGroup, FormArray, Validators, AbstractControl } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { CourseLevel } from '../../../core/models/models';

@Component({
  selector: 'app-course-builder',
  templateUrl: './course-builder.component.html',
  styleUrls: ['./course-builder.component.scss']
})
export class CourseBuilderComponent implements OnInit, OnDestroy {
  courseForm!: FormGroup;
  currentStep = 1;
  totalSteps = 4;
  isLoading = false;
  isSaving = false;
  isEditMode = false;
  courseId: string | null = null;
  thumbnailPreview: string | null = null;
  private destroy$ = new Subject<void>();

  readonly levels: CourseLevel[] = ['Beginner', 'Intermediate', 'Advanced'];
  readonly categories = ['Web Development', 'Data Science', 'UI/UX Design', 'Business', 'Cybersecurity', 'Cloud & DevOps', 'Mobile Dev', 'AI & ML'];
  readonly languages = ['English', 'Hindi', 'Tamil', 'Telugu', 'Kannada', 'Marathi'];
  readonly lessonTypes = ['Video', 'Article', 'Quiz', 'Assignment'];

  readonly steps = [
    { num: 1, label: 'Basic Info', icon: 'info' },
    { num: 2, label: 'Curriculum', icon: 'list' },
    { num: 3, label: 'Pricing',    icon: 'payments' },
    { num: 4, label: 'Publish',    icon: 'rocket_launch' },
  ];

  get sectionsArray(): FormArray { return this.courseForm.get('sections') as FormArray; }
  getLessonsArray(si: number): FormArray { return this.sectionsArray.at(si).get('lessons') as FormArray; }

  get step1Valid(): boolean { return (this.courseForm.get('title')?.valid && this.courseForm.get('category')?.valid && this.courseForm.get('level')?.valid) ?? false; }
  get step2Valid(): boolean { return this.sectionsArray.length > 0; }
  get step3Valid(): boolean { return this.courseForm.get('price')?.valid ?? false; }

  constructor(
    private fb: FormBuilder,
    private http: HttpClient,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.buildForm();
    this.route.params.pipe(takeUntil(this.destroy$)).subscribe(params => {
      if (params['courseId'] && params['courseId'] !== 'new') {
        this.isEditMode = true;
        this.courseId = params['courseId'];
        this.loadCourse(params['courseId']);
      } else {
        this.addSection(); // start with one blank section
      }
    });
  }

  buildForm(): void {
    this.courseForm = this.fb.group({
      title:       ['', [Validators.required, Validators.minLength(3)]],
      subtitle:    ['', [Validators.required, Validators.minLength(10)]],
      description: ['', [Validators.required, Validators.minLength(10)]],
      category:    ['', Validators.required],
      level:       ['Beginner', Validators.required],
      language:    ['English', Validators.required],
      tags:        [''],
      price:       [0, [Validators.required, Validators.min(0)]],
      isFree:      [false],
      sections:    this.fb.array([]),
    });

    this.courseForm.get('isFree')?.valueChanges.subscribe((free: boolean) => {
      if (free) this.courseForm.get('price')?.setValue(0);
    });
  }

  loadCourse(id: string): void {
    this.isLoading = true;
    this.http.get<any>(`${environment.apiUrl}/courses/${id}`)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          // Extract course from wrapped response
          const c = res?.data || res;
          this.courseForm.patchValue({
            title: c.title, subtitle: c.subtitle, description: c.description,
            category: c.categoryName || c.category, level: c.level, language: c.language,
            tags: c.tags?.join(', ') ?? '', price: c.price, isFree: c.price === 0,
          });
          c.sections?.forEach((s: any) => {
            const sGroup = this.newSectionGroup(s.title);
            s.lessons?.forEach((l: any) => {
              const lessonGroup = this.newLessonGroup(l.title, l.type || l.lessonType, l.isFreePreview);
              lessonGroup.patchValue({ 
                lessonId: l.lessonId,
                videoUploaded: !!l.videoPath // Mark as uploaded if videoPath exists
              });
              (sGroup.get('lessons') as FormArray).push(lessonGroup);
            });
            this.sectionsArray.push(sGroup);
          });
          if (!this.sectionsArray.length) this.addSection();
          this.isLoading = false;
        },
        error: () => { this.addSection(); this.isLoading = false; }
      });
  }

  // ─── Section CRUD ─────────────────────────────────────────────────────────
  newSectionGroup(title = ''): FormGroup {
    return this.fb.group({ title: [title, Validators.required], lessons: this.fb.array([]) });
  }

  addSection(): void { this.sectionsArray.push(this.newSectionGroup()); }
  removeSection(i: number): void { this.sectionsArray.removeAt(i); }

  // ─── Lesson CRUD ──────────────────────────────────────────────────────────
  newLessonGroup(title = '', type = 'Video', preview = false): FormGroup {
    return this.fb.group({
      title:         [title, Validators.required],
      lessonType:    [type],
      isFreePreview: [preview],
      lessonId:      [null], // Store lesson ID after creation
      videoUploaded: [false], // Track if video is uploaded
    });
  }

  addLesson(si: number): void { this.getLessonsArray(si).push(this.newLessonGroup()); }
  removeLesson(si: number, li: number): void { this.getLessonsArray(si).removeAt(li); }

  // ─── Video Upload ─────────────────────────────────────────────────────────
  onVideoSelect(event: Event, sectionIndex: number, lessonIndex: number): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;

    // Validate file type
    if (!file.type.startsWith('video/')) {
      alert('Please select a valid video file');
      return;
    }

    const lesson = this.getLessonsArray(sectionIndex).at(lessonIndex);
    const lessonId = lesson.get('lessonId')?.value;

    if (!lessonId) {
      alert('Please save the course first before uploading videos');
      return;
    }

    // Upload video
    const formData = new FormData();
    formData.append('file', file);

    this.http.post(
      `${environment.apiUrl}/lessons/${lessonId}/upload-video?courseId=${this.courseId}`,
      formData
    ).pipe(takeUntil(this.destroy$)).subscribe({
      next: () => {
        lesson.patchValue({ videoUploaded: true });
        alert('Video uploaded successfully!');
      },
      error: () => alert('Video upload failed. Please try again.')
    });
  }

  // ─── Thumbnail ────────────────────────────────────────────────────────────
  onThumbnailChange(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;
    const reader = new FileReader();
    reader.onload = () => { this.thumbnailPreview = reader.result as string; };
    reader.readAsDataURL(file);
  }

  // ─── Navigation ───────────────────────────────────────────────────────────
  nextStep(): void { if (this.currentStep < this.totalSteps) this.currentStep++; }
  prevStep(): void { if (this.currentStep > 1) this.currentStep--; }
  goToStep(n: number): void { if (n <= this.currentStep) this.currentStep = n; }

  // ─── Submit ───────────────────────────────────────────────────────────────
  onSubmit(publish = false): void {
    this.isSaving = true;
    const v = this.courseForm.value;
    
    // Clean sections data - assign proper sortOrder based on array index
    const cleanSections = v.sections.map((section: any, sectionIndex: number) => ({
      title: section.title,
      sortOrder: sectionIndex + 1, // 1-based indexing
      lessons: section.lessons.map((lesson: any, lessonIndex: number) => ({
        title: lesson.title,
        lessonType: lesson.lessonType,
        isFreePreview: lesson.isFreePreview,
        sortOrder: lessonIndex + 1 // 1-based indexing
      }))
    }));
    
    const payload = {
      title: v.title, subtitle: v.subtitle, description: v.description,
      categoryName: v.category, level: v.level, language: v.language,
      tags: v.tags?.split(',').map((t: string) => t.trim()).filter(Boolean) ?? [],
      price: v.isFree ? 0 : Number(v.price),
      status: 'Draft',  // Always save as draft first
      sections: cleanSections,
    };

    const req = this.isEditMode
      ? this.http.put(`${environment.apiUrl}/courses/${this.courseId}`, payload)
      : this.http.post(`${environment.apiUrl}/courses`, payload);

    req.pipe(takeUntil(this.destroy$)).subscribe({
      next: (res: any) => {
        const courseData = res?.data || res;
        const courseId = this.courseId || courseData?.courseId;
        
        // Store course ID and lesson IDs for video uploads
        if (!this.isEditMode && courseId) {
          this.courseId = courseId;
          this.isEditMode = true;
          
          // Map lesson IDs from response to form
          courseData?.sections?.forEach((section: any, si: number) => {
            section.lessons?.forEach((lesson: any, li: number) => {
              const lessonControl = this.getLessonsArray(si).at(li);
              lessonControl?.patchValue({ lessonId: lesson.lessonId });
            });
          });
        }

        // If publishing, call submit-review endpoint
        if (publish) {
          if (courseId) {
            this.http.patch(`${environment.apiUrl}/courses/${courseId}/submit-review`, {})
              .pipe(takeUntil(this.destroy$))
              .subscribe({
                next: () => { this.isSaving = false; this.router.navigate(['/instructor/courses']); },
                error: () => { this.isSaving = false; alert('Submit for review failed. Please try again.'); }
              });
          } else {
            this.isSaving = false;
            this.router.navigate(['/instructor/courses']);
          }
        } else {
          this.isSaving = false;
          alert('Course saved! You can now upload videos for your lessons.');
        }
      },
      error: () => { this.isSaving = false; alert('Save failed. Please try again.'); }
    });
  }

  get lessonCount(): number {
    return this.sectionsArray.controls.reduce((acc, s) =>
      acc + (s.get('lessons') as FormArray).length, 0);
  }

  ngOnDestroy(): void { this.destroy$.next(); this.destroy$.complete(); }
}
