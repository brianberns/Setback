import { Union } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Types.js";
import { BidAction$reflection } from "./BidAction.fs.js";
import { PlayAction$reflection } from "./PlayAction.fs.js";
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

