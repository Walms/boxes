module BoxTracker.Api.Tests.ImageProcessingTests

open System
open System.IO
open Xunit
open SixLabors.ImageSharp
open SixLabors.ImageSharp.PixelFormats
open BoxTracker.ImageProcessing

let private withTempDir (test: string -> 'T) : 'T =
    let dir : string = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())
    Directory.CreateDirectory(dir) |> ignore
    try
        test dir
    finally
        try Directory.Delete(dir, true) with _ -> ()

let private makeJpeg (path: string) (width: int) (height: int) : unit =
    use image = new Image<Rgba32>(width, height)
    image.SaveAsJpeg(path)

[<Fact>]
let ``processUploadedImage writes both full and thumb outputs`` () : unit =
    withTempDir (fun dir ->
        let input = Path.Combine(dir, "input.jpg")
        let full = Path.Combine(dir, "out-full.jpg")
        let thumb = Path.Combine(dir, "out-thumb.jpg")
        makeJpeg input 600 400

        let result = processUploadedImage input full thumb
        Assert.True(Result.isOk result)
        Assert.True(File.Exists(full))
        Assert.True(File.Exists(thumb))
    )

[<Fact>]
let ``processUploadedImage shrinks oversized images to within bounds`` () : unit =
    withTempDir (fun dir ->
        let input = Path.Combine(dir, "input.jpg")
        let full = Path.Combine(dir, "out-full.jpg")
        let thumb = Path.Combine(dir, "out-thumb.jpg")
        makeJpeg input 600 400

        processUploadedImage input full thumb |> ignore

        let thumbInfo = Image.Identify(thumb)
        Assert.True(thumbInfo.Width <= 250)
        Assert.True(thumbInfo.Height <= 250)

        let fullInfo = Image.Identify(full)
        Assert.True(fullInfo.Width <= 3500)
        Assert.True(fullInfo.Height <= 3500)
    )

[<Fact>]
let ``processUploadedImage returns Error for a non-image input`` () : unit =
    withTempDir (fun dir ->
        let input = Path.Combine(dir, "input.jpg")
        let full = Path.Combine(dir, "out-full.jpg")
        let thumb = Path.Combine(dir, "out-thumb.jpg")
        File.WriteAllText(input, "this is not an image")

        let result = processUploadedImage input full thumb
        Assert.True(Result.isError result)
    )

[<Fact>]
let ``processUploadedImage returns Error when input is missing`` () : unit =
    withTempDir (fun dir ->
        let input = Path.Combine(dir, "does-not-exist.jpg")
        let full = Path.Combine(dir, "out-full.jpg")
        let thumb = Path.Combine(dir, "out-thumb.jpg")

        let result = processUploadedImage input full thumb
        Assert.True(Result.isError result)
    )
