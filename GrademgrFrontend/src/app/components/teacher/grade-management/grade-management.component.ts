import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { CourseService } from '../../../services/course.service';
import { HttpClientModule } from '@angular/common/http';

interface Grade {
  id: string;
  studentName: string;
  studentEmail: string;
  assignmentName: string;
  gradeValue: number;
  comment?: string;
  enteredAt: Date;
}

interface NewGrade {
  studentEmail: string;
  assignmentName: string;
  grade: number;
  comment?: string;
}

interface PendingGrade {
  studentEmail: string;
  assignmentName: string;
  grade: number;
  comment?: string;
  isEditing?: boolean;
  isHighlighted?: boolean;
}

interface Course {
  id: string;
  courseName: string;
  courseCode: string;
  description?: string;
}

@Component({
  selector: 'app-grade-management',
  standalone: true,
  imports: [CommonModule, FormsModule, HttpClientModule],
  providers: [CourseService],
  templateUrl: './grade-management.component.html',
  styleUrls: ['./grade-management.component.scss']
})
export class GradeManagementComponent implements OnInit {
  courses: Course[] = [];
  selectedCourseId: string | null = null;
  grades: Grade[] = [];
  filteredGrades: Grade[] = [];
  pendingGrades: PendingGrade[] = [];
  
  // Pagination
  currentPage = 0;
  pageSize = 5;
  totalPages = 0;
  
  // Sorting
  sortField = 'date';
  sortDirection = 'desc';
  
  // Form data
  newGrade: NewGrade = {
    studentEmail: '',
    assignmentName: '',
    grade: 0,
    comment: ''
  };
  
  // Loading and error states
  isLoading = false;
  error = '';
  success = '';
  
  // Store original values during edit
  private editingGradeBackup: PendingGrade | null = null;

  constructor(
    private router: Router,
    private courseService: CourseService
  ) { }

  ngOnInit(): void {
    this.loadCourses();
  }

  loadCourses(): void {
    this.isLoading = true;
    this.courseService.getTeacherCourses()
      .subscribe({
        next: (data) => {
          this.courses = data;
          this.isLoading = false;
        },
        error: (err) => {
          this.error = 'Failed to load courses';
          console.error(err);
          this.isLoading = false;
        }
      });
  }

  onCourseChange(): void {
    if (this.selectedCourseId) {
      this.loadGrades(this.selectedCourseId);
    }
  }

  loadGrades(courseId: string): void {
    this.isLoading = true;
    this.courseService.getGradesForCourse(courseId)
      .subscribe({
        next: (data) => {
          this.grades = data;
          this.totalPages = Math.ceil(this.grades.length / this.pageSize);
          this.filterAndPaginateGrades();
          this.isLoading = false;
        },
        error: (err) => {
          this.error = 'Failed to load grades';
          console.error(err);
          this.isLoading = false;
        }
      });
  }

  submitGrade(): void {
    if (!this.selectedCourseId || !this.newGrade.studentEmail || !this.newGrade.grade || !this.newGrade.assignmentName) {
      return;
    }

    this.isLoading = true;
    this.error = '';
    this.success = '';

    this.courseService.addGrade(
      this.selectedCourseId,
      this.newGrade.studentEmail,
      this.newGrade.grade,
      this.newGrade.assignmentName,
      this.newGrade.comment
    ).subscribe({
      next: () => {
        this.success = 'Grade added successfully';
        this.resetForm();
        this.loadGrades(this.selectedCourseId!);
      },
      error: (err) => {
        this.error = 'Failed to add grade: ' + (err.error?.message || err.message || 'Unknown error');
        this.isLoading = false;
      }
    });
  }

