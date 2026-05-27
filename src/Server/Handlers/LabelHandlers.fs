module BoxTracker.Handlers.LabelHandlers

open Giraffe
open Microsoft.AspNetCore.Http
open BoxTracker.Storage
open BoxTracker.Dto
open BoxTracker.Labels

let boxLabel (id: string) : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let storage : Storage = ctx.GetService<Storage>()
            match storage.GetBox(id) with
            | None ->
                return! (setStatusCode 404 >=> json {| error = $"Box '%s{id}' not found" |}) next ctx
            | Some box ->
                let dto : BoxResponse = boxToDto box
                let locationName : string option =
                    dto.LocationCode |> Option.bind (fun code ->
                        match storage.GetLocation(code) with
                        | Some loc -> Some(BoxTracker.LocationName.value loc.Name)
                        | None -> None)
                let html : string = boxLabelHtml dto.Id dto.Label dto.LocationCode locationName
                return! htmlString html next ctx
        }

let locationLabel (code: string) : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let storage : Storage = ctx.GetService<Storage>()
            match storage.GetLocation(code) with
            | None ->
                return! (setStatusCode 404 >=> json {| error = $"Location '%s{code}' not found" |}) next ctx
            | Some loc ->
                let dto : LocationResponse = locationToDto loc
                let html : string = locationLabelHtml dto.Code dto.Name
                return! htmlString html next ctx
        }

let batchBoxLabels : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let idsParam : string option = ctx.TryGetQueryStringValue "ids"
            match idsParam with
            | None ->
                return! (setStatusCode 400 >=> json {| error = "Query parameter 'ids' is required" |}) next ctx
            | Some idsStr ->
                let storage : Storage = ctx.GetService<Storage>()
                let ids : string list =
                    idsStr.Split(',')
                    |> Array.map (fun (s: string) -> s.Trim().ToUpperInvariant())
                    |> Array.filter (fun (s: string) -> s.Length > 0)
                    |> List.ofArray
                let boxes : (string * string option * string option * string option) list =
                    ids
                    |> List.choose (fun (boxId: string) ->
                        match storage.GetBox(boxId) with
                        | Some box ->
                            let dto : BoxResponse = boxToDto box
                            let locationName : string option =
                                dto.LocationCode |> Option.bind (fun code ->
                                    match storage.GetLocation(code) with
                                    | Some loc -> Some(BoxTracker.LocationName.value loc.Name)
                                    | None -> None)
                            Some(dto.Id, dto.Label, dto.LocationCode, locationName)
                        | None -> None)
                match boxes with
                | [] ->
                    return! (setStatusCode 404 >=> json {| error = "No matching boxes found" |}) next ctx
                | _ ->
                    let html : string = batchBoxLabelHtml boxes
                    return! htmlString html next ctx
        }
