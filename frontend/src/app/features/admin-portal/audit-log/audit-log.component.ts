import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';
import { NotificationService } from '../../../core/services/notification.service';

interface AuditLogEntry {
  id: string;
  timestamp: string;
  userId: string;
  userName: string;
  userRole: string;
  action: string;
  resourceType: string;
  resourceId: string;
  ipAddress: string;
  outcome: string;
}

interface PaginatedResponse {
  items: AuditLogEntry[];
  totalCount: number;
  currentPage: number;
  totalPages: number;
}

@Component({
  selector: 'app-audit-log',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './audit-log.component.html',
  styleUrl: './audit-log.component.scss'
})
export class AuditLogComponent implements OnInit {
  private apiService = inject(ApiService);
  private notificationService = inject(NotificationService);

  entries = signal<AuditLogEntry[]>([]);
  loading = signal<boolean>(false);
  exporting = signal<boolean>(false);

  currentPage = signal<number>(1);
  pageSize = signal<number>(20);
  totalCount = signal<number>(0);
  totalPages = signal<number>(0);

  // Plain mutable object — [(ngModel)] on signal().property never writes back to the signal.
  filters = {
    userId: '',
    action: '',
    startDate: '',
    endDate: ''
  };

  actionTypes = [
    'UserLogin',
    'UserLogout',
    'AppointmentBooked',
    'AppointmentCancelled',
    'AppointmentRescheduled',
    'PatientDataAccessed',
    'ClinicalNotesAdded',
    'EhrDataAccessed',
    'ReportGenerated'
  ];

  ngOnInit(): void {
    this.loadAuditLog();
  }

  loadAuditLog(): void {
    this.loading.set(true);
    
    const params = new URLSearchParams();
    params.append('page', this.currentPage().toString());
    params.append('pageSize', this.pageSize().toString());
    
    if (this.filters.userId) params.append('userId', this.filters.userId);
    if (this.filters.action) params.append('action', this.filters.action);
    if (this.filters.startDate) params.append('startDate', this.filters.startDate);
    if (this.filters.endDate) params.append('endDate', this.filters.endDate);

    this.apiService.get<PaginatedResponse>(`/admin/audit-log?${params}`).subscribe({
      next: (data) => {
        this.entries.set(data.items);
        this.totalCount.set(data.totalCount);
        this.totalPages.set(data.totalPages);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
      }
    });
  }

  applyFilters(): void {
    this.currentPage.set(1);
    this.loadAuditLog();
  }

  clearFilters(): void {
    this.filters = {
      userId: '',
      action: '',
      startDate: '',
      endDate: ''
    };
    this.currentPage.set(1);
    this.loadAuditLog();
  }

  previousPage(): void {
    if (this.currentPage() > 1) {
      this.currentPage.update(p => p - 1);
      this.loadAuditLog();
    }
  }

  nextPage(): void {
    if (this.currentPage() < this.totalPages()) {
      this.currentPage.update(p => p + 1);
      this.loadAuditLog();
    }
  }

  exportLog(): void {
    this.exporting.set(true);

    const params = new URLSearchParams();
    if (this.filters.userId)    params.set('userId',    this.filters.userId);
    if (this.filters.action)    params.set('action',    this.filters.action);
    if (this.filters.startDate) params.set('startDate', this.filters.startDate);
    if (this.filters.endDate)   params.set('endDate',   this.filters.endDate);

    const endpoint = `/admin/audit-log/export${params.toString() ? '?' + params.toString() : ''}`;

    this.apiService.getBlob(endpoint).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `audit-log-${new Date().toISOString().slice(0, 10)}.csv`;
        a.click();
        URL.revokeObjectURL(url);
        this.exporting.set(false);
      },
      error: () => {
        this.notificationService.error('Error', 'Failed to export audit log');
        this.exporting.set(false);
      }
    });
  }

  formatDate(dateString: string): string {
    return new Date(dateString).toLocaleString('en-GB', {
      day: 'numeric',
      month: 'short',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  getOutcomeClass(outcome: string): string {
    return outcome.toLowerCase();
  }
}