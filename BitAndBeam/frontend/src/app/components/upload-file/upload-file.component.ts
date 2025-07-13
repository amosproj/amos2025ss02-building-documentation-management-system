import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiClientFactory } from '../../services/api-client.factory';
import { DocumentsApi } from '../../../api';
import { SidebarComponent } from '../sidebar/sidebar.component';
import { AiAssistantComponent } from '../ai-assistant/ai-assistant.component';
import { DocumentMetadataPopupComponent } from '../document-metadata-popup/document-metadata-popup.component';
import type { AxiosProgressEvent, AxiosResponse } from 'axios';
import { SidebarRefreshService } from '../../services/sidebar-refresh.service';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

interface UploadResponse {
  documentId: number;
  fileUrl: string;
  hasMetadata: boolean;
  suggestedAddress: {
    street: string;
    house_number: string;
    zip_code: string;
    city: string;
  };
  suggestedBuildingId?: number;
  suggestedBuildingName?: string;
  suggestedCategoryName?: string;
  categoryName?: string;
  keyInformation: any;
}

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
export class UploadFileComponent implements OnInit, OnDestroy {
  // For building association with documents
  selectedBuildingId: number | null = null;
  uploadedDocumentId: number | null = null;
  uploadResponse: UploadResponse | null = null;

  // File upload properties
  uploading = false;
  uploadProgress: number = 0;
  uploadedFile: File | null = null;
  isDragOver = false;
  errorMessage = '';
  successMessage = '';

  // Metadata popup control
  showMetadataPopup = false;

  // Cleanup
  private destroy$ = new Subject<void>();

  private documentsApi: DocumentsApi;

  constructor(
    private apiFactory: ApiClientFactory, 
    private sidebarRefreshService: SidebarRefreshService
  ) {
    this.documentsApi = this.apiFactory.create<DocumentsApi>(DocumentsApi);
  }

