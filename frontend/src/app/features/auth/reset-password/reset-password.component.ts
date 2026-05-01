import { Component, OnInit } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormGroup, FormBuilder, Validators } from '@angular/forms';
import { inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import { NotificationService } from '../../../core/services/notification.service';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterLink],
  templateUrl: './reset-password.component.html',
  styleUrl: './reset-password.component.scss'
})
export class ResetPasswordComponent implements OnInit {
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private apiService = inject(ApiService);
  private notificationService = inject(NotificationService);
  private fb = inject(FormBuilder);

  resetPasswordForm: FormGroup;
  loading = false;
  submitted = false;
  token = '';
  showPassword = false;
  showConfirmPassword = false;
  passwordStrength = 0;
  passwordStrengthText = '';
  passwordStrengthColor = '';
  tokenValid = true;
  tokenExpired = false;

  constructor() {
    this.resetPasswordForm = this.fb.group({
      password: ['', [Validators.required, Validators.minLength(8), this.passwordStrengthValidator.bind(this)]],
      confirmPassword: ['', [Validators.required]]
    }, { validators: this.passwordMatchValidator });

    this.resetPasswordForm.get('password')?.valueChanges.subscribe(value => {
      this.calculatePasswordStrength(value);
    });
  }

  ngOnInit(): void {
    this.token = this.route.snapshot.queryParamMap.get('token') || '';
    
    if (!this.token) {
      this.tokenValid = false;
      this.notificationService.error('Invalid Link', 'Password reset link is invalid or has expired');
    }
  }

  passwordStrengthValidator(control: any): { [key: string]: boolean } | null {
    const password = control.value;
    if (!password) return null;

    const hasUpperCase = /[A-Z]/.test(password);
    const hasLowerCase = /[a-z]/.test(password);
    const hasNumbers = /\d/.test(password);
    const hasSpecialChar = /[!@#$%^&*(),.?":{}|<>]/.test(password);
    const isLongEnough = password.length >= 8;

    if (!isLongEnough) {
      return { passwordTooShort: true };
    }

    const strength = [hasUpperCase, hasLowerCase, hasNumbers, hasSpecialChar].filter(Boolean).length;
    if (strength < 3) {
      return { passwordTooWeak: true };
    }

    return null;
  }

  passwordMatchValidator(group: FormGroup): { [key: string]: boolean } | null {
    const password = group.get('password')?.value;
    const confirmPassword = group.get('confirmPassword')?.value;
    return password === confirmPassword ? null : { passwordMismatch: true };
  }

  calculatePasswordStrength(password: string): void {
    if (!password) {
      this.passwordStrength = 0;
      this.passwordStrengthText = '';
      this.passwordStrengthColor = '';
      return;
    }

    let strength = 0;
    const checks = {
      length: password.length >= 12,
      uppercase: /[A-Z]/.test(password),
      lowercase: /[a-z]/.test(password),
      numbers: /\d/.test(password),
      special: /[!@#$%^&*(),.?":{}|<>]/.test(password)
    };

    strength = Object.values(checks).filter(Boolean).length;

    if (strength <= 1) {
      this.passwordStrength = 20;
      this.passwordStrengthText = 'Weak';
      this.passwordStrengthColor = '#dc3545';
    } else if (strength <= 2) {
      this.passwordStrength = 40;
      this.passwordStrengthText = 'Fair';
      this.passwordStrengthColor = '#ffc107';
    } else if (strength <= 3) {
      this.passwordStrength = 60;
      this.passwordStrengthText = 'Good';
      this.passwordStrengthColor = '#17a2b8';
    } else if (strength <= 4) {
      this.passwordStrength = 80;
      this.passwordStrengthText = 'Strong';
      this.passwordStrengthColor = '#28a745';
    } else {
      this.passwordStrength = 100;
      this.passwordStrengthText = 'Very Strong';
      this.passwordStrengthColor = '#00ff00';
    }
  }

  onSubmit(): void {
    if (this.resetPasswordForm.invalid || this.loading || !this.tokenValid) return;

    this.loading = true;

    this.apiService.post<any>('/auth/reset-password', {
      token: this.token,
      password: this.resetPasswordForm.value.password,
      confirmPassword: this.resetPasswordForm.value.confirmPassword
    }).subscribe({
      next: (response) => {
        this.submitted = true;
        this.notificationService.success('Password Reset', 'Your password has been successfully reset');
        this.loading = false;
        
        setTimeout(() => {
          this.router.navigate(['/auth/login']);
        }, 3000);
      },
      error: (error) => {
        if (error.status === 400 && error.error?.message?.includes('expired')) {
          this.tokenExpired = true;
          this.tokenValid = false;
        }
        this.notificationService.error('Reset Failed', error.error?.message || 'Unable to reset password. Please request a new reset link.');
        this.loading = false;
      }
    });
  }

  getErrorMessage(controlName: string): string {
    const control = this.resetPasswordForm.get(controlName);
    if (!control || !control.errors) return '';

    const errors: { [key: string]: string } = {
      required: 'This field is required',
      minlength: 'Minimum length not met',
      passwordTooShort: 'Password must be at least 8 characters',
      passwordTooWeak: 'Password must contain uppercase, lowercase, and numbers',
      passwordMismatch: 'Passwords do not match'
    };

    for (const errorKey in control.errors) {
      if (errors[errorKey]) {
        return errors[errorKey];
      }
    }

    return 'Invalid input';
  }

  isFieldInvalid(controlName: string): boolean {
    const control = this.resetPasswordForm.get(controlName);
    return control ? control.invalid && (control.dirty || control.touched) : false;
  }

  togglePassword(field: 'password' | 'confirmPassword'): void {
    if (field === 'password') {
      this.showPassword = !this.showPassword;
    } else {
      this.showConfirmPassword = !this.showConfirmPassword;
    }
  }
}
