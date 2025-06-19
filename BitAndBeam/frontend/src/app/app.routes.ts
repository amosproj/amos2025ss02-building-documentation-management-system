import { Routes } from '@angular/router';
import { HomeComponent } from './pages/home/home.component';
import { LoginComponent } from './pages/login/login.component';
import { UploadFileComponent } from './components/upload-file/upload-file.component';
import { FileViewComponent } from './pages/file-view/file-view.component';
import { AuthGuard } from './guards/auth.guard';
import { CreateBuildingComponent } from './components/create-building/create-building.component';


export const routes: Routes = [
  { path: '', component: HomeComponent },
  { path: 'login', component: LoginComponent },
  { path: 'upload', component: UploadFileComponent, canActivate: [AuthGuard] },
  { path: 'file-view', component: FileViewComponent, canActivate: [AuthGuard] },
  { path: 'documents/:id', component: FileViewComponent, canActivate: [AuthGuard] },
  { path: 'create-building', component: CreateBuildingComponent, canActivate: [AuthGuard] },
  { path: '**', redirectTo: 'upload' }
];
