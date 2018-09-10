import { IVmQuestion } from "./IVmQuestion";

export interface IVmCourseProgress {
  username: string;
  status: string;
  questions: IVmQuestion[];
}