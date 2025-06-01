import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  constructor(private http: HttpClient) {}

  login(email: string, password: string): Observable<any> {
    return this.http.post<{ token: string }>('/api/auth/login', {
      email,
      password
    }).pipe(
      tap(response => {
        // ✅ Save the token in localStorage
        localStorage.setItem('authToken', response.token);
      })
    );
  }

  isAuthenticated(): boolean {
    return !!localStorage.getItem('authToken');
  }

  logout(): void {
    localStorage.removeItem('authToken');
  }
  getUsername(): string | null {
    const token = localStorage.getItem('authToken');
    if (!token) return null;

    const payload = JSON.parse(atob(token.split('.')[1]));
    return payload?.username || null;
  }
}
