import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { StudentDashboardComponent } from './student-dashboard.component';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';

describe('StudentDashboardComponent', () => {
  let component: StudentDashboardComponent;
  let fixture: ComponentFixture<StudentDashboardComponent>;
  let router: Router;

  beforeEach(async () => {
    // Create simple localStorage mock
    const mockLocalStorage = {
      getItem: jest.fn().mockReturnValue('dummy-token'),
      setItem: jest.fn(),
      removeItem: jest.fn()
    };
    Object.defineProperty(window, 'localStorage', { value: mockLocalStorage });

    await TestBed.configureTestingModule({
      imports: [
        CommonModule,
        RouterTestingModule,
        StudentDashboardComponent
      ]
    }).compileComponents();

    router = TestBed.inject(Router);
    jest.spyOn(router, 'navigate');
    
    fixture = TestBed.createComponent(StudentDashboardComponent);
    component = fixture.componentInstance;
  });

  // Test 1: Basic component creation
  it('should create the component', () => {
    expect(component).toBeDefined();
  });

  // Test 2: Component should have logout method
  it('should have logout method', () => {
    expect(typeof component.logout).toBe('function');
  });

  // Test 3: Logout should clear token and navigate
  it('should clear token and navigate on logout', () => {
    component.logout();
    
    expect(localStorage.removeItem).toHaveBeenCalledWith('token');
    expect(router.navigate).toHaveBeenCalled();
  });

  // Test 4: Should check for token on init
  it('should check for token on init', () => {
    // Force token to be null for this specific test
    jest.spyOn(localStorage, 'getItem').mockReturnValueOnce(null);
    
    component.ngOnInit();
    expect(router.navigate).toHaveBeenCalled();
  });

  // Test 5: Should not navigate if token exists
  it('should not navigate if token exists', () => {
    // Mock token exists
    jest.spyOn(localStorage, 'getItem').mockReturnValueOnce('test-token');
    
    component.ngOnInit();
    expect(router.navigate).not.toHaveBeenCalled();
  });
});
