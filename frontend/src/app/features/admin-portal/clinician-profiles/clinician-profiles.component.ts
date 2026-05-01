import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import { NotificationService } from '../../../core/services/notification.service';

interface Clinician {
  id: string;
  userId: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  specialty: string;
  departmentId: string;
  departmentName: string;
  licenseNumber: string;
  qualifications: string[];
  status: string;
  startDate: string;
}

interface Department {
  id: string;
  name: string;
}

@Component({
  selector: 'app-clinician-profiles',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './clinician-profiles.component.html',
  styleUrl: './clinician-profiles.component.scss'
})
export class ClinicianProfilesComponent implements OnInit {
  private apiService = inject(ApiService);
  private notificationService = inject(NotificationService);
  private router = inject(Router);

  clinicians = signal<Clinician[]>([]);
  departments = signal<Department[]>([]);
  loading = signal<boolean>(false);
  showModal = signal<boolean>(false);
  showDetailsModal = signal<boolean>(false);
  modalMode = signal<'create' | 'edit'>('edit');
  selectedClinician = signal<Clinician | null>(null);

  filters = signal({
    search: '',
    specialty: '',
    department: '',
    status: ''
  });

  formData = signal({
    firstName: '',
    lastName: '',
    email: '',
    phoneNumber: '',
    specialty: '',
    departmentId: '',
    licenseNumber: '',
    qualifications: [] as string[],
    status: 'Active'
  });

  newQualification = signal('');

  specialties = ['General Practice', 'Cardiology', 'Dermatology', 'Orthopedics', 'Pediatrics', 'Psychiatry', 'Neurology', 'Oncology', 'Radiology', 'Surgery'];
  statuses = [
    { value: '', label: 'All Status' },
    { value: 'Active', label: 'Active' },
    { value: 'Inactive', label: 'Inactive' },
    { value: 'OnLeave', label: 'On Leave' }
  ];

  ngOnInit(): void {
    this.loadClinicians();
    this.loadDepartments();
  }

  loadClinicians(): void {
    this.loading.set(true);
    const params = new URLSearchParams();
    const f = this.filters();
    if (f.search) params.append('search', f.search);
    if (f.specialty) params.append('specialty', f.specialty);
    if (f.department) params.append('departmentId', f.department);
    if (f.status) params.append('status', f.status);
    this.apiService.get<Clinician[]>(`/clinicians?${params}`).subscribe({
      next: (data) => {
        this.clinicians.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.notificationService.error('Error', 'Failed to load clinicians');
      }
    });
  }

  loadDepartments(): void {
    this.apiService.get<Department[]>('/departments').subscribe({
      next: (data) => {
        this.departments.set(data);
      }
    });
  }

  applyFilters(): void {
    this.loadClinicians();
  }

  clearFilters(): void {
    this.filters.set({ search: '', specialty: '', department: '', status: '' });
    this.loadClinicians();
  }

  openEditModal(clinician: Clinician): void {
    this.modalMode.set('edit');
    this.selectedClinician.set(clinician);
    this.formData.set({
      firstName: clinician.firstName,
      lastName: clinician.lastName,
      email: clinician.email,
      phoneNumber: clinician.phoneNumber,
      specialty: clinician.specialty,
      departmentId: clinician.departmentId,
      licenseNumber: clinician.licenseNumber,
      qualifications: [...clinician.qualifications],
      status: clinician.status
    });
    this.showModal.set(true);
  }

  viewClinicianDetails(clinician: Clinician): void {
    this.selectedClinician.set(clinician);
    this.showDetailsModal.set(true);
  }

  closeModal(): void {
    this.showModal.set(false);
    this.selectedClinician.set(null);
    this.newQualification.set('');
  }

  closeDetailsModal(): void {
    this.showDetailsModal.set(false);
    this.selectedClinician.set(null);
  }

  addQualification(): void {
    const qual = this.newQualification().trim();
    if (qual) {
      this.formData.update(f => ({ ...f, qualifications: [...f.qualifications, qual] }));
      this.newQualification.set('');
    }
  }

  removeQualification(index: number): void {
    this.formData.update(f => ({ ...f, qualifications: f.qualifications.filter((_, i) => i !== index) }));
  }

  saveClinician(): void {
    const form = this.formData();
    if (!form.firstName || !form.lastName || !form.email || !form.specialty) {
      this.notificationService.warning('Warning', 'Please fill required fields');
      return;
    }
    this.loading.set(true);
    const clinicianId = this.selectedClinician()?.id;
    this.apiService.put(`/clinicians/${clinicianId}`, form).subscribe({
      next: () => {
        this.loading.set(false);
        this.notificationService.success('Success', 'Clinician updated successfully');
        this.closeModal();
        this.loadClinicians();
      },
      error: () => {
        this.loading.set(false);
        this.notificationService.error('Error', 'Failed to update clinician');
      }
    });
  }

  toggleClinicianStatus(clinician: Clinician): void {
    const newStatus = clinician.status === 'Active' ? 'Inactive' : 'Active';
    if (confirm(`Change status to ${newStatus}?`)) {
      this.apiService.put(`/clinicians/${clinician.id}`, { status: newStatus }).subscribe({
        next: () => {
          this.notificationService.success('Success', 'Status updated');
          this.loadClinicians();
        },
        error: () => {
          this.notificationService.error('Error', 'Failed to update status');
        }
      });
    }
  }

  viewAvailability(clinicianId: string): void {
    this.router.navigate(['/admin/clinicians', clinicianId, 'availability']);
  }

  getStatusClass(status: string): string {
    return status.toLowerCase().replace(' ', '-');
  }

  formatDate(dateString: string): string {
    return new Date(dateString).toLocaleDateString('en-GB', {
      day: 'numeric',
      month: 'short',
      year: 'numeric'
    });
  }
}
