module BoxTracker.PhotoJobStore

open System
open Microsoft.Data.Sqlite
open BoxTracker.Schema

[<Literal>]
let StatusPending = "pending"

[<Literal>]
let StatusProcessing = "processing"

[<Literal>]
let StatusCompleted = "completed"

[<Literal>]
let StatusFailed = "failed"

/// A durable record of an uploaded photo that still needs to be processed
/// (resized + encoded). Persisting it lets processing continue/resume on the
/// server even if the uploading client disconnects or the server restarts.
type PhotoJob = {
    Id: string
    EntityType: string          // "box" | "item" | "location"
    EntityId: string
    Status: string
    Error: string option
    SourcePath: string          // raw upload, relative to the data dir
    PhotoPath: string           // target base path (no "-full.webp" suffix), relative to the data dir
    OldPhotoPath: string option // previous photo to remove once the new one is ready
    CreatedAt: DateTimeOffset
    UpdatedAt: DateTimeOffset
}

/// Owns a dedicated SQLite connection (separate from the request-path Storage)
/// so the background worker never shares a connection with HTTP handlers.
type PhotoJobStore (connectionString: string) =

    let mutable connection : SqliteConnection option = None
    let gate : obj = obj ()

    let columns : string =
        "id, entity_type, entity_id, status, error, source_path, photo_path, old_photo_path, created_at, updated_at"

    let toDb (value: string option) : obj =
        match value with
        | Some v -> box v
        | None -> box DBNull.Value

    let readJob (r: SqliteDataReader) : PhotoJob =
        {
            Id = r.GetString(0)
            EntityType = r.GetString(1)
            EntityId = r.GetString(2)
            Status = r.GetString(3)
            Error = if r.IsDBNull(4) then None else Some(r.GetString(4))
            SourcePath = r.GetString(5)
            PhotoPath = r.GetString(6)
            OldPhotoPath = if r.IsDBNull(7) then None else Some(r.GetString(7))
            CreatedAt = r.GetString(8) |> DateTimeOffset.Parse
            UpdatedAt = r.GetString(9) |> DateTimeOffset.Parse
        }

    member this.Connect() : unit =
        match connection with
        | Some _ -> ()
        | None ->
            let conn : SqliteConnection = new SqliteConnection(connectionString)
            conn.Open()
            let pragma : SqliteCommand = conn.CreateCommand()
            pragma.CommandText <- "PRAGMA journal_mode=WAL; PRAGMA busy_timeout=5000;"
            pragma.ExecuteNonQuery() |> ignore
            let c : SqliteCommand = conn.CreateCommand()
            c.CommandText <- createTables
            c.ExecuteNonQuery() |> ignore
            connection <- Some conn

    member private this.Connection : SqliteConnection =
        match connection with
        | Some conn -> conn
        | None ->
            this.Connect()
            this.Connection

    member this.CreateJob
        (
            entityType: string,
            entityId: string,
            sourcePath: string,
            photoPath: string,
            oldPhotoPath: string option
        ) : PhotoJob =
        lock gate (fun () ->
            let now : DateTimeOffset = DateTimeOffset.UtcNow
            let id : string = Guid.NewGuid().ToString()
            let conn : SqliteConnection = this.Connection
            use c : SqliteCommand = conn.CreateCommand()
            c.CommandText <-
                $"INSERT INTO photo_job ({columns}) " +
                "VALUES (@id, @et, @eid, @status, NULL, @src, @photo, @old, @created, @updated)"
            c.Parameters.AddWithValue("@id", id) |> ignore
            c.Parameters.AddWithValue("@et", entityType) |> ignore
            c.Parameters.AddWithValue("@eid", entityId) |> ignore
            c.Parameters.AddWithValue("@status", StatusPending) |> ignore
            c.Parameters.AddWithValue("@src", sourcePath) |> ignore
            c.Parameters.AddWithValue("@photo", photoPath) |> ignore
            c.Parameters.AddWithValue("@old", toDb oldPhotoPath) |> ignore
            c.Parameters.AddWithValue("@created", now.ToString("o")) |> ignore
            c.Parameters.AddWithValue("@updated", now.ToString("o")) |> ignore
            c.ExecuteNonQuery() |> ignore
            {
                Id = id
                EntityType = entityType
                EntityId = entityId
                Status = StatusPending
                Error = None
                SourcePath = sourcePath
                PhotoPath = photoPath
                OldPhotoPath = oldPhotoPath
                CreatedAt = now
                UpdatedAt = now
            })

    member this.GetJob(id: string) : PhotoJob option =
        lock gate (fun () ->
            let conn : SqliteConnection = this.Connection
            use c : SqliteCommand = conn.CreateCommand()
            c.CommandText <- $"SELECT {columns} FROM photo_job WHERE id = @id"
            c.Parameters.AddWithValue("@id", id) |> ignore
            use r : SqliteDataReader = c.ExecuteReader()
            if r.Read() then Some(readJob r) else None)

    /// Atomically pick the oldest pending job and mark it as processing.
    member this.ClaimNext() : PhotoJob option =
        lock gate (fun () ->
            let conn : SqliteConnection = this.Connection
            let pending : PhotoJob option =
                use c : SqliteCommand = conn.CreateCommand()
                c.CommandText <-
                    $"SELECT {columns} FROM photo_job WHERE status = @s ORDER BY created_at ASC LIMIT 1"
                c.Parameters.AddWithValue("@s", StatusPending) |> ignore
                use r : SqliteDataReader = c.ExecuteReader()
                if r.Read() then Some(readJob r) else None
            match pending with
            | None -> None
            | Some job ->
                use u : SqliteCommand = conn.CreateCommand()
                u.CommandText <-
                    "UPDATE photo_job SET status = @proc, updated_at = @now WHERE id = @id AND status = @pending"
                u.Parameters.AddWithValue("@proc", StatusProcessing) |> ignore
                u.Parameters.AddWithValue("@now", DateTimeOffset.UtcNow.ToString("o")) |> ignore
                u.Parameters.AddWithValue("@id", job.Id) |> ignore
                u.Parameters.AddWithValue("@pending", StatusPending) |> ignore
                let rows : int = u.ExecuteNonQuery()
                if rows = 1 then Some { job with Status = StatusProcessing } else None)

    member this.MarkCompleted(id: string) : unit =
        lock gate (fun () ->
            let conn : SqliteConnection = this.Connection
            use c : SqliteCommand = conn.CreateCommand()
            c.CommandText <- "UPDATE photo_job SET status = @s, error = NULL, updated_at = @now WHERE id = @id"
            c.Parameters.AddWithValue("@s", StatusCompleted) |> ignore
            c.Parameters.AddWithValue("@now", DateTimeOffset.UtcNow.ToString("o")) |> ignore
            c.Parameters.AddWithValue("@id", id) |> ignore
            c.ExecuteNonQuery() |> ignore)

    member this.MarkFailed(id: string, error: string) : unit =
        lock gate (fun () ->
            let conn : SqliteConnection = this.Connection
            use c : SqliteCommand = conn.CreateCommand()
            c.CommandText <- "UPDATE photo_job SET status = @s, error = @err, updated_at = @now WHERE id = @id"
            c.Parameters.AddWithValue("@s", StatusFailed) |> ignore
            c.Parameters.AddWithValue("@err", error) |> ignore
            c.Parameters.AddWithValue("@now", DateTimeOffset.UtcNow.ToString("o")) |> ignore
            c.Parameters.AddWithValue("@id", id) |> ignore
            c.ExecuteNonQuery() |> ignore)

    /// Point the owning entity at its freshly processed photo.
    member this.SetEntityPhoto(entityType: string, entityId: string, photoPath: string) : unit =
        lock gate (fun () ->
            let conn : SqliteConnection = this.Connection
            let sql : string =
                match entityType with
                | "box" -> "UPDATE box SET photo_path = @p WHERE id = @id"
                | "item" -> "UPDATE item SET photo_path = @p WHERE id = @id"
                | "location" -> "UPDATE location SET photo_path = @p WHERE code = @id"
                | other -> failwith $"Unknown entity type '{other}'"
            use c : SqliteCommand = conn.CreateCommand()
            c.CommandText <- sql
            c.Parameters.AddWithValue("@p", photoPath) |> ignore
            c.Parameters.AddWithValue("@id", entityId) |> ignore
            c.ExecuteNonQuery() |> ignore)

    /// Jobs left in "processing" belong to a previous run that was interrupted;
    /// move them back to "pending" so the worker picks them up again.
    member this.ResetInterrupted() : unit =
        lock gate (fun () ->
            let conn : SqliteConnection = this.Connection
            use c : SqliteCommand = conn.CreateCommand()
            c.CommandText <- "UPDATE photo_job SET status = @pending, updated_at = @now WHERE status = @proc"
            c.Parameters.AddWithValue("@pending", StatusPending) |> ignore
            c.Parameters.AddWithValue("@proc", StatusProcessing) |> ignore
            c.Parameters.AddWithValue("@now", DateTimeOffset.UtcNow.ToString("o")) |> ignore
            c.ExecuteNonQuery() |> ignore)

    interface IDisposable with
        member this.Dispose() : unit =
            connection |> Option.iter (fun (c: SqliteConnection) -> c.Dispose())
            connection <- None
