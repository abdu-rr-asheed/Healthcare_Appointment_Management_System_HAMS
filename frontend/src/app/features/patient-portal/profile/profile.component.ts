import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import { NotificationService } from '../../../core/services/notification.service';
import { AuthService } from '../../../core/services/auth.service';

interface PatientProfile {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  nhsNumber: string;
  dateOfBirth: string;
  smsOptIn: boolean;
}

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.scss'
})
export class ProfileComponent implements OnInit {
  private apiService = inject(ApiService);
  private notificationService = inject(NotificationService);
  private authService = inject(AuthService);
  private router = inject(Router);

  profile = signal<PatientProfile | null>(null);
  loading = signal<boolean>(false);
  saving = signal<boolean>(false);
  isEditing = signal<boolean>(false);

  // Plain mutable object — [(ngModel)] writes back directly without needing signal.set().
  formData: Partial<PatientProfile> = {};
  error = signal<string>('');

  constructor() {}

  ngOnInit(): void {
    this.loadProfile();
  }

  loadProfile(): void {
    this.loading.set(true);
    this.apiService.get<PatientProfile>('/patients/me').subscribe({
      next: (data) => {
        this.profile.set(data);
        this.formData = { ...data };
        this.loading.set(false);
      },
      error: (error) => {
        this.notificationService.error('Error', 'Failed to load profile');
        this.loading.set(false);
      }
    });
  }

  toggleEdit(): void {
    this.isEditing.update(v => !v);
    if (!this.isEditing()) {
      const current = this.profile();
      if (current) {
        this.formData = { ...current };
      }
    }
  }

  saveProfile(): void {
    this.saving.set(true);
    this.error.set('');

    this.apiService.put('/patients/me', this.formData).subscribe({
      next: (data: any) => {
        this.profile.set(data as PatientProfile);
        this.formData = { ...data } as PatientProfile;
        this.isEditing.set(false);
        this.saving.set(false);
        this.notificationService.success('Success', 'Profile updated successfully');
      },
      error: (error) => {
        this.error.set(error.error?.message || 'Failed to update profile');
        this.saving.set(false);
      }
    });
  }

  toggleSmsOptIn(): void {
    this.formData = { ...this.formData, smsOptIn: !this.formData.smsOptIn };

    if (this.isEditing()) {
      this.saveProfile();
    }
  }

  formatDate(dateString: string): string {
    return new Date(dateString).toLocaleDateString('en-GB', {
      day: 'numeric',
      month: 'long',
      year: 'numeric'
    });
  }

  logout(): void {
    // logout() returns an Observable — must subscribe to trigger the HTTP call.
    this.authService.logout().subscribe({
      complete: () => this.router.navigate(['/auth/login']),
      error: () => this.router.navigate(['/auth/login'])
    });
  }
}