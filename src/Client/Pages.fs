module BoxTracker.Client.Pages

#if FABLE_COMPILER
open Browser.Types
#endif
open Feliz
open BoxTracker.Client.State
open BoxTracker.Client.Api

[<Fable.Core.Emit("$0.target.value")>]
let private targetValue (ev: obj) : string = failwith "JS only"

let private photoUrl (path: string option) (variant: string) : string option =
    path |> Option.map (fun p -> $"/api/%s{p}-%s{variant}.webp")

let private photoUrlFull (path: string option) : string option =
    photoUrl path "full"

let private photoUrlThumb (path: string option) : string option =
    photoUrl path "thumb"

let private imageViewer (state: State) (dispatch: Msg -> unit) : ReactElement =
    match state.ViewingImageUrl with
    | None -> Html.none
    | Some url ->
        Html.div [
            prop.className "image-viewer fixed inset-0 bg-black/90 z-50 flex items-center justify-center p-4"
            prop.onClick (fun e ->
                if e.currentTarget = e.target then dispatch CloseImageViewer
            )
            prop.children [
                Html.div [
                    prop.className "relative max-w-4xl max-h-screen"
                    prop.children [
                        Html.img [
                            prop.className "w-full h-auto max-h-screen object-contain"
                            prop.src url
                        ]
                        Html.button [
                            prop.className "absolute top-2 right-2 btn btn-circle btn-sm btn-ghost text-white hover:bg-white/10"
                            prop.onClick (fun _ -> dispatch CloseImageViewer)
                            prop.children [
                                Html.span [ prop.className "text-xl"; prop.text "✕" ]
                            ]
                        ]
                    ]
                ]
            ]
        ]

let private navItem (label: string) (page: Page) (dispatch: Msg -> unit) : ReactElement =
    Html.a [
        prop.text label
        prop.onClick (fun _ -> dispatch (Navigate page))
    ]

