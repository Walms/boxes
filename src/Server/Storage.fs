module BoxTracker.Storage

open System
open Microsoft.Data.Sqlite
open BoxTracker.Schema
open BoxTracker.Types
open BoxTracker.LocationCode
open BoxTracker.LocationName
open BoxTracker.BoxId
open BoxTracker.BoxLabel
open BoxTracker.ItemName
open BoxTracker.PhotoPath
open BoxTracker.Container

type SearchResult = {
    ItemId: Guid
    ItemName: string
    PhotoPath: string option
    BoxId: string
    BoxLabel: string option
    LocationCode: string option
    LocationName: string option
    AddedAt: DateTimeOffset
}

type Storage (connectionString: string) =

    let mutable connection : SqliteConnection option = None
    let mutable activeTransaction : SqliteTransaction option = None

    let unwrap (result: Result<'T, string>) : 'T =
        match result with
        | Ok v -> v
        | Error e -> failwith e

    let toDb (value: string option) : obj =
        match value with
        | Some v -> box v
        | None -> DBNull.Value

    let readLocation (reader: SqliteDataReader) : Location =
        let code : LocationCode = reader.GetString(0) |> LocationCode.tryParse |> unwrap
        let name : LocationName = reader.GetString(1) |> LocationName.create |> unwrap
        let isArchived : bool = reader.GetInt32(2) = 1
        let photo : PhotoPath option =
            if reader.IsDBNull(3) then None
            else reader.GetString(3) |> PhotoPath.tryParse |> unwrap |> Some
        let createdAt : DateTimeOffset = reader.GetString(4) |> DateTimeOffset.Parse
        { Code = code; Name = name; IsArchived = isArchived; Photo = photo; CreatedAt = createdAt }

    let readContainer (reader: SqliteDataReader) (toTypeIdx: int) (toIdIdx: int) : Container =
        if reader.IsDBNull(toTypeIdx) then Unassigned
        else
            match reader.GetString(toTypeIdx) with
            | "box" -> reader.GetString(toIdIdx) |> BoxId.tryParse |> unwrap |> InBox
            | "location" -> reader.GetString(toIdIdx) |> LocationCode.tryParse |> unwrap |> AtLocation
            | _ -> Unassigned

    let readBox (reader: SqliteDataReader) (offset: int) : Box =
        let id : BoxId = reader.GetString(offset) |> BoxId.tryParse |> unwrap
        let label : BoxLabel option =
            if reader.IsDBNull(offset + 1) then None
            else
                match BoxLabel.create (reader.GetString(offset + 1)) with
                | Ok l -> l
                | Error e -> failwith e
        let photo : PhotoPath option =
            if reader.IsDBNull(offset + 2) then None
            else reader.GetString(offset + 2) |> PhotoPath.tryParse |> unwrap |> Some
        let createdAt : DateTimeOffset = reader.GetString(offset + 3) |> DateTimeOffset.Parse
        let placement : Container = readContainer reader (offset + 4) (offset + 5)
        { Id = id; Label = label; Photo = photo; Placement = placement; CreatedAt = createdAt }

    let readItem (reader: SqliteDataReader) (offset: int) : Item =
        let id : Guid = Guid.Parse(reader.GetString(offset))
        let name : ItemName = reader.GetString(offset + 1) |> ItemName.create |> unwrap
        let photo : PhotoPath option =
            if reader.IsDBNull(offset + 2) then None
            else reader.GetString(offset + 2) |> PhotoPath.tryParse |> unwrap |> Some
        let addedAt : DateTimeOffset = reader.GetString(offset + 3) |> DateTimeOffset.Parse
        let placement : Container = readContainer reader (offset + 4) (offset + 5)
        { Id = id; Name = name; Photo = photo; Placement = placement; AddedAt = addedAt }

    let readMove (reader: SqliteDataReader) : Move =
        let id : Guid = Guid.Parse(reader.GetString(0))
        let entityType : string = reader.GetString(1)
        let entityId : string = reader.GetString(2)
        let toContainer : Container = readContainer reader 3 4
        let movedAt : DateTimeOffset = reader.GetString(5) |> DateTimeOffset.Parse
        { Id = id; EntityType = entityType; EntityId = entityId; To = toContainer; MovedAt = movedAt }

    let readSearchResult (reader: SqliteDataReader) : SearchResult =
        let itemId : Guid = Guid.Parse(reader.GetString(0))
        let itemName : string = reader.GetString(1)
        let photoPath : string option =
            if reader.IsDBNull(2) then None
            else Some(reader.GetString(2))
        let boxId : string =
            if reader.IsDBNull(3) then ""
            else reader.GetString(3)
        let boxLabel : string option =
            if reader.IsDBNull(4) then None
            else Some(reader.GetString(4))
        let locationCode : string option =
            if reader.IsDBNull(5) then None
            else Some(reader.GetString(5))
        let locationName : string option =
            if reader.IsDBNull(6) then None
            else Some(reader.GetString(6))
        let addedAt : DateTimeOffset = reader.GetDateTimeOffset(7)
        { ItemId = itemId; ItemName = itemName; PhotoPath = photoPath
          BoxId = boxId; BoxLabel = boxLabel
          LocationCode = locationCode; LocationName = locationName
          AddedAt = addedAt }

    let readList (read: SqliteDataReader -> 'T) (reader: SqliteDataReader) : 'T list =
        let results : ResizeArray<'T> = ResizeArray()
        while reader.Read() do
            results.Add(read reader)
        results |> List.ofSeq

    /// Create the schema and enable WAL once at startup. WAL is a persistent
    /// database property, so request connections and the background photo worker
    /// all inherit it without each having to set it.
    static member InitializeSchema(connectionString: string) : unit =
        use conn : SqliteConnection = new SqliteConnection(connectionString)
        conn.Open()
        let pragma : SqliteCommand = conn.CreateCommand()
        pragma.CommandText <- "PRAGMA journal_mode=WAL;"
        pragma.ExecuteNonQuery() |> ignore
        let c : SqliteCommand = conn.CreateCommand()
        c.CommandText <- createTables
        c.ExecuteNonQuery() |> ignore
        // Run migration for box.photo_path (safe to run multiple times)
        try
            let m : SqliteCommand = conn.CreateCommand()
            m.CommandText <- "ALTER TABLE box ADD COLUMN photo_path TEXT"
            m.ExecuteNonQuery() |> ignore
        with _ -> ()
        // Run migration for location.photo_path (safe to run multiple times)
        try
            let m : SqliteCommand = conn.CreateCommand()
            m.CommandText <- "ALTER TABLE location ADD COLUMN photo_path TEXT"
            m.ExecuteNonQuery() |> ignore
        with _ -> ()
        // Run migration for note table (safe to run multiple times)
        try
            let m : SqliteCommand = conn.CreateCommand()
            m.CommandText <- "CREATE TABLE IF NOT EXISTS note (id TEXT PRIMARY KEY, entity_type TEXT NOT NULL, entity_id TEXT NOT NULL, content TEXT NOT NULL, created_at TEXT NOT NULL, updated_at TEXT NOT NULL)"
            m.ExecuteNonQuery() |> ignore
        with _ -> ()
        try
            let m : SqliteCommand = conn.CreateCommand()
            m.CommandText <- "CREATE INDEX IF NOT EXISTS idx_note_entity ON note (entity_type, entity_id, created_at DESC)"
            m.ExecuteNonQuery() |> ignore
        with _ -> ()

    member this.Connect() : unit =
        match connection with
        | Some _ -> ()
        | None ->
            let conn : SqliteConnection = new SqliteConnection(connectionString)
            conn.Open()
            // Each Storage instance is request-scoped and holds its own pooled
            // connection. The busy timeout lets a writer wait briefly for the
            // write lock instead of failing when another connection holds it.
            let pragma : SqliteCommand = conn.CreateCommand()
            pragma.CommandText <- "PRAGMA busy_timeout=5000;"
            pragma.ExecuteNonQuery() |> ignore
            connection <- Some conn

    member this.Connection : SqliteConnection =
        match connection with
        | Some conn -> conn
        | None ->
            this.Connect()
            this.Connection

    interface IDisposable with
        member this.Dispose() : unit =
            connection |> Option.iter (fun (c: SqliteConnection) -> c.Dispose())
            connection <- None

    member private this.CreateCommand() : SqliteCommand =
        let c : SqliteCommand = this.Connection.CreateCommand()
        activeTransaction |> Option.iter (fun (t: SqliteTransaction) -> c.Transaction <- t)
        c

    /// Run work inside a single transaction so multi-statement write paths
    /// commit (and fsync) once instead of per statement. Nested calls join
    /// the ambient transaction; disposal without commit rolls back on error.
    member private this.InTransaction(work: unit -> 'T) : 'T =
        match activeTransaction with
        | Some _ -> work ()
        | None ->
            use txn : SqliteTransaction = this.Connection.BeginTransaction()
            activeTransaction <- Some txn
            try
                let result : 'T = work ()
                txn.Commit()
                result
            finally
                activeTransaction <- None

    member private this.GetBoxPlacement(boxId: string) : Container =
        use c : SqliteCommand = this.CreateCommand()
        c.CommandText <- """
            SELECT to_type, to_id FROM move
            WHERE entity_type = 'box' AND entity_id = @entityId
            ORDER BY moved_at DESC LIMIT 1
        """
        c.Parameters.AddWithValue("@entityId", boxId) |> ignore
        use reader : SqliteDataReader = c.ExecuteReader()
        if reader.Read() then readContainer reader 0 1
        else Unassigned

    member private this.GetItemPlacement(itemId: string) : Container =
        use c : SqliteCommand = this.CreateCommand()
        c.CommandText <- """
            SELECT to_type, to_id FROM move
            WHERE entity_type = 'item' AND entity_id = @entityId
            ORDER BY moved_at DESC LIMIT 1
        """
        c.Parameters.AddWithValue("@entityId", itemId) |> ignore
        use reader : SqliteDataReader = c.ExecuteReader()
        if reader.Read() then readContainer reader 0 1
        else Unassigned

    member private this.SyncItemToSearch(itemId: string, itemName: string) : unit =
        let placement : Container = this.GetItemPlacement(itemId)
        let boxLabel : string option =
            match placement with
            | InBox boxId ->
                use c : SqliteCommand = this.CreateCommand()
                c.CommandText <- "SELECT label FROM box WHERE id = @id"
                c.Parameters.AddWithValue("@id", BoxId.value boxId) |> ignore
                let r : obj = c.ExecuteScalar()
                match r with
                | :? string as l -> Some l
                | _ -> None
            | _ -> None
        let locationName : string option =
            match placement with
            | InBox boxId ->
                match this.GetBoxPlacement(BoxId.value boxId) with
                | AtLocation locCode ->
                    use c : SqliteCommand = this.CreateCommand()
                    c.CommandText <- "SELECT name FROM location WHERE code = @code"
                    c.Parameters.AddWithValue("@code", LocationCode.value locCode) |> ignore
                    let r : obj = c.ExecuteScalar()
                    match r with
                    | :? string as n -> Some n
                    | _ -> None
                | _ -> None
            | _ -> None
        use ins : SqliteCommand = this.CreateCommand()
        ins.CommandText <- "INSERT INTO item_search (item_id, item_name, box_label, location_name) VALUES (@itemId, @itemName, @boxLabel, @locationName)"
        ins.Parameters.AddWithValue("@itemId", itemId) |> ignore
        ins.Parameters.AddWithValue("@itemName", itemName) |> ignore
        ins.Parameters.AddWithValue("@boxLabel", toDb boxLabel) |> ignore
        ins.Parameters.AddWithValue("@locationName", toDb locationName) |> ignore
        ins.ExecuteNonQuery() |> ignore

    member private this.RemoveFromSearch(itemId: string) : unit =
        use c : SqliteCommand = this.CreateCommand()
        c.CommandText <- "DELETE FROM item_search WHERE item_id = @itemId"
        c.Parameters.AddWithValue("@itemId", itemId) |> ignore
        c.ExecuteNonQuery() |> ignore

    member private this.ReindexBoxItems(boxId: string) : unit =
        let items : (string * string) list =
            use c : SqliteCommand = this.CreateCommand()
            c.CommandText <- """
                SELECT i.id, i.name FROM item i
                INNER JOIN (
                    SELECT entity_id, to_type, to_id
                    FROM (
                        SELECT entity_id, to_type, to_id,
                               ROW_NUMBER() OVER (PARTITION BY entity_id ORDER BY moved_at DESC) as rn
                        FROM move WHERE entity_type = 'item'
                    )
                    WHERE rn = 1
                ) lp ON lp.entity_id = i.id
                WHERE lp.to_type = 'box' AND lp.to_id = @boxId
            """
            c.Parameters.AddWithValue("@boxId", boxId) |> ignore
            use reader : SqliteDataReader = c.ExecuteReader()
            readList (fun (r: SqliteDataReader) -> r.GetString(0), r.GetString(1)) reader
        for (iid, name) in items do
            this.RemoveFromSearch(iid)
            this.SyncItemToSearch(iid, name)

    member private this.ReindexLocationItems(code: string) : unit =
        let items : (string * string) list =
            use c : SqliteCommand = this.CreateCommand()
            c.CommandText <- """
                SELECT i.id, i.name FROM item i
                INNER JOIN (
                    SELECT entity_id, to_type, to_id
                    FROM (
                        SELECT entity_id, to_type, to_id,
                               ROW_NUMBER() OVER (PARTITION BY entity_id ORDER BY moved_at DESC) as rn
                        FROM move WHERE entity_type = 'item'
                    )
                    WHERE rn = 1 AND to_type = 'box'
                ) item_lp ON item_lp.entity_id = i.id
                INNER JOIN (
                    SELECT entity_id, to_type, to_id
                    FROM (
                        SELECT entity_id, to_type, to_id,
                               ROW_NUMBER() OVER (PARTITION BY entity_id ORDER BY moved_at DESC) as rn
                        FROM move WHERE entity_type = 'box'
                    )
                    WHERE rn = 1 AND to_type = 'location'
                ) box_lp ON box_lp.entity_id = item_lp.to_id
                WHERE box_lp.to_id = @code
            """
            c.Parameters.AddWithValue("@code", code) |> ignore
            use reader : SqliteDataReader = c.ExecuteReader()
            readList (fun (r: SqliteDataReader) -> r.GetString(0), r.GetString(1)) reader
        for (iid, name) in items do
            this.RemoveFromSearch(iid)
            this.SyncItemToSearch(iid, name)

    member this.ListLocations(includeArchived: bool) : Location list =
        use c : SqliteCommand = this.CreateCommand()
        if includeArchived then
            c.CommandText <- "SELECT code, name, is_archived, photo_path, created_at FROM location ORDER BY name"
        else
            c.CommandText <- "SELECT code, name, is_archived, photo_path, created_at FROM location WHERE is_archived = 0 ORDER BY name"
        use reader : SqliteDataReader = c.ExecuteReader()
        readList readLocation reader

    member this.GetLocation(code: string) : Location option =
        use c : SqliteCommand = this.CreateCommand()
        c.CommandText <- "SELECT code, name, is_archived, photo_path, created_at FROM location WHERE code = @code"
        c.Parameters.AddWithValue("@code", code) |> ignore
        use reader : SqliteDataReader = c.ExecuteReader()
        if reader.Read() then Some(readLocation reader)
        else None

    member this.CreateLocation(code: LocationCode, name: LocationName) : Location =
        let now : DateTimeOffset = DateTimeOffset.UtcNow
        use c : SqliteCommand = this.CreateCommand()
        c.CommandText <- "INSERT INTO location (code, name, is_archived, created_at) VALUES (@code, @name, 0, @createdAt)"
        c.Parameters.AddWithValue("@code", LocationCode.value code) |> ignore
        c.Parameters.AddWithValue("@name", LocationName.value name) |> ignore
        c.Parameters.AddWithValue("@createdAt", now.ToString("o")) |> ignore
        c.ExecuteNonQuery() |> ignore
        { Code = code; Name = name; IsArchived = false; Photo = None; CreatedAt = now }

    member this.UpdateLocationName(code: string, name: LocationName) : Location option =
        this.InTransaction(fun () ->
            use c : SqliteCommand = this.CreateCommand()
            c.CommandText <- "UPDATE location SET name = @name WHERE code = @code"
            c.Parameters.AddWithValue("@name", LocationName.value name) |> ignore
            c.Parameters.AddWithValue("@code", code) |> ignore
            let rows : int = c.ExecuteNonQuery()
            if rows = 0 then None
            else
                this.ReindexLocationItems(code)
                this.GetLocation(code))

    member this.UpdateLocationCode(oldCode: string, newCode: LocationCode) : Result<Location, string> =
        let newCodeStr : string = LocationCode.value newCode
        match this.GetLocation(newCodeStr) with
        | Some _ -> Error $"Location code '%s{newCodeStr}' is already in use"
        | None ->
            try
                this.InTransaction(fun () ->
                    use c1 : SqliteCommand = this.CreateCommand()
                    c1.CommandText <- "UPDATE location SET code = @newCode WHERE code = @oldCode"
                    c1.Parameters.AddWithValue("@newCode", newCodeStr) |> ignore
                    c1.Parameters.AddWithValue("@oldCode", oldCode) |> ignore
                    let rows : int = c1.ExecuteNonQuery()
                    if rows = 0 then Error $"Location '%s{oldCode}' not found"
                    else
                        use c2 : SqliteCommand = this.CreateCommand()
                        c2.CommandText <- "UPDATE move SET to_id = @newCode WHERE to_type = 'location' AND to_id = @oldCode"
                        c2.Parameters.AddWithValue("@newCode", newCodeStr) |> ignore
                        c2.Parameters.AddWithValue("@oldCode", oldCode) |> ignore
                        c2.ExecuteNonQuery() |> ignore
                        use c3 : SqliteCommand = this.CreateCommand()
                        c3.CommandText <- "UPDATE photo_job SET entity_id = @newCode WHERE entity_type = 'location' AND entity_id = @oldCode"
                        c3.Parameters.AddWithValue("@newCode", newCodeStr) |> ignore
                        c3.Parameters.AddWithValue("@oldCode", oldCode) |> ignore
                        c3.ExecuteNonQuery() |> ignore
                        this.ReindexLocationItems(newCodeStr)
                        match this.GetLocation(newCodeStr) with
                        | Some loc -> Ok loc
                        | None -> Error "Failed to fetch updated location")
            with ex ->
                Error ex.Message

    member this.GetAssignedBoxCount(code: string) : int =
        use c : SqliteCommand = this.CreateCommand()
        c.CommandText <- """
            SELECT COUNT(*) FROM box b
            INNER JOIN (
                SELECT entity_id, to_type, to_id
                FROM (
                    SELECT entity_id, to_type, to_id,
                           ROW_NUMBER() OVER (PARTITION BY entity_id ORDER BY moved_at DESC) as rn
                    FROM move WHERE entity_type = 'box'
                )
                WHERE rn = 1
            ) lp ON lp.entity_id = b.id
            WHERE lp.to_type = 'location' AND lp.to_id = @code
        """
        c.Parameters.AddWithValue("@code", code) |> ignore
        let result : obj = c.ExecuteScalar()
        match result with
        | :? int64 as n -> int n
        | _ -> 0

    member this.SetLocationArchived(code: string) : unit =
        use c : SqliteCommand = this.CreateCommand()
        c.CommandText <- "UPDATE location SET is_archived = 1 WHERE code = @code"
        c.Parameters.AddWithValue("@code", code) |> ignore
        c.ExecuteNonQuery() |> ignore

    member this.UpdateLocationPhoto(code: string, photoPath: PhotoPath option) : Location option =
        use c : SqliteCommand = this.CreateCommand()
        let photoValue : obj =
            match photoPath with
            | Some p -> box (PhotoPath.value p)
            | None -> DBNull.Value
        c.CommandText <- "UPDATE location SET photo_path = @photoPath WHERE code = @code"
        c.Parameters.AddWithValue("@code", code) |> ignore
        c.Parameters.AddWithValue("@photoPath", photoValue) |> ignore
        let rows : int = c.ExecuteNonQuery()
        if rows = 0 then None else this.GetLocation(code)

    member this.ListBoxes(locationCode: string option, unassigned: bool) : Box list =
        use c : SqliteCommand = this.CreateCommand()
        match locationCode, unassigned with
        | Some loc, _ ->
            c.CommandText <- """
                SELECT b.id, b.label, b.photo_path, b.created_at, lp.to_type, lp.to_id
                FROM box b
                INNER JOIN (
                    SELECT entity_id, to_type, to_id
                    FROM (
                        SELECT entity_id, to_type, to_id,
                               ROW_NUMBER() OVER (PARTITION BY entity_id ORDER BY moved_at DESC) as rn
                        FROM move WHERE entity_type = 'box'
                    )
                    WHERE rn = 1
                ) lp ON lp.entity_id = b.id
                WHERE lp.to_type = 'location' AND lp.to_id = @locationCode
                ORDER BY b.id
            """
            c.Parameters.AddWithValue("@locationCode", loc) |> ignore
        | None, true ->
            c.CommandText <- """
                SELECT b.id, b.label, b.photo_path, b.created_at, lp.to_type, lp.to_id
                FROM box b
                LEFT JOIN (
                    SELECT entity_id, to_type, to_id
                    FROM (
                        SELECT entity_id, to_type, to_id,
                               ROW_NUMBER() OVER (PARTITION BY entity_id ORDER BY moved_at DESC) as rn
                        FROM move WHERE entity_type = 'box'
                    )
                    WHERE rn = 1
                ) lp ON lp.entity_id = b.id
                WHERE lp.to_type IS NULL
                ORDER BY b.id
            """
        | None, false ->
            c.CommandText <- """
                SELECT b.id, b.label, b.photo_path, b.created_at, lp.to_type, lp.to_id
                FROM box b
                LEFT JOIN (
                    SELECT entity_id, to_type, to_id
                    FROM (
                        SELECT entity_id, to_type, to_id,
                               ROW_NUMBER() OVER (PARTITION BY entity_id ORDER BY moved_at DESC) as rn
                        FROM move WHERE entity_type = 'box'
                    )
                    WHERE rn = 1
                ) lp ON lp.entity_id = b.id
                ORDER BY b.id
            """
        use reader : SqliteDataReader = c.ExecuteReader()
        readList (fun (r: SqliteDataReader) -> readBox r 0) reader

    member this.GetBox(id: string) : Box option =
        use c : SqliteCommand = this.CreateCommand()
        c.CommandText <- """
            SELECT b.id, b.label, b.photo_path, b.created_at, lp.to_type, lp.to_id
            FROM box b
            LEFT JOIN (
                SELECT entity_id, to_type, to_id
                FROM (
                    SELECT entity_id, to_type, to_id,
                           ROW_NUMBER() OVER (PARTITION BY entity_id ORDER BY moved_at DESC) as rn
                    FROM move WHERE entity_type = 'box'
                )
                WHERE rn = 1
            ) lp ON lp.entity_id = b.id
            WHERE b.id = @id
        """
        c.Parameters.AddWithValue("@id", id) |> ignore
        use reader : SqliteDataReader = c.ExecuteReader()
        if reader.Read() then Some(readBox reader 0)
        else None

    member this.CreateBox(label: BoxLabel option) : Box =
        let nextSeq : int =
            use c : SqliteCommand = this.CreateCommand()
            c.CommandText <- "SELECT MAX(id) FROM box"
            let result : obj = c.ExecuteScalar()
            match result with
            | :? string as maxId -> BoxId.tryParse maxId |> unwrap |> BoxId.extractSequence |> (+) 1
            | _ -> 1
        let boxId : BoxId = BoxId.create nextSeq
        let id : string = BoxId.value boxId
        let labelValue : obj =
            match label with
            | Some l -> box (BoxLabel.value l)
            | None -> DBNull.Value
        let now : DateTimeOffset = DateTimeOffset.UtcNow
        use c : SqliteCommand = this.CreateCommand()
        c.CommandText <- "INSERT INTO box (id, label, created_at) VALUES (@id, @label, @createdAt)"
        c.Parameters.AddWithValue("@id", id) |> ignore
        c.Parameters.AddWithValue("@label", labelValue) |> ignore
        c.Parameters.AddWithValue("@createdAt", now.ToString("o")) |> ignore
        c.ExecuteNonQuery() |> ignore
        { Id = boxId; Label = label; Photo = None; Placement = Unassigned; CreatedAt = now }

    member this.UpdateBox(id: string, label: BoxLabel option) : Box option =
        this.InTransaction(fun () ->
            let labelValue : obj =
                match label with
                | Some l -> box (BoxLabel.value l)
                | None -> DBNull.Value
            use c : SqliteCommand = this.CreateCommand()
            c.CommandText <- "UPDATE box SET label = @label WHERE id = @id"
            c.Parameters.AddWithValue("@id", id) |> ignore
            c.Parameters.AddWithValue("@label", labelValue) |> ignore
            let rows : int = c.ExecuteNonQuery()
            if rows = 0 then None
            else
                this.ReindexBoxItems(id)
                this.GetBox(id))

    member this.DeleteBox(id: string) : string list =
        this.InTransaction(fun () ->
            let boxPhotoPath : string option =
                use c : SqliteCommand = this.CreateCommand()
                c.CommandText <- "SELECT photo_path FROM box WHERE id = @id AND photo_path IS NOT NULL"
                c.Parameters.AddWithValue("@id", id) |> ignore
                let r : obj = c.ExecuteScalar()
                match r with
                | :? string as p -> Some p
                | _ -> None
            let itemIds : string list =
                use c : SqliteCommand = this.CreateCommand()
                c.CommandText <- """
                    SELECT i.id FROM item i
                    INNER JOIN (
                        SELECT entity_id, to_type, to_id
                        FROM (
                            SELECT entity_id, to_type, to_id,
                                   ROW_NUMBER() OVER (PARTITION BY entity_id ORDER BY moved_at DESC) as rn
                            FROM move WHERE entity_type = 'item'
                        )
                        WHERE rn = 1
                    ) lp ON lp.entity_id = i.id
                    WHERE lp.to_type = 'box' AND lp.to_id = @boxId
                """
                c.Parameters.AddWithValue("@boxId", id) |> ignore
                use reader : SqliteDataReader = c.ExecuteReader()
                readList (fun (r: SqliteDataReader) -> r.GetString(0)) reader
            let itemPhotoPaths : string list =
                itemIds
                |> List.choose (fun iid ->
                    use c : SqliteCommand = this.CreateCommand()
                    c.CommandText <- "SELECT photo_path FROM item WHERE id = @id AND photo_path IS NOT NULL"
                    c.Parameters.AddWithValue("@id", iid) |> ignore
                    let r : obj = c.ExecuteScalar()
                    match r with
                    | :? string as p -> Some p
                    | _ -> None)
            let photoPaths : string list =
                (boxPhotoPath |> Option.toList) @ itemPhotoPaths
            for iid in itemIds do
                this.RecordMove("item", iid, None, None) |> ignore
            use delMoves : SqliteCommand = this.CreateCommand()
            delMoves.CommandText <- "DELETE FROM move WHERE entity_type = 'box' AND entity_id = @id"
            delMoves.Parameters.AddWithValue("@id", id) |> ignore
            delMoves.ExecuteNonQuery() |> ignore
            use delSearch : SqliteCommand = this.CreateCommand()
            delSearch.CommandText <- "DELETE FROM item_search WHERE item_id IN (SELECT id FROM item WHERE id IN (SELECT entity_id FROM move WHERE entity_type = 'item' AND to_type = 'box' AND to_id = @boxId))"
            delSearch.Parameters.AddWithValue("@boxId", id) |> ignore
            delSearch.ExecuteNonQuery() |> ignore
            use delBox : SqliteCommand = this.CreateCommand()
            delBox.CommandText <- "DELETE FROM box WHERE id = @id"
            delBox.Parameters.AddWithValue("@id", id) |> ignore
            delBox.ExecuteNonQuery() |> ignore
            photoPaths)

    member this.GetItemsForBox(boxId: string) : Item list =
        use c : SqliteCommand = this.CreateCommand()
        c.CommandText <- """
            SELECT i.id, i.name, i.photo_path, i.added_at, item_lp.to_type, item_lp.to_id
            FROM item i
            INNER JOIN (
                SELECT entity_id, to_type, to_id
                FROM (
                    SELECT entity_id, to_type, to_id,
                           ROW_NUMBER() OVER (PARTITION BY entity_id ORDER BY moved_at DESC) as rn
                    FROM move WHERE entity_type = 'item'
                )
                WHERE rn = 1
            ) item_lp ON item_lp.entity_id = i.id
            WHERE item_lp.to_type = 'box' AND item_lp.to_id = @boxId
            ORDER BY i.added_at
        """
        c.Parameters.AddWithValue("@boxId", boxId) |> ignore
        use reader : SqliteDataReader = c.ExecuteReader()
        readList (fun (r: SqliteDataReader) -> readItem r 0) reader

    member this.GetItem(id: string) : Item option =
        use c : SqliteCommand = this.CreateCommand()
        c.CommandText <- """
            SELECT i.id, i.name, i.photo_path, i.added_at, lp.to_type, lp.to_id
            FROM item i
            LEFT JOIN (
                SELECT entity_id, to_type, to_id
                FROM (
                    SELECT entity_id, to_type, to_id,
                           ROW_NUMBER() OVER (PARTITION BY entity_id ORDER BY moved_at DESC) as rn
                    FROM move WHERE entity_type = 'item'
                )
                WHERE rn = 1
            ) lp ON lp.entity_id = i.id
            WHERE i.id = @id
        """
        c.Parameters.AddWithValue("@id", id) |> ignore
        use reader : SqliteDataReader = c.ExecuteReader()
        if reader.Read() then Some(readItem reader 0)
        else None

    member this.CreateItem(name: ItemName, photoPath: PhotoPath option) : Item =
        let id : Guid = Guid.NewGuid()
        let photoValue : obj =
            match photoPath with
            | Some p -> box (PhotoPath.value p)
            | None -> DBNull.Value
        let now : DateTimeOffset = DateTimeOffset.UtcNow
        use c : SqliteCommand = this.CreateCommand()
        c.CommandText <- "INSERT INTO item (id, name, photo_path, added_at) VALUES (@id, @name, @photoPath, @addedAt)"
        c.Parameters.AddWithValue("@id", id.ToString()) |> ignore
        c.Parameters.AddWithValue("@name", ItemName.value name) |> ignore
        c.Parameters.AddWithValue("@photoPath", photoValue) |> ignore
        c.Parameters.AddWithValue("@addedAt", now.ToString("o")) |> ignore
        c.ExecuteNonQuery() |> ignore
        { Id = id; Name = name; Photo = photoPath; Placement = Unassigned; AddedAt = now }

    member this.AddItem(boxId: string, name: ItemName, photoPath: PhotoPath option) : Item =
        this.InTransaction(fun () ->
            let id : Guid = Guid.NewGuid()
            let photoValue : obj =
                match photoPath with
                | Some p -> box (PhotoPath.value p)
                | None -> DBNull.Value
            let now : DateTimeOffset = DateTimeOffset.UtcNow
            use c : SqliteCommand = this.CreateCommand()
            c.CommandText <- "INSERT INTO item (id, name, photo_path, added_at) VALUES (@id, @name, @photoPath, @addedAt)"
            c.Parameters.AddWithValue("@id", id.ToString()) |> ignore
            c.Parameters.AddWithValue("@name", ItemName.value name) |> ignore
            c.Parameters.AddWithValue("@photoPath", photoValue) |> ignore
            c.Parameters.AddWithValue("@addedAt", now.ToString("o")) |> ignore
            c.ExecuteNonQuery() |> ignore
            this.RecordMove("item", id.ToString(), Some "box", Some boxId) |> ignore
            { Id = id; Name = name; Photo = photoPath; Placement = InBox(BoxId.tryParse boxId |> unwrap); AddedAt = now })

    member this.UpdateItemName(id: string, name: ItemName) : Item option =
        this.InTransaction(fun () ->
            use c : SqliteCommand = this.CreateCommand()
            c.CommandText <- "UPDATE item SET name = @name WHERE id = @id"
            c.Parameters.AddWithValue("@id", id) |> ignore
            c.Parameters.AddWithValue("@name", ItemName.value name) |> ignore
            let rows : int = c.ExecuteNonQuery()
            if rows = 0 then None
            else
                this.RemoveFromSearch(id)
                this.SyncItemToSearch(id, ItemName.value name)
                this.GetItem(id))

    member this.UpdateItemPhoto(id: string, photoPath: PhotoPath option) : Item option =
        use c : SqliteCommand = this.CreateCommand()
        let photoValue : obj =
            match photoPath with
            | Some p -> box (PhotoPath.value p)
            | None -> DBNull.Value
        c.CommandText <- "UPDATE item SET photo_path = @photoPath WHERE id = @id"
        c.Parameters.AddWithValue("@id", id) |> ignore
        c.Parameters.AddWithValue("@photoPath", photoValue) |> ignore
        let rows : int = c.ExecuteNonQuery()
        if rows = 0 then None else this.GetItem(id)

    member this.UpdateBoxPhoto(id: string, photoPath: PhotoPath option) : Box option =
        use c : SqliteCommand = this.CreateCommand()
        let photoValue : obj =
            match photoPath with
            | Some p -> box (PhotoPath.value p)
            | None -> DBNull.Value
        c.CommandText <- "UPDATE box SET photo_path = @photoPath WHERE id = @id"
        c.Parameters.AddWithValue("@id", id) |> ignore
        c.Parameters.AddWithValue("@photoPath", photoValue) |> ignore
        let rows : int = c.ExecuteNonQuery()
        if rows = 0 then None else this.GetBox(id)

    member this.DeleteItem(id: string) : string option =
        this.InTransaction(fun () ->
            let photoPath : string option =
                use c : SqliteCommand = this.CreateCommand()
                c.CommandText <- "SELECT photo_path FROM item WHERE id = @id"
                c.Parameters.AddWithValue("@id", id) |> ignore
                let result : obj = c.ExecuteScalar()
                match result with
                | :? string as p -> Some p
                | _ -> None
            this.RemoveFromSearch(id)
            use delMoves : SqliteCommand = this.CreateCommand()
            delMoves.CommandText <- "DELETE FROM move WHERE entity_type = 'item' AND entity_id = @id"
            delMoves.Parameters.AddWithValue("@id", id) |> ignore
            delMoves.ExecuteNonQuery() |> ignore
            use delItem : SqliteCommand = this.CreateCommand()
            delItem.CommandText <- "DELETE FROM item WHERE id = @id"
            delItem.Parameters.AddWithValue("@id", id) |> ignore
            delItem.ExecuteNonQuery() |> ignore
            photoPath)

    member this.RecordMove(entityType: string, entityId: string, toType: string option, toId: string option) : Move =
        this.InTransaction(fun () ->
            let id : Guid = Guid.NewGuid()
            let now : DateTimeOffset = DateTimeOffset.UtcNow
            let toContainer : Container =
                match toType, toId with
                | Some "box", Some bid -> InBox(bid |> BoxId.tryParse |> unwrap)
                | Some "location", Some code -> AtLocation(code |> LocationCode.tryParse |> unwrap)
                | _ -> Unassigned
            use c : SqliteCommand = this.CreateCommand()
            c.CommandText <- "INSERT INTO move (id, entity_type, entity_id, to_type, to_id, moved_at) VALUES (@id, @entityType, @entityId, @toType, @toId, @movedAt)"
            c.Parameters.AddWithValue("@id", id.ToString()) |> ignore
            c.Parameters.AddWithValue("@entityType", entityType) |> ignore
            c.Parameters.AddWithValue("@entityId", entityId) |> ignore
            c.Parameters.AddWithValue("@toType", toDb toType) |> ignore
            c.Parameters.AddWithValue("@toId", toDb toId) |> ignore
            c.Parameters.AddWithValue("@movedAt", now.ToString("o")) |> ignore
            c.ExecuteNonQuery() |> ignore
            if entityType = "box" then
                this.ReindexBoxItems(entityId)
            elif entityType = "item" then
                this.RemoveFromSearch(entityId)
                let itemName =
                    use q : SqliteCommand = this.CreateCommand()
                    q.CommandText <- "SELECT name FROM item WHERE id = @id"
                    q.Parameters.AddWithValue("@id", entityId) |> ignore
                    q.ExecuteScalar() :?> string
                this.SyncItemToSearch(entityId, itemName)
            { Id = id; EntityType = entityType; EntityId = entityId; To = toContainer; MovedAt = now })

    member this.GetMoveHistory(entityType: string, entityId: string) : Move list =
        use c : SqliteCommand = this.CreateCommand()
        c.CommandText <- """
            SELECT id, entity_type, entity_id, to_type, to_id, moved_at
            FROM move
            WHERE entity_type = @entityType AND entity_id = @entityId
            ORDER BY moved_at DESC
        """
        c.Parameters.AddWithValue("@entityType", entityType) |> ignore
        c.Parameters.AddWithValue("@entityId", entityId) |> ignore
        use reader : SqliteDataReader = c.ExecuteReader()
        readList readMove reader

    member private this.ReadNote (reader: SqliteDataReader) : Note =
        { Id = Guid.Parse(reader.GetString(0))
          EntityType = reader.GetString(1)
          EntityId = reader.GetString(2)
          Content = reader.GetString(3)
          CreatedAt = reader.GetString(4) |> DateTimeOffset.Parse
          UpdatedAt = reader.GetString(5) |> DateTimeOffset.Parse }

    member this.ListNotes(entityType: string, entityId: string) : Note list =
        use c : SqliteCommand = this.CreateCommand()
        c.CommandText <- """
            SELECT id, entity_type, entity_id, content, created_at, updated_at
            FROM note
            WHERE entity_type = @entityType AND entity_id = @entityId
            ORDER BY created_at DESC
        """
        c.Parameters.AddWithValue("@entityType", entityType) |> ignore
        c.Parameters.AddWithValue("@entityId", entityId) |> ignore
        use reader : SqliteDataReader = c.ExecuteReader()
        readList this.ReadNote reader

    member this.CreateNote(entityType: string, entityId: string, content: string) : Note =
        let id : Guid = Guid.NewGuid()
        let now : DateTimeOffset = DateTimeOffset.UtcNow
        use c : SqliteCommand = this.CreateCommand()
        c.CommandText <- "INSERT INTO note (id, entity_type, entity_id, content, created_at, updated_at) VALUES (@id, @entityType, @entityId, @content, @createdAt, @updatedAt)"
        c.Parameters.AddWithValue("@id", id.ToString()) |> ignore
        c.Parameters.AddWithValue("@entityType", entityType) |> ignore
        c.Parameters.AddWithValue("@entityId", entityId) |> ignore
        c.Parameters.AddWithValue("@content", content) |> ignore
        c.Parameters.AddWithValue("@createdAt", now.ToString("o")) |> ignore
        c.Parameters.AddWithValue("@updatedAt", now.ToString("o")) |> ignore
        c.ExecuteNonQuery() |> ignore
        { Id = id; EntityType = entityType; EntityId = entityId; Content = content; CreatedAt = now; UpdatedAt = now }

    member this.UpdateNote(id: string, content: string) : Note option =
        let now : DateTimeOffset = DateTimeOffset.UtcNow
        use c : SqliteCommand = this.CreateCommand()
        c.CommandText <- "UPDATE note SET content = @content, updated_at = @updatedAt WHERE id = @id"
        c.Parameters.AddWithValue("@id", id) |> ignore
        c.Parameters.AddWithValue("@content", content) |> ignore
        c.Parameters.AddWithValue("@updatedAt", now.ToString("o")) |> ignore
        let rows : int = c.ExecuteNonQuery()
        if rows = 0 then None
        else
            use q : SqliteCommand = this.CreateCommand()
            q.CommandText <- "SELECT id, entity_type, entity_id, content, created_at, updated_at FROM note WHERE id = @id"
            q.Parameters.AddWithValue("@id", id) |> ignore
            use reader : SqliteDataReader = q.ExecuteReader()
            if reader.Read() then Some(this.ReadNote reader) else None

    member this.DeleteNote(id: string) : unit =
        use c : SqliteCommand = this.CreateCommand()
        c.CommandText <- "DELETE FROM note WHERE id = @id"
        c.Parameters.AddWithValue("@id", id) |> ignore
        c.ExecuteNonQuery() |> ignore

    member this.SearchItems(query: string option) : SearchResult list =
        match query with
        | Some q when not (String.IsNullOrWhiteSpace q) ->
            use c : SqliteCommand = this.CreateCommand()
            c.CommandText <- """
                SELECT i.id, i.name, i.photo_path,
                       COALESCE(item_lp.to_id, '') as box_id,
                       b.label as box_label,
                       box_lp.to_id as location_code,
                       l.name as location_name,
                       i.added_at
                FROM item_search s
                JOIN item i ON i.id = s.item_id
                LEFT JOIN (
                    SELECT entity_id, to_type, to_id
                    FROM (
                        SELECT entity_id, to_type, to_id,
                               ROW_NUMBER() OVER (PARTITION BY entity_id ORDER BY moved_at DESC) as rn
                        FROM move WHERE entity_type = 'item'
                    )
                    WHERE rn = 1 AND to_type = 'box'
                ) item_lp ON item_lp.entity_id = i.id
                LEFT JOIN box b ON b.id = item_lp.to_id
                LEFT JOIN (
                    SELECT entity_id, to_type, to_id
                    FROM (
                        SELECT entity_id, to_type, to_id,
                               ROW_NUMBER() OVER (PARTITION BY entity_id ORDER BY moved_at DESC) as rn
                        FROM move WHERE entity_type = 'box'
                    )
                    WHERE rn = 1 AND to_type = 'location'
                ) box_lp ON box_lp.entity_id = b.id
                LEFT JOIN location l ON l.code = box_lp.to_id
                WHERE item_search MATCH @query
                ORDER BY rank
                LIMIT 100
            """
            c.Parameters.AddWithValue("@query", q) |> ignore
            use reader : SqliteDataReader = c.ExecuteReader()
            readList readSearchResult reader
        | _ ->
            use c : SqliteCommand = this.CreateCommand()
            c.CommandText <- """
                SELECT i.id, i.name, i.photo_path,
                       COALESCE(item_lp.to_id, '') as box_id,
                       b.label as box_label,
                       box_lp.to_id as location_code,
                       l.name as location_name,
                       i.added_at
                FROM item i
                LEFT JOIN (
                    SELECT entity_id, to_type, to_id
                    FROM (
                        SELECT entity_id, to_type, to_id,
                               ROW_NUMBER() OVER (PARTITION BY entity_id ORDER BY moved_at DESC) as rn
                        FROM move WHERE entity_type = 'item'
                    )
                    WHERE rn = 1 AND to_type = 'box'
                ) item_lp ON item_lp.entity_id = i.id
                LEFT JOIN box b ON b.id = item_lp.to_id
                LEFT JOIN (
                    SELECT entity_id, to_type, to_id
                    FROM (
                        SELECT entity_id, to_type, to_id,
                               ROW_NUMBER() OVER (PARTITION BY entity_id ORDER BY moved_at DESC) as rn
                        FROM move WHERE entity_type = 'box'
                    )
                    WHERE rn = 1 AND to_type = 'location'
                ) box_lp ON box_lp.entity_id = b.id
                LEFT JOIN location l ON l.code = box_lp.to_id
                ORDER BY i.added_at DESC
                LIMIT 100
            """
            use reader : SqliteDataReader = c.ExecuteReader()
            readList readSearchResult reader

    member this.GetItemSearchResult(id: string) : SearchResult option =
        use c : SqliteCommand = this.CreateCommand()
        c.CommandText <- """
            SELECT i.id, i.name, i.photo_path,
                   COALESCE(item_lp.to_id, '') as box_id,
                   b.label as box_label,
                   box_lp.to_id as location_code,
                   l.name as location_name,
                   i.added_at
            FROM item i
            LEFT JOIN (
                SELECT entity_id, to_type, to_id
                FROM (
                    SELECT entity_id, to_type, to_id,
                           ROW_NUMBER() OVER (PARTITION BY entity_id ORDER BY moved_at DESC) as rn
                    FROM move WHERE entity_type = 'item' AND entity_id = @id
                )
                WHERE rn = 1 AND to_type = 'box'
            ) item_lp ON item_lp.entity_id = i.id
            LEFT JOIN box b ON b.id = item_lp.to_id
            LEFT JOIN (
                SELECT entity_id, to_type, to_id
                FROM (
                    SELECT entity_id, to_type, to_id,
                           ROW_NUMBER() OVER (PARTITION BY entity_id ORDER BY moved_at DESC) as rn
                    FROM move WHERE entity_type = 'box'
                )
                WHERE rn = 1 AND to_type = 'location'
            ) box_lp ON box_lp.entity_id = b.id
            LEFT JOIN location l ON l.code = box_lp.to_id
            WHERE i.id = @id
        """
        c.Parameters.AddWithValue("@id", id) |> ignore
        use reader : SqliteDataReader = c.ExecuteReader()
        if reader.Read() then Some(readSearchResult reader) else None
