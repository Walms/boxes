module BoxTracker.BoxId

type BoxId = private BoxId of string

let create (sequenceNumber: int) : BoxId =
    BoxId($"BOX-%03d{sequenceNumber}")

let value (BoxId s: BoxId) : string = s

let tryParse (s: string) : Result<BoxId, string> =
    if s <> null && s.StartsWith("BOX-") then
        let numberPart : string = s.Substring(4)
        match System.Int32.TryParse numberPart with
        | true, _ -> Ok(BoxId s)
        | false, _ -> Error $"Invalid BoxId format: '{s}'"
    else
        Error $"Invalid BoxId format: '{s}'"

let parse (s: string) : BoxId =
    match tryParse s with
    | Ok id -> id
    | Error msg -> failwith msg

let extractSequence (BoxId s: BoxId) : int =
    let numberPart : string = s.Substring(4)
    System.Int32.Parse numberPart
