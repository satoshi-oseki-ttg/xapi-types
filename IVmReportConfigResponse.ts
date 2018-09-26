import { IReportType } from './IReportType';
import { IVmReportConfig } from './IVmReportConfig';

export interface IVmReportConfigResponse {
  availableTypes: IReportType[];
  userConfig: IVmReportConfig;
}
