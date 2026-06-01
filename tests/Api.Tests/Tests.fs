module BoxTracker.Api.Tests.Tests

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
let ``Storage.ListLocations returns empty initially`` () : unit =
    withStorage (fun storage ->
        let locations : Location list = storage.ListLocations(false)
        Assert.Empty(locations)
    )

[<Fact>]
let ``Storage.CreateLocation and GetLocation roundtrip`` () : unit =
    withStorage (fun storage ->
        let code = makeCode "GARAGE"
        let name = makeName "Main Garage"
        let loc : Location = storage.CreateLocation(code, name)
        Assert.Equal("GARAGE", BoxTracker.LocationCode.value loc.Code)
        Assert.Equal("Main Garage", BoxTracker.LocationName.value loc.Name)
        Assert.False(loc.IsArchived)

        let fetched : Location option = storage.GetLocation("GARAGE")
        Assert.True(fetched.IsSome)
        Assert.Equal("Main Garage", BoxTracker.LocationName.value fetched.Value.Name)
    )

[<Fact>]
let ``Storage.ListLocations excludes archived`` () : unit =
    withStorage (fun storage ->
        storage.CreateLocation(makeCode "LOC1", makeName "Active") |> ignore
        storage.CreateLocation(makeCode "LOC2", makeName "ToArchive") |> ignore
        storage.SetLocationArchived("LOC2") |> ignore
        let active : Location list = storage.ListLocations(false)
        Assert.Single(active) |> ignore
        Assert.Equal("LOC1", BoxTracker.LocationCode.value active.[0].Code)
    )

[<Fact>]
let ``Storage.ListLocations includes archived when requested`` () : unit =
    withStorage (fun storage ->
        storage.CreateLocation(makeCode "LOC1", makeName "Active") |> ignore
        storage.CreateLocation(makeCode "LOC2", makeName "ToArchive") |> ignore
        storage.SetLocationArchived("LOC2")
        let all : Location list = storage.ListLocations(true)
        Assert.Equal(2, all.Length)
    )

[<Fact>]
let ``Storage.UpdateLocationName updates name`` () : unit =
    withStorage (fun storage ->
        storage.CreateLocation(makeCode "LOC1", makeName "Original") |> ignore
        let updated : Location option = storage.UpdateLocationName("LOC1", makeName "Updated")
        Assert.True(updated.IsSome)
        Assert.Equal("Updated", BoxTracker.LocationName.value updated.Value.Name)
    )

[<Fact>]
let ``Storage.GetLocation returns None for unknown code`` () : unit =
    withStorage (fun storage ->
        let result : Location option = storage.GetLocation("UNKNOWN")
        Assert.True(result.IsNone)
    )

[<Fact>]
let ``Storage.CreateBox auto-increments ID`` () : unit =
    withStorage (fun storage ->
        let box1 : Box = storage.CreateBox(None)
        let box2 : Box = storage.CreateBox(makeLabel "Kitchen")
        Assert.Equal("BOX-001", boxId box1)
        Assert.Equal("BOX-002", boxId box2)
        Assert.Equal(None, box1.Label)
        Assert.True(box2.Label.IsSome)
    )

[<Fact>]
let ``Storage.CreateBox starts unassigned`` () : unit =
    withStorage (fun storage ->
        let box : Box = storage.CreateBox(None)
        Assert.Equal(Unassigned, box.Placement)
    )

[<Fact>]
let ``Storage.ListBoxes returns all boxes`` () : unit =
    withStorage (fun storage ->
        storage.CreateBox(None) |> ignore
        storage.CreateBox(None) |> ignore
        let boxes : Box list = storage.ListBoxes(None, false)
        Assert.Equal(2, boxes.Length)
    )

[<Fact>]
let ``Storage.ListBoxes filters by location via moves`` () : unit =
    withStorage (fun storage ->
        storage.CreateLocation(makeCode "GARAGE", makeName "Garage") |> ignore
        let box1 : Box = storage.CreateBox(None)
        storage.CreateBox(None) |> ignore
        storage.RecordMove("box", boxId box1, Some "location", Some "GARAGE") |> ignore
        let garageBoxes : Box list = storage.ListBoxes(Some "GARAGE", false)
        Assert.Single(garageBoxes) |> ignore
        Assert.Equal("BOX-001", boxId garageBoxes.[0])
    )

[<Fact>]
let ``Storage.ListBoxes returns unassigned boxes`` () : unit =
    withStorage (fun storage ->
        storage.CreateLocation(makeCode "GARAGE", makeName "Garage") |> ignore
        let box1 : Box = storage.CreateBox(None)
        storage.CreateBox(None) |> ignore
        storage.RecordMove("box", boxId box1, Some "location", Some "GARAGE") |> ignore
        let unassigned : Box list = storage.ListBoxes(None, true)
        Assert.Single(unassigned) |> ignore
        Assert.Equal("BOX-002", boxId unassigned.[0])
    )

