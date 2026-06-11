module BoxTracker.Client.State

open Elmish
open BoxTracker.Client.Api

type Page =
    | LocationsList
    | LocationDetail of string
    | BoxesList
    | BoxDetail of string
    | ItemsList
    | ItemDetail of string

type Msg =
    | Navigate of Page
    | HashChanged of Page
    | ErrorOccurred of string
    | DismissError
    | ShowImageViewer of string
    | CloseImageViewer
    | LocationsLoaded of Result<LocationDto array, string>
    | ShowCreateLocationForm
    | NewLocationCodeChanged of string
    | NewLocationNameChanged of string
    | SubmitCreateLocation
    | LocationCreated of Result<LocationDto, string>
    | LocationDetailLoaded of Result<LocationDetailDto, string>
    | StartEditLocationName
    | EditLocationNameChanged of string
    | SubmitEditLocationName
    | LocationUpdated of Result<LocationDto, string>
    | CancelEditLocationName
    | StartEditLocationCode
    | EditLocationCodeChanged of string
    | SubmitEditLocationCode
    | LocationCodeUpdated of Result<LocationDto, string>
    | CancelEditLocationCode
    | ArchiveLocation
    | LocationArchived of Result<LocationDto, string>
    | BoxesLoaded of Result<BoxDto array, string>
    | BoxFilterChanged of string
    | LocationSearchChanged of string
    | BoxSearchChanged of string
    | ShowCreateBoxForm
    | NewBoxLabelChanged of string
    | SubmitCreateBox
    | BoxCreated of Result<BoxDto, string>
    | BoxDetailLoaded of Result<BoxDetailDto, string>
    | AvailableLocationsLoaded of Result<LocationDto array, string>
    | StartEditBoxLabel
    | EditBoxLabelChanged of string
    | SubmitEditBoxLabel
    | BoxUpdated of Result<BoxDto, string>
    | CancelEditBoxLabel
    | AssignBoxToLocation of string
    | DeleteBox
    | BoxDeleted of Result<unit, string>
    | UploadBoxPhoto of string * obj
    | NewItemNameChanged of string
    | PhotoSelected of obj
    | SubmitAddItem
    | ItemAdded of Result<AddItemResultDto, string>
    | DeleteItem of string
    | ItemDeleted of string * Result<unit, string>
    | ShowMoveItemDialog of string
    | BoxesForMoveLoaded of Result<BoxDto array, string>
    | MoveTargetBoxChanged of string
    | ConfirmMoveItem
    | ItemMoved of Result<MoveDto, string>
    | CancelMoveItem
    | SearchQueryChanged of string
    | SearchDebounceTriggered of string
    | SearchResultsLoaded of Result<SearchResultDto array, string>
    | AllItemsLoaded of Result<SearchResultDto array, string>
    | ShowCreateItemForm
    | NewStandaloneItemNameChanged of string
    | NewStandaloneItemBoxChanged of string
    | SubmitCreateStandaloneItem
    | StandaloneItemCreated of Result<ItemDto, string>
    | StartEditItem of string * string
    | EditItemNameChanged of string
    | SubmitEditItem
    | ItemNameUpdated of Result<ItemDto, string>
    | CancelEditItem
    | DeleteStandaloneItem of string
    | StandaloneItemDeleted of string * Result<unit, string>
    | ShowMoveItemStandaloneDialog of string
    | MoveItemTargetBoxChanged of string
    | ConfirmMoveItemStandalone
    | StandaloneItemMoved of Result<MoveDto, string>
    | CancelMoveItemStandalone
    | BoxesForItemMoveLoaded of BoxDto array
    | UploadItemPhoto of string * obj
    | ShowAddExistingItemDialog
    | UnassignedItemsLoaded of Result<SearchResultDto array, string>
    | SelectedExistingItemChanged of string
    | ConfirmAddExistingItem
    | CancelAddExistingItem
    | ExistingItemAdded of Result<MoveDto, string>
    | UnassignItem of string
    | ItemUnassigned of Result<MoveDto, string>
    | UnassignStandaloneItem of string
    | StandaloneItemUnassigned of Result<MoveDto, string>
    | ShowAddBoxToLocationDialog
    | BoxesForLocationMoveLoaded of BoxDto array
    | SelectedBoxForLocationMoveChanged of string
    | ConfirmAddBoxToLocation
    | BoxMovedToLocation of Result<MoveDto, string>
    | CancelAddBoxToLocation
    | UnassignBoxFromLocation of string
    | BoxUnassignedFromLocation of Result<MoveDto, string>
    | PrintBoxLabel
    | PrintLocationLabel
    | PrintMultipleBoxLabels
    | StartEditBoxInList of string * string option
    | EditBoxLabelInListChanged of string
    | SubmitEditBoxInList
    | CancelEditBoxInList
    | BoxUpdatedInList of Result<BoxDto, string>
    | DeleteBoxFromList of string
    | BoxDeletedFromList of string * Result<unit, string>
    | StartEditLocationInList of string * string
    | EditLocationNameInListChanged of string
    | SubmitEditLocationInList
    | CancelEditLocationInList
    | LocationUpdatedInList of Result<LocationDto, string>
    | ArchiveLocationFromList of string
    | LocationArchivedFromList of Result<LocationDto, string>
    | UploadLocationPhoto of string * obj
    | PhotoUploadStarted of Result<PhotoJobDto, string>
    | PollPhotoJob of string
    | PhotoJobPolled of Result<PhotoJobDto, string>
    | DismissPhotoProcessing
    | ShowHistory of string * string * string * string option
    | HistoryLoaded of Result<MoveDto array, string>
    | CloseHistory
    | OpenScanner
    | CloseScanner
    | QrScanned of string
    | ItemDetailLoaded of Result<SearchResultDto array, string>
    | NotesLoaded of Result<NoteDto array, string>
    | ShowAddNoteForm
    | NewNoteContentChanged of string
    | SubmitCreateNote
    | NoteCreated of Result<NoteDto, string>
    | StartEditNote of string * string
    | EditNoteContentChanged of string
    | SubmitEditNote
    | NoteUpdated of Result<NoteDto, string>
    | CancelEditNote
    | DeleteNote of string
    | NoteDeleted of Result<unit, string>
    | CancelAddNote

type State = {
    CurrentPage: Page
    Loading: bool
    UploadingPhoto: bool
    PhotoProcessing: bool
    PhotoJobId: string option
    Error: string option
    ViewingImageUrl: string option
    Locations: LocationDto array
    LocationSearch: string
    LocationDetail: LocationDetailDto option
    ShowCreateLocationForm: bool
    NewLocationCode: string
    NewLocationName: string
    EditingLocationName: bool
    EditLocationNameValue: string
    EditingLocationCode: bool
    EditLocationCodeValue: string
    Boxes: BoxDto array
    BoxSearch: string
    BoxFilter: string
    ShowCreateBoxForm: bool
    NewBoxLabel: string
    BoxDetail: BoxDetailDto option
    AvailableLocations: LocationDto array
    EditingBoxLabel: bool
    EditBoxLabelValue: string
    AssignLocationCode: string
    NewItemName: string
    SelectedPhoto: obj option
    MovingItemId: string option
    TargetBoxId: string
    AvailableBoxes: BoxDto array
    SearchQuery: string
    SearchResults: SearchResultDto array
    SearchDebounceTimer: int option
    AllItems: SearchResultDto array
    ShowCreateItemForm: bool
    NewStandaloneItemName: string
    NewStandaloneItemBoxId: string
    EditingItemId: string option
    EditItemNameValue: string
    MovingItemStandaloneId: string option
    MoveItemTargetBox: string
    BoxesForItemMove: BoxDto array
    AddingExistingItem: bool
    UnassignedItems: SearchResultDto array
    SelectedExistingItemId: string
    AddingBoxToLocation: bool
    BoxesForLocationMove: BoxDto array
    SelectedBoxForLocationMove: string
    EditingBoxIdInList: string option
    EditBoxLabelInListValue: string
    EditingLocationCodeInList: string option
    EditLocationNameInListValue: string
    ShowHistoryModal: bool
    HistoryTitle: string
    HistoryEntityType: string
    HistoryEntityId: string
    HistoryCreatedAt: string option
    HistoryMoves: MoveDto array
    HistoryLoading: bool
    DialogLoading: bool
    SearchLoading: bool
    ScannerOpen: bool
    ItemDetail: SearchResultDto option
    Notes: NoteDto array
    NoteEntityType: string
    NoteEntityId: string
    ShowAddNoteForm: bool
    NewNoteContent: string
    EditingNoteId: string option
    EditNoteContent: string
    DeletingNoteId: string option
}

