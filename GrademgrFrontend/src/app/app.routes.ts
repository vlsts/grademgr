import { Routes } from '@angular/router';
import { LoginComponent } from './components/auth/login/login.component';
import { SignupComponent } from './components/auth/signup/signup.component';
import { TeacherDashboardComponent } from './components/teacher/teacher-dashboard/teacher-dashboard.component';
import { StudentDashboardComponent } from './components/student/student-dashboard/student-dashboard.component';
import { CourseManagementComponent } from './components/teacher/course-management/course-management.component';
import { StudentManagementComponent } from './components/teacher/student-management/student-management.component';
import { GradeManagementComponent } from './components/teacher/grade-management/grade-management.component';
import { StudentGradeOverviewComponent } from './components/student/student-grade-overview/student-grade-overview.component';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'signup', component: SignupComponent },
  { path: 'student/dashboard', component: StudentDashboardComponent },
  { path: 'student/grades', component: StudentGradeOverviewComponent },
  { path: 'teacher/dashboard', component: TeacherDashboardComponent },
  { path: 'teacher/courses', component: CourseManagementComponent },
  { path: 'teacher/students', component:  StudentManagementComponent},
  { path: 'teacher/grades', component: GradeManagementComponent},
  { path: '', redirectTo: '/login', pathMatch: 'full' },
];
