import { Injectable } from '@angular/core';

// Tell TypeScript we're using a global window variable
declare const window: any;

@Injectable({
  providedIn: 'root',
})
export class ConfigService {
  get apiUrl(): string {
    return window.__env?.API_URL || 'http://localhost:5001';
  }
}
