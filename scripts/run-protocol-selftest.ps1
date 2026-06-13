$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "src\DualBox.ProtocolSelfTest\DualBox.ProtocolSelfTest.csproj"

dotnet run --project $project -c Release
