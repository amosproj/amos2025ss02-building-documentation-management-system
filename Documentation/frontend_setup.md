Minimal Angular Frontend Implementation

1. Purpose

This documentation describes how the Angular frontend project implements the user story requirements defined for BitandBeam.

The goal was to build a minimal Angular application that can serve as a foundation for UI development.

2. Setup
The project was created using Angular CLI:
ng new building-ui --directory . --routing --style css

✅ Criterion 1 fulfilled: Angular CLI was used to generate the workspace and project with routing support.

All files are located inside the BitandBeam/frontend/ folder.

3. Routing and Structure
Routing is configured in src/app/app-routes.ts:
const routes: Routes = [
  { path: '', component: HomeComponent },
  { path: 'upload', component: UploadFileComponent }
];
✅ Criterion 2 fulfilled:
The HomeComponent is accessible at / and displays a welcome message.

✅ Criterion 3 fulfilled:
The UploadFileComponent is accessible at /upload and allows file uploading.

4. Components

4.1 HomeComponent
Location: src/app/home/
Displays a static text: "Welcome to BUILD.ING".
Provides a navigation link to the upload page.
HTML (home.component.html):
<h1>Welcome to BUILD.ING</h1>
<a routerLink="/upload">Upload Files</a>
✅ Matches Criterion 2 exactly.

4.2 UploadFileComponent
Location: src/app/upload-file/
Allows the user to:
Upload files by selecting via file input
Upload files by drag-and-dropping them
Displays the names of uploaded files after selection.
HTML (upload-file.component.html):
<div
  (drop)="onFileDropped($event)"
  (dragover)="onDragOver($event)"
  (dragleave)="onDragLeave($event)"
  [class.dragover]="isDragOver"
  class="upload-drop-zone"
>
  <p>Drag & Drop your files here</p>
</div>

<input type="file" (change)="onFileSelected($event)" multiple />

<div class="uploaded-files" *ngIf="files.length > 0">
  <h3>Uploaded Files:</h3>
  <ul>
    <li *ngFor="let file of files">{{ file.name }}</li>
  </ul>
</div>
✅ Matches Criterion 3 (upload field with drag-and-drop or button).
✅ Matches Criterion 4 (uploaded files displayed in the interface).

5. Running the Project
To run the project locally:
1.Install dependencies: npm install
2. Serve the application: ng serve

6. Running Tests
Unit tests can be executed with:
ng test
Tests validate:
HomeComponent creation 
UploadFileComponent creation and basic file upload handling