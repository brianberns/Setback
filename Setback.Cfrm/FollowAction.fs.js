import { Union } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Types.js";
import { union_type, enum_type, int32_type } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Reflection.js";
import { Rank_lower, AbstractTrick__get_NumPlays } from "./AbstractTrick.fs.js";
import { comparePrimitives, safeHash, equals, compare } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Util.js";
import { PlayingCards_Rank__Rank_get_GamePoints } from "../Setback/Rank.fs.js";
import { map } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Option.js";
import { contains, find, minBy, where, maxBy, map as map_1, sort, toArray } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Seq.js";
import { distinct } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Seq2.js";
import { AbstractPlayoutModule_legalPlays } from "./AbstractPlayout.fs.js";
import { Card_$ctor_Z106B7333 } from "../PlayingCards/Card.fs.js";
import { SpanLayout$1__Slice_309E3581, SpanLayout_ofLength, SpanLayout_combine } from "./SpanLayout.fs.js";
import { Span$1__Fill_2B595, Char_fromDigit, Span$1__get_Length } from "./Prelude.fs.js";
import { PlayingCards_Rank__Rank_get_Char } from "../PlayingCards/Rank.fs.js";

export class FollowAction extends Union {
    constructor(tag, ...fields) {
        super();
        this.tag = (tag | 0);
        this.fields = fields;
    }
    cases() {
        return ["PlayTrump", "PlayTrumpWin", "PlayTrumpLose", "FollowSuitWin", "FollowSuitLose", "ContributeTen", "ContributeGame", "Duck"];
    }
}

export function FollowAction$reflection() {
    return union_type("Setback.Cfrm.FollowAction", [], FollowAction, () => [[["Item", enum_type("PlayingCards.Rank", int32_type, [["Two", 2], ["Three", 3], ["Four", 4], ["Five", 5], ["Six", 6], ["Seven", 7], ["Eight", 8], ["Nine", 9], ["Ten", 10], ["Jack", 11], ["Queen", 12], ["King", 13], ["Ace", 14]])]], [], [], [["Item", int32_type]], [["Item", int32_type]], [], [], []]);
}

export function FollowActionModule_create(lowTrumpRankOpt, playout, card) {
    const trick = playout.CurrentTrick;
    if (!(AbstractTrick__get_NumPlays(trick) > 0)) {
        debugger;
    }
    const matchValue = [playout.TrumpOpt, trick.SuitLedOpt, trick.HighPlayOpt];
    let pattern_matching_result, highPlay, suitLed, trump;
    if (matchValue[0] != null) {
        if (matchValue[1] != null) {
            if (matchValue[2] != null) {
                pattern_matching_result = 0;
                highPlay = matchValue[2];
                suitLed = matchValue[1];
                trump = matchValue[0];
            }
            else {
                pattern_matching_result = 1;
            }
        }
        else {
            pattern_matching_result = 1;
        }
    }
    else {
        pattern_matching_result = 1;
    }
    switch (pattern_matching_result) {
        case 0: {
            const highCard = highPlay.Play;
            if (card.Suit === trump) {
                if (!(compare(card.Rank, lowTrumpRankOpt) >= 0)) {
                    debugger;
                }
                if ((PlayingCards_Rank__Rank_get_GamePoints(card.Rank) > 0) ? true : equals(card.Rank, lowTrumpRankOpt)) {
                    return new FollowAction(0, card.Rank);
                }
                else if ((highCard.Suit !== trump) ? true : (card.Rank > highCard.Rank)) {
                    return new FollowAction(1);
                }
                else {
                    return new FollowAction(2);
                }
            }
            else if (card.Suit === suitLed) {
                if ((highCard.Suit === suitLed) ? (card.Rank > highCard.Rank) : false) {
                    return new FollowAction(3, PlayingCards_Rank__Rank_get_GamePoints(card.Rank));
                }
                else {
                    return new FollowAction(4, PlayingCards_Rank__Rank_get_GamePoints(card.Rank));
                }
            }
            else {
                const matchValue_1 = PlayingCards_Rank__Rank_get_GamePoints(card.Rank) | 0;
                switch (matchValue_1) {
                    case 0: {
                        return new FollowAction(7);
                    }
                    case 10: {
                        return new FollowAction(5);
                    }
                    default: {
                        return new FollowAction(6);
                    }
                }
            }
        }
        case 1: {
            throw (new Error("Unexpected"));
        }
    }
}

