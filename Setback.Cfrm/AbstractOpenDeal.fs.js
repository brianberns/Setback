import { Record } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Types.js";
import { AbstractClosedDealModule_addPlay, AbstractClosedDealModule_addBid, AbstractClosedDealModule_getActions, AbstractClosedDealModule_currentPlayerIndex, AbstractClosedDealModule_isComplete, AbstractClosedDealModule_initial, AbstractClosedDeal$reflection } from "./AbstractClosedDeal.fs.js";
import { Card$reflection } from "../PlayingCards/Card.fs.js";
import { record_type, bool_type, tuple_type, option_type, enum_type, int32_type, class_type } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Reflection.js";
import { ImmutableArray$1__SetItem_6570C449, Seq_tryMin, ImmutableArray$1__Item_Z524259A4, ImmutableArray_CreateRange_404FCA0C, ImmutableArray$1$reflection } from "./Prelude.fs.js";
import { contains, min, indexed, where, concat, length, fold, singleton, delay, toArray, mapIndexed, take, collect, sumBy, replicate, map } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Seq.js";
import { FSharpSet__Remove, ofSeq } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Set.js";
import { ofSeq as ofSeq_1, toSeq, FSharpMap__get_Item } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Map.js";
import { safeHash, compareArrays, sign, equals, comparePrimitives, numberHash, compare } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Util.js";
import { SeatModule_incr, SeatModule_numSeats, SeatModule_cycle } from "../Setback/Seat.fs.js";
import { PlayingCards_Rank__Rank_get_GamePoints } from "../Setback/Rank.fs.js";
import { numCardsPerDeal, numTeams, numCardsPerHand } from "../Setback/Setback.fs.js";
import { groupBy } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Seq2.js";
import { map as map_1 } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Option.js";
import { max, sum } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Array.js";
import { AbstractScore_op_Addition_44B33CC0, AbstractScoreModule_zero, AbstractScore, AbstractScoreModule_delta } from "./AbstractScore.fs.js";
import { rangeDouble } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Range.js";
import { AbstractHighBidModule_finalizeDealScore } from "./AbstractHighBid.fs.js";
import { AbstractTrick__get_NumPlays } from "./AbstractTrick.fs.js";
import { PlayActionModule_getPlay } from "./PlayAction.fs.js";
import { BidActionModule_getBid } from "./BidAction.fs.js";

export class AbstractOpenDeal extends Record {
    constructor(ClosedDeal, UnplayedCards, HandLowTrumpRankOpts, HighTrumpOpt, LowTrumpOpt, JackTrumpOpt, TotalGamePoints) {
        super();
        this.ClosedDeal = ClosedDeal;
        this.UnplayedCards = UnplayedCards;
        this.HandLowTrumpRankOpts = HandLowTrumpRankOpts;
        this.HighTrumpOpt = HighTrumpOpt;
        this.LowTrumpOpt = LowTrumpOpt;
        this.JackTrumpOpt = JackTrumpOpt;
        this.TotalGamePoints = (TotalGamePoints | 0);
    }
}

export function AbstractOpenDeal$reflection() {
    return record_type("Setback.Cfrm.AbstractOpenDeal", [], AbstractOpenDeal, () => [["ClosedDeal", AbstractClosedDeal$reflection()], ["UnplayedCards", ImmutableArray$1$reflection(class_type("Microsoft.FSharp.Collections.FSharpSet`1", [Card$reflection()]))], ["HandLowTrumpRankOpts", ImmutableArray$1$reflection(option_type(enum_type("PlayingCards.Rank", int32_type, [["Two", 2], ["Three", 3], ["Four", 4], ["Five", 5], ["Six", 6], ["Seven", 7], ["Eight", 8], ["Nine", 9], ["Ten", 10], ["Jack", 11], ["Queen", 12], ["King", 13], ["Ace", 14]])))], ["HighTrumpOpt", option_type(tuple_type(enum_type("PlayingCards.Rank", int32_type, [["Two", 2], ["Three", 3], ["Four", 4], ["Five", 5], ["Six", 6], ["Seven", 7], ["Eight", 8], ["Nine", 9], ["Ten", 10], ["Jack", 11], ["Queen", 12], ["King", 13], ["Ace", 14]]), int32_type))], ["LowTrumpOpt", option_type(enum_type("PlayingCards.Rank", int32_type, [["Two", 2], ["Three", 3], ["Four", 4], ["Five", 5], ["Six", 6], ["Seven", 7], ["Eight", 8], ["Nine", 9], ["Ten", 10], ["Jack", 11], ["Queen", 12], ["King", 13], ["Ace", 14]]))], ["JackTrumpOpt", option_type(bool_type)], ["TotalGamePoints", int32_type]]);
}

