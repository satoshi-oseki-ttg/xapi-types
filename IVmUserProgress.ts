import { IVmCourseAttempt } from "./IVmCourseAttempt";

export interface IVmUserProgress {
  username: string;
  userDisplayName: string;
  statementId: string;
  courseId: string;
  courseName: string;
  status: string;
  timestamp: Date;
  sessions: IVmCourseAttempt[]
}
