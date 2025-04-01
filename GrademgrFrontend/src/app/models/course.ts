export interface Course {
    id: string;
    courseName: string;
    courseCode: string;
    description: string;
    teacherId: string;
    studentIds: string[];
    createdAt: Date;
  }
  