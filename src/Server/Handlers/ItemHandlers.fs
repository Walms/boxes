module BoxTracker.Handlers.ItemHandlers

open System
open System.IO
open Giraffe
open Microsoft.AspNetCore.Http
open BoxTracker.Storage
open BoxTracker.Types
open BoxTracker.Dto
open BoxTracker.PhotoPath
open BoxTracker.BoxId
open BoxTracker.Container

let searchItems : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let storage : Storage = ctx.GetService<Storage>()
            let query : string option =
                ctx.TryGetQueryStringValue "q" |> Option.map (fun (s: string) -> s)
            let results : SearchResult list = storage.SearchItems(query)
            let dtos : SearchResultResponse list = results |> List.map searchResultToDto
            return! json dtos next ctx
        }

let createItem : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let! request : CreateItemRequest = ctx.BindJsonAsync<CreateItemRequest>()
            let storage : Storage = ctx.GetService<Storage>()
            match BoxTracker.ItemName.create request.Name with
            | Error msg ->
                return! (setStatusCode 400 >=> json {| error = msg |}) next ctx
            | Ok itemName ->
                let item : BoxTracker.Types.Item = storage.CreateItem(itemName, None)
                if not (String.IsNullOrEmpty request.BoxId) then
                    match storage.GetBox(request.BoxId) with
                    | None ->
                        return! (setStatusCode 404 >=> json {| error = $"Box '%s{request.BoxId}' not found" |}) next ctx
                    | Some _ ->
                        storage.RecordMove("item", item.Id.ToString(), Some "box", Some request.BoxId) |> ignore
                        let updated : BoxTracker.Types.Item option = storage.GetItem(item.Id.ToString())
                        return! (setStatusCode 201 >=> json (itemToDto updated.Value)) next ctx
                else
                    return! (setStatusCode 201 >=> json (itemToDto item)) next ctx
        }

let getItem (id: string) : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let storage : Storage = ctx.GetService<Storage>()
            match storage.GetItem(id) with
            | None ->
                return! (setStatusCode 404 >=> json {| error = $"Item '%s{id}' not found" |}) next ctx
            | Some item ->
                return! json (itemToDto item) next ctx
        }

let updateItemStandalone (id: string) : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let! request : UpdateItemRequest = ctx.BindJsonAsync<UpdateItemRequest>()
            let storage : Storage = ctx.GetService<Storage>()
            match BoxTracker.ItemName.create request.Name with
            | Error msg ->
                return! (setStatusCode 400 >=> json {| error = msg |}) next ctx
            | Ok name ->
                match storage.UpdateItemName(id, name) with
                | None ->
                    return! (setStatusCode 404 >=> json {| error = $"Item '%s{id}' not found" |}) next ctx
                | Some item ->
                    return! json (itemToDto item) next ctx
        }

let deleteItemStandalone (id: string) : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let storage : Storage = ctx.GetService<Storage>()
            let config : BoxTrackerConfig = ctx.GetService<BoxTrackerConfig>()
            let photoPath : string option = storage.DeleteItem(id)
            photoPath |> Option.iter (fun p ->
                let fullPath : string = Path.Combine(config.DataDir, p)
                if File.Exists(fullPath) then File.Delete(fullPath))
            return! json {| success = true |} next ctx
        }

let updateItemPhoto (id: string) : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            if not ctx.Request.HasFormContentType then
                return! (setStatusCode 400 >=> json {| error = "Expected multipart/form-data" |}) next ctx
            else
                let storage : Storage = ctx.GetService<Storage>()
                let config : BoxTrackerConfig = ctx.GetService<BoxTrackerConfig>()
                match storage.GetItem(id) with
                | None ->
                    return! (setStatusCode 404 >=> json {| error = $"Item '%s{id}' not found" |}) next ctx
                | Some item ->
                    item.Photo |> Option.iter (fun p ->
                        let fullPath : string = Path.Combine(config.DataDir, BoxTracker.PhotoPath.value p)
                        if File.Exists(fullPath) then File.Delete(fullPath))
                    let! form : IFormCollection = ctx.Request.ReadFormAsync()
                    let file : IFormFile = form.Files.GetFile("photo")
                    let photoPath : PhotoPath option =
                        if isNull file then None
                        else
                            let guid : Guid = Guid.NewGuid()
                            let ext : string =
                                let raw : string = Path.GetExtension(file.FileName)
                                raw.TrimStart('.').ToLowerInvariant()
                            let folder : string =
                                match item.Placement with
                                | InBox boxId -> BoxTracker.BoxId.value boxId
                                | _ -> id
                            let path : PhotoPath = BoxTracker.PhotoPath.create folder guid ext
                            let fullPath : string = Path.Combine(config.DataDir, BoxTracker.PhotoPath.value path)
                            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)) |> ignore
                            use stream : FileStream = new FileStream(fullPath, FileMode.Create)
                            file.CopyTo(stream)
                            Some path
                    match storage.UpdateItemPhoto(id, photoPath) with
                    | None ->
                        return! (setStatusCode 404 >=> json {| error = $"Item '%s{id}' not found" |}) next ctx
                    | Some updated ->
                        return! json (itemToDto updated) next ctx
        }
