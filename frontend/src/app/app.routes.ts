import { Routes } from '@angular/router';
import { AuthGuard } from './auth/guards/auth.guard';
import { LoginComponent } from './auth/login/login.component';
import { HomeComponent } from './home/home.component';
import { ChatPageComponent } from './chat-page/chat-page.component';

export const routes: Routes = [
  // Public routes
  { path: 'login', component: LoginComponent },
  { path: '', component: HomeComponent },
  { path: 'chat', component: ChatPageComponent },
  
  // Protected routes
  { 
    path: 'upload', 
    loadComponent: () => import('./upload-file/upload-file.component').then(c => c.UploadFileComponent),
    canActivate: [AuthGuard]
  },
  { 
    path: 'documents', 
    loadComponent: () => import('./document-list/document-list.component').then(c => c.DocumentListComponent),
    canActivate: [AuthGuard]
  },
  { 
    path: 'document/:id', 
    loadComponent: () => import('./document-viewer/document-viewer.component').then(c => c.DocumentViewerComponent),
    canActivate: [AuthGuard]
  },
  
  // Redirect any unknown routes to home
  { path: '**', redirectTo: '' }
];
