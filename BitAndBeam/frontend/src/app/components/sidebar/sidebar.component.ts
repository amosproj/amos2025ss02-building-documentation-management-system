import { Component, EventEmitter,Output  } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth.service';
import { FormsModule } from '@angular/forms';
import { BuildingService , DocumentItem , Building } from '../../services/building.service';

@Component({
  standalone: true,
  selector: 'app-sidebar',
  templateUrl: './sidebar.component.html',
  styleUrls: ['./sidebar.component.css'],
  imports: [CommonModule,FormsModule]
})
export class SidebarComponent {
  isExplorerCollapsed = false;
  buildings: Building[] = [];

  constructor(
    public authService: AuthService,
    private router: Router,
    public buildingService: BuildingService

  ) {}
  ngOnInit(): void {
    this.buildingService.getBuildings().subscribe({
      next: (data) => this.buildings = data,
      error: (err) => console.error('Failed to load buildings', err)
    });
  }

  toggleExplorer() {
    this.isExplorerCollapsed = !this.isExplorerCollapsed;
    console.log('Explorer collapsed:', this.isExplorerCollapsed);
  }

  viewDocument(doc: DocumentItem): void {
    if (!doc.id) {
      console.error('Document has no ID');
      return;
    }
    this.router.navigate(['/documents', doc.id]);
  }

  addBuilding(): void {
    this.router.navigate(['/create-building']);
  }


  deleteBuilding(id: number): void {
    if (!confirm('Are you sure you want to delete this building?')) return;

    this.buildingService.deleteBuilding(id).subscribe({
      next: () => this.buildings = this.buildings.filter(b => b.id !== id),
      error: (err) => console.error('Failed to delete building', err)
    });
  }

  logout() {
    this.authService.logout();
    this.router.navigate(['/']); // redirect to home/landing
  }





}
