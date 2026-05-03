import { Component, OnInit, OnDestroy } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Subject, takeUntil } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { Enrollment } from '../../../core/models/models';

@Component({
  selector: 'app-my-learning',
  templateUrl: './my-learning.component.html',
  styleUrls: ['./my-learning.component.scss']
})
export class MyLearningComponent implements OnInit, OnDestroy {
  enrollments: Enrollment[] = [];
  isLoading = true;
  activeFilter: 'all' | 'in-progress' | 'completed' = 'all';
  private destroy$ = new Subject<void>();

  get filtered(): Enrollment[] {
    if (this.activeFilter === 'in-progress') return this.enrollments.filter(e => !e.isCompleted && e.completionPercentage > 0);
    if (this.activeFilter === 'completed')   return this.enrollments.filter(e => e.isCompleted);
    return this.enrollments;
  }

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.http.get<any>(`${environment.apiUrl}/enrollments`)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => { 
          this.enrollments = Array.isArray(res?.data) ? res.data : [];
          this.isLoading = false; 
        },
        error: () => { 
          this.enrollments = []; 
          this.isLoading = false; 
        }
      });
  }

  ngOnDestroy(): void { this.destroy$.next(); this.destroy$.complete(); }
}
