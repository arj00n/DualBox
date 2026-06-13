param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [switch]$SkipSelfTest
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "src\DualBox.App\DualBox.App.csproj"
$artifacts = Join-Path $root "artifacts"
$output = Join-Path $artifacts "publish\$Runtime"
$zip = Join-Path $artifacts "DualBox-$Runtime.zip"

if (-not $SkipSelfTest) {
    & (Join-Path $root "scripts\run-protocol-selftest.ps1")
}

if (Test-Path $output) {
    Remove-Item $output -Recurse -Force
}

if (Test-Path $zip) {
    Remove-Item $zip -Force
}

dotnet restore $project -r $Runtime
dotnet publish $project `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:PublishTrimmed=false `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -o $output

Compress-Archive -Path (Join-Path $output "*") -DestinationPath $zip

Write-Host "Published standalone DualBox to $output"
Write-Host "Created release zip at $zip"
