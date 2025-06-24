import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { BuildingService } from '../../services/building.service';
import { Building as ApiBuilding } from '../../../api';

@Component({
  selector: 'app-create-building',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './create-building.component.html',
  styleUrls: ['./create-building.component.css']
})
export class CreateBuildingComponent {
  // Hardcoded organizations for now
  organizations = [
    {id: 1, name: 'Organization Alpha'},
    {id: 2, name: 'Organization Beta'}
  ];

  // Initialize the building object
  building: Partial<ApiBuilding> & { latitude?: number; longitude?: number } = {
    name: '',
    streetName: '',
    houseNumber:'',
    postalCode: '',
    city: '',
    country: '',
    constructionYear: null,
    totalArea: null,
    floors: null,
    description: '',
    organizationId: undefined,
    latitude: undefined,
    longitude: undefined
  };


  successMessage = '';
  errorMessage = '';

  constructor(private buildingService: BuildingService, private router: Router) {
  }

  submitForm() {
    if (!this.building.name?.trim() || !this.building.streetName?.trim() || !this.building.houseNumber?.trim() || !this.building.postalCode?.trim() 
      || !this.building.city?.trim() || !this.building.country?.trim() || !this.building.organizationId) {
      this.errorMessage = 'Name, Address, and Organization are required.';
      return;
    }

    // Create NpgsqlPoint from latitude and longitude
    const coordinates = (this.building.latitude != null && this.building.longitude != null)
      ? {x: this.building.longitude, y: this.building.latitude} // PostgreSQL point: (longitude, latitude)
      : undefined;

    const building: Partial<ApiBuilding> = {
      name: this.building.name,
      streetName: this.building.streetName,
      houseNumber: this.building.houseNumber,
      postalCode: this.building.postalCode,
      city: this.building.city,
      country: this.building.country,
      constructionYear: this.building.constructionYear,
      totalArea: this.building.totalArea,
      floors: this.building.floors,
      description: this.building.description,
      organizationId: this.building.organizationId,
      buildingDocumentRelations: [],
      coordinates: coordinates
    };

    console.log('Creating building with data:', building);

    this.buildingService.addBuilding(building).subscribe({
      next: () => {
        this.successMessage = 'Building created successfully!';
        this.errorMessage = '';
        setTimeout(() => this.router.navigate(['/upload']), 1500);
      },
      error: (err) => {
        console.error(err);
        this.errorMessage = 'Failed to create building. Please try again.';
      }
    });
  }
}
