import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HomeComponent } from './home.component';
import { UploadFileComponent } from '../../components/upload-file/upload-file.component';
import { CommonModule } from '@angular/common'; // Import CommonModule for ngFor, etc.
import { RouterModule } from '@angular/router'; // Import RouterModule for routing
import { By } from '@angular/platform-browser'; // Import By for query selector

describe('HomeComponent', () => {
  let component: HomeComponent;
  let fixture: ComponentFixture<HomeComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CommonModule, RouterModule, HomeComponent, UploadFileComponent], // Import both HomeComponent and UploadFileComponent as standalone components
    }).compileComponents();

    fixture = TestBed.createComponent(HomeComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create HomeComponent', () => {
    expect(component).toBeTruthy();
  });

  it('should contain the upload-file component', () => {
    // Find the upload-file component in the template
    const uploadFileComponent = fixture.debugElement.query(By.css('app-upload-file'));

    // Assert that the upload-file component is found
    expect(uploadFileComponent).toBeTruthy();
  });
});
