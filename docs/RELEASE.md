# Standalone Windows Build

This project publishes as a self-contained `win-x64` Windows app. The built app does not require the .NET runtime to be installed on the gaming PC.

## Build the app

On Windows with the .NET 8 SDK installed:

```powershell
.\scripts\build-release.ps1
```

Output:

- `artifacts\publish\win-x64\DualSensePass.exe`
- `artifacts\DualSensePass-win-x64.zip`

## Run the published app

```powershell
.\scripts\run-standalone.ps1
```

Or open:

```text
artifacts\publish\win-x64\DualSensePass.exe
```

## What still must be installed

The standalone app bundles .NET, but it does not bundle Windows drivers:

- ViGEmBus is still required for the virtual Xbox controller.
- HidHide is still recommended to hide the physical DualSense from games.

## Build without self-tests

If you only want to publish:

```powershell
.\scripts\build-release.ps1 -SkipSelfTest
```
