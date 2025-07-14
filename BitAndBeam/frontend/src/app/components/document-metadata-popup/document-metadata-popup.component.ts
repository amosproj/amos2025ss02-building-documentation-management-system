import {
  Component,
  EventEmitter,
  Input,
  OnInit,
  Output,
  OnDestroy,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  BuildingService,
  Building,
  DocumentResponse,
} from '../../services/building.service';
import { CategoryService, Category } from '../../services/category.service';
import { DocumentsApi, DocumentMetadataPatchRequest } from '../../../api/api';
import { ApiClientFactory } from '../../services/api-client.factory';
import { SidebarRefreshService } from '../../services/sidebar-refresh.service';

@Component({
  selector: 'app-document-metadata-popup',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './document-metadata-popup.component.html',
  styleUrls: ['./document-metadata-popup.component.css'],
})
export class DocumentMetadataPopupComponent implements OnInit, OnDestroy {
  @Input() documentId: number | null = null;
  @Input() documentName: string = '';
  @Input() documentData: DocumentResponse | null = null;
  @Input() suggestedCategoryName: string | null = null;
  @Input() suggestedBuildingId: number | null = null;
  @Output() closePopup = new EventEmitter<void>();
  @Output() saveMetadata = new EventEmitter<{
    categoryName: string | null;
    buildingId: number | null;
  }>();

  // Initialize as empty arrays to prevent undefined errors
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

  // Loading state for key information extraction
  isExtractingKeyInfo: boolean = false;

  constructor(
    private buildingService: BuildingService,
    private categoryService: CategoryService,
    private apiFactory: ApiClientFactory,
    private sidebarRefreshService: SidebarRefreshService,
  ) {}

  ngOnInit(): void {
    console.log('🔄 DocumentMetadataPopup ngOnInit called');
    this.loadCategories();
    this.loadBuildings();
  }

  ngOnDestroy(): void {
    this.clearNotificationTimeout();
  }

  setInitialValues(): void {
    console.log('🔍 Setting initial values:');
    console.log('📋 DocumentData:', this.documentData);
    console.log('🏷️ SuggestedCategoryName:', this.suggestedCategoryName);
    console.log('🏢 SuggestedBuildingId:', this.suggestedBuildingId);
    console.log('📂 Available categories:', this.categories.length);

    // Priority 1: Use suggested values from upload response (for new uploads)
    if (
      this.suggestedCategoryName !== null &&
      this.suggestedCategoryName !== undefined
    ) {
      this.selectedCategoryName = this.suggestedCategoryName;
      console.log('✅ Using suggested category:', this.suggestedCategoryName);
    }
    // Priority 2: Use existing document data (for editing existing documents)
    else if (
      this.documentData?.categoryName !== null &&
      this.documentData?.categoryName !== undefined
    ) {
      this.selectedCategoryName = this.documentData.categoryName;
      console.log(
        '✅ Using existing document category:',
        this.documentData.categoryName,
      );
    }
    // Priority 3: No category selected
    else {
      this.selectedCategoryName = null;
      console.log('⚪ No category pre-selected');
    }

    // Similar logic for building
    if (
      this.suggestedBuildingId !== null &&
      this.suggestedBuildingId !== undefined
    ) {
      this.selectedBuildingId = this.suggestedBuildingId;
      console.log('✅ Using suggested building ID:', this.suggestedBuildingId);
    } else if (
      this.documentData?.buildingId !== null &&
      this.documentData?.buildingId !== undefined
    ) {
      this.selectedBuildingId = this.documentData.buildingId;
      console.log(
        '✅ Using existing document building ID:',
        this.documentData.buildingId,
      );
    } else {
      this.selectedBuildingId = null;
      console.log('⚪ No building pre-selected');
    }
  }

  loadBuildings(): void {
    console.log('🏢 Loading buildings...');
    this.buildingService.getBuildings().subscribe({
      next: (data) => {
        console.log('✅ BuildingService response:', data);
        // Ensure buildings is always an array
        this.buildings = Array.isArray(data) ? data : [];
        console.log(
          '✅ Loaded buildings:',
          this.buildings.length,
          this.buildings,
        );
      },
      error: (err) => {
        console.error('❌ Failed to fetch buildings', err);
        this.buildings = [];
      },
    });
  }

