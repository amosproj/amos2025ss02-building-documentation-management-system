import { Component } from '@angular/core';
import { UploadFileComponent } from '../../components/upload-file/upload-file.component';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [UploadFileComponent, RouterModule],
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css'],
})
export class HomeComponent {}
