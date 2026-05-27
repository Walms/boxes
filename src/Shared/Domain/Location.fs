module BoxTracker.Location

open BoxTracker.Types
open BoxTracker.LocationCode

type EmptyLocation = private EmptyLocation of Location

let tryMakeEmpty (location: Location) (assignedBoxCount: int) : Result<EmptyLocation, string> =
    if assignedBoxCount > 0 then
        Error $"Cannot archive '%s{LocationCode.value location.Code}': %d{assignedBoxCount} box(es) still assigned"
    else
        Ok(EmptyLocation location)

let archive (EmptyLocation loc: EmptyLocation) : Location =
    { loc with IsArchived = true }

let get (EmptyLocation loc: EmptyLocation) : Location = loc
