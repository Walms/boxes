
import { Union } from "../../Client/fable_modules/fable-library-js.5.0.0/Types.js";
import { union_type, string_type } from "../../Client/fable_modules/fable-library-js.5.0.0/Reflection.js";
import { isNullOrWhiteSpace } from "../../Client/fable_modules/fable-library-js.5.0.0/String.js";
import { FSharpResult$2 } from "../../Client/fable_modules/fable-library-js.5.0.0/Result.js";
import { forAll } from "../../Client/fable_modules/fable-library-js.5.0.0/Seq.js";
import { isLetterOrDigit } from "../../Client/fable_modules/fable-library-js.5.0.0/Char.js";

export class LocationCode extends Union {
    constructor(Item) {
        super();
        this.tag = 0;
        this.fields = [Item];
    }
    cases() {
        return ["LocationCode"];
    }
}

export function LocationCode_$reflection() {
    return union_type("BoxTracker.LocationCode.LocationCode", [], LocationCode, () => [[["Item", string_type]]]);
}

export function create(raw) {
    const trimmed = raw.trim();
    if (isNullOrWhiteSpace(trimmed)) {
        return new FSharpResult$2(1, ["Location code must not be empty"]);
    }
    else if (trimmed.length > 20) {
        return new FSharpResult$2(1, ["Location code must be 20 characters or fewer"]);
    }
    else if (!forAll((c) => {
        if (isLetterOrDigit(c)) {
            return true;
        }
        else {
            return c === "-";
        }
    }, trimmed.split(""))) {
        return new FSharpResult$2(1, ["Location code may only contain letters, digits, and hyphens"]);
    }
    else {
        return new FSharpResult$2(0, [new LocationCode(trimmed.toUpperCase())]);
    }
}

export function value(_arg) {
    const s = _arg.fields[0];
    return s;
}

export function tryParse(s) {
    return create(s);
}

