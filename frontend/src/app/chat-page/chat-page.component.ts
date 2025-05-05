import { Component, ViewChild, ElementRef, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DocumentListComponent } from '../document-list/document-list.component';
import { DocumentViewerComponent } from '../document-viewer/document-viewer.component';
import { ChatbotComponent } from '../chatbot/chatbot.component';
import { DocumentService } from '../services/document.service';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

@Component({
  selector: 'app-chat-page',
  templateUrl: './chat-page.component.html',
  styleUrls: ['./chat-page.component.scss'],
  standalone: true,
  imports: [CommonModule, DocumentListComponent, DocumentViewerComponent, ChatbotComponent]
})
export class ChatPageComponent implements OnInit, OnDestroy {
  uploadedFiles: File[] = [];
  isUploading = false;
  uploadProgress = 0;
  showFeedback = false;
  feedbackMessage = '';
  selectedDocument: File | null = null;
  
  private destroy$ = new Subject<void>();
  
  constructor(private documentService: DocumentService) {}
  
  ngOnInit(): void {
    // Subscribe to the document service to get updated files
    this.documentService.uploadedFiles$
      .pipe(takeUntil(this.destroy$))
      .subscribe(files => {
        this.uploadedFiles = files;
      });
    
    // Initialize with any existing files
    this.uploadedFiles = this.documentService.getUploadedFiles();
  }
  
  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
  
  /**
   * Handle document selection from the document list
   */
  onDocumentSelected(file: File): void {
    this.selectedDocument = file;
    console.log('Selected document:', file.name);
  }
}
