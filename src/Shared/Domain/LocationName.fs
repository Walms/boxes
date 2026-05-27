module BoxTracker.LocationName

type LocationName = private LocationName of string

let create (raw: string) : Result<LocationName, string> =
    let trimmed : string =
        if System.Object.ReferenceEquals(raw, null) then ""
        else raw.Trim()
    if trimmed.Length = 0 then
        Error "Location name must not be empty"
    elif trimmed.Length > 200 then
        Error "Location name must be 200 characters or fewer"
    else
        Ok(LocationName trimmed)

let value (LocationName s: LocationName) : string = s
