import { Component } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-forgot-password',
  templateUrl: './forgot-password.component.html',
  styleUrls: ['./forgot-password.component.scss']
})
export class ForgotPasswordComponent {
  email = '';
  loading = false;
  submitted = false;
  error = '';

  constructor(
    private http: HttpClient,
    private router: Router
  ) {}

  onSubmit() {
    if (!this.email) {
      this.error = 'Please enter your email address.';
      return;
    }

    this.loading = true;
    this.error = '';

    this.http.post(`${environment.apiUrl}/auth/forgot-password`, { email: this.email })
      .subscribe({
        next: () => {
          this.loading = false;
          this.submitted = true;
        },
        error: (err) => {
          this.loading = false;
          this.error = err.error?.message || 'Failed to send reset email. Please try again.';
        }
      });
  }

  goToLogin() {
    this.router.navigate(['/login']);
  }
}
