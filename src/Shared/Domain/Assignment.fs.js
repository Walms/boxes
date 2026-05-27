
import { Union } from "../../Client/fable_modules/fable-library-js.5.0.0/Types.js";
import { LocationCode_$reflection } from "./LocationCode.fs.js";
import { union_type } from "../../Client/fable_modules/fable-library-js.5.0.0/Reflection.js";

export class Assignment extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["Unassigned", "AssignedTo"];
    }
}

export function Assignment_$reflection() {
    return union_type("BoxTracker.Assignment.Assignment", [], Assignment, () => [[], [["LocationCode", LocationCode_$reflection()]]]);
}