export function AbstractOpenDealModule_fromHands(dealer, hands) {
    return new AbstractOpenDeal(AbstractClosedDealModule_initial, ImmutableArray_CreateRange_404FCA0C(map((seat_1) => ofSeq(FSharpMap__get_Item(hands, seat_1), {
        Compare: (x, y) => compare(x, y),
    }), SeatModule_cycle(dealer))), ImmutableArray_CreateRange_404FCA0C(replicate(SeatModule_numSeats, void 0)), void 0, void 0, void 0, sumBy((card) => PlayingCards_Rank__Rank_get_GamePoints(card.Rank), collect((tuple) => tuple[1], toSeq(hands)), {
        GetZero: () => 0,
        Add: (x_1, y_1) => (x_1 + y_1),
    }));
}

export function AbstractOpenDealModule_fromDeck(dealer, deck) {
    const numCardsPerGroup = 3;
    if (!((numCardsPerHand % numCardsPerGroup) === 0)) {
        debugger;
    }
    return AbstractOpenDealModule_fromHands(dealer, ofSeq_1(map((tupledArg_1) => {
        const seat_1 = tupledArg_1[0] | 0;
        const pairs = tupledArg_1[1];
        const hand = take(numCardsPerHand, map((tuple) => tuple[1], pairs));
        return [seat_1, hand];
    }, groupBy((tupledArg) => {
        const iCard_1 = tupledArg[0] | 0;
        return SeatModule_incr((~(~(iCard_1 / numCardsPerGroup))) + 1, dealer) | 0;
    }, mapIndexed((iCard, card) => [iCard, card], deck.Cards), {
        Equals: (x, y) => (x === y),
        GetHashCode: (x) => numberHash(x),
    })), {
        Compare: (x_1, y_1) => comparePrimitives(x_1, y_1),
    }));
}

export function AbstractOpenDealModule_isComplete(deal) {
    return AbstractClosedDealModule_isComplete(deal.ClosedDeal);
}

export function AbstractOpenDealModule_isExhausted(deal) {
    let result;
    const matchValue = deal.ClosedDeal.PlayoutOpt;
    if (matchValue != null) {
        const playout = matchValue;
        if (playout.TrumpOpt != null) {
            const history = playout.History;
            const lowTakenOpt = map_1((tuple) => tuple[0], history.LowTakenOpt);
            if (equals(lowTakenOpt, deal.LowTrumpOpt)) {
                const jackTakenOpt = history.JackTakenOpt != null;
                if (equals(jackTakenOpt, deal.JackTrumpOpt)) {
                    let gameUntaken;
                    let gameTaken;
                    const points = history.GameScore.fields[0];
                    gameTaken = sum(points, {
                        GetZero: () => 0,
                        Add: (x, y) => (x + y),
                    });
                    gameUntaken = (deal.TotalGamePoints - gameTaken);
                    if (!(gameUntaken >= 0)) {
                        debugger;
                    }
                    if (gameUntaken === 0) {
                        result = true;
                    }
                    else {
                        const delta = Math.abs(AbstractScoreModule_delta(0, history.GameScore)) | 0;
                        if (!(delta >= 0)) {
                            debugger;
                        }
                        result = (gameUntaken < delta);
                    }
                }
                else {
                    result = false;
                }
            }
            else {
                result = false;
            }
        }
        else {
            result = false;
        }
    }
    else {
        result = AbstractOpenDealModule_isComplete(deal);
    }
    if (!(result ? true : (!AbstractOpenDealModule_isComplete(deal)))) {
        debugger;
    }
    return result;
}

