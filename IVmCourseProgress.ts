import { IVmCourseSession } from './IVmCourseSession';
import { IVmQuestion } from './IVmQuestion';

export interface IVmCourseProgress {
  username: string;
  sessions: IVmCourseSession[];
  status: string;
  questions: IVmQuestion[];
}
