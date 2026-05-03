import { Injectable } from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptor,
  HttpErrorResponse
} from '@angular/common/http';
import { Observable, throwError, BehaviorSubject } from 'rxjs';
import { catchError, switchMap, filter, take, finalize } from 'rxjs/operators';
import { AuthService } from '../services/auth.service';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  private isRefreshing = false;
  // Signals waiting requests when a token refresh completes.
  // null = refresh in progress; 'done' = refresh succeeded.
  private refreshDone$ = new BehaviorSubject<string | null>(null);

  // Endpoints that should never trigger a refresh loop on 401.
  private noRefreshEndpoints = [
    '/api/auth/login',
    '/api/auth/register',
    '/api/auth/forgot-password',
    '/api/auth/reset-password',
    '/api/auth/verify-mfa',
    '/api/auth/refresh-token',
    '/health'
  ];

  constructor(private authService: AuthService) {}

  intercept(request: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    // All requests must include credentials so the browser sends the HttpOnly
    // auth cookies automatically. No Authorization header is added — the JWT
    // middleware reads the token from the cookie on the backend.
    request = request.clone({ withCredentials: true });

    return next.handle(request).pipe(
      catchError((error: HttpErrorResponse) => {
        if (error.status === 401 && !this.isNoRefreshEndpoint(request.url)) {
          return this.handle401Error(request, next);
        }
        return throwError(() => error);
      })
    );
  }

  private isNoRefreshEndpoint(url: string): boolean {
    return this.noRefreshEndpoints.some(ep => url.includes(ep));
  }

  private handle401Error(
    request: HttpRequest<unknown>,
    next: HttpHandler
  ): Observable<HttpEvent<unknown>> {
    // If a refresh is already in flight, queue this request until it completes.
    if (this.isRefreshing) {
      return this.refreshDone$.pipe(
        filter(done => done !== null),
        take(1),
        switchMap(() => next.handle(request))
      );
    }

    this.isRefreshing = true;
    this.refreshDone$.next(null);

    // POST /auth/refresh-token with no body — the browser sends the
    // refresh_token HttpOnly cookie automatically.
    return this.authService.refreshToken().pipe(
      switchMap(() => {
        this.isRefreshing = false;
        this.refreshDone$.next('done');
        // Retry the original request; the new access_token cookie is now set.
        return next.handle(request);
      }),
      catchError(error => {
        this.isRefreshing = false;
        // Refresh failed (expired / revoked) — force the user back to login.
        this.authService.logout().subscribe();
        return throwError(() => error);
      }),
      finalize(() => {
        this.isRefreshing = false;
      })
    );
  }
}