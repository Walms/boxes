module BoxTracker.BoxLabel

type BoxLabel = private BoxLabel of string

let create (raw: string) : Result<BoxLabel option, string> =
    let trimmed : string =
        if System.Object.ReferenceEquals(raw, null) then ""
        else raw.Trim()
    if trimmed.Length = 0 then
        Ok None
    elif trimmed.Length > 200 then
        Error "Box label must be 200 characters or fewer"
    else
        Ok(Some(BoxLabel trimmed))

let value (BoxLabel s: BoxLabel) : string = s

let ofOption (o: BoxLabel option) : string option =
    o |> Option.map value
