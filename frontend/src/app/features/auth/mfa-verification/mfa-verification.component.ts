import { Component, OnInit, ElementRef, ViewChild } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { inject } from '@angular/core';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';

@Component({
  selector: 'app-mfa-verification',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './mfa-verification.component.html',
  styleUrl: './mfa-verification.component.scss'
})
export class MfaVerificationComponent implements OnInit {
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private authService = inject(AuthService);
  private notificationService = inject(NotificationService);

  @ViewChild('input0') input0!: ElementRef<HTMLInputElement>;
  @ViewChild('input1') input1!: ElementRef<HTMLInputElement>;
  @ViewChild('input2') input2!: ElementRef<HTMLInputElement>;
  @ViewChild('input3') input3!: ElementRef<HTMLInputElement>;
  @ViewChild('input4') input4!: ElementRef<HTMLInputElement>;
  @ViewChild('input5') input5!: ElementRef<HTMLInputElement>;

  codeInputs: string[] = ['', '', '', '', '', ''];
  loading = false;
  error = '';
  success = false;
  userId = '';
  canResend = false;
  countdown = 60;
  countdownInterval: any;

  ngOnInit(): void {
    this.userId = this.route.snapshot.queryParamMap.get('userId') || '';
    
    if (!this.userId) {
      this.notificationService.error('Invalid Request', 'Missing user ID. Please login again.');
      this.router.navigate(['/auth/login']);
      return;
    }

    this.startCountdown();
    setTimeout(() => {
      this.focusInput(0);
    }, 100);
  }

  ngOnDestroy(): void {
    if (this.countdownInterval) {
      clearInterval(this.countdownInterval);
    }
  }

  onInput(index: number, event: Event): void {
    const input = event.target as HTMLInputElement;
    const value = input.value;

    if (value.length > 1) {
      input.value = value.slice(0, 1);
      this.codeInputs[index] = value.slice(0, 1);
    } else {
      this.codeInputs[index] = value;
    }

    if (value && index < 5) {
      this.focusInput(index + 1);
    }

    this.error = '';

    if (this.isCodeComplete()) {
      this.submitCode();
    }
  }

  onKeyDown(index: number, event: KeyboardEvent): void {
    if (event.key === 'Backspace' && !this.codeInputs[index] && index > 0) {
      this.focusInput(index - 1);
    } else if (event.key === 'ArrowLeft' && index > 0) {
      event.preventDefault();
      this.focusInput(index - 1);
    } else if (event.key === 'ArrowRight' && index < 5) {
      event.preventDefault();
      this.focusInput(index + 1);
    }
  }

  onPaste(event: ClipboardEvent): void {
    event.preventDefault();
    const pastedData = event.clipboardData?.getData('text') || '';
    const digits = pastedData.replace(/\D/g, '').slice(0, 6);

    for (let i = 0; i < digits.length; i++) {
      this.codeInputs[i] = digits[i];
    }

    this.error = '';

    if (this.isCodeComplete()) {
      this.submitCode();
    } else {
      this.focusInput(digits.length);
    }
  }

  focusInput(index: number): void {
    const inputs = [this.input0, this.input1, this.input2, this.input3, this.input4, this.input5];
    setTimeout(() => {
      inputs[index]?.nativeElement?.focus();
    }, 10);
  }

  isCodeComplete(): boolean {
    return this.codeInputs.every(digit => digit.length === 1);
  }

  submitCode(): void {
    if (this.loading || !this.isCodeComplete()) return;

    this.loading = true;
    this.error = '';

    const code = this.codeInputs.join('');
    
    this.authService.verifyMfa(this.userId, code).subscribe({
      next: (response) => {
        this.success = true;
        this.notificationService.success('MFA Verified', 'Two-factor authentication successful');
        
        setTimeout(() => {
          this.redirectBasedOnRole(response.user.role);
        }, 1000);
      },
      error: (error) => {
        this.error = error.error?.message || 'Invalid verification code. Please try again.';
        this.notificationService.error('Verification Failed', this.error);
        this.loading = false;
        this.clearCode();
        this.focusInput(0);
      }
    });
  }

  resendCode(): void {
    if (this.loading || !this.canResend) return;

    this.loading = true;

    this.authService.resendMfa(this.userId).subscribe({
      next: () => {
        this.notificationService.success('Code Resent', 'A new verification code has been sent to your phone');
        this.startCountdown();
        this.loading = false;
        this.clearCode();
        this.focusInput(0);
      },
      error: (error) => {
        this.notificationService.error('Resend Failed', error.error?.message || 'Failed to resend code');
        this.loading = false;
      }
    });
  }

  startCountdown(): void {
    this.canResend = false;
    this.countdown = 60;

    if (this.countdownInterval) {
      clearInterval(this.countdownInterval);
    }

    this.countdownInterval = setInterval(() => {
      this.countdown--;
      if (this.countdown <= 0) {
        this.canResend = true;
        clearInterval(this.countdownInterval);
      }
    }, 1000);
  }

  clearCode(): void {
    this.codeInputs = ['', '', '', '', '', ''];
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

  getMinutes(): number {
    return Math.floor(this.countdown / 60);
  }

  getSeconds(): number {
    return this.countdown % 60;
  }
}
