import { Record } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Types.js";
import { AbstractAuctionModule_addBid, AbstractAuctionModule_currentBidderIndex, AbstractAuctionModule_isComplete, AbstractAuctionModule_initial, AbstractAuction$reflection } from "./AbstractAuction.fs.js";
import { AbstractPlayoutModule_addPlay, AbstractPlayoutModule_create, AbstractPlayoutModule_currentPlayerIndex, AbstractPlayoutModule_isComplete, AbstractPlayout$reflection } from "./AbstractPlayout.fs.js";
import { record_type, option_type } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Reflection.js";
import { map } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Array.js";
import { DealAction } from "./DealAction.fs.js";
import { PlayActionModule_getActions } from "./PlayAction.fs.js";
import { BidActionModule_getActions } from "./BidAction.fs.js";
import { map as map_1 } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Option.js";

export class AbstractClosedDeal extends Record {
    constructor(Auction, PlayoutOpt) {
        super();
        this.Auction = Auction;
        this.PlayoutOpt = PlayoutOpt;
    }
}

export function AbstractClosedDeal$reflection() {
    return record_type("Setback.Cfrm.AbstractClosedDeal", [], AbstractClosedDeal, () => [["Auction", AbstractAuction$reflection()], ["PlayoutOpt", option_type(AbstractPlayout$reflection())]]);
}

export const AbstractClosedDealModule_initial = new AbstractClosedDeal(AbstractAuctionModule_initial, void 0);

export function AbstractClosedDealModule_isComplete(closedDeal) {
    const matchValue = closedDeal.PlayoutOpt;
    if (matchValue != null) {
        const playout = matchValue;
        return AbstractPlayoutModule_isComplete(playout);
    }
    else {
        const auction = closedDeal.Auction;
        if (AbstractAuctionModule_isComplete(auction)) {
            return auction.HighBid.Bid === 0;
        }
        else {
            return false;
        }
    }
}

export function AbstractClosedDealModule_currentPlayerIndex(closedDeal) {
    const matchValue = closedDeal.PlayoutOpt;
    if (matchValue != null) {
        const playout = matchValue;
        return AbstractPlayoutModule_currentPlayerIndex(playout) | 0;
    }
    else {
        return AbstractAuctionModule_currentBidderIndex(closedDeal.Auction) | 0;
    }
}

export function AbstractClosedDealModule_getActions(hand, handLowTrumpRankOpt, closedDeal) {
    const matchValue = closedDeal.PlayoutOpt;
    if (matchValue != null) {
        const playout = matchValue;
        return map((arg0_1) => (new DealAction(1, arg0_1)), PlayActionModule_getActions(hand, handLowTrumpRankOpt, playout));
    }
    else {
        return map((arg0) => (new DealAction(0, arg0)), BidActionModule_getActions(hand, closedDeal.Auction));
    }
}

export function AbstractClosedDealModule_addBid(bid, closedDeal) {
    if (!(closedDeal.PlayoutOpt == null)) {
        debugger;
    }
    const auction_1 = AbstractAuctionModule_addBid(bid, closedDeal.Auction);
    let playoutOpt;
    const highBid = auction_1.HighBid;
    playoutOpt = ((AbstractAuctionModule_isComplete(auction_1) ? (highBid.Bid > 0) : false) ? AbstractPlayoutModule_create(highBid) : (void 0));
    return new AbstractClosedDeal(auction_1, playoutOpt);
}

export function AbstractClosedDealModule_addPlay(card, closedDeal) {
    if (!(closedDeal.PlayoutOpt != null)) {
        debugger;
    }
    return new AbstractClosedDeal(closedDeal.Auction, map_1((playout) => AbstractPlayoutModule_addPlay(card, playout), closedDeal.PlayoutOpt));
}

