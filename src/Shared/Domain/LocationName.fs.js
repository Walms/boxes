
import { Union } from "../../Client/fable_modules/fable-library-js.5.0.0/Types.js";
import { union_type, string_type } from "../../Client/fable_modules/fable-library-js.5.0.0/Reflection.js";
import { defaultOf } from "../../Client/fable_modules/fable-library-js.5.0.0/Util.js";
import { FSharpResult$2 } from "../../Client/fable_modules/fable-library-js.5.0.0/Result.js";

export class LocationName extends Union {
    constructor(Item) {
        super();
        this.tag = 0;
        this.fields = [Item];
    }
    cases() {
        return ["LocationName"];
    }
}

export function LocationName_$reflection() {
    return union_type("BoxTracker.LocationName.LocationName", [], LocationName, () => [[["Item", string_type]]]);
}

export function create(raw) {
    const trimmed = (raw === defaultOf()) ? "" : raw.trim();
    if (trimmed.length === 0) {
        return new FSharpResult$2(1, ["Location name must not be empty"]);
    }
    else if (trimmed.length > 200) {
        return new FSharpResult$2(1, ["Location name must be 200 characters or fewer"]);
    }
    else {
        return new FSharpResult$2(0, [new LocationName(trimmed)]);
    }
}

export function value(_arg) {
    return _arg.fields[0];
}

