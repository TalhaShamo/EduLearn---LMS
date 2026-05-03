import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { SharedModule } from '../../shared/shared.module';
import { AdminDashboardComponent } from './dashboard/admin-dashboard.component';
import { UserManagementComponent } from './user-management/user-management.component';
import { CourseReviewComponent } from './course-review/course-review.component';

const routes: Routes = [
  { path: '',           redirectTo: 'dashboard', pathMatch: 'full' },
  { path: 'dashboard',  component: AdminDashboardComponent },
  { path: 'users',      component: UserManagementComponent },
  { path: 'courses',    component: CourseReviewComponent },
];

@NgModule({
  declarations: [
    AdminDashboardComponent,
    UserManagementComponent,
    CourseReviewComponent,
  ],
  imports: [
    SharedModule,
    ReactiveFormsModule,
    FormsModule,
    HttpClientModule,
    RouterModule.forChild(routes),
  ],
})
export class AdminModule {}
