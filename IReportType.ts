export interface IReportType {
  type: string;
  name: string;
  adminOnly?: boolean; // some reports are only for admin users
}
