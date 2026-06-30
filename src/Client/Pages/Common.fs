module BoxTracker.Client.Pages.Common

#if FABLE_COMPILER
open Browser.Types
open Browser.Dom
#endif
open Feliz
open BoxTracker.Client.State
open BoxTracker.Client.Api

[<Fable.Core.Emit("new Date($0).toLocaleString(undefined, { dateStyle: 'medium', timeStyle: 'short' })")>]
let formatDate (iso: string) : string = failwith "JS only"

let photoUrl (path: string option) (variant: string) : string option =
    path |> Option.map (fun p -> $"/api/%s{p}-%s{variant}.jpg")

let photoUrlFull (path: string option) : string option =
    photoUrl path "full"

let photoUrlThumb (path: string option) : string option =
    photoUrl path "thumb"

let imageViewer (state: State) (dispatch: Msg -> unit) : ReactElement =
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
                    prop.className "relative w-11/12 h-5/6 max-w-7xl"
                    prop.children [
                        Html.img [
                            prop.className "w-full h-full object-contain"
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

let navItem (label: string) (page: Page) (dispatch: Msg -> unit) : ReactElement =
    Html.a [
        prop.text label
        prop.onClick (fun _ -> dispatch (Navigate page))
    ]

let breadcrumb (items: (string * Page option) list) (dispatch: Msg -> unit) : ReactElement =
    Html.div [
        prop.className "breadcrumbs text-sm mb-4 sm:mb-6"
        prop.children [
            Html.ul [
                prop.children [
                    for (label, page) in items do
                        Html.li [
                            match page with
                            | Some p ->
                                Html.a [
                                    prop.className "opacity-60 hover:opacity-100"
                                    prop.onClick (fun _ -> dispatch (Navigate p))
                                    prop.text label
                                ]
                            | None ->
                                Html.span [
                                    prop.className "font-semibold"
                                    prop.text label
                                ]
                        ]
                ]
            ]
        ]
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
                prop.className "flex-none flex items-center"
                prop.children [
                    Html.button [
                        prop.className "btn btn-ghost btn-circle btn-lg md:btn-md"
                        prop.title "Scan QR Code"
                        prop.onClick (fun _ -> dispatch OpenScanner)
                        prop.children [
                            Svg.svg [
                                svg.viewBox "0 0 21 21"
                                svg.className "w-6 h-6"
                                svg.fill "currentColor"
                                svg.children [
                                    // Top-left finder pattern (border + center)
                                    Svg.rect [ svg.x 0; svg.y 0; svg.width 7; svg.height 1 ]
                                    Svg.rect [ svg.x 0; svg.y 6; svg.width 7; svg.height 1 ]
                                    Svg.rect [ svg.x 0; svg.y 1; svg.width 1; svg.height 5 ]
                                    Svg.rect [ svg.x 6; svg.y 1; svg.width 1; svg.height 5 ]
                                    Svg.rect [ svg.x 2; svg.y 2; svg.width 3; svg.height 3 ]
                                    // Top-right finder pattern
                                    Svg.rect [ svg.x 14; svg.y 0; svg.width 7; svg.height 1 ]
                                    Svg.rect [ svg.x 14; svg.y 6; svg.width 7; svg.height 1 ]
                                    Svg.rect [ svg.x 14; svg.y 1; svg.width 1; svg.height 5 ]
                                    Svg.rect [ svg.x 20; svg.y 1; svg.width 1; svg.height 5 ]
                                    Svg.rect [ svg.x 16; svg.y 2; svg.width 3; svg.height 3 ]
                                    // Bottom-left finder pattern
                                    Svg.rect [ svg.x 0; svg.y 14; svg.width 7; svg.height 1 ]
                                    Svg.rect [ svg.x 0; svg.y 20; svg.width 7; svg.height 1 ]
                                    Svg.rect [ svg.x 0; svg.y 15; svg.width 1; svg.height 5 ]
                                    Svg.rect [ svg.x 6; svg.y 15; svg.width 1; svg.height 5 ]
                                    Svg.rect [ svg.x 2; svg.y 16; svg.width 3; svg.height 3 ]
                                    // Timing patterns (row 6 and col 6)
                                    Svg.rect [ svg.x 8;  svg.y 6; svg.width 1; svg.height 1 ]
                                    Svg.rect [ svg.x 10; svg.y 6; svg.width 1; svg.height 1 ]
                                    Svg.rect [ svg.x 12; svg.y 6; svg.width 1; svg.height 1 ]
                                    Svg.rect [ svg.x 6; svg.y 8;  svg.width 1; svg.height 1 ]
                                    Svg.rect [ svg.x 6; svg.y 10; svg.width 1; svg.height 1 ]
                                    Svg.rect [ svg.x 6; svg.y 12; svg.width 1; svg.height 1 ]
                                    // Format info strips
                                    Svg.rect [ svg.x 8; svg.y 0; svg.width 1; svg.height 1 ]
                                    Svg.rect [ svg.x 8; svg.y 2; svg.width 1; svg.height 1 ]
                                    Svg.rect [ svg.x 8; svg.y 4; svg.width 1; svg.height 1 ]
                                    Svg.rect [ svg.x 0; svg.y 8; svg.width 1; svg.height 1 ]
                                    Svg.rect [ svg.x 2; svg.y 8; svg.width 1; svg.height 1 ]
                                    Svg.rect [ svg.x 4; svg.y 8; svg.width 1; svg.height 1 ]
                                    // Data modules
                                    Svg.rect [ svg.x 8;  svg.y 9;  svg.width 2; svg.height 1 ]
                                    Svg.rect [ svg.x 11; svg.y 9;  svg.width 1; svg.height 2 ]
                                    Svg.rect [ svg.x 13; svg.y 9;  svg.width 2; svg.height 1 ]
                                    Svg.rect [ svg.x 17; svg.y 9;  svg.width 1; svg.height 1 ]
                                    Svg.rect [ svg.x 19; svg.y 9;  svg.width 2; svg.height 1 ]
                                    Svg.rect [ svg.x 9;  svg.y 10; svg.width 1; svg.height 1 ]
                                    Svg.rect [ svg.x 14; svg.y 10; svg.width 1; svg.height 1 ]
                                    Svg.rect [ svg.x 16; svg.y 10; svg.width 1; svg.height 2 ]
                                    Svg.rect [ svg.x 18; svg.y 10; svg.width 1; svg.height 1 ]
                                    Svg.rect [ svg.x 8;  svg.y 11; svg.width 1; svg.height 1 ]
                                    Svg.rect [ svg.x 12; svg.y 11; svg.width 1; svg.height 2 ]
                                    Svg.rect [ svg.x 14; svg.y 11; svg.width 1; svg.height 1 ]
                                    Svg.rect [ svg.x 19; svg.y 11; svg.width 2; svg.height 1 ]
                                    Svg.rect [ svg.x 9;  svg.y 12; svg.width 2; svg.height 1 ]
                                    Svg.rect [ svg.x 13; svg.y 12; svg.width 1; svg.height 1 ]
                                    Svg.rect [ svg.x 15; svg.y 12; svg.width 2; svg.height 1 ]
                                    Svg.rect [ svg.x 18; svg.y 12; svg.width 1; svg.height 1 ]
                                    Svg.rect [ svg.x 20; svg.y 12; svg.width 1; svg.height 1 ]
                                    Svg.rect [ svg.x 8;  svg.y 14; svg.width 1; svg.height 1 ]
                                    Svg.rect [ svg.x 10; svg.y 14; svg.width 3; svg.height 1 ]
                                    Svg.rect [ svg.x 15; svg.y 14; svg.width 1; svg.height 2 ]
                                    Svg.rect [ svg.x 17; svg.y 14; svg.width 1; svg.height 1 ]
                                    Svg.rect [ svg.x 19; svg.y 14; svg.width 2; svg.height 1 ]
                                    Svg.rect [ svg.x 9;  svg.y 15; svg.width 1; svg.height 1 ]
                                    Svg.rect [ svg.x 12; svg.y 15; svg.width 2; svg.height 1 ]
                                    Svg.rect [ svg.x 16; svg.y 15; svg.width 1; svg.height 1 ]
                                    Svg.rect [ svg.x 18; svg.y 15; svg.width 1; svg.height 1 ]
                                    Svg.rect [ svg.x 20; svg.y 15; svg.width 1; svg.height 1 ]
                                    Svg.rect [ svg.x 8;  svg.y 16; svg.width 1; svg.height 1 ]
                                    Svg.rect [ svg.x 11; svg.y 16; svg.width 1; svg.height 1 ]
                                    Svg.rect [ svg.x 13; svg.y 16; svg.width 2; svg.height 1 ]
                                    Svg.rect [ svg.x 16; svg.y 16; svg.width 1; svg.height 1 ]
                                    Svg.rect [ svg.x 19; svg.y 16; svg.width 2; svg.height 1 ]
                                    Svg.rect [ svg.x 9;  svg.y 17; svg.width 2; svg.height 1 ]
                                    Svg.rect [ svg.x 12; svg.y 17; svg.width 1; svg.height 1 ]
                                    Svg.rect [ svg.x 14; svg.y 17; svg.width 1; svg.height 1 ]
                                    Svg.rect [ svg.x 17; svg.y 17; svg.width 2; svg.height 1 ]
                                    Svg.rect [ svg.x 20; svg.y 17; svg.width 1; svg.height 1 ]
                                    Svg.rect [ svg.x 8;  svg.y 18; svg.width 1; svg.height 1 ]
                                    Svg.rect [ svg.x 11; svg.y 18; svg.width 2; svg.height 1 ]
                                    Svg.rect [ svg.x 14; svg.y 18; svg.width 1; svg.height 1 ]
                                    Svg.rect [ svg.x 16; svg.y 18; svg.width 1; svg.height 1 ]
                                    Svg.rect [ svg.x 19; svg.y 18; svg.width 1; svg.height 1 ]
                                    Svg.rect [ svg.x 9;  svg.y 19; svg.width 1; svg.height 1 ]
                                    Svg.rect [ svg.x 12; svg.y 19; svg.width 2; svg.height 1 ]
                                    Svg.rect [ svg.x 15; svg.y 19; svg.width 1; svg.height 2 ]
                                    Svg.rect [ svg.x 17; svg.y 19; svg.width 1; svg.height 1 ]
                                    Svg.rect [ svg.x 20; svg.y 19; svg.width 1; svg.height 1 ]
                                    Svg.rect [ svg.x 8;  svg.y 20; svg.width 2; svg.height 1 ]
                                    Svg.rect [ svg.x 11; svg.y 20; svg.width 1; svg.height 1 ]
                                    Svg.rect [ svg.x 14; svg.y 20; svg.width 1; svg.height 1 ]
                                    Svg.rect [ svg.x 17; svg.y 20; svg.width 1; svg.height 1 ]
                                    Svg.rect [ svg.x 19; svg.y 20; svg.width 2; svg.height 1 ]
                                ]
                            ]
                        ]
                    ]
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
                                    Html.li [
                                        Html.a [
                                            prop.text "Scan QR"
                                            prop.onClick (fun _ -> dispatch OpenScanner)
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

let errorAlert (state: State) (dispatch: Msg -> unit) : ReactElement =
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

let loadingSpinner (state: State) : ReactElement =
    if state.Loading then
        Html.div [
            prop.className "flex justify-center p-8"
            prop.children [
                Html.span [ prop.className "loading loading-spinner loading-lg" ]
            ]
        ]
    else Html.none

/// Full-width centred spinner for use inside a grid (spans all columns).
let gridLoadingSpinner : ReactElement =
    Html.div [
        prop.className "col-span-full flex justify-center py-12"
        prop.children [
            Html.span [ prop.className "loading loading-spinner loading-lg" ]
        ]
    ]

/// Centred spinner for use inside modal dialogs while their data loads.
let dialogLoadingSpinner : ReactElement =
    Html.div [
        prop.className "flex justify-center py-8"
        prop.children [
            Html.span [ prop.className "loading loading-spinner loading-md" ]
        ]
    ]

let photoStatusBanner (state: State) (dispatch: Msg -> unit) : ReactElement =
    if state.UploadingPhoto then
        Html.div [
            prop.className "flex items-center gap-2 p-3"
            prop.children [
                Html.span [ prop.className "loading loading-spinner loading-sm" ]
                Html.span [ prop.className "text-sm opacity-70"; prop.text "Uploading…" ]
            ]
        ]
    elif state.PhotoProcessing then
        Html.div [
            prop.className "alert alert-info rounded-lg gap-3 flex items-center text-sm py-2 px-3 mt-2"
            prop.children [
                Html.div [
                    prop.className "flex-1"
                    prop.children [
                        Html.span [ prop.className "loading loading-spinner loading-xs mr-2" ]
                        Html.text "Processing photo in the background — you can navigate away."
                    ]
                ]
                Html.button [
                    prop.className "btn btn-ghost btn-xs"
                    prop.text "OK"
                    prop.onClick (fun _ -> dispatch DismissPhotoProcessing)
                ]
            ]
        ]
    else Html.none

let historyModal (state: State) (dispatch: Msg -> unit) : ReactElement =
    if not state.ShowHistoryModal then Html.none
    else
        let moveDescription (move: MoveDto) =
            match move.ToType, move.ToId with
            | Some "location", Some code ->
                let name =
                    state.AvailableLocations
                    |> Array.tryFind (fun l -> l.Code = code)
                    |> Option.map (fun l -> l.Name)
                    |> Option.defaultValue code
                $"Moved to %s{name}"
            | Some "box", Some id -> $"Moved to %s{id}"
            | _ -> "Unassigned"

        let moves = state.HistoryMoves |> Array.rev
        let events : (string * string) list = [
            match state.HistoryCreatedAt with
            | Some dt -> yield ("Created", dt)
            | None -> ()
            for move in moves do
                yield (moveDescription move, move.MovedAt)
        ]
        let eventsArr = List.toArray events

        Html.div [
            prop.className "modal modal-open z-50"
            prop.onClick (fun e -> if e.currentTarget = e.target then dispatch CloseHistory)
            prop.children [
                Html.div [
                    prop.className "modal-box w-11/12 max-w-lg"
                    prop.children [
                        Html.div [
                            prop.className "flex items-center justify-between mb-1"
                            prop.children [
                                Html.h3 [ prop.className "font-bold text-lg"; prop.text "History" ]
                                Html.button [
                                    prop.className "btn btn-ghost btn-sm btn-circle"
                                    prop.text "✕"
                                    prop.onClick (fun _ -> dispatch CloseHistory)
                                ]
                            ]
                        ]
                        Html.p [ prop.className "text-sm opacity-60 mb-4 truncate"; prop.text state.HistoryTitle ]
                        if state.HistoryLoading then
                            Html.div [
                                prop.className "flex justify-center p-8"
                                prop.children [ Html.span [ prop.className "loading loading-spinner loading-md" ] ]
                            ]
                        elif Array.isEmpty eventsArr then
                            Html.p [ prop.className "text-center py-6 opacity-60 text-sm"; prop.text "No history recorded yet" ]
                        else
                            Html.ul [
                                prop.className "space-y-0 py-2"
                                prop.children [
                                    for i in 0 .. eventsArr.Length - 1 do
                                        let (label, timestamp) = eventsArr.[i]
                                        let isLast = i = eventsArr.Length - 1
                                        Html.li [
                                            prop.className "flex gap-3 items-start"
                                            prop.children [
                                                Html.div [
                                                    prop.className "flex flex-col items-center flex-shrink-0 pt-0.5"
                                                    prop.children [
                                                        Html.div [ prop.className "w-2.5 h-2.5 rounded-full bg-primary ring-2 ring-base-100" ]
                                                        if not isLast then
                                                            Html.div [ prop.className "w-px bg-base-300 flex-1 min-h-5 mt-1" ]
                                                    ]
                                                ]
                                                Html.div [
                                                    prop.className "pb-4 min-w-0"
                                                    prop.children [
                                                        Html.p [ prop.className "text-sm font-medium leading-tight"; prop.text label ]
                                                        Html.p [ prop.className "text-xs opacity-60 mt-0.5"; prop.text (formatDate timestamp) ]
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

[<Fable.Core.Emit("""
(function(videoEl, onFound, onError) {
    var stream = null;
    var rafId = 0;
    var found = false;
    var jsQR = null;
    var canvas = document.createElement('canvas');
    var ctx = canvas.getContext('2d');
    function scan() {
        if (found) return;
        if (jsQR && videoEl.readyState >= 2 && videoEl.videoWidth > 0) {
            canvas.width = videoEl.videoWidth;
            canvas.height = videoEl.videoHeight;
            ctx.drawImage(videoEl, 0, 0);
            var img = ctx.getImageData(0, 0, canvas.width, canvas.height);
            var result = jsQR(img.data, img.width, img.height);
            if (result && result.data) {
                found = true;
                onFound(result.data);
                return;
            }
        }
        rafId = requestAnimationFrame(scan);
    }
    (async function() {
        // Load jsQR on demand so it is code-split into its own chunk and only
        // downloaded when the scanner is actually opened.
        try { jsQR = (await import('jsqr')).default; }
        catch(e) { onError('Failed to load scanner'); return; }
        var tried = [
            { video: { facingMode: { exact: 'environment' } } },
            { video: { facingMode: 'environment' } },
            { video: true }
        ];
        for (var c of tried) {
            try { stream = await navigator.mediaDevices.getUserMedia(c); break; } catch(e) {}
        }
        if (!stream) { onError('Camera not available'); return; }
        videoEl.srcObject = stream;
        try {
            await new Promise(function(r) { videoEl.onloadedmetadata = r; });
            await videoEl.play();
        } catch(e) {}
        scan();
    })().catch(function(e) { onError(e.message || 'Camera error'); });
    return function() {
        found = true;
        cancelAnimationFrame(rafId);
        if (stream) stream.getTracks().forEach(function(t) { t.stop(); });
    };
})($0, $1, $2)
""")>]
let initScanner (videoEl: obj) (onFound: string -> unit) (onError: string -> unit) : (unit -> unit) = failwith "JS only"

[<Fable.Core.Emit("document.getElementById($0)")>]
let getElementById (id: string) : obj = failwith "JS only"

#if FABLE_COMPILER
[<ReactComponent>]
let QrScannerComponent (dispatch: Msg -> unit) : ReactElement =
    let errorMsg, setErrorMsg = React.useState<string option>(None)
    React.useEffect(fun () ->
        let videoEl = getElementById "qr-scanner-video"
        let cleanup =
            if isNull videoEl then
                setErrorMsg (Some "Video element not found")
                fun () -> ()
            else
                initScanner videoEl
                    (fun text -> dispatch (QrScanned text))
                    (fun err -> setErrorMsg (Some err))
        { new System.IDisposable with member _.Dispose() = cleanup() }
    , [||])
    Html.div [
        prop.className "relative w-full"
        prop.children [
            match errorMsg with
            | Some err ->
                Html.div [
                    prop.className "alert alert-error text-sm"
                    prop.text err
                ]
            | None ->
                Html.div [
                    prop.className "relative"
                    prop.children [
                        Html.video [
                            prop.id "qr-scanner-video"
                            prop.autoPlay true
                            prop.custom ("playsInline", true)
                            prop.className "w-full rounded-lg"
                        ]
                        Html.div [
                            prop.className "absolute bottom-2 inset-x-0 flex justify-center"
                            prop.children [
                                Html.span [
                                    prop.className "badge badge-primary text-xs px-3 py-1"
                                    prop.text "Looking for QR code..."
                                ]
                            ]
                        ]
                    ]
                ]
        ]
    ]
#else
let QrScannerComponent (_dispatch: Msg -> unit) : ReactElement = Html.none
#endif

let notesSection (state: State) (dispatch: Msg -> unit) : ReactElement =
    Html.div [
        prop.className "mt-6"
        prop.children [
            Html.div [
                prop.className "flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between mb-4"
                prop.children [
                    Html.h2 [
                        prop.className "text-lg sm:text-xl font-bold"
                        prop.text $"Notes (%i{state.Notes.Length})"
                    ]
                    if not state.ShowAddNoteForm then
                        Html.button [
                            prop.className "btn btn-outline btn-sm w-full sm:w-auto"
                            prop.text "+ Add Note"
                            prop.onClick (fun _ -> dispatch ShowAddNoteForm)
                        ]
                ]
            ]
            if state.ShowAddNoteForm then
                Html.div [
                    prop.className "card bg-base-200 border border-base-300 mb-4 shadow-sm"
                    prop.children [
                        Html.div [
                            prop.className "card-body p-4"
                            prop.children [
                                Html.textarea [
                                    prop.className "textarea textarea-bordered w-full text-base min-h-24 focus:textarea-primary"
                                    prop.placeholder "Write a note..."
                                    prop.value state.NewNoteContent
                                    prop.onChange (fun (s: string) -> dispatch (NewNoteContentChanged s))
                                    prop.autoFocus true
                                ]
                                Html.div [
                                    prop.className "flex gap-2 justify-end mt-2"
                                    prop.children [
                                        Html.button [
                                            prop.className "btn btn-ghost btn-sm"
                                            prop.text "Cancel"
                                            prop.onClick (fun _ -> dispatch CancelAddNote)
                                        ]
                                        Html.button [
                                            prop.className "btn btn-success btn-sm"
                                            prop.text "Save"
                                            prop.disabled (System.String.IsNullOrWhiteSpace state.NewNoteContent)
                                            prop.onClick (fun _ -> dispatch SubmitCreateNote)
                                        ]
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]
            if Array.isEmpty state.Notes && not state.ShowAddNoteForm then
                Html.div [
                    prop.className "text-center py-6 opacity-60"
                    prop.children [
                        Html.p [ prop.text "No notes yet" ]
                    ]
                ]
            for note in state.Notes do
                Html.div [
                    prop.key note.Id
                    prop.className "card bg-base-200 border border-base-300 mb-3 shadow-sm"
                    prop.children [
                        Html.div [
                            prop.className "card-body p-4"
                            prop.children [
                                if state.EditingNoteId = Some note.Id then
                                    Html.textarea [
                                        prop.className "textarea textarea-bordered w-full text-base min-h-24 focus:textarea-primary mb-2"
                                        prop.value state.EditNoteContent
                                        prop.onChange (fun (s: string) -> dispatch (EditNoteContentChanged s))
                                        prop.autoFocus true
                                    ]
                                    Html.div [
                                        prop.className "flex gap-2 justify-end"
                                        prop.children [
                                            Html.button [
                                                prop.className "btn btn-ghost btn-sm"
                                                prop.text "Cancel"
                                                prop.onClick (fun _ -> dispatch CancelEditNote)
                                            ]
                                            Html.button [
                                                prop.className "btn btn-success btn-sm"
                                                prop.text "Save"
                                                prop.disabled (System.String.IsNullOrWhiteSpace state.EditNoteContent)
                                                prop.onClick (fun _ -> dispatch SubmitEditNote)
                                            ]
                                        ]
                                    ]
                                else
                                    Html.div [
                                        prop.className "flex flex-col sm:flex-row sm:items-start sm:justify-between gap-2"
                                        prop.children [
                                            Html.p [
                                                prop.className "text-base whitespace-pre-wrap flex-1"
                                                prop.text note.Content
                                            ]
                                            Html.div [
                                                prop.className "flex gap-1 flex-shrink-0"
                                                prop.children [
                                                    Html.button [
                                                        prop.className "btn btn-ghost btn-xs"
                                                        prop.text "Edit"
                                                        prop.onClick (fun _ -> dispatch (StartEditNote (note.Id, note.Content)))
                                                    ]
                                                    Html.button [
                                                        prop.className "btn btn-ghost btn-xs text-error"
                                                        prop.text "Delete"
                                                        prop.onClick (fun _ -> dispatch (DeleteNote note.Id))
                                                    ]
                                                ]
                                            ]
                                        ]
                                    ]
                                    Html.div [
                                        prop.className "text-xs opacity-50 mt-2"
                                        prop.children [
                                            if note.UpdatedAt <> note.CreatedAt then
                                                Html.text $"Added %s{formatDate note.CreatedAt} · Edited %s{formatDate note.UpdatedAt}"
                                            else
                                                Html.text $"Added %s{formatDate note.CreatedAt}"
                                        ]
                                    ]
                            ]
                        ]
                    ]
                ]
        ]
    ]

let scannerModal (state: State) (dispatch: Msg -> unit) : ReactElement =
    if not state.ScannerOpen then Html.none
    else
        Html.div [
            prop.className "modal modal-open z-50"
            prop.onClick (fun e ->
                if e.currentTarget = e.target then dispatch CloseScanner
            )
            prop.children [
                Html.div [
                    prop.className "modal-box w-11/12 max-w-sm"
                    prop.children [
                        Html.div [
                            prop.className "flex justify-between items-center mb-4"
                            prop.children [
                                Html.h3 [
                                    prop.className "font-bold text-lg"
                                    prop.text "Scan QR Code"
                                ]
                                Html.button [
                                    prop.className "btn btn-ghost btn-sm btn-circle"
                                    prop.text "✕"
                                    prop.onClick (fun _ -> dispatch CloseScanner)
                                ]
                            ]
                        ]
                        QrScannerComponent dispatch
                        Html.div [
                            prop.className "modal-action mt-4"
                            prop.children [
                                Html.button [
                                    prop.className "btn btn-ghost btn-sm"
                                    prop.text "Cancel"
                                    prop.onClick (fun _ -> dispatch CloseScanner)
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]

