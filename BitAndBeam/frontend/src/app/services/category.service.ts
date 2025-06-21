import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, from } from 'rxjs';
import { ConfigService } from '../config.service';
import { Configuration, DocumentsApi } from '../../api';
import { HttpClient } from '@angular/common/http';

export interface Category {
  id: number;
  name: string;
}

@Injectable({ providedIn: 'root' })
export class CategoryService {
  private documentsApi: DocumentsApi;
  private categoriesSubject = new BehaviorSubject<Category[]>([]);
  categories$ = this.categoriesSubject.asObservable();

  // Default categories - in a real application, these would come from the backend
  private defaultCategories: Category[] = [
    { id: 1, name: 'Architecture' },
    { id: 2, name: 'Electrical' },
    { id: 3, name: 'Plumbing' },
    { id: 4, name: 'HVAC' },
    { id: 5, name: 'Structural' },
    { id: 6, name: 'Civil' },
    { id: 7, name: 'Mechanical' },
    { id: 8, name: 'Other' }
  ];

  constructor(private config: ConfigService, private http: HttpClient) {
    const configuration = new Configuration({ basePath: this.config.apiUrl });
    this.documentsApi = new DocumentsApi(configuration);

    // Initialize with default categories
    this.categoriesSubject.next(this.defaultCategories);
  }

  getCategories(): Observable<Category[]> {
    return this.categories$;
  }

  // In a real application, this would send the document category to the backend
  assignDocumentCategory(documentId: number, categoryId: number | null, buildingId: number | null): Observable<any> {
    const token = sessionStorage.getItem('jwt_token');

    const headers = {
      Authorization: `Bearer ${token}`,
      'Content-Type': 'application/json'
    };

    return this.http.patch<any>(
      `${this.config.apiUrl}/api/Documents/${documentId}`,
      { categoryId, buildingId },
      { headers }
    );
  }

  // Create a new category
  createCategory(category: { name: string }): Observable<Category> {
    // In a real application, this would send the new category to the backend
    // For now, we'll just add it to our local array with a new ID
    const newCategoryId = Math.max(...this.defaultCategories.map(c => c.id)) + 1;
    const newCategory: Category = {
      id: newCategoryId,
      name: category.name
    };

    this.defaultCategories.push(newCategory);
    this.categoriesSubject.next([...this.defaultCategories]);

    return from(Promise.resolve(newCategory));
  }
}
