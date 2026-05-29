module BoxTracker.Client.State

open Elmish
open BoxTracker.Client.Api

type Page =
    | LocationsList
    | LocationDetail of string
    | BoxesList
    | BoxDetail of string
    | ItemsList
    | ItemsSearch

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
    | ArchiveLocation
    | LocationArchived of Result<LocationDto, string>
    | BoxesLoaded of Result<BoxDto array, string>
    | BoxFilterChanged of string
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
    | BoxPhotoUploaded of Result<BoxDto, string>
    | NewItemNameChanged of string
    | PhotoSelected of obj
    | SubmitAddItem
    | ItemAdded of Result<ItemDto, string>
    | DeleteItem of string
    | ItemDeleted of Result<unit, string>
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
    | StandaloneItemDeleted of Result<unit, string>
    | ShowMoveItemStandaloneDialog of string
    | MoveItemTargetBoxChanged of string
    | ConfirmMoveItemStandalone
    | StandaloneItemMoved of Result<MoveDto, string>
    | CancelMoveItemStandalone
    | BoxesForItemMoveLoaded of BoxDto array
    | UploadItemPhoto of string * obj
    | ItemPhotoUploaded of Result<ItemDto, string>
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

type State = {
    CurrentPage: Page
    Loading: bool
    Error: string option
    ViewingImageUrl: string option
    Locations: LocationDto array
    LocationDetail: LocationDetailDto option
    ShowCreateLocationForm: bool
    NewLocationCode: string
    NewLocationName: string
    EditingLocationName: bool
    EditLocationNameValue: string
    Boxes: BoxDto array
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
    | "/items/search" -> ItemsSearch
    | _ -> LocationsList

let private hashFromPage (page: Page) : string =
    match page with
    | LocationsList -> "#/locations"
    | LocationDetail code -> $"#/locations/%s{code}"
    | BoxesList -> "#/boxes"
    | BoxDetail id -> $"#/boxes/%s{id}"
    | ItemsList -> "#/items"
    | ItemsSearch -> "#/items/search"

let private pageEqual (a: Page) (b: Page) : bool =
    match a, b with
    | LocationsList, LocationsList -> true
    | LocationDetail c1, LocationDetail c2 -> c1 = c2
    | BoxesList, BoxesList -> true
    | BoxDetail id1, BoxDetail id2 -> id1 = id2
    | ItemsList, ItemsList -> true
    | ItemsSearch, ItemsSearch -> true
    | _ -> false

let private loadPage (page: Page) : Cmd<Msg> =
    match page with
    | LocationsList ->
        Cmd.OfAsync.either getLocations () LocationsLoaded (fun ex -> ErrorOccurred ex.Message)
    | LocationDetail code ->
        Cmd.OfAsync.either getLocationDetail code LocationDetailLoaded (fun ex -> ErrorOccurred ex.Message)
    | BoxesList ->
        Cmd.batch [
            Cmd.OfAsync.either getLocations () LocationsLoaded (fun ex -> ErrorOccurred ex.Message)
            Cmd.OfAsync.either (fun () -> getBoxes None) () BoxesLoaded (fun ex -> ErrorOccurred ex.Message)
        ]
    | BoxDetail id ->
        Cmd.batch [
            Cmd.OfAsync.either getBoxDetail id BoxDetailLoaded (fun ex -> ErrorOccurred ex.Message)
            Cmd.OfAsync.either getLocations () AvailableLocationsLoaded (fun ex -> ErrorOccurred ex.Message)
        ]
    | ItemsList ->
        Cmd.batch [
            Cmd.OfAsync.either listItems () AllItemsLoaded (fun ex -> ErrorOccurred ex.Message)
            Cmd.OfAsync.either getLocations () LocationsLoaded (fun ex -> ErrorOccurred ex.Message)
            Cmd.OfAsync.either (fun () -> getBoxes None) () BoxesLoaded (fun ex -> ErrorOccurred ex.Message)
        ]
    | ItemsSearch ->
        Cmd.OfAsync.either searchItems "" SearchResultsLoaded (fun ex -> ErrorOccurred ex.Message)

let private hashChangeSub (dispatch: Msg -> unit) : unit =
    addHashChangeListener (fun () ->
        let hash : string = getHash ()
        let page : Page = pageFromHash hash
        dispatch (HashChanged page)
    )

let private resetPageState (state: State) : State =
    { state with
        ViewingImageUrl = None
        LocationDetail = None
        ShowCreateLocationForm = false
        NewLocationCode = ""
        NewLocationName = ""
        EditingLocationName = false
        EditLocationNameValue = ""
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
    }

let private navigateCmd (page: Page) : Cmd<Msg> =
    Cmd.ofEffect (fun (dispatch: Msg -> unit) -> dispatch (Navigate page))

let init () : State * Cmd<Msg> =
    let hash : string = getHash ()
    let page : Page = pageFromHash hash
    let state : State = {
        CurrentPage = page
        Loading = true
        Error = None
        ViewingImageUrl = None
        Locations = [||]
        LocationDetail = None
        ShowCreateLocationForm = false
        NewLocationCode = ""
        NewLocationName = ""
        EditingLocationName = false
        EditLocationNameValue = ""
        Boxes = [||]
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

    | LocationCreated (Ok _) ->
        { state with ShowCreateLocationForm = false; Loading = false },
        navigateCmd LocationsList

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

    | LocationUpdated (Ok _) ->
        match state.LocationDetail with
        | None -> { state with Loading = false }, Cmd.none
        | Some detail ->
            { state with EditingLocationName = false; Loading = false },
            navigateCmd (LocationDetail detail.Location.Code)

    | LocationUpdated (Error err) ->
        { state with Error = Some err; Loading = false }, Cmd.none

    | CancelEditLocationName ->
        { state with EditingLocationName = false }, Cmd.none

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

    | ShowCreateBoxForm ->
        { state with ShowCreateBoxForm = true; NewBoxLabel = "" }, Cmd.none

    | NewBoxLabelChanged label ->
        { state with NewBoxLabel = label }, Cmd.none

    | SubmitCreateBox ->
        let label : string = state.NewBoxLabel.Trim()
        { state with Loading = true },
        Cmd.OfAsync.either (fun () -> createBox label) () BoxCreated (fun ex -> ErrorOccurred ex.Message)

    | BoxCreated (Ok _) ->
        { state with ShowCreateBoxForm = false; Loading = false },
        navigateCmd BoxesList

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

    | BoxUpdated (Ok _) ->
        match state.BoxDetail with
        | None -> { state with Loading = false }, Cmd.none
        | Some detail ->
            { state with EditingBoxLabel = false; Loading = false },
            navigateCmd (BoxDetail detail.Box.Id)

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
        { state with Loading = true },
        Cmd.OfAsync.either (fun () -> uploadBoxPhoto boxId photo) () BoxPhotoUploaded (fun ex -> ErrorOccurred ex.Message)

    | BoxPhotoUploaded (Ok _) ->
        match state.BoxDetail with
        | None -> { state with Loading = false }, Cmd.none
        | Some detail ->
            { state with Loading = false },
            navigateCmd (BoxDetail detail.Box.Id)

    | BoxPhotoUploaded (Error err) ->
        { state with Error = Some err; Loading = false }, Cmd.none

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
                Cmd.OfAsync.either (fun () -> addItem detail.Box.Id name state.SelectedPhoto) () ItemAdded (fun ex -> ErrorOccurred ex.Message)

    | ItemAdded (Ok _) ->
        match state.BoxDetail with
        | None -> { state with Loading = false; SelectedPhoto = None }, Cmd.none
        | Some detail ->
            { state with Loading = false; SelectedPhoto = None },
            navigateCmd (BoxDetail detail.Box.Id)

    | ItemAdded (Error err) ->
        { state with Error = Some err; Loading = false }, Cmd.none

    | DeleteItem itemId ->
        match state.BoxDetail with
        | None -> state, Cmd.none
        | Some detail ->
            { state with Loading = true },
            Cmd.OfAsync.either (fun () -> deleteItem detail.Box.Id itemId) () ItemDeleted (fun ex -> ErrorOccurred ex.Message)

    | ItemDeleted (Ok _) ->
        match state.BoxDetail with
        | None -> { state with Loading = false }, Cmd.none
        | Some detail ->
            { state with Loading = false },
            navigateCmd (BoxDetail detail.Box.Id)

    | ItemDeleted (Error err) ->
        { state with Error = Some err; Loading = false }, Cmd.none

    | ShowMoveItemDialog itemId ->
        { state with MovingItemId = Some itemId; TargetBoxId = "" },
        Cmd.OfAsync.either (fun () -> getBoxes None) () BoxesForMoveLoaded (fun ex -> ErrorOccurred ex.Message)

    | BoxesForMoveLoaded (Ok boxes) ->
        { state with AvailableBoxes = boxes }, Cmd.none

    | BoxesForMoveLoaded (Error err) ->
        { state with Error = Some err; MovingItemId = None }, Cmd.none

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

    | ItemMoved (Ok _) ->
        match state.BoxDetail with
        | None -> { state with Loading = false; MovingItemId = None }, Cmd.none
        | Some detail ->
            { state with Loading = false; MovingItemId = None; TargetBoxId = "" },
            navigateCmd (BoxDetail detail.Box.Id)

    | ItemMoved (Error err) ->
        { state with Error = Some err; Loading = false; MovingItemId = None }, Cmd.none

    | CancelMoveItem ->
        { state with MovingItemId = None; TargetBoxId = "" }, Cmd.none

    | SearchQueryChanged query ->
        if System.String.IsNullOrWhiteSpace query then
            state.SearchDebounceTimer |> Option.iter clearTimeout
            { state with SearchQuery = query; SearchResults = [||]; SearchDebounceTimer = None }, Cmd.none
        else
            let cmd : Cmd<Msg> = Cmd.ofEffect (fun (dispatch: Msg -> unit) ->
                state.SearchDebounceTimer |> Option.iter clearTimeout
                let timerId : int = setTimeout (fun () -> dispatch (SearchDebounceTriggered query)) 300
                ()
            )
            { state with SearchQuery = query }, cmd

    | SearchDebounceTriggered query ->
        if query = state.SearchQuery then
            { state with SearchDebounceTimer = None },
            Cmd.OfAsync.either searchItems query SearchResultsLoaded (fun ex -> ErrorOccurred ex.Message)
        else
            state, Cmd.none

    | SearchResultsLoaded (Ok results) ->
        { state with SearchResults = results; Loading = false }, Cmd.none

    | SearchResultsLoaded (Error err) ->
        { state with Error = Some err; Loading = false }, Cmd.none

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

    | StandaloneItemCreated (Ok _) ->
        { state with ShowCreateItemForm = false; Loading = false; NewStandaloneItemName = ""; NewStandaloneItemBoxId = "" },
        navigateCmd ItemsList

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

    | ItemNameUpdated (Ok _) ->
        { state with EditingItemId = None; EditItemNameValue = ""; Loading = false },
        navigateCmd ItemsList

    | ItemNameUpdated (Error err) ->
        { state with Error = Some err; Loading = false }, Cmd.none

    | CancelEditItem ->
        { state with EditingItemId = None; EditItemNameValue = "" }, Cmd.none

    | DeleteStandaloneItem itemId ->
        { state with Loading = true },
        Cmd.OfAsync.either (fun () -> deleteItemStandalone itemId) () StandaloneItemDeleted (fun ex -> ErrorOccurred ex.Message)

    | StandaloneItemDeleted (Ok _) ->
        { state with Loading = false },
        navigateCmd ItemsList

    | StandaloneItemDeleted (Error err) ->
        { state with Error = Some err; Loading = false }, Cmd.none

    | ShowMoveItemStandaloneDialog itemId ->
        { state with MovingItemStandaloneId = Some itemId; MoveItemTargetBox = "" },
        Cmd.OfAsync.either (fun () -> getBoxes None) () (fun res -> match res with Ok boxes -> BoxesForItemMoveLoaded boxes | Error err -> ErrorOccurred err) (fun ex -> ErrorOccurred ex.Message)

    | BoxesForItemMoveLoaded boxes ->
        { state with BoxesForItemMove = boxes }, Cmd.none

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

    | StandaloneItemMoved (Ok _) ->
        { state with Loading = false; MovingItemStandaloneId = None; MoveItemTargetBox = "" },
        navigateCmd ItemsList

    | StandaloneItemMoved (Error err) ->
        { state with Error = Some err; Loading = false; MovingItemStandaloneId = None }, Cmd.none

    | CancelMoveItemStandalone ->
        { state with MovingItemStandaloneId = None; MoveItemTargetBox = "" }, Cmd.none

    | UploadItemPhoto (itemId, photo) ->
        { state with Loading = true },
        Cmd.OfAsync.either (fun () -> updateItemPhoto itemId photo) () ItemPhotoUploaded (fun ex -> ErrorOccurred ex.Message)

    | ItemPhotoUploaded (Ok _) ->
        { state with Loading = false },
        navigateCmd ItemsList

    | ItemPhotoUploaded (Error err) ->
        { state with Error = Some err; Loading = false }, Cmd.none

    | ShowAddExistingItemDialog ->
        { state with AddingExistingItem = true; SelectedExistingItemId = "" },
        Cmd.OfAsync.either listItems () UnassignedItemsLoaded (fun ex -> ErrorOccurred ex.Message)

    | UnassignedItemsLoaded (Ok items) ->
        let unassigned : SearchResultDto array =
            items |> Array.filter (fun (i: SearchResultDto) -> System.String.IsNullOrEmpty i.BoxId)
        { state with UnassignedItems = unassigned }, Cmd.none

    | UnassignedItemsLoaded (Error err) ->
        { state with Error = Some err; AddingExistingItem = false }, Cmd.none

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

    | ExistingItemAdded (Ok _) ->
        match state.BoxDetail with
        | None -> { state with Loading = false; AddingExistingItem = false }, Cmd.none
        | Some detail ->
            { state with Loading = false; AddingExistingItem = false; SelectedExistingItemId = ""; UnassignedItems = [||] },
            navigateCmd (BoxDetail detail.Box.Id)

    | ExistingItemAdded (Error err) ->
        { state with Error = Some err; Loading = false; AddingExistingItem = false }, Cmd.none

    | CancelAddExistingItem ->
        { state with AddingExistingItem = false; SelectedExistingItemId = ""; UnassignedItems = [||] }, Cmd.none

    | UnassignItem itemId ->
        { state with Loading = true },
        Cmd.OfAsync.either (fun () -> unassignEntity "item" itemId) () ItemUnassigned (fun ex -> ErrorOccurred ex.Message)

    | ItemUnassigned (Ok _) ->
        match state.BoxDetail with
        | None -> { state with Loading = false }, Cmd.none
        | Some detail ->
            { state with Loading = false },
            navigateCmd (BoxDetail detail.Box.Id)

    | ItemUnassigned (Error err) ->
        { state with Error = Some err; Loading = false }, Cmd.none

    | UnassignStandaloneItem itemId ->
        { state with Loading = true },
        Cmd.OfAsync.either (fun () -> unassignEntity "item" itemId) () StandaloneItemUnassigned (fun ex -> ErrorOccurred ex.Message)

    | StandaloneItemUnassigned (Ok _) ->
        { state with Loading = false },
        navigateCmd ItemsList

    | StandaloneItemUnassigned (Error err) ->
        { state with Error = Some err; Loading = false }, Cmd.none

    | ShowAddBoxToLocationDialog ->
        { state with AddingBoxToLocation = true; SelectedBoxForLocationMove = "" },
        Cmd.OfAsync.either (fun () -> getBoxes None) () (fun res -> match res with Ok boxes -> BoxesForLocationMoveLoaded boxes | Error err -> ErrorOccurred err) (fun ex -> ErrorOccurred ex.Message)

    | BoxesForLocationMoveLoaded boxes ->
        let currentLocCode : string =
            state.LocationDetail |> Option.map (fun d -> d.Location.Code) |> Option.defaultValue ""
        let filtered : BoxDto array =
            boxes |> Array.filter (fun b -> b.LocationCode <> Some currentLocCode)
        { state with BoxesForLocationMove = filtered }, Cmd.none

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

    | BoxMovedToLocation (Ok _) ->
        match state.LocationDetail with
        | None -> { state with Loading = false; AddingBoxToLocation = false }, Cmd.none
        | Some detail ->
            { state with Loading = false; AddingBoxToLocation = false; SelectedBoxForLocationMove = ""; BoxesForLocationMove = [||] },
            navigateCmd (LocationDetail detail.Location.Code)

    | BoxMovedToLocation (Error err) ->
        { state with Error = Some err; Loading = false; AddingBoxToLocation = false }, Cmd.none

    | CancelAddBoxToLocation ->
        { state with AddingBoxToLocation = false; SelectedBoxForLocationMove = ""; BoxesForLocationMove = [||] }, Cmd.none

    | UnassignBoxFromLocation boxId ->
        { state with Loading = true },
        Cmd.OfAsync.either (fun () -> unassignEntity "box" boxId) () BoxUnassignedFromLocation (fun ex -> ErrorOccurred ex.Message)

    | BoxUnassignedFromLocation (Ok _) ->
        match state.LocationDetail with
        | None -> { state with Loading = false }, Cmd.none
        | Some detail ->
            { state with Loading = false },
            navigateCmd (LocationDetail detail.Location.Code)

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
