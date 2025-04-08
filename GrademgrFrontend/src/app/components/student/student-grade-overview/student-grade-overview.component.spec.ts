import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { of } from 'rxjs';
import { By } from '@angular/platform-browser';

import { StudentGradeOverviewComponent } from './student-grade-overview.component';
import { CourseService } from '../../../services/course.service';

describe('StudentGradeOverviewComponent', () => {
  let component: StudentGradeOverviewComponent;
  let fixture: ComponentFixture<StudentGradeOverviewComponent>;
  let courseServiceMock: any;
  let routerMock: any;

  const mockCourses = [
    { id: '1', courseCode: 'CS101', courseName: 'Intro to CS' },
    { id: '2', courseCode: 'MATH200', courseName: 'Calculus' }
  ];

  const mockGrades = [
    { id: 'g1', courseId: '1', assignmentName: 'Midterm', gradeValue: 8.5, 
      enteredAt: '2025-03-15T12:00:00Z', enteredBy: 't1', comment: 'Good work' },
    { id: 'g2', courseId: '1', assignmentName: 'Final', gradeValue: 9.0, 
      enteredAt: '2025-04-01T12:00:00Z', enteredBy: 't1' },
    { id: 'g3', courseId: '2', assignmentName: 'Quiz', gradeValue: 7.5, 
      enteredAt: '2025-03-10T12:00:00Z', enteredBy: 't2', comment: 'Needs improvement' }
  ];

  beforeEach(async () => {
    courseServiceMock = {
      getStudentCourses: jest.fn().mockReturnValue(of(mockCourses)),
      getAllStudentGrades: jest.fn().mockReturnValue(of(mockGrades)),
      getStudentGradesForCourse: jest.fn().mockReturnValue(of([mockGrades[0], mockGrades[1]]))
    };

    routerMock = {
      navigate: jest.fn()
    };

    await TestBed.configureTestingModule({
      imports: [StudentGradeOverviewComponent, FormsModule],
      providers: [
        { provide: CourseService, useValue: courseServiceMock },
        { provide: Router, useValue: routerMock }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(StudentGradeOverviewComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  // Test 1: Component Creation
  it('should create the component', () => {
    expect(component).toBeTruthy();
  });

  // Test 2: Course Loading
  it('should load courses on init', () => {
    expect(courseServiceMock.getStudentCourses).toHaveBeenCalled();
    expect(component.courses).toEqual(mockCourses);
  });

  // Test 3: Grade Loading
  it('should load all grades on init', () => {
    expect(courseServiceMock.getAllStudentGrades).toHaveBeenCalled();
    expect(component.allGrades.length).toBe(3);
  });

  // Test 4: Course Selection
  it('should filter grades when course is selected', () => {
    // Spy on loadGradesForCourse method
    const loadSpy = jest.spyOn(component, 'loadGradesForCourse');
    
    // Change selected course
    component.selectedCourseId = '1';
    component.onCourseChange();
    
    expect(loadSpy).toHaveBeenCalledWith('1');
    expect(courseServiceMock.getStudentGradesForCourse).toHaveBeenCalledWith('1');
  });

  // Test 5: Grade Statistics
  it('should calculate correct grade statistics', () => {
    // Manually trigger stats calculation
    component.allGrades = mockGrades.map(grade => ({
      ...grade,
      courseCode: component.getCourseCode(grade.courseId)
    }));
    
    component.calculateStats();
    
    expect(component.overallAverage).toBeCloseTo(8.33, 1); // (8.5 + 9.0 + 7.5) / 3
    expect(component.highestGrade).toBe(9.0);
    expect(component.hasGrades).toBe(true);
  });

  // Test 7: Sorting
  it('should sort grades correctly', () => {
    // Setup
    component.allGrades = mockGrades.map(grade => ({
      ...grade,
      courseCode: component.getCourseCode(grade.courseId)
    }));
    
    // Sort by grade in ascending order
    component.sort('grade');
    component.updateFilteredGrades();
    
    expect(component.filteredGrades[0].gradeValue).toBe(7.5);
    expect(component.filteredGrades[2].gradeValue).toBe(9.0);
    
    // Sort by grade in descending order
    component.sort('grade');
    component.updateFilteredGrades();
    
    expect(component.filteredGrades[0].gradeValue).toBe(9.0);
    expect(component.filteredGrades[2].gradeValue).toBe(7.5);
  });

  // Test 8: Filtering
  it('should filter grades based on search term', () => {
    // Setup
    component.allGrades = mockGrades.map(grade => ({
      ...grade,
      courseCode: component.getCourseCode(grade.courseId)
    }));
    
    // Create fake event
    const event = { target: { value: 'Midterm' } } as any;
    
    // Apply filter
    component.applyFilter(event);
    
    expect(component.filteredGrades.length).toBe(1);
    expect(component.filteredGrades[0].assignmentName).toBe('Midterm');
  });

  // Test 9: Pagination
  it('should handle pagination correctly', () => {
    // Setup with more grades to trigger pagination
    const manyGrades = Array(25).fill(0).map((_, i) => ({
      id: `g${i}`,
      courseId: '1',
      courseCode: 'CS101',
      assignmentName: `Assignment ${i}`,
      gradeValue: 8.0,
      enteredAt: '2025-03-15T12:00:00Z',
      enteredBy: 't1'
    }));
    
    component.allGrades = manyGrades;
    component.pageSize = 10;
    component.updateFilteredGrades();
    
    // Initial page (page 0)
    expect(component.currentPage).toBe(0);
    expect(component.totalPages).toBe(3);
    expect(component.filteredGrades.length).toBe(10);
    
    // Move to next page
    component.nextPage();
    expect(component.currentPage).toBe(1);
    expect(component.filteredGrades.length).toBe(10);
    
    // Move to previous page
    component.prevPage();
    expect(component.currentPage).toBe(0);
  });

  // Test 10: Feedback Modal
  it('should show feedback modal when viewFeedback is called', () => {
    // Setup
    const gradeWithFeedback = {
      ...mockGrades[0],
      courseCode: 'CS101'
    };
    
    // Initially no feedback selected
    expect(component.selectedFeedback).toBeNull();
    
    // View feedback
    component.viewFeedback(gradeWithFeedback);
    fixture.detectChanges();
    
    // Check that feedback is selected
    expect(component.selectedFeedback).toBe(gradeWithFeedback);
    
    // Check that modal appears in the DOM
    const modal = fixture.debugElement.query(By.css('.modal'));
    expect(modal).toBeTruthy();
    
    // Check modal content
    const feedbackText = modal.query(By.css('.feedback-comment p'));
    expect(feedbackText.nativeElement.textContent).toBe('Good work');
  });
});
