import { Component, inject, OnInit, signal } from '@angular/core';
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
  patientName: string;
  patientNhsNumber: string;
  clinicianName: string;
  departmentName: string;
  status: string;
}

interface ClinicalNote {
  id: string;
  appointmentId: string;
  clinicianId: string;
  clinicianName: string;
  content: string;
  consultationType: string;
  findings: string;
  recommendations: string;
  isPrivate: boolean;
  createdAt: string;
  updatedAt: string;
  syncedToEhr: boolean;
  syncedAt: string;
}

@Component({
  selector: 'app-clinical-notes',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './clinical-notes.component.html',
  styleUrl: './clinical-notes.component.scss'
})
export class ClinicalNotesComponent implements OnInit {
  private apiService = inject(ApiService);
  private notificationService = inject(NotificationService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  appointment = signal<Appointment | null>(null);
  notes = signal<ClinicalNote[]>([]);
  loading = signal<boolean>(false);
  saving = signal<boolean>(false);

  appointmentId = signal<string>('');

  // Plain mutable object — [(ngModel)] on signal().property never writes back to the signal.
  formData = {
    content: '',
    consultationType: 'InitialConsultation',
    findings: '',
    recommendations: '',
    isPrivate: false
  };

  consultationTypes = [
    'InitialConsultation',
    'FollowUp',
    'Emergency',
    'Review'
  ];

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('appointmentId');
    if (id) {
      this.appointmentId.set(id);
      this.loadAppointment();
      this.loadNotes();
    } else {
      this.router.navigate(['/clinician/schedule']);
    }
  }

  loadAppointment(): void {
    this.apiService.get<Appointment>(`/appointments/${this.appointmentId()}`).subscribe({
      next: (data) => {
        this.appointment.set(data);
      }
    });
  }

  loadNotes(): void {
    this.loading.set(true);
    this.apiService.get<ClinicalNote[]>(`/appointments/${this.appointmentId()}/clinical-notes`).subscribe({
      next: (data) => {
        this.notes.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
      }
    });
  }

  saveNote(): void {
    const appointmentId = this.appointmentId();
    if (!appointmentId) return;

    this.saving.set(true);
    
    this.apiService.post<ClinicalNote>(
      `/appointments/${appointmentId}/clinical-notes`,
      this.formData
    ).subscribe({
      next: (note) => {
        this.notes.update(current => [note, ...current]);
        this.formData = {
          content: '',
          consultationType: 'InitialConsultation',
          findings: '',
          recommendations: '',
          isPrivate: false
        };
        this.saving.set(false);
        this.notificationService.success('Success', 'Clinical note saved');
      },
      error: () => {
        this.saving.set(false);
        this.notificationService.error('Error', 'Failed to save note');
      }
    });
  }

  syncToEhr(noteId: string): void {
    this.apiService.post(`/appointments/${this.appointmentId()}/clinical-notes/${noteId}/sync-to-ehr`, {}).subscribe({
      next: () => {
        this.loadNotes();
        this.notificationService.success('Success', 'Note synced to EHR');
      },
      error: () => {
        this.notificationService.error('Error', 'Failed to sync to EHR');
      }
    });
  }

  deleteNote(noteId: string): void {
    if (!confirm('Are you sure you want to delete this note?')) return;

    this.notificationService.info('Info', 'Note deletion not implemented in this version');
  }

  goBack(): void {
    this.router.navigate(['/clinician/schedule']);
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
}