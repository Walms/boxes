module BoxTracker.Api.Tests.StorageExtraTests

open System
open System.IO
open Xunit
open BoxTracker.Storage
open BoxTracker.Types
open BoxTracker.Container

let private withStorage (test: Storage -> 'T) : 'T =
    let tempFile : string = Path.GetTempFileName()
    let connStr : string = $"Data Source=%s{tempFile}"
    Storage.InitializeSchema(connStr)
    use storage : Storage = new Storage(connStr)
    storage.Connect()
    try
        test storage
    finally
        File.Delete(tempFile)

let private makeCode (s: string) : BoxTracker.LocationCode.LocationCode =
    BoxTracker.LocationCode.create s |> Result.defaultWith (fun e -> failwith e)

let private makeName (s: string) : BoxTracker.LocationName.LocationName =
    BoxTracker.LocationName.create s |> Result.defaultWith (fun e -> failwith e)

let private makeLabel (s: string) : BoxTracker.BoxLabel.BoxLabel option =
    BoxTracker.BoxLabel.create s |> Result.defaultWith (fun e -> failwith e)

let private makeItemName (s: string) : BoxTracker.ItemName.ItemName =
    BoxTracker.ItemName.create s |> Result.defaultWith (fun e -> failwith e)

let private boxId (box: Box) : string = BoxTracker.BoxId.value box.Id

[<Fact>]
let ``UpdateBox changes the label`` () : unit =
    withStorage (fun storage ->
        let box = storage.CreateBox(makeLabel "Old")
        let updated = storage.UpdateBox(boxId box, makeLabel "New")
        Assert.True(updated.IsSome)
        Assert.Equal(Some "New", updated.Value.Label |> Option.map BoxTracker.BoxLabel.value)
    )

[<Fact>]
let ``UpdateBox can clear the label`` () : unit =
    withStorage (fun storage ->
        let box = storage.CreateBox(makeLabel "Has Label")
        let updated = storage.UpdateBox(boxId box, None)
        Assert.True(updated.IsSome)
        Assert.Equal(None, updated.Value.Label)
    )

[<Fact>]
let ``UpdateBox returns None for an unknown box`` () : unit =
    withStorage (fun storage ->
        Assert.True((storage.UpdateBox("BOX-999", makeLabel "x")).IsNone)
    )

[<Fact>]
let ``UpdateBoxPhoto sets and clears the photo`` () : unit =
    withStorage (fun storage ->
        let box = storage.CreateBox(None)
        let photo = BoxTracker.PhotoPath.create (boxId box) (Guid.NewGuid()) "jpg"
        let withPhoto = storage.UpdateBoxPhoto(boxId box, Some photo)
        Assert.True(withPhoto.IsSome)
        Assert.True(withPhoto.Value.Photo.IsSome)
        let cleared = storage.UpdateBoxPhoto(boxId box, None)
        Assert.True(cleared.IsSome)
        Assert.True(cleared.Value.Photo.IsNone)
    )

[<Fact>]
let ``UpdateItemPhoto sets the photo`` () : unit =
    withStorage (fun storage ->
        let box = storage.CreateBox(None)
        let item = storage.AddItem(boxId box, makeItemName "Drill", None)
        let photo = BoxTracker.PhotoPath.create (boxId box) (Guid.NewGuid()) "jpg"
        let updated = storage.UpdateItemPhoto(item.Id.ToString(), Some photo)
        Assert.True(updated.IsSome)
        Assert.True(updated.Value.Photo.IsSome)
    )

[<Fact>]
let ``UpdateLocationPhoto sets and clears the photo`` () : unit =
    withStorage (fun storage ->
        storage.CreateLocation(makeCode "GARAGE", makeName "Garage") |> ignore
        let photo = BoxTracker.PhotoPath.create "GARAGE" (Guid.NewGuid()) "jpg"
        let withPhoto = storage.UpdateLocationPhoto("GARAGE", Some photo)
        Assert.True(withPhoto.IsSome)
        Assert.True(withPhoto.Value.Photo.IsSome)
        let cleared = storage.UpdateLocationPhoto("GARAGE", None)
        Assert.True(cleared.IsSome)
        Assert.True(cleared.Value.Photo.IsNone)
    )

[<Fact>]
let ``UpdateLocationPhoto returns None for unknown location`` () : unit =
    withStorage (fun storage ->
        Assert.True((storage.UpdateLocationPhoto("NOPE", None)).IsNone)
    )

[<Fact>]
let ``UpdateLocationCode renames and keeps boxes assigned`` () : unit =
    withStorage (fun storage ->
        storage.CreateLocation(makeCode "OLD", makeName "Old Name") |> ignore
        let box = storage.CreateBox(None)
        storage.RecordMove("box", boxId box, Some "location", Some "OLD") |> ignore

        let result = storage.UpdateLocationCode("OLD", makeCode "NEW")
        match result with
        | Ok loc -> Assert.Equal("NEW", BoxTracker.LocationCode.value loc.Code)
        | Error e -> Assert.Fail($"Expected Ok, got {e}")

        // The box's latest move now points at the renamed location.
        let fetched = storage.GetBox(boxId box)
        match fetched.Value.Placement with
        | AtLocation code -> Assert.Equal("NEW", BoxTracker.LocationCode.value code)
        | _ -> Assert.Fail("Expected AtLocation NEW")

        // The old code no longer resolves.
        Assert.True((storage.GetLocation("OLD")).IsNone)
        Assert.Equal(1, storage.GetAssignedBoxCount("NEW"))
    )

[<Fact>]
let ``UpdateLocationCode rejects a code already in use`` () : unit =
    withStorage (fun storage ->
        storage.CreateLocation(makeCode "ONE", makeName "One") |> ignore
        storage.CreateLocation(makeCode "TWO", makeName "Two") |> ignore
        Assert.True(Result.isError (storage.UpdateLocationCode("ONE", makeCode "TWO")))
    )

[<Fact>]
let ``UpdateLocationCode errors for an unknown location`` () : unit =
    withStorage (fun storage ->
        Assert.True(Result.isError (storage.UpdateLocationCode("MISSING", makeCode "NEW")))
    )

[<Fact>]
let ``SearchItems reflects a location code rename`` () : unit =
    withStorage (fun storage ->
        storage.CreateLocation(makeCode "OLD", makeName "Storage Room") |> ignore
        let box = storage.CreateBox(None)
        storage.RecordMove("box", boxId box, Some "location", Some "OLD") |> ignore
        storage.AddItem(boxId box, makeItemName "Lamp", None) |> ignore

        storage.UpdateLocationCode("OLD", makeCode "NEW") |> ignore

        let results = storage.SearchItems(Some "Lamp")
        Assert.Single(results) |> ignore
        Assert.Equal(Some "NEW", results.[0].LocationCode)
    )

[<Fact>]
let ``GetItemSearchResult returns box and location names`` () : unit =
    withStorage (fun storage ->
        storage.CreateLocation(makeCode "GARAGE", makeName "Garage") |> ignore
        let box = storage.CreateBox(makeLabel "Tools")
        storage.RecordMove("box", boxId box, Some "location", Some "GARAGE") |> ignore
        let item = storage.AddItem(boxId box, makeItemName "Drill", None)

        let result = storage.GetItemSearchResult(item.Id.ToString())
        Assert.True(result.IsSome)
        let r = result.Value
        Assert.Equal("Drill", r.ItemName)
        Assert.Equal(boxId box, r.BoxId)
        Assert.Equal(Some "Tools", r.BoxLabel)
        Assert.Equal(Some "GARAGE", r.LocationCode)
        Assert.Equal(Some "Garage", r.LocationName)
    )

[<Fact>]
let ``GetItemSearchResult handles an unassigned item`` () : unit =
    withStorage (fun storage ->
        let item = storage.CreateItem(makeItemName "Loose Cable", None)

        let result = storage.GetItemSearchResult(item.Id.ToString())
        Assert.True(result.IsSome)
        let r = result.Value
        Assert.Equal("Loose Cable", r.ItemName)
        Assert.Equal("", r.BoxId)
        Assert.Equal(None, r.BoxLabel)
        Assert.Equal(None, r.LocationCode)
    )

[<Fact>]
let ``GetItemSearchResult returns None for an unknown item`` () : unit =
    withStorage (fun storage ->
        Assert.True((storage.GetItemSearchResult(Guid.NewGuid().ToString())).IsNone)
    )
