import { Record } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Types.js";
import { record_type, class_type, option_type, tuple_type, enum_type, bool_type, int32_type } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Reflection.js";
import { AbstractScore_op_Addition_44B33CC0, AbstractScore, AbstractScoreModule_zero, AbstractScore$reflection } from "./AbstractScore.fs.js";
import { FSharpSet__Add, empty } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Set.js";
import { equals, compareArrays, min, comparePrimitives } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Util.js";
import { numTeams, numCardsPerHand } from "../Setback/Setback.fs.js";
import { toArray } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Seq.js";
import { rangeDouble } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Range.js";
import { SeatModule_numSeats } from "../Setback/Seat.fs.js";
import { AbstractTrickModule_highPlayerIndex, AbstractTrickModule_isComplete } from "./AbstractTrick.fs.js";
import { Option_unzip, ImmutableArray$1__Item_Z524259A4 } from "./Prelude.fs.js";
import { initialize, fold } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Array.js";
import { PlayingCards_Rank__Rank_get_GamePoints } from "../Setback/Rank.fs.js";

export class AbstractPlayoutHistory extends Record {
    constructor(NumTricksCompleted, HighEstablished, LowTakenOpt, JackTakenOpt, GameScore, TrumpVoids) {
        super();
        this.NumTricksCompleted = (NumTricksCompleted | 0);
        this.HighEstablished = HighEstablished;
        this.LowTakenOpt = LowTakenOpt;
        this.JackTakenOpt = JackTakenOpt;
        this.GameScore = GameScore;
        this.TrumpVoids = TrumpVoids;
    }
}

export function AbstractPlayoutHistory$reflection() {
    return record_type("Setback.Cfrm.AbstractPlayoutHistory", [], AbstractPlayoutHistory, () => [["NumTricksCompleted", int32_type], ["HighEstablished", bool_type], ["LowTakenOpt", option_type(tuple_type(enum_type("PlayingCards.Rank", int32_type, [["Two", 2], ["Three", 3], ["Four", 4], ["Five", 5], ["Six", 6], ["Seven", 7], ["Eight", 8], ["Nine", 9], ["Ten", 10], ["Jack", 11], ["Queen", 12], ["King", 13], ["Ace", 14]]), int32_type))], ["JackTakenOpt", option_type(int32_type)], ["GameScore", AbstractScore$reflection()], ["TrumpVoids", class_type("Microsoft.FSharp.Collections.FSharpSet`1", [int32_type])]]);
}

export const AbstractPlayoutHistoryModule_empty = new AbstractPlayoutHistory(0, false, void 0, void 0, AbstractScoreModule_zero, empty({
    Compare: (x, y) => comparePrimitives(x, y),
}));

export function AbstractPlayoutHistoryModule_isComplete(history) {
    if (!((history.NumTricksCompleted >= 0) ? (history.NumTricksCompleted <= numCardsPerHand) : false)) {
        debugger;
    }
    return history.NumTricksCompleted === numCardsPerHand;
}

const AbstractPlayoutHistoryModule_playIndexes = toArray(rangeDouble(0, 1, SeatModule_numSeats - 1));

export function AbstractPlayoutHistoryModule_addTrick(trick, history) {
    if (!AbstractTrickModule_isComplete(trick)) {
        debugger;
    }
    if (!(!AbstractPlayoutHistoryModule_isComplete(history))) {
        debugger;
    }
    const firstPlay = ImmutableArray$1__Item_Z524259A4(trick.Plays, 0);
    let iTrickWinnerTeam;
    const iPlayer = AbstractTrickModule_highPlayerIndex(trick) | 0;
    iTrickWinnerTeam = (iPlayer % numTeams);
    return fold((acc, iPlay) => {
        let matchValue, lowTaken, taken, patternInput, rankOpt, iTeamOpt, iPlayer_1;
        const play = ImmutableArray$1__Item_Z524259A4(trick.Plays, iPlay);
        const takenOpt = play.IsTrump ? [play.Rank, iTrickWinnerTeam] : (void 0);
        const gameScore = new AbstractScore(0, initialize(numTeams, (it) => ((it === iTrickWinnerTeam) ? PlayingCards_Rank__Rank_get_GamePoints(play.Rank) : 0), Int32Array));
        const isVoidTrump = firstPlay.IsTrump ? (!play.IsTrump) : false;
        return new AbstractPlayoutHistory(history.NumTricksCompleted + 1, (history.NumTricksCompleted === 0) ? (((!(!history.HighEstablished)) ? (() => {
            debugger;
        })() : null, firstPlay.Rank >= 11)) : history.HighEstablished, (matchValue = [acc.LowTakenOpt, takenOpt], (matchValue[0] != null) ? ((matchValue[1] != null) ? ((lowTaken = matchValue[0], (taken = matchValue[1], ((!(lowTaken[0] !== taken[0])) ? (() => {
            debugger;
        })() : null, min((x, y) => compareArrays(x, y), lowTaken, taken))))) : acc.LowTakenOpt) : takenOpt), (patternInput = Option_unzip(takenOpt), (rankOpt = patternInput[0], (iTeamOpt = patternInput[1], equals(rankOpt, 11) ? (((!(acc.JackTakenOpt == null)) ? (() => {
            debugger;
        })() : null, iTeamOpt)) : acc.JackTakenOpt))), AbstractScore_op_Addition_44B33CC0(acc.GameScore, gameScore), isVoidTrump ? ((iPlayer_1 = (((iPlay + trick.LeaderIndex) % SeatModule_numSeats) | 0), FSharpSet__Add(acc.TrumpVoids, iPlayer_1))) : acc.TrumpVoids);
    }, history, AbstractPlayoutHistoryModule_playIndexes);
}

