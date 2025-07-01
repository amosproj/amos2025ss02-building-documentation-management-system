import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

export type ThemeMode = 'light' | 'dark' | 'device';

@Injectable({
  providedIn: 'root'
})
export class ThemeService {
  private modeSubject = new BehaviorSubject<ThemeMode>('device');
  mode$: Observable<ThemeMode> = this.modeSubject.asObservable();
  private darkModeSubject = new BehaviorSubject<boolean>(false);
  darkMode$: Observable<boolean> = this.darkModeSubject.asObservable();
  private mediaQuery: MediaQueryList | null = null;

  constructor() {
    this.initTheme();
  }

  private initTheme(): void {
    const savedMode = localStorage.getItem('themeMode') as ThemeMode | null;
    const mode: ThemeMode = savedMode || 'device';
    this.setMode(mode);
  }

  setMode(mode: ThemeMode) {
    this.modeSubject.next(mode);
    localStorage.setItem('themeMode', mode);
    this.applyThemeMode(mode);
  }

  getMode(): ThemeMode {
    return this.modeSubject.value;
  }

  isDarkMode(): boolean {
    return this.darkModeSubject.value;
  }

  private applyThemeMode(mode: ThemeMode) {
    // Remove previous listener
    if (this.mediaQuery && this.mediaQuery.removeEventListener) {
      this.mediaQuery.removeEventListener('change', this.handleSystemThemeChange);
    }

    if (mode === 'light') {
      this.setDarkMode(false);
    } else if (mode === 'dark') {
      this.setDarkMode(true);
    } else {
      // Device mode
      this.mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
      this.setDarkMode(this.mediaQuery.matches);
      this.mediaQuery.addEventListener('change', this.handleSystemThemeChange);
    }
  }

  private handleSystemThemeChange = (event: MediaQueryListEvent) => {
    this.setDarkMode(event.matches);
  };

  private setDarkMode(isDark: boolean) {
    this.darkModeSubject.next(isDark);
    if (isDark) {
      document.body.classList.add('dark-mode');
    } else {
      document.body.classList.remove('dark-mode');
    }
  }
}

