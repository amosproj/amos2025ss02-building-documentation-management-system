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
  imports: [CommonModule, FormsModule]
})
export class LoginComponent implements OnInit {
  email = '';
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
      this.router.navigate(['/upload'], {replaceUrl: true});
    }
  }

  login(): void {
    this.authService.login(this.email, this.password).subscribe({
      next: () => {
        const returnUrl =
          this.route.snapshot.queryParamMap.get('returnUrl') || '/upload';
        this.router.navigate([returnUrl], { replaceUrl: true });
      },
      error: (err) => {
        console.error('Login failed', err);
        this.error = true;
      }
    });
    console.log('Login attempted with:', { username: this.email, password: this.password });
    if (this.authService.login(this.email, this.password)) {
      console.log('Login successful');
      const returnUrl =
        this.route.snapshot.queryParamMap.get('returnUrl') || '/upload';

      // ✅ Replace current history entry
      this.router.navigate([returnUrl], {replaceUrl: true});
    } else {
      console.log('Login failed');
      this.error = true;
    }
  }
}
