import { IVmQuestion } from "./IVmQuestion";
import { IVmCourseSession } from './IVmCourseSession';

export interface IVmCourseProgress {
  username: string;
  sessions: IVmCourseSession[];
  status: string;
  questions: IVmQuestion[];
}
