module BoxTracker.Handlers.PhotoJobHandlers

open System
open System.IO
open System.Threading.Tasks
open Giraffe
open Microsoft.AspNetCore.Http
open BoxTracker.PhotoJobStore
open BoxTracker.PhotoProcessing
open BoxTracker.PhotoPath
open BoxTracker.Dto

/// Persist the raw upload to a durable staging area and enqueue a processing
/// job. Returns immediately so the client can stop "uploading" and start
/// polling; the actual resize/encode happens on the background worker.
let enqueuePhotoJob
    (ctx: HttpContext)
    (entityType: string)
    (entityId: string)
    (folder: string)
    (oldPhoto: string option)
    (file: IFormFile) : Task<PhotoJob> =
    task {
        let store : PhotoJobStore = ctx.GetService<PhotoJobStore>()
        let signal : PhotoJobSignal = ctx.GetService<PhotoJobSignal>()
        let config : BoxTrackerConfig = ctx.GetService<BoxTrackerConfig>()

        let guid : Guid = Guid.NewGuid()
        let photoPath : string = BoxTracker.PhotoPath.createWebP folder guid |> BoxTracker.PhotoPath.value

        // Raw upload is kept until processing finishes, so an interrupted/
        // restarted server can still resume the job from disk. It lives outside
        // the web-served "photos" directory so it is never downloadable.
        let jobsDir : string = Path.Combine(config.DataDir, "photo-jobs")
        Directory.CreateDirectory(jobsDir) |> ignore
        let sourceRel : string = Path.Combine("photo-jobs", guid.ToString())
        let sourceAbs : string = Path.Combine(config.DataDir, sourceRel)

        use stream : FileStream = new FileStream(sourceAbs, FileMode.Create)
        do! file.CopyToAsync(stream)
        stream.Flush()
        stream.Close()

        let job : PhotoJob = store.CreateJob(entityType, entityId, sourceRel, photoPath, oldPhoto)
        signal.Notify()
        return job
    }

let getPhotoJob (id: string) : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let store : PhotoJobStore = ctx.GetService<PhotoJobStore>()
            match store.GetJob(id) with
            | None ->
                return! (setStatusCode 404 >=> json {| error = $"Photo job '%s{id}' not found" |}) next ctx
            | Some job ->
                return! json (photoJobToDto job) next ctx
        }
