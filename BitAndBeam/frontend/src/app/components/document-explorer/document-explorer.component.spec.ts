import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DocumentExplorerComponent } from './document-explorer.component';

describe('DocumentExplorerComponent', () => {
  let component: DocumentExplorerComponent;
  let fixture: ComponentFixture<DocumentExplorerComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DocumentExplorerComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(DocumentExplorerComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
