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
          console.log('Raw enrollment response:', res);
          this.enrollments = Array.isArray(res?.data) ? res.data : [];
          console.log('Enrollments before URL conversion:', this.enrollments);
          // Convert Docker internal URLs to external URLs
          this.enrollments = this.enrollments.map(e => ({
            ...e,
            courseThumbnailUrl: this.getThumbnailUrl(e.courseThumbnailUrl) || undefined
          }));
          console.log('Enrollments after URL conversion:', this.enrollments);
          this.isLoading = false; 
        },
        error: () => { 
          this.enrollments = []; 
          this.isLoading = false; 
        }
      });
  }
  
  getThumbnailUrl(url: string | null | undefined): string | null {
    if (!url) {
      console.log('No thumbnail URL provided');
      return null;
    }
    // If it's already a full URL, return as is
    if (url.startsWith('http')) {
      console.log('Full URL detected:', url);
      return url;
    }
    // Otherwise, prepend the course API base URL
    const fullUrl = `http://localhost:5002${url}`;
    console.log('Converted relative URL to:', fullUrl);
    return fullUrl;
  }
  
  getFullThumbnailUrl(url: string | null | undefined): string {
    if (!url) return '';
    if (url.startsWith('http')) return url;
    return `http://localhost:5002${url}`;
  }

  ngOnDestroy(): void { this.destroy$.next(); this.destroy$.complete(); }
}
