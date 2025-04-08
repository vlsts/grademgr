import { Component, OnInit } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { CommonModule } from '@angular/common'; 
import { Router } from '@angular/router';
import { UserService } from '../../../services/user.service';

@Component({
  selector: 'app-signup',
  standalone: true,
  imports: [ReactiveFormsModule, HttpClientModule, CommonModule],
  providers: [UserService],
  templateUrl: './signup.component.html',
  styleUrls: ['./signup.component.scss']
})
export class SignupComponent implements OnInit {
  signupForm!: FormGroup;
  errorMessage: string = '';
  successMessage: string = '';
  submitted = false;
  isLoading = false;
  showPassword = false;

  constructor(
    private formBuilder: FormBuilder,
    private userService: UserService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.signupForm = this.formBuilder.group({
      username: ['', [
        Validators.required, 
        Validators.minLength(3), 
        Validators.maxLength(20),
        Validators.pattern('^[a-zA-Z0-9_-]+$')
      ]],
      email: ['', [
        Validators.required, 
        Validators.email,
        Validators.pattern("[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$")
      ]],
      password: ['', [
        Validators.required, 
        Validators.minLength(6)
      ]],
      fullName: ['', [
        Validators.required,
        Validators.minLength(2),
        Validators.maxLength(50),
        Validators.pattern("^[a-zA-Z]+(([',. -][a-zA-Z ])?[a-zA-Z]*)*$")
      ]],
      role: ['', Validators.required]
    });
  }

  // Convenience getter for easy access to form fields
  get f() { return this.signupForm.controls; }

  onSubmit(): void {
    this.submitted = true;
    this.errorMessage = '';
    this.successMessage = '';

    // Stop here if form is invalid
    if (this.signupForm.invalid) {
      return;
    }

    this.isLoading = true;

    const signupData = this.signupForm.value;

    this.userService.register(signupData).subscribe({
      next: response => {
        this.successMessage = 'Registration successful! Redirecting to login...';
        setTimeout(() => this.goToLogin(), 3000); // Redirect after 3 seconds
      },
      error: err => {
        this.errorMessage = 'Registration failed. Please try again.';
        this.isLoading = false;
      }
    });
  }

  goToLogin() {
    this.router.navigate(['/login']);
  }

  getPasswordStrengthClass(): string {
    const password = this.signupForm.get('password')?.value || '';
    if (!password || password.length < 6) return 'weak';
    
    // Check for strong password (contains letters, numbers, and special characters)
    const hasLetters = /[a-zA-Z]/.test(password);
    const hasNumbers = /\d/.test(password);
    const hasSpecial = /[!@#$%^&*(),.?":{}|<>]/.test(password);
    
    if (password.length >= 8 && hasLetters && hasNumbers && hasSpecial) {
      return 'strong';
    } else if (password.length >= 6 && ((hasLetters && hasNumbers) || (hasLetters && hasSpecial) || (hasNumbers && hasSpecial))) {
      return 'medium';
    }
    return 'weak';
  }

  getPasswordStrengthText(): string {
    const strengthClass = this.getPasswordStrengthClass();
    switch (strengthClass) {
      case 'weak': return 'Weak password';
      case 'medium': return 'Medium strength password';
      case 'strong': return 'Strong password';
      default: return '';
    }
  }
}