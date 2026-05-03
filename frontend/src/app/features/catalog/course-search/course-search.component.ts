import { Component, OnInit, OnDestroy } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { FormBuilder, FormGroup } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, debounceTime, distinctUntilChanged, takeUntil } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { Course, CourseLevel, PagedResult } from '../../../core/models/models';

interface FilterState {
  query: string;
  category: string;
  level: string;
  minPrice: number | null;
  maxPrice: number | null;
  sortBy: string;
  page: number;
}

@Component({
  selector: 'app-course-search',
  templateUrl: './course-search.component.html',
  styleUrls: ['./course-search.component.scss']
})
export class CourseSearchComponent implements OnInit, OnDestroy {
  courses: Course[] = [];
  totalCount = 0;
  totalPages = 0;
  currentPage = 1;
  pageSize = 12;
  isLoading = true;

  filterForm!: FormGroup;

  readonly categories = [
    'All', 'Web Development', 'Data Science', 'UI/UX Design',
    'Business', 'Cybersecurity', 'Cloud & DevOps', 'Mobile Dev', 'AI & ML'
  ];

  readonly levels: CourseLevel[] = ['Beginner', 'Intermediate', 'Advanced'];

  readonly sortOptions = [
    { value: 'popular',   label: 'Most Popular' },
    { value: 'newest',    label: 'Newest First' },
    { value: 'rating',    label: 'Highest Rated' },
    { value: 'price-asc', label: 'Price: Low to High' },
    { value: 'price-desc',label: 'Price: High to Low' },
  ];

  selectedLevels = new Set<string>();
  activeCategory = 'All';
  showFilters = false;

  get pageNumbers(): number[] {
    return Array.from({ length: this.totalPages }, (_, i) => i + 1);
  }

  private destroy$ = new Subject<void>();

  constructor(
    private http: HttpClient,
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.filterForm = this.fb.group({
      query:    [''],
      sortBy:   ['popular'],
    });

    // Pre-fill from query params
    this.route.queryParams.pipe(takeUntil(this.destroy$)).subscribe(params => {
      if (params['q'])        this.filterForm.get('query')!.setValue(params['q'], { emitEvent: false });
      if (params['category']) this.activeCategory = params['category'];
      if (params['level'])    this.selectedLevels = new Set(params['level'].split(','));
      if (params['page'])     this.currentPage = +params['page'];
      this.loadCourses();
    });

    // Debounce search input
    this.filterForm.get('query')!.valueChanges
      .pipe(debounceTime(400), distinctUntilChanged(), takeUntil(this.destroy$))
      .subscribe(() => { this.currentPage = 1; this.loadCourses(); });

    // Reload on sort change
    this.filterForm.get('sortBy')!.valueChanges.pipe(takeUntil(this.destroy$))
      .subscribe(() => { this.currentPage = 1; this.loadCourses(); });
  }

  loadCourses(): void {
    this.isLoading = true;
    const { query, sortBy } = this.filterForm.value;

    let params = new HttpParams()
      .set('pageNumber', this.currentPage)
      .set('pageSize', this.pageSize)
      .set('sortBy', sortBy ?? 'popular');

    if (query)                          params = params.set('search', query);
    if (this.activeCategory !== 'All')  params = params.set('category', this.activeCategory);
    if (this.selectedLevels.size > 0)   params = params.set('levels', Array.from(this.selectedLevels).join(','));

    this.http.get<any>(`${environment.apiUrl}/courses`, { params })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.courses    = Array.isArray(res?.data) ? res.data : [];
          this.totalCount = res?.totalCount ?? this.courses.length;
          this.totalPages = res?.totalPages ?? Math.ceil(this.courses.length / this.pageSize);
          this.isLoading  = false;
        },
        error: () => {
          this.courses    = [];
          this.totalCount = 0;
          this.totalPages = 1;
          this.isLoading  = false;
        }
      });
  }

  setCategory(cat: string): void {
    this.activeCategory = cat;
    this.currentPage = 1;
    this.loadCourses();
  }

  toggleLevel(level: string): void {
    this.selectedLevels.has(level)
      ? this.selectedLevels.delete(level)
      : this.selectedLevels.add(level);
    this.currentPage = 1;
    this.loadCourses();
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages) return;
    this.currentPage = page;
    this.loadCourses();
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  clearFilters(): void {
    this.filterForm.reset({ query: '', sortBy: 'popular' });
    this.activeCategory = 'All';
    this.selectedLevels.clear();
    this.currentPage = 1;
    this.loadCourses();
  }

  get hasActiveFilters(): boolean {
    return this.activeCategory !== 'All' || this.selectedLevels.size > 0;
  }

  ngOnDestroy(): void { this.destroy$.next(); this.destroy$.complete(); }
}