  loadCategories(): void {
    console.log('🏷️ Loading categories...');

    // Primary method: Try using DocumentsApi directly
    const documentsApi = this.apiFactory.create(DocumentsApi);
    documentsApi
      .apiDocumentsCategoriesGet()
      .then((response: any) => {
        console.log('✅ Raw categories response:', response);

        // Safely extract categories from response
        this.categories = this.extractCategoriesFromResponse(response);

        console.log(
          '✅ Processed categories:',
          this.categories.length,
          this.categories,
        );

        // Set initial values after categories are loaded
        this.setInitialValues();
      })
      .catch((err: any) => {
        console.error('❌ DocumentsApi categories failed:', err);

        // Fallback: Try CategoryService
        console.log('🔄 Trying CategoryService as fallback...');
        this.categoryService.getCategories().subscribe({
          next: (data) => {
            console.log('✅ CategoryService response:', data);
            // Ensure categories is always an array
            this.categories = Array.isArray(data) ? data : [];
            this.setInitialValues();
          },
          error: (fallbackErr) => {
            console.error('❌ CategoryService also failed:', fallbackErr);

            // Last resort: Hardcode categories for testing
            console.log('🔧 Using hardcoded categories as last resort');
            this.categories = [
              {
                name: 'Energieausweis',
                description: 'Energy certificate documents',
              },
              {
                name: 'Antrag auf Baugenehmigung',
                description: 'Building permit applications',
              },
              { name: 'Bebauungsplan', description: 'Development plans' },
            ];
            this.setInitialValues();
          },
        });
      });
  }

  /**
   * Safely extract categories array from various response formats
   */
  private extractCategoriesFromResponse(response: any): Category[] {
    if (!response) return [];

    let categoriesData = response.data || response;

    // If the response has a categories property, use it
    if (categoriesData.categories && Array.isArray(categoriesData.categories)) {
      return categoriesData.categories;
    }

    // If the data itself is an array, use it
    if (Array.isArray(categoriesData)) {
      return categoriesData;
    }

    // If it's an object with numeric keys (like {0: {...}, 1: {...}}), convert to array
    if (
      categoriesData &&
      typeof categoriesData === 'object' &&
      !Array.isArray(categoriesData)
    ) {
      const keys = Object.keys(categoriesData);
      const isNumericKeys = keys.every((key) => !isNaN(Number(key)));
      if (isNumericKeys) {
        return keys
          .map((key) => categoriesData[key])
          .filter((item) => item && typeof item === 'object');
      }
    }

    // Default to empty array
    return [];
  }

  onCategoryChange(value: string | null): void {
    console.log('🔄 Category changed to:', value);
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
      this.showErrorNotification('Please enter a category name');
      return;
    }

