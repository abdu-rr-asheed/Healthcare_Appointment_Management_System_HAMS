import { Injectable, inject } from '@angular/core';
import {
  CanActivate,
  CanActivateFn,
  ActivatedRouteSnapshot,
  RouterStateSnapshot,
  Router
} from '@angular/router';
import { Observable, of } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { AuthService } from '../services/auth.service';

@Injectable({
  providedIn: 'root'
})
export class MfaGuard implements CanActivate {
  private authService = inject(AuthService);
  private router = inject(Router);

  canActivate(
    route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot
  ): Observable<boolean> {
    const requireMfa = route.data['requireMfa'] ?? true;

    if (!requireMfa) {
      return of(true);
    }

    return this.checkMfaStatus();
  }

  private checkMfaStatus(): Observable<boolean> {
    const isMfaEnabled  = this.authService.isMfaEnabled();
    const isMfaVerified = this.authService.isMfaVerified();

    // If MFA is not enabled for this user, no additional step is needed.
    if (!isMfaEnabled) {
      return of(true);
    }

    // MFA is enabled and has been verified in this session (in-memory flag set
    // by AuthService after verifyMfa() succeeds, or restored from sessionStorage
    // when a previously-authenticated user reloads the app).
    if (isMfaVerified) {
      return of(true);
    }

    this.router.navigate(['/auth/mfa'], {
      queryParams: { returnUrl: window.location.pathname }
    });
    return of(false);
  }
}

/**
 * Applied to the /auth/mfa route itself.
 * Redirects already-authenticated and MFA-verified users to their portal,
 * so they cannot revisit the MFA page after completing the auth flow.
 */
export const mfaRouteGuardFn: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAuthenticated && authService.isMfaVerified()) {
    const user = authService.currentUserValue;
    if (user) {
      switch (user.role) {
        case 'Patient':      router.navigate(['/patient/dashboard']);   break;
        case 'Clinician':    router.navigate(['/clinician/dashboard']); break;
        case 'Administrator': router.navigate(['/admin/dashboard']);    break;
        default:             router.navigate(['/dashboard']);
      }
    }
    return false;
  }
  return true;
};