import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Component } from '@angular/core';
import { Router ,ActivatedRoute} from '@angular/router';
import { CommonModule } from '@angular/common';
import { ConfigService } from '../../config.service';
import { SidebarComponent} from '../../components/sidebar/sidebar.component';
import { BuildingService, DocumentItem, DocumentResponse } from '../../services/building.service';
import { Configuration, DocumentsApi, Document as ApiDocument } from '../../../api';
import { NgxExtendedPdfViewerModule } from 'ngx-extended-pdf-viewer'; // Switched to NgxExtendedPdfViewerModule due to ng2-pdf-viewer incompatibility with Vite
import { ChangeDetectorRef } from '@angular/core';

@Component({
  standalone: true,
  selector: 'app-file-view',
  templateUrl: './file-view.component.html',
  styleUrls: ['./file-view.component.css'],
  imports: [CommonModule, NgxExtendedPdfViewerModule, SidebarComponent]
})
export class FileViewComponent {
  
  loadPdfBlob(documentId: number): void {
  // Make sure the URL uses the same case as your backend expects!
  const apiUrl = `/api/Documents/${documentId}/preview`; // or '/api/documents/...'
  console.log('About to call:', apiUrl);
  console.log('HttpClient instance:', this.http);

  this.http.get(apiUrl, { responseType: 'blob' }).subscribe({
    next: (blob) => {
      // Handle the blob, e.g.:
      const blobUrl = URL.createObjectURL(blob);
      this.selectedFile!.url = blobUrl;
      this.cdr.detectChanges();
      console.log('✅ Blob loaded:', blobUrl);
    },
    error: (err) => {
      console.error('❌ Failed to fetch PDF blob:', err);
      this.notFound = true;
    }
  });
}


  selectedFile: DocumentItem | null = null;
  notFound = false;
  isPdf = false;
  isImage = false;

  constructor(
    private http: HttpClient,
    private cdr: ChangeDetectorRef,
    private config: ConfigService,
    private route: ActivatedRoute,
    private router: Router,
    private buildingService: BuildingService
  ) {}
  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    const id = Number(idParam);

    console.log('📌 Route document ID:', id);


    if (!id || isNaN(id)) {
      console.error('❌ Invalid document ID in route:', idParam);
      this.notFound = true;
      return;
    }

    this.buildingService.getDocumentById(id).subscribe({
      next: (doc: ApiDocument) => {
        console.log('📄 Loaded document:', doc);
        console.log('🔧 Config API URL:', this.config.apiUrl);

        this.selectedFile = {
          id: doc.documentId!,
          name: doc.fileName ?? '',
          url: '',
          metadata: [
            { label: 'Uploaded', value: doc.uploadDate ?? '' },
            {
              label: 'Size',
              value: `${((doc.fileSize ?? 0) / 1024).toFixed(2)} KB`,
            },
            { label: 'Type', value: doc.fileType ?? 'unknown' },
          ],
        };
        console.log('🧾 Document ID before loading blob:', id);
          if (doc.documentId) {
            this.loadPdfBlob(id);
          } else {
            console.error('❌ Document ID is undefined!');
            this.notFound = true;
          }

        // Determine file type for viewer
        const fileType = (doc.fileType ?? '').toLowerCase();
        this.isPdf = fileType === 'pdf';
        this.isImage = fileType === 'png' || fileType === 'jpg' || fileType === 'jpeg';
      },
      error: (err) => {
        console.error('❌ Failed to load document:', err);
        this.notFound = true;
      },
    });
  }
  downloadFile(): void {
    if (this.selectedFile?.id) {
      this.buildingService.downloadDocument(this.selectedFile.id);
    }
  }

  deleteFile(): void {
    if (!this.selectedFile?.id) return;

    this.buildingService.deleteDocument(this.selectedFile.id).subscribe({
      next: () => this.router.navigate(['/upload']),
      error: (err) => console.error('Delete failed:', err)
    });
  }
}
