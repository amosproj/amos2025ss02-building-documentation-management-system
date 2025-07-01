import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { AiAssistantComponent } from './components/ai-assistant/ai-assistant.component';
import { ThemeService } from './services/theme.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, AiAssistantComponent],
  template: `
    <style>
      :host {
        display: block;
        height: 100vh;
        width: 100vw;
        overflow-x: hidden;
        overflow-y: auto;
        position: relative;
      }

      /* Ensure proper component visibility */
      app-upload-file,
      app-ai-assistant {
        display: block;
      }
    </style>
    <router-outlet></router-outlet>
    
    <!-- Global AI Assistant Widget that appears on all pages -->
    <app-ai-assistant [globalMode]="true"></app-ai-assistant>
  `,
})
export class AppComponent {
  title = 'BitAndBeam';
  
  constructor(private themeService: ThemeService) {}
}
