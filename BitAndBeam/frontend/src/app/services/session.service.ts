// session.service.ts
import { Injectable, computed, signal } from '@angular/core';
import { Router } from '@angular/router';
import { AuthApi, Configuration } from '../../api';
import jwt_decode from 'jwt-decode';
import { ConfigService } from '../config.service';

interface User {
  id: number;
  email: string;
  role: string;
  organizationId: number;
}

interface DecodedToken {
  exp: number;
  uid: string;
  sub: string;
  org: string;
  r: string;
}

@Injectable({ providedIn: 'root' })
export class SessionService {
  private tokenKey = 'jwt_token';
  private token = signal<string | null>(sessionStorage.getItem(this.tokenKey));
  private user = signal<User | null>(null);

  isAuthenticated = computed(() => !!this.token());
  currentUser = this.user.asReadonly();

  private authApi: AuthApi;

  constructor(private router: Router, private configService: ConfigService) {
    const apiUrl = this.configService.apiUrl;
    const apiConfig = new Configuration({ basePath: apiUrl });
    this.authApi = new AuthApi(apiConfig);
    this.restoreSession();
  }

  async login(email: string, password: string): Promise<boolean> {
    try {
      const response = await this.authApi.authLoginPost({ email, password });
      const result: any = response.data;

      if (result.token && result.user) {
        console.log('✅ Token from login:', result.token);

        // ✅ Changed: scheduleAutoLogout is now called asynchronously (non-blocking)
        this.setSession(result.token, result.user);
        return true;
      }
    } catch (err) {
      console.error('❌ Login failed:', err);
    }
    return false;
  }

  logout(): void {
    sessionStorage.removeItem(this.tokenKey);
    this.token.set(null);
    this.user.set(null);
    this.router.navigate(['/login']);
  }

  private setSession(token: string, user: User): void {
    this.token.set(token);
    this.user.set(user);
    sessionStorage.setItem(this.tokenKey, token);
    setTimeout(() => this.scheduleAutoLogout(token), 0);
  }


  private restoreSession(): void {
    const token = sessionStorage.getItem(this.tokenKey);
    if (!token) return;

    try {
      const decoded = jwt_decode<DecodedToken>(token);
      const now = Date.now() / 1000;

      if (decoded.exp > now) {
        this.token.set(token);
        const restoredUser: User = {
          id: Number(decoded.uid),
          email: decoded.sub,
          role: decoded.r,
          organizationId: Number(decoded.org)
        };
        this.user.set(restoredUser);

        // ✅ Changed: use non-blocking timeout for auto-logout
        setTimeout(() => this.scheduleAutoLogout(token), 0);
      } else {
        this.logout();
      }
    } catch {
      // ✅ Added: defensive catch block for malformed/invalid token
      this.logout();
    }
  }
  
  private scheduleAutoLogout(token: string): void {
    const decoded = jwt_decode<DecodedToken>(token);
    const expiresAt = decoded.exp * 1000;
    const timeout = expiresAt - Date.now();

    if (timeout > 0) {
      setTimeout(() => this.logout(), timeout);
    } else {
      this.logout();
    }
  }

  getToken(): string | null {
    return this.token();
  }
}
