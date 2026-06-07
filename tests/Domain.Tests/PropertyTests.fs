module BoxTracker.Domain.Tests.PropertyTests

open Xunit
open FsCheck.Xunit

// These tests probe the domain smart constructors for invariant violations:
// null-safety, canonical-form stability (idempotence) and roundtripping. The
// parse-don't-validate contract is that `create` is total over all strings
// (never throws) and that `value` always yields a string the constructor will
// accept again.

// --- LocationCode null-safety regression ---
// LocationCode.create historically called raw.Trim() with no null guard, so it
// threw NullReferenceException on null instead of returning Error like every
// other smart constructor (ItemName/LocationName/BoxLabel).

[<Fact>]
let ``LocationCode.create returns Error on null instead of throwing`` () : unit =
    Assert.True(BoxTracker.LocationCode.create null |> Result.isError)

[<Fact>]
let ``LocationCode.tryParse returns Error on null instead of throwing`` () : unit =
    Assert.True(BoxTracker.LocationCode.tryParse null |> Result.isError)

// --- Totality: create must never throw for any non-null string ---

[<Property>]
let ``LocationCode.create never throws`` (s: string) : bool =
    try
        BoxTracker.LocationCode.create s |> ignore
        true
    with _ ->
        false

[<Property>]
let ``ItemName.create never throws`` (s: string) : bool =
    try
        BoxTracker.ItemName.create s |> ignore
        true
    with _ ->
        false

[<Property>]
let ``LocationName.create never throws`` (s: string) : bool =
    try
        BoxTracker.LocationName.create s |> ignore
        true
    with _ ->
        false

[<Property>]
let ``BoxLabel.create never throws`` (s: string) : bool =
    try
        BoxTracker.BoxLabel.create s |> ignore
        true
    with _ ->
        false

// --- LocationCode canonical form ---
// A successfully created code is always upper-cased and made only of the
// allowed characters, regardless of the original casing.

[<Property>]
let ``LocationCode.create yields an uppercase code with a valid charset`` (s: string) : bool =
    match BoxTracker.LocationCode.create s with
    | Error _ -> true
    | Ok c ->
        let v : string = BoxTracker.LocationCode.value c
        v.Length > 0
        && v = v.ToUpperInvariant()
        && v |> Seq.forall (fun (ch: char) -> System.Char.IsLetterOrDigit ch || ch = '-')

// --- Trimming smart constructors produce trimmed, stable values ---

[<Property>]
let ``ItemName.create produces a trimmed value that re-parses unchanged`` (s: string) : bool =
    match BoxTracker.ItemName.create s with
    | Error _ -> true
    | Ok name ->
        let v : string = BoxTracker.ItemName.value name
        v = v.Trim()
        && (match BoxTracker.ItemName.create v with
            | Ok again -> BoxTracker.ItemName.value again = v
            | Error _ -> false)

[<Property>]
let ``LocationName.create produces a trimmed value that re-parses unchanged`` (s: string) : bool =
    match BoxTracker.LocationName.create s with
    | Error _ -> true
    | Ok name ->
        let v : string = BoxTracker.LocationName.value name
        v = v.Trim()
        && (match BoxTracker.LocationName.create v with
            | Ok again -> BoxTracker.LocationName.value again = v
            | Error _ -> false)

[<Property>]
let ``BoxLabel.create maps blank to None and trims otherwise`` (s: string) : bool =
    match BoxTracker.BoxLabel.create s with
    | Error _ -> true
    | Ok None -> System.String.IsNullOrWhiteSpace s
    | Ok (Some label) ->
        let v : string = BoxTracker.BoxLabel.value label
        v = v.Trim() && v.Length > 0

// --- BoxId roundtrips for non-negative sequence numbers ---

[<Property>]
let ``BoxId.create then extractSequence recovers a non-negative number`` (n: int) : bool =
    let n : int = abs n
    let id : BoxTracker.BoxId.BoxId = BoxTracker.BoxId.create n
    BoxTracker.BoxId.extractSequence id = n

[<Property>]
let ``BoxId value always round-trips through tryParse`` (n: int) : bool =
    let n : int = abs n
    let id : BoxTracker.BoxId.BoxId = BoxTracker.BoxId.create n
    let v : string = BoxTracker.BoxId.value id
    match BoxTracker.BoxId.tryParse v with
    | Ok parsed -> BoxTracker.BoxId.value parsed = v
    | Error _ -> false

[<Property>]
let ``BoxId.tryParse never throws`` (s: string) : bool =
    try
        BoxTracker.BoxId.tryParse s |> ignore
        true
    with _ ->
        false

// --- PhotoPath roundtrips and variant formatting ---

[<Property>]
let ``PhotoPath.create always produces a parseable path`` (boxId: string) (guid: System.Guid) (ext: string) : bool =
    let path : BoxTracker.PhotoPath.PhotoPath = BoxTracker.PhotoPath.create boxId guid ext
    let v : string = BoxTracker.PhotoPath.value path
    match BoxTracker.PhotoPath.tryParse v with
    | Ok parsed -> BoxTracker.PhotoPath.value parsed = v
    | Error _ -> false

[<Property>]
let ``PhotoPath.valueWithVariant appends the variant and a webp suffix`` (boxId: string) (guid: System.Guid) (variant: string) : bool =
    let path : BoxTracker.PhotoPath.PhotoPath = BoxTracker.PhotoPath.createWebP boxId guid
    let full : string = BoxTracker.PhotoPath.valueWithVariant path variant
    full.EndsWith($"-{variant}.webp") && full.StartsWith("photos/")

[<Property>]
let ``PhotoPath.tryParse never throws`` (s: string) : bool =
    try
        BoxTracker.PhotoPath.tryParse s |> ignore
        true
    with _ ->
        false
