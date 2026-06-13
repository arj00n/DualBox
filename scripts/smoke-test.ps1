param(
    [switch]$Build
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "src\DualSensePass.App\DualSensePass.App.csproj"

function Test-ServiceKey {
    param([string]$Name)
    $path = "HKLM:\SYSTEM\CurrentControlSet\Services\$Name"
    Test-Path $path
}

Write-Host "DualSense Pass smoke test"
Write-Host "-------------------------"

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Warning ".NET SDK/runtime was not found on PATH."
} else {
    dotnet --info
}

if (Test-ServiceKey "ViGEmBus") {
    Write-Host "ViGEmBus: found"
} else {
    Write-Warning "ViGEmBus: missing. The virtual Xbox controller cannot be created without it."
}

if (Test-ServiceKey "HidHide") {
    Write-Host "HidHide: found"
} else {
    Write-Warning "HidHide: missing. Games may see duplicate input from the physical DualSense."
}

$sonyDevices = Get-PnpDevice -PresentOnly -ErrorAction SilentlyContinue |
    Where-Object { $_.InstanceId -like "HID\VID_054C*" -or $_.InstanceId -like "USB\VID_054C*" -or $_.InstanceId -like "BTHENUM\*VID&0002054C*" }

if ($sonyDevices) {
    Write-Host "Sony HID devices:"
    $sonyDevices | Format-Table -AutoSize Status, Class, FriendlyName, InstanceId
} else {
    Write-Warning "No present Sony HID device was found. Connect the DualSense over USB for first validation."
}

if ($Build) {
    & (Join-Path $root "scripts\run-protocol-selftest.ps1")
    dotnet restore $project
    dotnet build $project -c Release
}

Write-Host ""
Write-Host "Next: run the app, click Start bridge, click Test rumble, then check joy.cpl for an Xbox 360 controller."
