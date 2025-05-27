import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable , from} from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { ConfigService } from '../config.service';
import { AxiosResponse } from 'axios';
import { map } from 'rxjs/operators';
import { Configuration, DocumentsApi, Document as ApiDocument, BuildingsApi,
  Building as ApiBuilding } from '../../api';

export interface DocumentItem {
  id: number;
  name: string;
  url: string;
  metadata: { label: string; value: string }[];
}

export interface DocumentResponse {
  documentId: number; // 👈 add this
  title: string;
  fileName: string;
  filePath?: string;
  fileSize: number;
  fileType: string;
  uploadDate: string;
}

export interface Building {
  id: number;
  name: string;
  documents?: DocumentItem[];
}

@Injectable({ providedIn: 'root' })
export class BuildingService {
  private documentsApi: DocumentsApi;
  private buildingsApi: BuildingsApi;
  private buildingsSubject = new BehaviorSubject<ApiBuilding[]>([]);
  buildings$ = this.buildingsSubject.asObservable();

  constructor(private config: ConfigService) {
    const configuration = new Configuration({ basePath: this.config.apiUrl });
    this.documentsApi = new DocumentsApi(configuration);
    this.buildingsApi = new BuildingsApi(configuration);
  }
  //Buildings
  getBuildings(): Observable<Building[]> {
    return from(
        this.buildingsApi.apiBuildingsGet().then(res =>
            res.data.map(apiB => ({
              id: apiB.buildingId!,
              name: apiB.name ?? '',
              documents: [] // you can map documents if needed
            }))
        )
    );
  }

  addBuilding(name: string): Observable<Building> {
    return from(
        this.buildingsApi.apiBuildingsPost({ name }).then(() =>
            this.buildingsApi.apiBuildingsGet().then(res => {
              const last = res.data[res.data.length - 1];
              return {
                id: last.buildingId!,
                name: last.name ?? '',
                documents: []
              };
            })
        )
    );
  }


  deleteBuilding(id: number): Observable<void> {
    return from(this.buildingsApi.apiBuildingsIdDelete(id).then(() => {}));
  }

  //Docs
  getDocumentById(id: number): Observable<ApiDocument> {
    return from(
        this.documentsApi.apiDocumentsIdGet(id)
            .then(res => (res as unknown as AxiosResponse<ApiDocument>).data)
    );
  }
  deleteDocument(id: number): Observable<void> {
    return from(this.documentsApi.apiDocumentsIdDelete(id).then(() => {}));
  }

  downloadDocument(id: number): void {
    const downloadUrl = `${this.config.apiUrl}/api/Documents/${id}/download`;
    window.open(downloadUrl, '_blank');
  }

  private selectedFileSubject = new BehaviorSubject<DocumentItem | null>(null);
  selectedFile$ = this.selectedFileSubject.asObservable();

  setSelectedFile(file: DocumentItem): void {
    this.selectedFileSubject.next(file);
  }

  getSelectedFile(): DocumentItem | null {
    return this.selectedFileSubject.getValue();
  }
}
