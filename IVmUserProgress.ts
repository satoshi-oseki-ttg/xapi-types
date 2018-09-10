import { IVmCourseSession } from './IVmCourseSession';

export interface IVmUserProgress {
  username: string;
  userDisplayName: string;
  statementId: string;
  courseId: string;
  courseName: string;
  status: string;
  timestamp: Date;
  sessions: IVmCourseSession[]
}
