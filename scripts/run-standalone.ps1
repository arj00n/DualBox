$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$exe = Join-Path $root "artifacts\publish\win-x64\DualBox.exe"

if (-not (Test-Path $exe)) {
    throw "Standalone executable not found. Run .\scripts\build-release.ps1 first."
}

Start-Process $exe