function AbstractOpenDealModule_dealScoreRaw(deal) {
    const toScore = (teamOpt) => {
        let team;
        if (!((teamOpt == null) ? true : ((team = (teamOpt | 0), (team >= 0) ? (team < numTeams) : false)))) {
            debugger;
        }
        return new AbstractScore(0, toArray(delay(() => collect((iTeam) => (equals(teamOpt, iTeam) ? singleton(1) : singleton(0)), rangeDouble(0, 1, numTeams - 1)))));
    };
    const matchValue = deal.ClosedDeal.PlayoutOpt;
    if (matchValue == null) {
        return AbstractScoreModule_zero;
    }
    else {
        const playout = matchValue;
        const history = playout.History;
        let gameTakenOpt;
        const deltaSign = sign(AbstractScoreModule_delta(0, history.GameScore)) | 0;
        switch (deltaSign) {
            case -1: {
                gameTakenOpt = 1;
                break;
            }
            case 0: {
                gameTakenOpt = (void 0);
                break;
            }
            case 1: {
                gameTakenOpt = 0;
                break;
            }
            default: {
                throw (new Error("Unexpected"));
            }
        }
        const scores = map(toScore, [map_1((tuple) => tuple[1], deal.HighTrumpOpt), map_1((tuple_1) => tuple_1[1], history.LowTakenOpt), history.JackTakenOpt, gameTakenOpt]);
        return fold((x, y) => AbstractScore_op_Addition_44B33CC0(x, y), AbstractScoreModule_zero, scores);
    }
}

export function AbstractOpenDealModule_dealScore(deal) {
    const rawScore = AbstractOpenDealModule_dealScoreRaw(deal);
    return AbstractHighBidModule_finalizeDealScore(rawScore, deal.ClosedDeal.Auction.HighBid);
}

export function AbstractOpenDealModule_currentPlayerIndex(deal) {
    return AbstractClosedDealModule_currentPlayerIndex(deal.ClosedDeal);
}

export function AbstractOpenDealModule_currentHand(deal) {
    const iPlayer = AbstractOpenDealModule_currentPlayerIndex(deal) | 0;
    return ImmutableArray$1__Item_Z524259A4(deal.UnplayedCards, iPlayer);
}

export function AbstractOpenDealModule_currentLowTrumpRankOpt(deal) {
    const iPlayer = AbstractOpenDealModule_currentPlayerIndex(deal) | 0;
    return ImmutableArray$1__Item_Z524259A4(deal.HandLowTrumpRankOpts, iPlayer);
}

export function AbstractOpenDealModule_getActions(deal) {
    if (AbstractOpenDealModule_isComplete(deal)) {
        return new Array(0);
    }
    else {
        const hand = AbstractOpenDealModule_currentHand(deal);
        const handLowTrumpRankOpt = AbstractOpenDealModule_currentLowTrumpRankOpt(deal);
        return AbstractClosedDealModule_getActions(hand, handLowTrumpRankOpt, deal.ClosedDeal);
    }
}

export function AbstractOpenDealModule_addBid(bid, deal) {
    return new AbstractOpenDeal(AbstractClosedDealModule_addBid(bid, deal.ClosedDeal), deal.UnplayedCards, deal.HandLowTrumpRankOpts, deal.HighTrumpOpt, deal.LowTrumpOpt, deal.JackTrumpOpt, deal.TotalGamePoints);
}

