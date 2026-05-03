import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { CourseCardComponent } from './components/course-card/course-card.component';
import { DurationPipe, TruncatePipe } from './pipes/shared-pipes';

@NgModule({
  declarations: [
    CourseCardComponent,
    DurationPipe,
    TruncatePipe,
  ],
  imports: [CommonModule, RouterModule],
  exports: [
    CourseCardComponent,
    DurationPipe,
    TruncatePipe,
    CommonModule,
    RouterModule,
  ],
})
export class SharedModule {}
