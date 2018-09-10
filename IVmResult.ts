// export interface IVmResult {
//   statementId: string;
//   type: string;
//   success: boolean;
//   duration: string | number; // 'PT12M1S' in xAPI, 12345 in view model as epoch
//   timestamp: number;
//   choices?: string[];
//   response?: string;
//   score?: number;
// }
export interface IVmResult {
  id: string; // question id
  statementId: any; // binary uuid
  type: string;
  response: string; // 1:option1,0:option2,0:option3 (<selected>:<option text>) for 'choice' type
  answerIds: number[]; // array of indexes for 'choice' type
  correctAnswerIds: number[];  // comma delimited numbers for 'choice' type
  success: boolean;
  score?: number;
  duration: string | number; // 'PT12M1S' in xAPI, 12345 in view model as epoch
  timestamp: number; // Unix epoch
}
