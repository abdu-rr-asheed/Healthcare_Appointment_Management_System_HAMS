import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import { NotificationService } from '../../../core/services/notification.service';
import { AuthService } from '../../../core/services/auth.service';

interface UserStats {
  totalUsers: number;
  patients: number;
  clinicians: number;
  admins: number;
  activeUsers: number;
  newUsersThisWeek: number;
}

interface AppointmentStats {
  total: number;
  today: number;
  thisWeek: number;
  pending: number;
  confirmed: number;
  completed: number;
  cancelled: number;
  didNotAttend: number;
  typeBreakdown: { initialConsultation: number; followUp: number; emergency: number };
}

interface DepartmentStats {
  totalDepartments: number;
  topDepartments: { name: string; count: number }[];
}

interface ActivityEntry {
  id: string;
  action: string;
  userName: string;
  userRole: string;
  resourceType: string;
  timestamp: string;
}

interface AdminDashboardData {
  userStats: UserStats;
  appointmentStats: AppointmentStats;
  departmentStats: DepartmentStats;
  recentActivity: ActivityEntry[];
  systemStatus: { databaseConnected: boolean; generatedAt: string };
}

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './admin-dashboard.component.html',
  styleUrl: './admin-dashboard.component.scss'
})
export class AdminDashboardComponent implements OnInit {
  private apiService = inject(ApiService);
  private notificationService = inject(NotificationService);
  private authService = inject(AuthService);

  data = signal<AdminDashboardData | null>(null);
  loading = signal<boolean>(true);
  error = signal<string>('');

  get adminName(): string {
    const u = this.authService.currentUserValue;
    return u ? `${u.firstName} ${u.lastName}` : 'Administrator';
  }

  ngOnInit(): void {
    this.loadDashboard();
  }

  loadDashboard(): void {
    this.loading.set(true);
    this.error.set('');

    this.apiService.get<AdminDashboardData>('/dashboard/admin').subscribe({
      next: (result) => {
        this.data.set(result);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load dashboard data');
        this.notificationService.error('Error', 'Could not load dashboard metrics');
        this.loading.set(false);
      }
    });
  }

  /** Returns a bar width % capped at 100 for visualising relative counts. */
  barPercent(value: number, total: number): number {
    if (!total) return 0;
    return Math.min(100, Math.round((value / total) * 100));
  }

  formatTimestamp(ts: string): string {
    return new Date(ts).toLocaleString('en-GB', {
      day: 'numeric', month: 'short', year: 'numeric',
      hour: '2-digit', minute: '2-digit'
    });
  }

  actionLabel(action: string): string {
    return action.replace(/_/g, ' ').toLowerCase()
      .replace(/\b\w/g, c => c.toUpperCase());
  }

  actionClass(action: string): string {
    const a = action.toUpperCase();
    if (a.includes('CREATE') || a.includes('REGISTER')) return 'action-create';
    if (a.includes('DELETE') || a.includes('CANCEL'))   return 'action-delete';
    if (a.includes('UPDATE') || a.includes('EDIT'))     return 'action-update';
    if (a.includes('LOGIN')  || a.includes('LOGOUT'))   return 'action-auth';
    return 'action-default';
  }
}
