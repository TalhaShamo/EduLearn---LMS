import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { HttpClientModule } from '@angular/common/http';
import { SharedModule } from '../../../shared/shared.module';
import { AnalyticsComponent } from './analytics.component';

@NgModule({
  declarations: [AnalyticsComponent],
  imports: [
    SharedModule,
    HttpClientModule,
    RouterModule.forChild([{ path: '', component: AnalyticsComponent }])
  ],
})
export class AnalyticsModule {}
