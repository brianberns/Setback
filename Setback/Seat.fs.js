import { map, delay, toArray } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Seq.js";
import { enum_type, int32_type, getEnumValues } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Reflection.js";
import { rangeDouble } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Range.js";

export const SeatModule_numSeats = toArray(getEnumValues(enum_type("Setback.Seat", int32_type, [["West", 0], ["North", 1], ["East", 2], ["South", 3]]))).length;

export function SeatModule_toChar(seat) {
    return "WNES"[seat];
}

export function SeatModule_fromChar(_arg1) {
    switch (_arg1) {
        case "E": {
            return 2;
        }
        case "N": {
            return 1;
        }
        case "S": {
            return 3;
        }
        case "W": {
            return 0;
        }
        default: {
            throw (new Error("Unexpected seat"));
        }
    }
}

export function SeatModule_incr(n, seat) {
    if (!(n >= 0)) {
        debugger;
    }
    return ((seat + n) % SeatModule_numSeats) | 0;
}

export const SeatModule_next = (seat) => SeatModule_incr(1, seat);

export function SeatModule_cycle(seat) {
    return delay(() => map((i) => SeatModule_incr(i, seat), rangeDouble(0, 1, SeatModule_numSeats - 1)));
}

export function Setback_Seat__Seat_get_Char(seat) {
    return SeatModule_toChar(seat);
}

export function Setback_Seat__Seat_get_Next(seat) {
    return SeatModule_next(seat);
}

