module BoxTracker.Handlers.BoxHandlers

open System
open System.IO
open Giraffe
open Microsoft.AspNetCore.Http
open BoxTracker.Storage
open BoxTracker.Types
open BoxTracker.BoxLabel
open BoxTracker.PhotoPath
open BoxTracker.Container
open BoxTracker.Dto
open BoxTracker.ImageProcessing

let private deletePhotoFiles (dataDir: string) (paths: string list) : unit =
    for (path: string) in paths do
        let basePath : string = Path.Combine(dataDir, path)
        let fullPath : string = $"%s{basePath}-full.webp"
        let thumbPath : string = $"%s{basePath}-thumb.webp"
        if File.Exists(fullPath) then File.Delete(fullPath)
        if File.Exists(thumbPath) then File.Delete(thumbPath)

let private deletePhotoFile (dataDir: string) (path: string) : unit =
    let basePath : string = Path.Combine(dataDir, path)
    let fullPath : string = $"%s{basePath}-full.webp"
    let thumbPath : string = $"%s{basePath}-thumb.webp"
    if File.Exists(fullPath) then File.Delete(fullPath)
    if File.Exists(thumbPath) then File.Delete(thumbPath)

let listBoxes : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let storage : Storage = ctx.GetService<Storage>()
            let locationFilter : string option =
                ctx.TryGetQueryStringValue "location" |> Option.map (fun (s: string) -> s)
            let unassigned : bool =
                match ctx.TryGetQueryStringValue "unassigned" with
                | Some "true" -> true
                | _ -> false
            let boxes : Box list = storage.ListBoxes(locationFilter, unassigned)
            let dtos : BoxResponse list = boxes |> List.map boxToDto
            return! json dtos next ctx
        }

let getBox (id: string) : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let storage : Storage = ctx.GetService<Storage>()
            match storage.GetBox(id) with
            | None ->
                return! (setStatusCode 404 >=> json {| error = $"Box '%s{id}' not found" |}) next ctx
            | Some box ->
                let items : Item list = storage.GetItemsForBox(id)
                let response : BoxDetailResponse = {
                    Box = boxToDto box
                    Items = items |> List.map itemToDto
                }
                return! json response next ctx
        }

let createBox : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let! request : CreateBoxRequest = ctx.BindJsonAsync<CreateBoxRequest>()
            let storage : Storage = ctx.GetService<Storage>()
            match BoxTracker.BoxLabel.create request.Label with
            | Error msg ->
                return! (setStatusCode 400 >=> json {| error = msg |}) next ctx
            | Ok label ->
                let box : Box = storage.CreateBox(label)
                return! (setStatusCode 201 >=> json (boxToDto box)) next ctx
        }

let updateBox (id: string) : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let! request : UpdateBoxRequest = ctx.BindJsonAsync<UpdateBoxRequest>()
            let storage : Storage = ctx.GetService<Storage>()
            let labelResult : Result<BoxLabel option, string> = BoxTracker.BoxLabel.create request.Label
            match labelResult with
            | Error msg ->
                return! (setStatusCode 400 >=> json {| error = msg |}) next ctx
            | Ok label ->
                match storage.UpdateBox(id, label) with
                | None ->
                    return! (setStatusCode 404 >=> json {| error = $"Box '%s{id}' not found" |}) next ctx
                | Some box ->
                    let needsMove : bool =
                        if not (String.IsNullOrEmpty request.LocationCode) then
                            match box.Placement with
                            | AtLocation code when BoxTracker.LocationCode.value code = request.LocationCode -> false
                            | _ -> true
                        else
                            match box.Placement with
                            | AtLocation _ -> true
                            | _ -> false
                    if needsMove then
                        if not (String.IsNullOrEmpty request.LocationCode) then
                            match BoxTracker.LocationCode.create request.LocationCode with
                            | Error msg ->
                                return! (setStatusCode 400 >=> json {| error = msg |}) next ctx
                            | Ok _ ->
                                storage.RecordMove("box", id, Some "location", Some request.LocationCode) |> ignore
                                let updatedBox : Box option = storage.GetBox(id)
                                return! json (boxToDto updatedBox.Value) next ctx
                        else
                            storage.RecordMove("box", id, None, None) |> ignore
                            let updatedBox : Box option = storage.GetBox(id)
                            return! json (boxToDto updatedBox.Value) next ctx
                    else
                        return! json (boxToDto box) next ctx
        }

let deleteBox (id: string) : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let storage : Storage = ctx.GetService<Storage>()
            let config : BoxTrackerConfig = ctx.GetService<BoxTrackerConfig>()
            let photoPaths : string list = storage.DeleteBox(id)
            deletePhotoFiles config.DataDir photoPaths
            return! json {| success = true |} next ctx
        }

