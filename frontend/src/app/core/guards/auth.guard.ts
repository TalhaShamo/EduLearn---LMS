import { Injectable } from '@angular/core';
import { CanActivate, ActivatedRouteSnapshot, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

@Injectable({ providedIn: 'root' })
export class AuthGuard implements CanActivate {
  constructor(private auth: AuthService, private router: Router) {}

  canActivate(): boolean {
    if (this.auth.isLoggedIn) return true;
    this.router.navigate(['/auth/login']);
    return false;
  }
}

@Injectable({ providedIn: 'root' })
export class RoleGuard implements CanActivate {
  constructor(private auth: AuthService, private router: Router) {}

  canActivate(route: ActivatedRouteSnapshot): boolean {
    const requiredRoles: string[] = route.data['roles'] ?? [];
    const userRole = this.auth.userRole;

    if (!this.auth.isLoggedIn) {
      this.router.navigate(['/auth/login']);
      return false;
    }

    if (requiredRoles.length && !requiredRoles.includes(userRole ?? '')) {
      this.router.navigate(['/']);
      return false;
    }

    return true;
  }
}

@Injectable({ providedIn: 'root' })
export class GuestGuard implements CanActivate {
  constructor(private auth: AuthService, private router: Router) {}

  canActivate(): boolean {
    if (!this.auth.isLoggedIn) return true;
    // Redirect logged-in users to their dashboard
    const role = this.auth.userRole;
    if (role === 'Instructor') this.router.navigate(['/instructor']);
    else if (role === 'Admin') this.router.navigate(['/admin']);
    else this.router.navigate(['/student/my-learning']);
    return false;
  }
}
