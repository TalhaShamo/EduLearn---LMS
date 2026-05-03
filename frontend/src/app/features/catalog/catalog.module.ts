import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Routes } from '@angular/router';

const routes: Routes = [
  {
    path: '',
    loadChildren: () => import('./home/home.module').then(m => m.HomeModule)
  },
  {
    path: 'courses',
    loadChildren: () => import('./course-search/course-search.module').then(m => m.CourseSearchModule)
  },
  {
    path: 'courses/:slug',
    loadChildren: () => import('./course-detail/course-detail.module').then(m => m.CourseDetailModule)
  },
];

@NgModule({
  imports: [CommonModule, RouterModule.forChild(routes)],
})
export class CatalogModule {}
