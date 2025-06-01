import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface DocumentItem {
  documentId: number;
  title: string;
  filePath: string;
}

export interface BuildingWithDocuments {
  buildingId: number;
  buildingName: string;
  documents: DocumentItem[];
}

@Injectable({
  providedIn: 'root'
})
export class DocumentExplorerService {
  private apiUrl = '/api/buildings/with-documents';

  constructor(private http: HttpClient) {}

  getDocumentsByBuilding(): Observable<BuildingWithDocuments[]> {
    return this.http.get<BuildingWithDocuments[]>(this.apiUrl);
  }
}
