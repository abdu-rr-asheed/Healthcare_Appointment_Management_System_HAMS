import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService, LoginRequest } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent {
  private router = inject(Router);
  private authService = inject(AuthService);
  private notificationService = inject(NotificationService);

  loginRequest: LoginRequest = {
    nhsNumber: '',
    password: ''
  };

  loading = false;
  showPassword = false;

  onSubmit(): void {
    if (this.loading) return;
    
    if (!this.loginRequest.nhsNumber || !this.loginRequest.password) {
      this.notificationService.warning('Validation', 'Please enter both NHS number and password');
      return;
    }
    
    this.loading = true;
    
    this.authService.login(this.loginRequest).subscribe({
      next: (response) => {
        if (response.requiresMfa) {
          this.notificationService.info('MFA Required', 'Please complete multi-factor authentication');
          this.router.navigate(['/auth/mfa'], { 
            queryParams: { userId: response.mfaUserId || response.user?.id } 
          });
        } else {
          this.notificationService.success('Welcome', `Welcome back, ${response.user?.firstName || 'User'}!`);
          this.redirectBasedOnRole(response.user.role);
        }
        this.loading = false;
      },
      error: (error: any) => {
        console.error('Login error:', error);
        let errorMessage = 'Invalid credentials. Please try again.';
        
        if (error.error?.message) {
          errorMessage = error.error.message;
        } else if (error.status === 401) {
          errorMessage = 'Invalid NHS number or password.';
        } else if (error.status === 403) {
          errorMessage = 'Your account has been locked. Please contact support.';
        } else if (error.status === 423) {
          errorMessage = 'Account locked due to too many failed attempts.';
        } else if (error.status === 0 || error.name === 'TimeoutError') {
          errorMessage = 'Unable to connect to server. Please check your internet connection.';
        }
        
        this.notificationService.error('Login Failed', errorMessage);
        this.loading = false;
      }
    });
  }

  private redirectBasedOnRole(role: string): void {
    switch (role) {
      case 'Patient':
        this.router.navigate(['/patient/dashboard']);
        break;
      case 'Clinician':
        this.router.navigate(['/clinician/dashboard']);
        break;
      case 'Administrator':
        this.router.navigate(['/admin/dashboard']);
        break;
      default:
        this.router.navigate(['/dashboard']);
    }
  }

  togglePassword(): void {
    this.showPassword = !this.showPassword;
  }
}
