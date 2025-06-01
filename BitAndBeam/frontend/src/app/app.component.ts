import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { DocumentExplorerComponent } from './components/document-explorer/document-explorer.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, DocumentExplorerComponent],
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent {
  title = 'BUILD.ING';
}
