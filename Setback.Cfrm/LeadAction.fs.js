import { Union } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Types.js";
import { union_type, option_type, enum_type, int32_type } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Reflection.js";
import { Rank_lower, AbstractTrick__get_NumPlays } from "./AbstractTrick.fs.js";
import { compareArrays, comparePrimitives, min, safeHash, equals, compare } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Util.js";
import { PlayingCards_Rank__Rank_get_GamePoints } from "../Setback/Rank.fs.js";
import { last, map } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Array.js";
import { BidActionModule_chooseTrumpRanks } from "./BidAction.fs.js";
import { map as map_1 } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Option.js";
import { minBy, contains, where, maxBy, pairwise, forAll, choose, sort, toArray } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Seq.js";
import { distinct } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Seq2.js";
import { AbstractPlayoutModule_legalPlays } from "./AbstractPlayout.fs.js";
import { Card_$ctor_Z106B7333 } from "../PlayingCards/Card.fs.js";

export class LeadAction extends Union {
    constructor(tag, ...fields) {
        super();
        this.tag = (tag | 0);
        this.fields = fields;
    }
    cases() {
        return ["EstablishTrump", "LeadTrump", "LeadStrong", "LeadWeak"];
    }
}

export function LeadAction$reflection() {
    return union_type("Setback.Cfrm.LeadAction", [], LeadAction, () => [[["Item", int32_type]], [["Item", option_type(enum_type("PlayingCards.Rank", int32_type, [["Two", 2], ["Three", 3], ["Four", 4], ["Five", 5], ["Six", 6], ["Seven", 7], ["Eight", 8], ["Nine", 9], ["Ten", 10], ["Jack", 11], ["Queen", 12], ["King", 13], ["Ace", 14]]))]], [], []]);
}

export function LeadActionModule_createOpt(hand, lowTrumpRankOpt, playout, card) {
    if (!(AbstractTrick__get_NumPlays(playout.CurrentTrick) === 0)) {
        debugger;
    }
    const matchValue = playout.TrumpOpt;
    if (matchValue != null) {
        const trump = matchValue | 0;
        return (card.Suit === trump) ? (((!(compare(card.Rank, lowTrumpRankOpt) >= 0)) ? (() => {
            debugger;
        })() : null, ((PlayingCards_Rank__Rank_get_GamePoints(card.Rank) > 0) ? true : equals(card.Rank, lowTrumpRankOpt)) ? (new LeadAction(1, card.Rank)) : (new LeadAction(1, void 0)))) : ((card.Rank > 10) ? (new LeadAction(2)) : (new LeadAction(3)));
    }
    else {
        let patternInput;
        const suits = map((tuple) => tuple[0], BidActionModule_chooseTrumpRanks(hand));
        const suit1Opt = (suits.length > 1) ? suits[1] : (void 0);
        patternInput = [suits[0], suit1Opt];
        const suit1Opt_1 = patternInput[1];
        const suit0 = patternInput[0] | 0;
        if (card.Suit === suit0) {
            return new LeadAction(0, 0);
        }
        else if (equals(card.Suit, suit1Opt_1)) {
            return new LeadAction(0, 1);
        }
        else {
            return void 0;
        }
    }
}

export function LeadActionModule_getActions(hand, handLowTrumpRankOpt, playout) {
    let lowTrumpRankOpt;
    if (!equals(Rank_lower(map_1((tuple) => tuple[0], playout.History.LowTakenOpt), playout.CurrentTrick.LowTrumpRankOpt), playout.CurrentTrick.LowTrumpRankOpt)) {
        debugger;
    }
    lowTrumpRankOpt = Rank_lower(handLowTrumpRankOpt, playout.CurrentTrick.LowTrumpRankOpt);
    const actions = toArray(sort(distinct(choose((card) => LeadActionModule_createOpt(hand, lowTrumpRankOpt, playout, card), AbstractPlayoutModule_legalPlays(hand, playout)), {
        Equals: (x, y) => equals(x, y),
        GetHashCode: (x) => safeHash(x),
    }), {
        Compare: (x_1, y_1) => compare(x_1, y_1),
    }));
    if (!(actions.length > 0)) {
        debugger;
    }
    return actions;
}

