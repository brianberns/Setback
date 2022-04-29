import { toArray } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Seq.js";
import { enum_type, int32_type, getEnumValues } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Reflection.js";

export const SuitModule_numSuits = toArray(getEnumValues(enum_type("PlayingCards.Suit", int32_type, [["Clubs", 0], ["Diamonds", 1], ["Hearts", 2], ["Spades", 3]]))).length;

export function SuitModule_toChar(suit) {
    return "♣♦♥♠"[suit];
}

export function SuitModule_toLetter(suit) {
    return "CDHS"[suit];
}

export function SuitModule_fromChar(_arg1) {
    switch (_arg1) {
        case "C":
        case "♣": {
            return 0;
        }
        case "D":
        case "♦": {
            return 1;
        }
        case "H":
        case "♥": {
            return 2;
        }
        case "S":
        case "♠": {
            return 3;
        }
        default: {
            throw (new Error("Unexpected suit char"));
        }
    }
}

export function PlayingCards_Suit__Suit_get_Char(suit) {
    return SuitModule_toChar(suit);
}

export function PlayingCards_Suit__Suit_get_Letter(suit) {
    return SuitModule_toLetter(suit);
}

