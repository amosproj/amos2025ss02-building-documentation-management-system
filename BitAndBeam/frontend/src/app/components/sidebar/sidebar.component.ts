import { Component, EventEmitter, Output, Input } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { SessionService } from '../../services/session.service';
import { FormsModule } from '@angular/forms';
import {
  BuildingService,
  DocumentItem,
  Building,
} from '../../services/building.service';
import { SidebarRefreshService } from '../../services/sidebar-refresh.service';
import { ThemeService, ThemeMode } from '../../services/theme.service';
import {
  trigger,
  transition,
  style,
  animate,
  state,
} from '@angular/animations';

@Component({
  standalone: true,
  selector: 'app-sidebar',
  templateUrl: './sidebar.component.html',
  styleUrls: ['./sidebar.component.css'],
  imports: [CommonModule, FormsModule],
  animations: [
    trigger('dropdownAnimation', [
      transition(':enter', [
        style({ opacity: 0, transform: 'translateY(10px)' }),
        animate(
          '200ms ease-out',
          style({ opacity: 1, transform: 'translateY(0)' }),
        ),
      ]),
      transition(':leave', [
        animate(
          '150ms ease-in',
          style({ opacity: 0, transform: 'translateY(10px)' }),
        ),
      ]),
    ]),
  ],
})
export class SidebarComponent {
  @Input() currentRoute: string = '';
  isDarkMode = false;
  themeMode: ThemeMode = 'device';
  isExplorerCollapsed = false;
  profileMenuOpen = false;
  expandedBuildings: Set<number | null> = new Set();

  groupedDocuments: {
    buildingId: number | null;
    buildingName: string;
    documents: DocumentItem[];
    isExpanded?: boolean;
  }[] = [];

  currentDocument: DocumentItem | null = null;

  constructor(
    public session: SessionService,
    private router: Router,
    private route: ActivatedRoute,
    public buildingService: BuildingService,
    private sidebarRefreshService: SidebarRefreshService,
    private themeService: ThemeService,
  ) {}

  ngOnInit(): void {
    // Ensure session is valid
    this.session.ensureSessionValid();

    // Get current document id from route if present
    let currentDocId: string | null = null;
    this.route.paramMap.subscribe((params) => {
      currentDocId = params.get('id');
    });

    // Load expanded buildings state from localStorage
    try {
      const savedExpanded = localStorage.getItem('expandedBuildings');
      if (savedExpanded) {
        const expandedIds = JSON.parse(savedExpanded);
        this.expandedBuildings = new Set(expandedIds);
      }
    } catch (e) {
      console.error('Error loading expanded buildings state', e);
    }
    // Load documents
    this.buildingService.getGroupedDocuments().subscribe({
      next: (data) => {
        this.groupedDocuments = data;
        // Apply expansion state to loaded documents
        this.updateDocumentExpansionState();
        // Set currentDocument if route has id
        if (currentDocId) {
          for (const group of this.groupedDocuments) {
            const found = group.documents.find(
              (doc) => String(doc.id) === String(currentDocId),
            );
            if (found) {
              this.currentDocument = found;
              break;
            }
          }
        }
      },
      error: (err) => console.error('Failed to load grouped documents', err),
    });

    // Subscribe to sidebar refresh
    this.sidebarRefreshService.refresh$.subscribe(() => {
      console.log('📣 Sidebar refresh triggered');
      this.buildingService.getGroupedDocuments().subscribe({
        next: (data) => (this.groupedDocuments = data),
        error: (err) => console.error('Sidebar refresh failed', err),
      });
    });

    // Subscribe to theme changes
    this.isDarkMode = this.themeService.isDarkMode();
    this.themeService.darkMode$.subscribe((isDark) => {
      this.isDarkMode = isDark;
    });
    // Subscribe to mode changes
    this.themeMode = this.themeService.getMode();
    this.themeService.mode$.subscribe((mode) => {
      this.themeMode = mode;
    });
  }

  toggleExplorer() {
    this.isExplorerCollapsed = !this.isExplorerCollapsed;
    console.log('Explorer collapsed:', this.isExplorerCollapsed);
  }

  toggleBuildingExpansion(buildingId: number | null, event?: MouseEvent): void {
    // Stop propagation if event is provided to prevent navigation
    if (event) {
      event.stopPropagation();
      event.preventDefault();
    }

    if (this.expandedBuildings.has(buildingId)) {
      this.expandedBuildings.delete(buildingId);
    } else {
      this.expandedBuildings.add(buildingId);
    }

    // Save expanded state to localStorage
    this.saveExpandedState();

    // Update the groupedDocuments with expansion state
    this.updateDocumentExpansionState();
  }

  isBuildingExpanded(buildingId: number | null): boolean {
    return this.expandedBuildings.has(buildingId);
  }

  private saveExpandedState(): void {
    try {
      const expandedArray = Array.from(this.expandedBuildings);
      localStorage.setItem('expandedBuildings', JSON.stringify(expandedArray));
    } catch (e) {
      console.error('Error saving expanded buildings state', e);
    }
  }

  private updateDocumentExpansionState(): void {
    this.groupedDocuments = this.groupedDocuments.map((group) => ({
      ...group,
      isExpanded: this.expandedBuildings.has(group.buildingId),
    }));
  }

  viewDocument(doc: DocumentItem): void {
    if (!doc.id) {
      console.error('Document has no ID');
      return;
    }
    this.currentDocument = doc;
    this.router.navigate(['/documents', doc.id]);
  }

  addBuilding(): void {
    this.router.navigate(['/create-building']);
  }

  navigateToHome(): void {
    console.log(
      '🏠 Navigating to home, checking user state before navigation:',
    );
    this.session.debugUserState();
    this.router.navigate(['/upload']);
  }

  deleteBuilding(buildingId: number) {
    if (confirm('Are you sure you want to delete this building?')) {
      this.buildingService.deleteBuilding(buildingId).subscribe({
        next: () => {
          console.log('Building deleted');
          // Refresh data after deletion
          this.buildingService.getGroupedDocuments().subscribe({
            next: (data) => (this.groupedDocuments = data),
            error: (err) =>
              console.error('Failed to reload grouped documents', err),
          });
        },
        error: (err) => console.error('Failed to delete building', err),
      });
    }
  }

  onThemeModeChange(event: Event) {
    const value = (event.target as HTMLSelectElement).value as ThemeMode;
    this.themeService.setMode(value);
  }

  toggleProfileMenu() {
    this.profileMenuOpen = !this.profileMenuOpen;
  }

  openSettings() {
    // Placeholder for settings modal/page
    alert('Settings coming soon!');
  }

  logout() {
    this.session.logout();
    this.router.navigate(['/']);
  }
}
