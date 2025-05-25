import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { PdfViewerModule } from 'ng2-pdf-viewer';
import { SidebarComponent} from '../../components/sidebar/sidebar.component';
import { BuildingService } from '../../services/building.service';


@Component({
  standalone: true,
  selector: 'app-file-view',
  templateUrl: './file-view.component.html',
  styleUrls: ['./file-view.component.css'],
  imports: [CommonModule, PdfViewerModule, SidebarComponent]
})
export class FileViewComponent {

  selectedFile: {
    name: string;
    url: string;
    metadata?: { label: string; value: string }[];
  } | null = null;

  constructor(private router: Router, private buildingService: BuildingService) {}
  ngOnInit() {
    this.buildingService.selectedFile$.subscribe(file => {
      this.selectedFile = file;
    });
  }

  selectFileToView(file: { name: string; url: string }) {
    this.selectedFile = file;
  }

  downloadFile(file: { name: string; url: string }) {
    try {
      const a = document.createElement('a');
      a.href = file.url;
      a.download = file.name;
      a.style.display = 'none';
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
    } catch (error) {
      console.error('Download failed:', error);
    }
  }


  clearViewer() {
    this.selectedFile = null;
  }

}
