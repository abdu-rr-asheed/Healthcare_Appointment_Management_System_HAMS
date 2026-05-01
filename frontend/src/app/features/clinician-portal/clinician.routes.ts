import { Routes } from '@angular/router';
import { ClinicianLayoutComponent } from '../../layouts/clinician-layout/clinician-layout.component';

export const CLINICIAN_ROUTES: Routes = [
  {
    path: '',
    component: ClinicianLayoutComponent,
    children: [
      {
        path: '',
        redirectTo: 'dashboard',
        pathMatch: 'full'
      },
      {
        path: 'dashboard',
        loadComponent: () => import('./dashboard/clinician-dashboard.component').then(m => m.ClinicianDashboardComponent)
      },
      {
        path: 'schedule',
        loadComponent: () => import('./schedule/schedule.component').then(m => m.ScheduleComponent)
      },
      {
        path: 'availability',
        loadComponent: () => import('./availability/availability.component').then(m => m.AvailabilityComponent)
      },
      {
        path: 'clinical-notes/:appointmentId',
        loadComponent: () => import('./clinical-notes/clinical-notes.component').then(m => m.ClinicalNotesComponent)
      }
    ]
  }
];