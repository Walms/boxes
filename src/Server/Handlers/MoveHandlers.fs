module BoxTracker.Handlers.MoveHandlers

open System
open Giraffe
open Microsoft.AspNetCore.Http
open BoxTracker.Storage
open BoxTracker.Types
open BoxTracker.Dto

let recordMove : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let! request : MoveRequest = ctx.BindJsonAsync<MoveRequest>()
            let storage : Storage = ctx.GetService<Storage>()
            let entityType : string = request.EntityType
            if entityType <> "item" && entityType <> "box" then
                return! (setStatusCode 400 >=> json {| error = "EntityType must be 'item' or 'box'" |}) next ctx
            else
                let toType : string option =
                    if String.IsNullOrEmpty request.ToType then None
                    else Some request.ToType
                let toId : string option =
                    if String.IsNullOrEmpty request.ToId then None
                    else Some request.ToId
                match toType with
                | Some "box" ->
                    match toId with
                    | None ->
                        return! (setStatusCode 400 >=> json {| error = "ToId is required when ToType is 'box'" |}) next ctx
                    | Some boxId ->
                        match storage.GetBox(boxId) with
                        | None ->
                            return! (setStatusCode 404 >=> json {| error = $"Box '%s{boxId}' not found" |}) next ctx
                        | Some _ ->
                            let move : Move = storage.RecordMove(entityType, request.EntityId, toType, toId)
                            return! (setStatusCode 201 >=> json (moveToDto move)) next ctx
                | Some "location" ->
                    match toId with
                    | None ->
                        return! (setStatusCode 400 >=> json {| error = "ToId is required when ToType is 'location'" |}) next ctx
                    | Some code ->
                        match storage.GetLocation(code) with
                        | None ->
                            return! (setStatusCode 404 >=> json {| error = $"Location '%s{code}' not found" |}) next ctx
                        | Some _ ->
                            let move : Move = storage.RecordMove(entityType, request.EntityId, toType, toId)
                            return! (setStatusCode 201 >=> json (moveToDto move)) next ctx
                | _ ->
                    let move : Move = storage.RecordMove(entityType, request.EntityId, None, None)
                    return! (setStatusCode 201 >=> json (moveToDto move)) next ctx
        }

let getMoveHistory : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let storage : Storage = ctx.GetService<Storage>()
            let entityType : string option =
                ctx.TryGetQueryStringValue "entityType" |> Option.map (fun (s: string) -> s)
            let entityId : string option =
                ctx.TryGetQueryStringValue "entityId" |> Option.map (fun (s: string) -> s)
            match entityType, entityId with
            | Some et, Some eid ->
                let moves : Move list = storage.GetMoveHistory(et, eid)
                let dtos : MoveResponse list = moves |> List.map moveToDto
                return! json dtos next ctx
            | _ ->
                return! (setStatusCode 400 >=> json {| error = "entityType and entityId query parameters are required" |}) next ctx
        }
