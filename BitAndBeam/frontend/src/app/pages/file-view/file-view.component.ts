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

@Component({
  standalone: true,
  selector: 'app-file-view',
  templateUrl: './file-view.component.html',
  styleUrls: ['./file-view.component.css'],
  imports: [CommonModule, PdfViewerModule, SidebarComponent, FormsModule]
})
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
  keyInformation: { label: string; value: string | null }[] = [];
  loadingKeyInfo: boolean = false;
  keyInfo: any = null;

  constructor(private config: ConfigService,private route: ActivatedRoute,private router: Router, private buildingService: BuildingService,  private categoryService: CategoryService,
  private apiFactory: ApiClientFactory , private sidebarRefreshService: SidebarRefreshService, private http: HttpClient,
              private session: SessionService) {}
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

          // ✅ Add document's category to the list if it doesn't exist
          if (doc.categoryName && !this.categories.some(c => c.name === doc.categoryName)) {
            this.categories.push({ name: doc.categoryName } as Category);
          }

          if (doc.keyInformation) {
            this.keyInformation = Object.entries(doc.keyInformation).map(([key, value]) => ({
              label: key.replace(/_/g, ' ').replace(/\b\w/g, c => c.toUpperCase()),  // Pretty label
              value: value ? String(value) : 'N/A'
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

  // ✅ New method: fetch key information
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

  // ✅ New method: update building/category
  saveMetadataChanges(): void {
    if (!this.selectedFile?.id) return;

    this.loading = true;
    this.toastMessage = '';

    const patchRequest: DocumentMetadataPatchRequest = {
      buildingId: this.selectedBuildingId,
      categoryName: this.selectedCategoryName ?? undefined
    };

    const documentsApi = this.apiFactory.create(DocumentsApi);
    documentsApi.apiDocumentsIdPatch(this.selectedFile.id, patchRequest)
      .then(() => {
        this.toastMessage = '✅ Metadata updated successfully.';
        this.sidebarRefreshService.triggerRefresh();

        // ✅ After save, reload document to update UI
        this.loadDocument(this.selectedFile!.id);

        setTimeout(() => this.toastMessage = '', 4000);
      })
      .catch(() => {
        this.toastMessage = '❌ Failed to update metadata.';
      })
      .finally(() => {
        this.loading = false;
      });
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

}
