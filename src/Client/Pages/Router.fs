module BoxTracker.Client.Pages.Router

open Feliz
open BoxTracker.Client.State
open BoxTracker.Client.Pages.Common
open BoxTracker.Client.Pages.Locations
open BoxTracker.Client.Pages.Boxes
open BoxTracker.Client.Pages.Items

let renderPage (state: State) (dispatch: Msg -> unit) : ReactElement =
    Html.div [
        prop.className "min-h-screen bg-base-100"
        prop.children [
            imageViewer state dispatch
            historyModal state dispatch
            scannerModal state dispatch
            navbar state dispatch
            Html.div [
                prop.className "app-main w-full mx-auto px-3 sm:px-4 md:px-6 py-3 sm:py-4 max-w-6xl"
                prop.children [
                    errorAlert state dispatch
                    match state.CurrentPage with
                    | LocationsList -> locationsPage state dispatch
                    | LocationDetail _ -> locationDetailPage state dispatch
                    | BoxesList -> boxesPage state dispatch
                    | BoxDetail _ -> boxDetailPage state dispatch
                    | ItemsList -> itemsPage state dispatch
                    | ItemDetail _ -> itemDetailPage state dispatch
                ]
            ]
        ]
    ]
