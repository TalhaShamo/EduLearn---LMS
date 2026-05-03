import { Component, OnInit, HostListener } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { User } from '../../core/models/models';

@Component({
  selector: 'app-header',
  templateUrl: './header.component.html',
  styleUrls: ['./header.component.scss']
})
export class HeaderComponent implements OnInit {
  currentUser: User | null = null;
  isScrolled = false;
  searchQuery = '';
  mobileMenuOpen = false;

  constructor(public auth: AuthService, private router: Router) {}

  ngOnInit(): void {
    this.auth.currentUser$.subscribe(user => this.currentUser = user);
  }

  @HostListener('window:scroll')
  onScroll(): void {
    this.isScrolled = window.scrollY > 20;
  }

  onSearch(): void {
    if (this.searchQuery.trim()) {
      this.router.navigate(['/courses'], { queryParams: { q: this.searchQuery.trim() } });
      this.searchQuery = '';
    }
  }

  getDashboardRoute(): string {
    switch (this.currentUser?.role) {
      case 'Instructor': return '/instructor';
      case 'Admin': return '/admin';
      default: return '/student/my-learning';
    }
  }

  logout(): void {
    this.auth.logout();
  }

  toggleMobileMenu(): void {
    this.mobileMenuOpen = !this.mobileMenuOpen;
  }
}
