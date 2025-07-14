import { Component, OnInit } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SessionService } from '../../services/session.service';

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
    private session: SessionService,
    private router: Router,
    private route: ActivatedRoute,
  ) {
    console.log('LoginComponent constructed');
  }

  ngOnInit(): void {
    console.log('LoginComponent initialized');
    if (this.session.isAuthenticated()) {
      this.router.navigate(['/upload'], { replaceUrl: true });
    }
  }

  async login(): Promise<void> {
    console.log('Login attempted with:', {
      username: this.username,
      password: this.password,
    });
    const success = await this.session.login(this.username, this.password);

    if (success) {
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
