import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { HttpClient, HttpHeaders, HttpErrorResponse, HttpClientModule } from '@angular/common/http';
import { catchError, finalize } from 'rxjs/operators';
import { Observable, throwError } from 'rxjs';
import { Course } from '../../../models/course';
import { CourseService } from '../../../services/course.service';

@Component({
  selector: 'app-course-management',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, HttpClientModule],
  providers: [CourseService],
  templateUrl: './course-management.component.html',
  styleUrls: ['./course-management.component.scss']
})
export class CourseManagementComponent implements OnInit {
  courses: Course[] = [];
  courseForm: FormGroup;
  studentEmailForm: FormGroup;
  isEditMode = false;
  currentCourseId: string | null = null;
  selectedCourse: Course | null = null;
  isLoading = true;
  showForm = false;
  showStudentForm = false;
  errorMessage = '';
  successMessage = '';
  private apiUrl = 'http://localhost:5052/api/courses';

  constructor(
    private router: Router,
    private fb: FormBuilder,
    private http: HttpClient,
    private courseService: CourseService
  ) {
    this.courseForm = this.fb.group({
      courseName: ['', [Validators.required]],
      courseCode: ['', [Validators.required, Validators.pattern('^[A-Z]{2,4}[0-9]{3,4}$')]],
      description: ['', [Validators.required]]
    });

    this.studentEmailForm = this.fb.group({
      studentEmail: ['', [Validators.required, Validators.email]]
    });
  }

  ngOnInit(): void {
    const token = localStorage.getItem('token');
    if (!token) {
      this.router.navigate(['/login']);
      return;
    }

    this.loadCourses();
  }

  // Error handling
  private handleError(error: HttpErrorResponse): Observable<never> {
    let errorMsg = 'An unknown error occurred!';
    if (error.error instanceof ErrorEvent) {
      // Client-side error
      errorMsg = `Error: ${error.error.message}`;
    } else {
      // Server-side error
      errorMsg = `Error Code: ${error.status}\nMessage: ${error.message}`;
      
      // If unauthorized, redirect to login
      if (error.status === 401 || error.status === 403) {
        localStorage.removeItem('token');
        this.router.navigate(['/login']);
      }
    }
    
    this.errorMessage = errorMsg;
    console.error(errorMsg);
    return throwError(() => new Error(errorMsg));
  }

  // Load courses
  loadCourses(): void {
    this.isLoading = true;
    this.errorMessage = '';
    
    this.courseService.getTeacherCourses()
      .pipe(
        catchError(error => this.handleError(error)),
        finalize(() => this.isLoading = false)
      )
      .subscribe(courses => {
        this.courses = courses;
      });
  }

  // Toggle course form
  toggleForm(): void {
    this.showForm = !this.showForm;
    if (!this.showForm) {
      this.resetForm();
    }
  }

  // Toggle student form
  toggleStudentForm(course: Course | null = null): void {
    if (course) {
      this.selectedCourse = course;
    }
    this.showStudentForm = !this.showStudentForm;
    if (this.showStudentForm) {
      this.showForm = false;
    } else {
      this.resetStudentForm();
    }
  }

  // Reset form
  resetForm(): void {
    this.courseForm.reset({
      courseName: '',
      courseCode: '',
      description: ''
    });
    this.isEditMode = false;
    this.currentCourseId = null;
    this.errorMessage = '';
  }

  // Reset student email form
  resetStudentForm(): void {
    this.studentEmailForm.reset({
      studentEmail: ''
    });
    this.selectedCourse = null;
    this.errorMessage = '';
    this.successMessage = '';
  }

  // Edit course
  editCourse(course: Course): void {
    this.courseForm.patchValue({
      courseName: course.courseName,
      courseCode: course.courseCode,
      description: course.description
    });
    this.isEditMode = true;
    this.currentCourseId = course.id;
    this.showForm = true;
  }

  // Delete course
  deleteCourse(courseId: string): void {
    if (confirm('Are you sure you want to delete this course?')) {
      this.errorMessage = '';
      
      this.courseService.deleteCourse(courseId)
        .pipe(catchError(error => this.handleError(error)))
        .subscribe(() => {
          this.courses = this.courses.filter(course => course.id !== courseId);
        });
    }
  }

  // Add student to course by email
  addStudentToCourse(): void {
    if (this.studentEmailForm.invalid || !this.selectedCourse) {
      return;
    }

    const email = this.studentEmailForm.value.studentEmail;
    this.errorMessage = '';
    this.successMessage = '';
    
    this.courseService.enrollStudent(this.selectedCourse.id, email)
      .pipe(catchError(error => this.handleError(error)))
      .subscribe(updatedCourse => {
        this.courses = this.courses.map(c => 
          c.id === updatedCourse.id ? updatedCourse : c
        );
        this.successMessage = `Student with email ${email} added to course successfully`;
        this.resetStudentForm();
      });
  }

  // Save course (create or update)
  saveCourse(): void {
    if (this.courseForm.invalid) {
      return;
    }

    const formValue = this.courseForm.value;
    this.errorMessage = '';
    
    // Create new course
    const newCourse = {
      courseName: formValue.courseName,
      courseCode: formValue.courseCode,
      description: formValue.description
    } as Course;
      
    this.courseService.createCourse(newCourse)
      .pipe(catchError(error => this.handleError(error)))
      .subscribe(createdCourse => {
        this.courses = [...this.courses, createdCourse];
        this.resetForm();
        this.showForm = false;
      });
  }

  viewStudents(course: Course): void {
    this.router.navigate(['/teacher/courses', course.id, 'students']);
  }

  // Go back to dashboard
  backToDashboard(): void {
    this.router.navigate(['/teacher/dashboard']);
  }

  // Logout
  logout(): void {
    localStorage.removeItem('token');
    this.router.navigate(['/login']);
  }
}