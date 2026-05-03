import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import { NotificationService } from '../../../core/services/notification.service';

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

@Component({
  selector: 'app-clinician-availability-admin',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="admin-avail-container">
      <div class="page-header">
        <a routerLink="/admin/clinicians" class="back-link">← Back to Clinicians</a>
        <h1>Clinician Availability</h1>
        <p class="subtitle">Read-only availability view for clinician {{ clinicianId() }}</p>
      </div>

      @if (loading()) {
        <div class="loading-state"><div class="spinner"></div><p>Loading availability...</p></div>
      } @else if (!availability()) {
        <div class="empty-state"><p>No availability data found for this clinician.</p></div>
      } @else {
        <div class="content-card">
          <h2>Weekly Schedule</h2>
          <div class="schedule-list">
            @for (slot of availability()!.regularSchedule; track slot.dayOfWeek) {
              @if (slot.isAvailable) {
                <div class="schedule-row">
                  <span class="day">{{ dayNames[slot.dayOfWeek] }}</span>
                  <span class="time">{{ slot.startTime }} – {{ slot.endTime }}</span>
                </div>
              }
            }
            @if (availability()!.regularSchedule.every(s => !s.isAvailable)) {
              <p class="no-data">No working days configured.</p>
            }
          </div>
        </div>

        @if (availability()!.leavePeriods?.length > 0) {
          <div class="content-card">
            <h2>Leave Periods</h2>
            <div class="leave-list">
              @for (leave of availability()!.leavePeriods; track leave.leavePeriodId) {
                <div class="leave-row">
                  <span class="leave-type">{{ leave.type }}</span>
                  <span class="leave-dates">{{ formatDate(leave.startDate) }} – {{ formatDate(leave.endDate) }}</span>
                  <span class="leave-status" [class.approved]="leave.isApproved">
                    {{ leave.isApproved ? 'Approved' : 'Pending' }}
                  </span>
                  @if (leave.reason) { <span class="leave-reason">{{ leave.reason }}</span> }
                </div>
              }
            </div>
          </div>
        }

        @if (availability()!.slotConfigurations?.length > 0) {
          <div class="content-card">
            <h2>Slot Configuration</h2>
            <div class="slot-list">
              @for (cfg of availability()!.slotConfigurations; track cfg.appointmentType) {
                <div class="slot-row">
                  <span class="appt-type">{{ cfg.appointmentType }}</span>
                  <span>{{ cfg.durationMinutes }} min duration</span>
                  <span>{{ cfg.bufferMinutes }} min buffer</span>
                </div>
              }
            </div>
          </div>
        }
      }
    </div>
  `,
  styles: [`
    .admin-avail-container { max-width: 900px; margin: 0 auto; }
    .page-header { margin-bottom: 24px; }
    .back-link { color: #2563eb; text-decoration: none; font-size: 14px; }
    .back-link:hover { text-decoration: underline; }
    h1 { margin: 8px 0 4px; }
    .subtitle { color: #6b7280; font-size: 14px; margin: 0; }
    .loading-state, .empty-state { text-align: center; padding: 48px; color: #6b7280; }
    .spinner { width: 32px; height: 32px; border: 3px solid #e5e7eb; border-top-color: #2563eb; border-radius: 50%; animation: spin 0.8s linear infinite; margin: 0 auto 12px; }
    @keyframes spin { to { transform: rotate(360deg); } }
    .content-card { background: #fff; border: 1px solid #e5e7eb; border-radius: 8px; padding: 20px; margin-bottom: 16px; }
    .content-card h2 { margin: 0 0 16px; font-size: 16px; }
    .schedule-row, .leave-row, .slot-row { display: flex; gap: 16px; padding: 8px 0; border-bottom: 1px solid #f3f4f6; align-items: center; }
    .day { font-weight: 600; min-width: 100px; }
    .leave-type { font-weight: 600; min-width: 80px; }
    .leave-status { padding: 2px 8px; border-radius: 4px; background: #fef3c7; color: #92400e; font-size: 12px; }
    .leave-status.approved { background: #d1fae5; color: #065f46; }
    .appt-type { font-weight: 600; min-width: 160px; }
    .no-data { color: #9ca3af; font-style: italic; }
  `]
})
export class ClinicianAvailabilityAdminComponent implements OnInit {
  private apiService = inject(ApiService);
  private notificationService = inject(NotificationService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  availability = signal<AvailabilityData | null>(null);
  loading = signal<boolean>(false);
  clinicianId = signal<string>('');
  dayNames = DAY_NAMES;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.router.navigate(['/admin/clinicians']);
      return;
    }
    this.clinicianId.set(id);
    this.loadAvailability(id);
  }

  loadAvailability(id: string): void {
    this.loading.set(true);
    this.apiService.get<AvailabilityData>(`/clinicians/${id}/availability`).subscribe({
      next: (data) => {
        this.availability.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.notificationService.error('Error', 'Failed to load clinician availability');
      }
    });
  }

  formatDate(dateString: string): string {
    if (!dateString) return '';
    return new Date(dateString).toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' });
  }
}
