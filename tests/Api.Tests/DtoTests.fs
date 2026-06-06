module BoxTracker.Api.Tests.DtoTests

open System
open Xunit
open BoxTracker.Types
open BoxTracker.Container
open BoxTracker.Dto
open BoxTracker.Storage
open BoxTracker.PhotoJobStore

let private code (s: string) : BoxTracker.LocationCode.LocationCode =
    BoxTracker.LocationCode.create s |> Result.defaultWith (fun e -> failwith e)

let private name (s: string) : BoxTracker.LocationName.LocationName =
    BoxTracker.LocationName.create s |> Result.defaultWith (fun e -> failwith e)

let private label (s: string) : BoxTracker.BoxLabel.BoxLabel option =
    BoxTracker.BoxLabel.create s |> Result.defaultWith (fun e -> failwith e)

let private itemName (s: string) : BoxTracker.ItemName.ItemName =
    BoxTracker.ItemName.create s |> Result.defaultWith (fun e -> failwith e)

[<Fact>]
let ``locationToDto maps all fields`` () : unit =
    let photo = BoxTracker.PhotoPath.create "GARAGE" (Guid.NewGuid()) "jpg"
    let created = DateTimeOffset.UtcNow
    let loc : Location = {
        Code = code "GARAGE"
        Name = name "Main Garage"
        IsArchived = true
        Photo = Some photo
        CreatedAt = created
    }
    let dto = locationToDto loc
    Assert.Equal("GARAGE", dto.Code)
    Assert.Equal("Main Garage", dto.Name)
    Assert.True(dto.IsArchived)
    Assert.Equal(Some (BoxTracker.PhotoPath.value photo), dto.PhotoPath)
    Assert.Equal(created, dto.CreatedAt)

[<Fact>]
let ``locationToDto maps None photo`` () : unit =
    let loc : Location = {
        Code = code "ATTIC"
        Name = name "Attic"
        IsArchived = false
        Photo = None
        CreatedAt = DateTimeOffset.UtcNow
    }
    let dto = locationToDto loc
    Assert.Equal(None, dto.PhotoPath)

[<Fact>]
let ``boxToDto maps location placement to LocationCode`` () : unit =
    let box : Box = {
        Id = BoxTracker.BoxId.create 1
        Label = label "Tools"
        Photo = None
        Placement = AtLocation(code "GARAGE")
        CreatedAt = DateTimeOffset.UtcNow
    }
    let dto = boxToDto box
    Assert.Equal("BOX-001", dto.Id)
    Assert.Equal(Some "Tools", dto.Label)
    Assert.Equal(Some "GARAGE", dto.LocationCode)

[<Fact>]
let ``boxToDto leaves LocationCode None when unassigned`` () : unit =
    let box : Box = {
        Id = BoxTracker.BoxId.create 2
        Label = None
        Photo = None
        Placement = Unassigned
        CreatedAt = DateTimeOffset.UtcNow
    }
    let dto = boxToDto box
    Assert.Equal(None, dto.Label)
    Assert.Equal(None, dto.LocationCode)

[<Fact>]
let ``boxToDto leaves LocationCode None when in another box`` () : unit =
    // Boxes can't really live in boxes, but the DTO mapper must not crash on it.
    let box : Box = {
        Id = BoxTracker.BoxId.create 3
        Label = None
        Photo = None
        Placement = InBox(BoxTracker.BoxId.create 1)
        CreatedAt = DateTimeOffset.UtcNow
    }
    Assert.Equal(None, (boxToDto box).LocationCode)

[<Fact>]
let ``itemToDto maps InBox placement to BoxId`` () : unit =
    let item : Item = {
        Id = Guid.NewGuid()
        Name = itemName "Wrench"
        Photo = None
        Placement = InBox(BoxTracker.BoxId.create 5)
        AddedAt = DateTimeOffset.UtcNow
    }
    let dto = itemToDto item
    Assert.Equal("Wrench", dto.Name)
    Assert.Equal(Some "BOX-005", dto.BoxId)

