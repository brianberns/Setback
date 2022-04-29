import { Record } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Types.js";
import { record_type, int32_type } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Reflection.js";
import { AbstractHighBidModule_create, AbstractHighBidModule_none, AbstractHighBid$reflection } from "./AbstractHighBid.fs.js";
import { SeatModule_numSeats } from "../Setback/Seat.fs.js";
import { contains } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Seq.js";
import { numberHash } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Util.js";

export class AbstractAuction extends Record {
    constructor(NumBids, HighBid) {
        super();
        this.NumBids = (NumBids | 0);
        this.HighBid = HighBid;
    }
}

export function AbstractAuction$reflection() {
    return record_type("Setback.Cfrm.AbstractAuction", [], AbstractAuction, () => [["NumBids", int32_type], ["HighBid", AbstractHighBid$reflection()]]);
}

export const AbstractAuctionModule_initial = new AbstractAuction(0, AbstractHighBidModule_none);

export function AbstractAuctionModule_isComplete(auction) {
    if (!((auction.NumBids >= 0) ? (auction.NumBids <= SeatModule_numSeats) : false)) {
        debugger;
    }
    return auction.NumBids === SeatModule_numSeats;
}

export function AbstractAuctionModule_currentBidderIndex(auction) {
    if (!(!AbstractAuctionModule_isComplete(auction))) {
        debugger;
    }
    return ((auction.NumBids + 1) % SeatModule_numSeats) | 0;
}

export function AbstractAuctionModule_legalBids(auction) {
    if (!(!AbstractAuctionModule_isComplete(auction))) {
        debugger;
    }
    const matchValue = auction.HighBid.Bid | 0;
    switch (matchValue) {
        case 0: {
            return [0, 2, 3, 4];
        }
        case 2: {
            return [0, 3, 4];
        }
        case 3: {
            return [0, 4];
        }
        case 4: {
            if (auction.NumBids === (SeatModule_numSeats - 1)) {
                if (!(AbstractAuctionModule_currentBidderIndex(auction) === 0)) {
                    debugger;
                }
                return [0, 4];
            }
            else {
                return [0];
            }
        }
        default: {
            throw (new Error("Unexpected"));
        }
    }
}

export function AbstractAuctionModule_addBid(bid, auction) {
    if (!contains(bid, AbstractAuctionModule_legalBids(auction), {
        Equals: (x, y) => (x === y),
        GetHashCode: (x) => numberHash(x),
    })) {
        debugger;
    }
    let highBid;
    if (bid === 0) {
        highBid = auction.HighBid;
    }
    else {
        if (!(bid >= auction.HighBid.Bid)) {
            debugger;
        }
        const iBidder = AbstractAuctionModule_currentBidderIndex(auction) | 0;
        if (!((bid > auction.HighBid.Bid) ? true : ((iBidder === 0) ? (bid === 4) : false))) {
            debugger;
        }
        highBid = AbstractHighBidModule_create(iBidder, bid);
    }
    return new AbstractAuction(auction.NumBids + 1, highBid);
}

