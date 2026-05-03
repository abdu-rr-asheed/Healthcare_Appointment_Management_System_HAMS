import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import { NotificationService } from '../../../core/services/notification.service';
import { AuthService } from '../../../core/services/auth.service';

interface ClinicianInfo {
  id: string;
  name: string;
  specialty: string;
  licenseNumber: string;
}

interface AppointmentSummary {
  id: string;
  confirmationReference: string;
  patientName: string;
  patientNhsNumber?: string;
  startDateTime: string;
  endDateTime: string;
  appointmentType: string;
  departmentName: string;
  status: string;
  notes?: string;
}

interface ClinicianStats {
  appointmentsToday: number;
  appointmentsThisWeek: number;
  completedAllTime: number;
  pendingToday: number;
  upcomingNext7Days: number;
}

interface NextAppointment {
  id: string;
  patientName: string;
  startDateTime: string;
}

interface ClinicianDashboardData {
  clinicianInfo: ClinicianInfo;
  todayAppointments: AppointmentSummary[];
  upcomingAppointments: AppointmentSummary[];
  stats: ClinicianStats;
  nextAppointment: NextAppointment | null;
}

@Component({
  selector: 'app-clinician-dashboard',
  standalone: true,
  imports: [CommonModule, DatePipe, RouterLink],
  templateUrl: './clinician-dashboard.component.html',
  styleUrl: './clinician-dashboard.component.scss'
})
export class ClinicianDashboardComponent implements OnInit {
  private apiService         = inject(ApiService);
  private notificationService = inject(NotificationService);
  private authService        = inject(AuthService);

  data    = signal<ClinicianDashboardData | null>(null);
  loading = signal<boolean>(true);
  error   = signal<string>('');

  readonly today = new Date();

  /** Currently-selected tab on the today's schedule view */
  activeTab = signal<'today' | 'upcoming'>('today');

  get clinicianName(): string {
    const u = this.authService.currentUserValue;
    return u ? `Dr. ${u.firstName} ${u.lastName}` : 'Clinician';
  }

  get greeting(): string {
    const h = new Date().getHours();
    if (h < 12) return 'Good morning';
    if (h < 17) return 'Good afternoon';
    return 'Good evening';
  }

  ngOnInit(): void {
    this.loadDashboard();
  }

  loadDashboard(): void {
    this.loading.set(true);
    this.error.set('');

    this.apiService.get<ClinicianDashboardData>('/dashboard/clinician').subscribe({
      next: (result) => {
        this.data.set(result);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load dashboard data');
        this.notificationService.error('Error', 'Could not load dashboard data');
        this.loading.set(false);
      }
    });
  }

  formatDate(dt: string): string {
    return new Date(dt).toLocaleDateString('en-GB', {
      weekday: 'short', day: 'numeric', month: 'short'
    });
  }

  formatTime(dt: string): string {
    return new Date(dt).toLocaleTimeString('en-GB', {
      hour: '2-digit', minute: '2-digit'
    });
  }

  minutesUntil(dt: string): number {
    return Math.floor((new Date(dt).getTime() - Date.now()) / 60000);
  }

  timeUntilLabel(dt: string): string {
    const mins = this.minutesUntil(dt);
    if (mins < 0)    return 'In progress';
    if (mins < 60)   return `In ${mins}m`;
    const hrs = Math.floor(mins / 60);
    const rem = mins % 60;
    return rem > 0 ? `In ${hrs}h ${rem}m` : `In ${hrs}h`;
  }

  statusClass(status: string): string {
    const s = status.toLowerCase();
    if (s === 'confirmed')    return 'status-confirmed';
    if (s === 'pending')      return 'status-pending';
    if (s === 'completed')    return 'status-completed';
    if (s === 'cancelled')    return 'status-cancelled';
    if (s === 'didnotattend') return 'status-dna';
    return 'status-default';
  }

  isNow(apt: AppointmentSummary): boolean {
    const start = new Date(apt.startDateTime).getTime();
    const end   = new Date(apt.endDateTime).getTime();
    const now   = Date.now();
    return now >= start && now <= end;
  }

  typeIcon(type: string): string {
    switch (type.toLowerCase()) {
      case 'initialconsultation': return '🩺';
      case 'followup':            return '🔁';
      case 'emergency':           return '🚨';
      default:                    return '📋';
    }
  }

  typeLabel(type: string): string {
    switch (type.toLowerCase()) {
      case 'initialconsultation': return 'Initial';
      case 'followup':            return 'Follow-up';
      case 'emergency':           return 'Emergency';
      default:                    return type;
    }
  }
}
