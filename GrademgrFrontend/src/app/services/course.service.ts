import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { map, Observable } from 'rxjs';
import { Course } from '../models/course';
import { CourseDetailResponse } from '../models/course-details';
import { StudentInfo } from '../models/student-info';

@Injectable({
  providedIn: 'root'
})
export class CourseService {
  private apiUrl = 'http://localhost:5052/api/Course';

  constructor(private http: HttpClient) {}

  private getAuthHeaders(): HttpHeaders {
    return new HttpHeaders({
      'Content-Type': 'application/json',
      'Authorization': 'Bearer ' + localStorage.getItem('token')
    });
  }

  getTeacherCourses(): Observable<Course[]> {
    return this.http.get<Course[]>(`${this.apiUrl}/teacher`, {
      headers: this.getAuthHeaders()
    });
  }

  getCourseStudents(courseId: string): Observable<StudentInfo[]> {
    return this.http.get<CourseDetailResponse>(`${this.apiUrl}/${courseId}`, {
      headers: this.getAuthHeaders()
    }).pipe(
      map((response: CourseDetailResponse) => response.students)
    );
  }
  
  createCourse(course: Course): Observable<Course> {
    return this.http.post<Course>(`${this.apiUrl}/create`, course, {
      headers: this.getAuthHeaders()
    });
  }

  deleteCourse(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`, {
      headers: this.getAuthHeaders()
    });
  }

  enrollStudent(courseId: string, studentEmail: string): Observable<Course> {
    return this.http.post<Course>(`${this.apiUrl}/${courseId}/students`, {studentEmail}, { 
      headers: this.getAuthHeaders()
    });
  }

  removeStudent(courseId: string, studentEmail: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${courseId}/students/${studentEmail}`, {
      headers: this.getAuthHeaders()
    });
  }
}

