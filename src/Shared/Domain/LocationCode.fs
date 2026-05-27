module BoxTracker.LocationCode

type LocationCode = private LocationCode of string

let create (raw: string) : Result<LocationCode, string> =
    if System.String.IsNullOrWhiteSpace raw then
        Error "Location code must not be empty"
    elif raw.Length > 20 then
        Error "Location code must be 20 characters or fewer"
    elif raw |> Seq.forall (fun (c: char) -> System.Char.IsLetterOrDigit c || c = '-') |> not then
        Error "Location code may only contain letters, digits, and hyphens"
    else
        Ok(LocationCode(raw.ToUpperInvariant()))

let value (LocationCode s: LocationCode) : string = s

let tryParse (s: string) : Result<LocationCode, string> = create s
