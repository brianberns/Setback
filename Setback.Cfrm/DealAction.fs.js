import { Union } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Types.js";
import { BidAction$reflection } from "./BidAction.fs.js";
import { PlayActionModule_copyTo, PlayActionModule_layout, PlayAction$reflection } from "./PlayAction.fs.js";
import { union_type } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Reflection.js";

export class DealAction extends Union {
    constructor(tag, ...fields) {
        super();
        this.tag = (tag | 0);
        this.fields = fields;
    }
    cases() {
        return ["DealBidAction", "DealPlayAction"];
    }
}

export function DealAction$reflection() {
    return union_type("Setback.Cfrm.DealAction", [], DealAction, () => [[["Item", BidAction$reflection()]], [["Item", PlayAction$reflection()]]]);
}

export const DealActionModule_layout = PlayActionModule_layout;

export function DealActionModule_copyTo(span, _arg1) {
    if (_arg1.tag === 1) {
        const action = _arg1.fields[0];
        PlayActionModule_copyTo(span, action);
    }
    else {
        throw (new Error("Unexpected"));
    }
}

