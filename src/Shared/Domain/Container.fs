module BoxTracker.Container

type Container =
    | Unassigned
    | InBox of BoxId: BoxTracker.BoxId.BoxId
    | AtLocation of LocationCode: BoxTracker.LocationCode.LocationCode
