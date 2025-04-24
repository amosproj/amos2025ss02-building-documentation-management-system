import { ComponentFixture, TestBed } from '@angular/core/testing';

import { UploadFileComponent } from './upload-file.component';

import { By } from '@angular/platform-browser';

describe('UploadFileComponent', () => {
  let component: UploadFileComponent;
  let fixture: ComponentFixture<UploadFileComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [UploadFileComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(UploadFileComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create UploadFileComponent', () => {
    expect(component).toBeTruthy();
  });

  it('should display file name after file selection', () => {
    // Arrange: Create a mock file
    const mockFile = new File([''], 'selected-file.txt', { type: 'text/plain' });
  
    // Create a DataTransfer object with the mock file
    const dataTransfer = new DataTransfer();
    dataTransfer.items.add(mockFile);  // Adding the file to the dataTransfer
  
    // Get the file input element
    const inputElement: HTMLInputElement = fixture.debugElement.query(By.css('input[type="file"]')).nativeElement;
  
    // Mock the 'files' property of the input element
    inputElement.files = dataTransfer.files; // This properly sets the 'files' property
  
    // Act: Trigger the change event to simulate file selection
    const event = new Event('change');
    inputElement.dispatchEvent(event);
  
    fixture.detectChanges(); // Update the view
  
    // Assert: Check if file name is displayed
    const fileNameDisplay = fixture.debugElement.query(By.css('div > p')).nativeElement;
    expect(fileNameDisplay.textContent).toContain(mockFile.name);
  });
  

  it('should display file name after file is dropped', () => {
    // Arrange: Create a mock file
    const mockFile = new File([''], 'dropped-file.txt', { type: 'text/plain' });
  
    // Create a DataTransfer object with the mock file
    const dataTransfer = new DataTransfer();
    dataTransfer.items.add(mockFile);  // Adding the file to the dataTransfer
  
    // Create a DragEvent with the DataTransfer object
    const dropEvent = new DragEvent('drop', {
      dataTransfer: dataTransfer,  // Correctly attach the DataTransfer object here
    });
  
    // Act: Find the drop zone and dispatch the drop event
    const dropZone = fixture.debugElement.query(By.css('.upload-dropzone')).nativeElement;
    dropZone.dispatchEvent(dropEvent);
  
    fixture.detectChanges(); // Update the view
  
    // Assert: Check if file name is displayed
    const fileNameDisplay = fixture.debugElement.query(By.css('div > p')).nativeElement;
    expect(fileNameDisplay.textContent).toContain(mockFile.name);
  });
      

  it('should not display file name when no file is selected', () => {
    // Act: Don't select any file
    fixture.detectChanges();

    // Assert: File name should not be displayed
    const fileNameDisplay = fixture.debugElement.query(By.css('div > p'));
    expect(fileNameDisplay).toBeNull();
  });
});
