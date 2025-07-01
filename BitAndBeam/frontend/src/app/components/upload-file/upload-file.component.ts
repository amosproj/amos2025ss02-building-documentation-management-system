import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiClientFactory } from '../../services/api-client.factory';
import { DocumentsApi } from '../../../api';
import { SidebarComponent } from '../sidebar/sidebar.component';
import { AiAssistantComponent } from '../ai-assistant/ai-assistant.component';
import { DocumentMetadataPopupComponent } from '../document-metadata-popup/document-metadata-popup.component';
import type { AxiosProgressEvent, AxiosResponse } from 'axios';
import { SidebarRefreshService } from '../../services/sidebar-refresh.service';

@Component({
  selector: 'app-upload-file',
  templateUrl: './upload-file.component.html',
  styleUrls: ['./upload-file.component.css'],
  standalone: true,
  imports: [
    CommonModule,
    SidebarComponent,
    AiAssistantComponent,
    DocumentMetadataPopupComponent
  ]
})
export class UploadFileComponent implements OnInit {
  // For building association with documents
  selectedBuildingId: number | null = null;
  uploadedDocumentId: number | null = null;

  // File upload properties
  uploading = false;
  uploadProgress: number = 0;
  uploadedFile: File | null = null;
  isDragOver = false;
  errorMessage = '';
  successMessage = '';

  // Metadata popup control
  showMetadataPopup = false;

  private documentsApi: DocumentsApi;

  constructor(private apiFactory: ApiClientFactory, private sidebarRefreshService: SidebarRefreshService) {
    this.documentsApi = this.apiFactory.create<DocumentsApi>(DocumentsApi);
  }

  ngOnInit(): void {
    // You could fetch the buildings list here if needed
    // For now, we're using null which means no specific building is selected
  }

  /**
   * Handle drag over event for file upload
   */
  onDragOver(event: DragEvent): void {
    if (this.uploading) return;
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver = true;
  }

  /**
   * Handle drag leave event for file upload
   */
  onDragLeave(event: DragEvent): void {
    if (this.uploading) return;
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver = false;
  }

  /**
   * Handle file drop event for file upload
   */
  onDrop(event: DragEvent): void {
    if (this.uploading) return;
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver = false;

    if (event.dataTransfer?.files?.length) {
      const file = event.dataTransfer.files[0];
      this.uploadedFile = file;
      this.uploadFile(file);
    }
  }

  /**
   * Handle file selection from file input
   */
  onFileSelected(event: any): void {
    if (this.uploading) return;
    const file = event.target.files[0];
    if (!file) return;

    this.uploadedFile = file;
    this.uploadFile(file);
  }

  /**
   * Upload the selected file
   */
  uploadFile(file: File): void {
    if (!file) return;

    this.uploading = true;
    this.uploadProgress = 0;
    this.errorMessage = '';
    this.successMessage = '';

    // Pass the file directly as the API expects a File object, not FormData
    this.documentsApi.apiDocumentsPost(file, {
      onUploadProgress: (progressEvent: AxiosProgressEvent) => {
        if (progressEvent.total) {
          this.uploadProgress = Math.round((progressEvent.loaded * 100) / progressEvent.total);
        }
      }
    }).then((response: AxiosResponse<any>) => {
      console.log('Upload successful', response.data);
      this.uploading = false;
      this.uploadedDocumentId = response.data.id || response.data.documentId;
      this.successMessage = `File "${file.name}" uploaded successfully!`;

      // Associate the document with a building if needed
      if (this.selectedBuildingId && this.uploadedDocumentId) {
        this.associateDocumentWithBuilding(this.uploadedDocumentId, this.selectedBuildingId);
      }

      // Show metadata popup after successful upload
      this.showMetadataPopup = true;

      // Emit the uploaded document ID to the parent component or handle locally
      this.onFileUploaded(this.uploadedDocumentId!);

      // Clear success message after a delay
      setTimeout(() => {
        this.successMessage = '';
      }, 5000);

    }).catch((error: any) => {
      console.error('Upload failed', error);
      this.uploading = false;
      this.errorMessage = 'Upload failed: ' + (error.response?.data?.message || error.message || 'Unknown error');

      // Clear error message after a delay
      setTimeout(() => {
        this.errorMessage = '';
      }, 5000);
    });
  }

  /**
   * Associate the uploaded document with a building using the metadata PATCH endpoint
   */
  private associateDocumentWithBuilding(documentId: number, buildingId: number): void {
    const metadata = {
      buildingId: buildingId,
      categoryName: null // Keep the category as is or null if not set yet
    };

    this.documentsApi.apiDocumentsIdPatch(documentId, metadata)
      .then(() => {
        console.log(`Document ${documentId} associated with building ${buildingId}`);
      })
      .catch(error => {
        console.error('Failed to associate document with building:', error);
      });
  }

  /**
   * Update document ID after successful upload
   * This is called from within the uploadFile method after a successful upload
   */
  onFileUploaded(documentId: number): void {
    console.log('File uploaded with document ID:', documentId);
    this.uploadedDocumentId = documentId;
    // Trigger sidebar refresh after upload
    this.sidebarRefreshService.triggerRefresh();
    // Any additional actions needed after a file is uploaded
    // For example, you might want to update a list of documents here
  }

  /**
   * Close the metadata popup
   */
  closeMetadataPopup(): void {
    this.showMetadataPopup = false;
    this.uploadedDocumentId = null;
  }

  /**
   * Save document metadata
   */
  saveDocumentMetadata(metadata: any): void {
    if (this.uploadedDocumentId) {
      this.documentsApi.apiDocumentsIdPatch(this.uploadedDocumentId, metadata)
        .then(() => {
          console.log('Document metadata updated successfully');
          this.showMetadataPopup = false;
        })
        .catch(error => {
          console.error('Failed to update document metadata:', error);
        });
    }
  }
}
