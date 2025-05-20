import { Component } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';

@Component({
  standalone: true,
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css'],
  imports: [CommonModule, FormsModule, RouterModule],
})
export class LoginComponent {
  username = '';
  password = '';
  error = false;

  constructor(
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute,
  ) {}

  login() {
    if (this.authService.login(this.username, this.password)) {
      const returnUrl =
        this.route.snapshot.queryParamMap.get('returnUrl');

// If no returnUrl or it's file-view from previous session, override it
      if (!returnUrl || returnUrl === '/file-view') {
        this.router.navigate(['/upload']);
      } else {
        this.router.navigate([returnUrl]);
      }
    }
  }
}