export function FollowActionModule_getActions(hand, handLowTrumpRankOpt, playout) {
    let lowTrumpRankOpt;
    if (!equals(Rank_lower(map((tuple) => tuple[0], playout.History.LowTakenOpt), playout.CurrentTrick.LowTrumpRankOpt), playout.CurrentTrick.LowTrumpRankOpt)) {
        debugger;
    }
    lowTrumpRankOpt = Rank_lower(handLowTrumpRankOpt, playout.CurrentTrick.LowTrumpRankOpt);
    const actions = toArray(sort(distinct(map_1((card) => FollowActionModule_create(lowTrumpRankOpt, playout, card), AbstractPlayoutModule_legalPlays(hand, playout)), {
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

export function FollowActionModule_getPlay(hand, handLowTrumpRankOpt, playout, action) {
    if (!(AbstractTrick__get_NumPlays(playout.CurrentTrick) > 0)) {
        debugger;
    }
    let lowTrumpRankOpt;
    if (!equals(Rank_lower(map((tuple) => tuple[0], playout.History.LowTakenOpt), playout.CurrentTrick.LowTrumpRankOpt), playout.CurrentTrick.LowTrumpRankOpt)) {
        debugger;
    }
    lowTrumpRankOpt = Rank_lower(handLowTrumpRankOpt, playout.CurrentTrick.LowTrumpRankOpt);
    const matchValue = [playout.TrumpOpt, playout.CurrentTrick.SuitLedOpt];
    let pattern_matching_result, suitLed, trump;
    if (matchValue[0] != null) {
        if (matchValue[1] != null) {
            pattern_matching_result = 0;
            suitLed = matchValue[1];
            trump = matchValue[0];
        }
        else {
            pattern_matching_result = 1;
        }
    }
    else {
        pattern_matching_result = 1;
    }
    switch (pattern_matching_result) {
        case 0: {
            switch (action.tag) {
                case 1: {
                    return maxBy((card_2) => card_2.Rank, where((card_1) => {
                        if (!((card_1.Suit !== trump) ? true : (compare(card_1.Rank, lowTrumpRankOpt) >= 0))) {
                            debugger;
                        }
                        if ((card_1.Suit === trump) ? (PlayingCards_Rank__Rank_get_GamePoints(card_1.Rank) === 0) : false) {
                            return compare(card_1.Rank, lowTrumpRankOpt) > 0;
                        }
                        else {
                            return false;
                        }
                    }, AbstractPlayoutModule_legalPlays(hand, playout)), {
                        Compare: (x_1, y_1) => comparePrimitives(x_1, y_1),
                    });
                }
                case 2: {
                    return minBy((card_4) => card_4.Rank, where((card_3) => {
                        if (!((card_3.Suit !== trump) ? true : (compare(card_3.Rank, lowTrumpRankOpt) >= 0))) {
                            debugger;
                        }
                        if ((card_3.Suit === trump) ? (PlayingCards_Rank__Rank_get_GamePoints(card_3.Rank) === 0) : false) {
                            return compare(card_3.Rank, lowTrumpRankOpt) > 0;
                        }
                        else {
                            return false;
                        }
                    }, AbstractPlayoutModule_legalPlays(hand, playout)), {
                        Compare: (x_2, y_2) => comparePrimitives(x_2, y_2),
                    });
                }
                case 3: {
                    const gamePoints = action.fields[0] | 0;
                    return maxBy((card_6) => card_6.Rank, where((card_5) => {
                        if ((card_5.Suit !== trump) ? (card_5.Suit === suitLed) : false) {
                            return PlayingCards_Rank__Rank_get_GamePoints(card_5.Rank) === gamePoints;
                        }
                        else {
                            return false;
                        }
                    }, AbstractPlayoutModule_legalPlays(hand, playout)), {
                        Compare: (x_3, y_3) => comparePrimitives(x_3, y_3),
                    });
                }
                case 4: {
                    const gamePoints_1 = action.fields[0] | 0;
                    return minBy((card_8) => card_8.Rank, where((card_7) => {
                        if ((card_7.Suit !== trump) ? (card_7.Suit === suitLed) : false) {
                            return PlayingCards_Rank__Rank_get_GamePoints(card_7.Rank) === gamePoints_1;
                        }
                        else {
                            return false;
                        }
                    }, AbstractPlayoutModule_legalPlays(hand, playout)), {
                        Compare: (x_4, y_4) => comparePrimitives(x_4, y_4),
                    });
                }
                case 5: {
                    return find((card_9) => {
                        if ((card_9.Suit !== trump) ? (card_9.Suit !== suitLed) : false) {
                            return card_9.Rank === 10;
                        }
                        else {
                            return false;
                        }
                    }, AbstractPlayoutModule_legalPlays(hand, playout));
                }
                case 6: {
                    return maxBy((card_11) => PlayingCards_Rank__Rank_get_GamePoints(card_11.Rank), where((card_10) => {
                        if (((card_10.Suit !== trump) ? (card_10.Suit !== suitLed) : false) ? (card_10.Rank !== 10) : false) {
                            return PlayingCards_Rank__Rank_get_GamePoints(card_10.Rank) > 0;
                        }
                        else {
                            return false;
                        }
                    }, AbstractPlayoutModule_legalPlays(hand, playout)), {
                        Compare: (x_5, y_5) => comparePrimitives(x_5, y_5),
                    });
                }
                case 7: {
                    return minBy((card_13) => card_13.Rank, where((card_12) => {
                        if ((card_12.Suit !== trump) ? (card_12.Suit !== suitLed) : false) {
                            return PlayingCards_Rank__Rank_get_GamePoints(card_12.Rank) === 0;
                        }
                        else {
                            return false;
                        }
                    }, AbstractPlayoutModule_legalPlays(hand, playout)), {
                        Compare: (x_6, y_6) => comparePrimitives(x_6, y_6),
                    });
                }
                default: {
                    const rank = action.fields[0] | 0;
                    const card = Card_$ctor_Z106B7333(rank, trump);
                    if (!contains(card, AbstractPlayoutModule_legalPlays(hand, playout), {
                        Equals: (x, y) => equals(x, y),
                        GetHashCode: (x) => safeHash(x),
                    })) {
                        debugger;
                    }
                    return card;
                }
            }
        }
        case 1: {
            throw (new Error("Unexpected"));
        }
    }
}

export const FollowActionModule_layout = SpanLayout_combine([SpanLayout_ofLength(1), SpanLayout_ofLength(1)]);

export function FollowActionModule_copyTo(span, action) {
    if (!(Span$1__get_Length(span) === FollowActionModule_layout.Length)) {
        debugger;
    }
    const toChar = (gamePoints) => {
        if (!((gamePoints >= 0) ? (gamePoints <= 10) : false)) {
            debugger;
        }
        if (gamePoints === 10) {
            return "T";
        }
        else {
            return Char_fromDigit(gamePoints);
        }
    };
    const slice0 = SpanLayout$1__Slice_309E3581(FollowActionModule_layout, 0, span);
    const slice1 = SpanLayout$1__Slice_309E3581(FollowActionModule_layout, 1, span);
    switch (action.tag) {
        case 1: {
            Span$1__Fill_2B595(slice0, "W");
            Span$1__Fill_2B595(slice1, "x");
            break;
        }
        case 2: {
            Span$1__Fill_2B595(slice0, "L");
            Span$1__Fill_2B595(slice1, "x");
            break;
        }
        case 3: {
            const gamePoints_1 = action.fields[0] | 0;
            Span$1__Fill_2B595(slice0, "w");
            Span$1__Fill_2B595(slice1, toChar(gamePoints_1));
            break;
        }
        case 4: {
            const gamePoints_2 = action.fields[0] | 0;
            Span$1__Fill_2B595(slice0, "l");
            Span$1__Fill_2B595(slice1, toChar(gamePoints_2));
            break;
        }
        case 5: {
            Span$1__Fill_2B595(slice0, "N");
            Span$1__Fill_2B595(slice1, ".");
            break;
        }
        case 6: {
            Span$1__Fill_2B595(slice0, "G");
            Span$1__Fill_2B595(slice1, ".");
            break;
        }
        case 7: {
            Span$1__Fill_2B595(slice0, "D");
            Span$1__Fill_2B595(slice1, ".");
            break;
        }
        default: {
            const rank = action.fields[0] | 0;
            Span$1__Fill_2B595(slice0, "T");
            Span$1__Fill_2B595(slice1, PlayingCards_Rank__Rank_get_Char(rank));
        }
    }
}

