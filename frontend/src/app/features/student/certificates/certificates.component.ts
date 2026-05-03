import { Component, OnInit, OnDestroy } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Subject, takeUntil } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { Certificate } from '../../../core/models/models';

@Component({
  selector: 'app-certificates',
  templateUrl: './certificates.component.html',
  styleUrls: ['./certificates.component.scss']
})
export class CertificatesComponent implements OnInit, OnDestroy {
  certificates: Certificate[] = [];
  isLoading = true;
  private destroy$ = new Subject<void>();

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    // Fetch completed enrollments instead of certificates endpoint
    const url = `${environment.apiUrl}/enrollments`;
    this.http.get<any>(url)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          const enrollments = res?.data || res;
          // Filter only completed enrollments and map to certificate format
          this.certificates = enrollments
            .filter((e: any) => e.isCompleted)
            .map((e: any) => ({
              certificateId: e.enrollmentId,
              courseId: e.courseId,
              courseName: e.courseTitle || 'Course',
              instructorName: 'Instructor',
              issuedAt: e.completedAt || new Date().toISOString(),
              pdfUrl: '',
              isRevoked: false
            }));
          this.isLoading = false;
        },
        error: () => {
          this.certificates = [];
          this.isLoading = false;
        }
      });
  }

  ngOnDestroy(): void { this.destroy$.next(); this.destroy$.complete(); }
}