  ngOnInit(): void {
    // Subscribe to any necessary services with proper cleanup
    // You could fetch the buildings list here if needed
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
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
    
    // Check if we're actually leaving the drag zone
    const target = event.target as HTMLElement;
    const relatedTarget = event.relatedTarget as HTMLElement;
    
    if (!target.contains(relatedTarget)) {
      this.isDragOver = false;
    }
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
      this.validateAndUploadFile(file);
    }
  }

  /**
   * Handle file selection from file input
   */
  onFileSelected(event: Event): void {
    if (this.uploading) return;
    
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    
    if (file) {
      this.validateAndUploadFile(file);
    }
    
    // Reset input value to allow re-upload of the same file
    input.value = '';
  }

  /**
   * Validate file before uploading
   */
  private validateAndUploadFile(file: File): void {
    // Validate file size (e.g., max 50MB)
    const maxSize = 50 * 1024 * 1024; // 50MB in bytes
    if (file.size > maxSize) {
      this.showError('File size exceeds 50MB limit');
      return;
    }

    // Validate file type
    const allowedTypes = [
      'application/pdf',
      'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
      'text/plain',
      'image/png',
      'image/jpeg',
      'image/jpg'
    ];

    if (!allowedTypes.includes(file.type)) {
      // Check by extension as fallback
      const allowedExtensions = ['.pdf', '.docx', '.txt', '.png', '.jpg', '.jpeg'];
      const fileName = file.name.toLowerCase();
      const hasAllowedExtension = allowedExtensions.some(ext => fileName.endsWith(ext));
      
      if (!hasAllowedExtension) {
        this.showError('Invalid file type. Please upload PDF, DOCX, TXT, PNG, or JPG files.');
        return;
      }
    }

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

    console.log('📤 Starting file upload:', file.name);

    // Pass the file directly as the API expects a File object, not FormData
    this.documentsApi.apiDocumentsPost(file, {
      onUploadProgress: (progressEvent: AxiosProgressEvent) => {
        if (progressEvent.total) {
          this.uploadProgress = Math.round((progressEvent.loaded * 100) / progressEvent.total);
          console.log(`📊 Upload progress: ${this.uploadProgress}%`);
        }
      }
    }).then((response: AxiosResponse<any>) => {
      console.log('✅ Upload successful:', response.data);
      this.handleUploadSuccess(response.data, file.name);
    }).catch((error: any) => {
      console.error('❌ Upload failed:', error);
      this.handleUploadError(error);
    });
  }

  /**
   * Handle successful upload
   */
  private handleUploadSuccess(data: any, fileName: string): void {
    this.uploading = false;
    
    // Store the complete upload response
    this.uploadResponse = data as UploadResponse;
    this.uploadedDocumentId = data.documentId;
    
    // Build success message with AI suggestions
    let suggestionInfo = '';
    if (data.suggestedCategoryName) {
      suggestionInfo += ` AI suggested category: ${data.suggestedCategoryName}.`;
    }
    if (data.suggestedBuildingName) {
      suggestionInfo += ` AI suggested building: ${data.suggestedBuildingName}.`;
    }
    
    this.successMessage = `File "${fileName}" uploaded successfully!${suggestionInfo}`;

    // Show metadata popup after successful upload
    this.showMetadataPopup = true;

    // Update sidebar if document ID exists
    if (data.documentId) {
      this.onFileUploaded(data.documentId);
    }

    // Clear success message after delay
    setTimeout(() => {
      this.successMessage = '';
    }, 8000);
  }

  /**
   * Handle upload error
   */
  private handleUploadError(error: any): void {
    this.uploading = false;
    this.uploadResponse = null;
    
    // Extract error message
    let errorMsg = 'Upload failed: ';
    
    if (error.response?.data?.message) {
      errorMsg += error.response.data.message;
    } else if (error.response?.data?.error) {
      errorMsg += error.response.data.error;
    } else if (error.message) {
      errorMsg += error.message;
    } else {
      errorMsg += 'Unknown error occurred';
    }
    
    // Handle specific error codes
    if (error.response?.status === 413) {
      errorMsg = 'File too large. Please upload a smaller file.';
    } else if (error.response?.status === 415) {
      errorMsg = 'Unsupported file type.';
    } else if (error.response?.status === 401) {
      errorMsg = 'Authentication failed. Please log in again.';
    }
    
    this.showError(errorMsg);
  }

  /**
   * Show error message
   */
  private showError(message: string): void {
    this.errorMessage = message;
    
    // Clear error message after delay
    setTimeout(() => {
      this.errorMessage = '';
    }, 5000);
  }

  /**
   * Update document ID after successful upload
   */
  onFileUploaded(documentId: number | null): void {
    if (!documentId) {
      console.log('⚠️ No document ID provided to onFileUploaded');
      return;
    }
    
    console.log('📄 File uploaded with document ID:', documentId);
    this.uploadedDocumentId = documentId;
    
    // Trigger sidebar refresh after upload
    this.sidebarRefreshService.triggerRefresh();
  }

  /**
   * Close the metadata popup
   */
  closeMetadataPopup(): void {
    console.log('❌ Closing metadata popup');
    this.showMetadataPopup = false;
    
    // Reset upload state
    setTimeout(() => {
      this.uploadedDocumentId = null;
      this.uploadResponse = null;
      this.uploadedFile = null;
    }, 300); // Small delay for animation
  }

  /**
   * Save document metadata - handled by popup component
   */
  saveDocumentMetadata(metadata: {categoryName: string | null, buildingId: number | null}): void {
    console.log('💾 Document metadata saved:', metadata);
    
    // Trigger sidebar refresh to show updated document
    this.sidebarRefreshService.triggerRefresh();
    
    // Close the popup
    this.closeMetadataPopup();
  }

  /**
   * Get the document name for the popup
   */
  get documentName(): string {
    return this.uploadedFile?.name || 'Unknown Document';
  }

  /**
   * Get suggested category name from upload response
   */
  get suggestedCategoryName(): string | null {
    return this.uploadResponse?.suggestedCategoryName || null;
  }

  /**
   * Get suggested building ID from upload response
   */
  get suggestedBuildingId(): number | null {
    return this.uploadResponse?.suggestedBuildingId || null;
  }
}