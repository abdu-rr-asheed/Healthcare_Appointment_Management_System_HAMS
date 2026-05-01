import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormGroup, FormBuilder, Validators } from '@angular/forms';
import { inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import { NotificationService } from '../../../core/services/notification.service';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterLink],
  templateUrl: './forgot-password.component.html',
  styleUrl: './forgot-password.component.scss'
})
export class ForgotPasswordComponent {
  private router = inject(Router);
  private apiService = inject(ApiService);
  private notificationService = inject(NotificationService);
  private fb = inject(FormBuilder);

  forgotPasswordForm: FormGroup;
  loading = false;
  submitted = false;
  useNhsNumber = true;

  constructor() {
    this.forgotPasswordForm = this.fb.group({
      identifier: ['', [Validators.required, this.identifierValidator.bind(this)]]
    });
  }

  identifierValidator(control: any): { [key: string]: boolean } | null {
    const value = control.value;
    if (!value) return null;

    if (this.useNhsNumber) {
      if (value.length < 10 || value.length > 50) {
        return { invalidNhsNumber: true };
      }
    } else {
      const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
      if (!emailPattern.test(value)) {
        return { invalidEmail: true };
      }
    }

    return null;
  }

  onSubmit(): void {
    if (this.forgotPasswordForm.invalid || this.loading) return;

    this.loading = true;
    const identifier = this.forgotPasswordForm.value.identifier;

    const endpoint = this.useNhsNumber 
      ? '/auth/forgot-password?nhsNumber=' + encodeURIComponent(identifier)
      : '/auth/forgot-password?email=' + encodeURIComponent(identifier);

    this.apiService.post<any>(endpoint, {}).subscribe({
      next: (response) => {
        this.submitted = true;
        this.notificationService.success('Reset Link Sent', 'Please check your email for password reset instructions');
        this.loading = false;
      },
      error: (error) => {
        this.notificationService.error('Request Failed', error.error?.message || 'Unable to send reset link. Please try again.');
        this.loading = false;
      }
    });
  }

  toggleInputMethod(): void {
    this.useNhsNumber = !this.useNhsNumber;
    this.forgotPasswordForm.patchValue({ identifier: '' });
    this.forgotPasswordForm.get('identifier')?.updateValueAndValidity();
  }

  getErrorMessage(): string {
    const control = this.forgotPasswordForm.get('identifier');
    if (!control || !control.errors) return '';

    if (control.errors['required']) {
      return 'This field is required';
    }
    if (control.errors['invalidNhsNumber']) {
      return 'Please enter a valid NHS number (10-50 characters)';
    }
    if (control.errors['invalidEmail']) {
      return 'Please enter a valid email address';
    }

    return 'Invalid input';
  }

  isFieldInvalid(): boolean {
    const control = this.forgotPasswordForm.get('identifier');
    return control ? control.invalid && (control.dirty || control.touched) : false;
  }
}
