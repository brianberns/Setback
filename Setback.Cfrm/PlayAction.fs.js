import { Union } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Types.js";
import { LeadActionModule_getPlay, LeadActionModule_getActions, LeadAction$reflection } from "./LeadAction.fs.js";
import { FollowActionModule_getPlay, FollowActionModule_getActions, FollowAction$reflection } from "./FollowAction.fs.js";
import { union_type } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Reflection.js";
import { AbstractTrick__get_NumPlays } from "./AbstractTrick.fs.js";
import { map } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Array.js";

export class PlayAction extends Union {
    constructor(tag, ...fields) {
        super();
        this.tag = (tag | 0);
        this.fields = fields;
    }
    cases() {
        return ["Lead", "Follow"];
    }
}

export function PlayAction$reflection() {
    return union_type("Setback.Cfrm.PlayAction", [], PlayAction, () => [[["Item", LeadAction$reflection()]], [["Item", FollowAction$reflection()]]]);
}

export const PlayActionModule_maxPerHand = 6;

export function PlayActionModule_getActions(hand, handLowTrumpRankOpt, playout) {
    const actions = (AbstractTrick__get_NumPlays(playout.CurrentTrick) === 0) ? map((arg0) => (new PlayAction(0, arg0)), LeadActionModule_getActions(hand, handLowTrumpRankOpt, playout)) : map((arg0_1) => (new PlayAction(1, arg0_1)), FollowActionModule_getActions(hand, handLowTrumpRankOpt, playout));
    if (!(actions.length <= PlayActionModule_maxPerHand)) {
        debugger;
    }
    return actions;
}

export function PlayActionModule_getPlay(hand, handLowTrumpRankOpt, playout, _arg1) {
    if (_arg1.tag === 1) {
        const action_1 = _arg1.fields[0];
        return FollowActionModule_getPlay(hand, handLowTrumpRankOpt, playout, action_1);
    }
    else {
        const action = _arg1.fields[0];
        return LeadActionModule_getPlay(hand, handLowTrumpRankOpt, playout, action);
    }
}

