import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { By } from '@angular/platform-browser';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { CourseManagementComponent } from './course-management.component';
import { CourseService } from '../../../services/course.service';
import { HttpClientTestingModule } from '@angular/common/http/testing';

describe('CourseManagementComponent', () => {
  let component: CourseManagementComponent;
  let fixture: ComponentFixture<CourseManagementComponent>;
  let courseServiceMock: any;
  let routerMock: any;
  
  const mockCourses = [
    { 
      id: '1', 
      courseName: 'Introduction to Programming', 
      courseCode: 'CS101', 
      description: 'Learn programming basics',
      createdAt: new Date().toISOString()
    },
    { 
      id: '2', 
      courseName: 'Data Structures', 
      courseCode: 'CS201', 
      description: 'Advanced data structures',
      createdAt: new Date().toISOString()
    }
  ];

  beforeEach(async () => {
    // Create mock services
    courseServiceMock = {
      getTeacherCourses: jest.fn().mockReturnValue(of(mockCourses)),
      createCourse: jest.fn().mockImplementation(course => of({...course, id: '3'})),
      deleteCourse: jest.fn().mockReturnValue(of({})),
      enrollStudent: jest.fn().mockImplementation((courseId, email) => 
        of(mockCourses.find(c => c.id === courseId)))
    };

    routerMock = {
      navigate: jest.fn()
    };

    // Setup TestBed
    await TestBed.configureTestingModule({
      imports: [
        CourseManagementComponent,
        ReactiveFormsModule,
        HttpClientTestingModule
      ],
      providers: [
        { provide: CourseService, useValue: courseServiceMock },
        { provide: Router, useValue: routerMock }
      ]
    }).compileComponents();

    // Create localStorage mock
    let localStorageMock = (() => {
      let storage: Record<string, string> = {};
      return {
        getItem: (key: string) => storage[key] || null,
        setItem: (key: string, value: string) => { storage[key] = value; },
        removeItem: (key: string) => { delete storage[key]; },
        clear: () => { storage = {}; }
      };
    })();
    
    Object.defineProperty(window, 'localStorage', { value: localStorageMock });
    localStorage.setItem('token', 'test-token');

    // Create component
    fixture = TestBed.createComponent(CourseManagementComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  // Test 2: Toggle course form visibility
  it('should toggle course form visibility', () => {
    // Initially form is hidden
    expect(component.showForm).toBeFalsy();
    
    // Click add course button
    const addButton = fixture.debugElement.query(By.css('.add-button'));
    addButton.triggerEventHandler('click', null);
    fixture.detectChanges();
    
    // Form should be visible
    expect(component.showForm).toBeTruthy();
    let form = fixture.debugElement.query(By.css('.course-form'));
    expect(form).toBeTruthy();
    
    // Click again to hide
    addButton.triggerEventHandler('click', null);
    fixture.detectChanges();
    
    // Form should be hidden
    expect(component.showForm).toBeFalsy();
    form = fixture.debugElement.query(By.css('.course-form'));
    expect(form).toBeFalsy();
  });

  // Test 3: Course form validation - Invalid form
  it('should validate course form fields', () => {
    // Show the form
    component.showForm = true;
    fixture.detectChanges();
    
    // Test form validation - initially form should be invalid
    expect(component.courseForm.valid).toBeFalsy();
    
    // Test invalid course code pattern
    component.courseForm.patchValue({
      courseName: 'Test Course',
      courseCode: 'invalid',  // Does not match pattern
      description: 'Test description'
    });
    expect(component.courseForm.get('courseCode')?.valid).toBeFalsy();
    
    // Test valid input
    component.courseForm.patchValue({
      courseName: 'Test Course',
      courseCode: 'CS101',
      description: 'Test description'
    });
    expect(component.courseForm.valid).toBeTruthy();
  });

  // Test 9: Navigation to dashboard
  it('should navigate back to dashboard', () => {
    component.backToDashboard();
    expect(routerMock.navigate).toHaveBeenCalledWith(['/teacher/dashboard']);
  });
});
