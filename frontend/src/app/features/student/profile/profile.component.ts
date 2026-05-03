import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../../core/services/auth.service';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-profile',
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.scss']
})
export class ProfileComponent implements OnInit {
  profileForm!: FormGroup;
  isSaving = false;
  saveSuccess = false;

  constructor(
    private fb: FormBuilder,
    private http: HttpClient,
    public authService: AuthService
  ) {}

  ngOnInit(): void {
    const u = this.authService.currentUser;
    this.profileForm = this.fb.group({
      fullName: [u?.fullName ?? '', [Validators.required, Validators.minLength(2)]],
      email:    [{ value: u?.email ?? '', disabled: true }],
    });
  }

  getInitials(): string {
    return (this.authService.currentUser?.fullName ?? 'U')
      .split(' ').map((n: string) => n[0]).join('').substring(0, 2).toUpperCase();
  }

  onSave(): void {
    if (this.profileForm.invalid) return;
    this.isSaving = true;
    const url = `${environment.apiUrl}/users/profile`;
    const body = { fullName: this.profileForm.get('fullName')!.value };

    this.http.put(url, body).subscribe({
      next: () => { this.isSaving = false; this.saveSuccess = true; setTimeout(() => this.saveSuccess = false, 3000); },
      error: () => { this.isSaving = false; this.saveSuccess = true; setTimeout(() => this.saveSuccess = false, 3000); }
    });
  }
}
