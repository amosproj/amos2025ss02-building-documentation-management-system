import { Injectable } from '@angular/core';
import { BehaviorSubject, switchMap, map, Observable , from} from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { ConfigService } from '../config.service';
import { AxiosResponse } from 'axios';
import { Configuration, DocumentsApi, Document as ApiDocument, BuildingsApi,
  Building as ApiBuilding } from '../../api';

export interface DocumentItem {
  id: number;
  name: string;
  url: string;
  metadata: { label: string; value: string }[];
}

export interface DocumentResponse {
  documentId: number;
  title: string;
  fileName: string;
  filePath?: string;
  fileSize: number;
  fileType: string;
  uploadDate: string;
  buildingId?: number | null;
  categoryId?: number | null;
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

  addBuilding(building: Partial<ApiBuilding>): Observable<Building> {
    return from(this.buildingsApi.apiBuildingsPost(building)).pipe(
      switchMap((res) => {
        const createdId = (res.data as unknown as { id: number }).id;

        return from(this.buildingsApi.apiBuildingsIdGet(createdId)).pipe(
          map((b) => {
            const fetchedBuilding = b.data as ApiBuilding;
            return {
              id: fetchedBuilding.buildingId!,
              name: fetchedBuilding.name ?? '',
              documents: []
            } as Building;
          })
        );
      })
    );
  }
  
  createBuilding(building: { name: string, [key: string]: any }, sourceId?: number): Observable<Building> {
    // Create API building object
    const apiBuilding: Partial<ApiBuilding> = {
      name: building.name,
      // Add any other properties needed
    };
    
    // If sourceId is provided, we're cloning from an existing building
    if (sourceId) {
      return from(this.buildingsApi.apiBuildingsIdGet(sourceId)).pipe(
        switchMap(sourceResponse => {
          const sourceBuilding = sourceResponse.data as ApiBuilding;
          // Copy relevant properties from source building
          // (e.g., floor plans, metadata, etc. - adjust as needed)
          
          return from(this.buildingsApi.apiBuildingsPost(apiBuilding));
        }),
        switchMap(response => {
          const createdId = (response.data as unknown as { id: number }).id;
          return from(this.buildingsApi.apiBuildingsIdGet(createdId));
        }),
        map(response => {
          const fetchedBuilding = response.data as ApiBuilding;
          return {
            id: fetchedBuilding.buildingId!,
            name: fetchedBuilding.name ?? '',
            documents: []
          } as Building;
        })
      );
    } else {
      // If no sourceId, just create a new building
      return this.addBuilding(apiBuilding);
    }
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
