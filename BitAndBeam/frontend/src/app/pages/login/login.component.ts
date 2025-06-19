import { Component, OnInit } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-login',
  standalone: true,
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css'],
  imports: [CommonModule, FormsModule],
})
export class LoginComponent implements OnInit {
  username = '';
  password = '';
  error = false;

  constructor(
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute
  ) {
    console.log('LoginComponent constructed');
  }

  ngOnInit(): void {
    console.log('LoginComponent initialized');
    // Redirect logged-in user away from login page
    if (this.authService.isAuthenticated()) {
      this.router.navigate(['/upload'], { replaceUrl: true });
    }
  }

  login(): void {
    console.log('Login attempted with:', { username: this.username, password: this.password });
    if (this.authService.login(this.username, this.password)) {
      console.log('Login successful');
      const returnUrl =
        this.route.snapshot.queryParamMap.get('returnUrl') || '/upload';

      // ✅ Replace current history entry
      this.router.navigate([returnUrl], { replaceUrl: true });
    } else {
      console.log('Login failed');
      this.error = true;
    }
  }
}
