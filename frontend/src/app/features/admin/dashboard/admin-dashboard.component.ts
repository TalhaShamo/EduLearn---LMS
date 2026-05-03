import { Component, OnInit, OnDestroy } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Subject, forkJoin } from 'rxjs';
import { takeUntil, catchError, of } from 'rxjs';
import { environment } from '../../../../environments/environment';

interface PlatformStats {
  totalUsers: number; totalCourses: number; totalEnrollments: number;
  totalRevenue: number; activeCoursesCount: number; pendingCoursesCount: number;
  dailyActiveUsers: number; newUsersThisMonth: number;
}

@Component({
  selector: 'app-admin-dashboard',
  templateUrl: './admin-dashboard.component.html',
  styleUrls: ['./admin-dashboard.component.scss']
})
export class AdminDashboardComponent implements OnInit, OnDestroy {
  isLoading = true;
  stats: PlatformStats = { totalUsers: 0, totalCourses: 0, totalEnrollments: 0, totalRevenue: 0, activeCoursesCount: 0, pendingCoursesCount: 0, dailyActiveUsers: 0, newUsersThisMonth: 0 };
  pendingCourses: any[] = [];
  recentUsers: any[] = [];
  private destroy$ = new Subject<void>();

  statCards = [
    { key: 'totalUsers',        label: 'Total Users',        icon: 'people',          color: '#3949AB', bg: 'rgba(57,73,171,0.1)', prefix: '' },
    { key: 'totalEnrollments',  label: 'Enrollments',        icon: 'school',          color: '#059669', bg: 'rgba(5,150,105,0.1)', prefix: '' },
    { key: 'pendingCoursesCount', label: 'Pending Review',   icon: 'pending_actions', color: '#DC2626', bg: 'rgba(220,38,38,0.1)', prefix: '' },
  ];

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    forkJoin({
      stats:   this.http.get<any>(`${environment.apiUrl}/admin/stats`).pipe(catchError(() => of({ data: null }))),
      pending: this.http.get<any>(`${environment.apiUrl}/courses/pending`).pipe(catchError(() => of({ data: [] }))),
      users:   this.http.get<any>(`${environment.apiUrl}/users/recent`).pipe(catchError(() => of({ data: [] }))),
    }).pipe(takeUntil(this.destroy$)).subscribe(({ stats, pending, users }) => {
      this.stats         = stats?.data ?? { totalUsers: 0, totalCourses: 0, totalEnrollments: 0, totalRevenue: 0, activeCoursesCount: 0, pendingCoursesCount: 0, dailyActiveUsers: 0, newUsersThisMonth: 0 };
      this.pendingCourses = Array.isArray(pending?.data) ? pending.data.slice(0, 5) : [];
      this.recentUsers   = Array.isArray(users?.data) ? users.data.slice(0, 6) : [];
      this.isLoading = false;
    });
  }

  getStatValue(key: string): string | number {
    const v = (this.stats as any)[key] ?? 0;
    return key === 'totalRevenue' ? Number(v).toLocaleString('en-IN') : v.toLocaleString();
  }



  ngOnDestroy(): void { this.destroy$.next(); this.destroy$.complete(); }
}
