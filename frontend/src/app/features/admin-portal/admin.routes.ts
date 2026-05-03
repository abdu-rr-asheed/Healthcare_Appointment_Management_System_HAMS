import { Routes } from '@angular/router';
import { AdminLayoutComponent } from '../../layouts/admin-layout/admin-layout.component';

export const ADMIN_ROUTES: Routes = [
  {
    path: '',
    component: AdminLayoutComponent,
    children: [
      {
        path: '',
        redirectTo: 'dashboard',
        pathMatch: 'full'
      },
      {
        path: 'dashboard',
        loadComponent: () => import('./dashboard/admin-dashboard.component').then(m => m.AdminDashboardComponent)
      },
      {
        path: 'reports',
        loadComponent: () => import('./reports/reports.component').then(m => m.ReportsComponent)
      },
      {
        path: 'audit-log',
        loadComponent: () => import('./audit-log/audit-log.component').then(m => m.AuditLogComponent)
      },
      {
        path: 'users',
        loadComponent: () => import('./user-management/user-management.component').then(m => m.UserManagementComponent)
      },
      {
        path: 'clinicians',
        loadComponent: () => import('./clinician-profiles/clinician-profiles.component').then(m => m.ClinicianProfilesComponent)
      },
      {
        path: 'clinicians/:id/availability',
        loadComponent: () => import('./clinician-availability/clinician-availability.component').then(m => m.ClinicianAvailabilityAdminComponent)
      }
    ]
  }
];