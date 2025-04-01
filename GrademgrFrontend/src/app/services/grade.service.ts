import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Grade } from '../models/grade';
import { GradeHistory } from '../models/grade-history';

@Injectable({
  providedIn: 'root'
})
export class GradeService {
  private apiUrl = 'http://api-url/api/grade'; // To replace with the actual API URL

  constructor(private http: HttpClient) {}

  getGrades(): Observable<Grade[]> {
    return this.http.get<Grade[]>(`${this.apiUrl}`);
  }

  getGradeById(id: string): Observable<Grade> {
    return this.http.get<Grade>(`${this.apiUrl}/${id}`);
  }

  createGrade(grade: Grade): Observable<Grade> {
    return this.http.post<Grade>(this.apiUrl, grade);
  }

  updateGrade(id: string, grade: Grade): Observable<Grade> {
    return this.http.put<Grade>(`${this.apiUrl}/${id}`, grade);
  }

  deleteGrade(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  getGradeHistory(gradeId: string): Observable<GradeHistory[]> {
    return this.http.get<GradeHistory[]>(`${this.apiUrl}/${gradeId}/history`);
  }
}
