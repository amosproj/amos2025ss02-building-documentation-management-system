import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class DocumentService {
  private uploadedFilesSubject = new BehaviorSubject<File[]>([]);
  public uploadedFiles$ = this.uploadedFilesSubject.asObservable();

  constructor() {}

  /**
   * Add files to the uploaded files list
   */
  addFiles(files: File[]): void {
    const currentFiles = this.uploadedFilesSubject.value;
    const newFiles = [...currentFiles, ...files];
    this.uploadedFilesSubject.next(newFiles);
  }

  /**
   * Get the current list of uploaded files
   */
  getUploadedFiles(): File[] {
    return this.uploadedFilesSubject.value;
  }

  /**
   * Clear all uploaded files
   */
  clearFiles(): void {
    this.uploadedFilesSubject.next([]);
  }
}
