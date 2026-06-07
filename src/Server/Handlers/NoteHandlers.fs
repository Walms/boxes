module BoxTracker.Handlers.NoteHandlers

open Giraffe
open Microsoft.AspNetCore.Http
open BoxTracker.Storage
open BoxTracker.Types
open BoxTracker.Dto

let listNotes : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let entityType : string = ctx.TryGetQueryStringValue "entityType" |> Option.defaultValue ""
            let entityId : string = ctx.TryGetQueryStringValue "entityId" |> Option.defaultValue ""
            if System.String.IsNullOrWhiteSpace entityType || System.String.IsNullOrWhiteSpace entityId then
                return! (setStatusCode 400 >=> json {| error = "entityType and entityId are required" |}) next ctx
            else
                let storage : Storage = ctx.GetService<Storage>()
                let notes : Note list = storage.ListNotes(entityType, entityId)
                return! json (notes |> List.map noteToDto) next ctx
        }

let createNote : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let! request : CreateNoteRequest = ctx.BindJsonAsync<CreateNoteRequest>()
            let content : string = request.Content.Trim()
            if System.String.IsNullOrWhiteSpace content then
                return! (setStatusCode 400 >=> json {| error = "Note content cannot be empty" |}) next ctx
            elif content.Length > 5000 then
                return! (setStatusCode 400 >=> json {| error = "Note content must be 5000 characters or less" |}) next ctx
            elif System.String.IsNullOrWhiteSpace request.EntityType then
                return! (setStatusCode 400 >=> json {| error = "entityType is required" |}) next ctx
            elif System.String.IsNullOrWhiteSpace request.EntityId then
                return! (setStatusCode 400 >=> json {| error = "entityId is required" |}) next ctx
            else
                let storage : Storage = ctx.GetService<Storage>()
                let note : Note = storage.CreateNote(request.EntityType, request.EntityId, content)
                return! (setStatusCode 201 >=> json (noteToDto note)) next ctx
        }

let updateNote (id: string) : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let! request : UpdateNoteRequest = ctx.BindJsonAsync<UpdateNoteRequest>()
            let content : string = request.Content.Trim()
            if System.String.IsNullOrWhiteSpace content then
                return! (setStatusCode 400 >=> json {| error = "Note content cannot be empty" |}) next ctx
            elif content.Length > 5000 then
                return! (setStatusCode 400 >=> json {| error = "Note content must be 5000 characters or less" |}) next ctx
            else
                let storage : Storage = ctx.GetService<Storage>()
                match storage.UpdateNote(id, content) with
                | None ->
                    return! (setStatusCode 404 >=> json {| error = $"Note '%s{id}' not found" |}) next ctx
                | Some note ->
                    return! json (noteToDto note) next ctx
        }

let deleteNote (id: string) : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let storage : Storage = ctx.GetService<Storage>()
            storage.DeleteNote(id)
            return! setStatusCode 204 next ctx
        }