[<Fable.Core.Emit("window.location.hash")>]
let private getHash () : string = failwith "JS only"

[<Fable.Core.Emit("window.location.hash = $0")>]
let private setHash (hash: string) : unit = failwith "JS only"

[<Fable.Core.Emit("window.addEventListener('hashchange', $0)")>]
let private addHashChangeListener (callback: unit -> unit) : unit = failwith "JS only"

[<Fable.Core.Emit("clearTimeout($0)")>]
let private clearTimeout (id: int) : unit = failwith "JS only"

[<Fable.Core.Emit("setTimeout($0, $1)")>]
let private setTimeout (callback: unit -> unit) (ms: int) : int = failwith "JS only"

[<Fable.Core.Emit("window.open($0, $1)")>]
let private openWindow (url: string) (target: string) : unit = failwith "JS only"

let private pageFromHash (hash: string) : Page =
    match hash.TrimStart('#') with
    | "" | "/" | "/locations" -> LocationsList
    | s when s.StartsWith("/locations/") -> LocationDetail(s.[11..])
    | "/boxes" -> BoxesList
    | s when s.StartsWith("/boxes/") -> BoxDetail(s.[7..])
    | "/items" -> ItemsList
    | s when s.StartsWith("/items/") -> ItemDetail(s.[7..])
    | _ -> LocationsList

let private hashFromPage (page: Page) : string =
    match page with
    | LocationsList -> "#/locations"
    | LocationDetail code -> $"#/locations/%s{code}"
    | BoxesList -> "#/boxes"
    | BoxDetail id -> $"#/boxes/%s{id}"
    | ItemsList -> "#/items"
    | ItemDetail id -> $"#/items/%s{id}"

let private pageEqual (a: Page) (b: Page) : bool =
    match a, b with
    | LocationsList, LocationsList -> true
    | LocationDetail c1, LocationDetail c2 -> c1 = c2
    | BoxesList, BoxesList -> true
    | BoxDetail id1, BoxDetail id2 -> id1 = id2
    | ItemsList, ItemsList -> true
    | ItemDetail id1, ItemDetail id2 -> id1 = id2
    | _ -> false

let private loadPage (page: Page) : Cmd<Msg> =
    match page with
    | LocationsList ->
        Cmd.OfAsync.either getLocations () LocationsLoaded (fun ex -> ErrorOccurred ex.Message)
    | LocationDetail code ->
        Cmd.batch [
            Cmd.OfAsync.either getLocationDetail code LocationDetailLoaded (fun ex -> ErrorOccurred ex.Message)
            Cmd.OfAsync.either (fun () -> getNotes "location" code) () NotesLoaded (fun ex -> ErrorOccurred ex.Message)
        ]
    | BoxesList ->
        Cmd.batch [
            Cmd.OfAsync.either getLocations () LocationsLoaded (fun ex -> ErrorOccurred ex.Message)
            Cmd.OfAsync.either (fun () -> getBoxes None) () BoxesLoaded (fun ex -> ErrorOccurred ex.Message)
        ]
    | BoxDetail id ->
        Cmd.batch [
            Cmd.OfAsync.either getBoxDetail id BoxDetailLoaded (fun ex -> ErrorOccurred ex.Message)
            Cmd.OfAsync.either getLocations () AvailableLocationsLoaded (fun ex -> ErrorOccurred ex.Message)
            Cmd.OfAsync.either (fun () -> getNotes "box" id) () NotesLoaded (fun ex -> ErrorOccurred ex.Message)
        ]
    | ItemsList ->
        Cmd.batch [
            Cmd.OfAsync.either listItems () AllItemsLoaded (fun ex -> ErrorOccurred ex.Message)
            Cmd.OfAsync.either getLocations () LocationsLoaded (fun ex -> ErrorOccurred ex.Message)
            Cmd.OfAsync.either (fun () -> getBoxes None) () BoxesLoaded (fun ex -> ErrorOccurred ex.Message)
        ]
    | ItemDetail id ->
        Cmd.batch [
            Cmd.OfAsync.either listItems () ItemDetailLoaded (fun ex -> ErrorOccurred ex.Message)
            Cmd.OfAsync.either getLocations () AvailableLocationsLoaded (fun ex -> ErrorOccurred ex.Message)
            Cmd.OfAsync.either (fun () -> getNotes "item" id) () NotesLoaded (fun ex -> ErrorOccurred ex.Message)
        ]

let private hashChangeSub (dispatch: Msg -> unit) : unit =
    addHashChangeListener (fun () ->
        let hash : string = getHash ()
        let page : Page = pageFromHash hash
        dispatch (HashChanged page)
    )

let private resetPageState (state: State) : State =
    { state with
        PhotoProcessing = false
        PhotoJobId = None
        ViewingImageUrl = None
        LocationSearch = ""
        LocationDetail = None
        ShowCreateLocationForm = false
        NewLocationCode = ""
        NewLocationName = ""
        EditingLocationName = false
        EditLocationNameValue = ""
        EditingLocationCode = false
        EditLocationCodeValue = ""
        ShowCreateBoxForm = false
        NewBoxLabel = ""
        BoxDetail = None
        EditingBoxLabel = false
        EditBoxLabelValue = ""
        AssignLocationCode = ""
        NewItemName = ""
        SelectedPhoto = None
        MovingItemId = None
        TargetBoxId = ""
        AvailableBoxes = [||]
        BoxSearch = ""
        SearchDebounceTimer = None
        AllItems = [||]
        ShowCreateItemForm = false
        NewStandaloneItemName = ""
        NewStandaloneItemBoxId = ""
        EditingItemId = None
        EditItemNameValue = ""
        MovingItemStandaloneId = None
        MoveItemTargetBox = ""
        BoxesForItemMove = [||]
        AddingExistingItem = false
        UnassignedItems = [||]
        SelectedExistingItemId = ""
        AddingBoxToLocation = false
        BoxesForLocationMove = [||]
        SelectedBoxForLocationMove = ""
        EditingBoxIdInList = None
        EditBoxLabelInListValue = ""
        EditingLocationCodeInList = None
        EditLocationNameInListValue = ""
        ShowHistoryModal = false
        HistoryTitle = ""
        HistoryEntityType = ""
        HistoryEntityId = ""
        HistoryCreatedAt = None
        HistoryMoves = [||]
        HistoryLoading = false
        DialogLoading = false
        SearchLoading = false
        ScannerOpen = false
        ItemDetail = None
        Notes = [||]
        NoteEntityType = ""
        NoteEntityId = ""
        ShowAddNoteForm = false
        NewNoteContent = ""
        EditingNoteId = None
        EditNoteContent = ""
        DeletingNoteId = None
    }

let private navigateCmd (page: Page) : Cmd<Msg> =
    Cmd.ofEffect (fun (dispatch: Msg -> unit) -> dispatch (Navigate page))

/// Poll a server-side photo job again after a short delay.
let private schedulePollCmd (jobId: string) : Cmd<Msg> =
    Cmd.ofEffect (fun (dispatch: Msg -> unit) ->
        setTimeout (fun () -> dispatch (PollPhotoJob jobId)) 800 |> ignore)

