module BoxTracker.Server.Program

open System
open System.IO
open System.Text.Json
open System.Text.Json.Serialization
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.StaticFiles
open Microsoft.Extensions.FileProviders
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open Giraffe
open BoxTracker.Router
open BoxTracker.Storage
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
    app.UseStaticFiles(StaticFileOptions(
        FileProvider = new PhysicalFileProvider(photosDir),
        RequestPath = PathString("/api/photos")
    )) |> ignore
    app.UseGiraffe webApp

let configureServices (services: IServiceCollection) : unit =
    services.AddGiraffe() |> ignore

    let jsonOpts : JsonSerializerOptions = JsonSerializerOptions()
    jsonOpts.PropertyNameCaseInsensitive <- true
    jsonOpts.Converters.Add(JsonFSharpConverter(JsonFSharpOptions.Default()))
    services.AddSingleton<Json.ISerializer>(SystemTextJson.Serializer(jsonOpts)) |> ignore

    let dbPath : string = Path.Combine(dataDir, "boxtracker.db")
    let connStr : string = $"Data Source=%s{dbPath}"

    let storage : Storage = new Storage(connStr)
    storage.Connect() |> ignore
    services.AddSingleton<Storage>(storage) |> ignore
    services.AddSingleton<BoxTrackerConfig>({ DataDir = dataDir }) |> ignore

[<EntryPoint>]
let main (args: string array) : int =
    Host.CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(fun (webHostBuilder: IWebHostBuilder) ->
            webHostBuilder
                .Configure(configureApp)
                .ConfigureServices(configureServices)
                |> ignore)
        .Build()
        .Run()
    0
