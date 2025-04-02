import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { CourseService } from '../../../services/course.service';
import { Course } from '../../../models/course';

interface StudentGrade {
  id: string;
  courseId: string;
  courseCode: string;
  assignmentName: string;
  gradeValue: number;
  enteredAt: string;
  enteredBy: string;
  comment?: string;
}

interface GradeRange {
  min: number;
  max: number;
  count: number;
}

@Component({
  selector: 'app-student-grade-overview',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './student-grade-overview.component.html',
  styleUrl: './student-grade-overview.component.scss'
})
export class StudentGradeOverviewComponent implements OnInit {
  courses: Course[] = [];
  selectedCourseId: string = 'all';
  
  // Grades
  allGrades: StudentGrade[] = [];
  filteredGrades: StudentGrade[] = [];
  selectedFeedback: StudentGrade | null = null;
  
  // Pagination
  currentPage: number = 0;
  pageSize: number = 10;
  totalPages: number = 0;
  
  // Sorting
  sortColumn: string = 'date';
  sortDirection: 'asc' | 'desc' = 'desc';
  
  // Stats
  overallAverage: number = 0;
  highestGrade: number | null = null;
  recentGrade: StudentGrade | null = null;
  hasGrades: boolean = false;
  
  // Grade distribution
  gradeRanges: GradeRange[] = [
    { min: 1, max: 2, count: 0 },
    { min: 3, max: 4, count: 0 },
    { min: 5, max: 6, count: 0 },
    { min: 7, max: 8, count: 0 },
    { min: 9, max: 10, count: 0 }
  ];
  maxRangeCount: number = 0;
  
  constructor(
    private courseService: CourseService,
    private router: Router
  ) {}
  
  ngOnInit(): void {
    this.loadCourses();
    this.loadAllGrades();
  }
  
  loadCourses(): void {
    this.courseService.getStudentCourses().subscribe({
      next: (courses) => {
        this.courses = courses;
      },
      error: (error) => {
        console.error('Error loading courses:', error);
      }
    });
  }
  
  loadAllGrades(): void {
    this.courseService.getAllStudentGrades().subscribe({
      next: (grades) => {
        // Map API response to our interface and add course details
        this.allGrades = grades.map(grade => ({
          ...grade,
          courseCode: this.getCourseCode(grade.courseId)
        }));
        
        this.calculateStats();
        this.updateFilteredGrades();
        this.sortGrades('date', 'desc'); // By default show newest first
      },
      error: (error) => {
        console.error('Error loading grades:', error);
      }
    });
  }
  
  loadGradesForCourse(courseId: string): void {
    this.courseService.getStudentGradesForCourse(courseId).subscribe({
      next: (grades) => {
        this.allGrades = grades.map(grade => ({
          ...grade,
          courseCode: this.getCourseCode(grade.courseId)
        }));
        
        this.calculateStats();
        this.updateFilteredGrades();
      },
      error: (error) => {
        console.error('Error loading grades for course:', error);
      }
    });
  }
  
  getCourseCode(courseId: string): string {
    const course = this.courses.find(c => c.id === courseId);
    return course ? course.courseCode : 'Unknown Course';
  }
  
  onCourseChange(): void {
    if (this.selectedCourseId === 'all') {
      this.loadAllGrades();
    } else {
      this.loadGradesForCourse(this.selectedCourseId);
    }
  }
  
  calculateStats(): void {
    if (this.allGrades.length === 0) {
      this.overallAverage = 0;
      this.highestGrade = null;
      this.recentGrade = null;
      this.hasGrades = false;
      return;
    }
    
    this.hasGrades = true;
    
    // Calculate average
    this.overallAverage = this.allGrades.reduce((sum, grade) => sum + grade.gradeValue, 0) / this.allGrades.length;
    
    // Find highest grade
    this.highestGrade = Math.max(...this.allGrades.map(grade => grade.gradeValue));
    
    // Find most recent grade
    this.recentGrade = [...this.allGrades].sort((a, b) => 
      new Date(b.enteredAt).getTime() - new Date(a.enteredAt).getTime()
    )[0];
    
    // Calculate grade distribution
    this.gradeRanges.forEach(range => {
      range.count = this.allGrades.filter(grade => 
        grade.gradeValue >= range.min && grade.gradeValue <= range.max
      ).length;
    });
    
    this.maxRangeCount = Math.max(...this.gradeRanges.map(range => range.count));
  }
  
  isInRange(min: number, max: number): boolean {
    return this.highestGrade !== null && 
           this.highestGrade >= min && 
           this.highestGrade <= max;
  }
  
  updateFilteredGrades(): void {
    const startIndex = this.currentPage * this.pageSize;
    const endIndex = startIndex + this.pageSize;
    
    this.filteredGrades = this.allGrades.slice(startIndex, endIndex);
    this.totalPages = Math.ceil(this.allGrades.length / this.pageSize);
  }
  
  applyFilter(event: Event): void {
    const filterValue = (event.target as HTMLInputElement).value.toLowerCase();
    
    if (filterValue) {
      this.filteredGrades = this.allGrades.filter(grade => 
        (grade.courseCode && grade.courseCode.toLowerCase().includes(filterValue)) || 
        (grade.assignmentName && grade.assignmentName.toLowerCase().includes(filterValue))
      );
    } else {
      this.updateFilteredGrades();
    }
  }
  
  sort(column: string): void {
    if (this.sortColumn === column) {
      this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortColumn = column;
      this.sortDirection = 'asc';
    }
    
    this.sortGrades(column, this.sortDirection);
  }
  
  sortGrades(column: string, direction: 'asc' | 'desc'): void {
    this.allGrades.sort((a, b) => {
      let comparison = 0;
      
      if (column === 'course') {
        comparison = a.courseCode.localeCompare(b.courseCode);
      } else if (column === 'assignment') {
        comparison = a.assignmentName.localeCompare(b.assignmentName);
      } else if (column === 'grade') {
        comparison = a.gradeValue - b.gradeValue;
      } else if (column === 'date') {
        comparison = new Date(a.enteredAt).getTime() - new Date(b.enteredAt).getTime();
      }
      
      return direction === 'asc' ? comparison : -comparison;
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
  
  viewFeedback(grade: StudentGrade): void {
    this.selectedFeedback = grade;
  }
  
  navigateBack(): void {
    this.router.navigate(['/student/dashboard']);
  }
}