  // Add to batch
  addToBatch(): void {
    if (!this.selectedCourseId || !this.newGrade.studentEmail || !this.newGrade.grade || !this.newGrade.assignmentName) {
      return;
    }
    
    // Add to pending grades
    this.pendingGrades.push({
      studentEmail: this.newGrade.studentEmail,
      assignmentName: this.newGrade.assignmentName,
      grade: this.newGrade.grade,
      comment: this.newGrade.comment,
      isHighlighted: true
    });
    
    // Reset form
    this.resetForm();
    
    // Remove highlight after 2 seconds
    const index = this.pendingGrades.length - 1;
    setTimeout(() => {
      if (this.pendingGrades[index]) {
        this.pendingGrades[index].isHighlighted = false;
      }
    }, 2000);
    
    // Show success
    this.success = 'Grade added to batch';
    setTimeout(() => {
      if (this.success === 'Grade added to batch') {
        this.success = '';
      }
    }, 3000);
  }
  
  // Start editing a pending grade
  editPendingGrade(index: number): void {
    // First, cancel any other editing that might be in progress
    this.pendingGrades.forEach((g, i) => {
      if (i !== index && g.isEditing) {
        this.cancelPendingGradeEdit(i);
      }
    });
    
    // Make a backup of the original values
    this.editingGradeBackup = { ...this.pendingGrades[index] };
    
    // Set the grade to editing mode
    this.pendingGrades[index].isEditing = true;
  }

  // Save edits to a pending grade
  savePendingGradeEdit(index: number): void {
    // Validate the data
    const grade = this.pendingGrades[index];
    
    if (!grade.studentEmail || !grade.assignmentName || !grade.grade || grade.grade < 0) {
      this.error = 'Please fill in all required fields with valid values';
      return;
    }
    
    // Exit editing mode
    grade.isEditing = false;
    
    // Highlight the row briefly to show it was updated
    grade.isHighlighted = true;
    setTimeout(() => {
      if (this.pendingGrades[index]) {
        this.pendingGrades[index].isHighlighted = false;
      }
    }, 2000);
    
    // Clear backup
    this.editingGradeBackup = null;
    
    // Show success message
    this.success = 'Grade updated in batch';
    setTimeout(() => {
      if (this.success === 'Grade updated in batch') {
        this.success = '';
      }
    }, 3000);
  }

  // Cancel editing of a pending grade
  cancelPendingGradeEdit(index: number): void {
    if (this.editingGradeBackup) {
      // Restore the original values
      this.pendingGrades[index] = { ...this.editingGradeBackup };
    }
    
    // Exit editing mode
    this.pendingGrades[index].isEditing = false;
    
    // Clear backup
    this.editingGradeBackup = null;
  }

  // Remove a grade from the pending batch
  removePendingGrade(index: number): void {
    this.pendingGrades.splice(index, 1);
  }

  // Clear all pending grades
  clearPendingGrades(): void {
    if (confirm('Are you sure you want to clear all pending grades?')) {
      this.pendingGrades = [];
      this.success = 'All pending grades cleared';
      setTimeout(() => {
        if (this.success === 'All pending grades cleared') {
          this.success = '';
        }
      }, 3000);
    }
  }

  // Submit all pending grades
  submitBatch(): void {
    if (!this.selectedCourseId || this.pendingGrades.length === 0) {
      return;
    }
    
    if (confirm(`Are you sure you want to submit ${this.pendingGrades.length} grades?`)) {
      this.isLoading = true;
      this.error = '';
      this.success = '';
      
      // Prepare grades in the format expected by the API
      const gradesToSubmit = this.pendingGrades.map(grade => ({
        studentMail: grade.studentEmail,
        gradeValue: grade.grade,
        assignmentName: grade.assignmentName,
        comment: grade.comment
      }));
      
      this.courseService.addMultipleGrades(this.selectedCourseId, gradesToSubmit)
        .subscribe({
          next: (response) => {
            if (response && response.success !== undefined) {
              if (response.failed === 0) {
                this.success = `Successfully added all ${response.success} grades`;
              } else {
                this.success = `Added ${response.success} grades successfully, but ${response.failed} failed`;
              }
            } else {
              this.success = `Grades submitted successfully`;
            }
            
            this.pendingGrades = [];
            this.loadGrades(this.selectedCourseId!);
            this.isLoading = false;
          },
          error: (err) => {
            this.error = 'Failed to submit grades: ' + (err.error?.message || err.message || 'Unknown error');
            this.isLoading = false;
          }
        });
    }
  }

