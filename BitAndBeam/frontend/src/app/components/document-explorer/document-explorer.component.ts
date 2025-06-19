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
  buildingDocumentGroups: any[] = [];

  constructor(private http: HttpClient, private router: Router) {}

  ngOnInit(): void {
    const token = localStorage.getItem('jwt');
    if (!token) {
      console.warn('⚠️ No JWT token found — redirecting to login');
    // Optionally inject Router via constructor to use:
      this.router.navigate(['/login']);
      return;
    }
    const headers = {
      headers: {
        Authorization: `Bearer ${token}`
      }
    };
    this.http.get<any[]>('/api/buildings/with-documents', headers).subscribe({
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
