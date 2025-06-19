import { Component } from '@angular/core';
import { Router ,ActivatedRoute} from '@angular/router';
import { CommonModule } from '@angular/common';
import { ConfigService } from '../../config.service';
import { SidebarComponent} from '../../components/sidebar/sidebar.component';
import { BuildingService, DocumentItem, DocumentResponse } from '../../services/building.service';
import { Configuration, DocumentsApi, Document as ApiDocument } from '../../../api';
import { NgxExtendedPdfViewerModule } from 'ngx-extended-pdf-viewer'; // Switched to NgxExtendedPdfViewerModule due to ng2-pdf-viewer incompatibility with Vite

@Component({
  standalone: true,
  selector: 'app-file-view',
  templateUrl: './file-view.component.html',
  styleUrls: ['./file-view.component.css'],
  imports: [CommonModule, NgxExtendedPdfViewerModule, SidebarComponent]
})
export class FileViewComponent {
  
  loadPdfBlob(documentId: number): void {
  const token = localStorage.getItem('jwt');
  if (!token) {
    console.error('❌ No JWT token found');
    this.notFound = true;
    return;
  }

  const apiUrl = `${this.config.apiUrl}/api/Documents/${documentId}/preview`;

  fetch(apiUrl, {
    method: 'GET',
    headers: {
      Authorization: `Bearer ${token}`
    }
  })
    .then(res => {
      if (!res.ok) {
        throw new Error('Failed to fetch PDF blob');
      }
      return res.blob();
    })
    .then(blob => {
      const blobUrl = URL.createObjectURL(blob);
      this.selectedFile!.url = blobUrl;
      console.log('🧾 PDF loaded as Blob URL:', blobUrl);
    })
    .catch(err => {
      console.error('❌ Error loading PDF blob:', err);
      this.notFound = true;
    });
}


  selectedFile: DocumentItem | null = null;
  notFound = false;
  isPdf = false;
  isImage = false;

  constructor(private config: ConfigService,private route: ActivatedRoute,private router: Router, private buildingService: BuildingService) {}
  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    const id = Number(idParam);

    if (!idParam || isNaN(id)) {
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
          url: `${this.config.apiUrl}/api/Documents/${doc.documentId}/preview`,
          metadata: [
            { label: 'Uploaded', value: doc.uploadDate ?? '' },
            {
              label: 'Size',
              value: `${((doc.fileSize ?? 0) / 1024).toFixed(2)} KB`,
            },
            { label: 'Type', value: doc.fileType ?? 'unknown' },
          ],
        };
        console.log('📂 Preview URL:', this.selectedFile.url);
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
