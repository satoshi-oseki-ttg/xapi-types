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
}
