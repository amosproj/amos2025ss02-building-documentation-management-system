import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { PdfViewerModule } from 'ng2-pdf-viewer';
import { ConfigService } from '../../config.service';
import { SidebarComponent} from '../../components/sidebar/sidebar.component';
import { TopBarComponent } from '../../components/top-bar/top-bar.component';
import { BuildingService, DocumentItem, DocumentResponse } from '../../services/building.service';
import { Configuration, DocumentsApi, Document as ApiDocument, DocumentMetadataPatchRequest } from '../../../api';
import { CategoryService, Category } from '../../services/category.service';
import { ApiClientFactory } from '../../services/api-client.factory';
import { SidebarRefreshService } from '../../services/sidebar-refresh.service';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { SessionService } from '../../services/session.service';
import { AiAssistantComponent } from '../../components/ai-assistant/ai-assistant.component';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { from } from 'rxjs';

@Component({
  standalone: true,
  selector: 'app-file-view',
  templateUrl: './file-view.component.html',
  styleUrls: ['./file-view.component.css'],
  imports: [CommonModule, PdfViewerModule, SidebarComponent, TopBarComponent, FormsModule, AiAssistantComponent]
})
export class FileViewComponent implements OnInit, OnDestroy {
  selectedFile: DocumentItem | null = null;
  notFound = false;
  isPdf = false;
  isImage = false;
  buildings: any[] = [];
  categories: Category[] = []; // Current categories in dropdown
  allCategories: Category[] = []; // All available categories
  selectedBuildingId: number | null = null;
  originalBuildingId: number | null = null;
  selectedCategoryName: string | null = null;
  originalCategoryName: string | null = null;
  loading = false;
  toastMessage = '';
  isMetadataPanelCollapsed = false;
  showToolbar = false;
  imageZoom = 1;
  pdfZoom = 1;

  // ✅ Key info variables
  metadataRaw: string = '';
  parsedMetadata: { label: string; value: string }[] = [];
  keyInformation: { label: string; value: string | null }[] = [];
  originalKeyInformation: { [key: string]: string | null } = {};
  touchedFields: { [label: string]: boolean } = {};
  loadingKeyInfo: boolean = false;
  keyInfo: any = null;
  hasChanges: boolean = false;

  // ✅ Analysis variables
  isAnalyzing: boolean = false;
  analysisMessage: string = '';
  analysisSuccess: boolean = true;

  // Cleanup
  private destroy$ = new Subject<void>();
  private blobUrl: string | null = null;

  constructor(
    private config: ConfigService,
    private route: ActivatedRoute,
    private router: Router,
    private buildingService: BuildingService,
    private categoryService: CategoryService,
    private apiFactory: ApiClientFactory,
    private sidebarRefreshService: SidebarRefreshService,
    private http: HttpClient,
    private session: SessionService,
  ) {
    // Initialize arrays to prevent undefined errors
    this.buildings = [];
    this.categories = [];
    this.allCategories = [];
  }

  ngOnInit(): void {
    document.addEventListener('mousemove', this.handleMouseMove);
    // Watch for route param changes
    this.route.paramMap.pipe(takeUntil(this.destroy$)).subscribe((paramMap) => {
      const idParam = paramMap.get('id');
      const id = Number(idParam);

      if (!idParam || isNaN(id)) {
        console.error('❌ Invalid document ID in route:', idParam);
        this.notFound = true;
        return;
      }

      this.notFound = false;
      this.selectedFile = null;
      this.isPdf = false;
      this.isImage = false;

      // Load buildings and categories first
      this.loadBuildingsAndCategories();
      this.loadDocument(id);
    });
  }

