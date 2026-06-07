module BoxTracker.LocationCode

type LocationCode = private LocationCode of string

let create (raw: string) : Result<LocationCode, string> =
    let trimmed : string =
        if System.Object.ReferenceEquals(raw, null) then ""
        else raw.Trim()
    if System.String.IsNullOrWhiteSpace trimmed then
        Error "Location code must not be empty"
    elif trimmed.Length > 20 then
        Error "Location code must be 20 characters or fewer"
    elif trimmed |> Seq.forall (fun (c: char) -> System.Char.IsLetterOrDigit c || c = '-') |> not then
        Error "Location code may only contain letters, digits, and hyphens"
    else
        Ok(LocationCode(trimmed.ToUpperInvariant()))

let value (LocationCode s: LocationCode) : string = s

let tryParse (s: string) : Result<LocationCode, string> = create s
