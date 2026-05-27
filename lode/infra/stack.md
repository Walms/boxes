# Technology Stack

| Component | Technology | Version |
|-----------|-----------|---------|
| SDK | .NET | 10.0.100 |
| Dev env | Nix flake | nixos-25.05 |
| F# compiler | FSharp.Core | (SDK-bundled) |
| Backend | Saturn | 0.17.0 |
| Backend core | Giraffe | 8.2.0 (via Saturn) |
| Frontend compiler | Fable | 5.0.0 (local dotnet tool) |
| Frontend bundler | Vite | 6.x (npm) |
| React bindings | Feliz | 3.3.3 |
| State management | Elmish | 4.2.0 |
| CSS framework | Tailwind CSS | 4.x (npm) |
| UI components | DaisyUI | 5.x (npm, @plugin) |
| Database | SQLite | via Microsoft.Data.Sqlite 9.0.5 |
| Full-text search | SQLite FTS5 | built-in |
| Testing | xUnit | 2.9.3 |
| Property testing | FsCheck | 3.1.0 |
| QR codes | QRCoder | 1.8.0 |

## Key Decisions
- Shared project targets `netstandard2.0` for Fable compatibility
- Server targets `net10.0`
- Fable installed as local dotnet tool (`dotnet tool install Fable --version 5.0.0`)
- `Fable.Template` NuGet package is stale at 3.9.0; project scaffolded manually
