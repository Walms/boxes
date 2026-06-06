module BoxTracker.Server.Program

open System
open System.IO
open System.Text.Json
open System.Text.Json.Serialization
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.ResponseCompression
open Microsoft.AspNetCore.StaticFiles
open Microsoft.Extensions.FileProviders
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open Giraffe
open BoxTracker.Router
open BoxTracker.Storage
open BoxTracker.PhotoJobStore
open BoxTracker.PhotoProcessing
open BoxTracker.ImageProcessing
open BoxTracker.Dto

let dataDir : string =
    match Environment.GetEnvironmentVariable "BOXTRACKER_DATA" with
    | null | "" -> "./data"
    | dir -> dir

let ensureDataDir () : unit =
    if not (Directory.Exists dataDir) then Directory.CreateDirectory dataDir |> ignore

let configureApp (app: IApplicationBuilder) : unit =
    ensureDataDir()
    let photosDir : string = Path.Combine(dataDir, "photos")
    if not (Directory.Exists photosDir) then Directory.CreateDirectory photosDir |> ignore
    app.UseResponseCompression() |> ignore
    // Photo filenames are content-addressed ({guid}-{variant}.jpg) and never
    // mutated in place, so they can be cached aggressively. Tell the browser to
    // keep them for a year and skip revalidation, so navigating between pages
    // reuses cached thumbnails instead of re-fetching every image.
    app.UseStaticFiles(StaticFileOptions(
        FileProvider = new PhysicalFileProvider(photosDir),
        RequestPath = PathString("/api/photos"),
        OnPrepareResponse = fun (ctx: StaticFileResponseContext) ->
            ctx.Context.Response.Headers.CacheControl <-
                Microsoft.Extensions.Primitives.StringValues "public, max-age=31536000, immutable"
    )) |> ignore
    app.UseGiraffe webApp

let configureServices (services: IServiceCollection) : unit =
    services.AddGiraffe() |> ignore

    // Compress API JSON responses (the bundle itself is gzip/zstd-compressed by
    // the reverse proxy). application/json is added explicitly because it is not
    // in the framework's default compressible MIME type set.
    services.AddResponseCompression(fun (options: ResponseCompressionOptions) ->
        options.EnableForHttps <- true
        options.MimeTypes <- Seq.append ResponseCompressionDefaults.MimeTypes [ "application/json" ]) |> ignore

    let jsonOpts : JsonSerializerOptions = JsonSerializerOptions()
    jsonOpts.PropertyNameCaseInsensitive <- true
    jsonOpts.Converters.Add(JsonFSharpConverter(JsonFSharpOptions.Default()))
    services.AddSingleton<Json.ISerializer>(SystemTextJson.Serializer(jsonOpts)) |> ignore

    let dbPath : string = Path.Combine(dataDir, "boxtracker.db")
    let connStr : string = $"Data Source=%s{dbPath}"

    // Create the schema and set WAL mode once at startup. Storage is then
    // registered per-request so concurrent requests each use their own pooled
    // SQLite connection instead of serializing on a single shared one.
    Storage.InitializeSchema(connStr)
    services.AddScoped<Storage>(fun (_: IServiceProvider) -> new Storage(connStr)) |> ignore
    services.AddSingleton<BoxTrackerConfig>({ DataDir = dataDir }) |> ignore

    // Photo processing runs on a durable, server-side queue so uploads return
    // quickly and processing survives client disconnects and server restarts.
    let photoJobStore : PhotoJobStore = new PhotoJobStore(connStr)
    photoJobStore.Connect()
    services.AddSingleton<PhotoJobStore>(photoJobStore) |> ignore
    services.AddSingleton<PhotoJobSignal>(PhotoJobSignal()) |> ignore
    services.AddHostedService<PhotoProcessingService>() |> ignore

[<EntryPoint>]
let main (args: string array) : int =
    if args.Length > 0 && args.[0] = "--migrate-photos" then
        migratePhotos dataDir
        0
    else
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(fun (webHostBuilder: IWebHostBuilder) ->
                webHostBuilder
                    .Configure(configureApp)
                    .ConfigureServices(configureServices)
                    |> ignore)
            .Build()
            .Run()
        0
