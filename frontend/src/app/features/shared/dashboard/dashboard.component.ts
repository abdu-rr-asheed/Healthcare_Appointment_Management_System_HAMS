import { Component, inject, OnInit } from '@angular/core';
import { AsyncPipe } from '@angular/common';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [AsyncPipe],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {
  authService = inject(AuthService);
  router = inject(Router);

  currentUser = this.authService.currentUser$;
  isAuthenticated = this.authService.isAuthenticated$;

  ngOnInit(): void {
    // Immediately redirect to the role-specific portal.
    // The generic /dashboard route is just a router-level fallback;
    // authenticated users should never linger on this page.
    this.navigateToPortal();
  }

  navigateToPortal(): void {
    const user = this.authService.currentUserValue;
    if (!user) return;

    switch (user.role) {
      case 'Patient':
        this.router.navigate(['/patient/dashboard']);
        break;
      case 'Clinician':
        this.router.navigate(['/clinician/dashboard']);
        break;
      case 'Administrator':
        this.router.navigate(['/admin/dashboard']);
        break;
      default:
        this.router.navigate(['/auth/login']);
    }
  }
}