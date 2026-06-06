module BoxTracker.ImageProcessing

open System
open System.IO
open System.Diagnostics
open SixLabors.ImageSharp
open SixLabors.ImageSharp.Processing
open SixLabors.ImageSharp.Formats.Jpeg

let private jpegEncoder (quality: int) : JpegEncoder = JpegEncoder(Quality = Nullable quality)

// jpegtran (libjpeg-turbo-progs on Ubuntu) losslessly rewrites a baseline
// JPEG as progressive, so the browser can show a blurry preview of the whole
// image immediately rather than revealing it line by line on slow connections.
// Silently skips if jpegtran is not installed.
let private makeProgressive (path: string) : unit =
    let tmpPath = path + ".tmp"
    try
        let psi =
            ProcessStartInfo(
                FileName = "jpegtran",
                Arguments = $"-progressive -copy none \"{path}\" \"{tmpPath}\"",
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            )
        use proc = Process.Start(psi)
        proc.WaitForExit()
        if proc.ExitCode = 0 && File.Exists(tmpPath) then
            File.Move(tmpPath, path, true)
        else
            if File.Exists(tmpPath) then File.Delete(tmpPath)
    with _ ->
        if File.Exists(tmpPath) then File.Delete(tmpPath)

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
        resizeImage fullImage 3500 3500
        fullImage.SaveAsJpeg(outputFullPath, jpegEncoder 85)
        makeProgressive outputFullPath

        use thumbImage : Image = image.Clone(fun cfg -> ())
        resizeImage thumbImage 250 250
        thumbImage.SaveAsJpeg(outputThumbPath, jpegEncoder 75)
        makeProgressive outputThumbPath

        Ok ()
    with
    | ex -> Error $"Failed to process image: {ex.Message}"

let migrateToProgressiveJpeg (dataDir: string) : unit =
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
            let jpgFile = Path.ChangeExtension(webpFile, ".jpg")
            if File.Exists(jpgFile) then
                try File.Delete(webpFile) with _ -> ()
                skipped <- skipped + 1
            else
                try
                    use image = Image.Load(webpFile)
                    let quality = if webpFile.Contains("-thumb.") then 75 else 85
                    image.SaveAsJpeg(jpgFile, jpegEncoder quality)
                    makeProgressive jpgFile
                    File.Delete(webpFile)
                    migrated <- migrated + 1
                with ex ->
                    eprintfn "Failed to migrate %s: %s" (Path.GetFileName webpFile) ex.Message
                    if File.Exists(jpgFile) then File.Delete(jpgFile)
                    failed <- failed + 1
        printfn "Migration complete: %d migrated, %d skipped, %d failed" migrated skipped failed
