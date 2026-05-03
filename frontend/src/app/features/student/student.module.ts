import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { HttpClientModule } from '@angular/common/http';
import { SharedModule } from '../../shared/shared.module';
import { MyLearningComponent } from './my-learning/my-learning.component';

const routes: Routes = [
  { path: '',              redirectTo: 'my-learning', pathMatch: 'full' },
  { path: 'my-learning',  component: MyLearningComponent },
  { path: 'certificates', loadChildren: () => import('./certificates/certificates.module').then(m => m.CertificatesModule) },
  { path: 'profile',      loadChildren: () => import('./profile/profile.module').then(m => m.ProfileModule) },
];

@NgModule({
  declarations: [MyLearningComponent],
  imports: [
    SharedModule,
    HttpClientModule,
    RouterModule.forChild(routes),
  ],
})
export class StudentModule {}
