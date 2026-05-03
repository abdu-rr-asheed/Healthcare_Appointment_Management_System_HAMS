import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface AppointmentCardData {
  id: string;
  dateTime: Date;
  endDateTime?: Date;
  clinicianName: string;
  clinicianSpecialty?: string;
  type: string;
  status: 'Scheduled' | 'Completed' | 'Cancelled' | 'NoShow' | 'Rescheduled';
  location?: string;
  notes?: string;
  canCancel?: boolean;
  canReschedule?: boolean;
}

@Component({
  selector: 'app-appointment-card',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="appointment-card" [ngClass]="'status-' + appointment.status.toLowerCase()">
      <div class="card-header">
        <div class="datetime">
          <span class="date">{{ formatDate(appointment.dateTime) }}</span>
          <span class="time">{{ formatTime(appointment.dateTime) }}</span>
          @if (appointment.endDateTime) {
            <span class="end-time">- {{ formatTime(appointment.endDateTime) }}</span>
          }
        </div>
        <span class="status-badge" [ngClass]="'badge-' + appointment.status.toLowerCase()">
          {{ appointment.status }}
        </span>
      </div>

      <div class="card-body">
        <h4 class="clinician-name">{{ appointment.clinicianName }}</h4>
        @if (appointment.clinicianSpecialty) {
          <p class="specialty">{{ appointment.clinicianSpecialty }}</p>
        }
        <p class="appointment-type">{{ appointment.type }}</p>
        @if (appointment.location) {
          <p class="location">{{ appointment.location }}</p>
        }
        @if (appointment.notes) {
          <p class="notes">{{ appointment.notes }}</p>
        }
      </div>

      @if (appointment.status === 'Scheduled') {
        <div class="card-actions">
          @if (appointment.canReschedule) {
            <button class="btn btn-secondary" (click)="onReschedule.emit(appointment)">
              Reschedule
            </button>
          }
          @if (appointment.canCancel) {
            <button class="btn btn-danger" (click)="onCancel.emit(appointment)">
              Cancel
            </button>
          }
          <button class="btn btn-primary" (click)="onViewDetails.emit(appointment)">
            View Details
          </button>
        </div>
      }
    </div>
  `,
  styles: [`
    .appointment-card {
      background: var(--surface-color, #fff);
      border-radius: 12px;
      padding: 16px;
      box-shadow: 0 2px 8px rgba(0,0,0,0.08);
      transition: transform 0.2s ease, box-shadow 0.2s ease;
    }
    .appointment-card:hover {
      transform: translateY(-2px);
      box-shadow: 0 4px 16px rgba(0,0,0,0.12);
    }
    .card-header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      margin-bottom: 12px;
    }
    .datetime {
      display: flex;
      flex-direction: column;
    }
    .date {
      font-weight: 600;
      font-size: 14px;
      color: #333;
    }
    .time, .end-time {
      font-size: 18px;
      font-weight: 700;
      color: #2563eb;
    }
    .status-badge {
      padding: 4px 12px;
      border-radius: 20px;
      font-size: 12px;
      font-weight: 600;
      text-transform: uppercase;
    }
    .badge-scheduled { background: #dbeafe; color: #1d4ed8; }
    .badge-completed { background: #d1fae5; color: #059669; }
    .badge-cancelled { background: #fee2e2; color: #dc2626; }
    .badge-noshow { background: #fef3c7; color: #d97706; }
    .badge-rescheduled { background: #e0e7ff; color: #4f46e5; }
    .card-body h4 { margin: 0 0 4px 0; font-size: 16px; }
    .specialty { color: #6b7280; font-size: 14px; margin: 0 0 8px 0; }
    .appointment-type { font-weight: 500; margin: 0 0 4px 0; }
    .location, .notes { color: #6b7280; font-size: 13px; margin: 0; }
    .card-actions {
      display: flex;
      gap: 8px;
      margin-top: 16px;
      padding-top: 16px;
      border-top: 1px solid #e5e7eb;
    }
    .btn {
      padding: 8px 16px;
      border-radius: 6px;
      font-size: 14px;
      font-weight: 500;
      cursor: pointer;
      border: none;
      transition: background 0.2s;
    }
    .btn-primary { background: #2563eb; color: white; }
    .btn-primary:hover { background: #1d4ed8; }
    .btn-secondary { background: #f3f4f6; color: #374151; }
    .btn-secondary:hover { background: #e5e7eb; }
    .btn-danger { background: #dc2626; color: white; }
    .btn-danger:hover { background: #b91c1c; }
  `]
})
export class AppointmentCardComponent {
  @Input() appointment!: AppointmentCardData;
  @Output() onCancel = new EventEmitter<AppointmentCardData>();
  @Output() onReschedule = new EventEmitter<AppointmentCardData>();
  @Output() onViewDetails = new EventEmitter<AppointmentCardData>();

  formatDate(date: Date): string {
    return new Date(date).toLocaleDateString('en-GB', {
      weekday: 'short',
      day: 'numeric',
      month: 'short'
    });
  }

  formatTime(date: Date): string {
    return new Date(date).toLocaleTimeString('en-GB', {
      hour: '2-digit',
      minute: '2-digit'
    });
  }
}