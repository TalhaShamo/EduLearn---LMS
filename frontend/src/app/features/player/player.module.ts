import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

const routes: Routes = [
  { path: ':courseId', loadChildren: () => import('./course-player/course-player.module').then(m => m.CoursePlayerModule) },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
})
export class PlayerModule {}
