import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
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

interface AvailableSlot {
  id: string;
  startDateTime: string;
  endDateTime: string;
  clinicianId: string;
  clinicianName: string;
  departmentId: string;
  departmentName: string;
  isAvailable: boolean;
}

@Component({
  selector: 'app-reschedule',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './reschedule.component.html',
  styleUrl: './reschedule.component.scss'
})
export class RescheduleComponent implements OnInit {
  private apiService = inject(ApiService);
  private notificationService = inject(NotificationService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  appointment = signal<Appointment | null>(null);
  alternativeSlots = signal<AvailableSlot[]>([]);
  selectedSlot = signal<AvailableSlot | null>(null);
  loading = signal<boolean>(false);
  submitting = signal<boolean>(false);
  currentStep = signal<number>(1);
  error = signal<string>('');

  appointmentId = signal<string>('');

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
        this.loadAlternativeSlots();
      },
      error: (error) => {
        this.notificationService.error('Error', 'Failed to load appointment details');
        this.loading.set(false);
      }
    });
  }

  loadAlternativeSlots(): void {
    const appointment = this.appointment();
    if (!appointment) return;

    const startDate = new Date();
    const endDate = new Date();
    endDate.setDate(endDate.getDate() + 30);

    this.apiService.get<AvailableSlot[]>(
      `/appointments/${this.appointmentId()}/alternativeslots?startDate=${startDate.toISOString()}&endDate=${endDate.toISOString()}`
    ).subscribe({
      next: (data) => {
        this.alternativeSlots.set(data);
        this.loading.set(false);
      },
      error: (error) => {
        this.notificationService.error('Error', 'Failed to load alternative slots');
        this.loading.set(false);
      }
    });
  }

  selectSlot(slot: AvailableSlot): void {
    this.selectedSlot.set(slot);
  }

  confirmReschedule(): void {
    const slot = this.selectedSlot();
    if (!slot) return;

    this.submitting.set(true);
    this.error.set('');

    this.apiService.put(`/appointments/${this.appointmentId()}/reschedule`, {
      newSlotId: slot.id,
      reason: 'Patient requested reschedule'
    }).subscribe({
      next: () => {
        this.notificationService.success('Success', 'Appointment rescheduled successfully');
        this.currentStep.set(3);
        this.submitting.set(false);
      },
      error: (error) => {
        this.error.set(error.error?.message || 'Failed to reschedule appointment');
        this.submitting.set(false);
      }
    });
  }

  backToSlots(): void {
    this.selectedSlot.set(null);
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

  getCurrentDateTime(): string {
    const apt = this.appointment();
    if (!apt) return '';
    return `${this.formatDate(apt.startDateTime)} at ${this.formatTime(apt.startDateTime)}`;
  }
}