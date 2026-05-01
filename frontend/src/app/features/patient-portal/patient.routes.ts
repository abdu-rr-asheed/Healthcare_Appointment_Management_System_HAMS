import { Routes } from '@angular/router';
import { PatientLayoutComponent } from '../../layouts/patient-layout/patient-layout.component';

export const PATIENT_ROUTES: Routes = [
  {
    path: '',
    component: PatientLayoutComponent,
    children: [
      {
        path: '',
        redirectTo: 'dashboard',
        pathMatch: 'full'
      },
      {
        path: 'dashboard',
        loadComponent: () => import('./dashboard/patient-dashboard.component').then(m => m.PatientDashboardComponent)
      },
      {
        path: 'booking',
        loadComponent: () => import('./booking/booking.component').then(m => m.BookingComponent)
      },
      {
        path: 'history',
        loadComponent: () => import('./history/appointment-history.component').then(m => m.AppointmentHistoryComponent)
      },
      {
        path: 'reschedule/:id',
        loadComponent: () => import('./reschedule/reschedule.component').then(m => m.RescheduleComponent)
      },
      {
        path: 'cancel/:id',
        loadComponent: () => import('./cancel/cancel.component').then(m => m.CancelComponent)
      },
      {
        path: 'profile',
        loadComponent: () => import('./profile/profile.component').then(m => m.ProfileComponent)
      }
    ]
  }
];