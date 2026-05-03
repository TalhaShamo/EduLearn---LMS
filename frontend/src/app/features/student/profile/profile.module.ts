import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { SharedModule } from '../../../shared/shared.module';
import { ReactiveFormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { ProfileComponent } from './profile.component';

@NgModule({
  declarations: [ProfileComponent],
  imports: [SharedModule, ReactiveFormsModule, HttpClientModule, RouterModule.forChild([{ path: '', component: ProfileComponent }])],
})
export class ProfileModule {}
