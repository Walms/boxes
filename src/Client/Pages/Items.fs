module BoxTracker.Client.Pages.Items

#if FABLE_COMPILER
open Browser.Types
open Browser.Dom
#endif
open Feliz
open BoxTracker.Client.State
open BoxTracker.Client.Api
open BoxTracker.Client.Pages.Common

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
                                        Html.span [ prop.className "label-text text-base font-medium"; prop.text "Select target box" ]
                                    ]
                                ]
                                if state.DialogLoading then
                                    dialogLoadingSpinner
                                else
                                    Html.ul [
                                        prop.className "menu bg-base-200 rounded-box w-full p-1 border border-base-300 max-h-64 overflow-y-auto"
                                        prop.children [
                                            for box in state.BoxesForItemMove do
                                                Html.li [
                                                    Html.a [
                                                        prop.className (if state.MoveItemTargetBox = box.Id then "active font-bold" else "")
                                                        prop.text (box.Label |> Option.defaultValue box.Id)
                                                        prop.onClick (fun _ -> dispatch (MoveItemTargetBoxChanged box.Id))
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
                                    prop.onClick (fun _ -> dispatch CancelMoveItemStandalone)
                                ]
                                Html.button [
                                    prop.className "btn btn-success btn-sm"
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

let private itemCard (state: State) (dispatch: Msg -> unit) (index: int) (item: SearchResultDto) : ReactElement =
    let isEditing = state.EditingItemId = Some item.ItemId
    Html.div [
        prop.className "catalog-row entity-item cursor-pointer"
        prop.onClick (fun _ -> if not isEditing then dispatch (Navigate (ItemDetail item.ItemId)))
        prop.children [
            Html.span [
                prop.className "row-index hidden sm:block"
                prop.text (sprintf "%03d" (index + 1))
            ]
            Html.div [
                prop.className "flex-1 min-w-0"
                prop.children [
                    Html.div [
                        prop.className "flex flex-col sm:flex-row sm:items-center gap-2 sm:gap-3"
                        prop.children [
                            match photoUrlThumb item.PhotoPath with
                            | Some thumbUrl ->
                                let fullUrl = photoUrlFull item.PhotoPath |> Option.defaultValue thumbUrl
                                Html.img [
                                    prop.className "w-full h-40 sm:w-10 sm:h-10 object-cover rounded sm:flex-shrink-0 cursor-pointer hover:opacity-80 transition-opacity"
                                    prop.src thumbUrl
                                    prop.custom("loading", "lazy")
                                    prop.custom("decoding", "async")
                                    prop.onClick (fun e -> e.stopPropagation(); dispatch (ShowImageViewer fullUrl))
                                ]
                            | None ->
                                Html.div [
                                    prop.className "hidden sm:flex w-10 h-10 bg-base-200 rounded items-center justify-center flex-shrink-0"
                                    prop.children [
                                        Html.span [ prop.className "text-base opacity-30"; prop.text "📦" ]
                                    ]
                                ]
                            Html.div [
                                prop.className "flex items-center gap-3 flex-1 min-w-0"
                                prop.children [
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
                                                        photoStatusBanner state dispatch
                                                        if not (state.UploadingPhoto || state.PhotoProcessing) then
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
                                                                    prop.className "btn btn-success btn-sm flex-1"
                                                                    prop.text "Save"
                                                                    prop.onClick (fun _ -> dispatch SubmitEditItem)
                                                                ]
                                                            ]
                                                        ]
                                                    ]
                                                ]
                                            else
                                                Html.p [
                                                    prop.className "text-base break-words"
                                                    prop.text item.ItemName
                                                ]
                                                Html.div [
                                                    prop.className "flex flex-wrap gap-1 mt-1"
                                                    prop.children [
                                                        if System.String.IsNullOrEmpty item.BoxId then
                                                            Html.span [ prop.className "badge badge-ghost badge-xs"; prop.text "Unassigned" ]
                                                        else
                                                            Html.button [
                                                                prop.className "btn btn-ghost btn-xs font-normal normal-case"
                                                                prop.onClick (fun e -> e.stopPropagation(); dispatch (Navigate (BoxDetail item.BoxId)))
                                                                prop.text ((item.BoxLabel |> Option.defaultValue item.BoxId) + " →")
                                                            ]
                                                            match item.LocationCode, item.LocationName with
                                                            | Some code, Some name ->
                                                                Html.button [
                                                                    prop.className "btn btn-ghost btn-xs font-normal normal-case"
                                                                    prop.onClick (fun e -> e.stopPropagation(); dispatch (Navigate (LocationDetail code)))
                                                                    prop.text (name + " →")
                                                                ]
                                                            | _ -> Html.none
                                                    ]
                                                ]
                                        ]
                                    ]
                                    if not isEditing then
                                        Html.div [
                                            prop.className "dropdown dropdown-end flex-shrink-0"
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
                                                                prop.onClick (fun _ -> dispatch (ShowHistory ("item", item.ItemId, item.ItemName, Some item.AddedAt)))
                                                            ]
                                                        ]
                                                        Html.li [
                                                            Html.a [
                                                                prop.text "Edit"
                                                                prop.onClick (fun _ -> dispatch (StartEditItem (item.ItemId, item.ItemName)))
                                                            ]
                                                        ]
                                                        Html.li [
                                                            Html.a [
                                                                prop.text "Move to box"
                                                                prop.onClick (fun _ -> dispatch (ShowMoveItemStandaloneDialog item.ItemId))
                                                            ]
                                                        ]
                                                        if not (System.String.IsNullOrEmpty item.BoxId) then
                                                            Html.li [
                                                                Html.a [
                                                                    prop.text "Unassign"
                                                                    prop.onClick (fun _ -> dispatch (UnassignStandaloneItem item.ItemId))
                                                                ]
                                                            ]
                                                        Html.li [
                                                            Html.a [
                                                                prop.className "text-error"
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
            ]
        ]
    ]

let itemsPage (state: State) (dispatch: Msg -> unit) : ReactElement =
    Html.div [
        prop.children [
            moveItemStandaloneDialog state dispatch
            Html.div [
                prop.className "page-header flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between mb-4"
                prop.children [
                    Html.h1 [
                        prop.text $"Items [%03i{state.AllItems.Length}]"
                    ]
                    Html.button [
                        prop.className "btn btn-success btn-sm sm:btn-md w-full sm:w-auto"
                        prop.text "+ New Item"
                        prop.onClick (fun _ -> dispatch ShowCreateItemForm)
                    ]
                ]
            ]
            if state.ShowCreateItemForm then
                Html.div [
                    prop.className "card bg-base-200 border border-base-300 mb-6"
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
                                                Html.span [ prop.className "label-text text-base font-medium"; prop.text "Item name" ]
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
                                                Html.span [ prop.className "label-text text-base font-medium"; prop.text "Assign to box (optional)" ]
                                            ]
                                        ]
                                        Html.select [
                                            prop.className "select select-bordered focus:select-primary text-base w-full"
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
                                            prop.className "btn btn-success btn-sm"
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
                prop.className "flex flex-col sm:flex-row gap-2 mb-4"
                prop.children [
                    Html.div [
                        prop.className "form-control flex-1"
                        prop.children [
                            Html.div [
                                prop.className "relative"
                                prop.children [
                                    Html.input [
                                        prop.className "input input-bordered w-full focus:input-primary text-base"
                                        prop.placeholder "Search items..."
                                        prop.value state.SearchQuery
                                        prop.onChange (fun (s: string) -> dispatch (SearchQueryChanged s))
                                    ]
                                    if state.SearchLoading then
                                        Html.span [
                                            prop.className "loading loading-spinner loading-sm absolute right-3 top-1/2 -translate-y-1/2 opacity-70"
                                        ]
                                ]
                            ]
                        ]
                    ]
                    if System.String.IsNullOrEmpty state.SearchQuery then
                        Html.div [
                            prop.className "form-control"
                            prop.children [
                                Html.select [
                                    prop.className "select select-bordered text-base w-full sm:w-auto"
                                    prop.value (
                                        match state.ItemSortOrder with
                                        | ItemName -> "name"
                                        | ItemDateAdded -> "date"
                                        | ItemBox -> "box")
                                    prop.onChange (fun (s: string) ->
                                        let order =
                                            match s with
                                            | "name" -> ItemName
                                            | "box" -> ItemBox
                                            | _ -> ItemDateAdded
                                        dispatch (ItemSortOrderChanged order))
                                    prop.children [
                                        Html.option [ prop.value "name"; prop.text "Sort: Name (A-Z)" ]
                                        Html.option [ prop.value "date"; prop.text "Sort: Date added" ]
                                        Html.option [ prop.value "box"; prop.text "Sort: Box" ]
                                    ]
                                ]
                            ]
                        ]
                ]
            ]
            let itemsToShow =
                if System.String.IsNullOrEmpty state.SearchQuery then
                    match state.ItemSortOrder with
                    | ItemName -> state.AllItems |> Array.sortBy (fun i -> i.ItemName.ToLowerInvariant())
                    | ItemDateAdded -> state.AllItems |> Array.sortByDescending (fun i -> i.AddedAt)
                    | ItemBox -> state.AllItems |> Array.sortBy (fun i -> (i.BoxLabel |> Option.defaultValue i.BoxId).ToLowerInvariant())
                else state.SearchResults
            let busy = state.Loading || state.SearchLoading
            Html.div [
                prop.className "catalog-list"
                prop.children [
                    if busy && Array.isEmpty itemsToShow then
                        Html.div [
                            prop.className "flex justify-center py-12"
                            prop.children [
                                Html.span [ prop.className "loading loading-spinner loading-lg" ]
                            ]
                        ]
                    elif Array.isEmpty itemsToShow && not busy then
                        Html.div [
                            prop.className "text-center py-8 opacity-60"
                            prop.children [
                                if System.String.IsNullOrEmpty state.SearchQuery then
                                    Html.p [ prop.className "text-lg"; prop.text "No items yet" ]
                                    Html.p [ prop.className "text-sm"; prop.text "Click \"+ New Item\" to create one, or add items to boxes from the box detail page" ]
                                else
                                    Html.p [ prop.text "No items found matching your search" ]
                            ]
                        ]
                    for i in 0 .. itemsToShow.Length - 1 do
                        itemCard state dispatch i itemsToShow.[i]
                ]
            ]
        ]
    ]


let itemDetailPage (state: State) (dispatch: Msg -> unit) : ReactElement =
    match state.ItemDetail with
    | None ->
        if state.Loading then loadingSpinner state
        else Html.div [ prop.text "Item not found" ]
    | Some item ->
        let itemCrumbs =
            if System.String.IsNullOrEmpty item.BoxId then
                [ "Items", Some ItemsList; item.ItemName, None ]
            else
                let boxLabel = item.BoxLabel |> Option.defaultValue item.BoxId
                match item.LocationCode, item.LocationName with
                | Some code, Some locName ->
                    [ "Locations", Some LocationsList; locName, Some (LocationDetail code); boxLabel, Some (BoxDetail item.BoxId); item.ItemName, None ]
                | _ ->
                    [ "Boxes", Some BoxesList; boxLabel, Some (BoxDetail item.BoxId); item.ItemName, None ]
        Html.div [
            prop.children [
                moveItemStandaloneDialog state dispatch
                breadcrumb itemCrumbs dispatch
                Html.div [
                    prop.className "card entity-item mb-6"
                    prop.children [
                        Html.div [
                            prop.className "card-body p-4 sm:p-6"
                            prop.children [
                                Html.div [
                                    prop.className "flex flex-col sm:flex-row sm:items-start sm:justify-between gap-4 mb-4"
                                    prop.children [
                                        if state.EditingItemId = Some item.ItemId then
                                            Html.div [
                                                prop.className "flex flex-col sm:flex-row items-end gap-2 flex-1"
                                                prop.children [
                                                    Html.input [
                                                        prop.className "input input-bordered input-sm flex-1 w-full text-base"
                                                        prop.value state.EditItemNameValue
                                                        prop.onChange (fun (s: string) -> dispatch (EditItemNameChanged s))
                                                    ]
                                                    Html.div [
                                                        prop.className "flex gap-2 w-full sm:w-auto"
                                                        prop.children [
                                                            Html.button [
                                                                prop.className "btn btn-ghost btn-sm flex-1 sm:flex-none"
                                                                prop.text "Cancel"
                                                                prop.onClick (fun _ -> dispatch CancelEditItem)
                                                            ]
                                                            Html.button [
                                                                prop.className "btn btn-success btn-sm flex-1 sm:flex-none"
                                                                prop.text "Save"
                                                                prop.onClick (fun _ -> dispatch SubmitEditItem)
                                                            ]
                                                        ]
                                                    ]
                                                ]
                                            ]
                                        else
                                            Html.h1 [
                                                prop.className "text-xl sm:text-2xl font-bold"
                                                prop.text item.ItemName
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
                                                                    prop.text "Edit Name"
                                                                    prop.onClick (fun _ -> dispatch (StartEditItem (item.ItemId, item.ItemName)))
                                                                ]
                                                            ]
                                                            Html.li [
                                                                Html.a [
                                                                    prop.text "View History"
                                                                    prop.onClick (fun _ -> dispatch (ShowHistory ("item", item.ItemId, item.ItemName, Some item.AddedAt)))
                                                                ]
                                                            ]
                                                            Html.li [
                                                                Html.a [
                                                                    prop.text "Move to box"
                                                                    prop.onClick (fun _ -> dispatch (ShowMoveItemStandaloneDialog item.ItemId))
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
                                                                                files |> List.tryHead |> Option.iter (fun f -> dispatch (UploadItemPhoto (item.ItemId, box f))))
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
                                                                                files |> List.tryHead |> Option.iter (fun f -> dispatch (UploadItemPhoto (item.ItemId, box f))))
#endif
                                                                        ]
                                                                    ]
                                                                ]
                                                            ]
                                                            if not (System.String.IsNullOrEmpty item.BoxId) then
                                                                Html.li [
                                                                    Html.a [
                                                                        prop.text "Unassign from box"
                                                                        prop.onClick (fun _ -> dispatch (UnassignStandaloneItem item.ItemId))
                                                                    ]
                                                                ]
                                                            Html.li [ Html.hr [] ]
                                                            Html.li [
                                                                Html.a [
                                                                    prop.className "text-error"
                                                                    prop.text "Delete Item"
                                                                    prop.onClick (fun _ -> dispatch (DeleteStandaloneItem item.ItemId))
                                                                ]
                                                            ]
                                                        ]
                                                    ]
                                                ]
                                            ]
                                    ]
                                ]
                                match photoUrlThumb item.PhotoPath with
                                | Some thumbUrl ->
                                    let fullUrl = photoUrlFull item.PhotoPath |> Option.defaultValue thumbUrl
                                    Html.div [
                                        prop.className "mb-4"
                                        prop.children [
                                            Html.img [
                                                prop.className "w-full max-w-md h-64 object-contain rounded border border-base-300 cursor-pointer hover:opacity-80 transition-opacity bg-base-200"
                                                prop.src thumbUrl
                                                prop.custom("decoding", "async")
                                                prop.onClick (fun _ -> dispatch (ShowImageViewer fullUrl))
                                            ]
                                        ]
                                    ]
                                | None ->
                                    Html.div [
                                        prop.className "mb-4 p-8 border border-dashed border-base-300 rounded-lg text-center opacity-40"
                                        prop.children [
                                            Html.p [ prop.text "No photo" ]
                                        ]
                                    ]
                                photoStatusBanner state dispatch
                            ]
                        ]
                    ]
                ]
                notesSection state dispatch
            ]
        ]

