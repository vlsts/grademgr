import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { ReactiveFormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { catchError, finalize } from 'rxjs/operators';
import { Observable, throwError } from 'rxjs';
import { CourseService } from '../../../services/course.service';
import { Course } from '../../../models/course';
import { HttpErrorResponse } from '@angular/common/http';
import { StudentInfo } from '../../../models/student-info';

@Component({
  selector: 'app-student-management',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, HttpClientModule],
  providers: [CourseService],
  templateUrl: './student-management.component.html',
  styleUrls: ['./student-management.component.scss']
})
export class StudentManagementComponent implements OnInit {
  courses: Course[] = [];
  selectedCourse: Course | null = null;
  students: StudentInfo[] = [];
  isLoading = true;
  errorMessage = '';
  successMessage = '';
  isRemovingStudent = false;

  constructor(
    private router: Router,
    private courseService: CourseService
  ) {}

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

  // Load all courses for the teacher
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

  // Select a course to view students
  selectCourse(course: Course): void {
    this.selectedCourse = course;
    this.loadStudentsForCourse(course.id);
  }

  // Load students for a specific course
  loadStudentsForCourse(courseId: string): void {
    this.isLoading = true;
    this.errorMessage = '';
    
    this.courseService.getCourseStudents(courseId)
      .pipe(
        catchError(error => this.handleError(error)),
        finalize(() => this.isLoading = false)
      )
      .subscribe(students => {
        this.students = students;
      });
  }

  // Remove a student from the course
  removeStudent(student: StudentInfo): void {
    if (!this.selectedCourse || this.isRemovingStudent) {
      return;
    }

    if (confirm(`Are you sure you want to remove ${student.fullName} from this course?`)) {
      this.isRemovingStudent = true;
      this.errorMessage = '';
      this.successMessage = '';
      
      this.courseService.removeStudent(this.selectedCourse.id, student.email)
        .pipe(
          catchError(error => this.handleError(error)),
          finalize(() => this.isRemovingStudent = false)
        )
        .subscribe(() => {
          // Remove the student from the local array
          this.students = this.students.filter(s => s.email !== student.email);
          this.successMessage = `Student ${student.fullName} has been removed from the course.`;
          
          // Optionally, reload the course list to update the student counts
          this.loadCourses();
        });
    }
  }

  // Go back to course list
  backToCourseList(): void {
    this.selectedCourse = null;
    this.students = [];
  }

  // Go back to dashboard
  backToDashboard(): void {
    this.router.navigate(['/teacher/dashboard']);
  }
}