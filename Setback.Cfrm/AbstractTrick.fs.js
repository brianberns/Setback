import { compare, max, comparePrimitives, min } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Util.js";
import { Record } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Types.js";
import { option_type, record_type, bool_type, enum_type, int32_type } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Reflection.js";
import { SpanLayout$1__Slice_309E3581, SpanLayout_ofLength, SpanLayout_combine } from "./SpanLayout.fs.js";
import { Char_fromDigit, Span$1__Slice_Z524259A4, ImmutableArray$1__Item_Z524259A4, ImmutableArrayBuilder$1__ToImmutable, ImmutableArrayBuilder$1__Add_2B595, ImmutableArrayBuilder$1__AddRange_BB573A, ImmutableArray_CreateBuilder_Z524259A4, ImmutableArray$1_get_Empty, ImmutableArray$1__get_Length, ImmutableArray$1$reflection, Span$1__Fill_2B595, Span$1__get_Length } from "./Prelude.fs.js";
import { PlayingCards_Rank__Rank_get_GamePoints } from "../Setback/Rank.fs.js";
import { map, defaultArg } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Option.js";
import { PlayingCards_Rank__Rank_get_Char } from "../PlayingCards/Rank.fs.js";
import { Card$reflection } from "../PlayingCards/Card.fs.js";
import { SeatModule_numSeats } from "../Setback/Seat.fs.js";
import { replicate } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Array.js";

export function Rank_lower(rankOptA, rankOptB) {
    const matchValue = [rankOptA, rankOptB];
    let pattern_matching_result, rankA, rankB;
    if (matchValue[0] != null) {
        if (matchValue[1] != null) {
            pattern_matching_result = 0;
            rankA = matchValue[0];
            rankB = matchValue[1];
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
            return min((x, y) => comparePrimitives(x, y), rankA, rankB);
        }
        case 1: {
            return max((x_1, y_1) => compare(x_1, y_1), rankOptA, rankOptB);
        }
    }
}

export class AbstractTrickPlay extends Record {
    constructor(Rank, IsTrump) {
        super();
        this.Rank = (Rank | 0);
        this.IsTrump = IsTrump;
    }
}

export function AbstractTrickPlay$reflection() {
    return record_type("Setback.Cfrm.AbstractTrickPlay", [], AbstractTrickPlay, () => [["Rank", enum_type("PlayingCards.Rank", int32_type, [["Two", 2], ["Three", 3], ["Four", 4], ["Five", 5], ["Six", 6], ["Seven", 7], ["Eight", 8], ["Nine", 9], ["Ten", 10], ["Jack", 11], ["Queen", 12], ["King", 13], ["Ace", 14]])], ["IsTrump", bool_type]]);
}

export function AbstractTrickPlayModule_create(trump, card) {
    return new AbstractTrickPlay(card.Rank, card.Suit === trump);
}

export const AbstractTrickPlayModule_layout = SpanLayout_combine([SpanLayout_ofLength(1), SpanLayout_ofLength(1)]);

export function AbstractTrickPlayModule_copyTo(span, lowTrumpRankOpt, play) {
    if (!(Span$1__get_Length(span) === AbstractTrickPlayModule_layout.Length)) {
        debugger;
    }
    let cRank;
    const isSignificant = (PlayingCards_Rank__Rank_get_GamePoints(play.Rank) > 0) ? true : ((play.IsTrump ? (play.Rank <= 5) : false) ? defaultArg(map((lowTrumpRank) => (play.Rank <= lowTrumpRank), lowTrumpRankOpt), false) : false);
    cRank = (isSignificant ? PlayingCards_Rank__Rank_get_Char(play.Rank) : "x");
    Span$1__Fill_2B595(SpanLayout$1__Slice_309E3581(AbstractTrickPlayModule_layout, 0, span), cRank);
    const cSuit = play.IsTrump ? "t" : "x";
    Span$1__Fill_2B595(SpanLayout$1__Slice_309E3581(AbstractTrickPlayModule_layout, 1, span), cSuit);
}

export class AbstractHighPlay extends Record {
    constructor(PlayerIndex, Play) {
        super();
        this.PlayerIndex = (PlayerIndex | 0);
        this.Play = Play;
    }
}

export function AbstractHighPlay$reflection() {
    return record_type("Setback.Cfrm.AbstractHighPlay", [], AbstractHighPlay, () => [["PlayerIndex", int32_type], ["Play", Card$reflection()]]);
}

export function AbstractHighPlayModule_create(playerIdx, card) {
    if (!((playerIdx >= 0) ? (playerIdx < SeatModule_numSeats) : false)) {
        debugger;
    }
    return new AbstractHighPlay(playerIdx, card);
}

export class AbstractTrick extends Record {
    constructor(LeaderIndex, Plays, SuitLedOpt, HighPlayOpt, LowTrumpRankOpt) {
        super();
        this.LeaderIndex = (LeaderIndex | 0);
        this.Plays = Plays;
        this.SuitLedOpt = SuitLedOpt;
        this.HighPlayOpt = HighPlayOpt;
        this.LowTrumpRankOpt = LowTrumpRankOpt;
    }
}

export function AbstractTrick$reflection() {
    return record_type("Setback.Cfrm.AbstractTrick", [], AbstractTrick, () => [["LeaderIndex", int32_type], ["Plays", ImmutableArray$1$reflection(AbstractTrickPlay$reflection())], ["SuitLedOpt", option_type(enum_type("PlayingCards.Suit", int32_type, [["Clubs", 0], ["Diamonds", 1], ["Hearts", 2], ["Spades", 3]]))], ["HighPlayOpt", option_type(AbstractHighPlay$reflection())], ["LowTrumpRankOpt", option_type(enum_type("PlayingCards.Rank", int32_type, [["Two", 2], ["Three", 3], ["Four", 4], ["Five", 5], ["Six", 6], ["Seven", 7], ["Eight", 8], ["Nine", 9], ["Ten", 10], ["Jack", 11], ["Queen", 12], ["King", 13], ["Ace", 14]]))]]);
}

