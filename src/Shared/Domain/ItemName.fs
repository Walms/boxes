module BoxTracker.ItemName

type ItemName = private ItemName of string

let create (raw: string) : Result<ItemName, string> =
    let trimmed : string =
        if System.Object.ReferenceEquals(raw, null) then ""
        else raw.Trim()
    if trimmed.Length = 0 then
        Error "Item name must not be empty"
    elif trimmed.Length > 200 then
        Error "Item name must be 200 characters or fewer"
    else
        Ok(ItemName trimmed)

let value (ItemName s: ItemName) : string = s
