import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ConfigService } from '../../config.service';
import { SidebarComponent } from '../sidebar/sidebar.component';
import { FormsModule } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { HttpClientModule } from '@angular/common/http';
import type { AxiosResponse } from 'axios';
import { DocumentsApi, Configuration } from '../../../api';
import { BuildingService } from '../../services/building.service';

@Component({
  selector: 'app-upload-file',
  standalone: true,
  imports: [CommonModule, RouterModule, SidebarComponent, FormsModule, HttpClientModule],
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

  private documentsApi: DocumentsApi;

  constructor(
    private config: ConfigService,
    private router: Router,
    public buildingService: BuildingService
  ) {
    const apiConfig = new Configuration({ basePath: this.config.apiUrl });
    this.documentsApi = new DocumentsApi(apiConfig);
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

}
