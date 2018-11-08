import { IVmCourseSession } from './IVmCourseSession';

export interface IVmUserProgress {
  username: string;
  userDisplayName: string;
  statementId: any;
  courseId: string;
  courseName: string;
  status: string;
  timestamp: Date;
  sessions: IVmCourseSession[]
}
