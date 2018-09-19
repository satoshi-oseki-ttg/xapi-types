import { InteractionType } from "./InteractionType";
import { IVmResult } from "./IVmResult";

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
