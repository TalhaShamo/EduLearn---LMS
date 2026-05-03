import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthGuard, RoleGuard, GuestGuard } from './core/guards/auth.guard';

const routes: Routes = [
  // ─── Public ─────────────────────────────────────────────────────────────
  {
    path: '',
    loadChildren: () => import('./features/catalog/catalog.module').then(m => m.CatalogModule)
  },
  {
    path: 'auth',
    canActivate: [GuestGuard],
    loadChildren: () => import('./features/auth/auth.module').then(m => m.AuthModule)
  },
  {
    path: 'verify/:certificateId',
    loadChildren: () => import('./features/public/public.module').then(m => m.PublicModule)
  },

  // ─── Student ─────────────────────────────────────────────────────────────
  {
    path: 'student',
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Student'] },
    loadChildren: () => import('./features/student/student.module').then(m => m.StudentModule)
  },
  {
    path: 'learn',
    canActivate: [AuthGuard],
    loadChildren: () => import('./features/player/player.module').then(m => m.PlayerModule)
  },
  {
    path: 'checkout',
    canActivate: [AuthGuard],
    loadChildren: () => import('./features/checkout/checkout.module').then(m => m.CheckoutModule)
  },

  // ─── Instructor ───────────────────────────────────────────────────────────
  {
    path: 'instructor',
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Instructor'] },
    loadChildren: () => import('./features/instructor/instructor.module').then(m => m.InstructorModule)
  },

  // ─── Admin ────────────────────────────────────────────────────────────────
  {
    path: 'admin',
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: ['Admin'] },
    loadChildren: () => import('./features/admin/admin.module').then(m => m.AdminModule)
  },

  // ─── Fallback ────────────────────────────────────────────────────────────
  { path: '**', redirectTo: '' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes, { scrollPositionRestoration: 'top' })],
  exports: [RouterModule]
})
export class AppRoutingModule {}
