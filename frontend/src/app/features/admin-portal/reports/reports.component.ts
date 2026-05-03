import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';
import { NotificationService } from '../../../core/services/notification.service';

interface ReportType {
  id: string;
  name: string;
  description: string;
}

interface GenerateReportRequest {
  reportType: string;
  startDate: string;
  endDate: string;
  format: string;
}

interface ReportData {
  summary: {
    totalBookings: number;
    totalCancellations: number;
    totalDna: number;
    averageUtilisation: number;
  };
}

@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './reports.component.html',
  styleUrl: './reports.component.scss'
})
export class ReportsComponent implements OnInit {
  private apiService = inject(ApiService);
  private notificationService = inject(NotificationService);

  reportTypes = signal<ReportType[]>([]);
  reportData = signal<ReportData | null>(null);
  loading = signal<boolean>(false);
  generating = signal<boolean>(false);
  downloadUrl = signal<string>('');

  // Plain mutable object — [(ngModel)] on signal().property never writes back to the signal.
  formData = {
    reportType: 'BookingSummary',
    startDate: new Date(Date.now() - 30 * 24 * 60 * 60 * 1000).toISOString().split('T')[0],
    endDate: new Date().toISOString().split('T')[0],
    format: 'CSV'
  };

  ngOnInit(): void {
    this.loadReportTypes();
  }

  loadReportTypes(): void {
    this.apiService.get<ReportType[]>('/admin/reports/types').subscribe({
      next: (data) => {
        this.reportTypes.set(data);
      }
    });
  }

  generateReport(): void {
    this.generating.set(true);
    this.reportData.set(null);
    this.downloadUrl.set('');

    const request: GenerateReportRequest = {
      reportType: this.formData.reportType,
      startDate: this.formData.startDate,
      endDate: this.formData.endDate,
      format: this.formData.format
    };

    this.apiService.post<{ reportId: string; downloadUrl: string }>('/admin/reports/generate', request).subscribe({
      next: (response) => {
        this.generating.set(false);
        this.downloadUrl.set(`/admin/reports/${response.reportId}/download?format=${this.formData.format}`);
        
        this.apiService.get<ReportData>(`/admin/reports/${response.reportId}`).subscribe({
          next: (data) => {
            this.reportData.set(data);
          }
        });
        
        this.notificationService.success('Success', 'Report generated successfully');
      },
      error: () => {
        this.generating.set(false);
        this.notificationService.error('Error', 'Failed to generate report');
      }
    });
  }

  downloadReport(): void {
    const url = this.downloadUrl();
    if (url) {
      window.open(url, '_blank');
    }
  }
}