/// Patch a freshly processed photo onto whichever loaded entities match the
/// completed job, so the new image appears without reloading the page.
let private applyCompletedPhoto (job: PhotoJobDto) (state: State) : State =
    match job.PhotoPath with
    | None -> state
    | Some _ ->
        match job.EntityType with
        | "item" ->
            let patchResult (r: SearchResultDto) : SearchResultDto =
                if r.ItemId = job.EntityId then { r with PhotoPath = job.PhotoPath } else r
            let patchItem (i: ItemDto) : ItemDto =
                if i.Id = job.EntityId then { i with PhotoPath = job.PhotoPath } else i
            { state with
                BoxDetail = state.BoxDetail |> Option.map (fun d -> { d with Items = d.Items |> Array.map patchItem })
                AllItems = state.AllItems |> Array.map patchResult
                SearchResults = state.SearchResults |> Array.map patchResult
                ItemDetail = state.ItemDetail |> Option.map patchResult }
        | "box" ->
            let patchBox (b: BoxDto) : BoxDto =
                if b.Id = job.EntityId then { b with PhotoPath = job.PhotoPath } else b
            { state with
                Boxes = state.Boxes |> Array.map patchBox
                BoxDetail = state.BoxDetail |> Option.map (fun d -> { d with Box = patchBox d.Box })
                LocationDetail = state.LocationDetail |> Option.map (fun d -> { d with Boxes = d.Boxes |> Array.map patchBox }) }
        | "location" ->
            let patchLocation (l: LocationDto) : LocationDto =
                if l.Code = job.EntityId then { l with PhotoPath = job.PhotoPath } else l
            { state with
                Locations = state.Locations |> Array.map patchLocation
                LocationDetail = state.LocationDetail |> Option.map (fun d -> { d with Location = patchLocation d.Location }) }
        | _ -> state

let init () : State * Cmd<Msg> =
    let hash : string = getHash ()
    let page : Page = pageFromHash hash
    let state : State = {
        CurrentPage = page
        Loading = true
        UploadingPhoto = false
        PhotoProcessing = false
        PhotoJobId = None
        Error = None
        ViewingImageUrl = None
        Locations = [||]
        LocationSearch = ""
        LocationDetail = None
        ShowCreateLocationForm = false
        NewLocationCode = ""
        NewLocationName = ""
        EditingLocationName = false
        EditLocationNameValue = ""
        EditingLocationCode = false
        EditLocationCodeValue = ""
        Boxes = [||]
        BoxSearch = ""
        BoxFilter = ""
        ShowCreateBoxForm = false
        NewBoxLabel = ""
        BoxDetail = None
        AvailableLocations = [||]
        EditingBoxLabel = false
        EditBoxLabelValue = ""
        AssignLocationCode = ""
        NewItemName = ""
        SelectedPhoto = None
        MovingItemId = None
        TargetBoxId = ""
        AvailableBoxes = [||]
        SearchQuery = ""
        SearchResults = [||]
        SearchDebounceTimer = None
        AllItems = [||]
        ShowCreateItemForm = false
        NewStandaloneItemName = ""
        NewStandaloneItemBoxId = ""
        EditingItemId = None
        EditItemNameValue = ""
        MovingItemStandaloneId = None
        MoveItemTargetBox = ""
        BoxesForItemMove = [||]
        AddingExistingItem = false
        UnassignedItems = [||]
        SelectedExistingItemId = ""
        AddingBoxToLocation = false
        BoxesForLocationMove = [||]
        SelectedBoxForLocationMove = ""
        EditingBoxIdInList = None
        EditBoxLabelInListValue = ""
        EditingLocationCodeInList = None
        EditLocationNameInListValue = ""
        ShowHistoryModal = false
        HistoryTitle = ""
        HistoryEntityType = ""
        HistoryEntityId = ""
        HistoryCreatedAt = None
        HistoryMoves = [||]
        HistoryLoading = false
        DialogLoading = false
        SearchLoading = false
        ScannerOpen = false
        ItemDetail = None
        Notes = [||]
        NoteEntityType = ""
        NoteEntityId = ""
        ShowAddNoteForm = false
        NewNoteContent = ""
        EditingNoteId = None
        EditNoteContent = ""
        DeletingNoteId = None
    }
    let cmds : Cmd<Msg> = Cmd.batch [
        Cmd.ofEffect hashChangeSub
        loadPage page
    ]
    state, cmds

