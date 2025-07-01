import { Injectable } from '@angular/core';
import {
  CanActivate,
  ActivatedRouteSnapshot,
  RouterStateSnapshot,
  Router
} from '@angular/router';
import { SessionService } from '../services/session.service';
import { BuildingService } from '../services/building.service';

@Injectable({ providedIn: 'root' })
export class AuthGuard implements CanActivate {
  constructor(
    private session: SessionService,
    private router: Router,
    private buildingService: BuildingService
  ) {}

  canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): boolean {
    // TEMPORARY: Skip authentication for testing
    //return true;

    
    const isLoggedIn = this.session.isAuthenticated();

    // Special case: direct access to /file-view without file selected
    if (state.url === '/file-view') {
      const file = this.buildingService.getSelectedFile?.();
      if (!file) {
        this.router.navigate(['/upload']);
        return false;
      }
    }

    if (isLoggedIn) return true;

    const returnUrl = state.url !== '/file-view' ? state.url : '/upload';
    this.router.navigate(['/login'], {
      queryParams: { returnUrl }
    });
    
    return false;
    
  }
}
