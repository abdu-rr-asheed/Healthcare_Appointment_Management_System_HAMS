import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import { NotificationService } from '../../../core/services/notification.service';

export interface Department {
  id: string;
  name: string;
  description: string;
}

export interface Clinician {
  id: string;
  userId: string;
  firstName: string;
  lastName: string;
  specialty: string;
  licenseNumber: string;
  qualifications: string[];
  status: string;
}

export interface AvailableSlot {
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
  selector: 'app-booking',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './booking.component.html',
  styleUrl: './booking.component.scss'
})
export class BookingComponent implements OnInit {
  private apiService = inject(ApiService);
  private notificationService = inject(NotificationService);
  private router = inject(Router);

  departments = signal<Department[]>([]);
  clinicians = signal<Clinician[]>([]);
  availableSlots = signal<AvailableSlot[]>([]);
  
  selectedDepartment = signal<string>('');
  selectedClinician = signal<string>('');
  selectedSlot = signal<AvailableSlot | null>(null);
  
  appointmentType = signal<string>('InitialConsultation');
  notes = signal<string>('');
  
  loading = signal<boolean>(false);
  currentStep = signal<number>(1);
  
  minDate = signal<Date>(new Date());
  maxDate = signal<Date>(new Date(Date.now() + 30 * 24 * 60 * 60 * 1000)); // 30 days ahead

  ngOnInit(): void {
    this.loadDepartments();
  }

  loadDepartments(): void {
    this.loading.set(true);
    this.apiService.get<Department[]>('/departments').subscribe({
      next: (data) => {
        this.departments.set(data);
        this.loading.set(false);
      },
      error: (error) => {
        this.notificationService.error('Error', 'Failed to load departments');
        this.loading.set(false);
      }
    });
  }

  selectDepartment(departmentId: string): void {
    this.selectedDepartment.set(departmentId);
    this.onDepartmentChange();
  }

  onDepartmentChange(): void {
    if (!this.selectedDepartment()) {
      this.clinicians.set([]);
      this.availableSlots.set([]);
      return;
    }

    this.loading.set(true);
    this.selectedClinician.set('');
    this.availableSlots.set([]);

    this.apiService.get<Clinician[]>(`/departments/${this.selectedDepartment()}/clinicians`).subscribe({
      next: (data) => {
        this.clinicians.set(data);
        this.loading.set(false);
      },
      error: (error) => {
        this.notificationService.error('Error', 'Failed to load clinicians');
        this.loading.set(false);
      }
    });
  }

  loadAvailableSlots(): void {
    if (!this.selectedDepartment()) {
      this.notificationService.warning('Warning', 'Please select a department first');
      return;
    }

    this.loading.set(true);
    this.currentStep.set(2);

    const startDate = this.minDate();
    const endDate = this.maxDate();

    let url = `/appointments/slots?departmentId=${this.selectedDepartment()}&startDate=${startDate.toISOString()}&endDate=${endDate.toISOString()}`;
    
    if (this.selectedClinician()) {
      url += `&clinicianId=${this.selectedClinician()}`;
    }

    this.apiService.get<AvailableSlot[]>(url).subscribe({
      next: (data) => {
        this.availableSlots.set(data);
        this.loading.set(false);
      },
      error: (error) => {
        this.notificationService.error('Error', 'Failed to load available slots');
        this.loading.set(false);
      }
    });
  }

  selectSlot(slot: AvailableSlot): void {
    this.selectedSlot.set(slot);
    this.currentStep.set(3);
  }

  backToSlots(): void {
    this.selectedSlot.set(null);
    this.currentStep.set(2);
  }

  confirmBooking(): void {
    if (!this.selectedSlot()) return;

    this.loading.set(true);

    const bookingRequest = {
      slotId: this.selectedSlot()!.id,
      appointmentType: this.appointmentType(),
      notes: this.notes()
    };

    this.apiService.post('/appointments', bookingRequest).subscribe({
      next: (response) => {
        this.notificationService.success('Success', 'Appointment booked successfully!');
        this.currentStep.set(4);
        this.loading.set(false);
      },
      error: (error) => {
        this.notificationService.error('Error', error.error?.message || 'Failed to book appointment');
        this.loading.set(false);
      }
    });
  }

  goToDashboard(): void {
    this.router.navigate(['/patient']);
  }

  resetBooking(): void {
    this.selectedDepartment.set('');
    this.selectedClinician.set('');
    this.selectedSlot.set(null);
    this.availableSlots.set([]);
    this.currentStep.set(1);
    this.appointmentType.set('InitialConsultation');
    this.notes.set('');
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

  filterSlotsByDate(date: Date): AvailableSlot[] {
    const dateString = date.toDateString();
    return this.availableSlots().filter(slot => 
      new Date(slot.startDateTime).toDateString() === dateString
    );
  }

  getAvailableDates(): Date[] {
    const dates = new Set<string>();
    this.availableSlots().forEach(slot => {
      dates.add(new Date(slot.startDateTime).toDateString());
    });
    return Array.from(dates).map(date => new Date(date));
  }

  getSlotsForSelectedDate(): AvailableSlot[] {
    return this.availableSlots();
  }
}