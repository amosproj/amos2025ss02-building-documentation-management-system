import { Component, ViewChild, ElementRef } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { NgIf, NgFor } from '@angular/common';
import { DocumentListComponent } from './document-list/document-list.component';
import { DocumentViewerComponent } from './document-viewer/document-viewer.component';
import { MetadataPanelComponent } from './metadata-panel/metadata-panel.component';
import { ChatbotComponent } from './chatbot/chatbot.component';
import { UploadFileComponent } from './upload-file/upload-file.component';

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
export class AppComponent {
  title = 'frontend';
  uploadedFiles: File[] = [];
  @ViewChild('fileInput') fileInput!: ElementRef<HTMLInputElement>;
  isUploading = false;
  uploadProgress = 0;
  showFeedback = false;
  feedbackMessage = '';

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
        
        // Add files to the uploadedFiles array for display
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