let update (msg: Msg) (state: State) : State * Cmd<Msg> =
    match msg with
    | Navigate page ->
        let hash : string = hashFromPage page
        setHash hash
        { resetPageState state with CurrentPage = page; Loading = true; Error = None }, loadPage page

    | HashChanged page ->
        if pageEqual page state.CurrentPage then state, Cmd.none
        else
            { resetPageState state with CurrentPage = page; Loading = true; Error = None }, loadPage page

    | ErrorOccurred err ->
        { state with Error = Some err; Loading = false }, Cmd.none

    | DismissError ->
        { state with Error = None }, Cmd.none

    | ShowImageViewer url ->
        { state with ViewingImageUrl = Some url }, Cmd.none

    | CloseImageViewer ->
        { state with ViewingImageUrl = None }, Cmd.none

    | LocationsLoaded (Ok locations) ->
        { state with Locations = locations; Loading = false }, Cmd.none

    | LocationsLoaded (Error err) ->
        { state with Error = Some err; Loading = false }, Cmd.none

    | ShowCreateLocationForm ->
        { state with ShowCreateLocationForm = true; NewLocationCode = ""; NewLocationName = "" }, Cmd.none

    | NewLocationCodeChanged code ->
        { state with NewLocationCode = code }, Cmd.none

    | NewLocationNameChanged name ->
        { state with NewLocationName = name }, Cmd.none

    | SubmitCreateLocation ->
        let code : string = state.NewLocationCode.Trim()
        let name : string = state.NewLocationName.Trim()
        if System.String.IsNullOrEmpty code || System.String.IsNullOrEmpty name then
            state, Cmd.none
        else
            { state with Loading = true },
            Cmd.OfAsync.either (fun () -> createLocation code name) () LocationCreated (fun ex -> ErrorOccurred ex.Message)

    | LocationCreated (Ok location) ->
        let locations : LocationDto array =
            Array.append state.Locations [| location |] |> Array.sortBy (fun (l: LocationDto) -> l.Name)
        { state with ShowCreateLocationForm = false; Loading = false; Locations = locations; NewLocationCode = ""; NewLocationName = "" },
        Cmd.none

    | LocationCreated (Error err) ->
        { state with Error = Some err; Loading = false }, Cmd.none

    | LocationDetailLoaded (Ok detail) ->
        { state with LocationDetail = Some detail; Loading = false }, Cmd.none

    | LocationDetailLoaded (Error err) ->
        { state with Error = Some err; Loading = false }, Cmd.none

    | StartEditLocationName ->
        let name : string =
            state.LocationDetail |> Option.map (fun d -> d.Location.Name) |> Option.defaultValue ""
        { state with EditingLocationName = true; EditLocationNameValue = name }, Cmd.none

    | EditLocationNameChanged name ->
        { state with EditLocationNameValue = name }, Cmd.none

    | SubmitEditLocationName ->
        match state.LocationDetail with
        | None -> state, Cmd.none
        | Some detail ->
            let name : string = state.EditLocationNameValue.Trim()
            if System.String.IsNullOrEmpty name then state, Cmd.none
            else
                { state with Loading = true },
                Cmd.OfAsync.either (fun () -> updateLocation detail.Location.Code name) () LocationUpdated (fun ex -> ErrorOccurred ex.Message)

    | LocationUpdated (Ok location) ->
        { state with
            EditingLocationName = false
            Loading = false
            LocationDetail = state.LocationDetail |> Option.map (fun d -> { d with Location = location })
            Locations = state.Locations |> Array.map (fun (l: LocationDto) -> if l.Code = location.Code then location else l) },
        Cmd.none

    | LocationUpdated (Error err) ->
        { state with Error = Some err; Loading = false }, Cmd.none

    | CancelEditLocationName ->
        { state with EditingLocationName = false }, Cmd.none

    | StartEditLocationCode ->
        let code : string =
            state.LocationDetail |> Option.map (fun d -> d.Location.Code) |> Option.defaultValue ""
        { state with EditingLocationCode = true; EditLocationCodeValue = code }, Cmd.none

    | EditLocationCodeChanged code ->
        { state with EditLocationCodeValue = code }, Cmd.none

    | SubmitEditLocationCode ->
        match state.LocationDetail with
        | None -> state, Cmd.none
        | Some detail ->
            let code : string = state.EditLocationCodeValue.Trim()
            if System.String.IsNullOrEmpty code then state, Cmd.none
            else
                { state with Loading = true },
                Cmd.OfAsync.either (fun () -> updateLocationCode detail.Location.Code code) () LocationCodeUpdated (fun ex -> ErrorOccurred ex.Message)

    | LocationCodeUpdated (Ok updatedLoc) ->
        { state with EditingLocationCode = false; Loading = false },
        navigateCmd (LocationDetail updatedLoc.Code)

    | LocationCodeUpdated (Error err) ->
        { state with Error = Some err; Loading = false }, Cmd.none

    | CancelEditLocationCode ->
        { state with EditingLocationCode = false }, Cmd.none

    | ArchiveLocation ->
        match state.LocationDetail with
        | None -> state, Cmd.none
        | Some detail ->
            { state with Loading = true },
            Cmd.OfAsync.either (fun () -> archiveLocation detail.Location.Code) () LocationArchived (fun ex -> ErrorOccurred ex.Message)

    | LocationArchived (Ok _) ->
        { state with Loading = false },
        navigateCmd LocationsList

    | LocationArchived (Error err) ->
        { state with Error = Some err; Loading = false }, Cmd.none

    | BoxesLoaded (Ok boxes) ->
        { state with Boxes = boxes; Loading = false }, Cmd.none

    | BoxesLoaded (Error err) ->
        { state with Error = Some err; Loading = false }, Cmd.none

    | BoxFilterChanged filter ->
        let cmd : Cmd<Msg> =
            if System.String.IsNullOrEmpty filter then
                Cmd.OfAsync.either (fun () -> getBoxes None) () BoxesLoaded (fun ex -> ErrorOccurred ex.Message)
            else
                Cmd.OfAsync.either (fun () -> getBoxes (Some filter)) () BoxesLoaded (fun ex -> ErrorOccurred ex.Message)
        { state with BoxFilter = filter }, cmd

    | LocationSearchChanged query ->
        { state with LocationSearch = query }, Cmd.none

    | BoxSearchChanged query ->
        { state with BoxSearch = query }, Cmd.none

    | ShowCreateBoxForm ->
        { state with ShowCreateBoxForm = true; NewBoxLabel = "" }, Cmd.none

    | NewBoxLabelChanged label ->
        { state with NewBoxLabel = label }, Cmd.none

    | SubmitCreateBox ->
        let label : string = state.NewBoxLabel.Trim()
        { state with Loading = true },
        Cmd.OfAsync.either (fun () -> createBox label) () BoxCreated (fun ex -> ErrorOccurred ex.Message)

    | BoxCreated (Ok newBox) ->
        // A new box is unassigned, so it only belongs in the list when no
        // location filter is active.
        let boxes : BoxDto array =
            if System.String.IsNullOrEmpty state.BoxFilter then
                Array.append state.Boxes [| newBox |] |> Array.sortBy (fun (b: BoxDto) -> b.Id)
            else state.Boxes
        { state with ShowCreateBoxForm = false; Loading = false; Boxes = boxes; NewBoxLabel = "" },
        Cmd.none

    | BoxCreated (Error err) ->
        { state with Error = Some err; Loading = false }, Cmd.none

    | BoxDetailLoaded (Ok detail) ->
        let locCode : string = detail.Box.LocationCode |> Option.defaultValue ""
        { state with BoxDetail = Some detail; Loading = false; AssignLocationCode = locCode }, Cmd.none

    | BoxDetailLoaded (Error err) ->
        { state with Error = Some err; Loading = false }, Cmd.none

    | AvailableLocationsLoaded (Ok locations) ->
        { state with AvailableLocations = locations }, Cmd.none

    | AvailableLocationsLoaded (Error err) ->
        { state with Error = Some err }, Cmd.none

    | StartEditBoxLabel ->
        let label : string =
            state.BoxDetail |> Option.bind (fun d -> d.Box.Label) |> Option.defaultValue ""
        { state with EditingBoxLabel = true; EditBoxLabelValue = label }, Cmd.none

    | EditBoxLabelChanged label ->
        { state with EditBoxLabelValue = label }, Cmd.none

    | SubmitEditBoxLabel ->
        match state.BoxDetail with
        | None -> state, Cmd.none
        | Some detail ->
            let label : string = state.EditBoxLabelValue.Trim()
            let locCode : string = detail.Box.LocationCode |> Option.defaultValue ""
            { state with Loading = true },
            Cmd.OfAsync.either (fun () -> updateBox detail.Box.Id label locCode) () BoxUpdated (fun ex -> ErrorOccurred ex.Message)

    | BoxUpdated (Ok updatedBox) ->
        { state with
            EditingBoxLabel = false
            Loading = false
            AssignLocationCode = updatedBox.LocationCode |> Option.defaultValue ""
            BoxDetail = state.BoxDetail |> Option.map (fun d -> if d.Box.Id = updatedBox.Id then { d with Box = updatedBox } else d)
            Boxes = state.Boxes |> Array.map (fun (b: BoxDto) -> if b.Id = updatedBox.Id then updatedBox else b) },
        Cmd.none

    | BoxUpdated (Error err) ->
        { state with Error = Some err; Loading = false }, Cmd.none

    | CancelEditBoxLabel ->
        { state with EditingBoxLabel = false }, Cmd.none

    | AssignBoxToLocation code ->
        match state.BoxDetail with
        | None -> state, Cmd.none
        | Some detail ->
            let label : string = detail.Box.Label |> Option.defaultValue ""
            { state with Loading = true; AssignLocationCode = code },
            Cmd.OfAsync.either (fun () -> updateBox detail.Box.Id label code) () BoxUpdated (fun ex -> ErrorOccurred ex.Message)

    | DeleteBox ->
        match state.BoxDetail with
        | None -> state, Cmd.none
        | Some detail ->
            { state with Loading = true },
            Cmd.OfAsync.either (fun () -> deleteBox detail.Box.Id) () BoxDeleted (fun ex -> ErrorOccurred ex.Message)

    | BoxDeleted (Ok _) ->
        { state with Loading = false },
        navigateCmd BoxesList

    | BoxDeleted (Error err) ->
        { state with Error = Some err; Loading = false }, Cmd.none

    | UploadBoxPhoto (boxId, photo) ->
        { state with UploadingPhoto = true; PhotoProcessing = false; Error = None },
        Cmd.OfAsync.either
            (fun () -> async {
                let! compressed = compressImage photo
                return! uploadBoxPhoto boxId compressed
            }) () PhotoUploadStarted (fun ex -> ErrorOccurred ex.Message)

    | NewItemNameChanged name ->
        { state with NewItemName = name }, Cmd.none

    | PhotoSelected photo ->
        { state with SelectedPhoto = Some photo }, Cmd.none

    | SubmitAddItem ->
        match state.BoxDetail with
        | None -> state, Cmd.none
        | Some detail ->
            let name : string = state.NewItemName.Trim()
            if System.String.IsNullOrEmpty name then state, Cmd.none
            else
                { state with Loading = true; NewItemName = "" },
                Cmd.OfAsync.either
                    (fun () -> async {
                        let! compressedPhoto =
                            match state.SelectedPhoto with
                            | Some p -> async { let! c = compressImage p in return Some c }
                            | None -> async { return None }
                        return! addItem detail.Box.Id name compressedPhoto
                    }) () ItemAdded (fun ex -> ErrorOccurred ex.Message)

    | ItemAdded (Ok result) ->
        let detail : BoxDetailDto option =
            state.BoxDetail |> Option.map (fun d -> { d with Items = Array.append d.Items [| result.Item |] })
        match result.PhotoJobId with
        | Some jobId ->
            // Item is shown immediately; its photo is still processing on the
            // server and gets patched in when the job completes.
            { state with Loading = false; SelectedPhoto = None; BoxDetail = detail; PhotoProcessing = true; PhotoJobId = Some jobId },
            schedulePollCmd jobId
        | None ->
            { state with Loading = false; SelectedPhoto = None; BoxDetail = detail },
            Cmd.none

    | ItemAdded (Error err) ->
        { state with Error = Some err; Loading = false }, Cmd.none

    | DeleteItem itemId ->
        match state.BoxDetail with
        | None -> state, Cmd.none
        | Some detail ->
            { state with Loading = true },
            Cmd.OfAsync.either (fun () -> deleteItem detail.Box.Id itemId) () (fun (res: Result<unit, string>) -> ItemDeleted(itemId, res)) (fun ex -> ErrorOccurred ex.Message)

    | ItemDeleted (itemId, Ok _) ->
        { state with
            Loading = false
            BoxDetail = state.BoxDetail |> Option.map (fun d -> { d with Items = d.Items |> Array.filter (fun (i: ItemDto) -> i.Id <> itemId) }) },
        Cmd.none

    | ItemDeleted (_, Error err) ->
        { state with Error = Some err; Loading = false }, Cmd.none

    | ShowMoveItemDialog itemId ->
        { state with MovingItemId = Some itemId; TargetBoxId = ""; AvailableBoxes = [||]; DialogLoading = true },
        Cmd.OfAsync.either (fun () -> getBoxes None) () BoxesForMoveLoaded (fun ex -> ErrorOccurred ex.Message)

    | BoxesForMoveLoaded (Ok boxes) ->
        { state with AvailableBoxes = boxes; DialogLoading = false }, Cmd.none

    | BoxesForMoveLoaded (Error err) ->
        { state with Error = Some err; MovingItemId = None; DialogLoading = false }, Cmd.none

    | MoveTargetBoxChanged boxId ->
        { state with TargetBoxId = boxId }, Cmd.none

    | ConfirmMoveItem ->
        match state.MovingItemId, state.BoxDetail with
        | Some itemId, Some detail ->
            if System.String.IsNullOrEmpty state.TargetBoxId then state, Cmd.none
            else
                { state with Loading = true },
                Cmd.OfAsync.either (fun () -> moveEntity "item" itemId "box" state.TargetBoxId) () ItemMoved (fun ex -> ErrorOccurred ex.Message)
        | _ -> state, Cmd.none

    | ItemMoved (Ok move) ->
        let detail : BoxDetailDto option =
            state.BoxDetail |> Option.map (fun d ->
                if move.ToId = Some d.Box.Id then d
                else { d with Items = d.Items |> Array.filter (fun (i: ItemDto) -> i.Id <> move.EntityId) })
        { state with Loading = false; MovingItemId = None; TargetBoxId = ""; BoxDetail = detail },
        Cmd.none

    | ItemMoved (Error err) ->
        { state with Error = Some err; Loading = false; MovingItemId = None }, Cmd.none

    | CancelMoveItem ->
        { state with MovingItemId = None; TargetBoxId = "" }, Cmd.none

    | SearchQueryChanged query ->
        if System.String.IsNullOrWhiteSpace query then
            state.SearchDebounceTimer |> Option.iter clearTimeout
            { state with SearchQuery = query; SearchResults = [||]; SearchDebounceTimer = None; SearchLoading = false }, Cmd.none
        else
            let cmd : Cmd<Msg> = Cmd.ofEffect (fun (dispatch: Msg -> unit) ->
                state.SearchDebounceTimer |> Option.iter clearTimeout
                let timerId : int = setTimeout (fun () -> dispatch (SearchDebounceTriggered query)) 300
                ()
            )
            { state with SearchQuery = query; SearchLoading = true }, cmd

    | SearchDebounceTriggered query ->
        if query = state.SearchQuery then
            { state with SearchDebounceTimer = None },
            Cmd.OfAsync.either searchItems query SearchResultsLoaded (fun ex -> ErrorOccurred ex.Message)
        else
            state, Cmd.none

    | SearchResultsLoaded (Ok results) ->
        { state with SearchResults = results; Loading = false; SearchLoading = false }, Cmd.none

    | SearchResultsLoaded (Error err) ->
        { state with Error = Some err; Loading = false; SearchLoading = false }, Cmd.none

    | AllItemsLoaded (Ok items) ->
        { state with AllItems = items; Loading = false }, Cmd.none

    | AllItemsLoaded (Error err) ->
        { state with Error = Some err; Loading = false }, Cmd.none

    | ShowCreateItemForm ->
        { state with ShowCreateItemForm = true; NewStandaloneItemName = ""; NewStandaloneItemBoxId = "" }, Cmd.none

    | NewStandaloneItemNameChanged name ->
        { state with NewStandaloneItemName = name }, Cmd.none

    | NewStandaloneItemBoxChanged boxId ->
        { state with NewStandaloneItemBoxId = boxId }, Cmd.none

    | SubmitCreateStandaloneItem ->
        let name : string = state.NewStandaloneItemName.Trim()
        if System.String.IsNullOrEmpty name then state, Cmd.none
        else
            { state with Loading = true },
            Cmd.OfAsync.either (fun () -> createItem name state.NewStandaloneItemBoxId) () StandaloneItemCreated (fun ex -> ErrorOccurred ex.Message)

    | StandaloneItemCreated (Ok item) ->
        let boxId : string = item.BoxId |> Option.defaultValue ""
        let assignedBox : BoxDto option = state.Boxes |> Array.tryFind (fun (b: BoxDto) -> b.Id = boxId)
        let locationCode : string option = assignedBox |> Option.bind (fun (b: BoxDto) -> b.LocationCode)
        let locationName : string option =
            locationCode
            |> Option.bind (fun code ->
                state.Locations
                |> Array.tryFind (fun (l: LocationDto) -> l.Code = code)
                |> Option.map (fun (l: LocationDto) -> l.Name))
        let entry : SearchResultDto =
            { ItemId = item.Id; ItemName = item.Name; PhotoPath = item.PhotoPath
              BoxId = boxId; BoxLabel = assignedBox |> Option.bind (fun (b: BoxDto) -> b.Label)
              LocationCode = locationCode; LocationName = locationName; AddedAt = item.AddedAt }
        { state with
            ShowCreateItemForm = false
            Loading = false
            NewStandaloneItemName = ""
            NewStandaloneItemBoxId = ""
            AllItems = Array.append [| entry |] state.AllItems },
        Cmd.none

    | StandaloneItemCreated (Error err) ->
        { state with Error = Some err; Loading = false }, Cmd.none

    | StartEditItem (itemId, currentName) ->
        { state with EditingItemId = Some itemId; EditItemNameValue = currentName }, Cmd.none

    | EditItemNameChanged name ->
        { state with EditItemNameValue = name }, Cmd.none

    | SubmitEditItem ->
        match state.EditingItemId with
        | None -> state, Cmd.none
        | Some itemId ->
            let name : string = state.EditItemNameValue.Trim()
            if System.String.IsNullOrEmpty name then state, Cmd.none
            else
                { state with Loading = true },
                Cmd.OfAsync.either (fun () -> updateItemStandalone itemId name) () ItemNameUpdated (fun ex -> ErrorOccurred ex.Message)

    | ItemNameUpdated (Ok item) ->
        let patchResult (r: SearchResultDto) : SearchResultDto =
            if r.ItemId = item.Id then { r with ItemName = item.Name } else r
        { state with
            EditingItemId = None
            EditItemNameValue = ""
            Loading = false
            AllItems = state.AllItems |> Array.map patchResult
            SearchResults = state.SearchResults |> Array.map patchResult
            ItemDetail = state.ItemDetail |> Option.map patchResult
            BoxDetail = state.BoxDetail |> Option.map (fun d -> { d with Items = d.Items |> Array.map (fun (i: ItemDto) -> if i.Id = item.Id then { i with Name = item.Name } else i) }) },
        Cmd.none

    | ItemNameUpdated (Error err) ->
        { state with Error = Some err; Loading = false }, Cmd.none

    | CancelEditItem ->
        { state with EditingItemId = None; EditItemNameValue = "" }, Cmd.none

    | DeleteStandaloneItem itemId ->
        { state with Loading = true },
        Cmd.OfAsync.either (fun () -> deleteItemStandalone itemId) () (fun (res: Result<unit, string>) -> StandaloneItemDeleted(itemId, res)) (fun ex -> ErrorOccurred ex.Message)

    | StandaloneItemDeleted (itemId, Ok _) ->
        let nextState : State =
            { state with
                Loading = false
                AllItems = state.AllItems |> Array.filter (fun (i: SearchResultDto) -> i.ItemId <> itemId)
                SearchResults = state.SearchResults |> Array.filter (fun (i: SearchResultDto) -> i.ItemId <> itemId) }
        match state.CurrentPage with
        | ItemDetail _ ->
            // The page we are on no longer exists; go back to the list.
            nextState, navigateCmd ItemsList
        | _ -> nextState, Cmd.none

    | StandaloneItemDeleted (_, Error err) ->
        { state with Error = Some err; Loading = false }, Cmd.none

    | ShowMoveItemStandaloneDialog itemId ->
        { state with MovingItemStandaloneId = Some itemId; MoveItemTargetBox = ""; BoxesForItemMove = [||]; DialogLoading = true },
        Cmd.OfAsync.either (fun () -> getBoxes None) () (fun res -> match res with Ok boxes -> BoxesForItemMoveLoaded boxes | Error err -> ErrorOccurred err) (fun ex -> ErrorOccurred ex.Message)

    | BoxesForItemMoveLoaded boxes ->
        { state with BoxesForItemMove = boxes; DialogLoading = false }, Cmd.none

    | MoveItemTargetBoxChanged boxId ->
        { state with MoveItemTargetBox = boxId }, Cmd.none

    | ConfirmMoveItemStandalone ->
        match state.MovingItemStandaloneId with
        | None -> state, Cmd.none
        | Some itemId ->
            if System.String.IsNullOrEmpty state.MoveItemTargetBox then state, Cmd.none
            else
                { state with Loading = true },
                Cmd.OfAsync.either (fun () -> moveEntity "item" itemId "box" state.MoveItemTargetBox) () StandaloneItemMoved (fun ex -> ErrorOccurred ex.Message)

    | StandaloneItemMoved (Ok move) ->
        let baseState : State =
            { state with Loading = false; MovingItemStandaloneId = None; MoveItemTargetBox = "" }
        let targetBox : BoxDto option =
            move.ToId |> Option.bind (fun boxId -> state.BoxesForItemMove |> Array.tryFind (fun (b: BoxDto) -> b.Id = boxId))
        match targetBox with
        | Some box ->
            let locationName : string option =
                box.LocationCode
                |> Option.bind (fun code ->
                    Array.append state.Locations state.AvailableLocations
                    |> Array.tryFind (fun (l: LocationDto) -> l.Code = code)
                    |> Option.map (fun (l: LocationDto) -> l.Name))
            let patchResult (r: SearchResultDto) : SearchResultDto =
                if r.ItemId = move.EntityId then
                    { r with BoxId = box.Id; BoxLabel = box.Label; LocationCode = box.LocationCode; LocationName = locationName }
                else r
            { baseState with
                AllItems = baseState.AllItems |> Array.map patchResult
                SearchResults = baseState.SearchResults |> Array.map patchResult
                ItemDetail = baseState.ItemDetail |> Option.map patchResult },
            Cmd.none
        | None ->
            // Target box not in the dialog's snapshot; fall back to a reload.
            baseState, navigateCmd state.CurrentPage

    | StandaloneItemMoved (Error err) ->
        { state with Error = Some err; Loading = false; MovingItemStandaloneId = None }, Cmd.none

    | CancelMoveItemStandalone ->
        { state with MovingItemStandaloneId = None; MoveItemTargetBox = "" }, Cmd.none

    | UploadItemPhoto (itemId, photo) ->
        { state with UploadingPhoto = true; PhotoProcessing = false; Error = None },
        Cmd.OfAsync.either
            (fun () -> async {
                let! compressed = compressImage photo
                return! updateItemPhoto itemId compressed
            }) () PhotoUploadStarted (fun ex -> ErrorOccurred ex.Message)

    | ShowAddExistingItemDialog ->
        { state with AddingExistingItem = true; SelectedExistingItemId = ""; UnassignedItems = [||]; DialogLoading = true },
        Cmd.OfAsync.either listItems () UnassignedItemsLoaded (fun ex -> ErrorOccurred ex.Message)

    | UnassignedItemsLoaded (Ok items) ->
        let unassigned : SearchResultDto array =
            items |> Array.filter (fun (i: SearchResultDto) -> System.String.IsNullOrEmpty i.BoxId)
        { state with UnassignedItems = unassigned; DialogLoading = false }, Cmd.none

    | UnassignedItemsLoaded (Error err) ->
        { state with Error = Some err; AddingExistingItem = false; DialogLoading = false }, Cmd.none

    | SelectedExistingItemChanged itemId ->
        { state with SelectedExistingItemId = itemId }, Cmd.none

    | ConfirmAddExistingItem ->
        match state.BoxDetail with
        | None -> state, Cmd.none
        | Some detail ->
            if System.String.IsNullOrEmpty state.SelectedExistingItemId then state, Cmd.none
            else
                { state with Loading = true },
                Cmd.OfAsync.either (fun () -> moveEntity "item" state.SelectedExistingItemId "box" detail.Box.Id) () ExistingItemAdded (fun ex -> ErrorOccurred ex.Message)

    | ExistingItemAdded (Ok move) ->
        match state.BoxDetail with
        | None -> { state with Loading = false; AddingExistingItem = false }, Cmd.none
        | Some detail ->
            let baseState : State =
                { state with Loading = false; AddingExistingItem = false; SelectedExistingItemId = ""; UnassignedItems = [||] }
            match state.UnassignedItems |> Array.tryFind (fun (i: SearchResultDto) -> i.ItemId = move.EntityId) with
            | Some entry ->
                let item : ItemDto =
                    { Id = entry.ItemId; BoxId = Some detail.Box.Id; Name = entry.ItemName; PhotoPath = entry.PhotoPath; AddedAt = entry.AddedAt }
                let items : ItemDto array =
                    Array.append detail.Items [| item |] |> Array.sortBy (fun (i: ItemDto) -> i.AddedAt)
                { baseState with BoxDetail = Some { detail with Items = items } }, Cmd.none
            | None ->
                // Moved item not in the dialog's snapshot; fall back to a reload.
                baseState, navigateCmd (BoxDetail detail.Box.Id)

    | ExistingItemAdded (Error err) ->
        { state with Error = Some err; Loading = false; AddingExistingItem = false }, Cmd.none

    | CancelAddExistingItem ->
        { state with AddingExistingItem = false; SelectedExistingItemId = ""; UnassignedItems = [||] }, Cmd.none

    | UnassignItem itemId ->
        { state with Loading = true },
        Cmd.OfAsync.either (fun () -> unassignEntity "item" itemId) () ItemUnassigned (fun ex -> ErrorOccurred ex.Message)

    | ItemUnassigned (Ok move) ->
        { state with
            Loading = false
            BoxDetail = state.BoxDetail |> Option.map (fun d -> { d with Items = d.Items |> Array.filter (fun (i: ItemDto) -> i.Id <> move.EntityId) }) },
        Cmd.none

    | ItemUnassigned (Error err) ->
        { state with Error = Some err; Loading = false }, Cmd.none

    | UnassignStandaloneItem itemId ->
        { state with Loading = true },
        Cmd.OfAsync.either (fun () -> unassignEntity "item" itemId) () StandaloneItemUnassigned (fun ex -> ErrorOccurred ex.Message)

    | StandaloneItemUnassigned (Ok move) ->
        let patchResult (r: SearchResultDto) : SearchResultDto =
            if r.ItemId = move.EntityId then
                { r with BoxId = ""; BoxLabel = None; LocationCode = None; LocationName = None }
            else r
        { state with
            Loading = false
            AllItems = state.AllItems |> Array.map patchResult
            SearchResults = state.SearchResults |> Array.map patchResult
            ItemDetail = state.ItemDetail |> Option.map patchResult },
        Cmd.none

    | StandaloneItemUnassigned (Error err) ->
        { state with Error = Some err; Loading = false }, Cmd.none

    | ShowAddBoxToLocationDialog ->
        { state with AddingBoxToLocation = true; SelectedBoxForLocationMove = ""; BoxesForLocationMove = [||]; DialogLoading = true },
        Cmd.OfAsync.either (fun () -> getBoxes None) () (fun res -> match res with Ok boxes -> BoxesForLocationMoveLoaded boxes | Error err -> ErrorOccurred err) (fun ex -> ErrorOccurred ex.Message)

    | BoxesForLocationMoveLoaded boxes ->
        let currentLocCode : string =
            state.LocationDetail |> Option.map (fun d -> d.Location.Code) |> Option.defaultValue ""
        let filtered : BoxDto array =
            boxes |> Array.filter (fun b -> b.LocationCode <> Some currentLocCode)
        { state with BoxesForLocationMove = filtered; DialogLoading = false }, Cmd.none

    | SelectedBoxForLocationMoveChanged boxId ->
        { state with SelectedBoxForLocationMove = boxId }, Cmd.none

    | ConfirmAddBoxToLocation ->
        match state.LocationDetail with
        | None -> state, Cmd.none
        | Some detail ->
            if System.String.IsNullOrEmpty state.SelectedBoxForLocationMove then state, Cmd.none
            else
                { state with Loading = true },
                Cmd.OfAsync.either (fun () -> moveEntity "box" state.SelectedBoxForLocationMove "location" detail.Location.Code) () BoxMovedToLocation (fun ex -> ErrorOccurred ex.Message)

    | BoxMovedToLocation (Ok move) ->
        match state.LocationDetail with
        | None -> { state with Loading = false; AddingBoxToLocation = false }, Cmd.none
        | Some detail ->
            let baseState : State =
                { state with Loading = false; AddingBoxToLocation = false; SelectedBoxForLocationMove = ""; BoxesForLocationMove = [||] }
            match state.BoxesForLocationMove |> Array.tryFind (fun (b: BoxDto) -> b.Id = move.EntityId) with
            | Some movedBox ->
                let boxes : BoxDto array =
                    Array.append detail.Boxes [| { movedBox with LocationCode = Some detail.Location.Code } |]
                    |> Array.sortBy (fun (b: BoxDto) -> b.Id)
                { baseState with LocationDetail = Some { detail with Boxes = boxes } }, Cmd.none
            | None ->
                // Moved box not in the dialog's snapshot; fall back to a reload.
                baseState, navigateCmd (LocationDetail detail.Location.Code)

    | BoxMovedToLocation (Error err) ->
        { state with Error = Some err; Loading = false; AddingBoxToLocation = false }, Cmd.none

    | CancelAddBoxToLocation ->
        { state with AddingBoxToLocation = false; SelectedBoxForLocationMove = ""; BoxesForLocationMove = [||] }, Cmd.none

    | UnassignBoxFromLocation boxId ->
        { state with Loading = true },
        Cmd.OfAsync.either (fun () -> unassignEntity "box" boxId) () BoxUnassignedFromLocation (fun ex -> ErrorOccurred ex.Message)

    | BoxUnassignedFromLocation (Ok move) ->
        { state with
            Loading = false
            LocationDetail = state.LocationDetail |> Option.map (fun d -> { d with Boxes = d.Boxes |> Array.filter (fun (b: BoxDto) -> b.Id <> move.EntityId) }) },
        Cmd.none

    | BoxUnassignedFromLocation (Error err) ->
        { state with Error = Some err; Loading = false }, Cmd.none

    | PrintBoxLabel ->
        match state.BoxDetail with
        | None -> state, Cmd.none
        | Some detail ->
            openWindow $"/api/boxes/%s{detail.Box.Id}/label" "_blank"
            state, Cmd.none

    | PrintLocationLabel ->
        match state.LocationDetail with
        | None -> state, Cmd.none
        | Some detail ->
            openWindow $"/api/locations/%s{detail.Location.Code}/label" "_blank"
            state, Cmd.none

    | PrintMultipleBoxLabels ->
        state, Cmd.none

    | StartEditBoxInList (boxId, currentLabel) ->
        { state with EditingBoxIdInList = Some boxId; EditBoxLabelInListValue = currentLabel |> Option.defaultValue "" }, Cmd.none

    | EditBoxLabelInListChanged label ->
        { state with EditBoxLabelInListValue = label }, Cmd.none

    | SubmitEditBoxInList ->
        match state.EditingBoxIdInList with
        | None -> state, Cmd.none
        | Some boxId ->
            let label : string = state.EditBoxLabelInListValue.Trim()
            if System.String.IsNullOrEmpty label then state, Cmd.none
            else
                let locCode : string =
                    state.Boxes |> Array.tryFind (fun b -> b.Id = boxId)
                    |> Option.bind (fun b -> b.LocationCode)
                    |> Option.defaultValue ""
                { state with Loading = true },
                Cmd.OfAsync.either (fun () -> updateBox boxId label locCode) () BoxUpdatedInList (fun ex -> ErrorOccurred ex.Message)

    | CancelEditBoxInList ->
        { state with EditingBoxIdInList = None; EditBoxLabelInListValue = "" }, Cmd.none

    | BoxUpdatedInList (Ok updatedBox) ->
        let boxes : BoxDto array = state.Boxes |> Array.map (fun b -> if b.Id = updatedBox.Id then updatedBox else b)
        { state with Boxes = boxes; EditingBoxIdInList = None; EditBoxLabelInListValue = ""; Loading = false }, Cmd.none

    | BoxUpdatedInList (Error err) ->
        { state with Error = Some err; Loading = false }, Cmd.none

    | DeleteBoxFromList boxId ->
        { state with Loading = true },
        Cmd.OfAsync.either (fun () -> deleteBox boxId) () (fun (res: Result<unit, string>) -> BoxDeletedFromList(boxId, res)) (fun ex -> ErrorOccurred ex.Message)

    | BoxDeletedFromList (boxId, Ok _) ->
        { state with Loading = false; Boxes = state.Boxes |> Array.filter (fun (b: BoxDto) -> b.Id <> boxId) },
        Cmd.none

    | BoxDeletedFromList (_, Error err) ->
        { state with Error = Some err; Loading = false }, Cmd.none

    | StartEditLocationInList (code, currentName) ->
        { state with EditingLocationCodeInList = Some code; EditLocationNameInListValue = currentName }, Cmd.none

    | EditLocationNameInListChanged name ->
        { state with EditLocationNameInListValue = name }, Cmd.none

    | SubmitEditLocationInList ->
        match state.EditingLocationCodeInList with
        | None -> state, Cmd.none
        | Some code ->
            let name : string = state.EditLocationNameInListValue.Trim()
            if System.String.IsNullOrEmpty name then state, Cmd.none
            else
                { state with Loading = true },
                Cmd.OfAsync.either (fun () -> updateLocation code name) () LocationUpdatedInList (fun ex -> ErrorOccurred ex.Message)

    | CancelEditLocationInList ->
        { state with EditingLocationCodeInList = None; EditLocationNameInListValue = "" }, Cmd.none

    | LocationUpdatedInList (Ok updatedLoc) ->
        let locations : LocationDto array = state.Locations |> Array.map (fun l -> if l.Code = updatedLoc.Code then updatedLoc else l)
        { state with Locations = locations; EditingLocationCodeInList = None; EditLocationNameInListValue = ""; Loading = false }, Cmd.none

    | LocationUpdatedInList (Error err) ->
        { state with Error = Some err; Loading = false }, Cmd.none

    | ArchiveLocationFromList code ->
        { state with Loading = true },
        Cmd.OfAsync.either (fun () -> archiveLocation code) () LocationArchivedFromList (fun ex -> ErrorOccurred ex.Message)

    | LocationArchivedFromList (Ok archivedLoc) ->
        let locations : LocationDto array = state.Locations |> Array.map (fun l -> if l.Code = archivedLoc.Code then archivedLoc else l)
        { state with Locations = locations; Loading = false }, Cmd.none

    | LocationArchivedFromList (Error err) ->
        { state with Error = Some err; Loading = false }, Cmd.none

    | UploadLocationPhoto (locationCode, photo) ->
        { state with UploadingPhoto = true; PhotoProcessing = false; Error = None },
        Cmd.OfAsync.either
            (fun () -> async {
                let! compressed = compressImage photo
                return! uploadLocationPhoto locationCode compressed
            }) () PhotoUploadStarted (fun ex -> ErrorOccurred ex.Message)

    | PhotoUploadStarted (Ok job) ->
        match job.Status with
        | "completed" ->
            applyCompletedPhoto job { state with UploadingPhoto = false; PhotoProcessing = false; PhotoJobId = None },
            Cmd.none
        | "failed" ->
            { state with UploadingPhoto = false; PhotoProcessing = false; PhotoJobId = None; Error = Some(job.Error |> Option.defaultValue "Image processing failed") },
            Cmd.none
        | _ ->
            // Upload finished; the server is now processing the photo.
            { state with UploadingPhoto = false; PhotoProcessing = true; PhotoJobId = Some job.Id },
            schedulePollCmd job.Id

    | PhotoUploadStarted (Error err) ->
        { state with Error = Some err; UploadingPhoto = false; PhotoProcessing = false }, Cmd.none

    | PollPhotoJob jobId ->
        match state.PhotoJobId with
        | Some current when current = jobId ->
            state, Cmd.OfAsync.either getPhotoJob jobId PhotoJobPolled (fun ex -> ErrorOccurred ex.Message)
        | _ ->
            // A newer job replaced this one (or processing was cancelled); stop polling.
            state, Cmd.none

    | PhotoJobPolled (Ok job) ->
        match state.PhotoJobId with
        | Some current when current = job.Id ->
            match job.Status with
            | "completed" ->
                applyCompletedPhoto job { state with PhotoProcessing = false; PhotoJobId = None },
                Cmd.none
            | "failed" ->
                { state with PhotoProcessing = false; PhotoJobId = None; Error = Some(job.Error |> Option.defaultValue "Image processing failed") },
                Cmd.none
            | _ ->
                state, schedulePollCmd job.Id
        | _ -> state, Cmd.none

    | PhotoJobPolled (Error err) ->
        { state with Error = Some err; PhotoProcessing = false; PhotoJobId = None }, Cmd.none

    | DismissPhotoProcessing ->
        { state with PhotoProcessing = false; PhotoJobId = None }, Cmd.none

    | ShowHistory (entityType, entityId, title, createdAt) ->
        { state with
            ShowHistoryModal = true
            HistoryEntityType = entityType
            HistoryEntityId = entityId
            HistoryTitle = title
            HistoryCreatedAt = createdAt
            HistoryMoves = [||]
            HistoryLoading = true },
        Cmd.OfAsync.either (fun () -> getMoveHistory entityType entityId) () HistoryLoaded (fun ex -> ErrorOccurred ex.Message)

    | HistoryLoaded (Ok moves) ->
        { state with HistoryMoves = moves; HistoryLoading = false }, Cmd.none

    | HistoryLoaded (Error err) ->
        { state with Error = Some err; HistoryLoading = false; ShowHistoryModal = false }, Cmd.none

    | CloseHistory ->
        { state with ShowHistoryModal = false; HistoryMoves = [||] }, Cmd.none

    | OpenScanner -> { state with ScannerOpen = true }, Cmd.none

    | CloseScanner -> { state with ScannerOpen = false }, Cmd.none

    | QrScanned text ->
        let page =
            if text.StartsWith("BOX-") then BoxDetail text
            else LocationDetail text
        { state with ScannerOpen = false }, navigateCmd page

    | ItemDetailLoaded (Ok items) ->
        match state.CurrentPage with
        | ItemDetail itemId ->
            let item = items |> Array.tryFind (fun i -> i.ItemId = itemId)
            { state with ItemDetail = item; Loading = false }, Cmd.none
        | _ -> { state with Loading = false }, Cmd.none

    | ItemDetailLoaded (Error err) ->
        { state with Error = Some err; Loading = false }, Cmd.none

    | NotesLoaded (Ok notes) ->
        let entityType, entityId =
            match state.CurrentPage with
            | LocationDetail code -> "location", code
            | BoxDetail id -> "box", id
            | ItemDetail id -> "item", id
            | _ -> "", ""
        { state with Notes = notes; NoteEntityType = entityType; NoteEntityId = entityId }, Cmd.none

    | NotesLoaded (Error _) ->
        state, Cmd.none

    | ShowAddNoteForm ->
        { state with ShowAddNoteForm = true; NewNoteContent = "" }, Cmd.none

    | CancelAddNote ->
        { state with ShowAddNoteForm = false; NewNoteContent = "" }, Cmd.none

    | NewNoteContentChanged content ->
        { state with NewNoteContent = content }, Cmd.none

    | SubmitCreateNote ->
        let content : string = state.NewNoteContent.Trim()
        if System.String.IsNullOrEmpty content then state, Cmd.none
        else
            { state with Loading = true },
            Cmd.OfAsync.either (fun () -> createNote state.NoteEntityType state.NoteEntityId content) () NoteCreated (fun ex -> ErrorOccurred ex.Message)

    | NoteCreated (Ok note) ->
        { state with Loading = false; Notes = Array.append [| note |] state.Notes; ShowAddNoteForm = false; NewNoteContent = "" }, Cmd.none

    | NoteCreated (Error err) ->
        { state with Error = Some err; Loading = false }, Cmd.none

    | StartEditNote (noteId, content) ->
        { state with EditingNoteId = Some noteId; EditNoteContent = content }, Cmd.none

    | CancelEditNote ->
        { state with EditingNoteId = None; EditNoteContent = "" }, Cmd.none

    | EditNoteContentChanged content ->
        { state with EditNoteContent = content }, Cmd.none

    | SubmitEditNote ->
        match state.EditingNoteId with
        | None -> state, Cmd.none
        | Some noteId ->
            let content : string = state.EditNoteContent.Trim()
            if System.String.IsNullOrEmpty content then state, Cmd.none
            else
                { state with Loading = true },
                Cmd.OfAsync.either (fun () -> updateNote noteId content) () NoteUpdated (fun ex -> ErrorOccurred ex.Message)

    | NoteUpdated (Ok updatedNote) ->
        let notes : NoteDto array = state.Notes |> Array.map (fun n -> if n.Id = updatedNote.Id then updatedNote else n)
        { state with Loading = false; Notes = notes; EditingNoteId = None; EditNoteContent = "" }, Cmd.none

    | NoteUpdated (Error err) ->
        { state with Error = Some err; Loading = false }, Cmd.none

    | DeleteNote noteId ->
        { state with Loading = true; DeletingNoteId = Some noteId },
        Cmd.OfAsync.either (fun () -> deleteNote noteId) () NoteDeleted (fun ex -> ErrorOccurred ex.Message)

    | NoteDeleted (Ok _) ->
        let remaining : NoteDto array =
            match state.DeletingNoteId with
            | Some nid -> state.Notes |> Array.filter (fun n -> n.Id <> nid)
            | None -> state.Notes
        { state with Loading = false; Notes = remaining; DeletingNoteId = None }, Cmd.none

    | NoteDeleted (Error err) ->
        { state with Error = Some err; Loading = false; DeletingNoteId = None }, Cmd.none