let navbar (state: State) (dispatch: Msg -> unit) : ReactElement =
    Html.div [
        prop.className "navbar bg-base-200 border-b border-base-300 sticky top-0 z-40"
        prop.children [
            Html.div [
                prop.className "flex-none"
                prop.children [
                    Html.a [
                        prop.className "btn btn-ghost text-lg md:text-xl tracking-tight px-2 md:px-4"
                        prop.text "BoxTracker"
                        prop.onClick (fun _ -> dispatch (Navigate LocationsList))
                    ]
                ]
            ]
            Html.div [
                prop.className "flex-1 hidden md:flex"
                prop.children [
                    Html.ul [
                        prop.className "menu menu-horizontal px-1"
                        prop.children [
                            Html.li [ navItem "Locations" LocationsList dispatch ]
                            Html.li [ navItem "Boxes" BoxesList dispatch ]
                            Html.li [ navItem "Items" ItemsList dispatch ]
                        ]
                    ]
                ]
            ]
            Html.div [
                prop.className "flex-1 md:hidden"
            ]
            Html.div [
                prop.className "flex-none"
                prop.children [
                    Html.div [
                        prop.className "dropdown dropdown-end"
                        prop.children [
                            Html.button [
                                prop.tabIndex 0
                                prop.className "btn btn-ghost btn-circle btn-lg md:btn-md"
                                prop.text "☰"
                            ]
                            Html.ul [
                                prop.tabIndex 0
                                prop.className "dropdown-content menu bg-base-300 rounded-lg z-10 w-52 p-2 shadow-xl"
                                prop.children [
                                    Html.li [ navItem "Locations" LocationsList dispatch ]
                                    Html.li [ navItem "Boxes" BoxesList dispatch ]
                                    Html.li [ navItem "Items" ItemsList dispatch ]
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
            prop.className "alert alert-error mb-4 rounded-lg gap-3 flex items-start sm:items-center text-base"
            prop.children [
                Html.div [ prop.className "flex-1"; prop.text err ]
                Html.button [
                    prop.className "btn btn-ghost btn-sm btn-circle flex-shrink-0"
                    prop.text "✕"
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
                    prop.className "modal-box w-11/12 max-w-md sm:max-w-lg"
                    prop.children [
                        Html.h3 [
                            prop.className "font-bold text-lg mb-4"
                            prop.text "Move item to another box"
                        ]
                        Html.div [
                            prop.className "form-control mb-4"
                            prop.children [
                                Html.label [
                                    prop.className "label pb-3"
                                    prop.children [
                                        Html.span [ prop.className "label-text text-sm font-medium"; prop.text "Select target box" ]
                                    ]
                                ]
                                Html.select [
                                    prop.className "select select-bordered w-full text-base ml-2"
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
                            prop.className "modal-action gap-2"
                            prop.children [
                                Html.button [
                                    prop.className "btn btn-ghost btn-sm"
                                    prop.text "Cancel"
                                    prop.onClick (fun _ -> dispatch CancelMoveItem)
                                ]
                                Html.button [
                                    prop.className "btn btn-primary btn-sm"
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
                    prop.className "modal-box w-11/12 max-w-md sm:max-w-lg"
                    prop.children [
                        Html.h3 [
                            prop.className "font-bold text-lg mb-4"
                            prop.text "Add existing item to this box"
                        ]
                        if Array.isEmpty state.UnassignedItems then
                            Html.p [
                                prop.className "text-center py-4 opacity-60 text-base"
                                prop.text "No unassigned items available"
                            ]
                        else
                            Html.ul [
                                prop.className "space-y-3 max-h-80 overflow-y-auto mb-4"
                                prop.children [
                                    for item in state.UnassignedItems do
                                        Html.li [
                                            prop.className [
                                                "flex items-center gap-3 p-3 rounded-lg cursor-pointer text-base"
                                                if state.SelectedExistingItemId = item.ItemId then "bg-primary text-primary-content"
                                                else "bg-base-300 hover:bg-base-200"
                                            ]
                                            prop.onClick (fun _ -> dispatch (SelectedExistingItemChanged item.ItemId))
                                            prop.children [
                                                match photoUrlFull item.PhotoPath with
                                                | Some url ->
                                                    Html.img [
                                                        prop.className "w-10 h-10 object-cover rounded flex-shrink-0 cursor-pointer hover:opacity-80 transition-opacity"
                                                        prop.src url
                                                        prop.onClick (fun e -> e.stopPropagation(); dispatch (ShowImageViewer url))
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
                            prop.className "modal-action gap-2"
                            prop.children [
                                Html.button [
                                    prop.className "btn btn-ghost btn-sm"
                                    prop.text "Cancel"
                                    prop.onClick (fun _ -> dispatch CancelAddExistingItem)
                                ]
                                Html.button [
                                    prop.className "btn btn-primary btn-sm"
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
                prop.className "flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between mb-6"
                prop.children [
                    Html.h1 [
                        prop.className "text-2xl sm:text-3xl font-bold"
                        prop.text "Locations"
                    ]
                    Html.button [
                        prop.className "btn btn-primary btn-sm sm:btn-md w-full sm:w-auto"
                        prop.text "+ New Location"
                        prop.onClick (fun _ -> dispatch ShowCreateLocationForm)
                    ]
                ]
            ]
            if state.ShowCreateLocationForm then
                Html.div [
                    prop.className "card bg-base-200 border border-base-300 mb-6 shadow-sm"
                    prop.children [
                        Html.div [
                            prop.className "card-body p-4 sm:p-6"
                            prop.children [
                                Html.div [
                                    prop.className "form-control mb-4"
                                    prop.children [
                                        Html.label [
                                            prop.className "label pb-3"
                                            prop.children [
                                                Html.span [ prop.className "label-text text-sm font-medium"; prop.text "Code" ]
                                            ]
                                        ]
                                        Html.input [
                                            prop.className "input input-bordered focus:input-primary text-base"
                                            prop.placeholder "e.g. GARAGE"
                                            prop.value state.NewLocationCode
                                            prop.onChange (fun (s: string) -> dispatch (NewLocationCodeChanged s))
                                        ]
                                    ]
                                ]
                                Html.div [
                                    prop.className "form-control mb-6"
                                    prop.children [
                                        Html.label [
                                            prop.className "label pb-3"
                                            prop.children [
                                                Html.span [ prop.className "label-text text-sm font-medium"; prop.text "Name" ]
                                            ]
                                        ]
                                        Html.input [
                                            prop.className "input input-bordered focus:input-primary text-base"
                                            prop.placeholder "e.g. Garage"
                                            prop.value state.NewLocationName
                                            prop.onChange (fun (s: string) -> dispatch (NewLocationNameChanged s))
                                        ]
                                    ]
                                ]
                                Html.div [
                                    prop.className "flex gap-2 justify-end"
                                    prop.children [
                                        Html.button [
                                            prop.className "btn btn-ghost btn-sm"
                                            prop.text "Cancel"
                                            prop.onClick (fun _ -> dispatch ShowCreateLocationForm)
                                        ]
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
                prop.className "form-control mb-4"
                prop.children [
                    Html.input [
                        prop.className "input input-bordered w-full focus:input-primary text-base"
                        prop.placeholder "Filter locations..."
                        prop.value state.LocationSearch
                        prop.onChange (fun (s: string) -> dispatch (LocationSearchChanged s))
                    ]
                ]
            ]
            let filteredLocations =
                if System.String.IsNullOrEmpty state.LocationSearch then state.Locations
                else
                    let q = state.LocationSearch.ToLowerInvariant()
                    state.Locations |> Array.filter (fun l ->
                        l.Name.ToLowerInvariant().Contains(q) || l.Code.ToLowerInvariant().Contains(q))
            Html.div [
                prop.className "grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3 sm:gap-4"
                prop.children [
                    if Array.isEmpty state.Locations && not state.Loading then
                        Html.div [
                            prop.className "col-span-full text-center py-12 opacity-60"
                            prop.children [
                                Html.p [ prop.className "text-lg"; prop.text "No locations yet" ]
                                Html.p [ prop.className "text-sm mt-2"; prop.text "Click \"+ New Location\" to create one" ]
                            ]
                        ]
                    elif Array.isEmpty filteredLocations then
                        Html.div [
                            prop.className "col-span-full text-center py-12 opacity-60"
                            prop.children [
                                Html.p [ prop.text "No locations match your search" ]
                            ]
                        ]
                    for loc in filteredLocations do
                        let isEditingThisLoc = state.EditingLocationCodeInList = Some loc.Code
                        Html.div [
                            prop.className "card bg-base-200"
                            prop.children [
                                Html.div [
                                    prop.className "card-body p-4 sm:p-5"
                                    prop.children [
                                        if isEditingThisLoc then
                                            Html.div [
                                                prop.className "flex flex-col sm:flex-row items-end gap-2"
                                                prop.children [
                                                    Html.input [
                                                        prop.className "input input-bordered input-sm flex-1 w-full text-base"
                                                        prop.value state.EditLocationNameInListValue
                                                        prop.onChange (fun (s: string) -> dispatch (EditLocationNameInListChanged s))
                                                    ]
                                                    Html.div [
                                                        prop.className "flex gap-2 w-full sm:w-auto"
                                                        prop.children [
                                                            Html.button [
                                                                prop.className "btn btn-ghost btn-sm flex-1 sm:flex-none"
                                                                prop.text "Cancel"
                                                                prop.onClick (fun _ -> dispatch CancelEditLocationInList)
                                                            ]
                                                            Html.button [
                                                                prop.className "btn btn-primary btn-sm flex-1 sm:flex-none"
                                                                prop.text "Save"
                                                                prop.onClick (fun _ -> dispatch SubmitEditLocationInList)
                                                            ]
                                                        ]
                                                    ]
                                                ]
                                            ]
                                        else
                                            Html.div [
                                                prop.className "flex items-start justify-between gap-2"
                                                prop.children [
                                                    Html.div [
                                                        prop.className "flex-1 min-w-0 cursor-pointer"
                                                        prop.onClick (fun _ -> dispatch (Navigate (LocationDetail loc.Code)))
                                                        prop.children [
                                                            Html.h2 [
                                                                prop.className "card-title text-lg flex items-center gap-2 flex-wrap hover:opacity-80"
                                                                prop.children [
                                                                    Html.text loc.Name
                                                                    if loc.IsArchived then
                                                                        Html.span [ prop.className "badge badge-ghost badge-sm"; prop.text "Archived" ]
                                                                ]
                                                            ]
                                                            Html.p [ prop.className "text-sm opacity-70"; prop.text loc.Code ]
                                                        ]
                                                    ]
                                                    Html.div [
                                                        prop.className "flex gap-1 flex-shrink-0"
                                                        prop.children [
                                                            Html.button [
                                                                prop.className "btn btn-ghost btn-sm"
                                                                prop.text "Edit"
                                                                prop.onClick (fun e -> e.stopPropagation(); dispatch (StartEditLocationInList (loc.Code, loc.Name)))
                                                            ]
                                                            if not loc.IsArchived then
                                                                Html.button [
                                                                    prop.className "btn btn-ghost btn-sm text-warning"
                                                                    prop.text "Archive"
                                                                    prop.onClick (fun e -> e.stopPropagation(); dispatch (ArchiveLocationFromList loc.Code))
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

let private addBoxToLocationDialog (state: State) (dispatch: Msg -> unit) : ReactElement =
    if not state.AddingBoxToLocation then Html.none
    else
        Html.div [
            prop.className "modal modal-open"
            prop.children [
                Html.div [
                    prop.className "modal-box w-11/12 max-w-md sm:max-w-lg"
                    prop.children [
                        Html.h3 [
                            prop.className "font-bold text-lg mb-4"
                            prop.text "Add box to this location"
                        ]
                        if Array.isEmpty state.BoxesForLocationMove then
                            Html.p [
                                prop.className "text-center py-4 opacity-60 text-base"
                                prop.text "No boxes available to add"
                            ]
                        else
                            Html.ul [
                                prop.className "space-y-3 max-h-80 overflow-y-auto mb-4"
                                prop.children [
                                    for box in state.BoxesForLocationMove do
                                        Html.li [
                                            prop.className [
                                                "flex items-center gap-3 p-3 rounded-lg cursor-pointer text-base"
                                                if state.SelectedBoxForLocationMove = box.Id then "bg-primary text-primary-content"
                                                else "bg-base-300 hover:bg-base-200"
                                            ]
                                            prop.onClick (fun _ -> dispatch (SelectedBoxForLocationMoveChanged box.Id))
                                            prop.children [
                                                Html.span [
                                                    prop.className "truncate flex-1"
                                                    prop.text (box.Label |> Option.defaultValue box.Id)
                                                ]
                                                match box.LocationCode with
                                                | Some code ->
                                                    Html.span [
                                                        prop.className "badge badge-outline badge-sm"
                                                        prop.text code
                                                    ]
                                                | None ->
                                                    Html.span [
                                                        prop.className "badge badge-ghost badge-sm"
                                                        prop.text "Unassigned"
                                                    ]
                                            ]
                                        ]
                                ]
                            ]
                        Html.div [
                            prop.className "modal-action gap-2"
                            prop.children [
                                Html.button [
                                    prop.className "btn btn-ghost btn-sm"
                                    prop.text "Cancel"
                                    prop.onClick (fun _ -> dispatch CancelAddBoxToLocation)
                                ]
                                Html.button [
                                    prop.className "btn btn-primary btn-sm"
                                    prop.text "Add to Location"
                                    prop.disabled (System.String.IsNullOrEmpty state.SelectedBoxForLocationMove)
                                    prop.onClick (fun _ -> dispatch ConfirmAddBoxToLocation)
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
                addBoxToLocationDialog state dispatch
                Html.div [
                    prop.className "mb-4 sm:mb-6"
                    prop.children [
                        Html.button [
                            prop.className "btn btn-ghost btn-sm gap-1"
                            prop.text "← Back"
                            prop.onClick (fun _ -> dispatch (Navigate LocationsList))
                        ]
                    ]
                ]
                Html.div [
                    prop.className "card bg-base-200 border border-base-300 mb-6 shadow-sm"
                    prop.children [
                        Html.div [
                            prop.className "card-body p-4 sm:p-6"
                            prop.children [
                                Html.div [
                                    prop.className "flex flex-col gap-4"
                                    prop.children [
                                        if state.EditingLocationName then
                                            Html.div [
                                                prop.className "flex flex-col sm:flex-row items-end gap-2"
                                                prop.children [
                                                    Html.input [
                                                        prop.className "input input-bordered input-sm flex-1 w-full text-base"
                                                        prop.value state.EditLocationNameValue
                                                        prop.onChange (fun (s: string) -> dispatch (EditLocationNameChanged s))
                                                    ]
                                                    Html.div [
                                                        prop.className "flex gap-2 w-full sm:w-auto"
                                                        prop.children [
                                                            Html.button [
                                                                prop.className "btn btn-ghost btn-sm flex-1 sm:flex-none"
                                                                prop.text "Cancel"
                                                                prop.onClick (fun _ -> dispatch CancelEditLocationName)
                                                            ]
                                                            Html.button [
                                                                prop.className "btn btn-primary btn-sm flex-1 sm:flex-none"
                                                                prop.text "Save"
                                                                prop.onClick (fun _ -> dispatch SubmitEditLocationName)
                                                            ]
                                                        ]
                                                    ]
                                                ]
                                            ]
                                        else
                                            Html.div [
                                                prop.className "flex flex-col sm:flex-row sm:items-start sm:justify-between gap-4"
                                                prop.children [
                                                    Html.h1 [
                                                        prop.className "text-2xl sm:text-3xl font-bold flex items-start gap-3"
                                                        prop.children [
                                                            Html.text detail.Location.Name
                                                            Html.span [
                                                                prop.className "badge badge-outline badge-lg"
                                                                prop.text detail.Location.Code
                                                            ]
                                                        ]
                                                    ]
                                                    Html.div [
                                                        prop.className "flex gap-2 w-full sm:w-auto flex-col sm:flex-row"
                                                        prop.children [
                                                            Html.button [
                                                                prop.className "btn btn-ghost btn-sm flex-1 sm:flex-none"
                                                                prop.text "Edit"
                                                                prop.onClick (fun _ -> dispatch StartEditLocationName)
                                                            ]
                                                            Html.button [
                                                                prop.className "btn btn-outline btn-sm flex-1 sm:flex-none"
                                                                prop.text "Print Label"
                                                                prop.onClick (fun _ -> dispatch PrintLocationLabel)
                                                            ]
                                                            Html.button [
                                                                prop.className "btn btn-error btn-sm flex-1 sm:flex-none"
                                                                prop.text "Archive"
                                                                prop.onClick (fun _ -> dispatch ArchiveLocation)
                                                            ]
                                                        ]
                                                    ]
                                                ]
                                            ]
                                    ]
                                ]
                                Html.div [
                                    prop.className "form-control"
                                    prop.children [
                                        Html.label [
                                            prop.className "label pb-3"
                                            prop.children [
                                                Html.span [ prop.className "label-text text-sm font-medium"; prop.text "Location photo" ]
                                            ]
                                        ]
                                        match photoUrlFull detail.Location.PhotoPath with
                                        | Some url ->
                                            Html.div [
                                                prop.className "mb-3"
                                                prop.children [
                                                    Html.img [
                                                        prop.className "w-48 h-48 object-cover rounded border border-base-300 cursor-pointer hover:opacity-80 transition-opacity"
                                                        prop.src url
                                                        prop.onClick (fun _ -> dispatch (ShowImageViewer url))
                                                    ]
                                                ]
                                            ]
                                        | None -> Html.none
                                        Html.div [
                                            prop.className "flex gap-2"
                                            prop.children [
                                                Html.label [
                                                    prop.className "btn btn-secondary btn-sm flex-1 cursor-pointer"
                                                    prop.children [
                                                        Html.text "📷 Take Photo"
                                                        Html.input [
                                                            prop.type' "file"
                                                            prop.accept "image/*"
                                                            prop.custom("capture", "environment")
                                                            prop.className "hidden"
#if FABLE_COMPILER
                                                            prop.onChange (fun (files: File list) ->
                                                                files |> List.tryHead |> Option.iter (fun f -> dispatch (UploadLocationPhoto (detail.Location.Code, box f))))
#endif
                                                        ]
                                                    ]
                                                ]
                                                Html.label [
                                                    prop.className "btn btn-outline btn-sm flex-1 cursor-pointer"
                                                    prop.children [
                                                        Html.text "📁 Choose"
                                                        Html.input [
                                                            prop.type' "file"
                                                            prop.accept "image/*"
                                                            prop.className "hidden"
#if FABLE_COMPILER
                                                            prop.onChange (fun (files: File list) ->
                                                                files |> List.tryHead |> Option.iter (fun f -> dispatch (UploadLocationPhoto (detail.Location.Code, box f))))
#endif
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
                Html.div [
                    prop.className "flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between mb-4"
                    prop.children [
                        Html.h2 [
                            prop.className "text-lg sm:text-xl font-bold"
                            prop.text $"Boxes (%i{detail.Boxes.Length})"
                        ]
                        Html.button [
                            prop.className "btn btn-outline btn-sm w-full sm:w-auto"
                            prop.text "+ Add Box"
                            prop.onClick (fun _ -> dispatch ShowAddBoxToLocationDialog)
                        ]
                    ]
                ]
                Html.div [
                    prop.className "grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3 sm:gap-4"
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
                                prop.className "card bg-base-200"
                                prop.children [
                                    Html.div [
                                        prop.className "card-body flex-row items-center justify-between p-4"
                                        prop.children [
                                            Html.span [
                                                prop.className "card-title cursor-pointer flex-1"
                                                prop.text (box.Label |> Option.defaultValue box.Id)
                                                prop.onClick (fun _ -> dispatch (Navigate (BoxDetail box.Id)))
                                            ]
                                            Html.button [
                                                prop.className "btn btn-ghost btn-sm"
                                                prop.text "Remove"
                                                prop.onClick (fun _ -> dispatch (UnassignBoxFromLocation box.Id))
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
                prop.className "flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between mb-6"
                prop.children [
                    Html.h1 [
                        prop.className "text-2xl sm:text-3xl font-bold"
                        prop.text "Boxes"
                    ]
                    Html.button [
                        prop.className "btn btn-primary btn-sm sm:btn-md w-full sm:w-auto"
                        prop.text "+ New Box"
                        prop.onClick (fun _ -> dispatch ShowCreateBoxForm)
                    ]
                ]
            ]
            if state.ShowCreateBoxForm then
                Html.div [
                    prop.className "card bg-base-200 border border-base-300 mb-6 shadow-sm"
                    prop.children [
                        Html.div [
                            prop.className "card-body p-4 sm:p-6"
                            prop.children [
                                Html.div [
                                    prop.className "form-control mb-6"
                                    prop.children [
                                        Html.label [
                                            prop.className "label pb-3"
                                            prop.children [
                                                Html.span [ prop.className "label-text text-sm font-medium"; prop.text "Label (optional)" ]
                                            ]
                                        ]
                                        Html.input [
                                            prop.className "input input-bordered focus:input-primary text-base"
                                            prop.placeholder "e.g. Kitchen supplies"
                                            prop.value state.NewBoxLabel
                                            prop.onChange (fun (s: string) -> dispatch (NewBoxLabelChanged s))
                                        ]
                                    ]
                                ]
                                Html.div [
                                    prop.className "flex gap-2 justify-end"
                                    prop.children [
                                        Html.button [
                                            prop.className "btn btn-ghost btn-sm"
                                            prop.text "Cancel"
                                            prop.onClick (fun _ -> dispatch ShowCreateBoxForm)
                                        ]
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
                prop.className "flex flex-col sm:flex-row gap-2 mb-4"
                prop.children [
                    Html.div [
                        prop.className "form-control flex-1"
                        prop.children [
                            Html.input [
                                prop.className "input input-bordered w-full focus:input-primary text-base"
                                prop.placeholder "Filter boxes..."
                                prop.value state.BoxSearch
                                prop.onChange (fun (s: string) -> dispatch (BoxSearchChanged s))
                            ]
                        ]
                    ]
                    Html.div [
                        prop.className "form-control"
                        prop.children [
                            Html.select [
                                prop.className "select select-bordered text-base ml-2"
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
                ]
            ]
            let filteredBoxes =
                if System.String.IsNullOrEmpty state.BoxSearch then state.Boxes
                else
                    let q = state.BoxSearch.ToLowerInvariant()
                    state.Boxes |> Array.filter (fun b ->
                        (b.Label |> Option.defaultValue b.Id).ToLowerInvariant().Contains(q))
            Html.div [
                prop.className "grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3 sm:gap-4"
                prop.children [
                    if Array.isEmpty state.Boxes && not state.Loading then
                        Html.div [
                            prop.className "col-span-full text-center py-12 opacity-60"
                            prop.children [
                                Html.p [ prop.className "text-lg"; prop.text "No boxes yet" ]
                                Html.p [ prop.className "text-sm mt-2"; prop.text "Click \"+ New Box\" to create one" ]
                            ]
                        ]
                    elif Array.isEmpty filteredBoxes then
                        Html.div [
                            prop.className "col-span-full text-center py-12 opacity-60"
                            prop.children [
                                Html.p [ prop.text "No boxes match your search" ]
                            ]
                        ]
                    for box in filteredBoxes do
                        let isEditingThisBox = state.EditingBoxIdInList = Some box.Id
                        Html.div [
                            prop.className "card bg-base-200"
                            prop.children [
                                Html.div [
                                    prop.className "card-body p-4 sm:p-5"
                                    prop.children [
                                        if isEditingThisBox then
                                            Html.div [
                                                prop.className "flex flex-col sm:flex-row items-end gap-2"
                                                prop.children [
                                                    Html.input [
                                                        prop.className "input input-bordered input-sm flex-1 w-full text-base"
                                                        prop.value state.EditBoxLabelInListValue
                                                        prop.onChange (fun (s: string) -> dispatch (EditBoxLabelInListChanged s))
                                                    ]
                                                    Html.div [
                                                        prop.className "flex gap-2 w-full sm:w-auto"
                                                        prop.children [
                                                            Html.button [
                                                                prop.className "btn btn-ghost btn-sm flex-1 sm:flex-none"
                                                                prop.text "Cancel"
                                                                prop.onClick (fun _ -> dispatch CancelEditBoxInList)
                                                            ]
                                                            Html.button [
                                                                prop.className "btn btn-primary btn-sm flex-1 sm:flex-none"
                                                                prop.text "Save"
                                                                prop.onClick (fun _ -> dispatch SubmitEditBoxInList)
                                                            ]
                                                        ]
                                                    ]
                                                ]
                                            ]
                                        else
                                            Html.div [
                                                prop.className "flex items-start justify-between gap-2"
                                                prop.children [
                                                    Html.div [
                                                        prop.className "flex-1 min-w-0 cursor-pointer"
                                                        prop.onClick (fun _ -> dispatch (Navigate (BoxDetail box.Id)))
                                                        prop.children [
                                                            Html.h2 [
                                                                prop.className "card-title text-lg truncate hover:opacity-80"
                                                                prop.text (box.Label |> Option.defaultValue box.Id)
                                                            ]
                                                            match box.LocationCode with
                                                            | Some code ->
                                                                Html.span [
                                                                    prop.className "badge badge-outline badge-sm"
                                                                    prop.text code
                                                                ]
                                                            | None -> Html.none
                                                        ]
                                                    ]
                                                    Html.div [
                                                        prop.className "flex gap-1 flex-shrink-0"
                                                        prop.children [
                                                            Html.button [
                                                                prop.className "btn btn-ghost btn-sm"
                                                                prop.text "Edit"
                                                                prop.onClick (fun e -> e.stopPropagation(); dispatch (StartEditBoxInList (box.Id, box.Label)))
                                                            ]
                                                            Html.button [
                                                                prop.className "btn btn-ghost btn-sm text-error"
                                                                prop.text "Delete"
                                                                prop.onClick (fun e -> e.stopPropagation(); dispatch (DeleteBoxFromList box.Id))
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
                    prop.className "mb-4 sm:mb-6"
                    prop.children [
                        Html.button [
                            prop.className "btn btn-ghost btn-sm gap-1"
                            prop.text "← Back"
                            prop.onClick (fun _ -> dispatch (Navigate BoxesList))
                        ]
                    ]
                ]
                Html.div [
                    prop.className "card bg-base-200 border border-base-300 mb-6 shadow-sm"
                    prop.children [
                        Html.div [
                            prop.className "card-body p-4 sm:p-6"
                            prop.children [
                                Html.div [
                                    prop.className "flex flex-col gap-4"
                                    prop.children [
                                        if state.EditingBoxLabel then
                                            Html.div [
                                                prop.className "flex flex-col sm:flex-row items-end gap-2"
                                                prop.children [
                                                    Html.input [
                                                        prop.className "input input-bordered input-sm flex-1 w-full text-base"
                                                        prop.value state.EditBoxLabelValue
                                                        prop.onChange (fun (s: string) -> dispatch (EditBoxLabelChanged s))
                                                    ]
                                                    Html.button [
                                                        prop.className "btn btn-ghost btn-sm flex-1 sm:flex-none"
                                                        prop.text "Cancel"
                                                        prop.onClick (fun _ -> dispatch CancelEditBoxLabel)
                                                    ]
                                                    Html.button [
                                                        prop.className "btn btn-primary btn-sm flex-1 sm:flex-none"
                                                        prop.text "Save"
                                                        prop.onClick (fun _ -> dispatch SubmitEditBoxLabel)
                                                    ]
                                                ]
                                            ]
                                        else
                                            Html.div [
                                                prop.className "flex flex-col sm:flex-row sm:items-start sm:justify-between gap-4"
                                                prop.children [
                                                    Html.h1 [
                                                        prop.className "text-2xl sm:text-3xl font-bold"
                                                        prop.text (detail.Box.Label |> Option.defaultValue detail.Box.Id)
                                                    ]
                                                    Html.div [
                                                        prop.className "flex gap-2 w-full sm:w-auto flex-col sm:flex-row"
                                                        prop.children [
                                                            Html.button [
                                                                prop.className "btn btn-ghost btn-sm flex-1 sm:flex-none"
                                                                prop.text "Edit"
                                                                prop.onClick (fun _ -> dispatch StartEditBoxLabel)
                                                            ]
                                                            Html.button [
                                                                prop.className "btn btn-outline btn-sm flex-1 sm:flex-none"
                                                                prop.text "Print Label"
                                                                prop.onClick (fun _ -> dispatch PrintBoxLabel)
                                                            ]
                                                            Html.button [
                                                                prop.className "btn btn-error btn-sm flex-1 sm:flex-none"
                                                                prop.text "Delete"
                                                                prop.onClick (fun _ -> dispatch DeleteBox)
                                                            ]
                                                        ]
                                                    ]
                                                ]
                                            ]
                                    ]
                                ]
                                Html.div [
                                    prop.className "form-control"
                                    prop.children [
                                        Html.label [
                                            prop.className "label pb-3"
                                            prop.children [
                                                Html.span [ prop.className "label-text text-sm font-medium"; prop.text "Assign to location" ]
                                            ]
                                        ]
                                        Html.select [
                                            prop.className "select select-bordered focus:select-primary text-base ml-2"
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
                                Html.div [
                                    prop.className "form-control"
                                    prop.children [
                                        Html.label [
                                            prop.className "label pb-3"
                                            prop.children [
                                                Html.span [ prop.className "label-text text-sm font-medium"; prop.text "Box photo" ]
                                            ]
                                        ]
                                        match photoUrlFull detail.Box.PhotoPath with
                                        | Some url ->
                                            Html.div [
                                                prop.className "mb-3"
                                                prop.children [
                                                    Html.img [
                                                        prop.className "w-48 h-48 object-cover rounded border border-base-300 cursor-pointer hover:opacity-80 transition-opacity"
                                                        prop.src url
                                                        prop.onClick (fun _ -> dispatch (ShowImageViewer url))
                                                    ]
                                                ]
                                            ]
                                        | None -> Html.none
                                        Html.div [
                                            prop.className "flex gap-2"
                                            prop.children [
                                                Html.label [
                                                    prop.className "btn btn-secondary btn-sm flex-1 cursor-pointer"
                                                    prop.children [
                                                        Html.text "📷 Take Photo"
                                                        Html.input [
                                                            prop.type' "file"
                                                            prop.accept "image/*"
                                                            prop.custom("capture", "environment")
                                                            prop.className "hidden"
#if FABLE_COMPILER
                                                            prop.onChange (fun (files: File list) ->
                                                                files |> List.tryHead |> Option.iter (fun f -> dispatch (UploadBoxPhoto (detail.Box.Id, box f))))
#endif
                                                        ]
                                                    ]
                                                ]
                                                Html.label [
                                                    prop.className "btn btn-outline btn-sm flex-1 cursor-pointer"
                                                    prop.children [
                                                        Html.text "📁 Choose"
                                                        Html.input [
                                                            prop.type' "file"
                                                            prop.accept "image/*"
                                                            prop.className "hidden"
#if FABLE_COMPILER
                                                            prop.onChange (fun (files: File list) ->
                                                                files |> List.tryHead |> Option.iter (fun f -> dispatch (UploadBoxPhoto (detail.Box.Id, box f))))
#endif
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
                Html.div [
                    prop.className "card bg-base-200 border border-base-300 mb-6 shadow-sm"
                    prop.children [
                        Html.div [
                            prop.className "card-body p-4 sm:p-6"
                            prop.children [
                                Html.div [
                                    prop.className "flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between mb-4"
                                    prop.children [
                                        Html.h2 [
                                            prop.className "text-lg sm:text-xl font-bold"
                                            prop.text $"Items (%i{detail.Items.Length})"
                                        ]
                                        Html.button [
                                            prop.className "btn btn-outline btn-sm w-full sm:w-auto"
                                            prop.text "+ Existing Item"
                                            prop.onClick (fun _ -> dispatch ShowAddExistingItemDialog)
                                        ]
                                    ]
                                ]
                                Html.div [
                                    prop.className "flex flex-col gap-2"
                                    prop.children [
                                        Html.input [
                                            prop.className "input input-bordered focus:input-primary text-base"
                                            prop.placeholder "Item name"
                                            prop.value state.NewItemName
                                            prop.onChange (fun (s: string) -> dispatch (NewItemNameChanged s))
                                        ]
                                        Html.div [
                                            prop.className "flex gap-2"
                                            prop.children [
                                                Html.label [
                                                    prop.className "btn btn-secondary btn-sm flex-1 cursor-pointer"
                                                    prop.children [
                                                        Html.text "📷 Take Photo"
                                                        Html.input [
                                                            prop.type' "file"
                                                            prop.accept "image/*"
                                                            prop.custom("capture", "environment")
                                                            prop.className "hidden"
#if FABLE_COMPILER
                                                            prop.onChange (fun (files: File list) ->
                                                                files |> List.tryHead |> Option.iter (fun f -> dispatch (PhotoSelected (box f))))
#endif
                                                        ]
                                                    ]
                                                ]
                                                Html.label [
                                                    prop.className "btn btn-outline btn-sm flex-1 cursor-pointer"
                                                    prop.children [
                                                        Html.text "📁 Choose"
                                                        Html.input [
                                                            prop.type' "file"
                                                            prop.accept "image/*"
                                                            prop.className "hidden"
#if FABLE_COMPILER
                                                            prop.onChange (fun (files: File list) ->
                                                                files |> List.tryHead |> Option.iter (fun f -> dispatch (PhotoSelected (box f))))
#endif
                                                        ]
                                                    ]
                                                ]
                                            ]
                                        ]
                                        Html.button [
                                            prop.className "btn btn-primary w-full"
                                            prop.text "Add Item"
                                            prop.onClick (fun _ -> dispatch SubmitAddItem)
                                        ]
                                    ]
                                ]
                                Html.ul [
                                    prop.className "mt-4 space-y-3"
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
                                                prop.className "flex flex-col sm:flex-row sm:items-center gap-3 bg-base-300 hover:bg-base-200 transition-colors rounded-lg p-3 sm:p-4"
                                                prop.children [
                                                    Html.div [
                                                        prop.className "flex items-center gap-3 flex-1 min-w-0"
                                                        prop.children [
                                                            match photoUrlThumb item.PhotoPath with
                                                            | Some url ->
                                                                Html.img [
                                                                    prop.className "w-12 h-12 object-cover rounded flex-shrink-0 cursor-pointer hover:opacity-80 transition-opacity"
                                                                    prop.src url
                                                                    prop.onClick (fun e -> e.stopPropagation(); dispatch (ShowImageViewer url))
                                                                ]
                                                            | None -> Html.none
                                                            Html.span [ prop.className "text-sm truncate"; prop.text item.Name ]
                                                        ]
                                                    ]
                                                    Html.div [
                                                        prop.className "flex gap-1 w-full sm:w-auto"
                                                        prop.children [
                                                            Html.button [
                                                                prop.className "btn btn-ghost btn-sm flex-1 sm:flex-none"
                                                                prop.text "Move"
                                                                prop.onClick (fun _ -> dispatch (ShowMoveItemDialog item.Id))
                                                            ]
                                                            Html.button [
                                                                prop.className "btn btn-ghost btn-sm flex-1 sm:flex-none"
                                                                prop.text "Unassign"
                                                                prop.onClick (fun _ -> dispatch (UnassignItem item.Id))
                                                            ]
                                                            Html.button [
                                                                prop.className "btn btn-ghost btn-sm flex-1 sm:flex-none"
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

let private moveItemStandaloneDialog (state: State) (dispatch: Msg -> unit) : ReactElement =
    match state.MovingItemStandaloneId with
    | None -> Html.none
    | Some _ ->
        Html.div [
            prop.className "modal modal-open"
            prop.children [
                Html.div [
                    prop.className "modal-box w-11/12 max-w-md sm:max-w-lg"
                    prop.children [
                        Html.h3 [
                            prop.className "font-bold text-lg mb-4"
                            prop.text "Move item to a box"
                        ]
                        Html.div [
                            prop.className "form-control mb-4"
                            prop.children [
                                Html.label [
                                    prop.className "label pb-3"
                                    prop.children [
                                        Html.span [ prop.className "label-text text-sm font-medium"; prop.text "Select target box" ]
                                    ]
                                ]
                                Html.select [
                                    prop.className "select select-bordered w-full text-base ml-2"
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
                            prop.className "modal-action gap-2"
                            prop.children [
                                Html.button [
                                    prop.className "btn btn-ghost btn-sm"
                                    prop.text "Cancel"
                                    prop.onClick (fun _ -> dispatch CancelMoveItemStandalone)
                                ]
                                Html.button [
                                    prop.className "btn btn-primary btn-sm"
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
        prop.className "card bg-base-200 hover:bg-base-300 transition-colors"
        prop.children [
            Html.div [
                prop.className "card-body p-3 sm:p-4"
                prop.children [
                    Html.div [
                        prop.className "flex flex-col sm:flex-row sm:items-start gap-3"
                        prop.children [
                            match photoUrlThumb item.PhotoPath with
                            | Some url ->
                                Html.img [
                                    prop.className "w-14 h-14 sm:w-16 sm:h-16 object-cover rounded flex-shrink-0 cursor-pointer hover:opacity-80 transition-opacity"
                                    prop.src url
                                    prop.onClick (fun e -> e.stopPropagation(); dispatch (ShowImageViewer url))
                                ]
                            | None ->
                                Html.div [
                                    prop.className "w-14 h-14 sm:w-16 sm:h-16 bg-base-300 rounded flex items-center justify-center flex-shrink-0"
                                    prop.children [
                                        Html.span [ prop.className "text-xl sm:text-2xl opacity-30"; prop.text "?" ]
                                    ]
                                ]
                            Html.div [
                                prop.className "flex-1 min-w-0"
                                prop.children [
                                    if isEditing then
                                        Html.div [
                                            prop.className "flex flex-col gap-2 w-full"
                                            prop.children [
                                                Html.input [
                                                    prop.className "input input-bordered input-sm w-full text-base"
                                                    prop.value state.EditItemNameValue
                                                    prop.onChange (fun (s: string) -> dispatch (EditItemNameChanged s))
                                                ]
                                                Html.div [
                                                    prop.className "flex gap-2"
                                                    prop.children [
                                                        Html.label [
                                                            prop.className "btn btn-secondary btn-xs flex-1 cursor-pointer"
                                                            prop.children [
                                                                Html.text "📷 Take Photo"
                                                                Html.input [
                                                                    prop.type' "file"
                                                                    prop.accept "image/*"
                                                                    prop.custom("capture", "environment")
                                                                    prop.className "hidden"
#if FABLE_COMPILER
                                                                    prop.onChange (fun (files: Browser.Types.File list) ->
                                                                        files |> List.tryHead |> Option.iter (fun f ->
                                                                            dispatch (UploadItemPhoto (item.ItemId, box f))))
#endif
                                                                ]
                                                            ]
                                                        ]
                                                        Html.label [
                                                            prop.className "btn btn-outline btn-xs flex-1 cursor-pointer"
                                                            prop.children [
                                                                Html.text "📁 Choose"
                                                                Html.input [
                                                                    prop.type' "file"
                                                                    prop.accept "image/*"
                                                                    prop.className "hidden"
#if FABLE_COMPILER
                                                                    prop.onChange (fun (files: Browser.Types.File list) ->
                                                                        files |> List.tryHead |> Option.iter (fun f ->
                                                                            dispatch (UploadItemPhoto (item.ItemId, box f))))
#endif
                                                                ]
                                                            ]
                                                        ]
                                                    ]
                                                ]
                                                Html.div [
                                                    prop.className "flex gap-2"
                                                    prop.children [
                                                        Html.button [
                                                            prop.className "btn btn-ghost btn-sm flex-1"
                                                            prop.text "Cancel"
                                                            prop.onClick (fun _ -> dispatch CancelEditItem)
                                                        ]
                                                        Html.button [
                                                            prop.className "btn btn-primary btn-sm flex-1"
                                                            prop.text "Save"
                                                            prop.onClick (fun _ -> dispatch SubmitEditItem)
                                                        ]
                                                    ]
                                                ]
                                            ]
                                        ]
                                    else
                                        Html.p [
                                            prop.className "font-semibold text-sm sm:text-base truncate"
                                            prop.text item.ItemName
                                        ]
                                    Html.p [
                                        prop.className "text-xs sm:text-sm opacity-70 truncate"
                                        prop.children [
                                            if System.String.IsNullOrEmpty item.BoxId then
                                                Html.span [ prop.className "badge badge-ghost badge-xs sm:badge-sm"; prop.text "Unassigned" ]
                                            else
                                                Html.text (item.BoxLabel |> Option.defaultValue item.BoxId)
                                                match item.LocationName with
                                                | Some name -> Html.text $" — %s{name}"
                                                | None -> Html.text ""
                                        ]
                                    ]
                                    if not isEditing then
                                        Html.div [
                                            prop.className "flex gap-1 mt-2 flex-wrap"
                                            prop.children [
                                                Html.button [
                                                    prop.className "btn btn-ghost btn-sm"
                                                    prop.text "Edit"
                                                    prop.onClick (fun _ -> dispatch (StartEditItem (item.ItemId, item.ItemName)))
                                                ]
                                                Html.button [
                                                    prop.className "btn btn-ghost btn-sm"
                                                    prop.text "Move"
                                                    prop.onClick (fun _ -> dispatch (ShowMoveItemStandaloneDialog item.ItemId))
                                                ]
                                                if not (System.String.IsNullOrEmpty item.BoxId) then
                                                    Html.button [
                                                        prop.className "btn btn-ghost btn-sm"
                                                        prop.text "Unassign"
                                                        prop.onClick (fun _ -> dispatch (UnassignStandaloneItem item.ItemId))
                                                    ]
                                                Html.button [
                                                    prop.className "btn btn-ghost btn-sm text-error"
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
                prop.className "flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between mb-6"
                prop.children [
                    Html.h1 [
                        prop.className "text-2xl sm:text-3xl font-bold"
                        prop.text $"Items (%i{state.AllItems.Length})"
                    ]
                    Html.button [
                        prop.className "btn btn-primary btn-sm sm:btn-md w-full sm:w-auto"
                        prop.text "+ New Item"
                        prop.onClick (fun _ -> dispatch ShowCreateItemForm)
                    ]
                ]
            ]
            if state.ShowCreateItemForm then
                Html.div [
                    prop.className "card bg-base-200 border border-base-300 mb-6 shadow-sm"
                    prop.children [
                        Html.div [
                            prop.className "card-body p-4 sm:p-6"
                            prop.children [
                                Html.div [
                                    prop.className "form-control mb-4"
                                    prop.children [
                                        Html.label [
                                            prop.className "label pb-3"
                                            prop.children [
                                                Html.span [ prop.className "label-text text-sm font-medium"; prop.text "Item name" ]
                                            ]
                                        ]
                                        Html.input [
                                            prop.className "input input-bordered focus:input-primary text-base"
                                            prop.placeholder "e.g. Kitchen knife set"
                                            prop.value state.NewStandaloneItemName
                                            prop.onChange (fun (s: string) -> dispatch (NewStandaloneItemNameChanged s))
                                        ]
                                    ]
                                ]
                                Html.div [
                                    prop.className "form-control mb-6"
                                    prop.children [
                                        Html.label [
                                            prop.className "label pb-3"
                                            prop.children [
                                                Html.span [ prop.className "label-text text-sm font-medium"; prop.text "Assign to box (optional)" ]
                                            ]
                                        ]
                                        Html.select [
                                            prop.className "select select-bordered focus:select-primary text-base ml-2"
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
                                    prop.className "flex gap-2 justify-end"
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
                prop.className "form-control mb-4"
                prop.children [
                    Html.input [
                        prop.className "input input-bordered w-full focus:input-primary text-base"
                        prop.placeholder "Search items..."
                        prop.value state.SearchQuery
                        prop.onChange (fun (s: string) -> dispatch (SearchQueryChanged s))
                    ]
                ]
            ]
            if System.String.IsNullOrEmpty state.SearchQuery then
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
            else
                Html.div [
                    prop.className "space-y-3"
                    prop.children [
                        if Array.isEmpty state.SearchResults && not state.Loading then
                            Html.div [
                                prop.className "text-center py-8 opacity-60"
                                prop.children [
                                    Html.p [ prop.text "No items found matching your search" ]
                                ]
                            ]
                        for r in state.SearchResults do
                            Html.div [
                                prop.className "card bg-base-200 hover:bg-base-300 transition-colors"
                                prop.children [
                                    Html.div [
                                        prop.className "card-body flex-col sm:flex-row sm:items-center gap-3 sm:gap-4 p-3 sm:p-4"
                                        prop.children [
                                            match photoUrlThumb r.PhotoPath with
                                            | Some url ->
                                                Html.img [
                                                    prop.className "w-14 h-14 sm:w-16 sm:h-16 object-cover rounded flex-shrink-0 cursor-pointer hover:opacity-80 transition-opacity"
                                                    prop.src url
                                                    prop.onClick (fun e -> e.stopPropagation(); dispatch (ShowImageViewer url))
                                                ]
                                            | None -> Html.none
                                            Html.div [
                                                prop.className "flex-1 min-w-0"
                                                prop.children [
                                                    Html.p [
                                                        prop.className "font-semibold text-sm sm:text-base truncate"
                                                        prop.text r.ItemName
                                                    ]
                                                    Html.p [
                                                        prop.className "text-xs sm:text-sm opacity-70 truncate"
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

let renderPage (state: State) (dispatch: Msg -> unit) : ReactElement =
    Html.div [
        prop.className "min-h-screen bg-base-100"
        prop.children [
            imageViewer state dispatch
            navbar state dispatch
            Html.div [
                prop.className "w-full mx-auto px-3 sm:px-4 md:px-6 py-3 sm:py-4 max-w-6xl"
                prop.children [
                    errorAlert state dispatch
                    match state.CurrentPage with
                    | LocationsList -> locationsPage state dispatch
                    | LocationDetail _ -> locationDetailPage state dispatch
                    | BoxesList -> boxesPage state dispatch
                    | BoxDetail _ -> boxDetailPage state dispatch
                    | ItemsList -> itemsPage state dispatch
                ]
            ]
        ]
    ]
