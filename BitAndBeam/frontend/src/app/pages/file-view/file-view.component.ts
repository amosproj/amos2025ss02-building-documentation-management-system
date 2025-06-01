import { Component } from '@angular/core';
import { Router ,ActivatedRoute} from '@angular/router';
import { CommonModule } from '@angular/common';
import { PdfViewerModule } from 'ng2-pdf-viewer';
import { ConfigService } from '../../config.service';
import { SidebarComponent} from '../../components/sidebar/sidebar.component';
import { BuildingService, DocumentItem, DocumentResponse } from '../../services/building.service';
import { Configuration, DocumentsApi, Document as ApiDocument } from '../../../api';
import { PDFDocument } from 'pdf-lib';

@Component({
  standalone: true,
  selector: 'app-file-view',
  templateUrl: './file-view.component.html',
  styleUrls: ['./file-view.component.css'],
  imports: [CommonModule, PdfViewerModule, SidebarComponent]
})
export class FileViewComponent {

  selectedFile: DocumentItem | null = null;
  notFound = false;
  pdfSrc: string | null = null;

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
      next: (doc: ApiDocument)  => {
        console.log('📄 Loaded document:', doc);
        console.log('🔧 Config API URL:', this.config.apiUrl);

        this.selectedFile = {
          id: doc.documentId!,
          name: doc.fileName ?? '',
          url: `${this.config.apiUrl}/api/Documents/${doc.documentId}/preview`,
          metadata: [
            { label: 'Uploaded', value: doc.uploadDate ?? '' },
            { label: 'Size', value: `${((doc.fileSize ?? 0) / 1024).toFixed(2)} KB` },
            { label: 'Type', value: doc.fileType ?? 'unknown' }
          ]
        };
        // Determine file type for viewer
        const fileType = (doc.fileType ?? '').toLowerCase();
        if (fileType === 'png' || fileType === 'jpg' || fileType === 'jpeg') {
          // ✅ Wrap image in single-page PDF dynamically
          await this.createPdfWithImage(this.selectedFile.url, fileType);
        } else {
          // ✅ Directly use the PDF file
          this.pdfSrc = this.selectedFile.url;
        }
      },
      error: (err) => {
        console.error('❌ Failed to load document:', err);
        this.notFound = true;
      }
    });
  }

  async createPdfWithImage(imageUrl: string, fileType: string) {
    const response = await fetch(imageUrl);
    const imageBytes = await response.arrayBuffer();

    const pdfDoc = await PDFDocument.create();
    const page = pdfDoc.addPage();

    let embeddedImage;
    if (fileType === 'png') {
      embeddedImage = await pdfDoc.embedPng(imageBytes);
    } else {
      embeddedImage = await pdfDoc.embedJpg(imageBytes);
    }

    const { width, height } = embeddedImage.scale(1);
    page.setSize(width, height);
    page.drawImage(embeddedImage, { x: 0, y: 0, width, height });

    const pdfBytes = await pdfDoc.save();
    const pdfBlob = new Blob([pdfBytes], { type: 'application/pdf' });
    this.pdfSrc = URL.createObjectURL(pdfBlob);
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
