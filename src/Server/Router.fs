module BoxTracker.Router

open Giraffe
open BoxTracker.Handlers.LocationHandlers
open BoxTracker.Handlers.BoxHandlers
open BoxTracker.Handlers.ItemHandlers
open BoxTracker.Handlers.MoveHandlers
open BoxTracker.Handlers.LabelHandlers
open BoxTracker.Handlers.PhotoJobHandlers

let webApp : HttpHandler =
    choose [
        GET >=> route "/api/locations" >=> listLocations
        POST >=> route "/api/locations" >=> createLocation
        GET >=> routef "/api/locations/%s" getLocation
        PUT >=> routef "/api/locations/%s" updateLocation
        PATCH >=> routef "/api/locations/%s/code" updateLocationCode
        DELETE >=> routef "/api/locations/%s" archiveLocation
        POST >=> routef "/api/locations/%s/photo" uploadLocationPhoto

        GET >=> route "/api/boxes" >=> listBoxes
        POST >=> route "/api/boxes" >=> createBox
        GET >=> routef "/api/boxes/%s" getBox
        PUT >=> routef "/api/boxes/%s" updateBox
        DELETE >=> routef "/api/boxes/%s" deleteBox
        POST >=> routef "/api/boxes/%s/photo" uploadBoxPhoto

        POST >=> route "/api/items" >=> createItem
        GET >=> routef "/api/items/%s" getItem
        PUT >=> routef "/api/items/%s" updateItemStandalone
        POST >=> routef "/api/items/%s/photo" updateItemPhoto
        DELETE >=> routef "/api/items/%s" deleteItemStandalone

        POST >=> routef "/api/boxes/%s/items" addItem
        PUT >=> routef "/api/boxes/%s/items" (fun boxId ->
            choose [
                routef "/%s" (fun itemId -> updateItem boxId itemId)
            ])
        DELETE >=> routef "/api/boxes/%s/items" (fun boxId ->
            choose [
                routef "/%s" (fun itemId -> deleteItem boxId itemId)
            ])

        GET >=> route "/api/items" >=> searchItems

        GET >=> routef "/api/photo-jobs/%s" getPhotoJob

        POST >=> route "/api/moves" >=> recordMove
        GET >=> route "/api/moves" >=> getMoveHistory

        GET >=> routef "/api/boxes/%s/label" boxLabel
        GET >=> routef "/api/locations/%s/label" locationLabel
        GET >=> route "/api/boxes/labels" >=> batchBoxLabels
    ]
