module BoxTracker.Client.Pages.Locations

#if FABLE_COMPILER
open Browser.Types
open Browser.Dom
#endif
open Feliz
open BoxTracker.Client.State
open BoxTracker.Client.Api
open BoxTracker.Client.Pages.Common

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
                        prop.className "btn btn-success btn-sm sm:btn-md w-full sm:w-auto"
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
                                                Html.span [ prop.className "label-text text-xl font-medium"; prop.text "Code" ]
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
                                                Html.span [ prop.className "label-text text-xl font-medium"; prop.text "Name" ]
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
                                            prop.className "btn btn-success btn-sm"
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
                    if state.Loading && Array.isEmpty state.Locations then
                        gridLoadingSpinner
                    elif Array.isEmpty state.Locations then
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
                        Html.div [
                            prop.className "card entity-location cursor-pointer hover:shadow-md transition-shadow"
                            prop.onClick (fun _ -> dispatch (Navigate (LocationDetail loc.Code)))
                            prop.children [
                                Html.div [
                                    prop.className "card-body p-4 sm:p-5"
                                    prop.children [
                                        Html.h2 [
                                            prop.className "text-base break-words flex items-center gap-2 flex-wrap"
                                            prop.children [
                                                Html.text loc.Name
                                                if loc.IsArchived then
                                                    Html.span [ prop.className "badge badge-ghost badge-sm"; prop.text "Archived" ]
                                            ]
                                        ]
                                        Html.p [ prop.className "text-sm opacity-70"; prop.text loc.Code ]
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
                        if state.DialogLoading then
                            dialogLoadingSpinner
                        elif Array.isEmpty state.BoxesForLocationMove then
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
                                    prop.className "btn btn-success btn-sm"
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
                breadcrumb [ "Locations", Some LocationsList; detail.Location.Name, None ] dispatch
                Html.div [
                    prop.className "card entity-location mb-6 shadow-sm"
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
                                                                prop.className "btn btn-success btn-sm flex-1 sm:flex-none"
                                                                prop.text "Save"
                                                                prop.onClick (fun _ -> dispatch SubmitEditLocationName)
                                                            ]
                                                        ]
                                                    ]
                                                ]
                                            ]
                                        elif state.EditingLocationCode then
                                            Html.div [
                                                prop.className "flex flex-col sm:flex-row items-end gap-2"
                                                prop.children [
                                                    Html.input [
                                                        prop.className "input input-bordered input-sm flex-1 w-full text-base font-mono uppercase"
                                                        prop.value state.EditLocationCodeValue
                                                        prop.onChange (fun (s: string) -> dispatch (EditLocationCodeChanged s))
                                                    ]
                                                    Html.div [
                                                        prop.className "flex gap-2 w-full sm:w-auto"
                                                        prop.children [
                                                            Html.button [
                                                                prop.className "btn btn-ghost btn-sm flex-1 sm:flex-none"
                                                                prop.text "Cancel"
                                                                prop.onClick (fun _ -> dispatch CancelEditLocationCode)
                                                            ]
                                                            Html.button [
                                                                prop.className "btn btn-success btn-sm flex-1 sm:flex-none"
                                                                prop.text "Save"
                                                                prop.onClick (fun _ -> dispatch SubmitEditLocationCode)
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
                                                        prop.className "dropdown dropdown-end flex-shrink-0"
                                                        prop.children [
                                                            Html.button [
                                                                prop.tabIndex 0
                                                                prop.className "btn btn-sm"
                                                                prop.text "Actions ▾"
                                                            ]
                                                            Html.ul [
                                                                prop.tabIndex 0
                                                                prop.className "dropdown-content menu bg-base-100 rounded-box z-20 w-52 p-1 shadow-lg border border-base-300"
                                                                prop.children [
                                                                    Html.li [
                                                                        Html.a [
                                                                            prop.text "Edit Name"
                                                                            prop.onClick (fun _ -> dispatch StartEditLocationName)
                                                                        ]
                                                                    ]
                                                                    Html.li [
                                                                        Html.a [
                                                                            prop.text "Edit Code"
                                                                            prop.onClick (fun _ -> dispatch StartEditLocationCode)
                                                                        ]
                                                                    ]
                                                                    Html.li [
                                                                        Html.a [
                                                                            prop.text "Print Label"
                                                                            prop.onClick (fun _ -> dispatch PrintLocationLabel)
                                                                        ]
                                                                    ]
                                                                    Html.li [
                                                                        Html.label [
                                                                            prop.className "cursor-pointer"
                                                                            prop.children [
                                                                                Html.text "Take Photo"
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
                                                                    ]
                                                                    Html.li [
                                                                        Html.label [
                                                                            prop.className "cursor-pointer"
                                                                            prop.children [
                                                                                Html.text "Choose Photo"
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
                                                                    Html.li [ Html.hr [] ]
                                                                    Html.li [
                                                                        Html.a [
                                                                            prop.className "text-warning"
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
                                ]
                                match photoUrlThumb detail.Location.PhotoPath with
                                | Some thumbUrl ->
                                    let fullUrl = photoUrlFull detail.Location.PhotoPath |> Option.defaultValue thumbUrl
                                    Html.div [
                                        prop.className "mt-2"
                                        prop.children [
                                            Html.img [
                                                prop.className "w-32 h-32 object-cover rounded border border-base-300 cursor-pointer hover:opacity-80 transition-opacity"
                                                prop.src thumbUrl
                                                prop.custom("decoding", "async")
                                                prop.onClick (fun _ -> dispatch (ShowImageViewer fullUrl))
                                            ]
                                        ]
                                    ]
                                | None -> Html.none
                                photoStatusBanner state dispatch
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
                                prop.className "card entity-box cursor-pointer hover:shadow-md transition-shadow"
                                prop.onClick (fun _ -> dispatch (Navigate (BoxDetail box.Id)))
                                prop.children [
                                    Html.div [
                                        prop.className "card-body p-4"
                                        prop.children [
                                            Html.span [
                                                prop.className "card-title"
                                                prop.text (box.Label |> Option.defaultValue box.Id)
                                            ]
                                        ]
                                    ]
                                ]
                            ]
                    ]
                ]
                notesSection state dispatch
            ]
        ]

