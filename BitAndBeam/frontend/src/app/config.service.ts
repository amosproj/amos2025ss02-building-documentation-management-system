import { Injectable } from '@angular/core';

declare global {
  interface Window {
    __env?: {
      API_URL?: string;
    };
  }
}

@Injectable({
  providedIn: 'root',
})
export class ConfigService {
  get apiUrl(): string {
    return window.__env?.API_URL || 'http://localhost:3000';
  }
}
