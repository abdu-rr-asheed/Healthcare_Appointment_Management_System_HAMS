import { Injectable } from '@angular/core';
import {
  CanActivate,
  CanActivateFn,
  Router,
  ActivatedRouteSnapshot,
  RouterStateSnapshot
} from '@angular/router';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';

@Injectable({
  providedIn: 'root'
})
export class RoleGuard implements CanActivate {
  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  canActivate(
    route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot
  ): boolean {
    const requiredRoles = route.data['roles'] as string[];
    const user = this.authService.currentUserValue;

    if (!user) {
      this.router.navigate(['/auth/login'], {
        queryParams: { returnUrl: state.url }
      });
      return false;
    }

    if (!requiredRoles || requiredRoles.length === 0) {
      return true;
    }

    const hasRequiredRole = requiredRoles.includes(user.role);

    if (!hasRequiredRole) {
      this.router.navigate(['/unauthorized']);
      return false;
    }

    return true;
  }
}

export const roleGuardFn: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  
  const requiredRoles = route.data?.['roles'] as string[] | undefined;
  const user = authService.currentUserValue;
  
  if (!user) {
    router.navigate(['/auth/login'], {
      queryParams: { returnUrl: state.url }
    });
    return false;
  }

  if (!requiredRoles || requiredRoles.length === 0) {
    return true;
  }

  const hasRequiredRole = requiredRoles.includes(user.role);

  if (!hasRequiredRole) {
    router.navigate(['/unauthorized']);
    return false;
  }

  return true;
};
