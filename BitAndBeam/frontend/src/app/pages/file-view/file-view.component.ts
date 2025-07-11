import { Component } from '@angular/core';
import { Router ,ActivatedRoute} from '@angular/router';
import { CommonModule } from '@angular/common';
import { PdfViewerModule } from 'ng2-pdf-viewer';
import { ConfigService } from '../../config.service';
import { SidebarComponent} from '../../components/sidebar/sidebar.component';
import { BuildingService, DocumentItem, DocumentResponse } from '../../services/building.service';
import { Configuration, DocumentsApi, Document as ApiDocument, DocumentMetadataPatchRequest } from '../../../api';
import { CategoryService, Category } from '../../services/category.service';
import { ApiClientFactory } from '../../services/api-client.factory';
import { SidebarRefreshService }  from '../../services/sidebar-refresh.service';
import { FormsModule } from '@angular/forms';
import { HttpClient , HttpHeaders} from '@angular/common/http';
import { SessionService } from '../../services/session.service'; //
import { AiAssistantComponent } from '../../components/ai-assistant/ai-assistant.component';


@Component({
  standalone: true,
  selector: 'app-file-view',
  templateUrl: './file-view.component.html',
  styleUrls: ['./file-view.component.css'],
  imports: [CommonModule, PdfViewerModule, SidebarComponent, FormsModule, AiAssistantComponent]
})

/**
 * Handles file viewing, document preview (PDF/Image),
 * category-based metadata parsing and editing, and saving to backend.
 */
export class FileViewComponent {

  selectedFile: DocumentItem | null = null;
  notFound = false;
  isPdf = false;
  isImage = false;
  buildings: any[] = [];
  categories: Category[] = [];
  selectedBuildingId: number | null = null;
  selectedCategoryName: string | null = null;
  loading = false;
  toastMessage = '';
  isMetadataPanelCollapsed = false;
  showToolbar = false;
  imageZoom = 1;
  pdfZoom = 1;

  // ✅ New variables for key info
  metadataRaw: string = '';
  parsedMetadata: { label: string; value: string }[] = [];
  keyInformation: { key: string; label: string; value: string | null }[] = [];
  loadingKeyInfo: boolean = false;
  keyInfo: any = null;
  hasChanges: boolean = false;
  // 🟡 Tracks if a field has been touched for validation feedback
  touchedFields: { [label: string]: boolean } = {};

  originalKeyInformation: { [key: string]: string } = {};
  originalCategoryName: string | null = null;
  originalBuildingId: number | null = null;


  constructor(private config: ConfigService,private route: ActivatedRoute,private router: Router, private buildingService: BuildingService,  private categoryService: CategoryService,
  private apiFactory: ApiClientFactory , private sidebarRefreshService: SidebarRefreshService, private http: HttpClient,
              private session: SessionService) {}

  /**
 * On component init:
 * - Watch route for document ID
 * - Fetch buildings, categories, and selected document data
 */
  ngOnInit(): void {
    // Watch for route param changes
    this.route.paramMap.subscribe(paramMap => {
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

      this.buildingService.getBuildings().subscribe(b => this.buildings = b);
      this.categoryService.getCategories().subscribe(c => this.categories = c);

      this.loadDocument(id);
    });
  }

