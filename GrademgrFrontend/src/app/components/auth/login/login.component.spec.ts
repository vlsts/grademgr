import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { By } from '@angular/platform-browser';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';

import { LoginComponent } from './login.component';
import { UserService } from '../../../services/user.service';

describe('LoginComponent', () => {
  let component: LoginComponent;
  let fixture: ComponentFixture<LoginComponent>;
  let userServiceMock: any;
  let routerMock: any;

  beforeEach(async () => {
    userServiceMock = {
      login: jest.fn()
    };

    routerMock = {
      navigate: jest.fn()
    };

    await TestBed.configureTestingModule({
      imports: [
        LoginComponent,
        ReactiveFormsModule,
        HttpClientTestingModule,
        RouterTestingModule
      ],
      providers: [
        { provide: UserService, useValue: userServiceMock },
        { provide: Router, useValue: routerMock }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  // Test 1: Component should be created
  it('should create the component', () => {
    expect(component).toBeTruthy();
  });

  // Test 2: Form should be initialized with empty values
  it('should initialize the form with empty email and password fields', () => {
    expect(component.loginForm.get('email')?.value).toBe('');
    expect(component.loginForm.get('password')?.value).toBe('');
  });

  // Test 3: Form should be invalid when empty
  it('should have an invalid form when empty', () => {
    expect(component.loginForm.valid).toBeFalsy();
  });

  // Test 4: Email validation - required
  it('should validate that email is required', () => {
    const emailControl = component.loginForm.get('email');
    emailControl?.setValue('');
    expect(emailControl?.valid).toBeFalsy();
    expect(emailControl?.hasError('required')).toBeTruthy();
  });

  // Test 5: Email validation - pattern
  it('should validate email format', () => {
    const emailControl = component.loginForm.get('email');
    
    // Invalid email
    emailControl?.setValue('invalid-email');
    expect(emailControl?.valid).toBeFalsy();
    expect(emailControl?.hasError('pattern')).toBeTruthy();
    
    // Valid email
    emailControl?.setValue('valid@example.com');
    expect(emailControl?.valid).toBeTruthy();
  });

  // Test 6: Password validation - required
  it('should validate that password is required', () => {
    const passwordControl = component.loginForm.get('password');
    passwordControl?.setValue('');
    expect(passwordControl?.valid).toBeFalsy();
    expect(passwordControl?.hasError('required')).toBeTruthy();
  });

  // Test 7: Password validation - min length
  it('should validate password minimum length', () => {
    const passwordControl = component.loginForm.get('password');
    
    // Too short password
    passwordControl?.setValue('12345');
    expect(passwordControl?.valid).toBeFalsy();
    expect(passwordControl?.hasError('minlength')).toBeTruthy();
    
    // Valid password length
    passwordControl?.setValue('123456');
    expect(passwordControl?.hasError('minlength')).toBeFalsy();
  });

  // Test 8: Form should be valid with proper data
  it('should have a valid form when all fields are properly filled', () => {
    component.loginForm.setValue({
      email: 'test@example.com',
      password: 'password123'
    });
    expect(component.loginForm.valid).toBeTruthy();
  });

  // Test 9: Show/hide password toggle
  it('should toggle password visibility when button is clicked', () => {
    // Initial state should be hidden
    expect(component.showPassword).toBeFalsy();
    
    // Find and click the toggle button
    const toggleButton = fixture.debugElement.query(By.css('.toggle-password'));
    toggleButton.triggerEventHandler('click', null);
    fixture.detectChanges();
    
    // Password should now be visible
    expect(component.showPassword).toBeTruthy();
    
    // Click again to hide
    toggleButton.triggerEventHandler('click', null);
    fixture.detectChanges();
    
    // Password should be hidden again
    expect(component.showPassword).toBeFalsy();
  });

  // Test 14: Loading state during login
  it('should show loading state during login attempt', () => {
    // Mock a delayed response
    userServiceMock.login.mockReturnValue(of({ token: 'fake-token', role: 'Student' }));
    
    component.loginForm.setValue({
      email: 'test@example.com',
      password: 'password123'
    });
    
    // Submit the form
    component.onSubmit();
    fixture.detectChanges();
    
    // Check if isLoading is true during the process
    expect(component.isLoading).toBeTruthy();
    
    // Check if the loading spinner is displayed
    const spinner = fixture.debugElement.query(By.css('.spinner'));
    expect(spinner).toBeTruthy();
    
    // Check if button text changed
    const buttonText = fixture.debugElement.query(By.css('button[type="submit"] span:last-child'));
    expect(buttonText.nativeElement.textContent).toContain('Logging in...');
  });

  // Test 15: Navigation to signup page
  it('should navigate to signup page when "Sign Up" button is clicked', () => {
    const signupButton = fixture.debugElement.query(By.css('.secondary-button'));
    signupButton.triggerEventHandler('click', null);
    
    expect(routerMock.navigate).toHaveBeenCalledWith(['/signup']);
  });
});
