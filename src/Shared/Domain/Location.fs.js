
import { Union } from "../../Client/fable_modules/fable-library-js.5.0.0/Types.js";
import { Location, Location_$reflection } from "./Types.fs.js";
import { union_type } from "../../Client/fable_modules/fable-library-js.5.0.0/Reflection.js";
import { value } from "./LocationCode.fs.js";
import { interpolate } from "../../Client/fable_modules/fable-library-js.5.0.0/String.js";
import { FSharpResult$2 } from "../../Client/fable_modules/fable-library-js.5.0.0/Result.js";

export class EmptyLocation extends Union {
    constructor(Item) {
        super();
        this.tag = 0;
        this.fields = [Item];
    }
    cases() {
        return ["EmptyLocation"];
    }
}

export function EmptyLocation_$reflection() {
    return union_type("BoxTracker.Location.EmptyLocation", [], EmptyLocation, () => [[["Item", Location_$reflection()]]]);
}

export function tryMakeEmpty(location, assignedBoxCount) {
    if (assignedBoxCount > 0) {
        return new FSharpResult$2(1, [`Cannot archive '${value(location.Code)}': ${interpolate("%d%P()", [assignedBoxCount])} box(es) still assigned`]);
    }
    else {
        return new FSharpResult$2(0, [new EmptyLocation(location)]);
    }
}

export function archive(_arg) {
    const loc = _arg.fields[0];
    return new Location(loc.Code, loc.Name, true, loc.CreatedAt);
}

export function get$(_arg) {
    return _arg.fields[0];
}

