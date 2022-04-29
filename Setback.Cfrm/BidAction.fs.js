import { Union } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Types.js";
import { union_type, enum_type, int32_type } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Reflection.js";
import { numberHash, uncurry, compare, comparePrimitives, min } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Util.js";
import { where, collect, contains, empty, singleton, append, delay, sortDescending, sumBy, map, sortWith, toArray } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Seq.js";
import { groupBy } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Seq2.js";
import { compareWith } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Array.js";
import { AbstractAuctionModule_legalBids } from "./AbstractAuction.fs.js";

export class BidAction extends Union {
    constructor(tag, ...fields) {
        super();
        this.tag = (tag | 0);
        this.fields = fields;
    }
    cases() {
        return ["BidAction"];
    }
}

export function BidAction$reflection() {
    return union_type("Setback.Cfrm.BidAction", [], BidAction, () => [[["Item", enum_type("Setback.Bid", int32_type, [["Pass", 0], ["Two", 2], ["Three", 3], ["Four", 4]])]]]);
}

function BidActionModule_compareArrays(itemsA, itemsB) {
    const minLength = min((x, y) => comparePrimitives(x, y), itemsA.length, itemsB.length) | 0;
    const loop = (index_mut) => {
        loop:
        while (true) {
            const index = index_mut;
            if (index === minLength) {
                return comparePrimitives(itemsA.length, itemsB.length) | 0;
            }
            else {
                const itemA = itemsA[index];
                const itemB = itemsB[index];
                const matchValue = compare(itemA, itemB) | 0;
                if (matchValue === 0) {
                    index_mut = (index + 1);
                    continue loop;
                }
                else {
                    const value = matchValue | 0;
                    return value | 0;
                }
            }
            break;
        }
    };
    return loop(0) | 0;
}

export const BidActionModule_numSuitsMax = 2;

export function BidActionModule_chooseTrumpRanks(hand) {
    const pairs = toArray(sortWith(uncurry(2, (tupledArg_1) => {
        const strengthA = tupledArg_1[0] | 0;
        return (tupledArg_2) => {
            const strengthB = tupledArg_2[0] | 0;
            const ranksA = tupledArg_1[1][1];
            const ranksB = tupledArg_2[1][1];
            let value_1;
            const matchValue = comparePrimitives(strengthA, strengthB) | 0;
            if (matchValue === 0) {
                value_1 = BidActionModule_compareArrays(ranksA, ranksB);
            }
            else {
                const value = matchValue | 0;
                value_1 = value;
            }
            return (-1 * value_1) | 0;
        };
    }), map((tupledArg) => {
        const suit = tupledArg[0] | 0;
        const cards = tupledArg[1];
        const strength = sumBy((card_1) => (card_1.Rank - 1), cards, {
            GetZero: () => 0,
            Add: (x_1, y_1) => (x_1 + y_1),
        }) | 0;
        const ranks = toArray(sortDescending(map((card_2) => card_2.Rank, cards), {
            Compare: (x_2, y_2) => comparePrimitives(x_2, y_2),
        }));
        return [strength, [suit, ranks]];
    }, groupBy((card) => card.Suit, hand, {
        Equals: (x, y) => (x === y),
        GetHashCode: (x) => numberHash(x),
    }))));
    if (!(pairs.length > 0)) {
        debugger;
    }
    const result = toArray(delay(() => {
        const patternInput = pairs[0];
        const suit0 = patternInput[1][0] | 0;
        const strength0 = patternInput[0] | 0;
        const ranks0 = patternInput[1][1];
        return append(singleton([suit0, ranks0]), delay(() => {
            if (pairs.length > 1) {
                const patternInput_1 = pairs[1];
                const suit1 = patternInput_1[1][0] | 0;
                const strength1 = patternInput_1[0] | 0;
                const ranks1 = patternInput_1[1][1];
                if (!((strength0 > strength1) ? true : ((strength0 === strength1) ? (BidActionModule_compareArrays(ranks0, ranks1) >= 0) : false))) {
                    debugger;
                }
                return (((strength0 - strength1) < 2) ? (compareWith((x_3, y_3) => comparePrimitives(x_3, y_3), ranks0, ranks1) !== 0) : false) ? singleton([suit1, ranks1]) : empty();
            }
            else {
                return empty();
            }
        }));
    }));
    if (!(result.length <= BidActionModule_numSuitsMax)) {
        debugger;
    }
    return result;
}

export function BidActionModule_getActions(hand, auction) {
    const hasJack = contains(11, collect((tuple) => tuple[1], BidActionModule_chooseTrumpRanks(hand)), {
        Equals: (x, y) => (x === y),
        GetHashCode: (x) => numberHash(x),
    });
    return toArray(map((arg0) => (new BidAction(0, arg0)), where((bid) => {
        if (bid !== 4) {
            return true;
        }
        else {
            return hasJack;
        }
    }, AbstractAuctionModule_legalBids(auction))));
}

export function BidActionModule_getBid(auction, _arg1) {
    const bid = _arg1.fields[0] | 0;
    if (!contains(bid, AbstractAuctionModule_legalBids(auction), {
        Equals: (x, y) => (x === y),
        GetHashCode: (x) => numberHash(x),
    })) {
        debugger;
    }
    return bid | 0;
}

