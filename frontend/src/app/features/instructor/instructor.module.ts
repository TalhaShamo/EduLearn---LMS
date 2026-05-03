import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { SharedModule } from '../../shared/shared.module';
import { InstructorDashboardComponent } from './dashboard/instructor-dashboard.component';
import { MyCoursesComponent } from './my-courses/my-courses.component';
import { CourseBuilderComponent } from './course-builder/course-builder.component';

const routes: Routes = [
  { path: '',            redirectTo: 'dashboard', pathMatch: 'full' },
  { path: 'dashboard',  component: InstructorDashboardComponent },
  { path: 'courses',    component: MyCoursesComponent },
  { path: 'courses/new', component: CourseBuilderComponent },
  { path: 'courses/:courseId/edit', component: CourseBuilderComponent },
  { path: 'analytics',  loadChildren: () => import('./analytics/analytics.module').then(m => m.AnalyticsModule) },
];

@NgModule({
  declarations: [
    InstructorDashboardComponent,
    MyCoursesComponent,
    CourseBuilderComponent,
  ],
  imports: [
    SharedModule,
    ReactiveFormsModule,
    FormsModule,
    HttpClientModule,
    RouterModule.forChild(routes),
  ],
})
export class InstructorModule {}
