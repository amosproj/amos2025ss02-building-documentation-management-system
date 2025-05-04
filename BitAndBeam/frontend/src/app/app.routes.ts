import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { HomeComponent } from './home/home.component';
import { UploadFileComponent } from './upload-file/upload-file.component';

export const routes: Routes = [
  { path: '', component: HomeComponent },
  { path: 'upload', component: UploadFileComponent }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