  // Navigation
  navigateBack(): void {
    if (this.pendingGrades.length > 0) {
      if (!confirm('You have unsaved grades. Are you sure you want to go back?')) {
        return;
      }
    }
    this.router.navigate(['/teacher/dashboard']);
  }

  // Delete an existing grade
  deleteGrade(id: string): void {
    if (confirm('Are you sure you want to delete this grade?')) {
      this.isLoading = true;
      this.error = '';
      this.success = '';
      
      this.courseService.deleteGrade(this.selectedCourseId!, id)
        .subscribe({
          next: () => {
            this.success = 'Grade deleted successfully';
            this.loadGrades(this.selectedCourseId!);
          },
          error: (err) => {
            this.error = 'Failed to delete grade: ' + (err.error?.message || err.message || 'Unknown error');
            this.isLoading = false;
          }
        });
    }
  }

  // Reset the grade form
  resetForm(): void {
    this.newGrade = {
      studentEmail: '',
      assignmentName: '',
      grade: 0,
      comment: ''
    };
  }

  // Pagination methods
  nextPage(): void {
    if (this.currentPage < this.totalPages - 1) {
      this.currentPage++;
      this.filterAndPaginateGrades();
    }
  }

  prevPage(): void {
    if (this.currentPage > 0) {
      this.currentPage--;
      this.filterAndPaginateGrades();
    }
  }

  // Apply filter and pagination
  filterAndPaginateGrades(): void {
    // Sort first
    const sorted = [...this.grades].sort((a, b) => {
      if (this.sortField === 'name') {
        return this.sortDirection === 'asc' 
          ? a.studentName.localeCompare(b.studentName) 
          : b.studentName.localeCompare(a.studentName);
      }
      
      if (this.sortField === 'email') {
        return this.sortDirection === 'asc' 
          ? a.studentEmail.localeCompare(b.studentEmail) 
          : b.studentEmail.localeCompare(a.studentEmail);
      }
      
      if (this.sortField === 'assignment') {
        return this.sortDirection === 'asc' 
          ? a.assignmentName.localeCompare(b.assignmentName) 
          : b.assignmentName.localeCompare(a.assignmentName);
      }
      
      if (this.sortField === 'grade') {
        return this.sortDirection === 'asc' 
          ? a.gradeValue - b.gradeValue 
          : b.gradeValue - a.gradeValue;
      }
      
      // Default: sort by date
      return this.sortDirection === 'asc' 
        ? new Date(a.enteredAt).getTime() - new Date(b.enteredAt).getTime() 
        : new Date(b.enteredAt).getTime() - new Date(a.enteredAt).getTime();
    });
    
    // Then paginate
    const start = this.currentPage * this.pageSize;
    this.filteredGrades = sorted.slice(start, start + this.pageSize);
  }

  // Sorting
  sort(field: string): void {
    if (this.sortField === field) {
      this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortField = field;
      this.sortDirection = 'asc';
    }
    
    this.filterAndPaginateGrades();
  }

  // Apply search filter
  applyFilter(event: Event): void {
    const filterValue = (event.target as HTMLInputElement).value.toLowerCase().trim();
    
    if (filterValue === '') {
      this.filterAndPaginateGrades();
      return;
    }
    
    const filtered = this.grades.filter(grade => {
      return grade.studentName.toLowerCase().includes(filterValue) ||
             grade.studentEmail.toLowerCase().includes(filterValue) ||
             grade.assignmentName.toLowerCase().includes(filterValue);
    });
    
    this.filteredGrades = filtered.slice(0, this.pageSize);
    this.totalPages = Math.ceil(filtered.length / this.pageSize);
    this.currentPage = 0;
  }
}