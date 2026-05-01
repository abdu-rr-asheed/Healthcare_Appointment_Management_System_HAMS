import { Component, inject } from '@angular/core';
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
export class DashboardComponent {
  authService = inject(AuthService);
  router = inject(Router);

  currentUser = this.authService.currentUser$;
  isAuthenticated = this.authService.isAuthenticated$;

  navigateToPortal(): void {
    const user = this.authService.currentUserValue;
    if (!user) return;

    switch (user.role) {
      case 'Patient':
        this.router.navigate(['/patient']);
        break;
      case 'Clinician':
        this.router.navigate(['/clinician']);
        break;
      case 'Administrator':
        this.router.navigate(['/admin']);
        break;
    }
  }
}