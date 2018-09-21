export interface IUIFilter {
  field: string;
  values: string[];
  matchMode: string; // defaults to 'startsWith'
}
