import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, of, throwError } from 'rxjs';
import { tap, catchError } from 'rxjs/operators';
import { ApiService } from './api.service';
import { STORAGE_KEYS } from '../../shared/constants';

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
  // accessToken and refreshToken are no longer in the response body —
  // they are delivered as HttpOnly cookies by the backend.
  accessToken?: string | null;
  refreshToken?: string | null;
  expiresAt?: string;
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

  // MFA verified state is tracked in memory only — never in any browser storage.
  // Tokens live in HttpOnly cookies (unreadable from JS), so we cannot inspect
  // the JWT directly. Having a valid session cookie implies MFA was completed,
  // because the backend never issues tokens without it (see AuthService.cs 2.1).
  private _mfaVerified = false;

  /** Synchronous snapshot of the current user — use currentUser$ for reactive bindings. */
  get currentUserValue(): User | null {
    return this.currentUserSubject.getValue();
  }

  /** Synchronous authenticated state — use isAuthenticated$ for reactive bindings. */
  get isAuthenticated(): boolean {
    return this.isAuthenticatedSubject.getValue();
  }

  /** True once MFA has been successfully verified in this browser session. */
  isMfaVerified(): boolean {
    return this._mfaVerified;
  }

  constructor(private apiService: ApiService) {
    this.checkAuthStatus();
  }

  private checkAuthStatus(): void {
    // Tokens are now HttpOnly cookies — JS cannot read them.
    // Restore the user profile from sessionStorage (written on login/refresh)
    // so the app can render immediately without a round-trip. The interceptor
    // will catch any 401 if the cookie has actually expired.
    const userStr = sessionStorage.getItem(STORAGE_KEYS.USER_DATA);
    if (userStr) {
      try {
        const user: User = JSON.parse(userStr);
        this.currentUserSubject.next(user);
        this.isAuthenticatedSubject.next(true);
        // A user found in sessionStorage obtained their JWT by completing the
        // full auth flow (including MFA if enabled). Mark as verified.
        this._mfaVerified = true;
      } catch {
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
        this._mfaVerified = true;
      }),
      catchError(error => {
        console.error('MFA verification error:', error);
        return throwError(() => error);
      })
    );
  }

  resendMfa(userId: string): Observable<any> {
    return this.apiService.post('/auth/resend-mfa', { userId }).pipe(
      catchError(error => {
        console.error('Resend MFA error:', error);
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
        // Store non-sensitive profile data in sessionStorage for fast startup.
        // Tokens are never stored here — they live in HttpOnly cookies only.
        sessionStorage.setItem(STORAGE_KEYS.USER_DATA, JSON.stringify(user));
      }),
      catchError(error => {
        this.clearAuth();
        return throwError(() => error);
      })
    );
  }

  refreshToken(): Observable<AuthResponse> {
    // No body needed — the browser sends the refresh_token HttpOnly cookie
    // automatically. The backend reads it, rotates both tokens, and sets
    // new cookies in the response.
    return this.apiService.post<AuthResponse>('/auth/refresh-token', {}).pipe(
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
    // Tokens arrive as HttpOnly cookies set by the backend — they are not
    // present in the response body and must NOT be stored in JS storage.
    sessionStorage.setItem(STORAGE_KEYS.USER_DATA, JSON.stringify(response.user));
    this.currentUserSubject.next(response.user);
    this.isAuthenticatedSubject.next(true);
  }

  private clearAuth(): void {
    sessionStorage.removeItem(STORAGE_KEYS.USER_DATA);
    this._mfaVerified = false;
    this.currentUserSubject.next(null);
    this.isAuthenticatedSubject.next(false);
  }

  isMfaEnabled(): boolean {
    const user = this.currentUserSubject.value;
    return user?.twoFactorEnabled ?? false;
  }

  setMfaVerified(verified: boolean): void {
    this._mfaVerified = verified;
  }
}