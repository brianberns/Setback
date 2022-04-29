import { Record } from "../Client/src/.fable/fable-library.3.2.9/Types.js";
import { record_type, lambda_type, class_type, option_type, int32_type, string_type } from "../Client/src/.fable/fable-library.3.2.9/Reflection.js";

export class ISetbackApi extends Record {
    constructor(GetActionIndex) {
        super();
        this.GetActionIndex = GetActionIndex;
    }
}

export function ISetbackApi$reflection() {
    return record_type("Setback.Web.Shared.ISetbackApi", [], ISetbackApi, () => [["GetActionIndex", lambda_type(string_type, class_type("Microsoft.FSharp.Control.FSharpAsync`1", [option_type(int32_type)]))]]);
}

