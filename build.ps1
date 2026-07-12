param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$Artifacts = Join-Path $Root "artifacts"

Set-Location $Root

dotnet restore p99.NET.sln
dotnet build p99.NET.sln --configuration $Configuration --no-restore
dotnet test p99.NET.sln --configuration $Configuration --no-build --verbosity normal

$Packages = Join-Path $Artifacts "packages"
New-Item -ItemType Directory -Force -Path $Packages | Out-Null

dotnet pack src/P99/P99.csproj `
  --configuration $Configuration `
  --no-build `
  --output $Packages

Write-Host "Packages written to $Packages"
