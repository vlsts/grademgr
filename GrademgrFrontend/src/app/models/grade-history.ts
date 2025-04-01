export interface GradeHistory {
    id: string;
    gradeId: string;
    previousGrade: number;
    changedAt: Date;
    changedBy: string;
    changeReason: string;
  }
  