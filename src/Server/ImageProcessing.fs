module BoxTracker.ImageProcessing

open System
open System.IO
open SixLabors.ImageSharp
open SixLabors.ImageSharp.Processing
open SixLabors.ImageSharp.Formats.Webp

let private webpEncoder : WebpEncoder = WebpEncoder(Quality = 80)

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

        use fullImage : Image = image.Clone(fun cfg -> ())
        resizeImage fullImage 2000 2000
        fullImage.SaveAsWebp(outputFullPath, webpEncoder)

        use thumbImage : Image = image.Clone(fun cfg -> ())
        resizeImage thumbImage 250 250
        thumbImage.SaveAsWebp(outputThumbPath, webpEncoder)

        Ok ()
    with
    | ex -> Error $"Failed to process image: {ex.Message}"
