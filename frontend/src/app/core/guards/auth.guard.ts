import { Injectable } from '@angular/core';
import {
  Router,
  ActivatedRouteSnapshot,
  RouterStateSnapshot,
  CanActivateFn
} from '@angular/router';
import { Observable, of } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';

@Injectable({
  providedIn: 'root'
})
export class AuthGuard {
  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  canActivate(
    route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot
  ): Observable<boolean> {
    // isAuthenticated$ is a BehaviorSubject — always truthy as an object, so a
    // ternary `isAuthenticated$ ? ... : ...` NEVER reaches the redirect branch.
    // Pipe the observable directly instead.
    return this.authService.isAuthenticated$.pipe(
      map(isAuthenticated => {
        if (isAuthenticated) {
          return true;
        }
        this.router.navigate(['/auth/login'], {
          queryParams: { returnUrl: state.url }
        });
        return false;
      }),
      catchError(() => {
        this.router.navigate(['/auth/login']);
        return of(false);
      })
    );
  }
}

export const authGuardFn: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  
  if (authService.isAuthenticated) {
    return true;
  }

  router.navigate(['/auth/login'], {
    queryParams: { returnUrl: state.url }
  });
  return false;
};