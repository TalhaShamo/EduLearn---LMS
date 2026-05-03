import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { ReactiveFormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { SharedModule } from '../../../shared/shared.module';
import { CourseSearchComponent } from './course-search.component';

@NgModule({
  declarations: [CourseSearchComponent],
  imports: [
    SharedModule,
    ReactiveFormsModule,
    HttpClientModule,
    RouterModule.forChild([{ path: '', component: CourseSearchComponent }])
  ],
})
export class CourseSearchModule {}
