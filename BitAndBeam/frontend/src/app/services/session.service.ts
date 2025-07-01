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

  // Always use the user object from the login response for email, not the JWT token.
  async login(email: string, password: string): Promise<boolean> {
    try {
      const response = await this.authApi.authLoginPost({ email, password });
      const result: any = response.data;

      if (result.token && result.user) {
        console.log('✅ Token from login:', result.token);
        this.setSession(result.token, result.user);
        return true;
      }
    } catch (err) {
      console.error('❌ Login failed:', err);
    }
    return false;
  }

  // Store user info in sessionStorage on login, restore on reload
  private setSession(token: string, user: User): void {
    this.token.set(token);
    this.user.set(user);
    sessionStorage.setItem(this.tokenKey, token);
    sessionStorage.setItem('user', JSON.stringify(user));
    setTimeout(() => this.scheduleAutoLogout(token), 0);
  }

  logout(): void {
    console.log('🚪 Logout called, clearing session');
    sessionStorage.removeItem(this.tokenKey);
    sessionStorage.removeItem('user');
    this.token.set(null);
    this.user.set(null);
    this.router.navigate(['/login']);
  }

  private restoreSession(): void {
    const token = sessionStorage.getItem(this.tokenKey);
    console.log('🔄 RestoreSession called, token exists:', !!token);
    if (!token) return;
    this.token.set(token);
    // Restore user from sessionStorage if available
    const userStr = sessionStorage.getItem('user');
    if (userStr) {
      try {
        const user: User = JSON.parse(userStr);
        this.user.set(user);
      } catch (e) {
        this.user.set(null);
      }
    }
    setTimeout(() => {
      try {
        const decoded = jwt_decode<DecodedToken>(token);
        const now = Date.now() / 1000;
        console.log('🔍 Token decoded, expires at:', new Date(decoded.exp * 1000), 'current time:', new Date());
        if (decoded.exp <= now) {
          console.log('❌ Token expired, logging out');
          this.logout();
        } else {
          setTimeout(() => this.scheduleAutoLogout(token), 0);
        }
      } catch (error) {
        console.log('❌ Token decode error, logging out:', error);
        this.logout();
      }
    }, 0);
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

  // Debug method to check user state
  debugUserState(): void {
    console.log('🐛 Current user state:', {
      token: this.getToken() ? 'exists' : 'null',
      user: this.currentUser,
      isAuthenticated: this.isAuthenticated()
    });
  }

  // Force session check and restoration if needed
  ensureSessionValid(): void {
    if (!this.currentUser && this.getToken()) {
      console.log('🔧 User data missing but token exists, restoring session...');
      this.restoreSession();
    }
  }
}