  /**
 * Loads document by ID from backend, sets preview,
 * parses metadata, and initializes document state.
 */
  loadDocument(id: number){
    this.buildingService.getDocumentById(id).subscribe({
      next: (doc: ApiDocument) => {
        this.metadataRaw = doc.metadata ?? '';
        if (doc.metadata) {
          try {
            const entries = doc.metadata
              .split(/[\r\n]+/)
              .map(line => {
                const separator = line.includes('=') ? '=' : line.includes(',') ? ',' : ':';
                const [key, ...rest] = line.split(separator).map(s => s.trim());
                const value = rest.join(separator);
                return [key, value];
              })
              .filter(arr => arr.length >= 2);

            const metadataObject: { [key: string]: string } = {};
            entries.forEach(([key, value]) => {
              metadataObject[key] = value;
            });

            this.parsedMetadata = [
              { label: 'Title', value: metadataObject['resourceName'] || 'N/A' },
              { label: 'Author', value: metadataObject['dc:creator'] || metadataObject['pdf:docinfo:creator'] || 'N/A' },
              { label: 'Created Date', value: metadataObject['dcterms:created']?.split('T')[0] || 'N/A' },
              { label: 'Modified Date', value: metadataObject['dcterms:modified']?.split('T')[0] || 'N/A' },
              { label: 'Page Count', value: metadataObject['pdf:ocrPageCount'] || metadataObject['xmpTPg:NPages'] || 'N/A' },
              { label: 'File Type', value: metadataObject['Content-Type'] || 'N/A' },
              { label: 'Category', value: doc.categoryName || 'N/A' },
            ];
          } catch (e) {
              console.error('❌ Failed to parse metadata', e);
              this.parsedMetadata = [];
            }
        }
        const token = this.session.getToken();
        const previewUrl = `${this.config.apiUrl}/api/Documents/${doc.documentId}/preview`;

        const headers = new HttpHeaders({
          Authorization: `Bearer ${token}`
        });

        this.http.get(previewUrl, { headers, responseType: 'blob' }).subscribe(blob => {
          const objectUrl = URL.createObjectURL(blob);

          this.selectedFile = {
            id: doc.documentId!,
            name: doc.fileName ?? '',
            url: objectUrl,
            metadata: [
              { label: 'Uploaded', value: doc.uploadDate ?? '' },
              {
                label: 'Size',
                value: `${((doc.fileSize ?? 0) / 1024).toFixed(2)} KB`,
              },
              { label: 'Type', value: doc.fileType ?? 'unknown' },
            ]
          };
          this.selectedBuildingId = doc.buildingId ?? null;
          this.selectedCategoryName = doc.categoryName ?? null;

          this.originalCategoryName = this.selectedCategoryName;
          this.originalBuildingId = this.selectedBuildingId;

          this.originalKeyInformation = {};
          if (doc.keyInformation) {
            Object.entries(doc.keyInformation).forEach(([key, value]) => {
              this.originalKeyInformation[key.toLowerCase()] = value !== null ? String(value) : '';
            });
          }

          // ✅ Add document's category to the list if it doesn't exist
          if (doc.categoryName && !this.categories.some(c => c.name === doc.categoryName)) {
            this.categories.push({ name: doc.categoryName } as Category);
          }

          if (doc.keyInformation) {
            this.keyInformation = Object.entries(doc.keyInformation).map(([key, value]) => ({
              key: key,
              label: key.replace(/_/g, ' ').replace(/\b\w/g, c => c.toUpperCase()),  // Pretty label
              value: value !== null ? String(value) : ''  // Force even nulls to show
            }));
          } else {
              this.keyInformation = [];
            }

          const fileType = (doc.fileType ?? '').toLowerCase();
          this.isPdf = fileType === 'pdf';
          this.isImage = fileType === 'png' || fileType === 'jpg' || fileType === 'jpeg';

          // ✅ Fetch key info
          this.fetchKeyInfo(id);

        }, err => {
          console.error('❌ Failed to load document preview:', err);
          this.notFound = true;
        });
      },
      error: (err) => {
        console.error('❌ Failed to load document metadata:', err);
        this.notFound = true;
      }
    });
  }

  /**
 * Loads extracted key information from backend.
 * If none is found, auto-generates empty key fields based on category.
 */
  fetchKeyInfo(id: number) {
    this.loadingKeyInfo = true;
    const token = this.session.getToken();
    const url = `${this.config.apiUrl}/api/Documents/${id}`;
    const headers = new HttpHeaders({
      Authorization: `Bearer ${token}`
    });

    this.http.get<any>(url, { headers }).subscribe({
      next: (data) => {
        this.keyInfo = {
          hasMetadata: data.hasMetadata,
          suggestedAddress: data.suggestedAddress,
          rawMetadata: data.metadata,
        };
        //safely fallback if address is missing
        if (!this.keyInfo.suggestedAddress) {
          this.keyInfo.suggestedAddress = {
            street: '',
            house_number: '',
            zip_code: '',
            city: ''
          };
        }
        // ✅ If no keyInformation found but category is selected, generate empty key fields
        if ((!data.keyInformation || Object.keys(data.keyInformation).length === 0) &&
            this.selectedCategoryName && this.categories.length > 0) {
          const match = this.categories.find(c => c.name === this.selectedCategoryName);
          if (match && Array.isArray(match.fields)) {
            this.keyInformation = this.generateKeyInfoFromCategory(match);
          }
        } else {
          this.keyInformation = (data.keyInformation || []).map((item: any) => ({
            key: item.key,
            label: item.name,
            value: item.value ?? ''
          }));
        }
        if (!this.keyInformation.length && this.selectedCategoryName) {
          this.onCategoryChange();
        }

        this.loadingKeyInfo = false;
      },
      error: (err) => {
        console.error('❌ Failed to load key info:', err);
        this.loadingKeyInfo = false;
      }
    });
  }

