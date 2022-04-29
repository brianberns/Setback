import { SuitModule_fromChar, SuitModule_numSuits, SuitModule_toChar } from "./Suit.fs.js";
import { RankModule_fromChar, RankModule_numRanks, RankModule_toChar } from "./Rank.fs.js";
import { printf, toText } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/String.js";
import { toString, Record } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Types.js";
import { enum_type, int32_type, getEnumValues, class_type } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Reflection.js";
import { map, collect, delay, toArray } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Seq.js";

export class Card extends Record {
    constructor(Rank, Suit) {
        super();
        this.Rank = (Rank | 0);
        this.Suit = (Suit | 0);
    }
    toString() {
        const this$ = this;
        const arg20 = SuitModule_toChar(this$.Suit);
        const arg10 = RankModule_toChar(this$.Rank);
        return toText(printf("%c%c"))(arg10)(arg20);
    }
}

export function Card$reflection() {
    return class_type("PlayingCards.Card", void 0, Card, class_type("System.ValueType"));
}

export function Card_$ctor_Z106B7333(rank, suit) {
    return new Card(rank, suit);
}

export function Card__get_String(this$) {
    return toString(this$);
}

export const CardModule_numCards = SuitModule_numSuits * RankModule_numRanks;

export function CardModule_fromString(str) {
    const rank = RankModule_fromChar(str[0]) | 0;
    const suit = SuitModule_fromChar(str[1]) | 0;
    return Card_$ctor_Z106B7333(rank, suit);
}

export const CardModule_allCards = toArray(delay(() => collect((suit) => map((rank) => Card_$ctor_Z106B7333(rank, suit), toArray(getEnumValues(enum_type("PlayingCards.Rank", int32_type, [["Two", 2], ["Three", 3], ["Four", 4], ["Five", 5], ["Six", 6], ["Seven", 7], ["Eight", 8], ["Nine", 9], ["Ten", 10], ["Jack", 11], ["Queen", 12], ["King", 13], ["Ace", 14]])))), toArray(getEnumValues(enum_type("PlayingCards.Suit", int32_type, [["Clubs", 0], ["Diamonds", 1], ["Hearts", 2], ["Spades", 3]]))))));

