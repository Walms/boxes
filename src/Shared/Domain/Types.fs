module BoxTracker.Types

open System
open BoxTracker.LocationCode
open BoxTracker.LocationName
open BoxTracker.BoxId
open BoxTracker.BoxLabel
open BoxTracker.ItemName
open BoxTracker.PhotoPath
open BoxTracker.Container

type Location = {
    Code: LocationCode
    Name: LocationName
    IsArchived: bool
    CreatedAt: DateTimeOffset
}

type Item = {
    Id: Guid
    Name: ItemName
    Photo: PhotoPath option
    Placement: Container
    AddedAt: DateTimeOffset
}

type Box = {
    Id: BoxId
    Label: BoxLabel option
    Placement: Container
    CreatedAt: DateTimeOffset
}

type Move = {
    Id: Guid
    EntityType: string
    EntityId: string
    To: Container
    MovedAt: DateTimeOffset
}
