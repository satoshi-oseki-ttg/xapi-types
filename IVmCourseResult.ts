import { IVmQuestion } from "./IVmQuestion";
import { IVmCourseSession } from "./IVmCourseSession";

export interface IVmCourseResult {
  username: string;
  courseId: string;
  status: string;
  start: number;
  end: number;
  sessions: IVmCourseSession[];
  questions: IVmQuestion[];
}