export function LeadActionModule_establishTrumpRank(ranks) {
    if (!(ranks.length > 0)) {
        debugger;
    }
    let highestLowRank;
    if (!forAll((tupledArg) => {
        const rank1 = tupledArg[0] | 0;
        const rank2 = tupledArg[1] | 0;
        return rank1 > rank2;
    }, pairwise(ranks))) {
        debugger;
    }
    highestLowRank = min((x, y) => comparePrimitives(x, y), 4, last(ranks));
    return maxBy((rank) => {
        const group = ((rank > 11) ? 3 : (((rank < 10) ? (rank > highestLowRank) : false) ? 2 : ((rank === 10) ? 1 : ((rank === 11) ? 0 : -1)))) | 0;
        return [group, rank];
    }, ranks, {
        Compare: (x_1, y_1) => compareArrays(x_1, y_1),
    }) | 0;
}

export function LeadActionModule_getPlay(hand, handLowTrumpRankOpt, playout, action) {
    if (!(AbstractTrick__get_NumPlays(playout.CurrentTrick) === 0)) {
        debugger;
    }
    let lowTrumpRankOpt;
    if (!equals(Rank_lower(map_1((tuple) => tuple[0], playout.History.LowTakenOpt), playout.CurrentTrick.LowTrumpRankOpt), playout.CurrentTrick.LowTrumpRankOpt)) {
        debugger;
    }
    lowTrumpRankOpt = Rank_lower(handLowTrumpRankOpt, playout.CurrentTrick.LowTrumpRankOpt);
    const matchValue = playout.TrumpOpt;
    if (matchValue != null) {
        const trump = matchValue | 0;
        if (action.tag === 1) {
            if (action.fields[0] == null) {
                return maxBy((card_3) => card_3.Rank, where((card_2) => {
                    if (!((card_2.Suit !== trump) ? true : (compare(card_2.Rank, lowTrumpRankOpt) >= 0))) {
                        debugger;
                    }
                    if ((card_2.Suit === trump) ? (PlayingCards_Rank__Rank_get_GamePoints(card_2.Rank) === 0) : false) {
                        return compare(card_2.Rank, lowTrumpRankOpt) > 0;
                    }
                    else {
                        return false;
                    }
                }, AbstractPlayoutModule_legalPlays(hand, playout)), {
                    Compare: (x_2, y_2) => comparePrimitives(x_2, y_2),
                });
            }
            else {
                const rank_1 = action.fields[0] | 0;
                const card_1 = Card_$ctor_Z106B7333(rank_1, trump);
                if (!contains(card_1, AbstractPlayoutModule_legalPlays(hand, playout), {
                    Equals: (x_1, y_1) => equals(x_1, y_1),
                    GetHashCode: (x_1) => safeHash(x_1),
                })) {
                    debugger;
                }
                return card_1;
            }
        }
        else if (action.tag === 2) {
            return maxBy((card_5) => card_5.Rank, where((card_4) => {
                if (card_4.Suit !== trump) {
                    return card_4.Rank > 10;
                }
                else {
                    return false;
                }
            }, AbstractPlayoutModule_legalPlays(hand, playout)), {
                Compare: (x_3, y_3) => comparePrimitives(x_3, y_3),
            });
        }
        else if (action.tag === 3) {
            return minBy((card_7) => card_7.Rank, where((card_6) => {
                if (card_6.Suit !== trump) {
                    return card_6.Rank <= 10;
                }
                else {
                    return false;
                }
            }, AbstractPlayoutModule_legalPlays(hand, playout)), {
                Compare: (x_4, y_4) => comparePrimitives(x_4, y_4),
            });
        }
        else {
            throw (new Error("Unexpected"));
        }
    }
    else {
        const suitRanks = BidActionModule_chooseTrumpRanks(hand);
        if (action.tag === 0) {
            const iSuit = action.fields[0] | 0;
            const patternInput = suitRanks[iSuit];
            const suit = patternInput[0] | 0;
            const ranks = patternInput[1];
            const rank = LeadActionModule_establishTrumpRank(ranks) | 0;
            const card = Card_$ctor_Z106B7333(rank, suit);
            if (!contains(card, AbstractPlayoutModule_legalPlays(hand, playout), {
                Equals: (x, y) => equals(x, y),
                GetHashCode: (x) => safeHash(x),
            })) {
                debugger;
            }
            return card;
        }
        else {
            throw (new Error("Unexpected"));
        }
    }
}

