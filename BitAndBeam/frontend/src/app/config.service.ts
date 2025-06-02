import { Injectable } from '@angular/core';

// Tell TypeScript we're using a global window variable
declare const window: any;

@Injectable({
  providedIn: 'root',
})
export class ConfigService {
  get apiUrl(): string {
    const envApiUrl = window.__env?.API_URL;
    if (!envApiUrl) {
      throw new Error('API_URL environment variable is not set. The application cannot function without it.');
    }
    return envApiUrl;
  }
}
