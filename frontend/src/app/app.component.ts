import { Component, ViewChild, ElementRef, OnInit } from '@angular/core';
import { RouterOutlet, Router } from '@angular/router';
import { NgIf, NgFor } from '@angular/common';
import { DocumentListComponent } from './document-list/document-list.component';
import { DocumentViewerComponent } from './document-viewer/document-viewer.component';
import { MetadataPanelComponent } from './metadata-panel/metadata-panel.component';
import { ChatbotComponent } from './chatbot/chatbot.component';
import { UploadFileComponent } from './upload-file/upload-file.component';
import { AuthService, User } from './auth/services/auth.service';
import { DocumentService } from './services/document.service';
import { takeUntil } from 'rxjs/operators';
import { Subject } from 'rxjs';

@Component({
  selector: 'app-root',
  imports: [
    RouterOutlet,
    NgIf,
    NgFor,
    DocumentListComponent,
    DocumentViewerComponent,
    MetadataPanelComponent,
    ChatbotComponent,
    UploadFileComponent
  ],
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss', './upload-styles.css']
})
export class AppComponent implements OnInit {
  title = 'frontend';
  uploadedFiles: File[] = [];
  @ViewChild('fileInput') fileInput!: ElementRef<HTMLInputElement>;
  isUploading = false;
  uploadProgress = 0;
  showFeedback = false;
  
  // Authentication properties
  isAuthenticated = false;
  currentUser: User | null = null;
  isInitialized = false;
  private destroy$ = new Subject<void>();
  feedbackMessage = '';

  constructor(
    private authService: AuthService, 
    private router: Router,
    private documentService: DocumentService
  ) {}

  ngOnInit(): void {
    // Subscribe to authentication state
    this.authService.isAuthenticated$
      .pipe(takeUntil(this.destroy$))
      .subscribe(isAuthenticated => {
        this.isAuthenticated = isAuthenticated;
        this.isInitialized = true;
      });

    // Subscribe to current user
    this.authService.currentUser$
      .pipe(takeUntil(this.destroy$))
      .subscribe(user => {
        this.currentUser = user;
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  logout(): void {
    this.authService.logout();
  }
  
  /**
   * Checks if the current route is a public route (home or login)
   * Used to conditionally display the main application layout
   */
  isPublicRoute(): boolean {
    const currentUrl = this.router.url;
    return currentUrl === '/' || currentUrl === '/login' || currentUrl === '/chat';
  }
  
  /**
   * Checks if the current route is the chat page
   * Used to conditionally display the upload button
   */
  isOnChatPage(): boolean {
    return this.router.url === '/chat';
  }

  triggerFileInput(): void {
    this.fileInput.nativeElement.click();
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      const files = Array.from(input.files);
      console.log('Files selected:', files);
      this.showFeedback = true;
      this.feedbackMessage = `Selected ${files.length} file(s): ${files.map(f => f.name).join(', ')}`;
      
      // Simulate upload process
      this.simulateFileUpload(files);
    }
  }
  
  simulateFileUpload(files: File[]): void {
    this.isUploading = true;
    this.uploadProgress = 0;
    
    // Simulate progress updates
    const interval = setInterval(() => {
      this.uploadProgress += 10;
      
      if (this.uploadProgress >= 100) {
        clearInterval(interval);
        this.isUploading = false;
        this.uploadProgress = 100;
        this.feedbackMessage = `Successfully uploaded ${files.length} file(s)`;
        
        // Add files to the document service so they can be accessed by other components
        this.documentService.addFiles(files);
        // Also update the local array for this component
        this.uploadedFiles = [...this.uploadedFiles, ...files];
        
        // Reset the input so the same file can be selected again if needed
        this.fileInput.nativeElement.value = '';
        
        // Hide feedback after 3 seconds
        setTimeout(() => {
          this.showFeedback = false;
        }, 3000);
      }
    }, 300); // Update progress every 300ms
  }
}