  /**
   * ✅ Fixed load buildings and categories - same approach as popup component
   */
  private loadBuildingsAndCategories(): void {
    // Load buildings
    this.buildingService
      .getBuildings()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (buildings) => {
          this.buildings = Array.isArray(buildings) ? buildings : [];
          console.log('✅ Loaded buildings:', this.buildings.length);
        },
        error: (err) => {
          console.error('❌ Failed to load buildings:', err);
          this.buildings = [];
        },
      });

    // ✅ Enhanced categories loading - SAME AS POPUP COMPONENT
    this.loadCategories();
  }

  /**
   * ✅ Load categories using same approach as document-metadata-popup
   */
  private loadCategories(): void {
    console.log('🏷️ Loading categories...');

    // Primary method: Try using DocumentsApi directly
    const documentsApi = this.apiFactory.create(DocumentsApi);
    documentsApi
      .apiDocumentsCategoriesGet()
      .then((response: any) => {
        // Safely extract categories from response
        this.allCategories = this.extractCategoriesFromResponse(response);
        this.categories = [...this.allCategories]; // Copy all categories for dropdown
      })
      .catch((err: any) => {
        console.error('❌ DocumentsApi categories failed:', err);

        // Fallback: Try CategoryService
        this.categoryService
          .getCategories()
          .pipe(takeUntil(this.destroy$))
          .subscribe({
            next: (data) => {
              // Ensure categories is always an array
              this.allCategories = Array.isArray(data) ? data : [];
              this.categories = [...this.allCategories];
            },
            error: (fallbackErr) => {
              console.error('❌ CategoryService also failed:', fallbackErr);
            },
          });
      });
  }

  /**
   * ✅ Safely extract categories array from various response formats - SAME AS POPUP
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

  /**
   * ✅ Enhanced loadDocument method
   */
  loadDocument(id: number): void {
    this.buildingService
      .getDocumentById(id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (doc: ApiDocument) => {
          this.metadataRaw = doc.metadata ?? '';
          if (doc.metadata) {
            try {
              const entries = doc.metadata
                .split(/[\r\n]+/)
                .map((line) => {
                  const separator = line.includes('=')
                    ? '='
                    : line.includes(',')
                      ? ','
                      : ':';
                  const [key, ...rest] = line
                    .split(separator)
                    .map((s) => s.trim());
                  const value = rest.join(separator);
                  return [key, value];
                })
                .filter((arr) => arr.length >= 2);

              const metadataObject: { [key: string]: string } = {};
              entries.forEach(([key, value]) => {
                metadataObject[key] = value;
              });

              this.parsedMetadata = [
                {
                  label: 'Title',
                  value: metadataObject['resourceName'] || 'N/A',
                },
                {
                  label: 'Author',
                  value:
                    metadataObject['dc:creator'] ||
                    metadataObject['pdf:docinfo:creator'] ||
                    'N/A',
                },
                {
                  label: 'Created Date',
                  value:
                    metadataObject['dcterms:created']?.split('T')[0] || 'N/A',
                },
                {
                  label: 'Modified Date',
                  value:
                    metadataObject['dcterms:modified']?.split('T')[0] || 'N/A',
                },
                {
                  label: 'Page Count',
                  value:
                    metadataObject['pdf:ocrPageCount'] ||
                    metadataObject['xmpTPg:NPages'] ||
                    'N/A',
                },
                {
                  label: 'File Type',
                  value: metadataObject['Content-Type'] || 'N/A',
                },
                { label: 'Category', value: doc.categoryName || 'N/A' },
              ];
            } catch (e) {
              console.error('❌ Failed to parse metadata', e);
              this.parsedMetadata = [];
            }
          }

          // ✅ Store original category name for comparison
          this.originalCategoryName = doc.categoryName ?? null;
          this.selectedCategoryName = doc.categoryName ?? null;
          this.selectedBuildingId = doc.buildingId ?? null;

          // ✅ Ensure all categories are available in dropdown
          // If document has a category that's not in our list, add it
          if (
            doc.categoryName &&
            !this.allCategories.some((c) => c.name === doc.categoryName)
          ) {
            this.categories = [
              ...this.allCategories,
              {
                name: doc.categoryName,
                description: 'Document category',
              } as Category,
            ];
          } else {
            this.categories = [...this.allCategories];
          }

          // Use OpenAPI SDK to fetch document preview (blob)
          const documentsApi = this.apiFactory.create(DocumentsApi);
          documentsApi
            .apiDocumentsIdPreviewGet(doc.documentId!, { responseType: 'blob' })
            .then((response: any) => {
              // Clean up previous blob URL if exists
              if (this.blobUrl) {
                URL.revokeObjectURL(this.blobUrl);
              }
              // response.data is the blob
              this.blobUrl = URL.createObjectURL(response.data);

              this.selectedFile = {
                id: doc.documentId!,
                name: doc.fileName ?? '',
                url: this.blobUrl,
                metadata: [
                  { label: 'Uploaded', value: doc.uploadDate ?? '' },
                  {
                    label: 'Size',
                    value: `${((doc.fileSize ?? 0) / 1024).toFixed(2)} KB`,
                  },
                  { label: 'Type', value: doc.fileType ?? 'unknown' },
                ],
              };

          if (doc.keyInformation) {
            this.keyInformation = Object.entries(doc.keyInformation).map(([key, value]) => ({
              label: key.replace(/_/g, ' ').replace(/\b\w/g, c => c.toUpperCase()),  // Pretty label
              value: value !== null ? String(value) : 'N/A'  // Force even nulls to show
            }));
          } else {
              this.keyInformation = [];
            }
            // ✅ Store original key information for restoration
            this.originalKeyInformation = {};
            this.keyInformation.forEach(k => {
              this.originalKeyInformation[k.label.toLowerCase().replace(/ /g, '_')] = k.value || '';
            });

              const fileType = (doc.fileType ?? '').toLowerCase();
              this.isPdf = fileType === 'pdf';
              this.isImage =
                fileType === 'png' || fileType === 'jpg' || fileType === 'jpeg';

              // ✅ Fetch key info
              this.fetchKeyInfo(id);
            })
            .catch((err: any) => {
              console.error('❌ Failed to load document preview:', err);
              this.notFound = true;
            });
        },
        error: (err) => {
          console.error('❌ Failed to load document metadata:', err);
          this.notFound = true;
        },
      });
  }

  // ✅ Fetch key information
  fetchKeyInfo(id: number): void {
    this.loadingKeyInfo = true;
    const documentsApi = this.apiFactory.create(DocumentsApi);
    from(
      documentsApi
        .apiDocumentsIdGet(id)
        .then(
          (res) =>
            (res as unknown as import('axios').AxiosResponse<ApiDocument>).data,
        ),
    )
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data: ApiDocument) => {
          // ApiDocument does not have hasMetadata or suggestedAddress, so only use available fields
          this.keyInfo = {
            rawMetadata: data.metadata,
            // Add other fields if needed from ApiDocument
          };

          // Safely fallback for address (if you expect address info in metadata or keyInformation)
          if (!this.keyInfo.suggestedAddress) {
            this.keyInfo.suggestedAddress = {
              street: '',
              house_number: '',
              zip_code: '',
              city: '',
            };
          }

          // ✅ Add safe checks for array operations
          if (
            (!data.keyInformation ||
              Object.keys(data.keyInformation).length === 0) &&
            this.selectedCategoryName &&
            Array.isArray(this.categories) &&
            this.categories.length > 0
          ) {
            const match = this.categories.find(
              (c) => c.name === this.selectedCategoryName,
            );
            if (match && Array.isArray(match.fields)) {
              this.keyInformation = match.fields.map((f) => ({
                label: f.name,
                value: '',
              }));
            }
          } else {
            this.keyInformation = Object.entries(data.keyInformation || {}).map(
              ([key, value]) => ({
                label: key
                  .replace(/_/g, ' ')
                  .replace(/\b\w/g, (c) => c.toUpperCase()),
                value: value !== null ? String(value) : 'N/A',
              }),
            );
          }

          if (!this.keyInformation.length && this.selectedCategoryName) {
            this.onCategoryChange();
          }

          this.loadingKeyInfo = false;
        },
        error: (err: unknown) => {
          console.error('❌ Failed to load key info:', err);
          this.loadingKeyInfo = false;
        },
      });
  }

  downloadFile(): void {
    if (!this.selectedFile?.id) return;
    this.buildingService.downloadDocument(
      this.selectedFile.id,
      this.selectedFile.name,
    );
  }

  deleteFile(): void {
    if (!this.selectedFile?.id) return;

    if (confirm('Are you sure you want to delete this document?')) {
      this.buildingService
        .deleteDocument(this.selectedFile.id)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: () => {
            this.sidebarRefreshService.triggerRefresh();
            this.router.navigate(['/upload']);
          },
          error: (err) => {
            console.error('Delete failed:', err);
            this.toastMessage = '❌ Failed to delete document.';
            setTimeout(() => (this.toastMessage = ''), 4000);
          },
        });
    }
  }

  /**
   * ✅ Enhanced onCategoryChange method
   */
  onCategoryChange(): void {
    this.analysisMessage = '';

  const selected = this.categories.find(
    (c) => c.name === this.selectedCategoryName,
  );

  if (!selected || !Array.isArray(selected.fields)) return;

  if (this.selectedCategoryName === this.originalCategoryName) {
    // ✅ User switched back to original category — restore original key info
    this.keyInformation = Object.entries(this.originalKeyInformation).map(([key, value]) => ({
      label: key.replace(/_/g, ' ').replace(/\b\w/g, c => c.toUpperCase()),
      value: value
    }));
  } else {
    // 🔄 Category really changed — build fields based on new template
    this.keyInformation = selected.fields.map((field) => {
      const existing = this.keyInformation.find((k) => k.label === field.name);
      return {
        label: field.name,
        value: existing?.value || '',
      };
    });
  }

  // ✅ Now intelligently check for real changes
  this.checkForChanges();
  }

  /**
   * ✅ Enhanced saveMetadataChanges method
   */
  saveMetadataChanges(): void {
    if (!this.selectedFile?.id) return;

    this.loading = true;
    this.toastMessage = '';

    // ✅ Prepare key information properly
    const keyInfoObject = Object.fromEntries(
      this.keyInformation.map((k) => [
        k.label.toLowerCase().replace(/ /g, '_'),
        k.value || null,
      ]),
    );

    const patchRequest: DocumentMetadataPatchRequest & {
      keyInformation?: any;
    } = {
      buildingId: this.selectedBuildingId,
      categoryName: this.selectedCategoryName ?? undefined,
      keyInformation: keyInfoObject,
    };

    const documentsApi = this.apiFactory.create(DocumentsApi);
    documentsApi
      .apiDocumentsIdPatch(this.selectedFile.id, patchRequest)
      .then(() => {
        this.toastMessage = '✅ Metadata saved successfully.';
        this.hasChanges = false;

        // ✅ Update original category after successful save
        this.originalCategoryName = this.selectedCategoryName;

        this.sidebarRefreshService.triggerRefresh();

        // Reload to refresh UI with latest data
        this.fetchKeyInfo(this.selectedFile!.id);

        setTimeout(() => (this.toastMessage = ''), 4000);
      })
      .catch((err) => {
        console.error('❌ Failed to save metadata:', err);
        this.toastMessage = '❌ Failed to save metadata.';
        setTimeout(() => (this.toastMessage = ''), 4000);
      })
      .finally(() => {
        this.loading = false;
      });
  }

  /**
   * ✅ Improved canAnalyze getter
   */
  get canAnalyze(): boolean {
    return !!(
      this.selectedCategoryName &&
      this.selectedCategoryName !== this.originalCategoryName &&
      !this.isAnalyzing &&
      this.selectedFile?.id
    );
  }

  /**
   * ✅ Enhanced getAnalyzeButtonTooltip method
   */
  getAnalyzeButtonTooltip(): string {
    if (this.isAnalyzing) {
      return 'Analysis in progress...';
    }
    if (!this.selectedCategoryName) {
      return 'Please select a category first';
    }
    if (this.selectedCategoryName === this.originalCategoryName) {
      return 'Category has not changed from original';
    }
    return `Re-analyze document with "${this.selectedCategoryName}" category`;
  }

  /**
   * ✅ Enhanced analyzeWithNewCategory method
   */
  analyzeWithNewCategory(): void {
    if (
      !this.selectedFile?.id ||
      !this.selectedCategoryName ||
      !this.canAnalyze
    ) {
      console.warn('Cannot analyze: missing requirements');
      return;
    }

    this.isAnalyzing = true;
    this.analysisSuccess = true;

    const documentsApi = this.apiFactory.create(DocumentsApi);

    // Try to use the OpenAPI client method for key extraction
    const extractMethod = documentsApi.apiDocumentsIdExtractKeyInformationPost;

    const analysisPromise = extractMethod.call(
      documentsApi,
      this.selectedFile.id,
      { categoryName: this.selectedCategoryName },
    );
    analysisPromise
      .then((response: any) => {
        console.log('✅ Key information extracted:', response.data || response);
        this.isAnalyzing = false;
        this.analysisSuccess = true;
        this.analysisMessage = '✅ AI analysis completed successfully!';

        // ✅ Update original category after successful analysis
        this.originalCategoryName = this.selectedCategoryName;

        // Reload document to get new key information
        this.fetchKeyInfo(this.selectedFile!.id);

        // Clear message after delay
        setTimeout(() => {
          this.analysisMessage = '';
        }, 4000);
      })
      .catch((error: any) => {
        console.error('❌ Key extraction failed:', error);
        this.handleAnalysisError(error);
      });
  }

  /**
   * ✅ Enhanced error handling
   */
  private handleAnalysisError(error: any): void {
    this.isAnalyzing = false;
    this.analysisSuccess = false;

    let errorMsg = 'AI analysis failed';

    // Better error message handling
    if (error?.response?.status === 401 || error?.status === 401) {
      errorMsg = 'Authentication failed - please check your session';
    } else if (error?.response?.status === 405 || error?.status === 405) {
      errorMsg = 'AI analysis feature not available';
    } else if (error?.response?.status === 404 || error?.status === 404) {
      errorMsg = 'Document not found';
    } else if (error?.response?.status === 500 || error?.status === 500) {
      errorMsg = 'Server error during analysis';
    } else if (error?.response?.data?.message || error?.error?.message) {
      errorMsg = error.response?.data?.message || error.error?.message;
    } else if (error?.message) {
      errorMsg = error.message;
    }

    this.analysisMessage = `❌ ${errorMsg}`;

    // Clear error message after delay
    setTimeout(() => {
      this.analysisMessage = '';
    }, 6000);
  }

  /**
   * ✅ TrackBy function for categories to improve performance
   */
  trackByCategory(index: number, category: Category): any {
    return category.name || index;
  }

  toggleMetadataPanel(): void {
    this.isMetadataPanelCollapsed = !this.isMetadataPanelCollapsed;
  }

  zoomIn(): void {
    if (this.isImage) {
      this.imageZoom = Math.min(this.imageZoom + 0.2, 5);
    } else if (this.isPdf) {
      this.pdfZoom = Math.min(this.pdfZoom + 0.2, 5);
    }
  }

  zoomOut(): void {
    if (this.isImage) {
      this.imageZoom = Math.max(this.imageZoom - 0.2, 0.2);
    } else if (this.isPdf) {
      this.pdfZoom = Math.max(this.pdfZoom - 0.2, 0.2);
    }
  }

  resetZoom(): void {
    this.imageZoom = 1;
    this.pdfZoom = 1;
  }
 /**
 * Checks if a given field label is marked as mandatory
 * in the selected category's field list.
 */
  isFieldRequired(label: string): boolean {
    const category = this.categories.find(c => c.name === this.selectedCategoryName);
    return !!category?.fields?.find(f => f.name === label && f.mandatory);
  }

  /**
 * Tries to guess the input type for a field based on its label.
 */
  getInputType(label: string): string {
    const lower = label.toLowerCase();
    if (lower.includes('datum') || lower.includes('date')) return 'date';
    if (lower.includes('zahl') || lower.includes('nummer') || lower.includes('anzahl')) return 'number';
    return 'text';
  }
  
  /**
 * Compares the current form state with the original state to determine
 * if changes were actually made (used to toggle Save button).
 */
  checkForChanges(): void {
    const currentInfo = Object.fromEntries(
      this.keyInformation.map(k => [k.label.toLowerCase().replace(/ /g, '_'), k.value || ''])
    );

    const categoryChanged = this.selectedCategoryName !== this.originalCategoryName;
    const buildingChanged = this.selectedBuildingId !== this.originalBuildingId;

    const metadataChanged = Object.keys(currentInfo).some(key =>
      currentInfo[key] !== this.originalKeyInformation[key]
    );

    this.hasChanges = categoryChanged || buildingChanged || metadataChanged;
  }
  withinPanel = false;

  handleMouseMove = (event: MouseEvent) => {
    const screenWidth = window.innerWidth;
    const cursorX = event.clientX;

    const withinEdgeZone = screenWidth - cursorX <= 40;

    if (withinEdgeZone || this.withinPanel) {
      if (this.isMetadataPanelCollapsed) {
        this.isMetadataPanelCollapsed = false;
      }
    } else {
      if (!this.isMetadataPanelCollapsed) {
        this.isMetadataPanelCollapsed = true;
      }
    }
  };

  /**
 * Generates empty key information fields from a category,
 * and initializes touched state for validation tracking.
 */
  private generateKeyInfoFromCategory(category: Category): { label: string; value: string }[] {
    this.touchedFields = {};
    if (!Array.isArray(category.fields)) {
      return [];
    }

    return category.fields.map(field => {
      this.touchedFields[field.name] = false;
      return {
        label: field.name,
        value: ''
      };
    });
  }
  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();

    // Clean up blob URL
    if (this.blobUrl) {
      URL.revokeObjectURL(this.blobUrl);
    }
    document.removeEventListener('mousemove', this.handleMouseMove);
  }

}
