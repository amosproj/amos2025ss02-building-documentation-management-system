import { Component, OnInit, OnDestroy, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DocumentService } from '../services/document.service';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

@Component({
  selector: 'app-document-list',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './document-list.component.html',
  styleUrl: './document-list.component.scss'
})
export class DocumentListComponent implements OnInit, OnDestroy {
  uploadedFiles: File[] = [];
  selectedFile: File | null = null;
  private destroy$ = new Subject<void>();
  
  @Output() fileSelected = new EventEmitter<File>();
  
  constructor(private documentService: DocumentService) {}
  
  ngOnInit(): void {
    // Subscribe to the document service to get updated files
    this.documentService.uploadedFiles$
      .pipe(takeUntil(this.destroy$))
      .subscribe(files => {
        this.uploadedFiles = files;
      });
    
    // Initialize with any existing files
    this.uploadedFiles = this.documentService.getUploadedFiles();
  }
  
  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
  
  /**
   * Select a document and emit the selection event
   */
  selectDocument(file: File): void {
    this.selectedFile = file;
    this.fileSelected.emit(file);
  }
  
  /**
   * Get file icon based on file type
   */
  getFileIcon(file: File): string {
    const extension = file.name.split('.').pop()?.toLowerCase() || '';
    
    switch(extension) {
      case 'pdf':
        return 'description'; // PDF icon
      case 'doc':
      case 'docx':
        return 'description'; // Word document icon
      case 'xls':
      case 'xlsx':
        return 'table_chart'; // Excel icon
      case 'ppt':
      case 'pptx':
        return 'slideshow'; // PowerPoint icon
      case 'jpg':
      case 'jpeg':
      case 'png':
      case 'gif':
        return 'image'; // Image icon
      default:
        return 'insert_drive_file'; // Default file icon
    }
  }
}
