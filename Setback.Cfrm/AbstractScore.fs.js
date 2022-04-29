import { Union } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Types.js";
import { union_type, array_type, int32_type } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Reflection.js";
import { initialize, replicate, map2 } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Array.js";
import { numTeams } from "../Setback/Setback.fs.js";
import { singleton, collect, delay, toArray } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Seq.js";
import { rangeDouble } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Range.js";

export class AbstractScore extends Union {
    constructor(tag, ...fields) {
        super();
        this.tag = (tag | 0);
        this.fields = fields;
    }
    cases() {
        return ["AbstractScore"];
    }
}

export function AbstractScore$reflection() {
    return union_type("Setback.Cfrm.AbstractScore", [], AbstractScore, () => [[["Item", array_type(int32_type)]]]);
}

export function AbstractScore__Item_Z524259A4(this$, teamIdx) {
    const points = this$.fields[0];
    return points[teamIdx] | 0;
}

export function AbstractScore_op_Addition_44B33CC0(_arg1, _arg2) {
    const pointsA = _arg1.fields[0];
    const pointsB = _arg2.fields[0];
    return new AbstractScore(0, map2((x, y) => (x + y), pointsA, pointsB, Int32Array));
}

export const AbstractScoreModule_zero = new AbstractScore(0, replicate(numTeams, 0, Int32Array));

export function AbstractScoreModule_forTeam(teamIdx, value) {
    if (!((teamIdx >= 0) ? (teamIdx < numTeams) : false)) {
        debugger;
    }
    return new AbstractScore(0, initialize(numTeams, (iTeam) => ((iTeam === teamIdx) ? value : 0), Int32Array));
}

export function AbstractScoreModule_shift(teamIdx, _arg1) {
    const points = _arg1.fields[0];
    return new AbstractScore(0, toArray(delay(() => collect((iTeam) => {
        const iShift = ((teamIdx + iTeam) % points.length) | 0;
        return singleton(points[iShift]);
    }, rangeDouble(0, 1, points.length - 1)))));
}

export function AbstractScoreModule_delta(iTeam, score) {
    if (!(numTeams === 2)) {
        debugger;
    }
    if (!((iTeam >= 0) ? (iTeam < numTeams) : false)) {
        debugger;
    }
    return (AbstractScore__Item_Z524259A4(score, iTeam) - AbstractScore__Item_Z524259A4(score, 1 - iTeam)) | 0;
}

