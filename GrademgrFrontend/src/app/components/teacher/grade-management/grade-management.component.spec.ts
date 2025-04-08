import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { RouterTestingModule } from '@angular/router/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { Router } from '@angular/router';
import { By } from '@angular/platform-browser';
import { of, throwError } from 'rxjs';

import { GradeManagementComponent } from './grade-management.component';
import { CourseService } from '../../../services/course.service';
import { CommonModule } from '@angular/common';
import { ElementRef } from '@angular/core';

describe('GradeManagementComponent', () => {
  let component: GradeManagementComponent;
  let fixture: ComponentFixture<GradeManagementComponent>;
  let courseServiceMock: any;
  let routerMock: any;

  const mockCourses = [
    { id: '1', courseName: 'Introduction to Programming', courseCode: 'CS101' },
    { id: '2', courseName: 'Data Structures', courseCode: 'CS201' }
  ];

  const mockGrades = [
    { 
      id: 'g1', 
      studentName: 'John Smith', 
      studentEmail: 'john@example.com', 
      assignmentName: 'Midterm', 
      gradeValue: 8.5, 
      comment: 'Good work', 
      enteredAt: new Date()
    },
    { 
      id: 'g2', 
      studentName: 'Jane Doe', 
      studentEmail: 'jane@example.com', 
      assignmentName: 'Final', 
      gradeValue: 9.0, 
      comment: null, 
      enteredAt: new Date()
    },
    { 
      id: 'g3', 
      studentName: 'Bob Johnson', 
      studentEmail: 'bob@example.com', 
      assignmentName: 'Quiz', 
      gradeValue: 7.5, 
      comment: 'Needs improvement', 
      enteredAt: new Date()
    }
  ];

  beforeEach(async () => {
    courseServiceMock = {
      getTeacherCourses: jest.fn().mockReturnValue(of(mockCourses)),
      getGradesForCourse: jest.fn().mockReturnValue(of(mockGrades)),
      addGrade: jest.fn().mockReturnValue(of({ message: 'Grade added successfully' })),
      addMultipleGrades: jest.fn().mockReturnValue(of({ message: 'Grades added successfully' })),
      deleteGrade: jest.fn().mockReturnValue(of({ message: 'Grade deleted successfully' })),
      updateGrade: jest.fn().mockReturnValue(of({ message: 'Grade updated successfully' }))
    };

    routerMock = {
      navigate: jest.fn()
    };

    await TestBed.configureTestingModule({
      imports: [
        CommonModule,
        FormsModule,
        HttpClientTestingModule,
        RouterTestingModule,
        GradeManagementComponent
      ],
      providers: [
        { provide: CourseService, useValue: courseServiceMock },
        { provide: Router, useValue: routerMock }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(GradeManagementComponent);
    component = fixture.componentInstance;

    // Mock ElementRef for file input
    const mockElementRef = new ElementRef(document.createElement('input'));
    component['fileInput'] = mockElementRef;

    fixture.detectChanges();
  });

  // Test 1: Component Creation
  it('should create the component', () => {
    expect(component).toBeTruthy();
  });

  // Test 5: Adding to Batch
  it('should add a grade to pending batch', () => {
    // Arrange
    component.selectedCourseId = '1';
    component.newGrade = {
      studentEmail: 'batch@example.com',
      assignmentName: 'Quiz 2',
      grade: 9.5,
      comment: 'Excellent'
    };
    
    // Act
    component.addToBatch();
    
    // Assert
    expect(component.pendingGrades.length).toBe(1);
    expect(component.pendingGrades[0].studentEmail).toBe('batch@example.com');
    expect(component.pendingGrades[0].grade).toBe(9.5);
    expect(component.newGrade.studentEmail).toBe(''); // Form should be reset
    expect(component.success).toBe('Grade added to batch');
  });
});
