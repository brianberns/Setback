import { max } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Array.js";
import { comparePrimitives } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Util.js";
import { numDealPoints, numTeams, winThreshold as winThreshold_1 } from "../Setback/Setback.fs.js";
import { singleton, append, delay, max as max_1, indexed, where, map, tryExactlyOne } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Seq.js";
import { printf, toText } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/String.js";
import { SpanAction$2, System_String__String_Create_Static_43E13F6 } from "./Prelude.fs.js";
import { AuctionHand_copyTo, AuctionHand_layout } from "./BaselineGameState.fs.js";
import { AbstractAuctionModule_currentBidderIndex } from "./AbstractAuction.fs.js";
import { AbstractScoreModule_shift } from "./AbstractScore.fs.js";

export function winningTeamOpt(_arg1) {
    const scores = _arg1.fields[0];
    const maxScore = max(scores, {
        Compare: (x, y) => comparePrimitives(x, y),
    }) | 0;
    if (maxScore >= winThreshold_1) {
        return tryExactlyOne(map((tuple) => tuple[0], where((tupledArg) => {
            const score = tupledArg[1] | 0;
            return score === maxScore;
        }, indexed(scores))));
    }
    else {
        return void 0;
    }
}

export function AbstractScore_toNeed(_arg1) {
    const scores = _arg1.fields[0];
    const winThreshold = max_1(delay(() => append(map((score) => (score + 1), scores), delay(() => singleton(winThreshold_1)))), {
        Compare: (x, y) => comparePrimitives(x, y),
    }) | 0;
    if (!(numTeams === 2)) {
        debugger;
    }
    return [winThreshold - scores[0], winThreshold - scores[1]];
}

export function AbstractScore_toAbbr(gameScore) {
    const toChar = (need) => {
        if (need <= numDealPoints) {
            return "0123456789"[need];
        }
        else {
            return "x";
        }
    };
    const patternInput = AbstractScore_toNeed(gameScore);
    const usNeed = patternInput[0] | 0;
    const themNeed = patternInput[1] | 0;
    const cThemNeed = toChar(themNeed);
    const arg20 = ((cThemNeed !== "x") ? (usNeed > themNeed) : false) ? "!" : ".";
    return toText(printf("%c%c"))(cThemNeed)(arg20);
}

export function AbstractHighBid_toAbbr(absHighBid) {
    return toText(printf("%d/%d"))(absHighBid.Bid)(absHighBid.BidderIndex);
}

export function AbstractAuction_toAbbr(absAuction) {
    const arg10 = AbstractHighBid_toAbbr(absAuction.HighBid);
    return toText(printf("%s/%d"))(arg10)(absAuction.NumBids);
}

export function Hand_toAbbr(hand) {
    return System_String__String_Create_Static_43E13F6(AuctionHand_layout.Length, hand, new SpanAction$2(0, (span, hand_1) => {
        AuctionHand_copyTo(span, hand_1);
    }));
}

export function toAbbr(auction, gameScore, hand) {
    let gameScore_1;
    const iCurTeam = (AbstractAuctionModule_currentBidderIndex(auction) % numTeams) | 0;
    gameScore_1 = AbstractScoreModule_shift(iCurTeam, gameScore);
    const arg30 = Hand_toAbbr(hand);
    const arg20 = AbstractAuction_toAbbr(auction);
    const arg10 = AbstractScore_toAbbr(gameScore_1);
    return toText(printf("%s/%s/%s"))(arg10)(arg20)(arg30);
}

