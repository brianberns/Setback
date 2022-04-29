import { interpolate, toText, join } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/String.js";
import { sortByDescending, toArray, map } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Seq.js";
import { PlayingCards_Rank__Rank_get_Char } from "./Rank.fs.js";
import { numberHash, comparePrimitives } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Util.js";
import { PlayingCards_Suit__Suit_get_Char } from "./Suit.fs.js";
import { groupBy } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Seq2.js";

export function HandModule_toString(hand) {
    return join(" ", map((tupledArg) => {
        const suit = tupledArg[0] | 0;
        const cards = tupledArg[1];
        let sCards;
        const arg00 = toArray(map((card_2) => PlayingCards_Rank__Rank_get_Char(card_2.Rank), sortByDescending((card_1) => card_1.Rank, cards, {
            Compare: (x_2, y_2) => comparePrimitives(x_2, y_2),
        })));
        sCards = (arg00.join(''));
        return toText(interpolate("%P()%P()", [sCards, PlayingCards_Suit__Suit_get_Char(suit)]));
    }, sortByDescending((tuple) => tuple[0], groupBy((card) => card.Suit, hand, {
        Equals: (x, y) => (x === y),
        GetHashCode: (x) => numberHash(x),
    }), {
        Compare: (x_1, y_1) => comparePrimitives(x_1, y_1),
    })));
}

