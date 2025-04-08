import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { By } from '@angular/platform-browser';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';

import { SignupComponent } from './signup.component';
import { UserService } from '../../../services/user.service';

describe('SignupComponent', () => {
  let component: SignupComponent;
  let fixture: ComponentFixture<SignupComponent>;
  let userServiceMock: any;
  let routerMock: any;

  beforeEach(async () => {
    userServiceMock = {
      register: jest.fn()
    };

    routerMock = {
      navigate: jest.fn()
    };

    await TestBed.configureTestingModule({
      imports: [
        SignupComponent,
        ReactiveFormsModule,
        HttpClientTestingModule,
        RouterTestingModule
      ],
      providers: [
        { provide: UserService, useValue: userServiceMock },
        { provide: Router, useValue: routerMock }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(SignupComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  // Test 1: Component should be created
  it('should create the component', () => {
    expect(component).toBeTruthy();
  });

  // Test 2: Form should be initialized with empty values
  it('should initialize form with empty values', () => {
    expect(component.signupForm.get('username')?.value).toBe('');
    expect(component.signupForm.get('email')?.value).toBe('');
    expect(component.signupForm.get('password')?.value).toBe('');
    expect(component.signupForm.get('fullName')?.value).toBe('');
    expect(component.signupForm.get('role')?.value).toBe('');
  });

  // Test 3: Form should be invalid when empty
  it('should have an invalid form when empty', () => {
    expect(component.signupForm.valid).toBeFalsy();
  });

  // Test 4: Username validation - required
  it('should validate that username is required', () => {
    const usernameControl = component.signupForm.get('username');
    expect(usernameControl?.valid).toBeFalsy();
    expect(usernameControl?.hasError('required')).toBeTruthy();
  });

  // Test 5: Username validation - min length
  it('should validate username minimum length', () => {
    const usernameControl = component.signupForm.get('username');
    
    // Too short username
    usernameControl?.setValue('ab');
    expect(usernameControl?.valid).toBeFalsy();
    expect(usernameControl?.hasError('minlength')).toBeTruthy();
    
    // Valid username length
    usernameControl?.setValue('abc');
    expect(usernameControl?.hasError('minlength')).toBeFalsy();
  });

  // Test 6: Username validation - max length
  it('should validate username maximum length', () => {
    const usernameControl = component.signupForm.get('username');
    
    // Too long username
    const longUsername = 'a'.repeat(21);
    usernameControl?.setValue(longUsername);
    expect(usernameControl?.valid).toBeFalsy();
    expect(usernameControl?.hasError('maxlength')).toBeTruthy();
    
    // Valid username length
    const validUsername = 'a'.repeat(20);
    usernameControl?.setValue(validUsername);
    expect(usernameControl?.hasError('maxlength')).toBeFalsy();
  });

  // Test 7: Username validation - pattern
  it('should validate username pattern', () => {
    const usernameControl = component.signupForm.get('username');
    
    // Invalid username with special characters
    usernameControl?.setValue('user@name');
    expect(usernameControl?.valid).toBeFalsy();
    expect(usernameControl?.hasError('pattern')).toBeTruthy();
    
    // Valid username
    usernameControl?.setValue('user_name-123');
    expect(usernameControl?.hasError('pattern')).toBeFalsy();
  });

  // Test 8: Email validation - required
  it('should validate that email is required', () => {
    const emailControl = component.signupForm.get('email');
    expect(emailControl?.valid).toBeFalsy();
    expect(emailControl?.hasError('required')).toBeTruthy();
  });

  // Test 9: Email validation - pattern
  it('should validate email format', () => {
    const emailControl = component.signupForm.get('email');
    
    // Invalid email
    emailControl?.setValue('invalid-email');
    expect(emailControl?.valid).toBeFalsy();
    expect(emailControl?.hasError('pattern') || emailControl?.hasError('email')).toBeTruthy();
    
    // Valid email
    emailControl?.setValue('valid@example.com');
    expect(emailControl?.valid).toBeTruthy();
  });

  // Test 10: Password validation - required
  it('should validate that password is required', () => {
    const passwordControl = component.signupForm.get('password');
    expect(passwordControl?.valid).toBeFalsy();
    expect(passwordControl?.hasError('required')).toBeTruthy();
  });

  // Test 11: Password validation - min length
  it('should validate password minimum length', () => {
    const passwordControl = component.signupForm.get('password');
    
    // Too short password
    passwordControl?.setValue('12345');
    expect(passwordControl?.valid).toBeFalsy();
    expect(passwordControl?.hasError('minlength')).toBeTruthy();
    
    // Valid password length
    passwordControl?.setValue('123456');
    expect(passwordControl?.hasError('minlength')).toBeFalsy();
  });

  // Test 12: Full Name validation - required
  it('should validate that full name is required', () => {
    const fullNameControl = component.signupForm.get('fullName');
    expect(fullNameControl?.valid).toBeFalsy();
    expect(fullNameControl?.hasError('required')).toBeTruthy();
  });

  // Test 13: Full Name validation - pattern
  it('should validate full name format', () => {
    const fullNameControl = component.signupForm.get('fullName');
    
    // Invalid full name with numbers
    fullNameControl?.setValue('John123 Doe');
    expect(fullNameControl?.valid).toBeFalsy();
    expect(fullNameControl?.hasError('pattern')).toBeTruthy();
    
    // Valid full names
    fullNameControl?.setValue('John Doe');
    expect(fullNameControl?.valid).toBeTruthy();
    
    fullNameControl?.setValue('Mary-Jane O\'Connor');
    expect(fullNameControl?.valid).toBeTruthy();
  });

  // Test 14: Role validation - required
  it('should validate that role is required', () => {
    const roleControl = component.signupForm.get('role');
    expect(roleControl?.valid).toBeFalsy();
    expect(roleControl?.hasError('required')).toBeTruthy();
  });

  // Test 15: Form should be valid with proper data
  it('should have a valid form when all fields are properly filled', () => {
    component.signupForm.setValue({
      username: 'testuser',
      email: 'test@example.com',
      password: 'password123',
      fullName: 'Test User',
      role: '1'
    });
    expect(component.signupForm.valid).toBeTruthy();
  });

  // Test 16: Show/hide password toggle
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

  // Test 17: Password strength indicator - weak
  it('should display weak password strength indicator for short passwords', () => {
    const passwordControl = component.signupForm.get('password');
    
    passwordControl?.setValue('123');
    fixture.detectChanges();
    
    expect(component.getPasswordStrengthClass()).toBe('weak');
    expect(component.getPasswordStrengthText()).toBe('Weak password');
  });

  // Test 18: Password strength indicator - medium
  it('should display medium password strength indicator for decent passwords', () => {
    const passwordControl = component.signupForm.get('password');
    
    passwordControl?.setValue('abc123');
    fixture.detectChanges();
    
    expect(component.getPasswordStrengthClass()).toBe('medium');
    expect(component.getPasswordStrengthText()).toBe('Medium strength password');
  });

  // Test 19: Password strength indicator - strong
  it('should display strong password strength indicator for complex passwords', () => {
    const passwordControl = component.signupForm.get('password');
    
    passwordControl?.setValue('Abc123!@#');
    fixture.detectChanges();
    
    expect(component.getPasswordStrengthClass()).toBe('strong');
    expect(component.getPasswordStrengthText()).toBe('Strong password');
  });

  // Test 23: Loading state during registration
  it('should show loading state during registration attempt', () => {
    // Mock a delayed response
    userServiceMock.register.mockReturnValue(of({ success: true }));
    
    component.signupForm.setValue({
      username: 'testuser',
      email: 'test@example.com',
      password: 'password123',
      fullName: 'Test User',
      role: '1'
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
    expect(buttonText.nativeElement.textContent).toContain('Creating account...');
  });

  // Test 24: Navigation to login page
  it('should navigate to login page when "Log In" button is clicked', () => {
    const loginButton = fixture.debugElement.query(By.css('.secondary-button'));
    loginButton.triggerEventHandler('click', null);
    
    expect(routerMock.navigate).toHaveBeenCalledWith(['/login']);
  });

  // Test 25: Form validation on submit
  it('should not call userService.register if form is invalid', () => {
    // Leave the form invalid (empty)
    component.onSubmit();
    
    // The register method should not be called
    expect(userServiceMock.register).not.toHaveBeenCalled();
  });
});
