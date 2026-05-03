import { Component, OnInit, OnDestroy } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Subject, takeUntil } from 'rxjs';
import { environment } from '../../../../environments/environment';

interface EnrollmentPoint { month: string; enrollments: number; revenue: number; }
interface CoursePerf { courseId: string; title: string; enrollments: number; revenue: number; rating: number; completionRate: number; }

@Component({
  selector: 'app-analytics',
  templateUrl: './analytics.component.html',
  styleUrls: ['./analytics.component.scss']
})
export class AnalyticsComponent implements OnInit, OnDestroy {
  isLoading = true;
  totalRevenue = 0;
  totalStudents = 0;
  totalCompletions = 0;
  avgRating = 0;
  chartData: EnrollmentPoint[] = [];
  coursePerf: CoursePerf[] = [];
  chartMax = 1;
  private destroy$ = new Subject<void>();

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.http.get<any>(`${environment.apiUrl}/instructor/analytics`)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data) => this.applyData(data),
        error: () => this.applyData(this.getMockData())
      });
  }

  applyData(data: any): void {
    this.totalRevenue    = data.totalRevenue;
    this.totalStudents   = data.totalStudents;
    this.totalCompletions = data.totalCompletions;
    this.avgRating       = data.avgRating;
    this.chartData       = data.enrollmentTrend;
    this.coursePerf      = data.coursePerformance;
    this.chartMax        = Math.max(...this.chartData.map((p: EnrollmentPoint) => p.enrollments), 1);
    this.isLoading = false;
  }

  barHeight(value: number): number {
    return Math.round((value / this.chartMax) * 100);
  }

  getMockData() {
    return {
      totalRevenue: 842300, totalStudents: 28450, totalCompletions: 12860, avgRating: 4.82,
      enrollmentTrend: [
        { month: 'Sep', enrollments: 1240, revenue: 72300 },
        { month: 'Oct', enrollments: 1850, revenue: 108200 },
        { month: 'Nov', enrollments: 2100, revenue: 128600 },
        { month: 'Dec', enrollments: 1700, revenue: 99800 },
        { month: 'Jan', enrollments: 2400, revenue: 146300 },
        { month: 'Feb', enrollments: 2960, revenue: 181400 },
        { month: 'Mar', enrollments: 3200, revenue: 198400 },
        { month: 'Apr', enrollments: 2800, revenue: 169100 },
      ],
      coursePerformance: [
        { courseId: '1', title: 'Complete Angular 17 Masterclass',   enrollments: 12400, revenue: 371280, rating: 4.8, completionRate: 68 },
        { courseId: '2', title: 'Advanced TypeScript Patterns',      enrollments: 5800,  revenue: 202930, rating: 4.9, completionRate: 74 },
        { courseId: '3', title: 'Node.js REST API Development',      enrollments: 6100,  revenue: 140230, rating: 4.7, completionRate: 61 },
        { courseId: '4', title: 'Docker & Kubernetes for Developers', enrollments: 4150,  revenue: 161850, rating: 4.8, completionRate: 59 },
      ]
    };
  }

  ngOnDestroy(): void { this.destroy$.next(); this.destroy$.complete(); }
}
