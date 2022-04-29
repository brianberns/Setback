import { Record } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Types.js";
import { record_type, array_type, tuple_type, int32_type } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Reflection.js";
import { Span$1__Slice_Z37302880 } from "./Prelude.fs.js";
import { last, scan } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Array.js";
import { zip, toArray } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Seq.js";

export class SpanLayout$1 extends Record {
    constructor(Length, ChildPositions) {
        super();
        this.Length = (Length | 0);
        this.ChildPositions = ChildPositions;
    }
}

export function SpanLayout$1$reflection(gen0) {
    return record_type("Setback.Cfrm.SpanLayout`1", [gen0], SpanLayout$1, () => [["Length", int32_type], ["ChildPositions", array_type(tuple_type(int32_type, SpanLayout$1$reflection(gen0)))]]);
}

export function SpanLayout$1__Slice_309E3581(layout, iChild, span) {
    const patternInput = layout.ChildPositions[iChild];
    const start = patternInput[0] | 0;
    const child = patternInput[1];
    return Span$1__Slice_Z37302880(span, start, child.Length);
}

export function SpanLayout_ofLength(length) {
    return new SpanLayout$1(length, new Array(0));
}

export function SpanLayout_combine(children) {
    const positions = scan((len, child) => (len + child.Length), 0, children, Int32Array);
    if (!(positions.length === (children.length + 1))) {
        debugger;
    }
    return new SpanLayout$1(last(positions), toArray(zip(positions, children)));
}

