
import { Union } from "../../Client/fable_modules/fable-library-js.5.0.0/Types.js";
import { BoxId_$reflection } from "./BoxId.fs.js";
import { LocationCode_$reflection } from "./LocationCode.fs.js";
import { union_type } from "../../Client/fable_modules/fable-library-js.5.0.0/Reflection.js";

export class Container extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["Unassigned", "InBox", "AtLocation"];
    }
}

export function Container_$reflection() {
    return union_type("BoxTracker.Container.Container", [], Container, () => [[], [["BoxId", BoxId_$reflection()]], [["LocationCode", LocationCode_$reflection()]]]);
}

