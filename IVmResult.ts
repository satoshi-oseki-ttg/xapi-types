export interface IVmResult {
  statementId: string;
  type: string;
  success: boolean;
  duration: string;
  timestamp: number;
  choices?: string[];
  response?: string;
  score?: number;
}
