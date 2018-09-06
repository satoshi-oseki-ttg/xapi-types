export interface IVmResult {
  statementId: string;
  type: string;
  success: boolean;
  duration: string | number; // 'PT12M1S' in xAPI, 12345 in view model as epoch
  timestamp: number;
  choices?: string[];
  response?: string;
  score?: number;
}
