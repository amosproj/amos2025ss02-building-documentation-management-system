import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss'],
  standalone: true,
  imports: [CommonModule, RouterModule]
})
export class HomeComponent {
  // No longer needed as we're not showing the upload component on the home page
  // onFilesUploaded(files: File[]) {
  //   console.log('Files uploaded:', files);
  // }
}
