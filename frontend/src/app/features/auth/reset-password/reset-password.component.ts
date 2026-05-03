import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-reset-password',
  templateUrl: './reset-password.component.html',
  styleUrls: ['./reset-password.component.scss']
})
export class ResetPasswordComponent implements OnInit {
  token = '';
  newPassword = '';
  confirmPassword = '';
  loading = false;
  success = false;
  error = '';
  showPassword = false;
  showConfirmPassword = false;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private http: HttpClient
  ) {}

  ngOnInit() {
    this.token = this.route.snapshot.queryParams['token'] || '';
    if (!this.token) {
      this.error = 'Invalid reset link.';
    }
  }

  get passwordStrength(): string {
    if (!this.newPassword) return '';
    if (this.newPassword.length < 6) return 'weak';
    if (this.newPassword.length < 10) return 'medium';
    return 'strong';
  }

  get passwordsMatch(): boolean {
    return this.newPassword === this.confirmPassword && this.confirmPassword.length > 0;
  }

  onSubmit() {
    this.error = '';

    if (!this.newPassword || !this.confirmPassword) {
      this.error = 'Please fill in all fields.';
      return;
    }

    if (this.newPassword.length < 6) {
      this.error = 'Password must be at least 6 characters long.';
      return;
    }

    if (this.newPassword !== this.confirmPassword) {
      this.error = 'Passwords do not match.';
      return;
    }

    this.loading = true;

    this.http.post(`${environment.apiUrl}/auth/reset-password`, {
      token: this.token,
      newPassword: this.newPassword
    }).subscribe({
      next: () => {
        this.loading = false;
        this.success = true;
      },
      error: (err) => {
        this.loading = false;
        this.error = err.error?.message || 'Failed to reset password. The link may have expired.';
      }
    });
  }

  goToLogin() {
    this.router.navigate(['/login']);
  }

  togglePasswordVisibility() {
    this.showPassword = !this.showPassword;
  }

  toggleConfirmPasswordVisibility() {
    this.showConfirmPassword = !this.showConfirmPassword;
  }
}
