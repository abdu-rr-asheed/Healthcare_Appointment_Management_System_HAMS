import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormGroup, FormBuilder, Validators } from '@angular/forms';
import { inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService, RegisterRequest } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';

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
      nhsNumber: ['', [Validators.required, Validators.minLength(10), Validators.maxLength(50)]],
      email: ['', [Validators.required, Validators.email, Validators.maxLength(100)]],
      phoneNumber: ['', [Validators.required, Validators.minLength(10), Validators.maxLength(20)]],
      password: ['', [Validators.required, Validators.minLength(8), this.passwordStrengthValidator.bind(this)]],
      confirmPassword: ['', [Validators.required]],
      dateOfBirth: ['', [Validators.required, this.ageValidator.bind(this)]],
      firstName: ['', [Validators.required, Validators.maxLength(100)]],
      lastName: ['', [Validators.required, Validators.maxLength(100)]],
      address: [''],
      city: [''],
      postcode: ['', [Validators.pattern('^[A-Z]{1,2}[0-9][A-Z0-9]? ?[0-9][A-Z]{2}$')]],
      mfaEnabled: [false],
      smsOptIn: [true],
      emergencyContactName: [''],
      emergencyContactPhone: ['']
    }, { validators: this.passwordMatchValidator });

    this.registerForm.get('password')?.valueChanges.subscribe(value => {
      this.calculatePasswordStrength(value);
    });
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

  ageValidator(control: any): { [key: string]: boolean } | null {
    if (!control.value) return null;
    const dob = new Date(control.value);
    const today = new Date();
    const age = today.getFullYear() - dob.getFullYear();
    const monthDiff = today.getMonth() - dob.getMonth();
    
    if (monthDiff < 0 || (monthDiff === 0 && today.getDate() < dob.getDate())) {
      return { tooYoung: true };
    }
    
    if (age < 16 || age > 120) {
      return { invalidAge: true };
    }
    
    return null;
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
      passwordTooShort: 'Password must be at least 8 characters',
      passwordTooWeak: 'Password must contain uppercase, lowercase, and numbers',
      passwordMismatch: 'Passwords do not match',
      tooYoung: 'You must be at least 16 years old',
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
