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

  addGrade(courseId: string, studentMail: string, gradeValue: number, assignmentName: string, comment?: string): Observable<any> {
    const request = { 
      studentMail,
      gradeValue,
      assignmentName,
      comment
    };
    return this.http.post<any>(`${this.apiUrl}/${courseId}/grades`, request, {
      headers: this.getAuthHeaders()
    });
  }

  getGradesForCourse(courseId: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/${courseId}/grades`, {
      headers: this.getAuthHeaders()
    });
  }

  deleteGrade(courseId: string, gradeId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${courseId}/grades/${gradeId}`, {
      headers: this.getAuthHeaders()
    });
  }

  getStudentGradesForCourse(courseId: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/${courseId}/grades/student`, {
      headers: this.getAuthHeaders()
    });
  }

  getAllStudentGrades(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/grades`, {
      headers: this.getAuthHeaders()
    });
  }

  getStudentCourses(): Observable<Course[]> {
    return this.http.get<Course[]>(`${this.apiUrl}/student/courses`, {
      headers: this.getAuthHeaders()
    });
  }
}

