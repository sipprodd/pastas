Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $repoRoot

dotnet restore Pastas.sln
dotnet build Pastas.sln -c Release --no-restore
dotnet test tests/Pastas.UnitTests/Pastas.UnitTests.csproj -c Release --no-build