[<Fact>]
let ``Storage.RecordMove assigns box to location`` () : unit =
    withStorage (fun storage ->
        storage.CreateLocation(makeCode "GARAGE", makeName "Garage") |> ignore
        let box : Box = storage.CreateBox(None)
        let move : Move = storage.RecordMove("box", boxId box, Some "location", Some "GARAGE")
        Assert.Equal("box", move.EntityType)
        Assert.Equal(boxId box, move.EntityId)
        match move.To with
        | AtLocation code -> Assert.Equal("GARAGE", BoxTracker.LocationCode.value code)
        | _ -> Assert.Fail("Expected AtLocation")

        let fetched : Box option = storage.GetBox(boxId box)
        Assert.True(fetched.IsSome)
        match fetched.Value.Placement with
        | AtLocation code -> Assert.Equal("GARAGE", BoxTracker.LocationCode.value code)
        | _ -> Assert.Fail("Expected AtLocation")
    )

[<Fact>]
let ``Storage.RecordMove unassigns box from location`` () : unit =
    withStorage (fun storage ->
        storage.CreateLocation(makeCode "GARAGE", makeName "Garage") |> ignore
        let box : Box = storage.CreateBox(None)
        storage.RecordMove("box", boxId box, Some "location", Some "GARAGE") |> ignore
        storage.RecordMove("box", boxId box, None, None) |> ignore
        let fetched : Box option = storage.GetBox(boxId box)
        Assert.True(fetched.IsSome)
        Assert.Equal(Unassigned, fetched.Value.Placement)
    )

[<Fact>]
let ``Storage.RecordMove moves box between locations`` () : unit =
    withStorage (fun storage ->
        storage.CreateLocation(makeCode "LOC1", makeName "Location 1") |> ignore
        storage.CreateLocation(makeCode "LOC2", makeName "Location 2") |> ignore
        let box : Box = storage.CreateBox(None)
        storage.RecordMove("box", boxId box, Some "location", Some "LOC1") |> ignore
        storage.RecordMove("box", boxId box, Some "location", Some "LOC2") |> ignore
        let fetched : Box option = storage.GetBox(boxId box)
        Assert.True(fetched.IsSome)
        match fetched.Value.Placement with
        | AtLocation code -> Assert.Equal("LOC2", BoxTracker.LocationCode.value code)
        | _ -> Assert.Fail("Expected AtLocation")
    )

[<Fact>]
let ``Storage.AddItem places item in box via move`` () : unit =
    withStorage (fun storage ->
        let box : Box = storage.CreateBox(None)
        let item : Item = storage.AddItem(boxId box, makeItemName "Wrench", None)
        Assert.Equal("Wrench", BoxTracker.ItemName.value item.Name)
        match item.Placement with
        | InBox bid -> Assert.Equal(box.Id, bid)
        | _ -> Assert.Fail("Expected InBox")

        let items : Item list = storage.GetItemsForBox(boxId box)
        Assert.Single(items) |> ignore
        Assert.Equal("Wrench", BoxTracker.ItemName.value items.[0].Name)
    )

[<Fact>]
let ``Storage.AddItem with photo path`` () : unit =
    withStorage (fun storage ->
        let box : Box = storage.CreateBox(None)
        let photo : BoxTracker.PhotoPath.PhotoPath = BoxTracker.PhotoPath.create (boxId box) (Guid.NewGuid()) "jpg"
        let item : Item = storage.AddItem(boxId box, makeItemName "Hammer", Some photo)
        Assert.True(item.Photo.IsSome)
        Assert.Equal(BoxTracker.PhotoPath.value photo, BoxTracker.PhotoPath.value item.Photo.Value)
    )

[<Fact>]
let ``Storage.CreateItem creates standalone item`` () : unit =
    withStorage (fun storage ->
        let item : Item = storage.CreateItem(makeItemName "Widget", None)
        Assert.Equal("Widget", BoxTracker.ItemName.value item.Name)
        Assert.Equal(Unassigned, item.Placement)

        let fetched : Item option = storage.GetItem(item.Id.ToString())
        Assert.True(fetched.IsSome)
        Assert.Equal(Unassigned, fetched.Value.Placement)
    )

