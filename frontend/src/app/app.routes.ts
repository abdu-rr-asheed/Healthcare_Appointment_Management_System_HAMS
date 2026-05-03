import { Routes } from '@angular/router';
import { authGuardFn } from './core/guards/auth.guard';
import { roleGuardFn } from './core/guards/role.guard';
import { MfaGuard } from './core/guards/mfa.guard';

export const routes: Routes = [
  {
    path: '',
    redirectTo: '/dashboard',
    pathMatch: 'full'
  },
  {
    path: 'auth',
    loadChildren: () => import('./features/auth/auth.routes').then(m => m.AUTH_ROUTES)
  },
  {
    path: 'patient',
    canActivate: [authGuardFn, roleGuardFn, MfaGuard],
    data: { roles: ['Patient'] },
    loadChildren: () => import('./features/patient-portal/patient.routes').then(m => m.PATIENT_ROUTES)
  },
  {
    path: 'clinician',
    canActivate: [authGuardFn, roleGuardFn, MfaGuard],
    data: { roles: ['Clinician'] },
    loadChildren: () => import('./features/clinician-portal/clinician.routes').then(m => m.CLINICIAN_ROUTES)
  },
  {
    path: 'admin',
    canActivate: [authGuardFn, roleGuardFn, MfaGuard],
    data: { roles: ['Administrator'] },
    loadChildren: () => import('./features/admin-portal/admin.routes').then(m => m.ADMIN_ROUTES)
  },
  {
    path: 'dashboard',
    canActivate: [authGuardFn],
    loadComponent: () => import('./features/shared/dashboard/dashboard.component').then(m => m.DashboardComponent)
  },
  {
    path: 'unauthorized',
    loadComponent: () => import('./features/shared/unauthorized/unauthorized.component').then(m => m.UnauthorizedComponent)
  },
  {
    path: '**',
    loadComponent: () => import('./features/shared/not-found/not-found.component').then(m => m.NotFoundComponent)
  }
];
