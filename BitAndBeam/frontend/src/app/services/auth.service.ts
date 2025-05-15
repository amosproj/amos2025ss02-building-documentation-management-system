import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private users = [
    { username: 'user1', password: 'pass1' },
    { username: 'user2', password: 'pass2' },
  ];

  private loggedInUser: string | null = null;

  login(username: string, password: string): boolean {
    const match = this.users.find(
      (u) => u.username === username && u.password === password,
    );
    if (match) {
      this.loggedInUser = username;
      return true;
    }
    return false;
  }

  isAuthenticated(): boolean {
    return this.loggedInUser !== null;
  }

  getUsername(): string | null {
    return this.loggedInUser;
  }

  logout(): void {
    this.loggedInUser = null;
  }
}