[<Fact>]
let ``itemToDto leaves BoxId None when unassigned`` () : unit =
    let item : Item = {
        Id = Guid.NewGuid()
        Name = itemName "Widget"
        Photo = None
        Placement = Unassigned
        AddedAt = DateTimeOffset.UtcNow
    }
    Assert.Equal(None, (itemToDto item).BoxId)

[<Fact>]
let ``moveToDto maps box destination`` () : unit =
    let move : Move = {
        Id = Guid.NewGuid()
        EntityType = "item"
        EntityId = Guid.NewGuid().ToString()
        To = InBox(BoxTracker.BoxId.create 9)
        MovedAt = DateTimeOffset.UtcNow
    }
    let dto = moveToDto move
    Assert.Equal(Some "box", dto.ToType)
    Assert.Equal(Some "BOX-009", dto.ToId)

[<Fact>]
let ``moveToDto maps location destination`` () : unit =
    let move : Move = {
        Id = Guid.NewGuid()
        EntityType = "box"
        EntityId = "BOX-001"
        To = AtLocation(code "GARAGE")
        MovedAt = DateTimeOffset.UtcNow
    }
    let dto = moveToDto move
    Assert.Equal(Some "location", dto.ToType)
    Assert.Equal(Some "GARAGE", dto.ToId)

[<Fact>]
let ``moveToDto maps unassigned destination to None`` () : unit =
    let move : Move = {
        Id = Guid.NewGuid()
        EntityType = "box"
        EntityId = "BOX-001"
        To = Unassigned
        MovedAt = DateTimeOffset.UtcNow
    }
    let dto = moveToDto move
    Assert.Equal(None, dto.ToType)
    Assert.Equal(None, dto.ToId)

[<Fact>]
let ``photoJobToDto exposes PhotoPath only when completed`` () : unit =
    let baseJob : PhotoJob = {
        Id = Guid.NewGuid().ToString()
        EntityType = "box"
        EntityId = "BOX-001"
        Status = StatusPending
        Error = None
        SourcePath = "uploads/x"
        PhotoPath = "photos/BOX-001/abc"
        OldPhotoPath = None
        CreatedAt = DateTimeOffset.UtcNow
        UpdatedAt = DateTimeOffset.UtcNow
    }
    Assert.Equal(None, (photoJobToDto baseJob).PhotoPath)
    let completed = { baseJob with Status = StatusCompleted }
    Assert.Equal(Some "photos/BOX-001/abc", (photoJobToDto completed).PhotoPath)

[<Fact>]
let ``photoJobToDto carries error through`` () : unit =
    let job : PhotoJob = {
        Id = Guid.NewGuid().ToString()
        EntityType = "item"
        EntityId = "abc"
        Status = StatusFailed
        Error = Some "boom"
        SourcePath = "uploads/x"
        PhotoPath = "photos/x/abc"
        OldPhotoPath = None
        CreatedAt = DateTimeOffset.UtcNow
        UpdatedAt = DateTimeOffset.UtcNow
    }
    let dto = photoJobToDto job
    Assert.Equal("failed", dto.Status)
    Assert.Equal(Some "boom", dto.Error)
    Assert.Equal(None, dto.PhotoPath)

[<Fact>]
let ``searchResultToDto maps fields including optionals`` () : unit =
    let id = Guid.NewGuid()
    let r : SearchResult = {
        ItemId = id
        ItemName = "Hammer"
        PhotoPath = Some "photos/BOX-001/p"
        BoxId = "BOX-001"
        BoxLabel = Some "Tools"
        LocationCode = Some "GARAGE"
        LocationName = Some "Garage"
        AddedAt = DateTimeOffset.UtcNow
    }
    let dto = searchResultToDto r
    Assert.Equal(id.ToString(), dto.ItemId)
    Assert.Equal("Hammer", dto.ItemName)
    Assert.Equal(Some "Tools", dto.BoxLabel)
    Assert.Equal(Some "GARAGE", dto.LocationCode)
