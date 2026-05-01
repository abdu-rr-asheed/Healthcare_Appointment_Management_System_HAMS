import { Component, inject, OnInit } from '@angular/core';
import { RouterOutlet, RouterLink } from '@angular/router';
import { AsyncPipe, JsonPipe } from '@angular/common';
import { AuthService } from './core/services/auth.service';
import { NotificationService } from './core/services/notification.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, AsyncPipe],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class AppComponent implements OnInit {
  authService = inject(AuthService);
  notificationService = inject(NotificationService);

  title = 'Healthcare Appointment Management System';
  isAuthenticated = false;
  currentUser = this.authService.currentUser$;

  ngOnInit(): void {
    this.authService.isAuthenticated$.subscribe(isAuth => {
      this.isAuthenticated = isAuth;
    });
  }

  logout(): void {
    this.authService.logout().subscribe({
      next: () => {
        this.notificationService.success('Success', 'You have been logged out successfully');
      },
      error: (error) => {
        this.notificationService.error('Error', 'Failed to logout');
      }
    });
  }
}