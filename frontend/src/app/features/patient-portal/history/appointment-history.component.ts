import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
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
  notes?: string;
  patient: {
    id: string;
    userId: string;
    nhsNumber: string;
    firstName: string;
    lastName: string;
    email: string;
    phoneNumber: string;
    dateOfBirth: string;
  };
  clinician: {
    id: string;
    userId: string;
    firstName: string;
    lastName: string;
    specialty: string;
    licenseNumber: string;
    qualifications: string[];
    status: string;
  };
  department: {
    id: string;
    name: string;
    description: string;
  };
  createdAt: string;
}

@Component({
  selector: 'app-appointment-history',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './appointment-history.component.html',
  styleUrl: './appointment-history.component.scss'
})
export class AppointmentHistoryComponent implements OnInit {
  private apiService = inject(ApiService);
  private notificationService = inject(NotificationService);

  appointments = signal<Appointment[]>([]);
  loading = signal<boolean>(true);
  
  filterStatus = signal<string>('');
  selectedAppointment = signal<Appointment | null>(null);
  showDetailsModal = signal<boolean>(false);

  statusOptions = [
    { value: '', label: 'All Appointments' },
    { value: 'Confirmed', label: 'Confirmed' },
    { value: 'Completed', label: 'Completed' },
    { value: 'Cancelled', label: 'Cancelled' },
    { value: 'DidNotAttend', label: 'Did Not Attend' }
  ];

  ngOnInit(): void {
    this.loadAppointments();
  }

  loadAppointments(): void {
    this.loading.set(true);
    
    let url = '/appointments/history';
    if (this.filterStatus()) {
      url += `?status=${this.filterStatus()}`;
    }

    this.apiService.get<Appointment[]>(url).subscribe({
      next: (data) => {
        this.appointments.set(data);
        this.loading.set(false);
      },
      error: (error) => {
        this.notificationService.error('Error', 'Failed to load appointment history');
        this.loading.set(false);
      }
    });
  }

  onStatusChange(): void {
    this.loadAppointments();
  }

  viewDetails(appointment: Appointment): void {
    this.selectedAppointment.set(appointment);
    this.showDetailsModal.set(true);
  }

  closeDetailsModal(): void {
    this.showDetailsModal.set(false);
    this.selectedAppointment.set(null);
  }

  getStatusClass(status: string): string {
    const statusMap: Record<string, string> = {
      'Confirmed': 'status-confirmed',
      'Completed': 'status-completed',
      'Cancelled': 'status-cancelled',
      'DidNotAttend': 'status-dna',
      'Pending': 'status-pending'
    };
    return statusMap[status] || 'status-default';
  }

  getStatusLabel(status: string): string {
    const labelMap: Record<string, string> = {
      'DidNotAttend': 'Did Not Attend'
    };
    return labelMap[status] || status;
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

  isUpcoming(appointment: Appointment): boolean {
    return new Date(appointment.startDateTime) > new Date() && appointment.status === 'Confirmed';
  }

  canCancel(appointment: Appointment): boolean {
    const appointmentDate = new Date(appointment.startDateTime);
    const now = new Date();
    const hoursUntilAppointment = (appointmentDate.getTime() - now.getTime()) / (1000 * 60 * 60);
    return hoursUntilAppointment > 24 && appointment.status === 'Confirmed';
  }
}