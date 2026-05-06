# Pastas

Pastas is a local Windows 10/11 clipboard manager. It keeps recent text and image clipboard items on the current machine, with quick search, preview, pinning, cleanup limits, and tray-first background behavior.

## Build From Source

Requirements:
- Windows 10/11
- .NET 8 SDK

From the repository root:

```powershell
dotnet restore Pastas.sln
dotnet build src/Pastas/Pastas.csproj
```

Run tests:

```powershell
dotnet test tests/Pastas.UnitTests/Pastas.UnitTests.csproj
```

Run a dev build:

```powershell
dotnet run --project src/Pastas/Pastas.csproj
```

Build the solution in Release mode:

```powershell
dotnet build Pastas.sln -c Release
```

Or use the helper script:

```powershell
.\scripts\build-release.ps1
```

## Publish Builds

Framework-dependent publish:

```powershell
dotnet publish src/Pastas/Pastas.csproj -c Release -o artifacts/Pastas-FrameworkDependent
```

Standalone single-file `.exe` for Windows x64:

```powershell
dotnet publish src/Pastas/Pastas.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o artifacts/Pastas-Standalone
```

Or use the helper script:

```powershell
.\scripts\publish-standalone.ps1
```

Publish output is written under `artifacts/`, which is ignored by Git.

Unsigned local `.exe` builds may trigger Windows SmartScreen warnings until the executable is signed.

## Daily Use

- Default hotkey: `Alt+V`
- Close button hides Pastas to the tray.
- Tray menu actions:
  - `Open Pastas`
  - `Settings`
  - `Pause capture` / `Resume capture`
  - `Exit`

## Privacy

Pastas is local-first. Clipboard history, settings, cached images, thumbnails, and diagnostics logs are stored locally under the user's Windows profile. Diagnostics are intended for lifecycle and error troubleshooting and must not include copied clipboard content.

## Repository Layout

- `src/Pastas` - WPF desktop application
- `tests/Pastas.UnitTests` - unit tests
- `docs` - architecture, MVP scope, and manual QA notes

## Principles

- Local-first by default
- Small, focused changes
- Layered architecture with dependencies pointing inward
