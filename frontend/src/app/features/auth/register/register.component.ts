import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormGroup, FormBuilder, Validators } from '@angular/forms';
import { inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService, RegisterRequest } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import {
  dateOfBirthValidator,
  nhsNumberValidator,
  passwordValidator,
  passwordMatchValidator as sharedPasswordMatchValidator
} from '../../../shared/validators';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterLink],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss'
})
export class RegisterComponent {
  private router = inject(Router);
  private authService = inject(AuthService);
  private notificationService = inject(NotificationService);
  private fb = inject(FormBuilder);

  registerForm: FormGroup;
  loading = false;
  showPassword = false;
  showConfirmPassword = false;
  passwordStrength = 0;
  passwordStrengthText = '';
  passwordStrengthColor = '';

  constructor() {
    this.registerForm = this.fb.group({
      nhsNumber: ['', [Validators.required, nhsNumberValidator()]],
      email: ['', [Validators.required, Validators.email, Validators.maxLength(100)]],
      phoneNumber: ['', [Validators.required, Validators.minLength(10), Validators.maxLength(20)]],
      password: ['', [Validators.required, Validators.minLength(8), passwordValidator()]],
      confirmPassword: ['', [Validators.required]],
      dateOfBirth: ['', [Validators.required, dateOfBirthValidator()]],
      firstName: ['', [Validators.required, Validators.maxLength(100)]],
      lastName: ['', [Validators.required, Validators.maxLength(100)]],
      address: [''],
      city: [''],
      postcode: ['', [Validators.pattern('^[A-Z]{1,2}[0-9][A-Z0-9]? ?[0-9][A-Z]{2}$')]],
      mfaEnabled: [false],
      smsOptIn: [true],
      emergencyContactName: [''],
      emergencyContactPhone: ['']
    }, { validators: sharedPasswordMatchValidator('password', 'confirmPassword') });

    this.registerForm.get('password')?.valueChanges.subscribe(value => {
      this.calculatePasswordStrength(value);
    });
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
    if (this.registerForm.invalid || this.loading) return;

    this.loading = true;

    const registerRequest: RegisterRequest = {
      ...this.registerForm.value,
      dateOfBirth: this.formatDate(this.registerForm.value.dateOfBirth)
    };

    this.authService.register(registerRequest).subscribe({
      next: (response) => {
        if (response.requiresMfa) {
          this.notificationService.info('Registration Successful', 'Please complete MFA verification to activate your account');
          this.router.navigate(['/auth/mfa'], { 
            queryParams: { userId: response.mfaUserId } 
          });
        } else {
          this.notificationService.success('Registration Complete', 'Your account has been created successfully');
          this.router.navigate(['/patient/dashboard']);
        }
      },
      error: (error) => {
        this.notificationService.error('Registration Failed', error.error?.message || 'An error occurred during registration');
        this.loading = false;
      },
      complete: () => {
        this.loading = false;
      }
    });
  }

  private formatDate(date: string): string {
    const d = new Date(date);
    const year = d.getFullYear();
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  getErrorMessage(controlName: string): string {
    const control = this.registerForm.get(controlName);
    if (!control || !control.errors) return '';

    const errors: { [key: string]: string } = {
      required: 'This field is required',
      email: 'Please enter a valid email address',
      minlength: 'Minimum length not met',
      maxlength: 'Maximum length exceeded',
      pattern: 'Invalid format',
      // nhsNumberValidator() — checksum failure
      nhsNumber: 'Please enter a valid 10-digit NHS number',
      // passwordValidator() error keys
      minLength: 'Password must be at least 8 characters',
      noUppercase: 'Password must contain an uppercase letter',
      noLowercase: 'Password must contain a lowercase letter',
      noNumber: 'Password must contain a number',
      noSpecialChar: 'Password must contain a special character (!@#$%^&* etc.)',
      // passwordMatchValidator (set on confirmPassword control)
      passwordMismatch: 'Passwords do not match',
      // dateOfBirthValidator() error keys
      tooYoung: 'You must be at least 16 years old',
      tooOld: 'Please enter a valid date of birth',
      futureDate: 'Date of birth cannot be in the future',
      // legacy fallbacks
      invalidAge: 'Please enter a valid date of birth'
    };

    for (const errorKey in control.errors) {
      if (errors[errorKey]) {
        return errors[errorKey];
      }
    }

    return 'Invalid input';
  }

  isFieldInvalid(controlName: string): boolean {
    const control = this.registerForm.get(controlName);
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
