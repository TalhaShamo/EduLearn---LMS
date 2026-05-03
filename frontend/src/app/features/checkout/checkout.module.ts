import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { HttpClientModule } from '@angular/common/http';
import { SharedModule } from '../../shared/shared.module';
import { CheckoutComponent } from './checkout.component';
import { AuthGuard } from '../../core/guards/auth.guard';

@NgModule({
  declarations: [CheckoutComponent],
  imports: [
    SharedModule,
    HttpClientModule,
    RouterModule.forChild([
      { path: ':courseId', component: CheckoutComponent, canActivate: [AuthGuard] }
    ])
  ],
})
export class CheckoutModule {}
