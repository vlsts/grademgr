import { Component, OnInit } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { CommonModule } from '@angular/common'; 
import { Router, RouterLink } from '@angular/router'; // Import Router
import { UserService } from '../../../services/user.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule, HttpClientModule, CommonModule],
  providers: [UserService],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss']
})
export class LoginComponent implements OnInit {
  loginForm!: FormGroup;
  errorMessage: string = '';
  submitted = false;

  constructor(
    private formBuilder: FormBuilder,
    private userService: UserService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loginForm = this.formBuilder.group({
      email: ['', [Validators.required]],
      password: ['', [Validators.required, Validators.minLength(6)]]
    });
  }

  get f() { return this.loginForm.controls; }

  onSubmit(): void {
    this.submitted = true;
    
    if (this.loginForm.invalid) {
      return;
    }

    const email = this.f['email'].value;
    const password = this.f['password'].value;

    this.userService.login(email, password).subscribe({
      next: response => {
        console.log('Login successful', response);
        localStorage.setItem('token', response.token);
        console.log('Token:', response.token);
        console.log('Role:', response.role);
        if (response.role === 'Teacher') {
          this.router.navigate(['/teacher/dashboard']);
        }
        else {
          this.router.navigate(['/student/dashboard']);
        }
      },
      error: err => {
        this.errorMessage = 'Login failed. Please check your credentials.';
      }
    });
  }

  goToSignup(): void {
    this.router.navigate(['/signup']);
  }
}