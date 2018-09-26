import { IReportType } from './IReportType';

export interface IVmReportConfig { // model used in UI
  username: string;
  selected: IReportType[]; // ordered by 'order' property
}
