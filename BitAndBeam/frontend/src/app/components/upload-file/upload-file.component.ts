import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ConfigService } from '../../config.service';
import { SidebarComponent } from '../sidebar/sidebar.component';
import { FormsModule } from '@angular/forms';
import { BuildingService } from '../../services/building.service';
import { RouterModule, Router } from '@angular/router';
import { HttpClient, HttpClientModule } from '@angular/common/http';
import { MarkdownBoldPipe } from '../../pipes/markdown-bold.pipe';  // Adjust relative path accordingly

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
  selectedBuildingIndex: number = 0;
  uploadedFile: File | null = null;
  showHistory: boolean = true; 

  // AI Chat Properties
  userInput: string = '';
  messages: { sender: 'user' | 'ai', text: string }[] = [];
  errorMessage: string = '';

  constructor(
    private config: ConfigService,
    private router: Router,
    public buildingService: BuildingService,
    private http: HttpClient
  ) {}

  ngOnInit() {
    console.log('API URL from config service:', this.config.apiUrl);
  }

  // File Upload Handlers
  onFileSelected(event: any) {
    const file = event.target.files[0];
    if (!file) return;

    this.uploading = true;
    this.uploadSuccess = false;
    this.uploadError = '';

    // Simulate upload with delay (replace this with actual HTTP upload later)
    setTimeout(() => {
      this.uploading = false;
      this.uploadSuccess = true;
      this.uploadedFile = file;
    }, 2000);
  }

  onDrop(event: DragEvent) {
    event.preventDefault();
    if (event.dataTransfer?.files) {
      const file = event.dataTransfer.files[0];
      if (file) {
        this.uploadedFile = file;
      }
    }
  }

  onDragOver(event: Event) {
    event.preventDefault();
  }

  assignFileToFolder() {
    if (this.uploadedFile && this.buildingService.getBuildings()[this.selectedBuildingIndex]) {
      this.buildingService.addDocumentToBuilding(this.selectedBuildingIndex, this.uploadedFile);
      this.uploadedFile = null;
    }
  }

  createAndAssignFolder() {
    const name = prompt('New building name:');
    if (name?.trim() && this.uploadedFile) {
      this.buildingService.addBuilding(name);
      const newIndex = this.buildingService.getBuildings().length - 1;
      this.buildingService.addDocumentToBuilding(newIndex, this.uploadedFile);
      this.selectedBuildingIndex = newIndex;
      this.uploadedFile = null;
    }
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

  this.http.post<any>('http://localhost:5001/api/Ollama/ask', {
    prompt: prompt,
    context: context
  }).subscribe({
    next: (response) => {
      this.messages.push({ sender: 'ai', text: response?.response || 'No response received.' });
    },
    error: (err) => {
      console.error('Error from AI API:', err);
      this.errorMessage = '⚠️ AI Assistant is not responding. Please try again later.';
    }
  });
}

  toggleHistory() {
      this.showHistory = !this.showHistory;
  }

}
