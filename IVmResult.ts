import { IVmResponse } from './IVmResponse';

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
  response: IVmResponse; 
  // answer: string; // used anywhere?
  success: boolean;
  score?: number;
  duration: string | number; // 'PT12M1S' in xAPI, 12345 in view model as epoch
  timestamp: number; // Unix epoch
}
