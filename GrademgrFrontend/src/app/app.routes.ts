import { Routes } from '@angular/router';
import { LoginComponent } from './components/auth/login/login.component';
import { SignupComponent } from './components/auth/signup/signup.component';
import { TeacherDashboardComponent } from './components/teacher/teacher-dashboard/teacher-dashboard.component';
import { StudentDashboardComponent } from './components/student/student-dashboard/student-dashboard.component';
import { CourseManagementComponent } from './components/teacher/course-management/course-management.component';
import { StudentManagementComponent } from './components/teacher/student-management/student-management.component';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'signup', component: SignupComponent },
  { path: 'teacher/dashboard', component: TeacherDashboardComponent },
  { path: 'student/dashboard', component: StudentDashboardComponent },
  { path: 'teacher/courses', component: CourseManagementComponent },
  { path: 'teacher/students', component:  StudentManagementComponent},
  { path: '', redirectTo: '/login', pathMatch: 'full' },
];
