import { Record } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Types.js";
import { record_type, enum_type, int32_type } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Reflection.js";
import { SeatModule_numSeats } from "../Setback/Seat.fs.js";
import { numTeams } from "../Setback/Setback.fs.js";
import { AbstractScoreModule_zero, AbstractScore, AbstractScore__Item_Z524259A4 } from "./AbstractScore.fs.js";
import { singleton, collect, delay, toArray } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Seq.js";
import { op_UnaryNegation_Int32 } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Int32.js";
import { rangeDouble } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Range.js";
import { equals } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Util.js";
import { SpanLayout$1__Slice_309E3581, SpanLayout_ofLength, SpanLayout_combine } from "./SpanLayout.fs.js";
import { Span$1__Fill_2B595, Char_fromDigit, Span$1__get_Length } from "./Prelude.fs.js";

export class AbstractHighBid extends Record {
    constructor(BidderIndex, Bid) {
        super();
        this.BidderIndex = (BidderIndex | 0);
        this.Bid = (Bid | 0);
    }
}

export function AbstractHighBid$reflection() {
    return record_type("Setback.Cfrm.AbstractHighBid", [], AbstractHighBid, () => [["BidderIndex", int32_type], ["Bid", enum_type("Setback.Bid", int32_type, [["Pass", 0], ["Two", 2], ["Three", 3], ["Four", 4]])]]);
}

export const AbstractHighBidModule_none = new AbstractHighBid(-1, 0);

export function AbstractHighBidModule_create(bidderIdx, bid) {
    if (!((bidderIdx >= 0) ? (bidderIdx < SeatModule_numSeats) : false)) {
        debugger;
    }
    if (!(bid !== 0)) {
        debugger;
    }
    return new AbstractHighBid(bidderIdx, bid);
}

export function AbstractHighBidModule_finalizeDealScore(dealScore, highBid) {
    if (highBid.Bid > 0) {
        const iBidderTeam = (highBid.BidderIndex % numTeams) | 0;
        const nBid = highBid.Bid | 0;
        if (!(nBid > 0)) {
            debugger;
        }
        if (AbstractScore__Item_Z524259A4(dealScore, iBidderTeam) < nBid) {
            return new AbstractScore(0, toArray(delay(() => collect((iTeam) => ((iTeam === iBidderTeam) ? singleton(op_UnaryNegation_Int32(nBid)) : singleton(AbstractScore__Item_Z524259A4(dealScore, iTeam))), rangeDouble(0, 1, numTeams - 1)))));
        }
        else {
            return dealScore;
        }
    }
    else {
        if (!equals(dealScore, AbstractScoreModule_zero)) {
            debugger;
        }
        return dealScore;
    }
}

export const AbstractHighBidModule_layout = SpanLayout_combine([SpanLayout_ofLength(1), SpanLayout_ofLength(1)]);

export function AbstractHighBidModule_copyTo(span, highBid) {
    if (!(Span$1__get_Length(span) === AbstractHighBidModule_layout.Length)) {
        debugger;
    }
    const cIndex = (highBid.BidderIndex === -1) ? "." : Char_fromDigit(highBid.BidderIndex);
    Span$1__Fill_2B595(SpanLayout$1__Slice_309E3581(AbstractHighBidModule_layout, 0, span), cIndex);
    const cBid = Char_fromDigit(highBid.Bid);
    Span$1__Fill_2B595(SpanLayout$1__Slice_309E3581(AbstractHighBidModule_layout, 1, span), cBid);
}

