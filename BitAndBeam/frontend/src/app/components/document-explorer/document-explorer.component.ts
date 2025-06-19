import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { RouterModule, Router } from '@angular/router'; 
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
  buildingDocumentGroups: { buildingName: string, documents: any[] }[] = [];

  constructor(private http: HttpClient, private router: Router) {}

  ngOnInit(): void {
    const token = localStorage.getItem('authToken');
    if (!token) {
      console.warn('⚠️ No JWT token found — redirecting to login');
      this.router.navigate(['/login']);
      return;
    }

    const headers = {
      headers: {
        Authorization: `Bearer ${token}`
      }
    };

    this.http.get<any[]>('/api/documents', headers).subscribe({
      next: (documents) => {
        console.log('📦 Fetched documents:', documents);
        this.buildingDocumentGroups = this.groupDocumentsByBuilding(documents);
        console.log('🏢 Grouped documents:', this.buildingDocumentGroups);
      },
      error: (err) => {
        console.error('Failed to fetch documents:', err);
      }
    });
  }

  groupDocumentsByBuilding(documents: any[]) {
    const groups: { [building: string]: any[] } = {};

    for (const doc of documents) {
      const name = doc.buildingName || 'Unassigned Building';
      if (!groups[name]) {
        groups[name] = [];
      }
      groups[name].push(doc);
    }

    return Object.entries(groups).map(([buildingName, docs]) => ({
      buildingName,
      documents: docs
    }));
  }
}
