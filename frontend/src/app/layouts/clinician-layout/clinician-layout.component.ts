import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive, RouterOutlet, Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { NotificationService } from '../../core/services/notification.service';

@Component({
  selector: 'app-clinician-layout',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="layout-container">
      <header class="layout-header">
        <div class="header-content">
          <div class="logo">
            <h1>HAMS</h1>
            <span class="subtitle">Clinician Portal</span>
          </div>
          
          <nav class="main-nav">
            <a routerLink="/clinician/dashboard" routerLinkActive="active" class="nav-link">Dashboard</a>
            <a routerLink="/clinician/schedule" routerLinkActive="active" class="nav-link">Schedule</a>
            <a routerLink="/clinician/availability" routerLinkActive="active" class="nav-link">Availability</a>
            <a routerLink="/clinician/clinical-notes" routerLinkActive="active" class="nav-link">Clinical Notes</a>
          </nav>
          
          <div class="user-menu">
            <span class="user-name">{{ userName }}</span>
            <button (click)="logout()" class="logout-btn">Logout</button>
          </div>
        </div>
      </header>
      
      <main class="layout-content">
        <router-outlet></router-outlet>
      </main>
      
      <footer class="layout-footer">
        <p>&copy; 2024 Healthcare Appointment Management System</p>
      </footer>
    </div>
  `,
  styles: [`
    .layout-container { min-height: 100vh; display: flex; flex-direction: column; }
    .layout-header { background: #fff; border-bottom: 1px solid #e5e7eb; padding: 0 24px; position: sticky; top: 0; z-index: 100; }
    .header-content { max-width: 1400px; margin: 0 auto; display: flex; align-items: center; height: 64px; gap: 32px; }
    .logo h1 { margin: 0; font-size: 24px; color: #2563eb; }
    .logo .subtitle { font-size: 12px; color: #6b7280; margin-left: 8px; }
    .main-nav { display: flex; gap: 8px; flex: 1; }
    .nav-link { padding: 8px 16px; border-radius: 6px; text-decoration: none; color: #374151; font-weight: 500; transition: all 0.2s; }
    .nav-link:hover { background: #f3f4f6; }
    .nav-link.active { background: #eff6ff; color: #2563eb; }
    .user-menu { display: flex; align-items: center; gap: 16px; }
    .user-name { font-weight: 500; }
    .logout-btn { padding: 8px 16px; background: #fee2e2; color: #dc2626; border: none; border-radius: 6px; cursor: pointer; font-weight: 500; }
    .logout-btn:hover { background: #fecaca; }
    .layout-content { flex: 1; padding: 24px; max-width: 1400px; margin: 0 auto; width: 100%; }
    .layout-footer { background: #f8fafc; padding: 16px; text-align: center; color: #6b7280; font-size: 14px; }
  `]
})
export class ClinicianLayoutComponent {
  private authService = inject(AuthService);
  private notificationService = inject(NotificationService);
  private router = inject(Router);

  get userName(): string {
    const user = this.authService.currentUserValue;
    return user ? `Dr. ${user.firstName} ${user.lastName}` : 'Clinician';
  }

  logout(): void {
    this.authService.logout().subscribe({
      next: () => {
        this.notificationService.success('Success', 'Logged out');
        this.router.navigate(['/auth/login']);
      },
      error: () => {
        this.notificationService.error('Error', 'Logout failed');
        this.router.navigate(['/auth/login']);
      }
    });
  }
}