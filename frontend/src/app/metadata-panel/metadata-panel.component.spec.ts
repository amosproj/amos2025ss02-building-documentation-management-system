import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MetadataPanelComponent } from './metadata-panel.component';

describe('MetadataPanelComponent', () => {
  let component: MetadataPanelComponent;
  let fixture: ComponentFixture<MetadataPanelComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MetadataPanelComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(MetadataPanelComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
