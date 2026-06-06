module BoxTracker.ImageProcessing

open System
open System.IO
open SixLabors.ImageSharp
open SixLabors.ImageSharp.Processing
open SixLabors.ImageSharp.Formats.Avif

// AVIF encoding is slow in ImageSharp's pure-C# codec — expect 20-60 s for a
// large image on a low-end VPS. That's fine here because processing runs on
// the background job queue; the file-size savings (~30-50% vs WebP) directly
// reduce the bandwidth that was causing sluggish loads.
let private avifEncoder (quality: int) : AvifEncoder = AvifEncoder(Quality = Nullable quality)

let private resizeImage (image: Image) (maxWidth: int) (maxHeight: int) : unit =
    if image.Width > maxWidth || image.Height > maxHeight then
        let newSize : Size =
            let aspectRatio : float = float image.Width / float image.Height
            if float maxWidth / float maxHeight > aspectRatio then
                Size(int (float maxHeight * aspectRatio), maxHeight)
            else
                Size(maxWidth, int (float maxWidth / aspectRatio))
        image.Mutate(fun ctx -> ctx.Resize(newSize) |> ignore)

let processUploadedImage (inputPath: string) (outputFullPath: string) (outputThumbPath: string) : Result<unit, string> =
    try
        use image : Image = Image.Load(inputPath)
        image.Mutate(fun ctx -> ctx.AutoOrient() |> ignore)

        use fullImage : Image = image.Clone(fun cfg -> ())
        resizeImage fullImage 2000 2000
        fullImage.SaveAsAvif(outputFullPath, avifEncoder 80)

        use thumbImage : Image = image.Clone(fun cfg -> ())
        resizeImage thumbImage 250 250
        thumbImage.SaveAsAvif(outputThumbPath, avifEncoder 65)

        Ok ()
    with
    | ex -> Error $"Failed to process image: {ex.Message}"

let migratePhotos (dataDir: string) : unit =
    let photosDir = Path.Combine(dataDir, "photos")
    if not (Directory.Exists photosDir) then
        eprintfn "Photos directory not found: %s" photosDir
    else
        let webpFiles =
            Directory.EnumerateFiles(photosDir, "*.webp", SearchOption.AllDirectories)
            |> Seq.toArray
        printfn "Found %d WebP file(s) to migrate" webpFiles.Length
        let mutable migrated = 0
        let mutable skipped = 0
        let mutable failed = 0
        for webpFile in webpFiles do
            let avifFile = Path.ChangeExtension(webpFile, ".avif")
            if File.Exists(avifFile) then
                try File.Delete(webpFile) with _ -> ()
                skipped <- skipped + 1
            else
                try
                    use image = Image.Load(webpFile)
                    let quality = if webpFile.Contains("-thumb.") then 65 else 80
                    image.SaveAsAvif(avifFile, avifEncoder quality)
                    File.Delete(webpFile)
                    migrated <- migrated + 1
                with ex ->
                    eprintfn "Failed to migrate %s: %s" (Path.GetFileName webpFile) ex.Message
                    if File.Exists(avifFile) then File.Delete(avifFile)
                    failed <- failed + 1
        printfn "Migration complete: %d migrated, %d skipped, %d failed" migrated skipped failed
