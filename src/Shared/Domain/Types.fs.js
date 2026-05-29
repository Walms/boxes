
import { Record } from "../../Client/fable_modules/fable-library-js.5.0.0/Types.js";
import { LocationCode_$reflection } from "./LocationCode.fs.js";
import { LocationName_$reflection } from "./LocationName.fs.js";
import { string_type, record_type, class_type, option_type, bool_type } from "../../Client/fable_modules/fable-library-js.5.0.0/Reflection.js";
import { PhotoPath_$reflection } from "./PhotoPath.fs.js";
import { ItemName_$reflection } from "./ItemName.fs.js";
import { Container_$reflection } from "./Container.fs.js";
import { BoxId_$reflection } from "./BoxId.fs.js";
import { BoxLabel_$reflection } from "./BoxLabel.fs.js";

export class Location extends Record {
    constructor(Code, Name, IsArchived, Photo, CreatedAt) {
        super();
        this.Code = Code;
        this.Name = Name;
        this.IsArchived = IsArchived;
        this.Photo = Photo;
        this.CreatedAt = CreatedAt;
    }
}

export function Location_$reflection() {
    return record_type("BoxTracker.Types.Location", [], Location, () => [["Code", LocationCode_$reflection()], ["Name", LocationName_$reflection()], ["IsArchived", bool_type], ["Photo", option_type(PhotoPath_$reflection())], ["CreatedAt", class_type("System.DateTimeOffset")]]);
}

export class Item extends Record {
    constructor(Id, Name, Photo, Placement, AddedAt) {
        super();
        this.Id = Id;
        this.Name = Name;
        this.Photo = Photo;
        this.Placement = Placement;
        this.AddedAt = AddedAt;
    }
}

export function Item_$reflection() {
    return record_type("BoxTracker.Types.Item", [], Item, () => [["Id", class_type("System.Guid")], ["Name", ItemName_$reflection()], ["Photo", option_type(PhotoPath_$reflection())], ["Placement", Container_$reflection()], ["AddedAt", class_type("System.DateTimeOffset")]]);
}

export class Box extends Record {
    constructor(Id, Label, Photo, Placement, CreatedAt) {
        super();
        this.Id = Id;
        this.Label = Label;
        this.Photo = Photo;
        this.Placement = Placement;
        this.CreatedAt = CreatedAt;
    }
}

export function Box_$reflection() {
    return record_type("BoxTracker.Types.Box", [], Box, () => [["Id", BoxId_$reflection()], ["Label", option_type(BoxLabel_$reflection())], ["Photo", option_type(PhotoPath_$reflection())], ["Placement", Container_$reflection()], ["CreatedAt", class_type("System.DateTimeOffset")]]);
}

export class Move extends Record {
    constructor(Id, EntityType, EntityId, To, MovedAt) {
        super();
        this.Id = Id;
        this.EntityType = EntityType;
        this.EntityId = EntityId;
        this.To = To;
        this.MovedAt = MovedAt;
    }
}

export function Move_$reflection() {
    return record_type("BoxTracker.Types.Move", [], Move, () => [["Id", class_type("System.Guid")], ["EntityType", string_type], ["EntityId", string_type], ["To", Container_$reflection()], ["MovedAt", class_type("System.DateTimeOffset")]]);
}

