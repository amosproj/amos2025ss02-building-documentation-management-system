import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, from, of } from 'rxjs';
import { ConfigService } from '../config.service';
import { Configuration, DocumentsApi } from '../../api';
import { ApiClientFactory } from './api-client.factory';

export interface Category {
  name: string;
  description?: string;
  fields?: CategoryField[];
}

export interface CategoryField {
  name: string;
  description: string;
  mandatory: boolean;
}

@Injectable({ providedIn: 'root' })
export class CategoryService {
  private documentsApi: DocumentsApi;
  private categoriesSubject = new BehaviorSubject<Category[]>([]);
  categories$ = this.categoriesSubject.asObservable();

  constructor(private config: ConfigService, private apiFactory: ApiClientFactory) {
    this.documentsApi = this.apiFactory.create(DocumentsApi);

    // Load categories from backend on initialization
    this.loadCategoriesFromBackend();
  }

  private loadCategoriesFromBackend(): void {
    this.documentsApi.apiDocumentsCategoriesGet()
      .then((response) => {
        // Handle the response properly - it might be directly the data or wrapped in response.data
        const categories = (response as any)?.data || response || [];
        this.categoriesSubject.next(categories as Category[]);
      })
      .catch((error) => {
        console.error('Failed to load categories from backend:', error);
        // Return empty array on error
        this.categoriesSubject.next([]);
      });
  }

  getCategories(): Observable<Category[]> {
    return this.categories$;
  }

  // Assign document category using the OpenAPI SDK
  assignDocumentCategory(documentId: number, categoryName: string | null, buildingId: number | null): Observable<any> {
    const patchRequest = { categoryName, buildingId };
    
    return from(
      this.documentsApi.apiDocumentsIdPatch(documentId, patchRequest)
        .then(response => response || {})
    );
  }

  // Create a new category
  createCategory(category: { name: string; description?: string }): Observable<Category> {
    // In a real application, this would send the new category to the backend
    // For now, we'll just add it to our local array
    const newCategory: Category = {
      name: category.name,
      description: category.description,
      fields: []
    };

    const currentCategories = this.categoriesSubject.value;
    const updatedCategories = [...currentCategories, newCategory];
    this.categoriesSubject.next(updatedCategories);

    return from(Promise.resolve(newCategory));
  }

  // Refresh categories from backend
  refreshCategories(): void {
    this.loadCategoriesFromBackend();
  }
}
