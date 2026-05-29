module BoxTracker.Domain.Tests.Tests

open Xunit
open FsCheck
open FsCheck.Xunit

[<Fact>]
let ``LocationCode.create accepts valid input`` () : unit =
    let result : Result<BoxTracker.LocationCode.LocationCode, string> =
        BoxTracker.LocationCode.create "BACK-LEFT"
    Assert.True(Result.isOk result)

[<Fact>]
let ``LocationCode.create rejects empty input`` () : unit =
    let result : Result<BoxTracker.LocationCode.LocationCode, string> =
        BoxTracker.LocationCode.create ""
    Assert.True(Result.isError result)

[<Fact>]
let ``LocationCode.create uppercases input`` () : unit =
    let result : Result<BoxTracker.LocationCode.LocationCode, string> =
        BoxTracker.LocationCode.create "back-left"
    match result with
    | Ok code -> Assert.Equal("BACK-LEFT", BoxTracker.LocationCode.value code)
    | Error _ -> Assert.Fail("Expected Ok")

[<Fact>]
let ``LocationCode.create rejects invalid characters`` () : unit =
    let result : Result<BoxTracker.LocationCode.LocationCode, string> =
        BoxTracker.LocationCode.create "BACK LEFT!"
    Assert.True(Result.isError result)

[<Fact>]
let ``LocationCode.create rejects over 20 characters`` () : unit =
    let result : Result<BoxTracker.LocationCode.LocationCode, string> =
        BoxTracker.LocationCode.create "ABCDEFGHIJKLMNOPQRSTU"
    Assert.True(Result.isError result)

[<Property>]
let ``LocationCode.create always uppercases when valid`` (s: string) : bool =
    let trimmed : string = s.Trim()
    let isValid : bool =
        not (System.String.IsNullOrEmpty trimmed)
        && trimmed.Length <= 20
        && trimmed |> Seq.forall (fun c -> System.Char.IsLetterOrDigit c || c = '-')
    match BoxTracker.LocationCode.create s with
    | Ok c -> isValid && BoxTracker.LocationCode.value c = trimmed.ToUpperInvariant()
    | Error _ -> not isValid

[<Property>]
let ``LocationCode.tryParse roundtrips via value`` (s: string) : bool =
    match BoxTracker.LocationCode.create s with
    | Ok created ->
        let serialized : string = BoxTracker.LocationCode.value created
        match BoxTracker.LocationCode.tryParse serialized with
        | Ok parsed -> BoxTracker.LocationCode.value parsed = serialized
        | Error _ -> false
    | Error _ -> true

[<Fact>]
let ``BoxId.create produces correct format`` () : unit =
    let id : BoxTracker.BoxId.BoxId = BoxTracker.BoxId.create 1
    Assert.Equal("BOX-001", BoxTracker.BoxId.value id)

[<Fact>]
let ``BoxId.create zero-pads to 3 digits`` () : unit =
    let id : BoxTracker.BoxId.BoxId = BoxTracker.BoxId.create 42
    Assert.Equal("BOX-042", BoxTracker.BoxId.value id)

[<Property>]
let ``BoxId.create always produces BOX-prefixed zero-padded format`` (n: int) : bool =
    let n : int = abs n
    let id : BoxTracker.BoxId.BoxId = BoxTracker.BoxId.create n
    let s : string = BoxTracker.BoxId.value id
    s.StartsWith("BOX-") && s.Length >= 7

[<Property>]
let ``BoxId.tryParse roundtrips all valid ids`` (n: int) : bool =
    let n : int = abs n
    let id : BoxTracker.BoxId.BoxId = BoxTracker.BoxId.create n
    let s : string = BoxTracker.BoxId.value id
    match BoxTracker.BoxId.tryParse s with
    | Ok parsed -> BoxTracker.BoxId.value parsed = s && BoxTracker.BoxId.extractSequence parsed = n
    | Error _ -> false

[<Property>]
let ``BoxId.tryParse rejects non-BOX strings`` (s: string) : bool =
    s |> BoxTracker.BoxId.tryParse |> Result.isError || s.StartsWith("BOX-")

[<Fact>]
let ``ItemName.create rejects empty`` () : unit =
    let result : Result<BoxTracker.ItemName.ItemName, string> =
        BoxTracker.ItemName.create ""
    Assert.True(Result.isError result)

[<Property>]
let ``ItemName.create accepts non-empty trimmed strings under 200 chars`` (s: string) : bool =
    let trimmed : string = s.Trim()
    if System.String.IsNullOrEmpty trimmed || trimmed.Length > 200 then
        BoxTracker.ItemName.create s |> Result.isError
    else
        match BoxTracker.ItemName.create s with
        | Ok name -> BoxTracker.ItemName.value name = trimmed
        | Error _ -> false

[<Property>]
let ``ItemName.create rejects strings over 200 chars`` (s: string) : bool =
    let long : string = new System.String(Array.concat [| Array.map (fun (c: char) -> if System.Char.IsWhiteSpace c then 'x' else c) (s.ToCharArray()); Array.replicate 201 'x' |])
    long.Length > 200 && (BoxTracker.ItemName.create long |> Result.isError)

