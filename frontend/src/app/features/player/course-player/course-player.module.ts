import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { HttpClientModule } from '@angular/common/http';
import { SharedModule } from '../../../shared/shared.module';
import { CoursePlayerComponent } from './course-player.component';
import { AuthGuard } from '../../../core/guards/auth.guard';

@NgModule({
  declarations: [CoursePlayerComponent],
  imports: [
    SharedModule,
    HttpClientModule,
    RouterModule.forChild([
      { path: '', component: CoursePlayerComponent, canActivate: [AuthGuard] }
    ])
  ],
})
export class CoursePlayerModule {}
