import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ConfigService } from '../../config.service';
import { SidebarComponent } from '../sidebar/sidebar.component';
import { FormsModule } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { HttpClientModule } from '@angular/common/http';
import type { AxiosResponse } from 'axios';
import { DocumentsApi, OllamaApi, Configuration, OllamaRequest } from '../../../api';
import { BuildingService } from '../../services/building.service';
import { MarkdownBoldPipe } from '../../pipes/markdown-bold.pipe';

@Component({
  selector: 'app-upload-file',
  standalone: true,
  imports: [CommonModule, RouterModule, SidebarComponent, FormsModule, HttpClientModule, MarkdownBoldPipe],
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
    public buildingService: BuildingService
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
        this.router.navigate(['/documents', documentId]);
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

    this.buildingService.addBuilding(name).subscribe({
      next: (building) => {
       // this.selectedBuildingId = building.id;
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

}
