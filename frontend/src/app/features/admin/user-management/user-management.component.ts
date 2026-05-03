import { Component, OnInit, OnDestroy } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { FormControl } from '@angular/forms';
import { Subject } from 'rxjs';
import { takeUntil, debounceTime, catchError, of } from 'rxjs';
import { environment } from '../../../../environments/environment';

interface AdminUser {
  userId: string; fullName: string; email: string; role: string;
  isVerified: boolean; isBanned: boolean; createdAt: string;
  profileImageUrl?: string; totalEnrollments?: number;
}

@Component({
  selector: 'app-user-management',
  templateUrl: './user-management.component.html',
  styleUrls: ['./user-management.component.scss']
})
export class UserManagementComponent implements OnInit, OnDestroy {
  isLoading = true;
  allUsers: AdminUser[] = [];
  users: AdminUser[] = [];
  activeRole = 'All';
  roles = ['All', 'Student', 'Instructor', 'Admin'];
  searchCtrl = new FormControl('');
  actionUserId: string | null = null;
  private destroy$ = new Subject<void>();

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.loadUsers();
    this.searchCtrl.valueChanges.pipe(debounceTime(300), takeUntil(this.destroy$))
      .subscribe(() => this.applyFilter());
  }

  loadUsers(): void {
    this.isLoading = true;
    this.http.get<any>(`${environment.apiUrl}/users`)
      .pipe(catchError(() => of({ data: [] })), takeUntil(this.destroy$))
      .subscribe(res => {
        this.allUsers = Array.isArray(res?.data) ? res.data : [];
        this.applyFilter();
        this.isLoading = false;
      });
  }

  applyFilter(): void {
    const q = (this.searchCtrl.value ?? '').toLowerCase();
    this.users = this.allUsers.filter(u => {
      const roleMatch = this.activeRole === 'All' || u.role === this.activeRole;
      const searchMatch = !q || u.fullName.toLowerCase().includes(q) || u.email.toLowerCase().includes(q);
      return roleMatch && searchMatch;
    });
  }

  setRole(role: string): void { this.activeRole = role; this.applyFilter(); }

  banUser(userId: string): void {
    this.actionUserId = userId;
    this.http.patch(`${environment.apiUrl}/users/${userId}/ban`, {})
      .pipe(catchError(() => of(null)), takeUntil(this.destroy$))
      .subscribe(() => {
        const u = this.allUsers.find(x => x.userId === userId);
        if (u) u.isBanned = true;
        this.applyFilter();
        this.actionUserId = null;
      });
  }

  unbanUser(userId: string): void {
    this.actionUserId = userId;
    this.http.patch(`${environment.apiUrl}/users/${userId}/unban`, {})
      .pipe(catchError(() => of(null)), takeUntil(this.destroy$))
      .subscribe(() => {
        const u = this.allUsers.find(x => x.userId === userId);
        if (u) u.isBanned = false;
        this.applyFilter();
        this.actionUserId = null;
      });
  }

  verifyUser(userId: string): void {
    this.http.patch(`${environment.apiUrl}/users/${userId}/verify`, {})
      .pipe(catchError(() => of(null)), takeUntil(this.destroy$))
      .subscribe(() => {
        const u = this.allUsers.find(x => x.userId === userId);
        if (u) u.isVerified = true;
        this.applyFilter();
      });
  }

  deleteUser(userId: string): void {
    if (!confirm('Are you sure you want to permanently delete this user? This action cannot be undone.')) {
      return;
    }
    
    this.actionUserId = userId;
    this.http.delete(`${environment.apiUrl}/users/${userId}`)
      .pipe(catchError(() => of(null)), takeUntil(this.destroy$))
      .subscribe(() => {
        // Remove user from local array
        this.allUsers = this.allUsers.filter(x => x.userId !== userId);
        this.applyFilter();
        this.actionUserId = null;
      });
  }

  getInitials(name: string): string { return name.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2); }



  ngOnDestroy(): void { this.destroy$.next(); this.destroy$.complete(); }
}
