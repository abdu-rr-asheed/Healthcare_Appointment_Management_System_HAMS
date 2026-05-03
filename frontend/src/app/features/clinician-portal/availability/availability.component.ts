import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';
import { NotificationService } from '../../../core/services/notification.service';
import { AuthService } from '../../../core/services/auth.service';

interface RegularSchedule {
  dayOfWeek: number;
  startTime: string;
  endTime: string;
  recurring: boolean;
  isAvailable: boolean;
}

interface LeavePeriod {
  leavePeriodId: string;
  startDate: string;
  endDate: string;
  reason: string;
  type: string;
  isApproved: boolean;
}

interface SlotConfiguration {
  appointmentType: string;
  durationMinutes: number;
  bufferMinutes: number;
}

interface AvailabilityData {
  clinicianId: string;
  regularSchedule: RegularSchedule[];
  leavePeriods: LeavePeriod[];
  slotConfigurations: SlotConfiguration[];
}

const DAY_NAMES = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];
const APPOINTMENT_TYPES = ['InitialConsultation', 'FollowUp', 'Emergency'];

@Component({
  selector: 'app-availability',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './availability.component.html',
  styleUrl: './availability.component.scss'
})
export class AvailabilityComponent implements OnInit {
  private apiService = inject(ApiService);
  private notificationService = inject(NotificationService);
  private authService = inject(AuthService);

  availability = signal<AvailabilityData | null>(null);
  loading = signal<boolean>(false);
  saving = signal<boolean>(false);
  generating = signal<boolean>(false);
  clinicianId = signal<string>('');

  activeTab = signal<'schedule' | 'leave' | 'slots'>('schedule');
  
  scheduleForm = signal<RegularSchedule[]>(
    DAY_NAMES.map((_, i) => ({
      dayOfWeek: i,
      startTime: '09:00',
      endTime: '17:00',
      recurring: true,
      isAvailable: i >= 1 && i <= 5
    }))
  );

  // Plain mutable objects — [(ngModel)] on signal().property never writes back to the signal.
  newLeave = {
    startDate: '',
    endDate: '',
    reason: '',
    type: 'Annual'
  };

  slotConfigForm = signal<SlotConfiguration[]>(
    APPOINTMENT_TYPES.map(type => ({
      appointmentType: type,
      durationMinutes: type === 'InitialConsultation' ? 30 : type === 'FollowUp' ? 15 : 45,
      bufferMinutes: 10
    }))
  );

  generateDateRange = {
    startDate: new Date().toISOString().split('T')[0],
    endDate: new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString().split('T')[0]
  };

  dayNames = DAY_NAMES;
  leaveTypes = ['Annual', 'Sick', 'Study', 'Other'];
  appointmentTypes = APPOINTMENT_TYPES;

  hasSchedule = computed(() => 
    this.scheduleForm().some(s => s.isAvailable)
  );

  ngOnInit(): void {
    this.loadClinicianId();
  }

  loadClinicianId(): void {
    const user = this.authService.currentUserValue;
    if (user?.clinicianId) {
      this.clinicianId.set(user.clinicianId);
      this.loadAvailability();
    }
  }

  loadAvailability(): void {
    const id = this.clinicianId();
    if (!id) return;

    this.loading.set(true);
    this.apiService.get<AvailabilityData>(`/clinicians/${id}/availability`).subscribe({
      next: (data) => {
        this.availability.set(data);
        if (data.regularSchedule?.length > 0) {
          const merged = DAY_NAMES.map((_, i) => {
            const existing = data.regularSchedule.find(s => s.dayOfWeek === i);
            return existing || {
              dayOfWeek: i,
              startTime: '09:00',
              endTime: '17:00',
              recurring: true,
              isAvailable: false
            };
          });
          this.scheduleForm.set(merged);
        }
        if (data.slotConfigurations?.length > 0) {
          this.slotConfigForm.set(data.slotConfigurations);
        }
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
      }
    });
  }

  toggleDayAvailability(index: number): void {
    this.scheduleForm.update(schedules => {
      const updated = [...schedules];
      updated[index] = { ...updated[index], isAvailable: !updated[index].isAvailable };
      return updated;
    });
  }

  updateScheduleTime(index: number, field: 'startTime' | 'endTime', value: string): void {
    this.scheduleForm.update(schedules => {
      const updated = [...schedules];
      updated[index] = { ...updated[index], [field]: value };
      return updated;
    });
  }

  saveSchedule(): void {
    this.saving.set(true);
    const id = this.clinicianId();

    const request = {
      regularSchedule: this.scheduleForm().filter(s => s.isAvailable),
      slotConfigurations: this.slotConfigForm()
    };

    this.apiService.put(`/clinicians/${id}/availability`, request).subscribe({
      next: () => {
        this.saving.set(false);
        this.notificationService.success('Success', 'Availability saved successfully');
      },
      error: () => {
        this.saving.set(false);
        this.notificationService.error('Error', 'Failed to save availability');
      }
    });
  }

  addLeavePeriod(): void {
    if (!this.newLeave.startDate || !this.newLeave.endDate || !this.newLeave.reason) {
      this.notificationService.warning('Warning', 'Please fill all leave period fields');
      return;
    }

    this.saving.set(true);
    const id = this.clinicianId();

    const request = {
      regularSchedule: this.scheduleForm(),
      leavePeriods: [{ ...this.newLeave, isApproved: false }],
      slotConfigurations: this.slotConfigForm()
    };

    this.apiService.put(`/clinicians/${id}/availability`, request).subscribe({
      next: () => {
        this.saving.set(false);
        this.notificationService.success('Success', 'Leave period added');
        this.newLeave = { startDate: '', endDate: '', reason: '', type: 'Annual' };
        this.loadAvailability();
      },
      error: () => {
        this.saving.set(false);
        this.notificationService.error('Error', 'Failed to add leave period');
      }
    });
  }

  removeLeavePeriod(leaveId: string): void {
    if (confirm('Remove this leave period?')) {
      this.notificationService.success('Success', 'Leave period removed (save to apply)');
    }
  }

  updateSlotConfig(index: number, field: string, value: number): void {
    this.slotConfigForm.update(configs => {
      const updated = [...configs];
      updated[index] = { ...updated[index], [field]: value };
      return updated;
    });
  }

  generateSlots(): void {
    if (!this.generateDateRange.startDate || !this.generateDateRange.endDate) {
      this.notificationService.warning('Warning', 'Please select date range');
      return;
    }

    this.generating.set(true);
    const id = this.clinicianId();

    this.apiService.post<{ slotsGenerated: number; slotsBlocked: number; warnings: string[] }>(`/clinicians/${id}/slots/generate`, {
      startDate: this.generateDateRange.startDate,
      endDate: this.generateDateRange.endDate
    }).subscribe({
      next: (response) => {
        this.generating.set(false);
        this.notificationService.success(
          'Success', 
          `Generated ${response.slotsGenerated} slots${response.slotsBlocked ? `, ${response.slotsBlocked} blocked due to leave` : ''}`
        );
      },
      error: () => {
        this.generating.set(false);
        this.notificationService.error('Error', 'Failed to generate slots');
      }
    });
  }

  setTab(tab: 'schedule' | 'leave' | 'slots'): void {
    this.activeTab.set(tab);
  }

  formatDate(dateString: string): string {
    if (!dateString) return '';
    return new Date(dateString).toLocaleDateString('en-GB');
  }
}
