import { Record } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Types.js";
import { CardModule_allCards, Card$reflection } from "./Card.fs.js";
import { record_type, array_type } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Reflection.js";
import { toList, iterate } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Seq.js";
import { randomNext } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Util.js";
import { rangeDouble } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Range.js";
import { Array_clone } from "./Prelude.fs.js";

export class Deck extends Record {
    constructor(Cards) {
        super();
        this.Cards = Cards;
    }
}

export function Deck$reflection() {
    return record_type("PlayingCards.Deck", [], Deck, () => [["Cards", array_type(Card$reflection())]]);
}

function DeckModule_knuthShuffle(rng, items) {
    const swap = (i, j) => {
        const item = items[i];
        items[i] = items[j];
        items[j] = item;
    };
    const len = items.length | 0;
    iterate((i_1) => {
        swap(i_1, randomNext(i_1, len));
    }, toList(rangeDouble(0, 1, len - 2)));
    return items;
}

export function DeckModule_shuffle(rng) {
    return new Deck(DeckModule_knuthShuffle(rng, Array_clone(CardModule_allCards)));
}

