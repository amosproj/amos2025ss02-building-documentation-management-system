import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth.service';
import { FormsModule } from '@angular/forms';


@Component({
  standalone: true,
  selector: 'app-sidebar',
  templateUrl: './sidebar.component.html',
  styleUrls: ['./sidebar.component.css'],
  imports: [CommonModule,FormsModule]
})
export class SidebarComponent {
  isExplorerCollapsed = false;


  buildings: { name: string; documents: string[] }[] = [];

  constructor(
    public authService: AuthService,
    private router: Router

  ) {}

  toggleExplorer() {
    this.isExplorerCollapsed = !this.isExplorerCollapsed;
    console.log('Explorer collapsed:', this.isExplorerCollapsed);
  }

  goToFileView() {
    this.router.navigate(['/file-view']);
  }
  addBuilding() {
    const name = prompt('Enter building name:');
    if (name) {
      this.buildings.push({ name, documents: [] });
    }
  }
  deleteBuilding(index: number) {
    this.buildings.splice(index, 1);
  }

}