[<Fact>]
let ``Storage.RecordMove moves item between boxes`` () : unit =
    withStorage (fun storage ->
        let box1 : Box = storage.CreateBox(None)
        let box2 : Box = storage.CreateBox(None)
        let item : Item = storage.AddItem(boxId box1, makeItemName "Drill", None)
        storage.RecordMove("item", item.Id.ToString(), Some "box", Some (boxId box2)) |> ignore

        let inBox1 : Item list = storage.GetItemsForBox(boxId box1)
        Assert.Empty(inBox1)

        let inBox2 : Item list = storage.GetItemsForBox(boxId box2)
        Assert.Single(inBox2) |> ignore
        Assert.Equal("Drill", BoxTracker.ItemName.value inBox2.[0].Name)
    )

[<Fact>]
let ``Storage.RecordMove unassigns item from box`` () : unit =
    withStorage (fun storage ->
        let box : Box = storage.CreateBox(None)
        let item : Item = storage.AddItem(boxId box, makeItemName "Drill", None)
        storage.RecordMove("item", item.Id.ToString(), None, None) |> ignore

        let inBox : Item list = storage.GetItemsForBox(boxId box)
        Assert.Empty(inBox)

        let fetched : Item option = storage.GetItem(item.Id.ToString())
        Assert.True(fetched.IsSome)
        Assert.Equal(Unassigned, fetched.Value.Placement)
    )

[<Fact>]
let ``Storage.UpdateItemName updates name`` () : unit =
    withStorage (fun storage ->
        let box : Box = storage.CreateBox(None)
        let item : Item = storage.AddItem(boxId box, makeItemName "Old Name", None)
        let updated : Item option = storage.UpdateItemName(item.Id.ToString(), makeItemName "New Name")
        Assert.True(updated.IsSome)
        Assert.Equal("New Name", BoxTracker.ItemName.value updated.Value.Name)
    )

[<Fact>]
let ``Storage.DeleteItem removes item and its moves`` () : unit =
    withStorage (fun storage ->
        let box : Box = storage.CreateBox(None)
        let item : Item = storage.AddItem(boxId box, makeItemName "ToDelete", None)
        let photoPath : string option = storage.DeleteItem(item.Id.ToString())
        Assert.True(photoPath.IsNone)
        Assert.Empty(storage.GetItemsForBox(boxId box))
        Assert.True(storage.GetItem(item.Id.ToString()).IsNone)
    )

[<Fact>]
let ``Storage.DeleteItem returns photo path`` () : unit =
    withStorage (fun storage ->
        let box : Box = storage.CreateBox(None)
        let photo : BoxTracker.PhotoPath.PhotoPath = BoxTracker.PhotoPath.create (boxId box) (Guid.NewGuid()) "jpg"
        let item : Item = storage.AddItem(boxId box, makeItemName "WithPhoto", Some photo)
        let returnedPath : string option = storage.DeleteItem(item.Id.ToString())
        Assert.True(returnedPath.IsSome)
        Assert.Equal(BoxTracker.PhotoPath.value photo, returnedPath.Value)
    )

[<Fact>]
let ``Storage.DeleteBox unassigns items`` () : unit =
    withStorage (fun storage ->
        let box : Box = storage.CreateBox(None)
        storage.AddItem(boxId box, makeItemName "Item1", None) |> ignore
        storage.AddItem(boxId box, makeItemName "Item2", None) |> ignore
        let paths : string list = storage.DeleteBox(boxId box)
        Assert.Empty(paths)
    )

[<Fact>]
let ``Storage.DeleteBox returns photo paths from items`` () : unit =
    withStorage (fun storage ->
        let box : Box = storage.CreateBox(None)
        let photo1 : BoxTracker.PhotoPath.PhotoPath = BoxTracker.PhotoPath.create (boxId box) (Guid.NewGuid()) "jpg"
        let photo2 : BoxTracker.PhotoPath.PhotoPath = BoxTracker.PhotoPath.create (boxId box) (Guid.NewGuid()) "png"
        storage.AddItem(boxId box, makeItemName "Item1", Some photo1) |> ignore
        storage.AddItem(boxId box, makeItemName "Item2", Some photo2) |> ignore
        let paths : string list = storage.DeleteBox(boxId box)
        Assert.Equal(2, paths.Length)
    )

[<Fact>]
let ``Storage.GetMoveHistory returns moves for entity`` () : unit =
    withStorage (fun storage ->
        storage.CreateLocation(makeCode "LOC1", makeName "Location 1") |> ignore
        storage.CreateLocation(makeCode "LOC2", makeName "Location 2") |> ignore
        let box : Box = storage.CreateBox(None)
        storage.RecordMove("box", boxId box, Some "location", Some "LOC1") |> ignore
        storage.RecordMove("box", boxId box, Some "location", Some "LOC2") |> ignore
        let history : Move list = storage.GetMoveHistory("box", boxId box)
        Assert.Equal(2, history.Length)
        match history.[0].To with
        | AtLocation code -> Assert.Equal("LOC2", BoxTracker.LocationCode.value code)
        | _ -> Assert.Fail("Expected AtLocation")
        match history.[1].To with
        | AtLocation code -> Assert.Equal("LOC1", BoxTracker.LocationCode.value code)
        | _ -> Assert.Fail("Expected AtLocation")
    )

