import { ComponentFixture, TestBed } from '@angular/core/testing';

import { UploadFileComponent } from './upload-file.component';
import { CommonModule } from '@angular/common';

describe('UploadFileComponent', () => {
  let component: UploadFileComponent;
  let fixture: ComponentFixture<UploadFileComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        CommonModule,
        UploadFileComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(UploadFileComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
  it('should add selected files to the files array', () => {
    const mockFile = new File(['dummy content'], 'example.txt', { type: 'text/plain' });
    const event = {
      target: {
        files: [mockFile]
      }
    } as any;

    component.onFileSelected(event);
    expect(component.files.length).toBe(1);
    expect(component.files[0].name).toBe('example.txt');
  });
});
