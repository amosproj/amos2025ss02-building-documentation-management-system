import { Injectable } from '@angular/core';
import {
  CanActivate,
  ActivatedRouteSnapshot,
  RouterStateSnapshot,
  Router
} from '@angular/router';
import { AuthService } from '../services/auth.service';
import { BuildingService } from '../services/building.service';

@Injectable({ providedIn: 'root' })
export class AuthGuard implements CanActivate {
  constructor(
    private authService: AuthService,
    private router: Router,
    private buildingService: BuildingService
  ) {}

  canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): boolean {
    const isLoggedIn = this.authService.isAuthenticated();

    // Trying to access file-view directly without selecting a file
    if (state.url === '/file-view') {
      const file = this.buildingService.getSelectedFile?.(); // or store it via service
      if (!file) {
        this.router.navigate(['/upload']);
        return false;
      }
    }

    if (isLoggedIn) return true;

    // Allow redirect back ONLY to upload
    const returnUrl = state.url !== '/file-view' ? state.url : '/upload';

    this.router.navigate(['/login'], {
      queryParams: { returnUrl }
    });
    return false;
  }
}
