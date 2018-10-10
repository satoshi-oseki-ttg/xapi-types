import { IVmCourseSession } from './IVmCourseSession';
import { IVmQuestion } from './IVmQuestion';

export interface IVmCourseResult {
  username: string;
  courseId: string;
  status: string;
  start: number;
  end: number;
  sessions: IVmCourseSession[];
  questions: IVmQuestion[];
  lastTimestamp: Date;
}
