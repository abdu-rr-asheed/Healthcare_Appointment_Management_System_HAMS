import { Injectable, inject } from '@angular/core';
import { 
  CanActivate, 
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