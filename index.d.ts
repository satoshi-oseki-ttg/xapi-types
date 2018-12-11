export type InteractionType =
'true-false' |
'choice' |
'fill-in' |
'long-fill-in' |
'likert' |
'matching' |
'performance' |
'sequencing' |
'numeric' |
'other';

export interface IReportConfig {
  type: string;
  order: number;
}

export interface IReportType {
  type: string;
  name: string;
  adminOnly?: boolean; // some reports are only for admin users
}

export interface IUIFilter {
  field: string;
  values: string[];
  matchMode: string; // defaults to 'startsWith'
}

export interface IUISort {
  sortField: string;
  sortOrder: number;
}

export interface IUserReportConfig {
  username: string;
  selected: IReportConfig[];
}

export interface IVmCourse {
  id: string;
  name: string;
}

export interface IVmCourseProgress {
  username: string;
  sessions: IVmCourseSession[];
  status: string;
  questions: IVmQuestion[];
  profiles: IVmUserProfile[];
}

export interface IVmCourseProgressResponse {
  courseId: string;
  courseName: string;
  users: IVmCourseProgress[];
  totalCount: number;
  maxNumberOfQuestions: number;
  userIndexWithMaxNumberOfQuestions: number;
  profileFields: IVmProfileTitle[];
}

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

export interface IVmCourseSession {
  start: number; // Unix Epoch
  end: number; // Unix Epoch
}

export interface IVmQuestion {
    text: string;
    type: InteractionType;
    // for 'choice'
    choices?: string[];
    // for 'matching'
    source?: string[];
    target?: string[];

    correctAnswers: string[];  // array of comma delimited answers

    results: IVmResult[];
    attempts: number;
    // necessary because 'results' above might not have all the results from all sessions.
    // This is set in GraphQL server, not in Analytics.
}

export interface IVmReportConfig { // model used in UI
  username: string;
  selected: IReportType[]; // ordered by 'order' property
}

export interface IVmReportConfigResponse {
  availableTypes: IReportType[];
  userConfig: IVmReportConfig;
}

export interface IVmResponse {
  answer: string; // plain text answer
  value: string; // in bespoke syntax
  // 1:option1,0:option2,0:option3 (<selected>:<option text>) for 'choice' type
}

export interface IVmResult {
  id: string; // question id
  statementId: any; // binary uuid
  type: string;
  response: IVmResponse; 
  // answer: string; // used anywhere?
  success: boolean;
  score?: number;
  duration: string | number; // 'PT12M1S' in xAPI, 12345 in view model as epoch
  timestamp: number; // Unix epoch
}

export interface IVmUserProgress {
  username: string;
  userDisplayName: string;
  statementId: any;
  courseId: string;
  courseName: string;
  status: string;
  timestamp: Date;
  sessions: IVmCourseSession[];
}

export interface IVmProfileTitle {
  title: string;
}

export interface IVmUserProfile {
  username: string;
  title: string;
  text: string;
}
