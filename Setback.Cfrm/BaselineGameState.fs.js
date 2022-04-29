import { SpanLayout$1__Slice_309E3581, SpanLayout_ofLength, SpanLayout_combine } from "./SpanLayout.fs.js";
import { map, min, replicate } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Array.js";
import { BidActionModule_chooseTrumpRanks, BidActionModule_numSuitsMax } from "./BidAction.fs.js";
import { numCardsPerHand } from "../Setback/Setback.fs.js";
import { System_String__String_Contains_244C7CD6, SpanAction$2, System_String__String_Create_Static_43E13F6, Char_fromDigit, Span$1__Slice_Z524259A4, Span$1__Slice_Z37302880, Span$1__Fill_2B595, Span$1__get_Length } from "./Prelude.fs.js";
import { comparePrimitives } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Util.js";
import { PlayingCards_Rank__Rank_get_Char } from "../PlayingCards/Rank.fs.js";
import { PlayActionModule_maxPerHand } from "./PlayAction.fs.js";
import { DealActionModule_copyTo, DealActionModule_layout } from "./DealAction.fs.js";
import { AbstractPlayoutModule_copyTo, AbstractPlayoutModule_layout } from "./AbstractPlayout.fs.js";
import { AbstractAuctionModule_isComplete, AbstractAuctionModule_copyTo, AbstractAuctionModule_layout } from "./AbstractAuction.fs.js";
import { AbstractOpenDealModule_currentLowTrumpRankOpt, AbstractOpenDealModule_currentHand } from "./AbstractOpenDeal.fs.js";
import { trimEnd } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/String.js";

export const AuctionHand_layout = SpanLayout_combine(replicate(BidActionModule_numSuitsMax, SpanLayout_ofLength(numCardsPerHand)));

function AuctionHand_copySuitTo(ranks, span) {
    if (!(Span$1__get_Length(span) === numCardsPerHand)) {
        debugger;
    }
    const minRank = min(ranks, {
        Compare: (x, y) => comparePrimitives(x, y),
    }) | 0;
    for (let iRank = 0; iRank <= (numCardsPerHand - 1); iRank++) {
        let cRank;
        if (iRank < ranks.length) {
            const rank = ranks[iRank] | 0;
            cRank = (((rank >= 10) ? true : (rank === minRank)) ? PlayingCards_Rank__Rank_get_Char(rank) : "x");
        }
        else {
            cRank = ".";
        }
        Span$1__Fill_2B595(Span$1__Slice_Z37302880(span, iRank, 1), cRank);
    }
}

export function AuctionHand_copyTo(span, hand) {
    if (!(Span$1__get_Length(span) === AuctionHand_layout.Length)) {
        debugger;
    }
    const ranksArrays = map((tuple) => tuple[1], BidActionModule_chooseTrumpRanks(hand));
    if (!(ranksArrays.length <= BidActionModule_numSuitsMax)) {
        debugger;
    }
    for (let iRanks = 0; iRanks <= (BidActionModule_numSuitsMax - 1); iRanks++) {
        const slice = SpanLayout$1__Slice_309E3581(AuctionHand_layout, iRanks, span);
        if (iRanks < ranksArrays.length) {
            AuctionHand_copySuitTo(ranksArrays[iRanks], slice);
        }
        else {
            if (!(iRanks > 0)) {
                debugger;
            }
            Span$1__Fill_2B595(slice, ".");
        }
    }
}

export const PlayoutHand_layout = SpanLayout_combine(replicate(PlayActionModule_maxPerHand, DealActionModule_layout));

export function PlayoutHand_copyTo(span, dealActions) {
    if (!(dealActions.length <= PlayActionModule_maxPerHand)) {
        debugger;
    }
    if (!(Span$1__get_Length(span) === PlayoutHand_layout.Length)) {
        debugger;
    }
    for (let iAction = 0; iAction <= (dealActions.length - 1); iAction++) {
        const action = dealActions[iAction];
        const slice = SpanLayout$1__Slice_309E3581(PlayoutHand_layout, iAction, span);
        DealActionModule_copyTo(slice, action);
    }
    Span$1__Fill_2B595(Span$1__Slice_Z524259A4(span, 2 * dealActions.length), ".");
}

export const AbstractPlayoutPlus_layout = SpanLayout_combine([AbstractPlayoutModule_layout, PlayoutHand_layout]);

