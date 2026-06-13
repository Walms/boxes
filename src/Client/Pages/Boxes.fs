module BoxTracker.Client.Pages.Boxes

#if FABLE_COMPILER
open Browser.Types
open Browser.Dom
#endif
open Feliz
open BoxTracker.Client.State
open BoxTracker.Client.Api
open BoxTracker.Client.Pages.Common

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
                                        Html.span [ prop.className "label-text text-base font-medium"; prop.text "Select target box" ]
                                    ]
                                ]
                                if state.DialogLoading then
                                    dialogLoadingSpinner
                                else
                                    Html.ul [
                                        prop.className "menu bg-base-200 rounded-box w-full p-1 border border-base-300 max-h-64 overflow-y-auto"
                                        prop.children [
                                            for box in state.AvailableBoxes do
                                                if box.Id <> currentBoxId then
                                                    Html.li [
                                                        Html.a [
                                                            prop.className (if state.TargetBoxId = box.Id then "active font-bold" else "")
                                                            prop.text (box.Label |> Option.defaultValue box.Id)
                                                            prop.onClick (fun _ -> dispatch (MoveTargetBoxChanged box.Id))
                                                        ]
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
                                    prop.className "btn btn-success btn-sm"
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
                        if state.DialogLoading then
                            dialogLoadingSpinner
                        elif Array.isEmpty state.UnassignedItems then
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
                                                match photoUrlThumb item.PhotoPath with
                                                | Some thumbUrl ->
                                                    let fullUrl = photoUrlFull item.PhotoPath |> Option.defaultValue thumbUrl
                                                    Html.img [
                                                        prop.className "w-10 h-10 object-cover rounded flex-shrink-0 cursor-pointer hover:opacity-80 transition-opacity"
                                                        prop.src thumbUrl
                                                        prop.custom("loading", "lazy")
                                                        prop.custom("decoding", "async")
                                                        prop.onClick (fun e -> e.stopPropagation(); dispatch (ShowImageViewer fullUrl))
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
                                    prop.className "btn btn-success btn-sm"
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
                        prop.className "btn btn-success btn-sm sm:btn-md w-full sm:w-auto"
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
                                                Html.span [ prop.className "label-text text-base font-medium"; prop.text "Label (optional)" ]
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
                                            prop.className "btn btn-success btn-sm"
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
                                prop.className "select select-bordered text-base w-full sm:w-auto"
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
                        prop.className "form-control"
                        prop.children [
                            Html.select [
                                prop.className "select select-bordered text-base w-full sm:w-auto"
                                prop.value (
                                    match state.BoxSortOrder with
                                    | BoxNumber -> "number"
                                    | BoxLabel -> "label"
                                    | BoxDateAdded -> "date")
                                prop.onChange (fun (s: string) ->
                                    let order =
                                        match s with
                                        | "label" -> BoxLabel
                                        | "date" -> BoxDateAdded
                                        | _ -> BoxNumber
                                    dispatch (BoxSortOrderChanged order))
                                prop.children [
                                    Html.option [ prop.value "number"; prop.text "Sort: Box number" ]
                                    Html.option [ prop.value "label"; prop.text "Sort: Label (A-Z)" ]
                                    Html.option [ prop.value "date"; prop.text "Sort: Date added" ]
                                ]
                            ]
                        ]
                    ]
                ]
            ]
            let sortedBoxes =
                match state.BoxSortOrder with
                | BoxNumber -> state.Boxes |> Array.sortBy (fun b -> b.Id)
                | BoxLabel -> state.Boxes |> Array.sortBy (fun b -> (b.Label |> Option.defaultValue b.Id).ToLowerInvariant())
                | BoxDateAdded -> state.Boxes |> Array.sortBy (fun b -> b.CreatedAt)
            let filteredBoxes =
                if System.String.IsNullOrEmpty state.BoxSearch then sortedBoxes
                else
                    let q = state.BoxSearch.ToLowerInvariant()
                    sortedBoxes |> Array.filter (fun b ->
                        (b.Label |> Option.defaultValue b.Id).ToLowerInvariant().Contains(q))
            Html.div [
                prop.className "grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3 sm:gap-4"
                prop.children [
                    if state.Loading && Array.isEmpty state.Boxes then
                        gridLoadingSpinner
                    elif Array.isEmpty state.Boxes then
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
                        Html.div [
                            prop.className "card entity-box cursor-pointer hover:shadow-md transition-shadow"
                            prop.onClick (fun _ -> dispatch (Navigate (BoxDetail box.Id)))
                            prop.children [
                                Html.div [
                                    prop.className "card-body p-4 sm:p-5"
                                    prop.children [
                                        Html.h2 [
                                            prop.className "text-lg break-words"
                                            prop.text (box.Label |> Option.defaultValue box.Id)
                                        ]
                                        match box.LocationCode with
                                        | Some code ->
                                            Html.button [
                                                prop.className "btn btn-ghost btn-xs font-normal normal-case"
                                                prop.text (code + " →")
                                                prop.onClick (fun e -> e.stopPropagation(); dispatch (Navigate (LocationDetail code)))
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
        let boxLabel = detail.Box.Label |> Option.defaultValue detail.Box.Id
        let boxCrumbs =
            if System.String.IsNullOrEmpty state.AssignLocationCode then
                [ "Boxes", Some BoxesList; boxLabel, None ]
            else
                let locName =
                    state.AvailableLocations
                    |> Array.tryFind (fun l -> l.Code = state.AssignLocationCode)
                    |> Option.map (fun l -> l.Name)
                    |> Option.defaultValue state.AssignLocationCode
                [ "Locations", Some LocationsList; locName, Some (LocationDetail state.AssignLocationCode); boxLabel, None ]
        Html.div [
            prop.children [
                moveItemDialog state dispatch
                addExistingItemDialog state dispatch
                breadcrumb boxCrumbs dispatch
                Html.div [
                    prop.className "card entity-box mb-6 shadow-sm"
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
                                                        prop.className "btn btn-success btn-sm flex-1 sm:flex-none"
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
                                                        prop.className "dropdown dropdown-end w-full sm:w-auto"
                                                        prop.children [
                                                            Html.button [
                                                                prop.tabIndex 0
                                                                prop.className "btn btn-outline w-full sm:w-auto text-base font-normal normal-case"
                                                                prop.text "Actions ▾"
                                                            ]
                                                            Html.ul [
                                                                prop.tabIndex 0
                                                                prop.className "dropdown-content menu bg-base-100 rounded-box z-20 w-52 p-1 shadow-lg border border-base-300"
                                                                prop.children [
                                                                    Html.li [
                                                                        Html.a [
                                                                            prop.text "Edit Label"
                                                                            prop.onClick (fun _ -> dispatch StartEditBoxLabel)
                                                                        ]
                                                                    ]
                                                                    Html.li [
                                                                        Html.a [
                                                                            prop.text "Print Label"
                                                                            prop.onClick (fun _ -> dispatch PrintBoxLabel)
                                                                        ]
                                                                    ]
                                                                    Html.li [
                                                                        Html.a [
                                                                            prop.text "View History"
                                                                            prop.onClick (fun _ -> dispatch (ShowHistory ("box", detail.Box.Id, detail.Box.Label |> Option.defaultValue detail.Box.Id, Some detail.Box.CreatedAt)))
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
                                                                                        files |> List.tryHead |> Option.iter (fun f -> dispatch (UploadBoxPhoto (detail.Box.Id, box f))))
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
                                                                                        files |> List.tryHead |> Option.iter (fun f -> dispatch (UploadBoxPhoto (detail.Box.Id, box f))))
#endif
                                                                                ]
                                                                            ]
                                                                        ]
                                                                    ]
                                                                    Html.li [ Html.hr [] ]
                                                                    Html.li [
                                                                        Html.a [
                                                                            prop.className "text-error"
                                                                            prop.text "Delete Box"
                                                                            prop.onClick (fun _ -> dispatch DeleteBox)
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
                                    prop.className "form-control"
                                    prop.children [
                                        Html.label [
                                            prop.className "label pb-3"
                                            prop.children [
                                                Html.span [ prop.className "label-text text-base font-medium"; prop.text "Assign to location" ]
                                                if not (System.String.IsNullOrEmpty state.AssignLocationCode) then
                                                    Html.button [
                                                        prop.className "btn btn-ghost btn-sm"
                                                        prop.text "View location →"
                                                        prop.onClick (fun _ -> dispatch (Navigate (LocationDetail state.AssignLocationCode)))
                                                    ]
                                            ]
                                        ]
                                        Html.div [
                                            prop.className "dropdown w-full"
                                            prop.children [
                                                Html.button [
                                                    prop.tabIndex 0
                                                    prop.className "btn btn-outline w-full text-base font-normal normal-case"
                                                    prop.children [
                                                        Html.span [
                                                            let selectedName =
                                                                if System.String.IsNullOrEmpty state.AssignLocationCode then "Unassigned"
                                                                else
                                                                    state.AvailableLocations
                                                                    |> Array.tryFind (fun l -> l.Code = state.AssignLocationCode)
                                                                    |> Option.map (fun l -> l.Name)
                                                                    |> Option.defaultValue "Unassigned"
                                                            prop.text selectedName
                                                        ]
                                                        Html.span [ prop.text "▾" ]
                                                    ]
                                                ]
                                                Html.ul [
                                                    prop.tabIndex 0
                                                    prop.className "dropdown-content menu bg-base-200 z-20 w-full p-1 shadow-lg border border-base-300"
                                                    prop.children [
                                                        Html.li [
                                                            Html.a [
                                                                if System.String.IsNullOrEmpty state.AssignLocationCode then
                                                                    prop.className "font-bold"
                                                                prop.text "Unassigned"
                                                                prop.onClick (fun _ -> dispatch (AssignBoxToLocation ""))
                                                            ]
                                                        ]
                                                        for loc in state.AvailableLocations do
                                                            if not loc.IsArchived then
                                                                Html.li [
                                                                    Html.a [
                                                                        if state.AssignLocationCode = loc.Code then
                                                                            prop.className "font-bold"
                                                                        prop.text loc.Name
                                                                        prop.onClick (fun _ -> dispatch (AssignBoxToLocation loc.Code))
                                                                    ]
                                                                ]
                                                    ]
                                                ]
                                            ]
                                        ]
                                    ]
                                ]
                                match photoUrlThumb detail.Box.PhotoPath with
                                | Some thumbUrl ->
                                    let fullUrl = photoUrlFull detail.Box.PhotoPath |> Option.defaultValue thumbUrl
                                    Html.div [
                                        prop.className "mt-2"
                                        prop.children [
                                            Html.img [
                                                prop.className "w-full h-48 sm:w-32 sm:h-32 object-cover rounded border border-base-300 cursor-pointer hover:opacity-80 transition-opacity"
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
                    prop.className "card entity-item mb-6 shadow-sm"
                    prop.children [
                        Html.div [
                            prop.className "card-body p-4 sm:p-6"
                            prop.children [
                                Html.div [
                                    prop.className "flex items-center justify-between mb-4"
                                    prop.children [
                                        Html.h2 [
                                            prop.className "text-lg sm:text-xl font-bold"
                                            prop.text $"Items (%i{detail.Items.Length})"
                                        ]
                                        Html.div [
                                            prop.className "flex gap-2"
                                            prop.children [
                                                Html.button [
                                                    prop.className "btn btn-ghost btn-sm"
                                                    prop.text "+ Existing"
                                                    prop.onClick (fun _ -> dispatch ShowAddExistingItemDialog)
                                                ]
                                            ]
                                        ]
                                    ]
                                ]
                                Html.details [
                                    prop.className "mb-4"
                                    prop.children [
                                        Html.summary [
                                            prop.className "btn btn-outline btn-sm w-full cursor-pointer"
                                            prop.children [ Html.text "+ Add New Item" ]
                                        ]
                                        Html.div [
                                            prop.className "flex flex-col gap-2 mt-3"
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
                                                    prop.className "btn btn-success w-full"
                                                    prop.text "Add Item"
                                                    prop.onClick (fun _ -> dispatch SubmitAddItem)
                                                ]
                                            ]
                                        ]
                                    ]
                                ]
                                Html.ul [
                                    prop.className "space-y-1"
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
                                                prop.className "flex flex-col sm:flex-row sm:items-center gap-3 rounded-lg px-3 py-2 hover:bg-base-200 transition-colors cursor-pointer"
                                                prop.onClick (fun _ -> dispatch (Navigate (ItemDetail item.Id)))
                                                prop.children [
                                                    match photoUrlThumb item.PhotoPath with
                                                    | Some thumbUrl ->
                                                        let fullUrl = photoUrlFull item.PhotoPath |> Option.defaultValue thumbUrl
                                                        Html.img [
                                                            prop.className "w-full h-40 sm:w-9 sm:h-9 object-cover rounded sm:flex-shrink-0 cursor-pointer hover:opacity-80 transition-opacity"
                                                            prop.src thumbUrl
                                                            prop.custom("loading", "lazy")
                                                            prop.custom("decoding", "async")
                                                            prop.onClick (fun e -> e.stopPropagation(); dispatch (ShowImageViewer fullUrl))
                                                        ]
                                                    | None -> Html.none
                                                    Html.span [ prop.className "text-sm flex-1 truncate"; prop.text item.Name ]
                                                    Html.div [
                                                        prop.className "dropdown dropdown-end"
                                                        prop.children [
                                                            Html.button [
                                                                prop.tabIndex 0
                                                                prop.className "btn btn-ghost btn-sm btn-circle opacity-50 hover:opacity-100"
                                                                prop.onClick (fun e -> e.stopPropagation())
                                                                prop.children [ Html.text "⋮" ]
                                                            ]
                                                            Html.ul [
                                                                prop.tabIndex 0
                                                                prop.className "dropdown-content menu bg-base-100 rounded-box z-20 w-44 p-1 shadow-lg border border-base-300"
                                                                prop.children [
                                                                    Html.li [
                                                                        Html.a [
                                                                            prop.text "View History"
                                                                            prop.onClick (fun _ -> dispatch (ShowHistory ("item", item.Id, item.Name, Some item.AddedAt)))
                                                                        ]
                                                                    ]
                                                                    Html.li [
                                                                        Html.a [
                                                                            prop.text "Move to box"
                                                                            prop.onClick (fun _ -> dispatch (ShowMoveItemDialog item.Id))
                                                                        ]
                                                                    ]
                                                                    Html.li [
                                                                        Html.a [
                                                                            prop.text "Unassign"
                                                                            prop.onClick (fun _ -> dispatch (UnassignItem item.Id))
                                                                        ]
                                                                    ]
                                                                    Html.li [
                                                                        Html.a [
                                                                            prop.className "text-error"
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
                ]
                notesSection state dispatch
            ]
        ]

