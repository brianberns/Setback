import { toArray } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Seq.js";
import { enum_type, int32_type, getEnumValues } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Reflection.js";

export const RankModule_numRanks = toArray(getEnumValues(enum_type("PlayingCards.Rank", int32_type, [["Two", 2], ["Three", 3], ["Four", 4], ["Five", 5], ["Six", 6], ["Seven", 7], ["Eight", 8], ["Nine", 9], ["Ten", 10], ["Jack", 11], ["Queen", 12], ["King", 13], ["Ace", 14]]))).length;

export function RankModule_toChar(rank) {
    return "23456789TJQKA"[rank - 2];
}

export function RankModule_fromChar(_arg1) {
    switch (_arg1) {
        case "A": {
            return 14;
        }
        case "J": {
            return 11;
        }
        case "K": {
            return 13;
        }
        case "Q": {
            return 12;
        }
        case "T": {
            return 10;
        }
        default: {
            const c = _arg1;
            const n = (c.charCodeAt(0) - "0".charCodeAt(0)) | 0;
            if ((n >= 2) ? (n <= 9) : false) {
                return n | 0;
            }
            else {
                throw (new Error("Unexpected rank char"));
            }
        }
    }
}

export function PlayingCards_Rank__Rank_get_Char(rank) {
    return RankModule_toChar(rank);
}

