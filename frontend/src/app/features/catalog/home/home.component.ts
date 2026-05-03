import { Component, OnInit, OnDestroy } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { Subject, takeUntil } from 'rxjs';

interface Course {
  id: string;
  title: string;
  slug: string;
  instructorName?: string;
  thumbnailUrl?: string;
  price: number;
  level: string;
  enrollmentCount?: number;
  rating?: number;
  category?: string;
}

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss']
})
export class HomeComponent implements OnInit, OnDestroy {
  featuredCourses: Course[] = [];
  isLoading = true;
  private destroy$ = new Subject<void>();

  stats = [
    { value: '500+', label: 'Expert courses', icon: 'menu_book' },
    { value: '50K+', label: 'Active students', icon: 'people' },
    { value: '98%',  label: 'Satisfaction rate', icon: 'thumb_up' },
    { value: '120+', label: 'Expert instructors', icon: 'cast_for_education' },
  ];

  categories = [
    { name: 'Web Development', icon: 'code', color: '#3949AB', bg: 'rgba(57,73,171,0.08)' },
    { name: 'Data Science',    icon: 'bar_chart', color: '#00897B', bg: 'rgba(0,137,123,0.08)' },
    { name: 'UI/UX Design',   icon: 'brush', color: '#E91E63', bg: 'rgba(233,30,99,0.08)' },
    { name: 'Business',       icon: 'trending_up', color: '#F57C00', bg: 'rgba(245,124,0,0.08)' },
    { name: 'Cybersecurity',  icon: 'security', color: '#5C6BC0', bg: 'rgba(92,107,192,0.08)' },
    { name: 'Cloud & DevOps', icon: 'cloud', color: '#039BE5', bg: 'rgba(3,155,229,0.08)' },
    { name: 'Mobile Dev',     icon: 'smartphone', color: '#43A047', bg: 'rgba(67,160,71,0.08)' },
    { name: 'AI & ML',        icon: 'psychology', color: '#8E24AA', bg: 'rgba(142,36,170,0.08)' },
  ];

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.loadFeaturedCourses();
  }

  loadFeaturedCourses(): void {
    this.http.get<{ data: Course[] }>(`${environment.apiUrl}/courses?pageSize=8&sortBy=enrollmentCount`)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.featuredCourses = res?.data ?? [];
          this.isLoading = false;
        },
        error: () => {
          // Show mock data if API not available
          this.featuredCourses = this.getMockCourses();
          this.isLoading = false;
        }
      });
  }

  getStars(rating: number = 4.5): number[] {
    return Array(5).fill(0);
  }

  getMockCourses(): Course[] {
    return [
      { id: '1', title: 'Complete Angular 17 Masterclass', slug: 'angular-17', instructorName: 'Sarah Johnson', price: 2999, level: 'Intermediate', enrollmentCount: 12400, rating: 4.8, category: 'Web Development' },
      { id: '2', title: 'Python for Data Science & ML', slug: 'python-data-science', instructorName: 'Ahmed Khan', price: 2499, level: 'Beginner', enrollmentCount: 9800, rating: 4.7, category: 'Data Science' },
      { id: '3', title: 'UI/UX Design Fundamentals', slug: 'uiux-design', instructorName: 'Priya Sharma', price: 1999, level: 'Beginner', enrollmentCount: 7600, rating: 4.6, category: 'UI/UX Design' },
      { id: '4', title: 'AWS Cloud Practitioner Bootcamp', slug: 'aws-cloud', instructorName: 'James Wilson', price: 3499, level: 'Intermediate', enrollmentCount: 6200, rating: 4.9, category: 'Cloud & DevOps' },
      { id: '5', title: 'React.js — The Complete Guide', slug: 'react-complete', instructorName: 'Emma Davis', price: 2299, level: 'Intermediate', enrollmentCount: 11000, rating: 4.8, category: 'Web Development' },
      { id: '6', title: 'Ethical Hacking for Beginners', slug: 'ethical-hacking', instructorName: 'Omar Farooq', price: 3299, level: 'Beginner', enrollmentCount: 5400, rating: 4.7, category: 'Cybersecurity' },
      { id: '7', title: 'Machine Learning A-Z', slug: 'ml-az', instructorName: 'Liu Wei', price: 4499, level: 'Advanced', enrollmentCount: 8900, rating: 4.9, category: 'AI & ML' },
      { id: '8', title: 'Flutter & Dart Masterclass', slug: 'flutter-dart', instructorName: 'Zara Ahmed', price: 2799, level: 'Intermediate', enrollmentCount: 4700, rating: 4.6, category: 'Mobile Dev' },
    ];
  }

  getInitials(name: string): string {
    return name?.split(' ').map(n => n[0]).join('').substring(0,2).toUpperCase() ?? 'EL';
  }

  ngOnDestroy(): void { this.destroy$.next(); this.destroy$.complete(); }
}
