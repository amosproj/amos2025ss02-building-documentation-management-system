import { Injectable, PLATFORM_ID, Inject } from '@angular/core';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable, of } from 'rxjs';
import { tap, delay } from 'rxjs/operators';
import { isPlatformBrowser } from '@angular/common';

export interface User {
  id: number;
  username: string;
  password: string;
  displayName: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  // Hardcoded users as per Criterion 4
  private users: User[] = [
    { id: 1, username: 'admin', password: 'admin123', displayName: 'Administrator' },
    { id: 2, username: 'user', password: 'user123', displayName: 'Regular User' }
  ];

  private currentUserSubject = new BehaviorSubject<User | null>(null);
  public currentUser$: Observable<User | null> = this.currentUserSubject.asObservable();
  
  private isAuthenticatedSubject = new BehaviorSubject<boolean>(false);
  public isAuthenticated$ = this.isAuthenticatedSubject.asObservable();
  private isBrowser: boolean;

  constructor(
    private router: Router,
    @Inject(PLATFORM_ID) private platformId: Object
  ) {
    this.isBrowser = isPlatformBrowser(this.platformId);
    
    // Initialize authentication state
    this.isAuthenticatedSubject.next(this.hasStoredUser());
    
    // Check if user is stored in localStorage on service initialization (browser only)
    if (this.isBrowser) {
      this.loadStoredUser();
    }
  }

  login(username: string, password: string): Observable<boolean> {
    // Find user with matching credentials
    const user = this.users.find(u => 
      u.username === username && u.password === password);
    
    // Simulate API delay
    return of(!!user).pipe(
      delay(800),
      tap(isValid => {
        if (isValid && user) {
          if (this.isBrowser) {
            // Store user info in localStorage (without password)
            const { password, ...userToStore } = user;
            localStorage.setItem('currentUser', JSON.stringify(userToStore));
          }
          
          // Update subjects
          this.currentUserSubject.next(user);
          this.isAuthenticatedSubject.next(true);
        }
      })
    );
  }

  logout(): void {
    // Clear stored user
    if (this.isBrowser) {
      localStorage.removeItem('currentUser');
    }
    
    // Update subjects
    this.currentUserSubject.next(null);
    this.isAuthenticatedSubject.next(false);
    
    // Navigate to login
    this.router.navigate(['/login']);
  }

  getCurrentUser(): User | null {
    return this.currentUserSubject.value;
  }

  private hasStoredUser(): boolean {
    if (!this.isBrowser) {
      return false;
    }
    return !!localStorage.getItem('currentUser');
  }

  private loadStoredUser(): void {
    if (!this.isBrowser) {
      return;
    }
    
    const storedUser = localStorage.getItem('currentUser');
    if (storedUser) {
      const parsedUser = JSON.parse(storedUser);
      // Find the full user (with password) from our users array
      const fullUser = this.users.find(u => u.id === parsedUser.id);
      if (fullUser) {
        this.currentUserSubject.next(fullUser);
        this.isAuthenticatedSubject.next(true);
      }
    }
  }
}
