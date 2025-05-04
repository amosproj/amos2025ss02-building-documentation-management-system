import { Component } from '@angular/core';
import {UploadFileComponent} from "../upload-file/upload-file.component";

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [
    UploadFileComponent
  ],
  templateUrl: './home.component.html',
  styleUrl: './home.component.css'
})
export class HomeComponent {

}
