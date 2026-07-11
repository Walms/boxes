module BoxTracker.Client.Api

open Fable.Core
open Fable.Core.JsInterop

type LocationDto = {
    Code: string
    Name: string
    IsArchived: bool
    PhotoPath: string option
    CreatedAt: string
}

type BoxDto = {
    Id: string
    Label: string option
    PhotoPath: string option
    LocationCode: string option
    CreatedAt: string
}

type ItemDto = {
    Id: string
    BoxId: string option
    Name: string
    PhotoPath: string option
    AddedAt: string
}

type SearchResultDto = {
    ItemId: string
    ItemName: string
    PhotoPath: string option
    BoxId: string
    BoxLabel: string option
    LocationCode: string option
    LocationName: string option
    AddedAt: string
}

type PhotoJobDto = {
    Id: string
    EntityType: string
    EntityId: string
    Status: string
    Error: string option
    PhotoPath: string option
}

type AddItemResultDto = {
    Item: ItemDto
    PhotoJobId: string option
}

type MoveDto = {
    Id: string
    EntityType: string
    EntityId: string
    ToType: string option
    ToId: string option
    MovedAt: string
}

type LocationDetailDto = {
    Location: LocationDto
    Boxes: BoxDto array
}

type BoxDetailDto = {
    Box: BoxDto
    Items: ItemDto array
}

type NoteDto = {
    Id: string
    EntityType: string
    EntityId: string
    Content: string
    CreatedAt: string
    UpdatedAt: string
}

type private ErrorDto = {
    error: string
}

[<Emit("fetch($0, $1)")>]
let private fetchReq (url: string) (options: obj) : JS.Promise<obj> = failwith "JS only"

[<Emit("fetch($0)")>]
let private fetchGet (url: string) : JS.Promise<obj> = failwith "JS only"

[<Emit("$0.text()")>]
let private responseText (resp: obj) : JS.Promise<string> = failwith "JS only"

[<Emit("$0.ok")>]
let private responseOk (resp: obj) : bool = failwith "JS only"

[<Emit("$0.status")>]
let private responseStatus (resp: obj) : int = failwith "JS only"

[<Emit("JSON.parse($0)")>]
let ofJson<'T> (json: string) : 'T = failwith "JS only"

[<Emit("encodeURIComponent($0)")>]
let private encodeUriComponent (s: string) : string = failwith "JS only"

[<Emit("JSON.stringify($0)")>]
let toJson (value: obj) : string = failwith "JS only"

[<Emit("new FormData()")>]
let createFormData () : obj = failwith "JS only"

[<Emit("$0.append($1, $2)")>]
let formDataAppend (fd: obj) (key: string) (value: obj) : unit = failwith "JS only"

[<Emit("$0.append($1, $2)")>]
let formDataAppendString (fd: obj) (key: string) (value: string) : unit = failwith "JS only"

[<Emit("""
new Promise((resolve, reject) => {
    const MAX = 1920;
    const QUALITY = 0.82;
    const img = new Image();
    img.onload = () => {
        let w = img.naturalWidth, h = img.naturalHeight;
        if (w <= MAX && h <= MAX && $0.size < 512 * 1024) { URL.revokeObjectURL(img.src); resolve($0); return; }
        const r = Math.min(MAX / w, MAX / h, 1);
        w = Math.round(w * r); h = Math.round(h * r);
        const canvas = document.createElement('canvas');
        canvas.width = w; canvas.height = h;
        canvas.getContext('2d').drawImage(img, 0, 0, w, h);
        URL.revokeObjectURL(img.src);
        canvas.toBlob(b => b ? resolve(b) : reject(new Error('Compression failed')), 'image/jpeg', QUALITY);
    };
    img.onerror = () => { URL.revokeObjectURL(img.src); reject(new Error('Failed to load image')); };
    img.src = URL.createObjectURL($0);
})
""")>]
let private compressImageJs (file: obj) : JS.Promise<obj> = failwith "JS only"

let compressImage (file: obj) : Async<obj> =
    compressImageJs file |> Async.AwaitPromise

