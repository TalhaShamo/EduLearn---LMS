import { Injectable, Injector } from '@angular/core';
import { HttpRequest, HttpHandler, HttpEvent, HttpInterceptor } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AuthService } from '../services/auth.service';

@Injectable()
export class JwtInterceptor implements HttpInterceptor {
  private authService?: AuthService;

  constructor(private injector: Injector) {}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    // Lazy inject AuthService to avoid circular dependency
    if (!this.authService) {
      this.authService = this.injector.get(AuthService);
    }

    const token = this.authService.accessToken;

    if (token) {
      req = req.clone({
        setHeaders: { Authorization: `Bearer ${token}` },
        withCredentials: true   // sends HttpOnly refresh-token cookie too
      });
    } else {
      req = req.clone({ withCredentials: true });
    }

    return next.handle(req);
  }
}
