import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../../environments/environment';

@Component({
  selector: 'app-certificate-view',
  templateUrl: './certificate-view.component.html',
  styleUrls: ['./certificate-view.component.scss']
})
export class CertificateViewComponent implements OnInit {
  certificate: any = null;
  isLoading = true;
  currentDate = new Date();

  constructor(
    private route: ActivatedRoute,
    private http: HttpClient
  ) {}

  ngOnInit(): void {
    const certId = this.route.snapshot.paramMap.get('id');
    this.loadCertificate(certId!);
  }

  loadCertificate(certId: string): void {
    // Fetch enrollment data to get certificate details
    this.http.get<any>(`${environment.apiUrl}/enrollments`)
      .subscribe({
        next: (res) => {
          const enrollments = res?.data || res;
          const enrollment = enrollments.find((e: any) => e.enrollmentId === certId);
          
          if (enrollment) {
            this.certificate = {
              certificateId: enrollment.enrollmentId,
              courseName: enrollment.courseTitle || 'Course',
              instructorName: 'Instructor',
              studentName: 'Student',
              issuedAt: enrollment.completedAt || new Date().toISOString(),
              completionDate: new Date(enrollment.completedAt || new Date()).toLocaleDateString('en-US', { 
                year: 'numeric', 
                month: 'long', 
                day: 'numeric' 
              })
            };
          } else {
            // Fallback if enrollment not found
            this.certificate = {
              certificateId: certId,
              courseName: 'Course',
              instructorName: 'Instructor',
              studentName: 'Student',
              issuedAt: new Date().toISOString(),
              completionDate: new Date().toLocaleDateString('en-US', { 
                year: 'numeric', 
                month: 'long', 
                day: 'numeric' 
              })
            };
          }
          this.isLoading = false;
        },
        error: () => {
          // Fallback on error
          this.certificate = {
            certificateId: certId,
            courseName: 'Course',
            instructorName: 'Instructor',
            studentName: 'Student',
            issuedAt: new Date().toISOString(),
            completionDate: new Date().toLocaleDateString('en-US', { 
              year: 'numeric', 
              month: 'long', 
              day: 'numeric' 
            })
          };
          this.isLoading = false;
        }
      });
  }

  print(): void {
    window.print();
  }
}
