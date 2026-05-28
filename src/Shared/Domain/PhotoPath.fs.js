
import { Union } from "../../Client/fable_modules/fable-library-js.5.0.0/Types.js";
import { union_type, string_type } from "../../Client/fable_modules/fable-library-js.5.0.0/Reflection.js";
import { defaultOf } from "../../Client/fable_modules/fable-library-js.5.0.0/Util.js";
import { FSharpResult$2 } from "../../Client/fable_modules/fable-library-js.5.0.0/Result.js";
import { concat } from "../../Client/fable_modules/fable-library-js.5.0.0/String.js";

export class PhotoPath extends Union {
    constructor(Item) {
        super();
        this.tag = 0;
        this.fields = [Item];
    }
    cases() {
        return ["PhotoPath"];
    }
}

export function PhotoPath_$reflection() {
    return union_type("BoxTracker.PhotoPath.PhotoPath", [], PhotoPath, () => [[["Item", string_type]]]);
}

export function create(boxId, guid, extension) {
    return new PhotoPath(`photos/${boxId}/${guid}.${extension}`);
}

export function value(_arg) {
    const s = _arg.fields[0];
    return s;
}

export function tryParse(s) {
    if ((s !== defaultOf()) && s.startsWith("photos/")) {
        return new FSharpResult$2(0, [new PhotoPath(s)]);
    }
    else {
        return new FSharpResult$2(1, [concat("Invalid photo path: \'", s, ..."\'")]);
    }
}

