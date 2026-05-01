import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, of, throwError } from 'rxjs';
import { map, tap, catchError } from 'rxjs/operators';
import { ApiService } from './api.service';

export interface User {
  id: string;
  nhsNumber: string;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
  twoFactorEnabled: boolean;
  isActive: boolean;
  clinicianId?: string;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: User;
  requiresMfa: boolean;
  mfaUserId?: string;
}

export interface LoginRequest {
  nhsNumber: string;
  password: string;
}

export interface RegisterRequest {
  nhsNumber: string;
  email: string;
  phoneNumber: string;
  password: string;
  confirmPassword: string;
  dateOfBirth: string;
  firstName: string;
  lastName: string;
  address: string;
  city: string;
  postcode: string;
  mfaEnabled: boolean;
  smsOptIn: boolean;
  emergencyContactName?: string;
  emergencyContactPhone?: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private currentUserSubject = new BehaviorSubject<User | null>(null);
  public currentUser$ = this.currentUserSubject.asObservable();
  private isAuthenticatedSubject = new BehaviorSubject<boolean>(false);
  public isAuthenticated$ = this.isAuthenticatedSubject.asObservable();

  constructor(private apiService: ApiService) {
    this.checkAuthStatus();
  }

  private checkAuthStatus(): void {
    const token = localStorage.getItem('access_token');
    const userStr = localStorage.getItem('current_user');
    
    if (token && userStr) {
      try {
        const user = JSON.parse(userStr);
        this.currentUserSubject.next(user);
        this.isAuthenticatedSubject.next(true);
      } catch (e) {
        this.clearAuth();
      }
    }
  }

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.apiService.post<AuthResponse>('/auth/login', request).pipe(
      tap(response => {
        if (!response.requiresMfa) {
          this.setAuthData(response);
        }
      }),
      catchError(error => {
        console.error('Login error:', error);
        this.clearAuth();
        return throwError(() => error);
      })
    );
  }

  register(request: RegisterRequest): Observable<AuthResponse> {
    return this.apiService.post<AuthResponse>('/auth/register', request).pipe(
      tap(response => {
        if (!response.requiresMfa) {
          this.setAuthData(response);
        }
      }),
      catchError(error => {
        console.error('Registration error:', error);
        return throwError(() => error);
      })
    );
  }

  verifyMfa(userId: string, code: string): Observable<AuthResponse> {
    return this.apiService.post<AuthResponse>('/auth/verify-mfa', { userId, code }).pipe(
      tap(response => {
        this.setAuthData(response);
      }),
      catchError(error => {
        console.error('MFA verification error:', error);
        return throwError(() => error);
      })
    );
  }

  logout(): Observable<any> {
    return this.apiService.post('/auth/logout', {}).pipe(
      tap(() => {
        this.clearAuth();
      }),
      catchError(error => {
        console.error('Logout error:', error);
        this.clearAuth();
        return of(null);
      })
    );
  }

  getCurrentUser(): Observable<User> {
    return this.apiService.get<User>('/auth/me').pipe(
      tap(user => {
        this.currentUserSubject.next(user);
        localStorage.setItem('current_user', JSON.stringify(user));
      }),
      catchError(error => {
        this.clearAuth();
        return throwError(() => error);
      })
    );
  }

  getToken(): string | null {
    return localStorage.getItem('access_token');
  }

  getRefreshToken(): string | null {
    return localStorage.getItem('refresh_token');
  }

  refreshToken(): Observable<AuthResponse> {
    const refreshToken = this.getRefreshToken();
    if (!refreshToken) {
      return throwError(() => new Error('No refresh token available'));
    }

    return this.apiService.post<AuthResponse>('/auth/refresh-token', { refreshToken }).pipe(
      tap(response => {
        this.setAuthData(response);
      }),
      catchError(error => {
        this.clearAuth();
        return throwError(() => error);
      })
    );
  }

  getUserRole(): string | null {
    const user = this.currentUserSubject.value;
    return user ? user.role : null;
  }

  private setAuthData(response: AuthResponse): void {
    localStorage.setItem('access_token', response.accessToken);
    localStorage.setItem('refresh_token', response.refreshToken);
    localStorage.setItem('current_user', JSON.stringify(response.user));
    
    this.currentUserSubject.next(response.user);
    this.isAuthenticatedSubject.next(true);
  }

  private clearAuth(): void {
    localStorage.removeItem('access_token');
    localStorage.removeItem('refresh_token');
    localStorage.removeItem('current_user');
    
    this.currentUserSubject.next(null);
    this.isAuthenticatedSubject.next(false);
  }

  get currentUserValue(): User | null {
    return this.currentUserSubject.value;
  }

  get isAuthenticated(): boolean {
    return this.isAuthenticatedSubject.value;
  }

  isMfaEnabled(): boolean {
    const user = this.currentUserSubject.value;
    return user?.twoFactorEnabled ?? false;
  }

  isMfaVerified(): boolean {
    return localStorage.getItem('mfa_verified') === 'true';
  }

  setMfaVerified(verified: boolean): void {
    localStorage.setItem('mfa_verified', verified.toString());
  }
}