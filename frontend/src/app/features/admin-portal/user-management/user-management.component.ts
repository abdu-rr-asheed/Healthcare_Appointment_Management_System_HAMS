import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';
import { NotificationService } from '../../../core/services/notification.service';

interface User {
  id: string;
  nhsNumber: string;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
  twoFactorEnabled: boolean;
  isActive: boolean;
  createdAt: string;
  lastLoginAt: string;
}

interface PaginatedResponse {
  items: User[];
  totalCount: number;
  currentPage: number;
  totalPages: number;
}

@Component({
  selector: 'app-user-management',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './user-management.component.html',
  styleUrl: './user-management.component.scss'
})
export class UserManagementComponent implements OnInit {
  private apiService = inject(ApiService);
  private notificationService = inject(NotificationService);

  users = signal<User[]>([]);
  loading = signal<boolean>(false);
  showModal = signal<boolean>(false);
  modalMode = signal<'create' | 'edit' | 'view'>('create');
  selectedUser = signal<User | null>(null);

  currentPage = signal<number>(1);
  pageSize = signal<number>(10);
  totalCount = signal<number>(0);
  totalPages = signal<number>(0);

  filters = signal({
    search: '',
    role: '',
    status: ''
  });

  formData = signal({
    firstName: '',
    lastName: '',
    email: '',
    phoneNumber: '',
    nhsNumber: '',
    role: 'Patient',
    password: '',
    isActive: true,
    twoFactorEnabled: false
  });

  roles = ['Patient', 'Clinician', 'Administrator'];
  statuses = [
    { value: '', label: 'All Status' },
    { value: 'active', label: 'Active' },
    { value: 'inactive', label: 'Inactive' }
  ];

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.loading.set(true);
    const params = new URLSearchParams();
    params.append('page', this.currentPage().toString());
    params.append('pageSize', this.pageSize().toString());
    const f = this.filters();
    if (f.search) params.append('search', f.search);
    if (f.role) params.append('role', f.role);
    if (f.status) params.append('status', f.status);
    this.apiService.get<PaginatedResponse>(`/admin/users?${params}`).subscribe({
      next: (data) => {
        this.users.set(data.items);
        this.totalCount.set(data.totalCount);
        this.totalPages.set(data.totalPages);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.notificationService.error('Error', 'Failed to load users');
      }
    });
  }

  applyFilters(): void {
    this.currentPage.set(1);
    this.loadUsers();
  }

  clearFilters(): void {
    this.filters.set({ search: '', role: '', status: '' });
    this.currentPage.set(1);
    this.loadUsers();
  }

  previousPage(): void {
    if (this.currentPage() > 1) {
      this.currentPage.update(p => p - 1);
      this.loadUsers();
    }
  }

  nextPage(): void {
    if (this.currentPage() < this.totalPages()) {
      this.currentPage.update(p => p + 1);
      this.loadUsers();
    }
  }

  openCreateModal(): void {
    this.modalMode.set('create');
    this.formData.set({
      firstName: '',
      lastName: '',
      email: '',
      phoneNumber: '',
      nhsNumber: '',
      role: 'Patient',
      password: '',
      isActive: true,
      twoFactorEnabled: false
    });
    this.showModal.set(true);
  }

  openEditModal(user: User): void {
    this.modalMode.set('edit');
    this.selectedUser.set(user);
    this.formData.set({
      firstName: user.firstName,
      lastName: user.lastName,
      email: user.email,
      phoneNumber: '',
      nhsNumber: user.nhsNumber,
      role: user.role,
      password: '',
      isActive: user.isActive,
      twoFactorEnabled: user.twoFactorEnabled
    });
    this.showModal.set(true);
  }

  viewUserDetails(user: User): void {
    this.modalMode.set('view');
    this.selectedUser.set(user);
    this.showModal.set(true);
  }

  closeModal(): void {
    this.showModal.set(false);
    this.selectedUser.set(null);
  }

  saveUser(): void {
    const form = this.formData();
    if (!form.firstName || !form.lastName || !form.email) {
      this.notificationService.warning('Warning', 'Please fill required fields');
      return;
    }
    if (this.modalMode() === 'create' && !form.password) {
      this.notificationService.warning('Warning', 'Password is required for new users');
      return;
    }
    this.loading.set(true);
    if (this.modalMode() === 'create') {
      this.apiService.post('/admin/users', form).subscribe({
        next: () => {
          this.loading.set(false);
          this.notificationService.success('Success', 'User created successfully');
          this.closeModal();
          this.loadUsers();
        },
        error: () => {
          this.loading.set(false);
          this.notificationService.error('Error', 'Failed to create user');
        }
      });
    } else {
      const userId = this.selectedUser()?.id;
      this.apiService.put(`/admin/users/${userId}`, {
        firstName: form.firstName,
        lastName: form.lastName,
        email: form.email,
        role: form.role,
        isActive: form.isActive
      }).subscribe({
        next: () => {
          this.loading.set(false);
          this.notificationService.success('Success', 'User updated successfully');
          this.closeModal();
          this.loadUsers();
        },
        error: () => {
          this.loading.set(false);
          this.notificationService.error('Error', 'Failed to update user');
        }
      });
    }
  }

  toggleUserStatus(user: User): void {
    const action = user.isActive ? 'deactivate' : 'activate';
    if (confirm(`Are you sure you want to ${action} this user?`)) {
      this.apiService.put(`/admin/users/${user.id}/status`, { isActive: !user.isActive }).subscribe({
        next: () => {
          this.notificationService.success('Success', `User ${action}d successfully`);
          this.loadUsers();
        },
        error: () => {
          this.notificationService.error('Error', `Failed to ${action} user`);
        }
      });
    }
  }

  formatDate(dateString: string): string {
    if (!dateString) return 'Never';
    return new Date(dateString).toLocaleDateString('en-GB', {
      day: 'numeric',
      month: 'short',
      year: 'numeric'
    });
  }

  getRoleClass(role: string): string {
    return role.toLowerCase();
  }

  getStatusClass(isActive: boolean): string {
    return isActive ? 'active' : 'inactive';
  }

  getPageNumbers(): number[] {
    const pages: number[] = [];
    const start = Math.max(1, this.currentPage() - 2);
    const end = Math.min(this.totalPages(), this.currentPage() + 2);
    for (let i = start; i <= end; i++) {
      pages.push(i);
    }
    return pages;
  }

  getEndIndex(): number {
    return Math.min(this.currentPage() * this.pageSize(), this.totalCount());
  }
}
