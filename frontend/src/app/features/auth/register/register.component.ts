import { Component, OnInit, OnDestroy } from '@angular/core';
import { FormBuilder, FormGroup, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { Subject, takeUntil } from 'rxjs';

function passwordMatchValidator(control: AbstractControl): ValidationErrors | null {
  const password = control.get('password');
  const confirm = control.get('confirmPassword');
  if (password && confirm && password.value !== confirm.value) {
    confirm.setErrors({ passwordMismatch: true });
    return { passwordMismatch: true };
  }
  return null;
}

@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.scss']
})
export class RegisterComponent implements OnInit, OnDestroy {
  registerForm!: FormGroup;
  isLoading = false;
  errorMessage = '';
  registrationSuccess = false;
  registeredEmail = '';
  hidePassword = true;
  hideConfirm = true;
  selectedRole = 'Student';
  private destroy$ = new Subject<void>();

  roles = [
    { value: 'Student', label: 'I want to learn', icon: 'school', desc: 'Browse and enroll in courses' },
    { value: 'Instructor', label: 'I want to teach', icon: 'cast_for_education', desc: 'Create and publish courses' },
  ];

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.registerForm = this.fb.group({
      fullName: ['', [Validators.required, Validators.minLength(2)]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(8),
        Validators.pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)/)
      ]],
      confirmPassword: ['', Validators.required],
      role: ['Student', Validators.required],
      terms: [false, Validators.requiredTrue],
    }, { validators: passwordMatchValidator });
  }

  get fullNameCtrl() { return this.registerForm.get('fullName')!; }
  get emailCtrl() { return this.registerForm.get('email')!; }
  get passwordCtrl() { return this.registerForm.get('password')!; }
  get confirmCtrl() { return this.registerForm.get('confirmPassword')!; }

  selectRole(role: string): void {
    this.selectedRole = role;
    this.registerForm.get('role')!.setValue(role);
  }

  onSubmit(): void {
    console.log('onSubmit called');
    console.log('Form valid:', this.registerForm.valid);
    console.log('Form value:', this.registerForm.value);
    console.log('Form errors:', this.registerForm.errors);
    
    if (this.registerForm.invalid) { 
      console.log('Form is invalid, marking all as touched');
      this.registerForm.markAllAsTouched(); 
      return; 
    }
    
    this.isLoading = true;
    this.errorMessage = '';

    const { fullName, email, password, role } = this.registerForm.value;
    
    console.log('Making API call with:', { fullName, email, password: '***', role });

    this.authService.register({ fullName, email, password, role })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          console.log('Registration success:', res);
          this.isLoading = false;
          this.registrationSuccess = true;
          this.registeredEmail = email;
        },
        error: (err) => {
          console.error('Registration error:', err);
          this.isLoading = false;
          this.errorMessage = err?.error?.message || 'Registration failed. Please try again.';
        }
      });
  }

  ngOnDestroy(): void { this.destroy$.next(); this.destroy$.complete(); }
}
