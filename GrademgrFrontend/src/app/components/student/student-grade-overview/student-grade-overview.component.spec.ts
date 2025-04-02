import { ComponentFixture, TestBed } from '@angular/core/testing';

import { StudentGradeOverviewComponent } from './student-grade-overview.component';

describe('StudentGradeOverviewComponent', () => {
  let component: StudentGradeOverviewComponent;
  let fixture: ComponentFixture<StudentGradeOverviewComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [StudentGradeOverviewComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(StudentGradeOverviewComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
