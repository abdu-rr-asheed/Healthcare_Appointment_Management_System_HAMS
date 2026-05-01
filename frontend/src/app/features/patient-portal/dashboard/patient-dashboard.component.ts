import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import { NotificationService } from '../../../core/services/notification.service';

export interface Appointment {
  id: string;
  confirmationReference: string;
  startDateTime: string;
  endDateTime: string;
  status: string;
  appointmentType: string;
  clinician: {
    firstName: string;
    lastName: string;
    specialty: string;
  };
  department: {
    name: string;
  };
}

@Component({
  selector: 'app-patient-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './patient-dashboard.component.html',
  styleUrl: './patient-dashboard.component.scss'
})
export class PatientDashboardComponent implements OnInit {
  private apiService = inject(ApiService);
  private notificationService = inject(NotificationService);

  upcomingAppointments = signal<Appointment[]>([]);
  loading = signal<boolean>(true);

  ngOnInit(): void {
    this.loadUpcomingAppointments();
  }

  loadUpcomingAppointments(): void {
    this.loading.set(true);
    this.apiService.get<Appointment[]>('/appointments/upcoming').subscribe({
      next: (data) => {
        this.upcomingAppointments.set(data.slice(0, 5)); // Show next 5 appointments
        this.loading.set(false);
      },
      error: (error) => {
        this.notificationService.error('Error', 'Failed to load appointments');
        this.loading.set(false);
      }
    });
  }

  formatDate(dateString: string): string {
    return new Date(dateString).toLocaleDateString('en-GB', {
      weekday: 'short',
      day: 'numeric',
      month: 'short',
      year: 'numeric'
    });
  }

  formatTime(dateString: string): string {
    return new Date(dateString).toLocaleTimeString('en-GB', {
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  getDaysUntilAppointment(appointment: Appointment): number {
    const appointmentDate = new Date(appointment.startDateTime);
    const now = new Date();
    const diffTime = appointmentDate.getTime() - now.getTime();
    return Math.ceil(diffTime / (1000 * 60 * 60 * 24));
  }

  getAppointmentDateNumber(appointment: Appointment): number {
    return new Date(appointment.startDateTime).getDate();
  }

  getAppointmentDateMonth(appointment: Appointment): string {
    return new Date(appointment.startDateTime).toLocaleString('en-GB', { month: 'short' });
  }
}