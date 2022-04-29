import { toArray } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Seq.js";
import { enum_type, int32_type, getEnumValues } from "../Setback.Web/Client/src/.fable/fable-library.3.2.9/Reflection.js";

export const BidModule_numBids = toArray(getEnumValues(enum_type("Setback.Bid", int32_type, [["Pass", 0], ["Two", 2], ["Three", 3], ["Four", 4]]))).length;

