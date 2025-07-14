import {
  Component,
  OnInit,
  OnDestroy,
  OnChanges,
  Input,
  SimpleChanges,
  effect,
  AfterViewInit,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { MarkdownBoldPipe } from '../../pipes/markdown-bold.pipe';
import { ThemeService } from '../../services/theme.service';
import { SessionService } from '../../services/session.service';
import { BuildingService } from '../../services/building.service';
import { Subscription } from 'rxjs';
import { ApiClientFactory } from '../../services/api-client.factory';
import {
  OllamaApi,
  OllamaRequest,
  DocumentChatbotRequest,
  DocumentsApi,
} from '../../../api';

interface ChatMessage {
  text: string;
  sender: 'user' | 'assistant';
  timestamp?: Date;
}

@Component({
  selector: 'app-ai-assistant',
  standalone: true,
  imports: [CommonModule, FormsModule, HttpClientModule, MarkdownBoldPipe],
  templateUrl: './ai-assistant.component.html',
  styleUrls: ['./ai-assistant.component.css'],
})
export class AiAssistantComponent implements OnInit, OnDestroy {
  @Input() globalMode: boolean = false; // Whether this is the global floating widget
  @Input() documentId?: number;
  @Input() documentTitle?: string;

  messages: ChatMessage[] = [];
  userInput = '';
  errorMessage = '';
  showChatInterface = false; // Start closed in global mode
  isProcessing = false;
  isDarkMode = false;
  isAuthenticated = false;

  private themeSubscription: Subscription | null = null;
  private ollamaApi: OllamaApi | null = null;

  constructor(
    private themeService: ThemeService,
    private apiClientFactory: ApiClientFactory,
    private sessionService: SessionService,
    private buildingService: BuildingService,
  ) {
    // Don't create API client in constructor - create it when needed

    // Watch for authentication state changes using effect
    effect(() => {
      this.isAuthenticated = this.sessionService.isAuthenticated();
    });
  }

  ngOnInit(): void {
    this.themeSubscription = this.themeService.darkMode$.subscribe((isDark) => {
      this.isDarkMode = isDark;
    });

    // Initialize with current theme and auth state
    this.isDarkMode = this.themeService.isDarkMode();
    this.isAuthenticated = this.sessionService.isAuthenticated();

    // Always start with chat interface hidden after page load/reload/redirect
    this.showChatInterface = false;

    // Load FontAwesome if not already loaded
    this.loadFontAwesome();
  }

  ngOnDestroy(): void {
    if (this.themeSubscription) {
      this.themeSubscription.unsubscribe();
      console.log('💥 AiAssistantComponent destroyed');
    }
  }

  // Show the chat interface
  showChat(): void {
    this.showChatInterface = true;
  }

  // Hide the chat interface
  hideChat(): void {
    this.showChatInterface = false;
  }

  // Toggle chat visibility
  toggleChat(): void {
    this.showChatInterface = !this.showChatInterface;
  }

  // Load FontAwesome icons for the chat interface
  private loadFontAwesome(): void {
    if (!document.getElementById('font-awesome-css')) {
      const link = document.createElement('link');
      link.id = 'font-awesome-css';
      link.rel = 'stylesheet';
      link.href =
        'https://cdnjs.cloudflare.com/ajax/libs/font-awesome/5.15.4/css/all.min.css';
      document.head.appendChild(link);
    }
  }

  // Check if user is authenticated
  isUserAuthenticated(): boolean {
    return this.isAuthenticated;
  }

  // Get or create API client with current authentication token
  private getOllamaApi(): OllamaApi {
    // Only create API client if it doesn't exist yet
    if (!this.ollamaApi) {
      // Create API client with current token
      this.ollamaApi = this.apiClientFactory.create<OllamaApi>(OllamaApi);
    }

    return this.ollamaApi;
  }

  private getDocumentsApi(): DocumentsApi {
    return this.apiClientFactory.create(DocumentsApi);
  }

  sendMessage = (): void => {
    const userMessage = this.userInput.trim();
    if (!userMessage || this.isProcessing) {
      return;
    }

    // Add user message to chat
    this.messages.push({
      text: userMessage,
      sender: 'user',
      timestamp: new Date(),
    });

    this.userInput = '';
    this.errorMessage = '';
    this.isProcessing = true;

    // Prepare the previous messages for context if needed
    const previousMessages = this.messages
      .slice(-10) // Get last 10 messages for context
      .map((msg) => ({ role: msg.sender, content: msg.text }));

    if (this.documentId) {
      const request: DocumentChatbotRequest = {
        userInput: userMessage,
      };
      this.getDocumentsApi()
        .apiDocumentsDocumentIdAskPost(this.documentId, request)
        .then((res) => {
          this.messages.push({
            text: res?.data?.response ?? 'No response received.',
            sender: 'assistant',
            timestamp: new Date(),
          });
          console.log('📎 Sending document request with ID:', this.documentId);
          console.log('📥 Response from document ask:', res);
          this.isProcessing = false;
        })
        .catch((error: unknown) => {
          console.error('Error asking document question:', error);
          this.handleError('Failed to get answer for this document.');
          this.isProcessing = false;
        });
    } else {
      const ollamaRequest: OllamaRequest = {
        prompt: userMessage,
        context: {
          conversation: previousMessages,
        },
      };

      this.getOllamaApi()
        .apiOllamaAskPost(ollamaRequest)
        .then((response) => {
          const responseData = response as any;
          if (responseData && responseData.data && responseData.data.response) {
            this.messages.push({
              text: responseData.data.response,
              sender: 'assistant',
              timestamp: new Date(),
            });
          } else {
            this.handleError('Received an empty response from the AI');
          }
          this.isProcessing = false;
        })
        .catch((error) => {
          console.error('Error calling Ollama API:', error);
          this.handleError('Failed to get a response from the AI assistant');
          this.isProcessing = false;
        });
    }
  };

  handleError(message: string): void {
    this.errorMessage = message;
    setTimeout(() => {
      this.errorMessage = '';
    }, 5000);
  }
}
