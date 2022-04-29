import { toIterator, stringHash, compare, min, getEnumerator } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Util.js";
import { some } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Option.js";
import { union_type, lambda_type, unit_type, class_type } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Reflection.js";
import { Union } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Types.js";
import { addRangeInPlace, replicate } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Array.js";
import { toArray, contains } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Seq.js";
import { Array_clone } from "../PlayingCards/Prelude.fs.js";

export function Seq_tryMin(items) {
    const e = getEnumerator(items);
    try {
        if (e["System.Collections.IEnumerator.MoveNext"]()) {
            let result = e["System.Collections.Generic.IEnumerator`1.get_Current"]();
            while (e["System.Collections.IEnumerator.MoveNext"]()) {
                result = min((x, y) => compare(x, y), e["System.Collections.Generic.IEnumerator`1.get_Current"](), result);
            }
            return some(result);
        }
        else {
            return void 0;
        }
    }
    finally {
        e.Dispose();
    }
}

export function Option_unzip(_arg1) {
    if (_arg1 == null) {
        return [void 0, void 0];
    }
    else {
        const b = _arg1[1];
        const a = _arg1[0];
        return [some(a), some(b)];
    }
}

export function Char_fromDigit(digit) {
    return "0123456789"[digit];
}

export function Char_fromHexDigit(digit) {
    return "0123456789ABCDEF"[digit];
}

export class Span$1 {
    constructor(array) {
        this.array = array;
    }
}

export function Span$1$reflection(gen0) {
    return class_type("Setback.Cfrm.Span`1", [gen0], Span$1);
}

export function Span$1_$ctor_B867673(array) {
    return new Span$1(array);
}

export function Span$1__get_Length(_) {
    return _.array.length;
}

export function Span$1__Slice_Z524259A4(_, start) {
    return Span$1_$ctor_B867673(_.array.slice(start, _.array.length));
}

export function Span$1__Slice_Z37302880(_, start, length) {
    return Span$1_$ctor_B867673(_.array.slice(start, ((start + length) - 1) + 1));
}

export function Span$1__Fill_2B595(_, item) {
    for (let i = 0; i <= _.array.length; i++) {
        _.array[i] = item;
    }
}

export class SpanAction$2 extends Union {
    constructor(tag, ...fields) {
        super();
        this.tag = (tag | 0);
        this.fields = fields;
    }
    cases() {
        return ["SpanAction"];
    }
}

export function SpanAction$2$reflection(gen0, gen1) {
    return union_type("Setback.Cfrm.SpanAction`2", [gen0, gen1], SpanAction$2, () => [[["Item", lambda_type(Span$1$reflection(gen0), lambda_type(gen1, unit_type))]]]);
}

export function System_String__String_Create_Static_43E13F6(length, state, _arg1) {
    const action = _arg1.fields[0];
    const array = replicate(length, "?");
    action(Span$1_$ctor_B867673(array), state);
    return array.join('');
}

export function System_String__String_Contains_244C7CD6(str, c) {
    return contains(c, str.split(""), {
        Equals: (x, y) => (x === y),
        GetHashCode: (x) => stringHash(x),
    });
}

export class ImmutableArray$1 {
    constructor(items) {
        this.items = items;
    }
    GetEnumerator() {
        const _ = this;
        return ImmutableArray$1__getEnumerator(_);
    }
    [Symbol.iterator]() {
        return toIterator(this.GetEnumerator());
    }
    ["System.Collections.IEnumerable.GetEnumerator"]() {
        const _ = this;
        return ImmutableArray$1__getEnumerator(_);
    }
}

export function ImmutableArray$1$reflection(gen0) {
    return class_type("Setback.Cfrm.ImmutableArray`1", [gen0], ImmutableArray$1);
}

export function ImmutableArray$1_$ctor_B867673(items) {
    return new ImmutableArray$1(items);
}

export function ImmutableArray$1_$ctor_BB573A(items) {
    return ImmutableArray$1_$ctor_B867673(toArray(items));
}

export function ImmutableArray$1_get_Empty() {
    return ImmutableArray$1_$ctor_B867673(new Array(0));
}

export function ImmutableArray$1__Item_Z524259A4(_, index) {
    return _.items[index];
}

export function ImmutableArray$1__get_Length(_) {
    return _.items.length;
}

export function ImmutableArray$1__SetItem_6570C449(_, index, item) {
    const items$0027 = Array_clone(_.items);
    items$0027[index] = item;
    return ImmutableArray$1_$ctor_B867673(items$0027);
}

function ImmutableArray$1__getEnumerator(this$) {
    return getEnumerator(this$.items);
}

export class ImmutableArrayBuilder$1 {
    constructor(n) {
        this.items = [];
    }
}

export function ImmutableArrayBuilder$1$reflection(gen0) {
    return class_type("Setback.Cfrm.ImmutableArrayBuilder`1", [gen0], ImmutableArrayBuilder$1);
}

export function ImmutableArrayBuilder$1_$ctor_Z524259A4(n) {
    return new ImmutableArrayBuilder$1(n);
}

export function ImmutableArrayBuilder$1__AddRange_BB573A(_, range) {
    addRangeInPlace(range, _.items);
}

export function ImmutableArrayBuilder$1__Add_2B595(_, item) {
    void (_.items.push(item));
}

export function ImmutableArrayBuilder$1__ToImmutable(_) {
    return ImmutableArray$1_$ctor_B867673(toArray(_.items));
}

export class ImmutableArray {
    constructor() {
    }
}

export function ImmutableArray$reflection() {
    return class_type("Setback.Cfrm.ImmutableArray", void 0, ImmutableArray);
}

export function ImmutableArray_CreateBuilder_Z524259A4(n) {
    return ImmutableArrayBuilder$1_$ctor_Z524259A4(n);
}

export function ImmutableArray_CreateRange_404FCA0C(range) {
    return ImmutableArray$1_$ctor_BB573A(range);
}

