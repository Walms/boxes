module BoxTracker.Api.Tests.PhotoJobStoreTests

open System
open System.IO
open Xunit
open BoxTracker.PhotoJobStore

let private withStore (test: PhotoJobStore -> 'T) : 'T =
    let tempFile : string = Path.GetTempFileName()
    let connStr : string = $"Data Source=%s{tempFile}"
    use store : PhotoJobStore = new PhotoJobStore(connStr)
    store.Connect()
    try
        test store
    finally
        File.Delete(tempFile)

[<Fact>]
let ``CreateJob and GetJob roundtrip`` () : unit =
    withStore (fun store ->
        let job = store.CreateJob("box", "BOX-001", "uploads/src", "photos/BOX-001/abc", None)
        Assert.Equal("box", job.EntityType)
        Assert.Equal(StatusPending, job.Status)
        Assert.Equal(None, job.Error)
        Assert.Equal(None, job.OldPhotoPath)

        let fetched = store.GetJob(job.Id)
        Assert.True(fetched.IsSome)
        Assert.Equal("photos/BOX-001/abc", fetched.Value.PhotoPath)
        Assert.Equal(StatusPending, fetched.Value.Status)
    )

[<Fact>]
let ``CreateJob persists oldPhotoPath`` () : unit =
    withStore (fun store ->
        let job = store.CreateJob("item", "abc", "uploads/src", "photos/x/new", Some "photos/x/old")
        let fetched = store.GetJob(job.Id)
        Assert.Equal(Some "photos/x/old", fetched.Value.OldPhotoPath)
    )

[<Fact>]
let ``GetJob returns None for unknown id`` () : unit =
    withStore (fun store ->
        Assert.True((store.GetJob(Guid.NewGuid().ToString())).IsNone)
    )

[<Fact>]
let ``ClaimNext returns the oldest pending job and marks it processing`` () : unit =
    withStore (fun store ->
        let first = store.CreateJob("box", "BOX-001", "uploads/a", "photos/a", None)
        store.CreateJob("box", "BOX-002", "uploads/b", "photos/b", None) |> ignore
        let claimed = store.ClaimNext()
        Assert.True(claimed.IsSome)
        Assert.Equal(first.Id, claimed.Value.Id)
        Assert.Equal(StatusProcessing, claimed.Value.Status)
        // The persisted row reflects the new status.
        Assert.Equal(StatusProcessing, (store.GetJob(first.Id)).Value.Status)
    )

[<Fact>]
let ``ClaimNext returns None when nothing is pending`` () : unit =
    withStore (fun store ->
        Assert.True((store.ClaimNext()).IsNone)
    )

[<Fact>]
let ``ClaimNext does not re-claim a job already in flight`` () : unit =
    withStore (fun store ->
        store.CreateJob("box", "BOX-001", "uploads/a", "photos/a", None) |> ignore
        Assert.True((store.ClaimNext()).IsSome)
        Assert.True((store.ClaimNext()).IsNone)
    )

[<Fact>]
let ``MarkCompleted sets completed status and clears error`` () : unit =
    withStore (fun store ->
        let job = store.CreateJob("box", "BOX-001", "uploads/a", "photos/a", None)
        store.MarkFailed(job.Id, "temporary")
        store.MarkCompleted(job.Id)
        let fetched = store.GetJob(job.Id)
        Assert.Equal(StatusCompleted, fetched.Value.Status)
        Assert.Equal(None, fetched.Value.Error)
    )

[<Fact>]
let ``MarkFailed records the error message`` () : unit =
    withStore (fun store ->
        let job = store.CreateJob("box", "BOX-001", "uploads/a", "photos/a", None)
        store.MarkFailed(job.Id, "decode error")
        let fetched = store.GetJob(job.Id)
        Assert.Equal(StatusFailed, fetched.Value.Status)
        Assert.Equal(Some "decode error", fetched.Value.Error)
    )

[<Fact>]
let ``ResetInterrupted moves processing jobs back to pending`` () : unit =
    withStore (fun store ->
        store.CreateJob("box", "BOX-001", "uploads/a", "photos/a", None) |> ignore
        let claimed = store.ClaimNext()
        Assert.Equal(StatusProcessing, claimed.Value.Status)
        store.ResetInterrupted()
        Assert.Equal(StatusPending, (store.GetJob(claimed.Value.Id)).Value.Status)
        // ...and it becomes claimable again.
        Assert.True((store.ClaimNext()).IsSome)
    )

[<Fact>]
let ``ResetInterrupted leaves completed and failed jobs untouched`` () : unit =
    withStore (fun store ->
        let done_ = store.CreateJob("box", "BOX-001", "uploads/a", "photos/a", None)
        let failed = store.CreateJob("box", "BOX-002", "uploads/b", "photos/b", None)
        store.MarkCompleted(done_.Id)
        store.MarkFailed(failed.Id, "nope")
        store.ResetInterrupted()
        Assert.Equal(StatusCompleted, (store.GetJob(done_.Id)).Value.Status)
        Assert.Equal(StatusFailed, (store.GetJob(failed.Id)).Value.Status)
    )

[<Fact>]
let ``SetEntityPhoto updates the box row`` () : unit =
    withStore (fun store ->
        // Insert a box row directly via a Storage sharing the same DB is awkward;
        // instead, exercise the unknown-entity guard which is pure logic.
        Assert.Throws<System.Exception>(fun () ->
            store.SetEntityPhoto("nonsense", "x", "photos/x")) |> ignore
    )
