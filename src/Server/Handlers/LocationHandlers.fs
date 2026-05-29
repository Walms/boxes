module BoxTracker.Handlers.LocationHandlers

open System
open System.IO
open Giraffe
open Microsoft.AspNetCore.Http
open BoxTracker.Storage
open BoxTracker.Types
open BoxTracker.Location
open BoxTracker.PhotoPath
open BoxTracker.Dto
open BoxTracker.ImageProcessing

let listLocations : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let storage : Storage = ctx.GetService<Storage>()
            let includeArchived : bool =
                match ctx.TryGetQueryStringValue "includeArchived" with
                | Some "true" -> true
                | _ -> false
            let locations : Location list = storage.ListLocations(includeArchived)
            let dtos : LocationResponse list = locations |> List.map locationToDto
            return! json dtos next ctx
        }

let getLocation (code: string) : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let storage : Storage = ctx.GetService<Storage>()
            match storage.GetLocation(code) with
            | None ->
                return! (setStatusCode 404 >=> json {| error = $"Location '%s{code}' not found" |}) next ctx
            | Some location ->
                let boxes : Box list = storage.ListBoxes(Some code, false)
                let response : LocationDetailResponse = {
                    Location = locationToDto location
                    Boxes = boxes |> List.map boxToDto
                }
                return! json response next ctx
        }

let createLocation : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let! request : CreateLocationRequest = ctx.BindJsonAsync<CreateLocationRequest>()
            let storage : Storage = ctx.GetService<Storage>()
            match BoxTracker.LocationCode.create request.Code with
            | Error msg ->
                return! (setStatusCode 400 >=> json {| error = msg |}) next ctx
            | Ok code ->
                match BoxTracker.LocationName.create request.Name with
                | Error msg ->
                    return! (setStatusCode 400 >=> json {| error = msg |}) next ctx
                | Ok name ->
                    let location : Location = storage.CreateLocation(code, name)
                    return! (setStatusCode 201 >=> json (locationToDto location)) next ctx
        }

let updateLocation (code: string) : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let! request : UpdateLocationRequest = ctx.BindJsonAsync<UpdateLocationRequest>()
            let storage : Storage = ctx.GetService<Storage>()
            match BoxTracker.LocationName.create request.Name with
            | Error msg ->
                return! (setStatusCode 400 >=> json {| error = msg |}) next ctx
            | Ok name ->
                match storage.UpdateLocationName(code, name) with
                | None ->
                    return! (setStatusCode 404 >=> json {| error = $"Location '%s{code}' not found" |}) next ctx
                | Some location ->
                    return! json (locationToDto location) next ctx
        }

let uploadLocationPhoto (code: string) : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            if not ctx.Request.HasFormContentType then
                return! (setStatusCode 400 >=> json {| error = "Expected multipart/form-data" |}) next ctx
            else
                let storage : Storage = ctx.GetService<Storage>()
                let config : BoxTrackerConfig = ctx.GetService<BoxTrackerConfig>()
                match storage.GetLocation(code) with
                | None ->
                    return! (setStatusCode 404 >=> json {| error = $"Location '%s{code}' not found" |}) next ctx
                | Some location ->
                    location.Photo |> Option.iter (fun p ->
                        let basePath : string = Path.Combine(config.DataDir, BoxTracker.PhotoPath.value p)
                        let fullVariant : string = $"%s{basePath}-full.webp"
                        let thumbVariant : string = $"%s{basePath}-thumb.webp"
                        if File.Exists(fullVariant) then File.Delete(fullVariant)
                        if File.Exists(thumbVariant) then File.Delete(thumbVariant)
                        if File.Exists(basePath) then File.Delete(basePath))
                    let! form : IFormCollection = ctx.Request.ReadFormAsync()
                    let file : IFormFile = form.Files.GetFile("photo")
                    if isNull file then
                        return! (setStatusCode 400 >=> json {| error = "No photo file provided" |}) next ctx
                    else
                        let guid : Guid = Guid.NewGuid()
                        let path : PhotoPath = BoxTracker.PhotoPath.createWebP $"location-%s{code}" guid
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
                            match storage.UpdateLocationPhoto(code, Some path) with
                            | None ->
                                return! (setStatusCode 404 >=> json {| error = $"Location '%s{code}' not found" |}) next ctx
                            | Some updated ->
                                return! json (locationToDto updated) next ctx
        }

let archiveLocation (code: string) : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let storage : Storage = ctx.GetService<Storage>()
            match storage.GetLocation(code) with
            | None ->
                return! (setStatusCode 404 >=> json {| error = $"Location '%s{code}' not found" |}) next ctx
            | Some location ->
                let boxCount : int = storage.GetAssignedBoxCount(code)
                match BoxTracker.Location.tryMakeEmpty location boxCount with
                | Error msg ->
                    return! (setStatusCode 400 >=> json {| error = msg |}) next ctx
                | Ok _ ->
                    storage.SetLocationArchived(code)
                    let archived : Location = { location with IsArchived = true }
                    return! json (locationToDto archived) next ctx
        }
