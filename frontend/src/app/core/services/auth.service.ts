import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { LoginRequest, RegisterRequest, AuthResponse, User } from '../models/models';
import { jwtDecode } from 'jwt-decode';

interface JwtPayload {
  sub?: string;
  nameid?: string;
  email?: string;
  role?: string;
  unique_name?: string;
  ["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"]?: string;
  exp: number;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly apiUrl = environment.apiUrl;
  private readonly TOKEN_KEY = 'edulearn_token';
  private readonly USER_KEY = 'edulearn_user';

  // In-memory token with localStorage backup
  private _accessToken: string | null = null;
  private _currentUser = new BehaviorSubject<User | null>(null);

  currentUser$ = this._currentUser.asObservable();

  constructor(private http: HttpClient, private router: Router) {
    // Restore session from localStorage on startup
    this.restoreSession();
  }


  get currentUser(): User | null {
    return this._currentUser.value;
  }

  get accessToken(): string | null {
    return this._accessToken;
  }

  get isLoggedIn(): boolean {
    return !!this._accessToken;
  }

  get userRole(): string | null {
    return this._currentUser.value?.role ?? null;
  }

  login(req: LoginRequest): Observable<ApiResponse<AuthResponse>> {
    return this.http.post<ApiResponse<AuthResponse>>(`${this.apiUrl}/auth/login`, req)
      .pipe(tap(res => {
        if (res.success) {
          this.setSession(res.data);
        }
      }));
  }

  register(req: RegisterRequest): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${this.apiUrl}/auth/register`, req);
  }

  logout(): void {
    this.http.post(`${this.apiUrl}/auth/logout`, {}).subscribe();
    this._accessToken = null;
    this._currentUser.next(null);
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.USER_KEY);
    this.router.navigate(['/auth/login']);
  }

  private restoreSession(): void {
    const token = localStorage.getItem(this.TOKEN_KEY);
    const userJson = localStorage.getItem(this.USER_KEY);
    
    if (token && userJson) {
      try {
        // Check if token is expired
        const payload = jwtDecode<JwtPayload>(token);
        const now = Math.floor(Date.now() / 1000);
        
        if (payload.exp > now) {
          // Token is still valid
          this._accessToken = token;
          this._currentUser.next(JSON.parse(userJson));
        } else {
          // Token expired, clear storage
          localStorage.removeItem(this.TOKEN_KEY);
          localStorage.removeItem(this.USER_KEY);
        }
      } catch {
        // Invalid token, clear storage
        localStorage.removeItem(this.TOKEN_KEY);
        localStorage.removeItem(this.USER_KEY);
      }
    }
  }

  tryRefresh(): void {
    this.http.post<ApiResponse<AuthResponse>>(`${this.apiUrl}/auth/refresh-token`, {}, { withCredentials: true })
      .subscribe({
        next: res => {
          if (res?.success) this.setSession(res.data);
        },
        error: () => { /* No valid refresh token — user must log in */ }
      });
  }

  private setSession(data: AuthResponse): void {
    this._accessToken = data.accessToken;

    // Decode JWT to get user info
    try {
      const payload = jwtDecode<JwtPayload>(data.accessToken);
      const roleFromJwt = payload.role ?? payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"];
      const userIdFromJwt = payload.sub ?? payload.nameid;
      const user: User = {
        userId: userIdFromJwt ?? data.user?.userId,
        email: payload.email ?? data.user?.email,
        role: (roleFromJwt ?? data.user?.role) as any,
        fullName: data.user?.fullName ?? payload.unique_name ?? payload.email,
        profileImageUrl: data.user?.profileImageUrl,
        isVerified: true
      };
      this._currentUser.next(user);
      
      // Persist to localStorage
      localStorage.setItem(this.TOKEN_KEY, data.accessToken);
      localStorage.setItem(this.USER_KEY, JSON.stringify(user));
    } catch {
      this._currentUser.next(data.user);
      
      // Persist to localStorage
      localStorage.setItem(this.TOKEN_KEY, data.accessToken);
      localStorage.setItem(this.USER_KEY, JSON.stringify(data.user));
    }
  }
}

// Minimal wrapper to avoid circular import
interface ApiResponse<T> { success: boolean; data: T; message?: string; }
