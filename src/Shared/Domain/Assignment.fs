module BoxTracker.Assignment

type Assignment =
    | Unassigned
    | AssignedTo of LocationCode: BoxTracker.LocationCode.LocationCode