[<Fact>]
let ``LocationName.create trims whitespace`` () : unit =
    let result : Result<BoxTracker.LocationName.LocationName, string> =
        BoxTracker.LocationName.create "  Back Wall  "
    match result with
    | Ok name -> Assert.Equal("Back Wall", BoxTracker.LocationName.value name)
    | Error _ -> Assert.Fail("Expected Ok")

[<Property>]
let ``LocationName.create trims all inputs`` (s: string) : bool =
    let trimmed : string = s.Trim()
    if System.String.IsNullOrEmpty trimmed || trimmed.Length > 200 then
        BoxTracker.LocationName.create s |> Result.isError
    else
        match BoxTracker.LocationName.create s with
        | Ok name -> BoxTracker.LocationName.value name = trimmed
        | Error _ -> false

[<Property>]
let ``LocationName.create rejects strings over 200 chars`` (s: string) : bool =
    let long : string = new System.String(Array.concat [| Array.map (fun (c: char) -> if System.Char.IsWhiteSpace c then 'x' else c) (s.ToCharArray()); Array.replicate 201 'x' |])
    long.Length > 200 && (BoxTracker.LocationName.create long |> Result.isError)

[<Property>]
let ``BoxLabel.create returns None for empty/whitespace`` (s: string) : bool =
    let trimmed : string = s.Trim()
    if System.String.IsNullOrEmpty trimmed then
        match BoxTracker.BoxLabel.create s with
        | Ok None -> true
        | _ -> false
    else
        true

[<Property>]
let ``BoxLabel.create rejects over 200 chars`` (s: string) : bool =
    let long : string = new System.String(Array.concat [| Array.map (fun (c: char) -> if System.Char.IsWhiteSpace c then 'x' else c) (s.ToCharArray()); Array.replicate 201 'x' |])
    long.Length > 200 && (BoxTracker.BoxLabel.create long |> Result.isError)

[<Property>]
let ``BoxLabel.create trims and wraps non-empty valid strings`` (s: string) : bool =
    let trimmed : string = s.Trim()
    if System.String.IsNullOrEmpty trimmed || trimmed.Length > 200 then
        true
    else
        match BoxTracker.BoxLabel.create s with
        | Ok (Some label) -> BoxTracker.BoxLabel.value label = trimmed
        | _ -> false

[<Property>]
let ``PhotoPath.create produces path with photos prefix`` (boxId: string) (guid: System.Guid) : bool =
    let path : BoxTracker.PhotoPath.PhotoPath = BoxTracker.PhotoPath.create boxId guid "jpg"
    let s : string = BoxTracker.PhotoPath.value path
    s.StartsWith("photos/") && s.Contains(boxId) && s.EndsWith(".jpg")

[<Property>]
let ``PhotoPath.tryParse accepts paths starting with photos/`` (s: string) : bool =
    if s <> null && s.StartsWith("photos/") then
        BoxTracker.PhotoPath.tryParse s |> Result.isOk
    else
        BoxTracker.PhotoPath.tryParse s |> Result.isError

[<Fact>]
let ``Location.tryMakeEmpty rejects location with assigned boxes`` () : unit =
    let code : BoxTracker.LocationCode.LocationCode =
        BoxTracker.LocationCode.create "TEST" |> Result.defaultWith (fun _ -> failwith "bad code")
    let loc : BoxTracker.Types.Location = {
        Code = code
        Name = BoxTracker.LocationName.create "Test" |> Result.defaultWith (fun _ -> failwith "bad name")
        IsArchived = false
        Photo = None
        CreatedAt = System.DateTimeOffset.UtcNow
    }
    let result : Result<_, string> = BoxTracker.Location.tryMakeEmpty loc 3
    Assert.True(Result.isError result)

[<Fact>]
let ``Location.tryMakeEmpty accepts location with zero boxes`` () : unit =
    let code : BoxTracker.LocationCode.LocationCode =
        BoxTracker.LocationCode.create "TEST" |> Result.defaultWith (fun _ -> failwith "bad code")
    let loc : BoxTracker.Types.Location = {
        Code = code
        Name = BoxTracker.LocationName.create "Test" |> Result.defaultWith (fun _ -> failwith "bad name")
        IsArchived = false
        Photo = None
        CreatedAt = System.DateTimeOffset.UtcNow
    }
    let result : Result<_, string> = BoxTracker.Location.tryMakeEmpty loc 0
    Assert.True(Result.isOk result)

[<Fact>]
let ``Location.archive sets IsArchived to true`` () : unit =
    let code : BoxTracker.LocationCode.LocationCode =
        BoxTracker.LocationCode.create "TEST" |> Result.defaultWith (fun _ -> failwith "bad code")
    let loc : BoxTracker.Types.Location = {
        Code = code
        Name = BoxTracker.LocationName.create "Test" |> Result.defaultWith (fun _ -> failwith "bad name")
        IsArchived = false
        Photo = None
        CreatedAt = System.DateTimeOffset.UtcNow
    }
    let empty : BoxTracker.Location.EmptyLocation =
        BoxTracker.Location.tryMakeEmpty loc 0 |> Result.defaultWith (fun _ -> failwith "should be ok")
    let archived : BoxTracker.Types.Location = BoxTracker.Location.archive empty
    Assert.True(archived.IsArchived)
    Assert.Equal(BoxTracker.LocationCode.value loc.Code, BoxTracker.LocationCode.value archived.Code)
