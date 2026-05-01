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
    const isMfaVerified = this.authService.isMfaVerified();
    const isMfaEnabled = this.authService.isMfaEnabled();

    if (!isMfaEnabled) {
      return of(true);
    }

    if (isMfaVerified) {
      return of(true);
    }

    this.router.navigate(['/auth/mfa-verify'], {
      queryParams: { returnUrl: window.location.pathname }
    });
    return of(false);
  }
}