export function AbstractPlayoutPlus_copyTo(span, dealActions, handLowTrumpRankOpt, playout) {
    if (!(AbstractPlayoutPlus_layout.Length === Span$1__get_Length(span))) {
        debugger;
    }
    const slice = SpanLayout$1__Slice_309E3581(AbstractPlayoutPlus_layout, 0, span);
    AbstractPlayoutModule_copyTo(slice, handLowTrumpRankOpt, playout);
    const slice_1 = SpanLayout$1__Slice_309E3581(AbstractPlayoutPlus_layout, 1, span);
    PlayoutHand_copyTo(slice_1, dealActions);
}

export const AbstractAuctionPlus_layout = (() => {
    const nFill = ((AbstractPlayoutPlus_layout.Length - AbstractAuctionModule_layout.Length) - AuctionHand_layout.Length) | 0;
    return SpanLayout_combine([AbstractAuctionModule_layout, AuctionHand_layout, SpanLayout_ofLength(nFill)]);
})();

export function AbstractAuctionPlus_copyTo(span, hand, auction) {
    if (!(AbstractAuctionPlus_layout.Length === Span$1__get_Length(span))) {
        debugger;
    }
    const slice = SpanLayout$1__Slice_309E3581(AbstractAuctionPlus_layout, 0, span);
    AbstractAuctionModule_copyTo(slice, auction);
    const slice_1 = SpanLayout$1__Slice_309E3581(AbstractAuctionPlus_layout, 1, span);
    AuctionHand_copyTo(slice_1, hand);
    Span$1__Fill_2B595(SpanLayout$1__Slice_309E3581(AbstractAuctionPlus_layout, 2, span), ".");
}

export const EstablishTrump_layout = (() => {
    const nFill = (((AbstractPlayoutPlus_layout.Length - 1) - 1) - AuctionHand_layout.Length) | 0;
    return SpanLayout_combine([SpanLayout_ofLength(1), SpanLayout_ofLength(1), AuctionHand_layout, SpanLayout_ofLength(nFill)]);
})();

export function EstablishTrump_copyTo(span, hand, auction) {
    if (!(EstablishTrump_layout.Length === Span$1__get_Length(span))) {
        debugger;
    }
    if (!AbstractAuctionModule_isComplete(auction)) {
        debugger;
    }
    Span$1__Fill_2B595(SpanLayout$1__Slice_309E3581(EstablishTrump_layout, 0, span), "E");
    const cBid = Char_fromDigit(auction.HighBid.Bid);
    Span$1__Fill_2B595(SpanLayout$1__Slice_309E3581(EstablishTrump_layout, 1, span), cBid);
    const slice = SpanLayout$1__Slice_309E3581(EstablishTrump_layout, 2, span);
    AuctionHand_copyTo(slice, hand);
    Span$1__Fill_2B595(SpanLayout$1__Slice_309E3581(EstablishTrump_layout, 3, span), ".");
}

export function copyTo(span, dealActions, openDeal) {
    const closedDeal = openDeal.ClosedDeal;
    const auction = closedDeal.Auction;
    const hand = AbstractOpenDealModule_currentHand(openDeal);
    const matchValue = closedDeal.PlayoutOpt;
    if (matchValue != null) {
        const playout = matchValue;
        if (playout.TrumpOpt == null) {
            EstablishTrump_copyTo(span, hand, auction);
        }
        else {
            const handLowTrumpRankOpt = AbstractOpenDealModule_currentLowTrumpRankOpt(openDeal);
            AbstractPlayoutPlus_copyTo(span, dealActions, handLowTrumpRankOpt, playout);
        }
    }
    else {
        AbstractAuctionPlus_copyTo(span, hand, auction);
    }
}

export function getKey(dealActions, openDeal) {
    let str;
    if (!(AbstractAuctionPlus_layout.Length === AbstractPlayoutPlus_layout.Length)) {
        debugger;
    }
    str = System_String__String_Create_Static_43E13F6(AbstractPlayoutPlus_layout.Length, [dealActions, openDeal], new SpanAction$2(0, (span, _arg1) => {
        const openDeal_1 = _arg1[1];
        const dealActions_1 = _arg1[0];
        Span$1__Fill_2B595(span, "?");
        copyTo(span, dealActions_1, openDeal_1);
    }));
    if (!(!System_String__String_Contains_244C7CD6(str, "?"))) {
        debugger;
    }
    return trimEnd(str, ".");
}

