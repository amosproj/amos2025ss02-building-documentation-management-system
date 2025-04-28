import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { DocumentListComponent } from './document-list/document-list.component';
import { DocumentViewerComponent } from './document-viewer/document-viewer.component';
import { MetadataPanelComponent } from './metadata-panel/metadata-panel.component';
import { ChatbotComponent } from './chatbot/chatbot.component';
import { UploadFileComponent } from './upload-file/upload-file.component';

@Component({
  selector: 'app-root',
  imports: [
    RouterOutlet,
    DocumentListComponent,
    DocumentViewerComponent,
    MetadataPanelComponent,
    ChatbotComponent,
    UploadFileComponent
  ],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent {
  title = 'frontend';
  uploadedFiles: File[] = [];
}
