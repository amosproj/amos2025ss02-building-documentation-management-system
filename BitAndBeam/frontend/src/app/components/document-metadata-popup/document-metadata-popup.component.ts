import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BuildingService, Building, DocumentResponse } from '../../services/building.service';
import { CategoryService, Category } from '../../services/category.service';
import { DocumentsApi, DocumentMetadataPatchRequest } from '../../../api/api';
import { ApiClientFactory } from '../../services/api-client.factory';
import { SidebarRefreshService }  from '../../services/sidebar-refresh.service';

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
  @Output() saveMetadata = new EventEmitter<{categoryName: string | null, buildingId: number | null}>();

  buildings: Building[] = [];
  categories: Category[] = [];
  selectedBuildingId: number | null = null;
  selectedCategoryName: string | null = null;
  isOtherCategory: boolean = false;
  otherCategoryName: string = '';
  readonly OTHER_CATEGORY_OPTION = 'other';

  // Notification properties
  showNotification: boolean = false;
  notificationMessage: string = '';
  notificationType: 'success' | 'error' = 'success';
  notificationTimeout: any = null;

  constructor(
    private buildingService: BuildingService,
    private categoryService: CategoryService,
    private apiFactory: ApiClientFactory,
    private sidebarRefreshService: SidebarRefreshService
) {
  }

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

    // If documentData has a categoryName, preselect that category
    if (this.documentData && this.documentData.categoryName !== undefined) {
      this.selectedCategoryName = this.documentData.categoryName;
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
      this.selectedCategoryName = null;
    } else {
      this.isOtherCategory = false;
      this.selectedCategoryName = value;
    }
  }

  createNewCategory(): void {
    if (!this.otherCategoryName.trim()) {
      alert('Please enter a category name');
      return;
    }

    this.categoryService.createCategory({ name: this.otherCategoryName }).subscribe({
      next: (newCategory: Category) => {
        this.loadCategories(); // reloads the list, avoids duplicates
        this.selectedCategoryName = newCategory.name;
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

    // Determine categoryName based on selection; null if manual input selected
    const categoryName: string | null = this.isOtherCategory ? this.otherCategoryName : this.selectedCategoryName;

    this.updateDocumentMetadata(this.documentId!, categoryName, this.selectedBuildingId);
  }


  private updateDocumentMetadata(documentId: number, categoryName: string | null, buildingId: number | null): void {
    // ✅ Rewritten: use only the OpenAPI client for PATCH (removed categoryService.assignDocumentCategory)
    const patchRequest: DocumentMetadataPatchRequest = {
      categoryName: null,
      buildingId
    };

    const documentsApi = this.apiFactory.create(DocumentsApi);

    documentsApi.apiDocumentsIdPatch(documentId, patchRequest).then(response => {
      // ✅ Emit metadata saved event
      this.saveMetadata.emit({ categoryName, buildingId });
      this.sidebarRefreshService.triggerRefresh(); // ✅ Refresh sidebar
      console.log('✅ Document metadata updated:', response.data);

      // Show success notification and close immediately
      this.showSuccessNotification('Metadata updated successfully');
      this.onClose(); // Close popup immediately after successful submission
    }).catch(error => {
      console.error('❌ Failed to update document metadata', error);
      this.showErrorNotification('Failed to update document metadata');
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
