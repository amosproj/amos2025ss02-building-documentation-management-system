import { Component } from '@angular/core';
import { UploadFileComponent } from '../../components/upload-file/upload-file.component';

@Component({
    selector: 'app-home',
    imports: [UploadFileComponent],
    templateUrl: './home.component.html',
    styleUrls: ['./home.component.css']
})
export class HomeComponent {}
