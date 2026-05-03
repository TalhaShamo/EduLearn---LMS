import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-verify-email',
  templateUrl: './verify-email.component.html',
  styleUrls: ['./verify-email.component.scss']
})
export class VerifyEmailComponent implements OnInit {
  loading = true;
  success = false;
  error = '';
  token = '';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private http: HttpClient
  ) {}

  ngOnInit() {
    this.token = this.route.snapshot.queryParams['token'] || '';
    if (this.token) {
      this.verifyEmail();
    } else {
      this.loading = false;
      this.error = 'Invalid verification link.';
    }
  }

  verifyEmail() {
    this.http.post(`${environment.apiUrl}/auth/verify-email`, { token: this.token })
      .subscribe({
        next: () => {
          this.loading = false;
          this.success = true;
        },
        error: (err) => {
          this.loading = false;
          this.error = err.error?.message || 'Verification failed. The link may have expired.';
        }
      });
  }

  resendVerification() {
    // This would require the user to be logged in, so we'll just redirect to login
    this.router.navigate(['/login']);
  }

  goToLogin() {
    this.router.navigate(['/login']);
  }
}
