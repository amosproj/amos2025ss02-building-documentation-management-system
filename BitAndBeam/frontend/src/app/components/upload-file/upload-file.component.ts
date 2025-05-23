import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ConfigService } from '../../config.service';
import { SidebarComponent } from '../sidebar/sidebar.component';
import { FormsModule } from '@angular/forms';
import { BuildingService } from '../../services/building.service'

import { RouterModule , Router } from '@angular/router';

import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { HttpClientModule } from '@angular/common/http';

@Component({
  selector: 'app-upload-file',
  standalone: true,
  imports: [CommonModule, RouterModule, SidebarComponent,FormsModule, HttpClientModule],
  templateUrl: './upload-file.component.html',
  styleUrls: ['./upload-file.component.css'],
})
export class UploadFileComponent implements OnInit {
  uploading = false;
  uploadSuccess = false;
  uploadError = '';
  selectedBuildingIndex: number = 0;

  constructor(
    private config: ConfigService,
    private router: Router,
    public buildingService: BuildingService,
    private http: HttpClient

) {}
  ngOnInit() {
    console.log('API URL from config service:', this.config.apiUrl);
  }
  uploadedFile: File | null = null; // Change to single file

  // Handle file selection
  onFileSelected(event: any) {
    const file = event.target.files[0];
    if (!file) return;

    this.uploadedFile = file;
    this.uploading = true;
    this.uploadSuccess = false;
    this.uploadError = '';

    const formData = new FormData();

    if(this.uploadedFile){
      formData.append('file', this.uploadedFile);
    }
    

    this.http.post(`${this.config.apiUrl}/api/documents`, formData).subscribe({
    next: () => {
      this.uploading = false;
      this.uploadSuccess = true;
    },
    error: (error: HttpErrorResponse) => {
      this.uploading = false;
      this.uploadError = 'Upload failed: ' + error.message;
    }
  });

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

}



