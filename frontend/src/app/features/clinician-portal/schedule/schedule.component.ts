import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import { NotificationService } from '../../../core/services/notification.service';
import { AuthService } from '../../../core/services/auth.service';

interface ScheduledAppointment {
  id: string;
  patientId: string;
  patientName: string;
  patientNhsNumber: string;
  startDateTime: string;
  endDateTime: string;
  appointmentType: string;
  status: string;
  hasClinicalNotes: boolean;
}

interface ScheduleResponse {
  clinicianId: string;
  viewType: string;
  dateRange: { start: string; end: string };
  appointments: ScheduledAppointment[];
}

@Component({
  selector: 'app-schedule',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './schedule.component.html',
  styleUrl: './schedule.component.scss'
})
export class ScheduleComponent implements OnInit {
  private apiService = inject(ApiService);
  private notificationService = inject(NotificationService);
  private authService = inject(AuthService);
  private router = inject(Router);

  appointments = signal<ScheduledAppointment[]>([]);
  loading = signal<boolean>(false);
  clinicianId = signal<string>('');

  viewType = signal<'daily' | 'weekly'>('daily');
  selectedDate = signal<Date>(new Date());

  statusColors: Record<string, string> = {
    'Confirmed': '#28a745',
    'Pending': '#ffc107',
    'Completed': '#6c757d',
    'DidNotAttend': '#dc3545',
    'Cancelled': '#6c757d'
  };

  ngOnInit(): void {
    this.loadClinicianId();
  }

  loadClinicianId(): void {
    const user = this.authService.currentUserValue;
    if (user?.clinicianId) {
      this.clinicianId.set(user.clinicianId);
      this.loadSchedule();
    } else {
      this.notificationService.error('Error', 'Clinician profile not found');
    }
  }

  loadSchedule(): void {
    const clinicianId = this.clinicianId();
    if (!clinicianId) return;

    this.loading.set(true);
    
    const startDate = this.selectedDate().toISOString();
    
    this.apiService.get<ScheduleResponse>(
      `/clinicians/${clinicianId}/schedule?viewType=${this.viewType()}&startDate=${startDate}`
    ).subscribe({
      next: (data) => {
        this.appointments.set(data.appointments || []);
        this.loading.set(false);
      },
      error: (error) => {
        this.notificationService.error('Error', 'Failed to load schedule');
        this.loading.set(false);
      }
    });
  }

  setViewType(type: 'daily' | 'weekly'): void {
    this.viewType.set(type);
    this.loadSchedule();
  }

  previousPeriod(): void {
    const current = this.selectedDate();
    if (this.viewType() === 'daily') {
      current.setDate(current.getDate() - 1);
    } else {
      current.setDate(current.getDate() - 7);
    }
    this.selectedDate.set(new Date(current));
    this.loadSchedule();
  }

  nextPeriod(): void {
    const current = this.selectedDate();
    if (this.viewType() === 'daily') {
      current.setDate(current.getDate() + 1);
    } else {
      current.setDate(current.getDate() + 7);
    }
    this.selectedDate.set(new Date(current));
    this.loadSchedule();
  }

  today(): void {
    this.selectedDate.set(new Date());
    this.loadSchedule();
  }

  getDateDisplay(): string {
    const date = this.selectedDate();
    if (this.viewType() === 'weekly') {
      const weekEnd = new Date(date);
      weekEnd.setDate(weekEnd.getDate() + 6);
      return `${date.toLocaleDateString('en-GB', { day: 'numeric', month: 'short' })} - ${weekEnd.toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' })}`;
    }
    return date.toLocaleDateString('en-GB', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' });
  }

  formatTime(dateString: string): string {
    return new Date(dateString).toLocaleTimeString('en-GB', {
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  getStatusClass(status: string): string {
    return status.toLowerCase().replace(/ /g, '-');
  }

  viewAppointment(appointment: ScheduledAppointment): void {
    this.router.navigate(['/clinician/clinical-notes', appointment.id]);
  }

  markAsDna(appointment: ScheduledAppointment): void {
    if (confirm(`Mark ${appointment.patientName} as Did Not Attend?`)) {
      this.apiService.post(`/appointments/${appointment.id}/dna`, {
        reason: 'Patient did not attend'
      }).subscribe({
        next: () => {
          this.notificationService.success('Success', 'Appointment marked as DNA');
          this.loadSchedule();
        },
        error: () => {
          this.notificationService.error('Error', 'Failed to mark as DNA');
        }
      });
    }
  }
}