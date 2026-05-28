
import { Union } from "../../Client/fable_modules/fable-library-js.5.0.0/Types.js";
import { union_type, string_type } from "../../Client/fable_modules/fable-library-js.5.0.0/Reflection.js";
import { defaultOf } from "../../Client/fable_modules/fable-library-js.5.0.0/Util.js";
import { FSharpResult$2 } from "../../Client/fable_modules/fable-library-js.5.0.0/Result.js";

export class ItemName extends Union {
    constructor(Item) {
        super();
        this.tag = 0;
        this.fields = [Item];
    }
    cases() {
        return ["ItemName"];
    }
}

export function ItemName_$reflection() {
    return union_type("BoxTracker.ItemName.ItemName", [], ItemName, () => [[["Item", string_type]]]);
}

export function create(raw) {
    const trimmed = (raw === defaultOf()) ? "" : raw.trim();
    if (trimmed.length === 0) {
        return new FSharpResult$2(1, ["Item name must not be empty"]);
    }
    else if (trimmed.length > 200) {
        return new FSharpResult$2(1, ["Item name must be 200 characters or fewer"]);
    }
    else {
        return new FSharpResult$2(0, [new ItemName(trimmed)]);
    }
}

export function value(_arg) {
    const s = _arg.fields[0];
    return s;
}

