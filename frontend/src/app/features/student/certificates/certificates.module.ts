import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { SharedModule } from '../../../shared/shared.module';
import { HttpClientModule } from '@angular/common/http';
import { CertificatesComponent } from './certificates.component';
import { CertificateViewComponent } from './certificate-view/certificate-view.component';

const routes: Routes = [
  { path: '', component: CertificatesComponent },
  { path: 'view/:id', component: CertificateViewComponent }
];

@NgModule({
  declarations: [CertificatesComponent, CertificateViewComponent],
  imports: [SharedModule, HttpClientModule, RouterModule.forChild(routes)],
})
export class CertificatesModule {}
