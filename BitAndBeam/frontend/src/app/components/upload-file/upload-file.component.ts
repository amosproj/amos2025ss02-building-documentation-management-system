import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
    selector: 'app-upload-file', 
    imports: [CommonModule],
    templateUrl: './upload-file.component.html',
    styleUrls: ['./upload-file.component.css']
})
export class UploadFileComponent {
  uploadedFile: File | null = null;  // Change to single file

  // Handle file selection
  onFileSelected(event: any) {
    const file = event.target.files[0];  // Only get the first file
    if (file) {
      this.uploadedFile = file;
    }
  }

  // Handle drag-and-drop file selection
  onDrop(event: DragEvent) {
    event.preventDefault();
    if (event.dataTransfer?.files) {
      const file = event.dataTransfer.files[0];  // Only get the first file
      if (file) {
        this.uploadedFile = file;
      }
    }
  }

  // Handle drag over event (required for drop event to trigger)
  onDragOver(event: Event) {
    event.preventDefault();
  }
}
