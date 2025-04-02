import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CourseService } from '../../../services/course.service';
import { Course } from '../../../models/course';
import { MatDialogModule } from '@angular/material/dialog';

// Update the Grade interface to match the API response
interface Grade {
  id: string;
  studentName: string;
  studentEmail: string;
  gradeValue: number; // Changed from 'grade' to 'gradeValue'
  enteredAt: string;  // Changed from 'dateAdded' to 'enteredAt'
  comment?: string;
  assignmentName: string;
  courseId: string;   // Added courseId
  enteredBy: string;  // Added enteredBy
  studentId: string;  // Added studentId
}

@Component({
  selector: 'app-grade-management',
  standalone: true,
  imports: [CommonModule, FormsModule, MatDialogModule],
  templateUrl: './grade-management.component.html',
  styleUrl: './grade-management.component.scss'
})
export class GradeManagementComponent implements OnInit {
  courses: Course[] = [];
  selectedCourseId: string | null = null;
  
  // Grades
  allGrades: Grade[] = [];
  filteredGrades: Grade[] = [];
  
  // Pagination
  currentPage: number = 0;
  pageSize: number = 10;
  totalPages: number = 0;
  
  // Sorting
  sortColumn: string = '';
  sortDirection: 'asc' | 'desc' = 'asc';
  
  // New grade form
  newGrade = {
    studentEmail: '',
    grade: null as number | null,
    assignmentName: '',
    comment: ''
  };
  
  constructor(
    private courseService: CourseService,
    private router: Router,
  ) {}
  
  ngOnInit(): void {
    this.loadCourses();
  }
  
  loadCourses(): void {
    this.courseService.getTeacherCourses().subscribe({
      next: (courses) => {
        this.courses = courses;
      },
      error: (error) => {
        console.error('Error loading courses:', error);
      }
    });
  }
  
  onCourseChange(): void {
    if (this.selectedCourseId) {
      this.loadGradesForCourse(this.selectedCourseId);
    }
  }
  
  loadGradesForCourse(courseId: string): void {
    this.courseService.getGradesForCourse(courseId).subscribe({
      next: (grades) => {
        this.allGrades = grades;
        this.updateFilteredGrades();
      },
      error: (error) => {
        console.error('Error loading grades:', error);
      }
    });
  }
  
  updateFilteredGrades(): void {
    const startIndex = this.currentPage * this.pageSize;
    const endIndex = startIndex + this.pageSize;
    
    this.filteredGrades = this.allGrades.slice(startIndex, endIndex);
    this.totalPages = Math.ceil(this.allGrades.length / this.pageSize);
  }
  
  // Update the filteredGrades and sort methods to use gradeValue
  applyFilter(event: Event): void {
    const filterValue = (event.target as HTMLInputElement).value.toLowerCase();
    
    if (filterValue) {
      this.filteredGrades = this.allGrades.filter(grade => 
        (grade.studentName && grade.studentName.toLowerCase().includes(filterValue)) || 
        grade.studentEmail.toLowerCase().includes(filterValue) ||
        grade.assignmentName.toLowerCase().includes(filterValue)
      );
    } else {
      this.updateFilteredGrades();
    }
  }
  
  // Update this method to handle sorting by grade
  sort(column: string): void {
    if (this.sortColumn === column) {
      this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortColumn = column;
      this.sortDirection = 'asc';
    }
    
    this.allGrades.sort((a, b) => {
      let comparison = 0;
      
      if (column === 'name') {
        comparison = a.studentName.localeCompare(b.studentName);
      } else if (column === 'email') {
        comparison = a.studentEmail.localeCompare(b.studentEmail);
      } else if (column === 'grade') {
        comparison = a.gradeValue - b.gradeValue; // Use gradeValue
      } else if (column === 'assignment') {
        comparison = a.assignmentName.localeCompare(b.assignmentName);
      } else if (column === 'date') {
        comparison = new Date(a.enteredAt).getTime() - new Date(b.enteredAt).getTime(); // Use enteredAt
      }
      
      return this.sortDirection === 'asc' ? comparison : -comparison;
    });
    
    this.updateFilteredGrades();
  }
  
  prevPage(): void {
    if (this.currentPage > 0) {
      this.currentPage--;
      this.updateFilteredGrades();
    }
  }
  
  nextPage(): void {
    if (this.currentPage < this.totalPages - 1) {
      this.currentPage++;
      this.updateFilteredGrades();
    }
  }

  // Update the submitGrade method to use the correct property names
  submitGrade(): void {
    if (!this.selectedCourseId || !this.newGrade.studentEmail || !this.newGrade.grade || !this.newGrade.assignmentName) {
      return;
    }
    
    this.courseService.addGrade(
      this.selectedCourseId,
      this.newGrade.studentEmail,
      this.newGrade.grade, // This will be mapped to gradeValue in the service
      this.newGrade.assignmentName,
      this.newGrade.comment
    ).subscribe({
      next: () => {
        // Refresh grades
        this.loadGradesForCourse(this.selectedCourseId!);
        
        // Reset the form
        this.newGrade = {
          studentEmail: '',
          grade: null,
          assignmentName: '',
          comment: ''
        };
      },
      error: (err) => {
        console.error('Error adding grade:', err);
      }
    });
  }
  
  deleteGrade(gradeId: string): void {
    // Confirm deletion - optional, can be removed if you don't want a confirmation
    if (confirm('Are you sure you want to delete this grade?')) {
      this.courseService.deleteGrade(this.selectedCourseId!, gradeId).subscribe({
        next: () => {
          // Reload grades after successful deletion
          this.loadGradesForCourse(this.selectedCourseId!);
          
          // Show success message (optional)
          console.log('Grade deleted successfully');
        },
        error: (error) => {
          console.error('Error deleting grade:', error);
          // Show error message to user (optional)
        }
      });
    }
  }
  
  navigateBack(): void {
    this.router.navigate(['/teacher/dashboard']);
  }
}
