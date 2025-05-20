import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { PdfViewerModule } from 'ng2-pdf-viewer';
import { SidebarComponent} from '../../components/sidebar/sidebar.component';


@Component({
  standalone: true,
  selector: 'app-file-view',
  templateUrl: './file-view.component.html',
  styleUrls: ['./file-view.component.css'],
  imports: [CommonModule, PdfViewerModule, SidebarComponent]
})
export class FileViewComponent {

  fileList = [
    {
      name: '0_Introduction_deeplearning.pdf',
      url: '/assets/0_Introduction_deeplearning.pdf',  // ✅ Add leading slash
      metadata: [
        { label: 'Title', value: 'Intro to Deep Learning' },
        { label: 'Author', value: 'FAU Pattern Recognition Lab' },
        { label: 'Date', value: '2025-04-25' }
      ]
    }
  ];

  selectedFile = this.fileList[0];

  constructor(private router: Router) {}


  selectFile(file: any) {
    this.selectedFile = file;
  }

  downloadFile(file: any) {
    const a = document.createElement('a');
    a.href = file.url;
    a.download = file.name;
    a.click();
  }

  deleteFile(file: any) {
    this.fileList = this.fileList.filter(f => f !== file);
    if (this.selectedFile === file) {
      this.selectedFile = this.fileList[0] || null;
    }
  }
}
