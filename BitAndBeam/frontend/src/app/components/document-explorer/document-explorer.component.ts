import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { RouterModule } from '@angular/router'; 
import { CommonModule } from '@angular/common';


@Component({
  selector: 'app-document-explorer',
  standalone: true,
  templateUrl: './document-explorer.component.html',
  styleUrls: ['./document-explorer.component.css'],
  imports: [
    CommonModule,
    RouterModule
  ]
})
export class DocumentExplorerComponent implements OnInit {
  buildingDocumentGroups: any[] = [];

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.http.get<any[]>('/api/buildings/with-documents').subscribe({
      next: (data) => {
        console.log('📦 Building data:', data);
        this.buildingDocumentGroups = data;
      },
      error: (err) => {
        console.error('Failed to fetch documents:', err);
      }
    });
  }
}
