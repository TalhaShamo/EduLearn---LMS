import { Injectable, Injector } from '@angular/core';
import { HttpRequest, HttpHandler, HttpEvent, HttpInterceptor, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

@Injectable()
export class ErrorInterceptor implements HttpInterceptor {
  private authService?: AuthService;

  constructor(
    private snackBar: MatSnackBar,
    private router: Router,
    private injector: Injector
  ) {}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    return next.handle(req).pipe(
      catchError((error: HttpErrorResponse) => {
        let message = 'Something went wrong. Please try again.';

        if (error.error?.message) {
          message = error.error.message;
        } else if (error.error?.errors?.length) {
          message = error.error.errors[0];
        }

        switch (error.status) {
          case 401:
            // Don't trigger logout for the silent refresh-token probe on startup
            if (!req.url.includes('/auth/refresh-token')) {
              // Lazy inject AuthService to avoid circular dependency
              if (!this.authService) {
                this.authService = this.injector.get(AuthService);
              }
              this.authService.logout();
            }
            break;
          case 403:
            this.router.navigate(['/']);
            this.showError('Access denied.');
            break;
          case 404:
            // Let component handle 404s
            break;
          case 0:
            this.showError('Cannot connect to server. Is the backend running?');
            break;
          default:
            this.showError(message);
        }

        return throwError(() => error);
      })
    );
  }

  private showError(msg: string): void {
    this.snackBar.open(msg, 'Dismiss', {
      duration: 5000,
      panelClass: ['snack-error'],
      horizontalPosition: 'right',
      verticalPosition: 'top'
    });
  }
}
