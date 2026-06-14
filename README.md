# DualBox

DualBox turns a PS5 DualSense or DualSense Edge into an Xbox One-style controller bridge for Windows Game Pass and PC games.

## Features

- DualSense USB and Bluetooth input support
- Xbox One-style feedback pipeline with main motors and trigger motors
- Current packaged backend: XInput compatibility through ViGEmBus
- DualSense vibration from Xbox feedback
- Racing-tuned rumble shaping
- Racing adaptive-trigger profile
- Touchpad mapping for View/Back, Menu/Start, Guide, or disabled
- PS button shortcut for Xbox Game Bar
- Stick deadzone controls for drift cleanup
- Live input, trigger, button, feedback, and drift status panel
- Optional launch on Windows startup

## Download

The standalone Windows build is included at:

```text
artifacts\DualBox-win-x64.zip
```

Extract the zip and run:

```text
DualBox.exe
```

## Requirements

- Windows 10 or 11
- ViGEmBus
- HidHide
- PS5 DualSense or DualSense Edge controller

## Build From Source

From a Windows terminal:

```powershell
cd path\to\DualBox
dotnet restore .\src\DualBox.App\DualBox.App.csproj
dotnet run --project .\src\DualBox.App\DualBox.App.csproj -c Release
```

For a standalone Windows build:

```powershell
.\scripts\build-release.ps1
```

This creates:

```text
artifacts\publish\win-x64\DualBox.exe
artifacts\DualBox-win-x64.zip
```

## Test

```powershell
.\scripts\smoke-test.ps1 -Build
```

```powershell
.\scripts\run-protocol-selftest.ps1
```

## Forza Setup

1. Connect the DualSense over USB for the first test.
2. Open DualBox.
3. Confirm the app says ViGEmBus is found.
4. Set touchpad press to `View / Back`.
5. Keep adaptive triggers set to `Racing`.
6. Click `Start bridge`.
7. Press L2/R2 and confirm the triggers have resistance.
8. Click `Test feedback` and confirm the DualSense vibrates.
9. Press the PS button and confirm Xbox Game Bar opens.
10. Move both sticks and adjust deadzone sliders until `Drift status` says the filtered output is centered while the sticks are untouched.
11. Open `joy.cpl` from Windows Run and confirm the virtual Xbox controller appears.
12. Start the game.
13. If the map does not open from touchpad press, stop the bridge and try `Menu / Start`.

## HidHide setup

HidHide is what prevents double input.

1. Open HidHide Configuration Client.
2. Add `DualBox.exe` to the Applications allow list.
3. On Devices, hide the physical DualSense.
4. Enable device hiding.
5. Restart DualBox.

The game should now only see the virtual Xbox controller.

## Status

- USB input parsing: implemented
- Bluetooth input parsing: implemented
- USB feedback output: implemented
- Bluetooth feedback output: implemented
- Xbox One-style four-channel feedback model: implemented
- XInput compatibility backend: implemented
- Racing-biased rumble shaping: implemented
- Racing adaptive-trigger profile: implemented
- PS button to Game Bar hotkey: implemented
- Start with Windows: implemented via HKCU Run key
- Stick drift deadzone controls: implemented
- True Xbox One virtual device backend: planned
- Touchpad-to-map-style binding: implemented

See [docs/TEST_PLAN.md](docs/TEST_PLAN.md) for the step-by-step Windows validation flow, [docs/TROUBLESHOOTING.md](docs/TROUBLESHOOTING.md) for likely first-test issues, and [docs/PROTOCOL_NOTES.md](docs/PROTOCOL_NOTES.md) for HID report references.

See [docs/RELEASE.md](docs/RELEASE.md) for standalone publishing details.
