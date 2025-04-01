export interface User {
    id: string;
    username: string;
    email: string;
    password: string;
    role: 'Student' | 'Teacher';
    createdAt: Date;
    fullName: string;
  }
  