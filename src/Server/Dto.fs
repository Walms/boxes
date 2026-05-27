module BoxTracker.Dto

open System
open BoxTracker.Types
open BoxTracker.LocationCode
open BoxTracker.LocationName
open BoxTracker.BoxId
open BoxTracker.BoxLabel
open BoxTracker.ItemName
open BoxTracker.PhotoPath
open BoxTracker.Container
open BoxTracker.Storage

type BoxTrackerConfig = {
    DataDir: string
}

type LocationResponse = {
    Code: string
    Name: string
    IsArchived: bool
    CreatedAt: DateTimeOffset
}

type BoxResponse = {
    Id: string
    Label: string option
    LocationCode: string option
    CreatedAt: DateTimeOffset
}

type ItemResponse = {
    Id: string
    BoxId: string option
    Name: string
    PhotoPath: string option
    AddedAt: DateTimeOffset
}

type SearchResultResponse = {
    ItemId: string
    ItemName: string
    PhotoPath: string option
    BoxId: string
    BoxLabel: string option
    LocationCode: string option
    LocationName: string option
}

type MoveResponse = {
    Id: string
    EntityType: string
    EntityId: string
    ToType: string option
    ToId: string option
    MovedAt: DateTimeOffset
}

type LocationDetailResponse = {
    Location: LocationResponse
    Boxes: BoxResponse list
}

type BoxDetailResponse = {
    Box: BoxResponse
    Items: ItemResponse list
}

type ItemDetailResponse = {
    Item: ItemResponse
}

type CreateLocationRequest = {
    Code: string
    Name: string
}

type UpdateLocationRequest = {
    Name: string
}

type CreateBoxRequest = {
    Label: string
}

type UpdateBoxRequest = {
    Label: string
    LocationCode: string
}

type CreateItemRequest = {
    Name: string
    BoxId: string
}

type UpdateItemRequest = {
    Name: string
}

type MoveRequest = {
    EntityType: string
    EntityId: string
    ToType: string
    ToId: string
}

let locationToDto (loc: Location) : LocationResponse = {
    Code = LocationCode.value loc.Code
    Name = LocationName.value loc.Name
    IsArchived = loc.IsArchived
    CreatedAt = loc.CreatedAt
}

let boxToDto (box: Box) : BoxResponse = {
    Id = BoxId.value box.Id
    Label = box.Label |> Option.map BoxLabel.value
    LocationCode =
        match box.Placement with
        | AtLocation code -> Some(LocationCode.value code)
        | _ -> None
    CreatedAt = box.CreatedAt
}

let itemToDto (item: Item) : ItemResponse = {
    Id = item.Id.ToString()
    BoxId =
        match item.Placement with
        | InBox boxId -> Some(BoxId.value boxId)
        | _ -> None
    Name = ItemName.value item.Name
    PhotoPath = item.Photo |> Option.map PhotoPath.value
    AddedAt = item.AddedAt
}

let searchResultToDto (r: SearchResult) : SearchResultResponse = {
    ItemId = r.ItemId.ToString()
    ItemName = r.ItemName
    PhotoPath = r.PhotoPath
    BoxId = r.BoxId
    BoxLabel = r.BoxLabel
    LocationCode = r.LocationCode
    LocationName = r.LocationName
}

let moveToDto (m: Move) : MoveResponse = {
    Id = m.Id.ToString()
    EntityType = m.EntityType
    EntityId = m.EntityId
    ToType =
        match m.To with
        | InBox _ -> Some "box"
        | AtLocation _ -> Some "location"
        | Unassigned -> None
    ToId =
        match m.To with
        | InBox boxId -> Some(BoxId.value boxId)
        | AtLocation code -> Some(LocationCode.value code)
        | Unassigned -> None
    MovedAt = m.MovedAt
}
