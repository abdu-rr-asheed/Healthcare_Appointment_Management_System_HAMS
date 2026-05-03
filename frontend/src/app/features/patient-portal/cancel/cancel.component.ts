import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import { NotificationService } from '../../../core/services/notification.service';

interface Appointment {
  id: string;
  confirmationReference: string;
  startDateTime: string;
  endDateTime: string;
  clinicianName: string;
  departmentName: string;
  status: string;
  appointmentType: string;
}

@Component({
  selector: 'app-cancel',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './cancel.component.html',
  styleUrl: './cancel.component.scss'
})
export class CancelComponent implements OnInit {
  private apiService = inject(ApiService);
  private notificationService = inject(NotificationService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  appointment = signal<Appointment | null>(null);
  loading = signal<boolean>(false);
  submitting = signal<boolean>(false);
  currentStep = signal<number>(1);
  error = signal<string>('');

  reason = signal<string>('');
  acknowledgeLateNotice = signal<boolean>(false);
  showLateWarning = signal<boolean>(false);

  appointmentId = signal<string>('');

  reasons = [
    { value: 'illness', label: 'Illness' },
    { value: 'work', label: 'Work commitments' },
    { value: 'family', label: 'Family emergency' },
    { value: 'transport', label: 'Transport issues' },
    { value: 'other', label: 'Other' }
  ];

  readonly CANCELLATION_NOTICE_HOURS = 24;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.appointmentId.set(id);
      this.loadAppointment(id);
    } else {
      this.router.navigate(['/patient']);
    }
  }

  loadAppointment(id: string): void {
    this.loading.set(true);
    this.apiService.get<Appointment>(`/appointments/${id}`).subscribe({
      next: (data) => {
        this.appointment.set(data);
        this.checkNoticePeriod();
        this.loading.set(false);
      },
      error: (error) => {
        this.notificationService.error('Error', 'Failed to load appointment details');
        this.loading.set(false);
      }
    });
  }

  checkNoticePeriod(): void {
    const apt = this.appointment();
    if (!apt) return;

    const appointmentTime = new Date(apt.startDateTime).getTime();
    const now = Date.now();
    const hoursUntilAppointment = (appointmentTime - now) / (1000 * 60 * 60);

    if (hoursUntilAppointment < this.CANCELLATION_NOTICE_HOURS) {
      this.showLateWarning.set(true);
    }
  }

  confirmCancellation(): void {
    if (this.showLateWarning() && !this.acknowledgeLateNotice()) {
      this.error.set('Please acknowledge the late cancellation notice');
      return;
    }

    this.submitting.set(true);
    this.error.set('');

    this.apiService.delete(`/appointments/${this.appointmentId()}`, {
      reason: this.reason(),
      acknowledgeLateNotice: this.acknowledgeLateNotice()
    }).subscribe({
      next: () => {
        this.notificationService.success('Success', 'Appointment cancelled successfully');
        this.currentStep.set(2);
        this.submitting.set(false);
      },
      error: (error) => {
        this.error.set(error.error?.message || 'Failed to cancel appointment');
        this.submitting.set(false);
      }
    });
  }

  goToDashboard(): void {
    this.router.navigate(['/patient']);
  }

  formatDate(dateString: string): string {
    return new Date(dateString).toLocaleDateString('en-GB', {
      weekday: 'long',
      day: 'numeric',
      month: 'long',
      year: 'numeric'
    });
  }

  formatTime(dateString: string): string {
    return new Date(dateString).toLocaleTimeString('en-GB', {
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  getAppointmentDateTime(): string {
    const apt = this.appointment();
    if (!apt) return '';
    return `${this.formatDate(apt.startDateTime)} at ${this.formatTime(apt.startDateTime)}`;
  }

  getHoursUntilAppointment(): number {
    const apt = this.appointment();
    if (!apt) return 0;
    const appointmentTime = new Date(apt.startDateTime).getTime();
    const now = Date.now();
    return Math.floor((appointmentTime - now) / (1000 * 60 * 60));
  }
}