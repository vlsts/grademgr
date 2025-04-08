import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { By } from '@angular/platform-browser';
import { of, throwError } from 'rxjs';

import { StudentManagementComponent } from './student-management.component';
import { CourseService } from '../../../services/course.service';
import { CommonModule } from '@angular/common';

describe('StudentManagementComponent', () => {
  let component: StudentManagementComponent;
  let fixture: ComponentFixture<StudentManagementComponent>;
  let courseServiceMock: any;
  let routerMock: any;

  // Mock data
  const mockCourses = [
    { id: '1', courseName: 'Introduction to Programming', courseCode: 'CS101', description: 'Learn basics', teacherId: 'teacher1' },
    { id: '2', courseName: 'Data Structures', courseCode: 'CS201', description: 'Advanced concepts', teacherId: 'teacher1' }
  ];

  const mockStudents = [
    { username: 'student1', fullName: 'John Doe', email: 'john@example.com' },
    { username: 'student2', fullName: 'Jane Smith', email: 'jane@example.com' },
    { username: 'student3', fullName: 'Bob Johnson', email: 'bob@example.com' }
  ];

  beforeEach(async () => {
    // Create mock services
    courseServiceMock = {
      getTeacherCourses: jest.fn().mockReturnValue(of(mockCourses)),
      getCourseStudents: jest.fn().mockReturnValue(of(mockStudents)),
      removeStudent: jest.fn().mockReturnValue(of({}))
    };

    routerMock = {
      navigate: jest.fn()
    };

    // Configure testing module
    await TestBed.configureTestingModule({
      imports: [
        CommonModule,
        ReactiveFormsModule,
        HttpClientTestingModule,
        RouterTestingModule,
        StudentManagementComponent
      ],
      providers: [
        { provide: CourseService, useValue: courseServiceMock },
        { provide: Router, useValue: routerMock }
      ]
    }).compileComponents();

    // Create component
    fixture = TestBed.createComponent(StudentManagementComponent);
    component = fixture.componentInstance;

    // Set up localStorage mock
    let localStorageMock = (() => {
      let store: Record<string, string> = {};
      return {
        getItem: (key: string) => store[key] || null,
        setItem: (key: string, value: string) => { store[key] = value; },
        removeItem: (key: string) => { delete store[key]; },
        clear: () => { store = {}; }
      };
    })();
    
    Object.defineProperty(window, 'localStorage', { value: localStorageMock });
    localStorage.setItem('token', 'test-token');

    fixture.detectChanges();
  });

  // Test 2: Redirect When No Token
  it('should redirect to login when there is no token', () => {
    localStorage.removeItem('token');
    component.ngOnInit();
    expect(routerMock.navigate).toHaveBeenCalledWith(['/login']);
  });

  // Test 5: Back to Course List
  it('should go back to course list when back button is clicked', () => {
    // First select a course
    component.selectCourse(mockCourses[0]);
    fixture.detectChanges();
    
    // Then click back button
    component.backToCourseList();
    
    // Verify state is reset
    expect(component.selectedCourse).toBeNull();
    expect(component.students).toEqual([]);
  });

  // Test 6: Back to Dashboard
  it('should navigate to dashboard when back to dashboard is clicked', () => {
    const backButton = fixture.debugElement.query(By.css('.back-button'));
    backButton.triggerEventHandler('click', null);
    
    expect(routerMock.navigate).toHaveBeenCalledWith(['/teacher/dashboard']);
  });

  // Test 7: Student Removal Confirmation
  it('should prompt confirmation when removing a student', () => {
    // Mock window.confirm
    global.confirm = jest.fn().mockReturnValue(false);
    
    // First select a course
    component.selectCourse(mockCourses[0]);
    fixture.detectChanges();
    
    // Attempt to remove a student
    component.removeStudent(mockStudents[0]);
    
    // Verify confirmation dialog was shown
    expect(global.confirm).toHaveBeenCalledWith(
      `Are you sure you want to remove ${mockStudents[0].fullName} from this course?`
    );
    
    // Since we mocked confirm to return false, the service should not be called
    expect(courseServiceMock.removeStudent).not.toHaveBeenCalled();
  });
});