let private parseError (status: int) (text: string) : string =
    try
        let err : ErrorDto = ofJson<ErrorDto> text
        if not (isNull (box err)) && not (isNull (box err.error)) && err.error <> "" then
            err.error
        else
            $"Server error (HTTP %d{status})"
    with _ ->
        if text <> null && text.Trim() <> "" && text.Length < 300 then
            $"Server error: %s{text.Trim()}"
        else
            $"Server error (HTTP %d{status})"

let private get<'T> (url: string) : Async<Result<'T, string>> =
    async {
        try
            let! resp = fetchGet url |> Async.AwaitPromise
            let! text = responseText resp |> Async.AwaitPromise
            if responseOk resp then
                return Ok(ofJson<'T> text)
            else
                return Error(parseError (responseStatus resp) text)
        with ex ->
            return Error $"Network error: %s{ex.Message}"
    }

let private send<'T> (method: string) (url: string) (body: obj option) : Async<Result<'T, string>> =
    async {
        try
            let options : obj =
                match body with
                | Some b ->
                    let o = createObj [
                        "method" ==> method
                        "headers" ==> createObj [ "Content-Type" ==> "application/json" ]
                        "body" ==> toJson b
                    ]
                    o
                | None ->
                    createObj [ "method" ==> method ]
            let! resp = fetchReq url options |> Async.AwaitPromise
            let! text = responseText resp |> Async.AwaitPromise
            if responseOk resp then
                return Ok(ofJson<'T> text)
            else
                return Error(parseError (responseStatus resp) text)
        with ex ->
            return Error $"Network error: %s{ex.Message}"
    }

let private upload<'T> (url: string) (fd: obj) : Async<Result<'T, string>> =
    async {
        try
            let options : obj =
                createObj [
                    "method" ==> "POST"
                    "body" ==> fd
                ]
            let! resp = fetchReq url options |> Async.AwaitPromise
            let! text = responseText resp |> Async.AwaitPromise
            if responseOk resp then
                return Ok(ofJson<'T> text)
            else
                return Error(parseError (responseStatus resp) text)
        with ex ->
            return Error $"Network error: %s{ex.Message}"
    }

let private deleteReq (url: string) : Async<Result<unit, string>> =
    async {
        try
            let options : obj = createObj [ "method" ==> "DELETE" ]
            let! resp = fetchReq url options |> Async.AwaitPromise
            let! text = responseText resp |> Async.AwaitPromise
            if responseOk resp then
                return Ok()
            else
                return Error(parseError (responseStatus resp) text)
        with ex ->
            return Error $"Network error: %s{ex.Message}"
    }

let getLocations () : Async<Result<LocationDto array, string>> =
    get<LocationDto array> "/api/locations"

let getLocationDetail (code: string) : Async<Result<LocationDetailDto, string>> =
    get<LocationDetailDto> $"/api/locations/%s{code}"

let createLocation (code: string) (name: string) : Async<Result<LocationDto, string>> =
    send<LocationDto> "POST" "/api/locations" (Some {| Code = code; Name = name |})

let updateLocation (code: string) (name: string) : Async<Result<LocationDto, string>> =
    send<LocationDto> "PUT" $"/api/locations/%s{code}" (Some {| Name = name |})

let updateLocationCode (oldCode: string) (newCode: string) : Async<Result<LocationDto, string>> =
    send<LocationDto> "PATCH" $"/api/locations/%s{oldCode}/code" (Some {| Code = newCode |})

let archiveLocation (code: string) : Async<Result<LocationDto, string>> =
    send<LocationDto> "DELETE" $"/api/locations/%s{code}" None

let getBoxes (location: string option) : Async<Result<BoxDto array, string>> =
    match location with
    | Some loc -> get<BoxDto array> $"/api/boxes?location=%s{loc}"
    | None -> get<BoxDto array> "/api/boxes"

let getBoxDetail (id: string) : Async<Result<BoxDetailDto, string>> =
    get<BoxDetailDto> $"/api/boxes/%s{id}"

let createBox (label: string) : Async<Result<BoxDto, string>> =
    send<BoxDto> "POST" "/api/boxes" (Some {| Label = label |})

let updateBox (id: string) (label: string) (locationCode: string) : Async<Result<BoxDto, string>> =
    send<BoxDto> "PUT" $"/api/boxes/%s{id}" (Some {| Label = label; LocationCode = locationCode |})

let deleteBox (id: string) : Async<Result<unit, string>> =
    deleteReq $"/api/boxes/%s{id}"

let createItem (name: string) (boxId: string) : Async<Result<ItemDto, string>> =
    send<ItemDto> "POST" "/api/items" (Some {| Name = name; BoxId = boxId |})

let getItem (id: string) : Async<Result<SearchResultDto, string>> =
    get<SearchResultDto> $"/api/items/%s{id}"

let deleteItemStandalone (itemId: string) : Async<Result<unit, string>> =
    deleteReq $"/api/items/%s{itemId}"

let addItem (boxId: string) (name: string) (photo: obj option) : Async<Result<AddItemResultDto, string>> =
    let fd : obj = createFormData ()
    formDataAppendString fd "name" name
    photo |> Option.iter (fun p -> formDataAppend fd "photo" p)
    upload<AddItemResultDto> $"/api/boxes/%s{boxId}/items" fd

let updateItem (boxId: string) (itemId: string) (name: string) : Async<Result<ItemDto, string>> =
    send<ItemDto> "PUT" $"/api/boxes/%s{boxId}/items/%s{itemId}" (Some {| Name = name |})

let deleteItem (boxId: string) (itemId: string) : Async<Result<unit, string>> =
    deleteReq $"/api/boxes/%s{boxId}/items/%s{itemId}"

let moveEntity (entityType: string) (entityId: string) (toType: string) (toId: string) : Async<Result<MoveDto, string>> =
    send<MoveDto> "POST" "/api/moves" (Some {| EntityType = entityType; EntityId = entityId; ToType = toType; ToId = toId |})

let unassignEntity (entityType: string) (entityId: string) : Async<Result<MoveDto, string>> =
    send<MoveDto> "POST" "/api/moves" (Some {| EntityType = entityType; EntityId = entityId; ToType = ""; ToId = "" |})

let getMoveHistory (entityType: string) (entityId: string) : Async<Result<MoveDto array, string>> =
    get<MoveDto array> $"/api/moves?entityType=%s{entityType}&entityId=%s{entityId}"

let listItems () : Async<Result<SearchResultDto array, string>> =
    get<SearchResultDto array> "/api/items"

let updateItemStandalone (itemId: string) (name: string) : Async<Result<ItemDto, string>> =
    send<ItemDto> "PUT" $"/api/items/%s{itemId}" (Some {| Name = name |})

let updateItemPhoto (itemId: string) (photo: obj) : Async<Result<PhotoJobDto, string>> =
    let fd : obj = createFormData ()
    formDataAppend fd "photo" photo
    upload<PhotoJobDto> $"/api/items/%s{itemId}/photo" fd

let uploadBoxPhoto (boxId: string) (photo: obj) : Async<Result<PhotoJobDto, string>> =
    let fd : obj = createFormData ()
    formDataAppend fd "photo" photo
    upload<PhotoJobDto> $"/api/boxes/%s{boxId}/photo" fd

let uploadLocationPhoto (locationCode: string) (photo: obj) : Async<Result<PhotoJobDto, string>> =
    let fd : obj = createFormData ()
    formDataAppend fd "photo" photo
    upload<PhotoJobDto> $"/api/locations/%s{locationCode}/photo" fd

let getPhotoJob (jobId: string) : Async<Result<PhotoJobDto, string>> =
    get<PhotoJobDto> $"/api/photo-jobs/%s{jobId}"

let searchItems (query: string) : Async<Result<SearchResultDto array, string>> =
    get<SearchResultDto array> $"/api/items?q=%s{encodeUriComponent query}"

let getNotes (entityType: string) (entityId: string) : Async<Result<NoteDto array, string>> =
    get<NoteDto array> $"/api/notes?entityType=%s{entityType}&entityId=%s{entityId}"

let createNote (entityType: string) (entityId: string) (content: string) : Async<Result<NoteDto, string>> =
    send<NoteDto> "POST" "/api/notes" (Some {| EntityType = entityType; EntityId = entityId; Content = content |})

let updateNote (noteId: string) (content: string) : Async<Result<NoteDto, string>> =
    send<NoteDto> "PUT" $"/api/notes/%s{noteId}" (Some {| Content = content |})

let deleteNote (noteId: string) : Async<Result<unit, string>> =
    deleteReq $"/api/notes/%s{noteId}"
