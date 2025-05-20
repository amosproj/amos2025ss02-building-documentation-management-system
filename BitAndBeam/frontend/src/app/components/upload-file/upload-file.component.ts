import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ConfigService } from '../../config.service';
import { SidebarComponent } from '../sidebar/sidebar.component';
import { FormsModule } from '@angular/forms';

import { RouterModule , Router } from '@angular/router';

@Component({
  selector: 'app-upload-file',
  standalone: true,
  imports: [CommonModule, RouterModule, SidebarComponent,FormsModule],
  templateUrl: './upload-file.component.html',
  styleUrls: ['./upload-file.component.css'],
})
export class UploadFileComponent implements OnInit {
  uploading = false;
  uploadSuccess = false;
  uploadError = '';
  selectedBuildingIndex: number = 0;
  buildings: { name: string; documents: string[] }[] = [];

  constructor(
    private config: ConfigService,
    private router: Router

) {}
  ngOnInit() {
    console.log('API URL from config service:', this.config.apiUrl);
  }
  uploadedFile: File | null = null; // Change to single file

  // Handle file selection
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


  // Handle drag-and-drop file selection
  onDrop(event: DragEvent) {
    event.preventDefault();
    if (event.dataTransfer?.files) {
      const file = event.dataTransfer.files[0]; // Only get the first file
      if (file) {
        this.uploadedFile = file;
      }
    }
  }

  // Handle drag over event (required for drop event to trigger)
  onDragOver(event: Event) {
    event.preventDefault();
  }

  assignFileToFolder() {
    if (this.uploadedFile && this.buildings[this.selectedBuildingIndex]) {
      this.buildings[this.selectedBuildingIndex].documents.push(this.uploadedFile.name);
      this.uploadedFile = null;
    }
  }

  createAndAssignFolder() {
    const name = prompt('New building name:');
    if (name?.trim()) {
      this.buildings.push({ name, documents: [this.uploadedFile?.name || ''] });
      this.selectedBuildingIndex = this.buildings.length - 1;
      this.uploadedFile = null;
    }
  }
}



