import { Injectable } from '@angular/core';
import { BehaviorSubject, switchMap, map, Observable, from } from 'rxjs';
import { ConfigService } from '../config.service';
import { AxiosResponse } from 'axios';
import {
  Configuration,
  DocumentsApi,
  Document as ApiDocument,
  BuildingsApi,
  Building as ApiBuilding
} from '../../api';
import { ApiClientFactory } from './api-client.factory';
import { SessionService } from './session.service';

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
  private buildingsSubject = new BehaviorSubject<ApiBuilding[]>([]);
  buildings$ = this.buildingsSubject.asObservable();
  private buildingsApi: BuildingsApi;
  private documentsApi: DocumentsApi;



  constructor(
    private config: ConfigService,
    private apiFactory: ApiClientFactory,
    private session: SessionService
    

  ) {
    this.buildingsApi = this.apiFactory.create(BuildingsApi);
    this.documentsApi = this.apiFactory.create(DocumentsApi);
  }

  // Buildings
  getBuildings(): Observable<Building[]> {
    const buildingsApi = this.apiFactory.create(BuildingsApi);

    return from(
      buildingsApi.apiBuildingsGet().then(res =>
        res.data.map(apiB => ({
          id: apiB.buildingId!,
          name: apiB.name ?? '',
          documents: []
        }))
      )
    );
  }

  addBuilding(building: Partial<ApiBuilding>): Observable<Building> {
    const buildingsApi = this.apiFactory.create(BuildingsApi);

    return from(buildingsApi.apiBuildingsPost(building)).pipe(
      switchMap(res => {
        const createdId = (res.data as unknown as { id: number }).id;
        return from(buildingsApi.apiBuildingsIdGet(createdId)).pipe(
          map(b => {
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
    const buildingsApi = this.apiFactory.create(BuildingsApi);

    return from(buildingsApi.apiBuildingsIdDelete(id).then(() => {}));
  }

  // Documents
  getDocumentById(id: number): Observable<ApiDocument> {
    const documentsApi = this.apiFactory.create(DocumentsApi);

    return from(
      documentsApi.apiDocumentsIdGet(id).then(res =>
        (res as unknown as AxiosResponse<ApiDocument>).data
      )
    );
  }

  deleteDocument(id: number): Observable<void> {
    const documentsApi = this.apiFactory.create(DocumentsApi);

    return from(documentsApi.apiDocumentsIdDelete(id).then(() => {}));
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
