module BoxTracker.PhotoProcessing

open System
open System.IO
open System.Threading
open System.Threading.Tasks
open Microsoft.Extensions.Hosting
open BoxTracker.PhotoJobStore
open BoxTracker.Dto
open BoxTracker.ImageProcessing

/// Lets upload handlers wake the worker immediately instead of waiting for the
/// next poll. A single slot is enough: it just means "there might be work".
type PhotoJobSignal () =
    let sem : SemaphoreSlim = new SemaphoreSlim(0, 1)

    member _.Notify() : unit =
        if sem.CurrentCount = 0 then
            try sem.Release() |> ignore
            with _ -> ()

    member _.WaitAsync(timeout: TimeSpan, ct: CancellationToken) : Task<bool> =
        sem.WaitAsync(timeout, ct)

/// Hosted background worker that drains pending photo jobs sequentially.
type PhotoProcessingService (store: PhotoJobStore, signal: PhotoJobSignal, config: BoxTrackerConfig) =
    inherit BackgroundService()

    let dataDir : string = config.DataDir

    let deleteVariants (relBase: string) : unit =
        let basePath : string = Path.Combine(dataDir, relBase)
        let fullPath : string = $"%s{basePath}-full.avif"
        let thumbPath : string = $"%s{basePath}-thumb.avif"
        if File.Exists(fullPath) then File.Delete(fullPath)
        if File.Exists(thumbPath) then File.Delete(thumbPath)

    let processJob (job: PhotoJob) : unit =
        let basePath : string = Path.Combine(dataDir, job.PhotoPath)
        let dir : string = Path.GetDirectoryName(basePath)
        if not (String.IsNullOrEmpty dir) then Directory.CreateDirectory(dir) |> ignore
        let sourceAbs : string = Path.Combine(dataDir, job.SourcePath)
        let result : Result<unit, string> =
            processUploadedImage sourceAbs ($"%s{basePath}-full.avif") ($"%s{basePath}-thumb.avif")
        match result with
        | Error msg ->
            store.MarkFailed(job.Id, msg)
        | Ok () ->
            store.SetEntityPhoto(job.EntityType, job.EntityId, job.PhotoPath)
            job.OldPhotoPath |> Option.iter deleteVariants
            store.MarkCompleted(job.Id)
        if File.Exists(sourceAbs) then
            try File.Delete(sourceAbs) with _ -> ()

    override _.ExecuteAsync(ct: CancellationToken) : Task =
        task {
            // Recover anything that was mid-flight when the process last stopped.
            store.ResetInterrupted()
            while not ct.IsCancellationRequested do
                let mutable keepDraining : bool = true
                while keepDraining && not ct.IsCancellationRequested do
                    match store.ClaimNext() with
                    | Some job ->
                        try processJob job
                        with ex ->
                            try store.MarkFailed(job.Id, ex.Message) with _ -> ()
                    | None ->
                        keepDraining <- false
                if not ct.IsCancellationRequested then
                    try
                        let! _ = signal.WaitAsync(TimeSpan.FromSeconds 2.0, ct)
                        ()
                    with :? OperationCanceledException -> ()
        } :> Task
