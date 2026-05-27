module BoxTracker.PhotoPath

type PhotoPath = private PhotoPath of string

let create (boxId: string) (guid: System.Guid) (extension: string) : PhotoPath =
    PhotoPath $"photos/%s{boxId}/%s{guid.ToString()}.%s{extension}"

let value (PhotoPath s: PhotoPath) : string = s

let tryParse (s: string) : Result<PhotoPath, string> =
    if s <> null && s.StartsWith("photos/") then
        Ok(PhotoPath s)
    else
        Error $"Invalid photo path: '{s}'"
