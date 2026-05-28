module BoxTracker.Client.Pages

#if FABLE_COMPILER
open Browser.Types
#endif
open Feliz
open BoxTracker.Client.State
open BoxTracker.Client.Api

[<Fable.Core.Emit("$0.target.value")>]
let private targetValue (ev: obj) : string = failwith "JS only"

let private photoUrl (path: string option) : string option =
    path |> Option.map (fun p -> "/api/" + p)

let navbar (state: State) (dispatch: Msg -> unit) : ReactElement =
    Html.div [
        prop.className "navbar bg-base-200"
        prop.children [
            Html.div [
                prop.className "flex-none"
                prop.children [
                    Html.a [
                        prop.className "btn btn-ghost text-xl"
                        prop.text "BoxTracker"
                        prop.onClick (fun _ -> dispatch (Navigate LocationsList))
                    ]
                ]
            ]
            Html.div [
                prop.className "flex-1"
                prop.children [
                    Html.ul [
                        prop.className "menu menu-horizontal px-1"
                        prop.children [
                            Html.li [
                                Html.a [
                                    prop.text "Locations"
                                    prop.onClick (fun _ -> dispatch (Navigate LocationsList))
                                ]
                            ]
                            Html.li [
                                Html.a [
                                    prop.text "Boxes"
                                    prop.onClick (fun _ -> dispatch (Navigate BoxesList))
                                ]
                            ]
                            Html.li [
                                Html.a [
                                    prop.text "Items"
                                    prop.onClick (fun _ -> dispatch (Navigate ItemsList))
                                ]
                            ]
                            Html.li [
                                Html.a [
                                    prop.text "Search"
                                    prop.onClick (fun _ -> dispatch (Navigate ItemsSearch))
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ]

let private errorAlert (state: State) (dispatch: Msg -> unit) : ReactElement =
    match state.Error with
    | None -> Html.none
    | Some err ->
        Html.div [
            prop.className "alert alert-error"
            prop.children [
                Html.span [ prop.text err ]
                Html.button [
                    prop.className "btn btn-sm btn-ghost"
                    prop.text "x"
                    prop.onClick (fun _ -> dispatch DismissError)
                ]
            ]
        ]

let private loadingSpinner (state: State) : ReactElement =
    if state.Loading then
        Html.div [
            prop.className "flex justify-center p-8"
            prop.children [
                Html.span [ prop.className "loading loading-spinner loading-lg" ]
            ]
        ]
    else Html.none

let private moveItemDialog (state: State) (dispatch: Msg -> unit) : ReactElement =
    match state.MovingItemId with
    | None -> Html.none
    | Some _ ->
        let currentBoxId : string =
            state.BoxDetail |> Option.map (fun d -> d.Box.Id) |> Option.defaultValue ""
        Html.div [
            prop.className "modal modal-open"
            prop.children [
                Html.div [
                    prop.className "modal-box"
                    prop.children [
                        Html.h3 [
                            prop.className "font-bold text-lg mb-4"
                            prop.text "Move item to another box"
                        ]
                        Html.div [
                            prop.className "form-control"
                            prop.children [
                                Html.label [
                                    prop.className "label"
                                    prop.children [
                                        Html.span [ prop.className "label-text"; prop.text "Select target box" ]
                                    ]
                                ]
                                Html.select [
                                    prop.className "select select-bordered w-full"
                                    prop.value state.TargetBoxId
                                    prop.onChange (fun (s: string) -> dispatch (MoveTargetBoxChanged s))
                                    prop.children [
                                        Html.option [ prop.value ""; prop.text "Choose a box..." ]
                                        for box in state.AvailableBoxes do
                                            if box.Id <> currentBoxId then
                                                Html.option [
                                                    prop.value box.Id
                                                    prop.text (box.Label |> Option.defaultValue box.Id)
                                                ]
                                    ]
                                ]
                            ]
                        ]
                        Html.div [
                            prop.className "modal-action"
                            prop.children [
                                Html.button [
                                    prop.className "btn btn-ghost"
                                    prop.text "Cancel"
                                    prop.onClick (fun _ -> dispatch CancelMoveItem)
                                ]
                                Html.button [
                                    prop.className "btn btn-primary"
                                    prop.text "Move"
                                    prop.disabled (System.String.IsNullOrEmpty state.TargetBoxId)
                                    prop.onClick (fun _ -> dispatch ConfirmMoveItem)
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]

let private addExistingItemDialog (state: State) (dispatch: Msg -> unit) : ReactElement =
    if not state.AddingExistingItem then Html.none
    else
        Html.div [
            prop.className "modal modal-open"
            prop.children [
                Html.div [
                    prop.className "modal-box"
                    prop.children [
                        Html.h3 [
                            prop.className "font-bold text-lg mb-4"
                            prop.text "Add existing item to this box"
                        ]
                        if Array.isEmpty state.UnassignedItems then
                            Html.p [
                                prop.className "text-center py-4 opacity-60"
                                prop.text "No unassigned items available"
                            ]
                        else
                            Html.ul [
                                prop.className "space-y-2 max-h-80 overflow-y-auto"
                                prop.children [
                                    for item in state.UnassignedItems do
                                        Html.li [
                                            prop.className [
                                                "flex items-center gap-3 p-3 rounded-lg cursor-pointer"
                                                if state.SelectedExistingItemId = item.ItemId then "bg-primary text-primary-content"
                                                else "bg-base-300 hover:bg-base-200"
                                            ]
                                            prop.onClick (fun _ -> dispatch (SelectedExistingItemChanged item.ItemId))
                                            prop.children [
                                                match photoUrl item.PhotoPath with
                                                | Some url ->
                                                    Html.img [
                                                        prop.className "w-10 h-10 object-cover rounded flex-shrink-0"
                                                        prop.src url
                                                    ]
                                                | None ->
                                                    Html.div [
                                                        prop.className "w-10 h-10 bg-base-100 rounded flex items-center justify-center flex-shrink-0"
                                                        prop.children [
                                                            Html.span [ prop.className "text-sm opacity-30"; prop.text "?" ]
                                                        ]
                                                    ]
                                                Html.span [ prop.className "truncate"; prop.text item.ItemName ]
                                            ]
                                        ]
                                ]
                            ]
                        Html.div [
                            prop.className "modal-action"
                            prop.children [
                                Html.button [
                                    prop.className "btn btn-ghost"
                                    prop.text "Cancel"
                                    prop.onClick (fun _ -> dispatch CancelAddExistingItem)
                                ]
                                Html.button [
                                    prop.className "btn btn-primary"
                                    prop.text "Add to Box"
                                    prop.disabled (System.String.IsNullOrEmpty state.SelectedExistingItemId)
                                    prop.onClick (fun _ -> dispatch ConfirmAddExistingItem)
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]

let locationsPage (state: State) (dispatch: Msg -> unit) : ReactElement =
    Html.div [
        prop.children [
            Html.div [
                prop.className "flex items-center justify-between mb-4"
                prop.children [
                    Html.h1 [
                        prop.className "text-2xl font-bold"
                        prop.text "Locations"
                    ]
                    Html.button [
                        prop.className "btn btn-primary btn-sm"
                        prop.text "+ New Location"
                        prop.onClick (fun _ -> dispatch ShowCreateLocationForm)
                    ]
                ]
            ]
            if state.ShowCreateLocationForm then
                Html.div [
                    prop.className "card bg-base-200 mb-4"
                    prop.children [
                        Html.div [
                            prop.className "card-body"
                            prop.children [
                                Html.div [
                                    prop.className "form-control"
                                    prop.children [
                                        Html.label [
                                            prop.className "label"
                                            prop.children [
                                                Html.span [ prop.className "label-text"; prop.text "Code" ]
                                            ]
                                        ]
                                        Html.input [
                                            prop.className "input input-bordered"
                                            prop.placeholder "e.g. GARAGE"
                                            prop.value state.NewLocationCode
                                            prop.onChange (fun (s: string) -> dispatch (NewLocationCodeChanged s))
                                        ]
                                    ]
                                ]
                                Html.div [
                                    prop.className "form-control"
                                    prop.children [
                                        Html.label [
                                            prop.className "label"
                                            prop.children [
                                                Html.span [ prop.className "label-text"; prop.text "Name" ]
                                            ]
                                        ]
                                        Html.input [
                                            prop.className "input input-bordered"
                                            prop.placeholder "e.g. Garage"
                                            prop.value state.NewLocationName
                                            prop.onChange (fun (s: string) -> dispatch (NewLocationNameChanged s))
                                        ]
                                    ]
                                ]
                                Html.div [
                                    prop.className "card-actions justify-end"
                                    prop.children [
                                        Html.button [
                                            prop.className "btn btn-primary btn-sm"
                                            prop.text "Create"
                                            prop.onClick (fun _ -> dispatch SubmitCreateLocation)
                                        ]
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]
            Html.div [
                prop.className "grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4"
                prop.children [
                    if Array.isEmpty state.Locations && not state.Loading then
                        Html.div [
                            prop.className "col-span-full text-center py-8 opacity-60"
                            prop.children [
                                Html.p [ prop.className "text-lg"; prop.text "No locations yet" ]
                                Html.p [ prop.className "text-sm"; prop.text "Click \"+ New Location\" to create one" ]
                            ]
                        ]
                    for loc in state.Locations do
                        Html.div [
                            prop.className "card bg-base-200 cursor-pointer"
                            prop.onClick (fun _ -> dispatch (Navigate (LocationDetail loc.Code)))
                            prop.children [
                                Html.div [
                                    prop.className "card-body"
                                    prop.children [
                                        Html.h2 [
                                            prop.className "card-title"
                                            prop.children [
                                                Html.text loc.Name
                                                if loc.IsArchived then
                                                    Html.span [ prop.className "badge badge-ghost"; prop.text "Archived" ]
                                            ]
                                        ]
                                        Html.p [ prop.text loc.Code ]
                                    ]
                                ]
                            ]
                        ]
                ]
            ]
        ]
    ]

let locationDetailPage (state: State) (dispatch: Msg -> unit) : ReactElement =
    match state.LocationDetail with
    | None ->
        if state.Loading then loadingSpinner state
        else Html.div [ prop.text "Location not found" ]
    | Some detail ->
        Html.div [
            prop.children [
                Html.div [
                    prop.className "flex items-center gap-2 mb-4"
                    prop.children [
                        Html.button [
                            prop.className "btn btn-ghost btn-sm"
                            prop.text "< Back"
                            prop.onClick (fun _ -> dispatch (Navigate LocationsList))
                        ]
                    ]
                ]
                Html.div [
                    prop.className "card bg-base-200 mb-4"
                    prop.children [
                        Html.div [
                            prop.className "card-body"
                            prop.children [
                                Html.div [
                                    prop.className "flex items-center justify-between"
                                    prop.children [
                                        if state.EditingLocationName then
                                            Html.div [
                                                prop.className "flex items-center gap-2"
                                                prop.children [
                                                    Html.input [
                                                        prop.className "input input-bordered input-sm"
                                                        prop.value state.EditLocationNameValue
                                                        prop.onChange (fun (s: string) -> dispatch (EditLocationNameChanged s))
                                                    ]
                                                    Html.button [
                                                        prop.className "btn btn-primary btn-sm"
                                                        prop.text "Save"
                                                        prop.onClick (fun _ -> dispatch SubmitEditLocationName)
                                                    ]
                                                    Html.button [
                                                        prop.className "btn btn-ghost btn-sm"
                                                        prop.text "Cancel"
                                                        prop.onClick (fun _ -> dispatch CancelEditLocationName)
                                                    ]
                                                ]
                                            ]
                                        else
                                            Html.h1 [
                                                prop.className "text-2xl font-bold"
                                                prop.children [
                                                    Html.text detail.Location.Name
                                                    Html.span [
                                                        prop.className "badge badge-outline ml-2"
                                                        prop.text detail.Location.Code
                                                    ]
                                                ]
                                            ]
                                        if not state.EditingLocationName then
                                            Html.div [
                                                prop.className "flex gap-2"
                                                prop.children [
                                                    Html.button [
                                                        prop.className "btn btn-ghost btn-sm"
                                                        prop.text "Edit"
                                                        prop.onClick (fun _ -> dispatch StartEditLocationName)
                                                    ]
                                                    Html.button [
                                                        prop.className "btn btn-error btn-sm"
                                                        prop.text "Archive"
                                                        prop.onClick (fun _ -> dispatch ArchiveLocation)
                                                    ]
                                                ]
                                            ]
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]
                Html.h2 [
                    prop.className "text-lg font-semibold mb-2"
                    prop.text $"Boxes in this location (%i{detail.Boxes.Length})"
                ]
                Html.div [
                    prop.className "grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4"
                    prop.children [
                        if Array.isEmpty detail.Boxes then
                            Html.div [
                                prop.className "col-span-full text-center py-8 opacity-60"
                                prop.children [
                                    Html.p [ prop.text "No boxes in this location" ]
                                ]
                            ]
                        for box in detail.Boxes do
                            Html.div [
                                prop.className "card bg-base-200 cursor-pointer"
                                prop.onClick (fun _ -> dispatch (Navigate (BoxDetail box.Id)))
                                prop.children [
                                    Html.div [
                                        prop.className "card-body"
                                        prop.children [
                                            Html.h2 [
                                                prop.className "card-title"
                                                prop.text (box.Label |> Option.defaultValue box.Id)
                                            ]
                                        ]
                                    ]
                                ]
                            ]
                    ]
                ]
            ]
        ]

let boxesPage (state: State) (dispatch: Msg -> unit) : ReactElement =
    Html.div [
        prop.children [
            Html.div [
                prop.className "flex items-center justify-between mb-4"
                prop.children [
                    Html.h1 [
                        prop.className "text-2xl font-bold"
                        prop.text "Boxes"
                    ]
                    Html.button [
                        prop.className "btn btn-primary btn-sm"
                        prop.text "+ New Box"
                        prop.onClick (fun _ -> dispatch ShowCreateBoxForm)
                    ]
                ]
            ]
            if state.ShowCreateBoxForm then
                Html.div [
                    prop.className "card bg-base-200 mb-4"
                    prop.children [
                        Html.div [
                            prop.className "card-body"
                            prop.children [
                                Html.div [
                                    prop.className "form-control"
                                    prop.children [
                                        Html.label [
                                            prop.className "label"
                                            prop.children [
                                                Html.span [ prop.className "label-text"; prop.text "Label (optional)" ]
                                            ]
                                        ]
                                        Html.input [
                                            prop.className "input input-bordered"
                                            prop.placeholder "e.g. Kitchen supplies"
                                            prop.value state.NewBoxLabel
                                            prop.onChange (fun (s: string) -> dispatch (NewBoxLabelChanged s))
                                        ]
                                    ]
                                ]
                                Html.div [
                                    prop.className "card-actions justify-end"
                                    prop.children [
                                        Html.button [
                                            prop.className "btn btn-primary btn-sm"
                                            prop.text "Create"
                                            prop.onClick (fun _ -> dispatch SubmitCreateBox)
                                        ]
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]
            Html.div [
                prop.className "form-control mb-4"
                prop.children [
                    Html.select [
                        prop.className "select select-bordered"
                        prop.value state.BoxFilter
                        prop.onChange (fun (s: string) -> dispatch (BoxFilterChanged s))
                        prop.children [
                            Html.option [ prop.value ""; prop.text "All locations" ]
                            for loc in state.Locations do
                                Html.option [
                                    prop.value loc.Code
                                    prop.text loc.Name
                                ]
                        ]
                    ]
                ]
            ]
            Html.div [
                prop.className "grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4"
                prop.children [
                    if Array.isEmpty state.Boxes && not state.Loading then
                        Html.div [
                            prop.className "col-span-full text-center py-8 opacity-60"
                            prop.children [
                                Html.p [ prop.className "text-lg"; prop.text "No boxes yet" ]
                                Html.p [ prop.className "text-sm"; prop.text "Click \"+ New Box\" to create one" ]
                            ]
                        ]
                    for box in state.Boxes do
                        Html.div [
                            prop.className "card bg-base-200 cursor-pointer"
                            prop.onClick (fun _ -> dispatch (Navigate (BoxDetail box.Id)))
                            prop.children [
                                Html.div [
                                    prop.className "card-body"
                                    prop.children [
                                        Html.h2 [
                                            prop.className "card-title"
                                            prop.text (box.Label |> Option.defaultValue box.Id)
                                        ]
                                        match box.LocationCode with
                                        | Some code ->
                                            Html.span [
                                                prop.className "badge badge-outline"
                                                prop.text code
                                            ]
                                        | None -> Html.none
                                    ]
                                ]
                            ]
                        ]
                ]
            ]
        ]
    ]

let boxDetailPage (state: State) (dispatch: Msg -> unit) : ReactElement =
    match state.BoxDetail with
    | None ->
        if state.Loading then loadingSpinner state
        else Html.div [ prop.text "Box not found" ]
    | Some detail ->
        Html.div [
            prop.children [
                moveItemDialog state dispatch
                addExistingItemDialog state dispatch
                Html.div [
                    prop.className "flex items-center gap-2 mb-4"
                    prop.children [
                        Html.button [
                            prop.className "btn btn-ghost btn-sm"
                            prop.text "< Back"
                            prop.onClick (fun _ -> dispatch (Navigate BoxesList))
                        ]
                    ]
                ]
                Html.div [
                    prop.className "card bg-base-200 mb-4"
                    prop.children [
                        Html.div [
                            prop.className "card-body"
                            prop.children [
                                Html.div [
                                    prop.className "flex items-center justify-between"
                                    prop.children [
                                        if state.EditingBoxLabel then
                                            Html.div [
                                                prop.className "flex items-center gap-2"
                                                prop.children [
                                                    Html.input [
                                                        prop.className "input input-bordered input-sm"
                                                        prop.value state.EditBoxLabelValue
                                                        prop.onChange (fun (s: string) -> dispatch (EditBoxLabelChanged s))
                                                    ]
                                                    Html.button [
                                                        prop.className "btn btn-primary btn-sm"
                                                        prop.text "Save"
                                                        prop.onClick (fun _ -> dispatch SubmitEditBoxLabel)
                                                    ]
                                                    Html.button [
                                                        prop.className "btn btn-ghost btn-sm"
                                                        prop.text "Cancel"
                                                        prop.onClick (fun _ -> dispatch CancelEditBoxLabel)
                                                    ]
                                                ]
                                            ]
                                        else
                                            Html.h1 [
                                                prop.className "text-2xl font-bold"
                                                prop.children [
                                                    Html.text (detail.Box.Label |> Option.defaultValue detail.Box.Id)
                                                ]
                                            ]
                                        if not state.EditingBoxLabel then
                                            Html.div [
                                                prop.className "flex gap-2"
                                                prop.children [
                                                    Html.button [
                                                        prop.className "btn btn-ghost btn-sm"
                                                        prop.text "Edit"
                                                        prop.onClick (fun _ -> dispatch StartEditBoxLabel)
                                                    ]
                                                    Html.button [
                                                        prop.className "btn btn-error btn-sm"
                                                        prop.text "Delete Box"
                                                        prop.onClick (fun _ -> dispatch DeleteBox)
                                                    ]
                                                ]
                                            ]
                                    ]
                                ]
                                Html.div [
                                    prop.className "form-control mt-2"
                                    prop.children [
                                        Html.label [
                                            prop.className "label"
                                            prop.children [
                                                Html.span [ prop.className "label-text"; prop.text "Assign to location" ]
                                            ]
                                        ]
                                        Html.select [
                                            prop.className "select select-bordered select-sm"
                                            prop.value state.AssignLocationCode
                                            prop.onChange (fun (s: string) -> dispatch (AssignBoxToLocation s))
                                            prop.children [
                                                Html.option [ prop.value ""; prop.text "Unassigned" ]
                                                for loc in state.AvailableLocations do
                                                    if not loc.IsArchived then
                                                        Html.option [
                                                            prop.value loc.Code
                                                            prop.text loc.Name
                                                        ]
                                            ]
                                        ]
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]
                Html.div [
                    prop.className "card bg-base-200 mb-4"
                    prop.children [
                        Html.div [
                            prop.className "card-body"
                            prop.children [
                                Html.div [
                                    prop.className "flex items-center justify-between"
                                    prop.children [
                                        Html.h2 [
                                            prop.className "card-title"
                                            prop.text $"Items (%i{detail.Items.Length})"
                                        ]
                                        Html.button [
                                            prop.className "btn btn-outline btn-sm"
                                            prop.text "+ Existing Item"
                                            prop.onClick (fun _ -> dispatch ShowAddExistingItemDialog)
                                        ]
                                    ]
                                ]
                                Html.div [
                                    prop.className "flex items-end gap-2 mt-2"
                                    prop.children [
                                        Html.input [
                                            prop.className "input input-bordered input-sm flex-1"
                                            prop.placeholder "Item name"
                                            prop.value state.NewItemName
                                            prop.onChange (fun (s: string) -> dispatch (NewItemNameChanged s))
                                        ]
                                        Html.div [
                                            prop.className "form-control"
                                            prop.children [
                                                Html.input [
                                                    prop.className "file-input file-input-bordered file-input-sm"
                                                    prop.type' "file"
                                                    prop.accept "image/*"
#if FABLE_COMPILER
                                                    prop.onChange (fun (files: File list) ->
                                                        files |> List.tryHead |> Option.iter (fun f -> dispatch (PhotoSelected (box f))))
#endif
                                                ]
                                            ]
                                        ]
                                        Html.button [
                                            prop.className "btn btn-primary btn-sm"
                                            prop.text "Add Item"
                                            prop.onClick (fun _ -> dispatch SubmitAddItem)
                                        ]
                                    ]
                                ]
                                Html.ul [
                                    prop.className "mt-4 space-y-2"
                                    prop.children [
                                        if Array.isEmpty detail.Items then
                                            Html.li [
                                                prop.className "text-center py-6 opacity-60"
                                                prop.children [
                                                    Html.p [ prop.text "No items in this box yet" ]
                                                ]
                                            ]
                                        for item in detail.Items do
                                            Html.li [
                                                prop.className "flex items-center justify-between bg-base-300 rounded-lg p-3"
                                                prop.children [
                                                    Html.div [
                                                        prop.className "flex items-center gap-3"
                                                        prop.children [
                                                            match photoUrl item.PhotoPath with
                                                            | Some url ->
                                                                Html.img [
                                                                    prop.className "w-12 h-12 object-cover rounded"
                                                                    prop.src url
                                                                ]
                                                            | None -> Html.none
                                                            Html.span [ prop.text item.Name ]
                                                        ]
                                                    ]
                                                    Html.div [
                                                        prop.className "flex gap-1"
                                                        prop.children [
                                                            Html.button [
                                                                prop.className "btn btn-ghost btn-xs"
                                                                prop.text "Move"
                                                                prop.onClick (fun _ -> dispatch (ShowMoveItemDialog item.Id))
                                                            ]
                                                            Html.button [
                                                                prop.className "btn btn-ghost btn-xs"
                                                                prop.text "Unassign"
                                                                prop.onClick (fun _ -> dispatch (UnassignItem item.Id))
                                                            ]
                                                            Html.button [
                                                                prop.className "btn btn-ghost btn-xs"
                                                                prop.text "Delete"
                                                                prop.onClick (fun _ -> dispatch (DeleteItem item.Id))
                                                            ]
                                                        ]
                                                    ]
                                                ]
                                            ]
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]

let searchPage (state: State) (dispatch: Msg -> unit) : ReactElement =
    Html.div [
        prop.children [
            Html.h1 [
                prop.className "text-2xl font-bold mb-4"
                prop.text "Search Items"
            ]
            Html.div [
                prop.className "form-control mb-4"
                prop.children [
                    Html.input [
                        prop.className "input input-bordered w-full"
                        prop.placeholder "Search for items..."
                        prop.value state.SearchQuery
                        prop.onChange (fun (s: string) -> dispatch (SearchQueryChanged s))
                    ]
                ]
            ]
            Html.div [
                prop.className "space-y-2"
                prop.children [
                    if not (System.String.IsNullOrEmpty state.SearchQuery) && Array.isEmpty state.SearchResults && not state.Loading then
                        Html.div [
                            prop.className "text-center py-8 opacity-60"
                            prop.children [
                                Html.p [ prop.text "No items found matching your search" ]
                            ]
                        ]
                    for r in state.SearchResults do
                        Html.div [
                            prop.className "card bg-base-200"
                            prop.children [
                                Html.div [
                                    prop.className "card-body flex-row items-center gap-4 p-4"
                                    prop.children [
                                        match photoUrl r.PhotoPath with
                                        | Some url ->
                                            Html.img [
                                                prop.className "w-16 h-16 object-cover rounded"
                                                prop.src url
                                            ]
                                        | None -> Html.none
                                        Html.div [
                                            prop.className "flex-1"
                                            prop.children [
                                                Html.p [
                                                    prop.className "font-semibold"
                                                    prop.text r.ItemName
                                                ]
                                                Html.p [
                                                    prop.className "text-sm opacity-70"
                                                    prop.children [
                                                        Html.text (r.BoxLabel |> Option.defaultValue r.BoxId)
                                                        match r.LocationName with
                                                        | Some name -> Html.text $" — %s{name}"
                                                        | None -> Html.text ""
                                                    ]
                                                ]
                                            ]
                                        ]
                                    ]
                                ]
                            ]
                        ]
                ]
            ]
        ]
    ]

let private moveItemStandaloneDialog (state: State) (dispatch: Msg -> unit) : ReactElement =
    match state.MovingItemStandaloneId with
    | None -> Html.none
    | Some _ ->
        Html.div [
            prop.className "modal modal-open"
            prop.children [
                Html.div [
                    prop.className "modal-box"
                    prop.children [
                        Html.h3 [
                            prop.className "font-bold text-lg mb-4"
                            prop.text "Move item to a box"
                        ]
                        Html.div [
                            prop.className "form-control"
                            prop.children [
                                Html.label [
                                    prop.className "label"
                                    prop.children [
                                        Html.span [ prop.className "label-text"; prop.text "Select target box" ]
                                    ]
                                ]
                                Html.select [
                                    prop.className "select select-bordered w-full"
                                    prop.value state.MoveItemTargetBox
                                    prop.onChange (fun (s: string) -> dispatch (MoveItemTargetBoxChanged s))
                                    prop.children [
                                        Html.option [ prop.value ""; prop.text "Choose a box..." ]
                                        for box in state.BoxesForItemMove do
                                            Html.option [
                                                prop.value box.Id
                                                prop.text (box.Label |> Option.defaultValue box.Id)
                                            ]
                                    ]
                                ]
                            ]
                        ]
                        Html.div [
                            prop.className "modal-action"
                            prop.children [
                                Html.button [
                                    prop.className "btn btn-ghost"
                                    prop.text "Cancel"
                                    prop.onClick (fun _ -> dispatch CancelMoveItemStandalone)
                                ]
                                Html.button [
                                    prop.className "btn btn-primary"
                                    prop.text "Move"
                                    prop.disabled (System.String.IsNullOrEmpty state.MoveItemTargetBox)
                                    prop.onClick (fun _ -> dispatch ConfirmMoveItemStandalone)
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]

let private itemCard (state: State) (dispatch: Msg -> unit) (item: SearchResultDto) : ReactElement =
    let isEditing = state.EditingItemId = Some item.ItemId
    Html.div [
        prop.className "card bg-base-200"
        prop.children [
            Html.div [
                prop.className "card-body p-4"
                prop.children [
                    Html.div [
                        prop.className "flex items-start gap-3"
                        prop.children [
                            match photoUrl item.PhotoPath with
                            | Some url ->
                                Html.img [
                                    prop.className "w-16 h-16 object-cover rounded flex-shrink-0"
                                    prop.src url
                                ]
                            | None ->
                                Html.div [
                                    prop.className "w-16 h-16 bg-base-300 rounded flex items-center justify-center flex-shrink-0"
                                    prop.children [
                                        Html.span [ prop.className "text-2xl opacity-30"; prop.text "?" ]
                                    ]
                                ]
                            Html.div [
                                prop.className "flex-1 min-w-0"
                                prop.children [
                                    if isEditing then
                                        Html.div [
                                            prop.className "flex items-center gap-2 flex-wrap"
                                            prop.children [
                                                Html.input [
                                                    prop.className "input input-bordered input-sm flex-1"
                                                    prop.value state.EditItemNameValue
                                                    prop.onChange (fun (s: string) -> dispatch (EditItemNameChanged s))
                                                ]
                                                Html.button [
                                                    prop.className "btn btn-primary btn-xs"
                                                    prop.text "Save"
                                                    prop.onClick (fun _ -> dispatch SubmitEditItem)
                                                ]
                                                Html.button [
                                                    prop.className "btn btn-ghost btn-xs"
                                                    prop.text "Cancel"
                                                    prop.onClick (fun _ -> dispatch CancelEditItem)
                                                ]
                                            ]
                                        ]
                                        Html.div [
                                            prop.className "form-control mt-1"
                                            prop.children [
                                                Html.input [
                                                    prop.className "file-input file-input-bordered file-input-xs"
                                                    prop.type' "file"
                                                    prop.accept "image/*"
#if FABLE_COMPILER
                                                    prop.onChange (fun (files: Browser.Types.File list) ->
                                                        files |> List.tryHead |> Option.iter (fun f ->
                                                            dispatch (UploadItemPhoto (item.ItemId, box f))))
#endif
                                                ]
                                            ]
                                        ]
                                    else
                                        Html.p [
                                            prop.className "font-semibold truncate"
                                            prop.text item.ItemName
                                        ]
                                    Html.p [
                                        prop.className "text-sm opacity-70"
                                        prop.children [
                                            if System.String.IsNullOrEmpty item.BoxId then
                                                Html.span [ prop.className "badge badge-ghost badge-sm"; prop.text "Unassigned" ]
                                            else
                                                Html.text (item.BoxLabel |> Option.defaultValue item.BoxId)
                                                match item.LocationName with
                                                | Some name -> Html.text $" — %s{name}"
                                                | None -> Html.text ""
                                        ]
                                    ]
                                    if not isEditing then
                                        Html.div [
                                            prop.className "flex gap-1 mt-2"
                                            prop.children [
                                                Html.button [
                                                    prop.className "btn btn-ghost btn-xs"
                                                    prop.text "Edit"
                                                    prop.onClick (fun _ -> dispatch (StartEditItem (item.ItemId, item.ItemName)))
                                                ]
                                                Html.button [
                                                    prop.className "btn btn-ghost btn-xs"
                                                    prop.text "Move"
                                                    prop.onClick (fun _ -> dispatch (ShowMoveItemStandaloneDialog item.ItemId))
                                                ]
                                                if not (System.String.IsNullOrEmpty item.BoxId) then
                                                    Html.button [
                                                        prop.className "btn btn-ghost btn-xs"
                                                        prop.text "Unassign"
                                                        prop.onClick (fun _ -> dispatch (UnassignStandaloneItem item.ItemId))
                                                    ]
                                                Html.button [
                                                    prop.className "btn btn-ghost btn-xs text-error"
                                                    prop.text "Delete"
                                                    prop.onClick (fun _ -> dispatch (DeleteStandaloneItem item.ItemId))
                                                ]
                                            ]
                                        ]
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ]

let itemsPage (state: State) (dispatch: Msg -> unit) : ReactElement =
    Html.div [
        prop.children [
            moveItemStandaloneDialog state dispatch
            Html.div [
                prop.className "flex items-center justify-between mb-4"
                prop.children [
                    Html.h1 [
                        prop.className "text-2xl font-bold"
                        prop.text $"Items (%i{state.AllItems.Length})"
                    ]
                    Html.button [
                        prop.className "btn btn-primary btn-sm"
                        prop.text "+ New Item"
                        prop.onClick (fun _ -> dispatch ShowCreateItemForm)
                    ]
                ]
            ]
            if state.ShowCreateItemForm then
                Html.div [
                    prop.className "card bg-base-200 mb-4"
                    prop.children [
                        Html.div [
                            prop.className "card-body"
                            prop.children [
                                Html.div [
                                    prop.className "form-control"
                                    prop.children [
                                        Html.label [
                                            prop.className "label"
                                            prop.children [
                                                Html.span [ prop.className "label-text"; prop.text "Item name" ]
                                            ]
                                        ]
                                        Html.input [
                                            prop.className "input input-bordered"
                                            prop.placeholder "e.g. Kitchen knife set"
                                            prop.value state.NewStandaloneItemName
                                            prop.onChange (fun (s: string) -> dispatch (NewStandaloneItemNameChanged s))
                                        ]
                                    ]
                                ]
                                Html.div [
                                    prop.className "form-control"
                                    prop.children [
                                        Html.label [
                                            prop.className "label"
                                            prop.children [
                                                Html.span [ prop.className "label-text"; prop.text "Assign to box (optional)" ]
                                            ]
                                        ]
                                        Html.select [
                                            prop.className "select select-bordered"
                                            prop.value state.NewStandaloneItemBoxId
                                            prop.onChange (fun (s: string) -> dispatch (NewStandaloneItemBoxChanged s))
                                            prop.children [
                                                Html.option [ prop.value ""; prop.text "Leave unassigned" ]
                                                for box in state.Boxes do
                                                    Html.option [
                                                        prop.value box.Id
                                                        prop.text (box.Label |> Option.defaultValue box.Id)
                                                    ]
                                            ]
                                        ]
                                    ]
                                ]
                                Html.div [
                                    prop.className "card-actions justify-end"
                                    prop.children [
                                        Html.button [
                                            prop.className "btn btn-ghost btn-sm"
                                            prop.text "Cancel"
                                            prop.onClick (fun _ -> dispatch (Navigate ItemsList))
                                        ]
                                        Html.button [
                                            prop.className "btn btn-primary btn-sm"
                                            prop.text "Create"
                                            prop.disabled (System.String.IsNullOrEmpty(state.NewStandaloneItemName.Trim()))
                                            prop.onClick (fun _ -> dispatch SubmitCreateStandaloneItem)
                                        ]
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]
            Html.div [
                prop.className "space-y-2"
                prop.children [
                    if Array.isEmpty state.AllItems && not state.Loading then
                        Html.div [
                            prop.className "text-center py-8 opacity-60"
                            prop.children [
                                Html.p [ prop.className "text-lg"; prop.text "No items yet" ]
                                Html.p [ prop.className "text-sm"; prop.text "Click \"+ New Item\" to create one, or add items to boxes from the box detail page" ]
                            ]
                        ]
                    for item in state.AllItems do
                        itemCard state dispatch item
                ]
            ]
        ]
    ]

let renderPage (state: State) (dispatch: Msg -> unit) : ReactElement =
    Html.div [
        prop.className "min-h-screen bg-base-100"
        prop.children [
            navbar state dispatch
            Html.div [
                prop.className "container mx-auto p-4"
                prop.children [
                    errorAlert state dispatch
                    match state.CurrentPage with
                    | LocationsList -> locationsPage state dispatch
                    | LocationDetail _ -> locationDetailPage state dispatch
                    | BoxesList -> boxesPage state dispatch
                    | BoxDetail _ -> boxDetailPage state dispatch
                    | ItemsList -> itemsPage state dispatch
                    | ItemsSearch -> searchPage state dispatch
                ]
            ]
        ]
    ]
