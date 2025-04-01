import { StudentInfo } from "./student-info";

export interface CourseDetailResponse {
    id: string;
    courseName: string;
    courseCode: string;
    description: string;
    teacherEmail: string;
    teacherName: string;
    createdAt: Date;
    students: StudentInfo[];
  }