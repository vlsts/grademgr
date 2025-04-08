import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { Router } from '@angular/router';
import { By } from '@angular/platform-browser';
import { Location } from '@angular/common';

import { TeacherDashboardComponent } from './teacher-dashboard.component';

describe('TeacherDashboardComponent', () => {
  let component: TeacherDashboardComponent;
  let fixture: ComponentFixture<TeacherDashboardComponent>;
  let router: Router;
  let location: Location;

  beforeEach(async () => {
    // Create spy objects
    const routerSpy = {
      navigate: jest.fn()
    };

    // Set up TestBed
    await TestBed.configureTestingModule({
      imports: [
        TeacherDashboardComponent,
        RouterTestingModule.withRoutes([
          { path: 'teacher/courses', component: {} as any },
          { path: 'teacher/grades', component: {} as any },
          { path: 'teacher/students', component: {} as any },
          { path: 'login', component: {} as any }
        ])
      ],
      providers: [
        { provide: Router, useValue: routerSpy }
      ]
    }).compileComponents();

    router = TestBed.inject(Router);
    location = TestBed.inject(Location);
    
    // Mock localStorage
    const localStorageMock = (() => {
      let store: Record<string, string> = {};
      return {
        getItem: jest.fn((key: string) => store[key] || null),
        setItem: jest.fn((key: string, value: string) => {
          store[key] = value.toString();
        }),
        removeItem: jest.fn((key: string) => {
          delete store[key];
        }),
        clear: jest.fn(() => {
          store = {};
        })
      };
    })();

    Object.defineProperty(window, 'localStorage', {
      value: localStorageMock
    });

    fixture = TestBed.createComponent(TeacherDashboardComponent);
    component = fixture.componentInstance;
  });

  // Test 1: Component Creation
  it('should create the component', () => {
    localStorage.setItem('token', 'test-token');
    fixture.detectChanges();
    expect(component).toBeTruthy();
  });

  // Test 2: Authentication Check on Init - With Token
  it('should not redirect if token exists on initialization', () => {
    localStorage.setItem('token', 'test-token');
    
    fixture.detectChanges();
    
    expect(router.navigate).not.toHaveBeenCalled();
  });

  // Test 3: Authentication Check on Init - No Token
  it('should redirect to login if no token exists on initialization', () => {
    localStorage.removeItem('token');
    
    fixture.detectChanges();
    
    expect(router.navigate).toHaveBeenCalledWith(['/login']);
  });

  // Test 4: Header Elements Present
  it('should display header with title and logout button', () => {
    localStorage.setItem('token', 'test-token');
    fixture.detectChanges();
    
    const headerElement = fixture.debugElement.query(By.css('header'));
    const titleElement = fixture.debugElement.query(By.css('header h1'));
    const logoutButton = fixture.debugElement.query(By.css('header button'));
    
    expect(headerElement).toBeTruthy();
    expect(titleElement.nativeElement.textContent).toBe('Teacher Dashboard');
    expect(logoutButton.nativeElement.textContent).toBe('Logout');
  });

  // Test 5: Welcome Message
  it('should display welcome message', () => {
    localStorage.setItem('token', 'test-token');
    fixture.detectChanges();
    
    const welcomeHeading = fixture.debugElement.query(By.css('h2'));
    const welcomeParagraph = fixture.debugElement.query(By.css('.dashboard-content > p'));
    
    expect(welcomeHeading.nativeElement.textContent).toBe('Welcome, Teacher!');
    expect(welcomeParagraph.nativeElement.textContent).toContain('This is your teacher dashboard');
  });

  // Test 6: Action Cards Present
  it('should display three action cards', () => {
    localStorage.setItem('token', 'test-token');
    fixture.detectChanges();
    
    const cards = fixture.debugElement.queryAll(By.css('.card'));
    
    expect(cards.length).toBe(3);
    expect(cards[0].query(By.css('h3')).nativeElement.textContent).toBe('Manage Courses');
    expect(cards[1].query(By.css('h3')).nativeElement.textContent).toBe('Manage Grades');
    expect(cards[2].query(By.css('h3')).nativeElement.textContent).toBe('View Students');
  });

  // Test 7: Navigation to Courses
  it('should navigate to courses page when courses button is clicked', () => {
    localStorage.setItem('token', 'test-token');
    fixture.detectChanges();
    
    const coursesButton = fixture.debugElement.queryAll(By.css('.card'))[0]
      .query(By.css('button'));
    coursesButton.triggerEventHandler('click', null);
    
    expect(router.navigate).toHaveBeenCalledWith(['/teacher/courses']);
  });

  // Test 8: Navigation to Grades
  it('should navigate to grades page when grades button is clicked', () => {
    localStorage.setItem('token', 'test-token');
    fixture.detectChanges();
    
    const gradesButton = fixture.debugElement.queryAll(By.css('.card'))[1]
      .query(By.css('button'));
    gradesButton.triggerEventHandler('click', null);
    
    expect(router.navigate).toHaveBeenCalledWith(['/teacher/grades']);
  });

  // Test 9: Navigation to Students
  it('should navigate to students page when students button is clicked', () => {
    localStorage.setItem('token', 'test-token');
    fixture.detectChanges();
    
    const studentsButton = fixture.debugElement.queryAll(By.css('.card'))[2]
      .query(By.css('button'));
    studentsButton.triggerEventHandler('click', null);
    
    expect(router.navigate).toHaveBeenCalledWith(['/teacher/students']);
  });

  // Test 10: Logout Functionality
  it('should clear token and navigate to login on logout', () => {
    localStorage.setItem('token', 'test-token');
    fixture.detectChanges();
    
    const logoutButton = fixture.debugElement.query(By.css('header button'));
    logoutButton.triggerEventHandler('click', null);
    
    expect(localStorage.removeItem).toHaveBeenCalledWith('token');
    expect(router.navigate).toHaveBeenCalledWith(['/login']);
  });
});
