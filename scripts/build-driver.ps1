param(
    [string]$Configuration = "Release",
    [string]$Platform = "x64"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "drivers\DualBoxVirtualPad\DualBoxVirtualPad.vcxproj"

if (-not (Test-Path $project)) {
    throw "Driver project not found: $project"
}

msbuild $project /p:Configuration=$Configuration /p:Platform=$Platform

