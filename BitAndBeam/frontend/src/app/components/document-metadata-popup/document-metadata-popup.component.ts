import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BuildingService, Building, DocumentResponse } from '../../services/building.service';
import { CategoryService, Category } from '../../services/category.service';
import { DocumentsApi, DocumentMetadataPatchRequest } from '../../../api/api';


@Component({
  selector: 'app-document-metadata-popup',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './document-metadata-popup.component.html',
  styleUrls: ['./document-metadata-popup.component.css']
})
export class DocumentMetadataPopupComponent implements OnInit {
  @Input() documentId: number | null = null;
  @Input() documentName: string = '';
  @Input() documentData: DocumentResponse | null = null;
  @Output() closePopup = new EventEmitter<void>();
  @Output() saveMetadata = new EventEmitter<{categoryId: number | null, buildingId: number | null}>();
  
  buildings: Building[] = [];
  categories: Category[] = [];
  selectedBuildingId: number | null = null;
  selectedCategoryId: number | null = null;
  isOtherCategory: boolean = false;
  otherCategoryName: string = '';
  readonly OTHER_CATEGORY_OPTION = 'other';
  documentsApi = new DocumentsApi();

  // Notification properties
  showNotification: boolean = false;
  notificationMessage: string = '';
  notificationType: 'success' | 'error' = 'success';
  notificationTimeout: any = null;
  
  constructor(
    private buildingService: BuildingService,
    private categoryService: CategoryService
  ) {}
  
  ngOnInit(): void {
    this.loadBuildings();
    this.loadCategories();
    this.setInitialValues();
  }
  
  setInitialValues(): void {
    // If documentData is provided and has a buildingId, preselect that building
    if (this.documentData && this.documentData.buildingId !== undefined) {
      this.selectedBuildingId = this.documentData.buildingId;
    } else {
      // Otherwise preselect "No Building"
      this.selectedBuildingId = null;
    }
    
    // If documentData has a categoryId, preselect that category
    if (this.documentData && this.documentData.categoryId !== undefined) {
      this.selectedCategoryId = this.documentData.categoryId;
    }
  }
  
  loadBuildings(): void {
    this.buildingService.getBuildings().subscribe({
      next: (data) => this.buildings = data,
      error: (err) => console.error('Failed to fetch buildings', err)
    });
  }
  
  loadCategories(): void {
    this.categoryService.getCategories().subscribe({
      next: (data) => this.categories = data,
      error: (err) => console.error('Failed to fetch categories', err)
    });
  }
  
  onCategoryChange(value: string | null): void {
    if (value === this.OTHER_CATEGORY_OPTION) {
      this.isOtherCategory = true;
      this.selectedCategoryId = null;
    } else {
      this.isOtherCategory = false;
    }
  }
  
  createNewCategory(): void {
    if (!this.otherCategoryName.trim()) {
      alert('Please enter a category name');
      return;
    }
    
    this.categoryService.createCategory({ name: this.otherCategoryName }).subscribe({
      next: (newCategory: Category) => {
        this.categories.push(newCategory);
        this.selectedCategoryId = newCategory.id;
        this.isOtherCategory = false;
        this.otherCategoryName = '';
        this.showSuccessNotification('New category created successfully');
      },
      error: (err: Error) => {
        console.error('Failed to create category', err);
        this.showErrorNotification('Failed to create category');
      }
    });
  }
  
  onClose(): void {
    this.closePopup.emit();
  }
  
  onSave(): void {
    if (!this.documentId) {
      alert('No document ID available');
      return;
    }

    // Determine categoryId based on selection; null if manual input selected
    const categoryId: number | null = this.isOtherCategory ? null : this.selectedCategoryId;



    this.updateDocumentMetadata(this.documentId!, categoryId, this.selectedBuildingId);
  }


  private updateDocumentMetadata(documentId: number, categoryId: number | null, buildingId: number | null): void {
    // Use OpenAPI client to update document metadata
    this.categoryService.assignDocumentCategory(documentId, categoryId, buildingId).subscribe({
      next: () => {
        // Emit event for parent components that may need to know about the update
        this.saveMetadata.emit({
          categoryId: categoryId,
          buildingId: buildingId
        });
        const patchRequest: DocumentMetadataPatchRequest = {
          categoryId: categoryId, // number or undefined
          buildingId: buildingId  // number or undefined
        };
        this.documentsApi.apiDocumentsIdPatch(documentId, patchRequest).then(response => {
            // handle success, e.g. response.data
            console.log('Document metadata updated:', response.data);
            // Show success notification
            this.showSuccessNotification('Metadata updated successfully');
            
            // Close the popup after a short delay to allow user to see the message
            setTimeout(() => this.onClose(), 1500);
          })
          .catch(error => {
              console.error('Failed to update document metadata', error);
          });
      },
      error: (err) => {
        console.error(err);
        this.showErrorNotification('Failed to update document metadata');
      }
    });
  }
  
  /**
   * Show a success notification
   */
  private showSuccessNotification(message: string): void {
    this.notificationType = 'success';
    this.notificationMessage = message;
    this.showNotification = true;
    this.clearNotificationTimeout();
    this.notificationTimeout = setTimeout(() => {
      this.showNotification = false;
    }, 5000);
  }

  /**
   * Show an error notification
   */
  private showErrorNotification(message: string): void {
    this.notificationType = 'error';
    this.notificationMessage = message;
    this.showNotification = true;
    this.clearNotificationTimeout();
    this.notificationTimeout = setTimeout(() => {
      this.showNotification = false;
    }, 5000);
  }

  /**
   * Clear any existing notification timeout
   */
  private clearNotificationTimeout(): void {
    if (this.notificationTimeout) {
      clearTimeout(this.notificationTimeout);
      this.notificationTimeout = null;
    }
  }
}