export function AbstractOpenDealModule_addPlay(card, deal) {
    let iPlayer_1, hand_1, hand$0027;
    let patternInput;
    const matchValue = deal.ClosedDeal.PlayoutOpt;
    if (matchValue == null) {
        throw (new Error("Unexpected"));
    }
    else {
        const playout = matchValue;
        if (playout.TrumpOpt != null) {
            if (!(deal.HighTrumpOpt != null)) {
                debugger;
            }
            if (!(deal.LowTrumpOpt != null)) {
                debugger;
            }
            if (!(deal.JackTrumpOpt != null)) {
                debugger;
            }
            patternInput = [deal.HighTrumpOpt, deal.LowTrumpOpt, deal.JackTrumpOpt, deal.HandLowTrumpRankOpts];
        }
        else {
            if (!(playout.History.NumTricksCompleted === 0)) {
                debugger;
            }
            if (!(AbstractTrick__get_NumPlays(playout.CurrentTrick) === 0)) {
                debugger;
            }
            if (!(deal.HighTrumpOpt == null)) {
                debugger;
            }
            if (!(deal.LowTrumpOpt == null)) {
                debugger;
            }
            if (!(deal.JackTrumpOpt == null)) {
                debugger;
            }
            if (!(length(concat(deal.UnplayedCards)) === numCardsPerDeal)) {
                debugger;
            }
            const trump = card.Suit | 0;
            const rankTeams = toArray(collect((tupledArg) => {
                const iPlayer = tupledArg[0] | 0;
                const cards = tupledArg[1];
                const iTeam = (iPlayer % numTeams) | 0;
                return map((card_1) => [card_1.Rank, iTeam], where((c) => (c.Suit === trump), cards));
            }, indexed(deal.UnplayedCards)));
            const ranks = map((tuple) => tuple[0], rankTeams);
            const highTrumpOpt = max(rankTeams, {
                Compare: (x, y) => compareArrays(x, y),
            });
            const lowTrumpOpt = min(ranks, {
                Compare: (x_1, y_1) => comparePrimitives(x_1, y_1),
            });
            const jackTrumpOpt = contains(11, ranks, {
                Equals: (x_2, y_2) => (x_2 === y_2),
                GetHashCode: (x_2) => numberHash(x_2),
            });
            const handLowTrumpRankOpts = ImmutableArray_CreateRange_404FCA0C(map((hand) => Seq_tryMin(map((card_3) => card_3.Rank, where((card_2) => (card_2.Suit === trump), hand))), deal.UnplayedCards));
            patternInput = [highTrumpOpt, lowTrumpOpt, jackTrumpOpt, handLowTrumpRankOpts];
        }
    }
    const lowTrumpOpt_1 = patternInput[1];
    const jackTrumpOpt_1 = patternInput[2];
    const highTrumpOpt_1 = patternInput[0];
    const handLowTrumpRankOpts_1 = patternInput[3];
    return new AbstractOpenDeal(AbstractClosedDealModule_addPlay(card, deal.ClosedDeal), (iPlayer_1 = (AbstractOpenDealModule_currentPlayerIndex(deal) | 0), (hand_1 = ImmutableArray$1__Item_Z524259A4(deal.UnplayedCards, iPlayer_1), ((!contains(card, hand_1, {
        Equals: (x_3, y_3) => equals(x_3, y_3),
        GetHashCode: (x_3) => safeHash(x_3),
    })) ? (() => {
        debugger;
    })() : null, (hand$0027 = FSharpSet__Remove(hand_1, card), ImmutableArray$1__SetItem_6570C449(deal.UnplayedCards, iPlayer_1, hand$0027))))), handLowTrumpRankOpts_1, highTrumpOpt_1, lowTrumpOpt_1, jackTrumpOpt_1, deal.TotalGamePoints);
}

export function AbstractOpenDealModule_addAction(action, deal) {
    if (action.tag === 1) {
        const playAction = action.fields[0];
        let playout_1;
        const matchValue = deal.ClosedDeal.PlayoutOpt;
        if (matchValue == null) {
            throw (new Error("Unexpected"));
        }
        else {
            const playout = matchValue;
            playout_1 = playout;
        }
        let card;
        const hand = AbstractOpenDealModule_currentHand(deal);
        const handLowTrumpRankOpt = AbstractOpenDealModule_currentLowTrumpRankOpt(deal);
        card = PlayActionModule_getPlay(hand, handLowTrumpRankOpt, playout_1, playAction);
        return AbstractOpenDealModule_addPlay(card, deal);
    }
    else {
        const bidAction = action.fields[0];
        const bid = BidActionModule_getBid(deal.ClosedDeal.Auction, bidAction) | 0;
        return AbstractOpenDealModule_addBid(bid, deal);
    }
}

