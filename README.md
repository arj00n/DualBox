# DualBox

DualBox is a Windows app that turns a PS5 DualSense or DualSense Edge into a virtual Xbox controller for Game Pass and other XInput-first PC games.

The first target is the out-of-box feel you want for Forza:

- DualSense input is read directly over HID.
- A virtual Xbox 360 controller is created through ViGEmBus.
- XInput rumble feedback is translated back to DualSense motor output.
- A racing-biased rumble translator smooths and shapes vibration for road/engine-style feedback.
- A racing adaptive-trigger profile adds brake/throttle resistance directly on the DualSense.
- Touchpad press defaults to Xbox View/Back, which is a good first guess for map-style actions.
- Touchpad press can be changed to Menu/Start, Guide, or disabled in the app before starting the bridge.
- PS button presses can open Xbox Game Bar by sending `Windows+G`.
- Left and right stick deadzone sliders help compensate for stick drift.
- The live testing area shows raw sticks, filtered Xbox stick output, buttons, triggers, trigger bars, drift status, and recent rumble.
- Touchpad and adaptive-trigger choices are saved under the user's Windows app data folder.
- The app registers itself to open on Windows launch by default.
- HidHide is recommended so games see only the virtual Xbox controller, not both controllers.

## Reality check

This app can make a DualSense behave like a supported Xbox controller to Game Pass games. It can translate normal XInput vibration to the DualSense motors.

It cannot create true PS5 adaptive-trigger effects or Sony game-specific HD haptics from nothing if the PC game only sends ordinary XInput rumble. That information is not present in the Xbox controller API. For games that expose native DualSense output, a separate passthrough mode can be added later, but that is a different path than Xbox emulation.

## Windows prerequisites

Install these before building:

1. .NET 8 SDK
2. ViGEmBus, so the virtual Xbox controller can exist
3. HidHide, so the physical DualSense can be hidden from games while this app is allowed to read it

The published standalone app does not require the .NET runtime on the gaming PC, but ViGEmBus and HidHide are still driver-level dependencies.

ViGEmBus is retired, but it is still the common signed virtual-controller bus used by tools such as DS4Windows. Avoid random download mirrors; use the official Nefarius/GitHub sources.

## Build

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

For a prerequisite and build check:

```powershell
.\scripts\smoke-test.ps1 -Build
```

To run parser and rumble-shaping checks without controller hardware:

```powershell
.\scripts\run-protocol-selftest.ps1
```

## First Forza test

1. Connect the DualSense over USB for the first test.
2. Open DualBox.
3. Confirm the app says ViGEmBus is found.
4. Set touchpad press to `View / Back`.
5. Keep adaptive triggers set to `Racing`.
6. Click `Start bridge`.
7. Press L2/R2 and confirm the triggers have resistance.
8. Click `Test rumble` and confirm the DualSense vibrates.
9. Press the PS button and confirm Xbox Game Bar opens.
10. Move both sticks and adjust deadzone sliders until `Drift status` says the filtered output is centered while the sticks are untouched.
11. Open `joy.cpl` from Windows Run and confirm an Xbox 360 controller appears.
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

## Current implementation status

- USB input parsing: implemented
- Bluetooth input parsing: implemented by report offset, needs Windows hardware validation
- USB rumble output: implemented
- Bluetooth rumble output: implemented with CRC report framing, needs Windows hardware validation
- Racing-biased rumble shaping: implemented
- Racing adaptive-trigger profile: implemented, needs Windows hardware validation
- PS button to Game Bar hotkey: implemented, needs Windows validation
- Start with Windows: implemented via HKCU Run key
- Stick drift deadzone controls: implemented
- Virtual Xbox 360 output: implemented
- Touchpad-to-map-style binding: implemented
- Installer/MSIX: not implemented yet

## Next engineering steps

1. Build on Windows and fix any package/API drift from `Nefarius.ViGEm.Client`.
2. Validate DualSense USB report parsing in the live input panel.
3. Validate rumble in `joy.cpl` and in Forza.
4. Add a controller calibration screen for stick dead zones and trigger curves.
5. Tune brake/throttle resistance after the first real Forza drive.
6. Add per-game profiles once the Forza mapping is confirmed.

See [docs/TEST_PLAN.md](docs/TEST_PLAN.md) for the step-by-step Windows validation flow, [docs/TROUBLESHOOTING.md](docs/TROUBLESHOOTING.md) for likely first-test issues, and [docs/PROTOCOL_NOTES.md](docs/PROTOCOL_NOTES.md) for HID report references.

See [docs/RELEASE.md](docs/RELEASE.md) for standalone publishing details.