export function AbstractTrick__get_NumPlays(trick) {
    return ImmutableArray$1__get_Length(trick.Plays);
}

export function AbstractTrickModule_create(lowTrumpRankOpt, leaderIdx) {
    if (!((leaderIdx >= 0) ? (leaderIdx < SeatModule_numSeats) : false)) {
        debugger;
    }
    return new AbstractTrick(leaderIdx, ImmutableArray$1_get_Empty(), void 0, void 0, lowTrumpRankOpt);
}

export function AbstractTrickModule_isComplete(trick) {
    if (!((AbstractTrick__get_NumPlays(trick) >= 0) ? (AbstractTrick__get_NumPlays(trick) <= SeatModule_numSeats) : false)) {
        debugger;
    }
    if (!(ImmutableArray$1__get_Length(trick.Plays) === AbstractTrick__get_NumPlays(trick))) {
        debugger;
    }
    return AbstractTrick__get_NumPlays(trick) === SeatModule_numSeats;
}

export function AbstractTrickModule_currentPlayerIndex(trick) {
    return (AbstractTrick__get_NumPlays(trick) + trick.LeaderIndex) % SeatModule_numSeats;
}

export function AbstractTrickModule_addPlay(trump, card, trick) {
    let play, builder, isHighPlay, matchValue, highPlay, highCard, iPlayer;
    if (!(!AbstractTrickModule_isComplete(trick))) {
        debugger;
    }
    return new AbstractTrick(trick.LeaderIndex, (play = AbstractTrickPlayModule_create(trump, card), (builder = ImmutableArray_CreateBuilder_Z524259A4(AbstractTrick__get_NumPlays(trick) + 1), (ImmutableArrayBuilder$1__AddRange_BB573A(builder, trick.Plays), (ImmutableArrayBuilder$1__Add_2B595(builder, play), ImmutableArrayBuilder$1__ToImmutable(builder))))), defaultArg(trick.SuitLedOpt, card.Suit), (isHighPlay = ((matchValue = trick.HighPlayOpt, (matchValue != null) ? ((highPlay = matchValue, (highCard = highPlay.Play, ((card.Suit === highCard.Suit) ? (card.Rank > highCard.Rank) : false) ? true : ((card.Suit === trump) ? (highCard.Suit !== trump) : false)))) : true)), isHighPlay ? ((iPlayer = (AbstractTrickModule_currentPlayerIndex(trick) | 0), AbstractHighPlayModule_create(iPlayer, card))) : trick.HighPlayOpt), (card.Suit === trump) ? defaultArg(map((e2) => min((x, y) => comparePrimitives(x, y), card.Rank, e2), trick.LowTrumpRankOpt), card.Rank) : trick.LowTrumpRankOpt);
}

export function AbstractTrickModule_highPlayerIndex(trick) {
    if (!(AbstractTrick__get_NumPlays(trick) > 0)) {
        debugger;
    }
    const matchValue = trick.HighPlayOpt;
    if (matchValue == null) {
        throw (new Error("Unexpected"));
    }
    else {
        const highPlay = matchValue;
        return highPlay.PlayerIndex | 0;
    }
}

const AbstractTrickModule_Plays_layout = SpanLayout_combine(replicate(SeatModule_numSeats - 1, AbstractTrickPlayModule_layout));

function AbstractTrickModule_Plays_copyTo(span, lowTrumpRankOpt, plays) {
    for (let iPlay = 0; iPlay <= (ImmutableArray$1__get_Length(plays) - 1); iPlay++) {
        const play = ImmutableArray$1__Item_Z524259A4(plays, iPlay);
        const slice = SpanLayout$1__Slice_309E3581(AbstractTrickModule_Plays_layout, iPlay, span);
        AbstractTrickPlayModule_copyTo(slice, lowTrumpRankOpt, play);
    }
    Span$1__Fill_2B595(Span$1__Slice_Z524259A4(span, ImmutableArray$1__get_Length(plays) * AbstractTrickPlayModule_layout.Length), ".");
}

export const AbstractTrickModule_layout = SpanLayout_combine([AbstractTrickModule_Plays_layout, SpanLayout_ofLength(1)]);

export function AbstractTrickModule_copyTo(span, handLowTrumpRankOpt, trick) {
    if (!(!AbstractTrickModule_isComplete(trick))) {
        debugger;
    }
    if (!(Span$1__get_Length(span) === AbstractTrickModule_layout.Length)) {
        debugger;
    }
    const slice = SpanLayout$1__Slice_309E3581(AbstractTrickModule_layout, 0, span);
    const lowTrumpRankOpt = Rank_lower(trick.LowTrumpRankOpt, handLowTrumpRankOpt);
    AbstractTrickModule_Plays_copyTo(slice, lowTrumpRankOpt, trick.Plays);
    const cWinner = defaultArg(map((highPlay) => {
        const iTrickPlayer = (((highPlay.PlayerIndex - trick.LeaderIndex) + SeatModule_numSeats) % SeatModule_numSeats) | 0;
        return Char_fromDigit(iTrickPlayer);
    }, trick.HighPlayOpt), ".");
    Span$1__Fill_2B595(SpanLayout$1__Slice_309E3581(AbstractTrickModule_layout, 1, span), cWinner);
}

