import { Component, Input } from '@angular/core';
import { Course } from '../../../core/models/models';

@Component({
  selector: 'app-course-card',
  templateUrl: './course-card.component.html',
  styleUrls: ['./course-card.component.scss']
})
export class CourseCardComponent {
  Math = Math;
  @Input() course!: Course;
  @Input() showBadge = false;

  getInitials(name: string): string {
    return (name ?? '').split(' ').map(n => n[0]).join('').substring(0, 2).toUpperCase();
  }

  formatRating(r: number): string {
    return r?.toFixed(1) ?? '—';
  }

  get stars(): number[] { return [1, 2, 3, 4, 5]; }

  get levelClass(): string {
    return (this.course?.level ?? '').toLowerCase();
  }
  
  getThumbnailUrl(): string {
    if (!this.course?.thumbnailUrl) return '';
    // If it's already a full URL, return as is
    if (this.course.thumbnailUrl.startsWith('http')) {
      return this.course.thumbnailUrl;
    }
    // Otherwise, prepend the course API base URL
    return `http://localhost:5002${this.course.thumbnailUrl}`;
  }
}
