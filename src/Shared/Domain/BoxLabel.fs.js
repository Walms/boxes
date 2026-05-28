
import { Union } from "../../Client/fable_modules/fable-library-js.5.0.0/Types.js";
import { union_type, string_type } from "../../Client/fable_modules/fable-library-js.5.0.0/Reflection.js";
import { defaultOf } from "../../Client/fable_modules/fable-library-js.5.0.0/Util.js";
import { FSharpResult$2 } from "../../Client/fable_modules/fable-library-js.5.0.0/Result.js";
import { map } from "../../Client/fable_modules/fable-library-js.5.0.0/Option.js";

export class BoxLabel extends Union {
    constructor(Item) {
        super();
        this.tag = 0;
        this.fields = [Item];
    }
    cases() {
        return ["BoxLabel"];
    }
}

export function BoxLabel_$reflection() {
    return union_type("BoxTracker.BoxLabel.BoxLabel", [], BoxLabel, () => [[["Item", string_type]]]);
}

export function create(raw) {
    const trimmed = (raw === defaultOf()) ? "" : raw.trim();
    if (trimmed.length === 0) {
        return new FSharpResult$2(0, [undefined]);
    }
    else if (trimmed.length > 200) {
        return new FSharpResult$2(1, ["Box label must be 200 characters or fewer"]);
    }
    else {
        return new FSharpResult$2(0, [new BoxLabel(trimmed)]);
    }
}

export function value(_arg) {
    return _arg.fields[0];
}

export function ofOption(o) {
    return map(value, o);
}

