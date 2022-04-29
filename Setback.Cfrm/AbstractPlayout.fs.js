import { Record } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Types.js";
import { AbstractHighBid$reflection } from "./AbstractHighBid.fs.js";
import { record_type, option_type, enum_type, int32_type } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Reflection.js";
import { AbstractPlayoutHistoryModule_copyTo, AbstractPlayoutHistoryModule_layout, AbstractPlayoutHistoryModule_addTrick, AbstractPlayoutHistoryModule_isComplete, AbstractPlayoutHistoryModule_empty, AbstractPlayoutHistory$reflection } from "./AbstractPlayoutHistory.fs.js";
import { AbstractTrickModule_copyTo, AbstractTrickModule_layout, AbstractTrickModule_highPlayerIndex, AbstractTrickModule_isComplete, AbstractTrickModule_addPlay, AbstractTrick__get_NumPlays, AbstractTrickModule_currentPlayerIndex, AbstractTrickModule_create, AbstractTrick$reflection } from "./AbstractTrick.fs.js";
import { where, exists } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Seq.js";
import { equals } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Util.js";
import { map, defaultArg } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Option.js";
import { SpanLayout$1__Slice_309E3581, SpanLayout_combine } from "./SpanLayout.fs.js";
import { Span$1__get_Length } from "./Prelude.fs.js";

export class AbstractPlayout extends Record {
    constructor(HighBid, TrumpOpt, History, CurrentTrick) {
        super();
        this.HighBid = HighBid;
        this.TrumpOpt = TrumpOpt;
        this.History = History;
        this.CurrentTrick = CurrentTrick;
    }
}

export function AbstractPlayout$reflection() {
    return record_type("Setback.Cfrm.AbstractPlayout", [], AbstractPlayout, () => [["HighBid", AbstractHighBid$reflection()], ["TrumpOpt", option_type(enum_type("PlayingCards.Suit", int32_type, [["Clubs", 0], ["Diamonds", 1], ["Hearts", 2], ["Spades", 3]]))], ["History", AbstractPlayoutHistory$reflection()], ["CurrentTrick", AbstractTrick$reflection()]]);
}

export function AbstractPlayoutModule_create(highBid) {
    if (!(highBid.BidderIndex >= 0)) {
        debugger;
    }
    if (!(highBid.Bid > 0)) {
        debugger;
    }
    return new AbstractPlayout(highBid, void 0, AbstractPlayoutHistoryModule_empty, AbstractTrickModule_create(void 0, highBid.BidderIndex));
}

export function AbstractPlayoutModule_isComplete(playout) {
    return AbstractPlayoutHistoryModule_isComplete(playout.History);
}

export function AbstractPlayoutModule_currentPlayerIndex(playout) {
    if (!(!AbstractPlayoutModule_isComplete(playout))) {
        debugger;
    }
    return AbstractTrickModule_currentPlayerIndex(playout.CurrentTrick) | 0;
}

export function AbstractPlayoutModule_legalPlays(hand, playout) {
    if (!(!AbstractPlayoutModule_isComplete(playout))) {
        debugger;
    }
    const trick = playout.CurrentTrick;
    if (!((trick.SuitLedOpt == null) === (AbstractTrick__get_NumPlays(trick) === 0))) {
        debugger;
    }
    const matchValue = [playout.TrumpOpt, trick.SuitLedOpt];
    if (matchValue[1] != null) {
        if (matchValue[0] != null) {
            const suitLed = matchValue[1] | 0;
            const trump = matchValue[0] | 0;
            const followsSuit = (card) => (card.Suit === suitLed);
            if (exists(followsSuit, hand)) {
                return where((card_1) => {
                    if (card_1.Suit === trump) {
                        return true;
                    }
                    else {
                        return followsSuit(card_1);
                    }
                }, hand);
            }
            else {
                return hand;
            }
        }
        else {
            throw (new Error("Unexpected"));
        }
    }
    else {
        return hand;
    }
}

export function AbstractPlayoutModule_addPlay(card, playout) {
    if (!(!AbstractPlayoutModule_isComplete(playout))) {
        debugger;
    }
    let trump;
    if (!((playout.TrumpOpt != null) ? true : ((AbstractTrick__get_NumPlays(playout.CurrentTrick) === 0) ? equals(playout.History, AbstractPlayoutHistoryModule_empty) : false))) {
        debugger;
    }
    trump = defaultArg(playout.TrumpOpt, card.Suit);
    const trick_1 = AbstractTrickModule_addPlay(trump, card, playout.CurrentTrick);
    const trickComplete = AbstractTrickModule_isComplete(trick_1);
    const history_1 = trickComplete ? AbstractPlayoutHistoryModule_addTrick(trick_1, playout.History) : playout.History;
    let curTrick;
    if (trickComplete ? (!AbstractPlayoutHistoryModule_isComplete(history_1)) : false) {
        const lowTrumpRankOpt = map((tuple) => tuple[0], history_1.LowTakenOpt);
        curTrick = AbstractTrickModule_create(lowTrumpRankOpt, AbstractTrickModule_highPlayerIndex(trick_1));
    }
    else {
        curTrick = trick_1;
    }
    return new AbstractPlayout(playout.HighBid, trump, history_1, curTrick);
}

export const AbstractPlayoutModule_layout = SpanLayout_combine([AbstractPlayoutHistoryModule_layout, AbstractTrickModule_layout]);

export function AbstractPlayoutModule_copyTo(span, handLowTrumpRankOpt, playout) {
    if (!(!AbstractPlayoutModule_isComplete(playout))) {
        debugger;
    }
    if (!(Span$1__get_Length(span) === AbstractPlayoutModule_layout.Length)) {
        debugger;
    }
    const trick = playout.CurrentTrick;
    const slice = SpanLayout$1__Slice_309E3581(AbstractPlayoutModule_layout, 0, span);
    AbstractPlayoutHistoryModule_copyTo(slice, handLowTrumpRankOpt, trick, playout.History);
    const slice_1 = SpanLayout$1__Slice_309E3581(AbstractPlayoutModule_layout, 1, span);
    AbstractTrickModule_copyTo(slice_1, handLowTrumpRankOpt, trick);
}