[<Fact>]
let ``Storage.SearchItems finds items by name`` () : unit =
    withStorage (fun storage ->
        storage.CreateLocation(makeCode "GARAGE", makeName "Garage") |> ignore
        let box : Box = storage.CreateBox(makeLabel "Tools")
        storage.RecordMove("box", boxId box, Some "location", Some "GARAGE") |> ignore
        storage.AddItem(boxId box, makeItemName "Christmas decorations", None) |> ignore
        let results : SearchResult list = storage.SearchItems(Some "christmas")
        Assert.Single(results) |> ignore
        Assert.Equal("Christmas decorations", results.[0].ItemName)
        Assert.Equal(boxId box, results.[0].BoxId)
    )

[<Fact>]
let ``Storage.SearchItems finds items by location name`` () : unit =
    withStorage (fun storage ->
        storage.CreateLocation(makeCode "GARAGE", makeName "Garage") |> ignore
        let box : Box = storage.CreateBox(None)
        storage.RecordMove("box", boxId box, Some "location", Some "GARAGE") |> ignore
        storage.AddItem(boxId box, makeItemName "Wrench", None) |> ignore
        let results : SearchResult list = storage.SearchItems(Some "garage")
        Assert.True(results.Length >= 1)
    )

[<Fact>]
let ``Storage.SearchItems returns all items when no query`` () : unit =
    withStorage (fun storage ->
        let box : Box = storage.CreateBox(None)
        storage.AddItem(boxId box, makeItemName "Wrench", None) |> ignore
        storage.AddItem(boxId box, makeItemName "Hammer", None) |> ignore
        let results : SearchResult list = storage.SearchItems(None)
        Assert.Equal(2, results.Length)
    )

[<Fact>]
let ``Storage.SearchItems returns empty for no match`` () : unit =
    withStorage (fun storage ->
        let box : Box = storage.CreateBox(None)
        storage.AddItem(boxId box, makeItemName "Wrench", None) |> ignore
        let results : SearchResult list = storage.SearchItems(Some "xyznonexistent")
        Assert.Empty(results)
    )

[<Fact>]
let ``Storage.SearchItems updates when item name changes`` () : unit =
    withStorage (fun storage ->
        let box : Box = storage.CreateBox(None)
        let item : Item = storage.AddItem(boxId box, makeItemName "Old Name", None)
        storage.UpdateItemName(item.Id.ToString(), makeItemName "New Name") |> ignore
        let oldResults : SearchResult list = storage.SearchItems(Some "Old")
        Assert.Empty(oldResults)
        let newResults : SearchResult list = storage.SearchItems(Some "New")
        Assert.Single(newResults) |> ignore
    )

[<Fact>]
let ``Storage.SearchItems updates when box location changes`` () : unit =
    withStorage (fun storage ->
        storage.CreateLocation(makeCode "ATTIC", makeName "Attic") |> ignore
        let box : Box = storage.CreateBox(None)
        storage.AddItem(boxId box, makeItemName "Wrench", None) |> ignore
        storage.SearchItems(Some "attic") |> ignore
        storage.RecordMove("box", boxId box, Some "location", Some "ATTIC") |> ignore
        let results : SearchResult list = storage.SearchItems(Some "attic")
        Assert.Single(results) |> ignore
        Assert.Equal(Some "Attic", results.[0].LocationName)
    )

[<Fact>]
let ``Storage.SearchItems removes deleted items`` () : unit =
    withStorage (fun storage ->
        let box : Box = storage.CreateBox(None)
        let item : Item = storage.AddItem(boxId box, makeItemName "ToDelete", None)
        storage.DeleteItem(item.Id.ToString()) |> ignore
        let results : SearchResult list = storage.SearchItems(Some "ToDelete")
        Assert.Empty(results)
    )

[<Fact>]
let ``Storage.GetAssignedBoxCount returns correct count`` () : unit =
    withStorage (fun storage ->
        storage.CreateLocation(makeCode "LOC1", makeName "Test") |> ignore
        let box1 : Box = storage.CreateBox(None)
        storage.CreateBox(None) |> ignore
        storage.RecordMove("box", boxId box1, Some "location", Some "LOC1") |> ignore
        let count : int = storage.GetAssignedBoxCount("LOC1")
        Assert.Equal(1, count)
    )
