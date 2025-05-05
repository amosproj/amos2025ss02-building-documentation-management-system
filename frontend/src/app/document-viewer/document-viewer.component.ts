import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-document-viewer',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './document-viewer.component.html',
  styleUrls: ['./document-viewer.component.scss']
})
export class DocumentViewerComponent implements OnChanges {
  @Input() files: File[] = [];
  @Input() selectedFile: File | null = null;
  
  fileContent: string | ArrayBuffer | null = null;
  fileType: string = '';
  isLoading: boolean = false;
  errorMessage: string | null = null;
  
  ngOnChanges(changes: SimpleChanges): void {
    if (changes['selectedFile'] && changes['selectedFile'].currentValue) {
      this.loadFileContent(changes['selectedFile'].currentValue);
    }
  }
  
  loadFileContent(file: File): void {
    this.isLoading = true;
    this.errorMessage = null;
    this.fileType = file.type;
    
    const reader = new FileReader();
    
    reader.onload = (e) => {
      this.fileContent = e.target?.result || null;
      this.isLoading = false;
    };
    
    reader.onerror = () => {
      this.errorMessage = 'Error loading file. Please try again.';
      this.isLoading = false;
    };
    
    if (file.type.includes('text') || 
        file.type.includes('application/json') ||
        file.name.endsWith('.txt') ||
        file.name.endsWith('.json') ||
        file.name.endsWith('.md') ||
        file.name.endsWith('.csv')) {
      reader.readAsText(file);
    } else if (file.type.includes('image')) {
      reader.readAsDataURL(file);
    } else {
      // For PDFs and other binary files
      reader.readAsArrayBuffer(file);
    }
  }
  
  getFileIcon(file: File): string {
    const extension = file.name.split('.').pop()?.toLowerCase() || '';
    
    switch(extension) {
      case 'pdf':
        return 'description';
      case 'doc':
      case 'docx':
        return 'description';
      case 'xls':
      case 'xlsx':
        return 'table_chart';
      case 'ppt':
      case 'pptx':
        return 'slideshow';
      case 'jpg':
      case 'jpeg':
      case 'png':
      case 'gif':
        return 'image';
      default:
        return 'insert_drive_file';
    }
  }
}
