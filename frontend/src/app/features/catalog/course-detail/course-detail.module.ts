import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { HttpClientModule } from '@angular/common/http';
import { SharedModule } from '../../../shared/shared.module';
import { CourseDetailComponent } from './course-detail.component';

@NgModule({
  declarations: [CourseDetailComponent],
  imports: [
    SharedModule,
    HttpClientModule,
    RouterModule.forChild([{ path: '', component: CourseDetailComponent }])
  ],
})
export class CourseDetailModule {}
