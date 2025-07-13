import { Component, Input } from '@angular/core';
import { Router } from '@angular/router';

@Component({
  selector: 'app-top-bar',
  standalone: true,
  templateUrl: './top-bar.component.html',
  styleUrls: ['./top-bar.component.css'],
})
export class TopBarComponent {
   @Input() toggleMetadataPanel: () => void = () => {};
  constructor(private router: Router) {}

  // ✅ Navigates to home page
  navigateToHome() {
    this.router.navigate(['/upload']);
  }
}
