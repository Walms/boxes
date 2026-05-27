
import { FSharpRef, Union } from "../../Client/fable_modules/fable-library-js.5.0.0/Types.js";
import { union_type, string_type } from "../../Client/fable_modules/fable-library-js.5.0.0/Reflection.js";
import { concat, substring, interpolate } from "../../Client/fable_modules/fable-library-js.5.0.0/String.js";
import { Exception, defaultOf } from "../../Client/fable_modules/fable-library-js.5.0.0/Util.js";
import { parse as parse_1, tryParse as tryParse_1 } from "../../Client/fable_modules/fable-library-js.5.0.0/Int32.js";
import { FSharpResult$2 } from "../../Client/fable_modules/fable-library-js.5.0.0/Result.js";

export class BoxId extends Union {
    constructor(Item) {
        super();
        this.tag = 0;
        this.fields = [Item];
    }
    cases() {
        return ["BoxId"];
    }
}

export function BoxId_$reflection() {
    return union_type("BoxTracker.BoxId.BoxId", [], BoxId, () => [[["Item", string_type]]]);
}

export function create(sequenceNumber) {
    return new BoxId(`BOX-${interpolate("%03d%P()", [sequenceNumber])}`);
}

export function value(_arg) {
    const s = _arg.fields[0];
    return s;
}

export function tryParse(s) {
    let outArg;
    if ((s !== defaultOf()) && s.startsWith("BOX-")) {
        const numberPart = substring(s, 4);
        if (((outArg = 0, [tryParse_1(numberPart, 511, false, 32, new FSharpRef(() => (outArg | 0), (v) => {
            outArg = (v | 0);
        })), outArg]))[0]) {
            return new FSharpResult$2(0, [new BoxId(s)]);
        }
        else {
            return new FSharpResult$2(1, [concat("Invalid BoxId format: \'", s, ..."\'")]);
        }
    }
    else {
        return new FSharpResult$2(1, [concat("Invalid BoxId format: \'", s, ..."\'")]);
    }
}

export function parse(s) {
    const matchValue = tryParse(s);
    if (matchValue.tag === 1) {
        const msg = matchValue.fields[0];
        throw new Exception(msg);
    }
    else {
        const id = matchValue.fields[0];
        return id;
    }
}

export function extractSequence(_arg) {
    const s = _arg.fields[0];
    const numberPart = substring(s, 4);
    return parse_1(numberPart, 511, false, 32) | 0;
}

