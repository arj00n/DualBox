$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "src\DualSensePass.ProtocolSelfTest\DualSensePass.ProtocolSelfTest.csproj"

dotnet run --project $project -c Release
