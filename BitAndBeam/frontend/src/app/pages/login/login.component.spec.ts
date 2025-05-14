import { ComponentFixture, TestBed } from '@angular/core/testing';
import { LoginComponent } from './login.component';
import { AuthService } from '../../services/auth.service';
import { Router, ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { By } from '@angular/platform-browser';
import { of } from 'rxjs';

describe('LoginComponent', () => {
  let component: LoginComponent;
  let fixture: ComponentFixture<LoginComponent>;
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let routerSpy: jasmine.SpyObj<Router>;

  beforeEach(async () => {
    authServiceSpy = jasmine.createSpyObj('AuthService', ['login']);

    routerSpy = jasmine.createSpyObj('Router', ['navigate', 'createUrlTree'], {
      events: of(), // ✅ Required for router.events.subscribe
      serializeUrl: (tree: any) => '/mock-url', // ✅ Return string for routerLink logic
    });

    const activatedRouteStub = {
      snapshot: {
        queryParamMap: {
          get: (key: string) => null,
        },
      },
    };

    await TestBed.configureTestingModule({
      imports: [LoginComponent, FormsModule],
      providers: [
        { provide: AuthService, useValue: authServiceSpy },
        { provide: Router, useValue: routerSpy },
        { provide: ActivatedRoute, useValue: activatedRouteStub },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
  });

  it('should create the component', () => {
    expect(component).toBeTruthy();
  });

  it('should call AuthService.login and navigate on success', () => {
    authServiceSpy.login.and.returnValue(true);
    component.username = 'user1';
    component.password = 'pass1';

    component.login();

    expect(authServiceSpy.login).toHaveBeenCalledWith('user1', 'pass1');
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/upload']);
  });

  it('should set error if login fails', () => {
    authServiceSpy.login.and.returnValue(false);
    component.username = 'wrong';
    component.password = 'wrong';

    component.login();

    expect(component.error).toBeTrue();
  });

  it('should render form with inputs and button', () => {
    fixture.detectChanges();
    const inputs = fixture.debugElement.queryAll(By.css('input'));
    const button = fixture.debugElement.query(By.css('button'));

    expect(inputs.length).toBe(2);
    expect(button.nativeElement.textContent).toContain('Login');
  });
});
