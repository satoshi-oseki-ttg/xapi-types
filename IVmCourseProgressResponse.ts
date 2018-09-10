import { IVmCourseProgress } from "./IVmCourseProgress";

export interface IVmCourseProgressResponse {
  courseId: string;
  courseName: string;
  users: IVmCourseProgress[];
  totalCount: number;
  maxNumberOfQuestions: number;
}