  downloadFile(): void {
    if (!this.selectedFile?.id) return;
    this.buildingService.downloadDocument(this.selectedFile.id, this.selectedFile.name);
  }

  deleteFile(): void {
    if (!this.selectedFile?.id) return;

    this.buildingService.deleteDocument(this.selectedFile.id).subscribe({
      next: () => this.router.navigate(['/upload']),
      error: (err) => console.error('Delete failed:', err)
    });
  }

  /**
 * Builds patch request and sends metadata updates to backend.
 * Regenerates view after successful save.
 */
  saveMetadataChanges(): void {
    if (!this.selectedFile?.id) return;

    this.loading = true;
    this.toastMessage = '';

    // 🟡 AUTO-GENERATE blank key info if none is loaded but category is selected
    if ((!this.keyInformation || this.keyInformation.length === 0) &&
        this.selectedCategoryName && this.categories.length > 0) {
      const match = this.categories.find(c => c.name === this.selectedCategoryName);
      if (match && Array.isArray(match.fields) && match.fields.length > 0) {
        this.keyInformation = this.generateKeyInfoFromCategory(match);
      }
    }

    const patchRequest: DocumentMetadataPatchRequest & {
      keyInformation?: any;
    } = {
      buildingId: this.selectedBuildingId,
      categoryName: this.selectedCategoryName ?? undefined,
      keyInformation: Object.fromEntries(
        this.keyInformation.map(k => [k.label.toLowerCase().replace(/ /g, '_'), k.value])
      )
    };

    console.log('📦 Patch request payload:', patchRequest);
    const documentsApi = this.apiFactory.create(DocumentsApi);
    documentsApi.apiDocumentsIdPatch(this.selectedFile.id, patchRequest)
      .then(() => {
        this.toastMessage = '✅ Metadata updated successfully.';
        this.hasChanges = false;
        this.sidebarRefreshService.triggerRefresh();

        // ✅ After save, reload document to update UI
        this.loadDocument(this.selectedFile!.id);
        this.fetchKeyInfo(this.selectedFile!.id);

        setTimeout(() => this.toastMessage = '', 4000);
      })
      .catch(() => {
        this.toastMessage = '❌ Failed to update metadata.';
      })
      .finally(() => {
        this.loading = false;
      });
  }
  /**
 * Resets key fields when category is changed,
 * initializes touchedFields map for validation.
 */
  onCategoryChange(): void {
    const selected = this.categories.find(c => c.name === this.selectedCategoryName);
    if (selected && Array.isArray(selected.fields)) {
      this.keyInformation = this.generateKeyInfoFromCategory(selected);
    }
  }

  toggleMetadataPanel(): void {
    this.isMetadataPanelCollapsed = !this.isMetadataPanelCollapsed;
  }

  zoomIn() {
    if (this.isImage) {
      this.imageZoom = Math.min(this.imageZoom + 0.2, 5);
    } else if (this.isPdf) {
      this.pdfZoom = Math.min(this.pdfZoom + 0.2, 5);
    }
  }

  zoomOut() {
    if (this.isImage) {
      this.imageZoom = Math.max(this.imageZoom - 0.2, 0.2);
    } else if (this.isPdf) {
      this.pdfZoom = Math.max(this.pdfZoom - 0.2, 0.2);
    }
  }

  resetZoom() {
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
        key: field.key,
        label: field.name,
        value: ''
      };
    });
  }
}