    this.categoryService
      .createCategory({ name: this.otherCategoryName })
      .subscribe({
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
        },
      });
  }

  onClose(): void {
    this.clearNotificationTimeout();
    this.closePopup.emit();
  }

  onSave(): void {
    if (!this.documentId) {
      this.showErrorNotification('No document ID available');
      return;
    }

    // Determine categoryName based on selection
    const categoryName: string | null = this.isOtherCategory
      ? this.otherCategoryName
      : this.selectedCategoryName;

    console.log(
      '💾 Saving with category:',
      categoryName,
      'and building:',
      this.selectedBuildingId,
    );

    // Show processing message immediately
    this.isExtractingKeyInfo = true;

    // Make both API calls in parallel if category is selected
    if (categoryName && categoryName.trim()) {
      this.saveWithKeyExtraction(
        this.documentId,
        categoryName,
        this.selectedBuildingId,
      );
    } else {
      // Just update metadata without key extraction
      this.updateDocumentMetadata(
        this.documentId,
        categoryName,
        this.selectedBuildingId,
      );
    }
  }

  /**
   * Save metadata and extract key information with better error handling
   */
  private saveWithKeyExtraction(
    documentId: number,
    categoryName: string,
    buildingId: number | null,
  ): void {
    const documentsApi = this.apiFactory.create(DocumentsApi);

    // Prepare patch request
    const patchRequest: DocumentMetadataPatchRequest = {
      categoryName: categoryName,
      buildingId: buildingId,
    };

    console.log('🔄 Saving document metadata...');

    // First save the metadata
    documentsApi
      .apiDocumentsIdPatch(documentId, patchRequest)
      .then((metadataResponse) => {
        console.log('✅ Document metadata saved successfully');

        // Now try to extract key information
        console.log('🔍 Attempting to extract key information...');

        this.extractKeyInformationPromise(documentId, categoryName)
          .then((extractionResponse) => {
            console.log('✅ Key information extracted successfully');
            this.isExtractingKeyInfo = false;
            this.showSuccessNotification('Document processed successfully!');

            setTimeout(() => {
              this.completeSave(categoryName, buildingId);
            }, 500);
          })
          .catch((extractionError) => {
            console.warn(
              '⚠️ Key extraction failed, but document was saved:',
              extractionError,
            );
            this.isExtractingKeyInfo = false;
            // Don't show error - document was saved successfully
            this.showSuccessNotification('Document saved successfully!');

            setTimeout(() => {
              this.completeSave(categoryName, buildingId);
            }, 500);
          });
      })
      .catch((error) => {
        console.error('❌ Failed to save document metadata:', error);
        this.isExtractingKeyInfo = false;
        this.showErrorNotification('Failed to save document');
      });
  }

  /**
   * Extract key information as a promise
   */
  private async extractKeyInformationPromise(
    documentId: number,
    categoryName: string,
  ): Promise<any> {
    const documentsApi = this.apiFactory.create(DocumentsApi);

    try {
      const response =
        await documentsApi.apiDocumentsIdExtractKeyInformationPost(documentId, {
          categoryName: categoryName,
        });
      console.log('✅ Key information extraction response:', response);
      return response;
    } catch (error) {
      console.error('❌ Key information extraction failed:', error);
      return await Promise.reject('Key information extraction failed');
    }
  }

  private updateDocumentMetadata(
    documentId: number,
    categoryName: string | null,
    buildingId: number | null,
  ): void {
    const patchRequest: DocumentMetadataPatchRequest = {
      categoryName: categoryName,
      buildingId: buildingId,
    };

    console.log('🔄 Sending PATCH request:', patchRequest);

    const documentsApi = this.apiFactory.create(DocumentsApi);

    documentsApi
      .apiDocumentsIdPatch(documentId, patchRequest)
      .then((response) => {
        console.log('✅ Document metadata updated:', response.data);
        this.isExtractingKeyInfo = false;
        this.completeSave(categoryName, buildingId);
      })
      .catch((error) => {
        console.error('❌ Failed to update document metadata', error);
        this.isExtractingKeyInfo = false;
        this.showErrorNotification('Failed to update document metadata');
      });
  }

  private completeSave(
    categoryName: string | null,
    buildingId: number | null,
  ): void {
    // Emit metadata saved event
    this.saveMetadata.emit({ categoryName, buildingId });
    this.sidebarRefreshService.triggerRefresh();

    // Close popup immediately
    this.onClose();
  }

  /**
   * Show a success notification
   */
  private showSuccessNotification(message: string): void {
    this.notificationType = 'success';
    this.notificationMessage = message;
    this.showNotification = true;
    this.clearNotificationTimeout();

    // Don't auto-hide if we're processing
    if (!this.isExtractingKeyInfo) {
      this.notificationTimeout = setTimeout(() => {
        this.showNotification = false;
      }, 3000);
    }
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
   * TrackBy function for categories to improve performance
   */
  trackByCategory(index: number, category: Category): any {
    return category.name || index;
  }

  /**
   * Get building name by ID for display purposes
   */
  getBuildingName(buildingId: number | null): string {
    if (!buildingId) return 'Unknown Building';
    const building = this.buildings.find((b: Building) => b.id === buildingId);
    return building ? building.name : `Building #${buildingId}`;
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