let uploadBoxPhoto (id: string) : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            if not ctx.Request.HasFormContentType then
                return! (setStatusCode 400 >=> json {| error = "Expected multipart/form-data" |}) next ctx
            else
                let storage : Storage = ctx.GetService<Storage>()
                let config : BoxTrackerConfig = ctx.GetService<BoxTrackerConfig>()
                match storage.GetBox(id) with
                | None ->
                    return! (setStatusCode 404 >=> json {| error = $"Box '%s{id}' not found" |}) next ctx
                | Some box ->
                    box.Photo |> Option.iter (fun p ->
                        let basePath : string = Path.Combine(config.DataDir, BoxTracker.PhotoPath.value p)
                        let fullPath : string = $"%s{basePath}-full.webp"
                        let thumbPath : string = $"%s{basePath}-thumb.webp"
                        if File.Exists(fullPath) then File.Delete(fullPath)
                        if File.Exists(thumbPath) then File.Delete(thumbPath))
                    let! form : IFormCollection = ctx.Request.ReadFormAsync()
                    let file : IFormFile = form.Files.GetFile("photo")
                    if isNull file then
                        return! (setStatusCode 400 >=> json {| error = "No photo file provided" |}) next ctx
                    else
                        let guid : Guid = Guid.NewGuid()
                        let path : PhotoPath = BoxTracker.PhotoPath.createWebP id guid
                        let basePath : string = Path.Combine(config.DataDir, BoxTracker.PhotoPath.value path)
                        let dir : string = Path.GetDirectoryName(basePath)
                        Directory.CreateDirectory(dir) |> ignore

                        let tempFile : string = Path.Combine(dir, $"temp-%s{Guid.NewGuid().ToString()}")
                        use stream : FileStream = new FileStream(tempFile, FileMode.Create)
                        file.CopyTo(stream)
                        stream.Flush()
                        stream.Close()

                        let result : Result<unit, string> = processUploadedImage tempFile ($"%s{basePath}-full.webp") ($"%s{basePath}-thumb.webp")
                        File.Delete(tempFile)
                        match result with
                        | Error msg ->
                            return! (setStatusCode 400 >=> json {| error = $"Image processing failed: {msg}" |}) next ctx
                        | Ok () ->
                            match storage.UpdateBoxPhoto(id, Some path) with
                            | None ->
                                return! (setStatusCode 404 >=> json {| error = $"Box '%s{id}' not found" |}) next ctx
                            | Some updated ->
                                return! json (boxToDto updated) next ctx
        }

let addItem (boxId: string) : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            if not ctx.Request.HasFormContentType then
                return! (setStatusCode 400 >=> json {| error = "Expected multipart/form-data" |}) next ctx
            else
                let storage : Storage = ctx.GetService<Storage>()
                let config : BoxTrackerConfig = ctx.GetService<BoxTrackerConfig>()
                match storage.GetBox(boxId) with
                | None ->
                    return! (setStatusCode 404 >=> json {| error = $"Box '%s{boxId}' not found" |}) next ctx
                | Some _ ->
                    let! form : IFormCollection = ctx.Request.ReadFormAsync()
                    let name : string = form.["name"].ToString()
                    match BoxTracker.ItemName.create name with
                    | Error msg ->
                        return! (setStatusCode 400 >=> json {| error = msg |}) next ctx
                    | Ok itemName ->
                        let file : IFormFile = form.Files.GetFile("photo")
                        if isNull file then
                            let item : Item = storage.AddItem(boxId, itemName, None)
                            return! (setStatusCode 201 >=> json (itemToDto item)) next ctx
                        else
                            let guid : Guid = Guid.NewGuid()
                            let path : PhotoPath = BoxTracker.PhotoPath.createWebP boxId guid
                            let basePath : string = Path.Combine(config.DataDir, BoxTracker.PhotoPath.value path)
                            let dir : string = Path.GetDirectoryName(basePath)
                            Directory.CreateDirectory(dir) |> ignore

                            let tempFile : string = Path.Combine(dir, $"temp-%s{Guid.NewGuid().ToString()}")
                            use stream : FileStream = new FileStream(tempFile, FileMode.Create)
                            file.CopyTo(stream)
                            stream.Flush()
                            stream.Close()

                            let result : Result<unit, string> = processUploadedImage tempFile ($"%s{basePath}-full.webp") ($"%s{basePath}-thumb.webp")
                            File.Delete(tempFile)
                            match result with
                            | Error msg ->
                                return! (setStatusCode 400 >=> json {| error = $"Image processing failed: {msg}" |}) next ctx
                            | Ok () ->
                                let item : Item = storage.AddItem(boxId, itemName, Some path)
                                return! (setStatusCode 201 >=> json (itemToDto item)) next ctx
        }

let updateItem (boxId: string) (itemId: string) : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let! request : UpdateItemRequest = ctx.BindJsonAsync<UpdateItemRequest>()
            let storage : Storage = ctx.GetService<Storage>()
            match BoxTracker.ItemName.create request.Name with
            | Error msg ->
                return! (setStatusCode 400 >=> json {| error = msg |}) next ctx
            | Ok name ->
                match storage.UpdateItemName(itemId, name) with
                | None ->
                    return! (setStatusCode 404 >=> json {| error = $"Item '%s{itemId}' not found" |}) next ctx
                | Some item ->
                    return! json (itemToDto item) next ctx
        }

let deleteItem (boxId: string) (itemId: string) : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let storage : Storage = ctx.GetService<Storage>()
            let config : BoxTrackerConfig = ctx.GetService<BoxTrackerConfig>()
            let photoPath : string option = storage.DeleteItem(itemId)
            photoPath |> Option.iter (fun p ->
                let basePath : string = Path.Combine(config.DataDir, p)
                let fullPath : string = $"%s{basePath}-full.webp"
                let thumbPath : string = $"%s{basePath}-thumb.webp"
                if File.Exists(fullPath) then File.Delete(fullPath)
                if File.Exists(thumbPath) then File.Delete(thumbPath))
            return! json {| success = true |} next ctx
        }
