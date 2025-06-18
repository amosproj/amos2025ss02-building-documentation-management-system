import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ConfigService } from '../../config.service';
import { SidebarComponent } from '../sidebar/sidebar.component';
import { FormsModule } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { HttpClientModule } from '@angular/common/http';
import type { AxiosResponse } from 'axios';
import { DocumentsApi, Building as ApiBuilding,  OllamaApi, Configuration, OllamaRequest } from '../../../api'; 
import { BuildingService } from '../../services/building.service';
import { CategoryService } from '../../services/category.service';
import { MarkdownBoldPipe } from '../../pipes/markdown-bold.pipe';
import { DocumentMetadataPopupComponent } from '../document-metadata-popup/document-metadata-popup.component';

@Component({
  selector: 'app-upload-file',
  standalone: true,
  imports: [CommonModule, RouterModule, SidebarComponent, FormsModule, HttpClientModule, MarkdownBoldPipe, DocumentMetadataPopupComponent],
  templateUrl: './upload-file.component.html',
  styleUrls: ['./upload-file.component.css'],
})
export class UploadFileComponent implements OnInit {
  uploading = false;
  uploadSuccess = false;
  uploadError = '';
  uploadedFile: File | null = null;
  selectedBuildingId: number | null = null;
  buildings: any[] = [];
  
  // Metadata popup properties
  showMetadataPopup = false;
  uploadedDocumentId: number | null = null;

  // AI Chat Properties
  showHistory: boolean = true; 
  userInput: string = '';
  messages: { sender: 'user' | 'ai', text: string }[] = [];
  errorMessage: string = '';

  private documentsApi: DocumentsApi;
  private ollamaApi: OllamaApi;


  constructor(
    private config: ConfigService,
    private router: Router,
    public buildingService: BuildingService,
    private categoryService: CategoryService
  ) {
    this.documentsApi = new DocumentsApi(new Configuration({ basePath: this.config.apiUrl }));
    this.ollamaApi = new OllamaApi(new Configuration({ basePath: this.config.apiUrl }));
  }

  ngOnInit() {
    this.buildingService.getBuildings().subscribe({
      next: (data) => this.buildings = data,
      error: (err) => console.error('Failed to fetch buildings', err)
    });
  }

  onFileSelected(event: any) {
    const file = event.target.files[0];
    if (!file) return;

    this.uploadedFile = file;
    this.uploadDocumentToServer(file);
  }

  onDrop(event: DragEvent) {
    event.preventDefault();
    if (event.dataTransfer?.files?.length) {
      const file = event.dataTransfer.files[0];
      this.uploadedFile = file;
      this.uploadDocumentToServer(file);
    }
  }

  onDragOver(event: Event) {
    event.preventDefault();
  }

  uploadDocumentToServer(file: File): void {
    this.documentsApi.apiDocumentsPost(file)
      .then((axiosResponse: AxiosResponse<any>) => {
        const documentId = axiosResponse.data?.documentId;

        if (!documentId) {
          this.uploadError = 'Upload succeeded but no document ID found in response body.';
          return;
        }

        this.uploadSuccess = true;
        this.uploadedDocumentId = documentId;
        
        // Show the metadata popup instead of navigating directly
        this.showMetadataPopup = true;
      })
      .catch(error => {
        this.uploading = false;
        this.uploadError = 'Upload failed: ' + error.message;
      });
  }

  extractDocumentIdFromLocation(location: string | undefined): number | null {
    if (!location) return null;
    const match = location.match(/\/(\d+)(\/)?$/);
    return match ? parseInt(match[1], 10) : null;
  }

  createBuildingAndUpload() {
    const name = prompt('New building name:');
    if (!name?.trim() || !this.uploadedFile) return;

    const building: Partial<ApiBuilding> = { name }; // Only the name for now

    this.buildingService.addBuilding(building).subscribe({
      next: (building) => {
        this.uploadDocumentToServer(this.uploadedFile!);
      },
      error: (err) => console.error('Failed to create building', err)
    });
  }


  // ✅ AI Chat Message Sender
  sendMessage() {
    const prompt = this.userInput.trim();
    if (!prompt) return;

    // Add user's message to history
    this.messages.push({ sender: 'user', text: prompt });
    this.userInput = '';
    this.errorMessage = '';

    // Full conversation context after current push
    const context = this.messages
      .map(msg => (msg.sender === 'user' ? 'User: ' : 'AI: ') + msg.text)
      .join('\n');

      const requestPayload: OllamaRequest = {
        prompt: prompt,
        context: context
      };

      this.ollamaApi.apiOllamaAskPost(requestPayload)
        .then((res) => {
          const responseText = (res.data as any)?.response || 'No response received.';
          this.messages.push({ sender: 'ai', text: responseText });
        })
        .catch((err: any) => {
          console.error('Error from AI API:', err);
          this.errorMessage = '⚠️ AI Assistant is not responding. Please try again later.';
        });

  }

  toggleHistory() {
      this.showHistory = !this.showHistory;
  }
  
  // Metadata popup handlers
  closeMetadataPopup(): void {
    this.showMetadataPopup = false;
    
    // If user closes the popup without saving, navigate to the document view
    if (this.uploadedDocumentId) {
      this.router.navigate(['/documents', this.uploadedDocumentId]);
    }
  }
  
  saveDocumentMetadata(metadata: {categoryId: number | null, buildingId: number | null}): void {
    if (this.uploadedDocumentId) {
      this.categoryService.assignDocumentCategory(
        this.uploadedDocumentId, 
        metadata.categoryId,
        metadata.buildingId
      ).subscribe({
        next: () => {
          this.showMetadataPopup = false;
          this.refreshDocuments(); // Refresh the document/building list after update
        },
        error: (err) => {
          console.error('Failed to assign document metadata', err);
          this.showMetadataPopup = false;
          this.refreshDocuments();
        }
      });
    }
  }

  // Refresh the building and document list after update
  refreshDocuments(): void {
    // Reload buildings (which include documents)
    this.buildingService.getBuildings().subscribe({
      next: (data) => this.buildings = data,
      error: (err) => console.error('Failed to refresh buildings', err)
    });
    // Optionally, reset upload state
    this.uploadedFile = null;
    this.uploadSuccess = false;
    this.uploadError = '';
    this.uploadedDocumentId = null;
  }

}
