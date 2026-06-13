# DualSense Pass Test Plan

Use this on the Windows machine that has the DualSense and Game Pass installed.

## Phase 1: Driver and build

1. Open PowerShell in the project root.
2. Run `.\scripts\run-protocol-selftest.ps1`.
3. Run `.\scripts\smoke-test.ps1 -Build`.
4. Run `.\scripts\build-release.ps1`.
5. Confirm:
   - .NET is found.
   - ViGEmBus is found.
   - HidHide is either found or intentionally not configured yet.
   - A Sony HID device appears after connecting the DualSense.
   - `artifacts\publish\win-x64\DualSensePass.exe` exists.

## Phase 2: Controller bridge

1. Run `dotnet run --project .\src\DualSensePass.App\DualSensePass.App.csproj -c Release`.
2. Click `Start bridge`.
3. Confirm the app shows `Bridge running`.
4. Keep adaptive triggers set to `Racing`.
5. Press L2 and R2 and confirm they have resistance.
6. Change adaptive triggers to `Off`, then confirm L2/R2 return to normal.
7. Change adaptive triggers back to `Racing`.
8. Click `Test rumble`.
9. Confirm the DualSense vibrates briefly.
10. Press the PS button.
11. Confirm Xbox Game Bar opens.
12. Leave both sticks untouched and watch the live stick values.
13. Raise the left/right deadzone sliders until `Filtered Xbox sticks` returns to `0, 0` and `Drift status` says the filtered output is centered while the sticks are untouched.
14. Open `joy.cpl`.
15. Confirm an Xbox 360 controller appears.
16. Open the Xbox controller properties and verify:
   - Left and right sticks move correctly.
   - L2 and R2 move the trigger bars.
   - Cross maps to A.
   - Circle maps to B.
   - Square maps to X.
   - Triangle maps to Y.
   - Touchpad press maps to Back/View by default.
   - The adjusted deadzone suppresses stick drift at rest.

## Phase 3: HidHide

1. Open HidHide Configuration Client.
2. Add the running `DualSensePass.exe` path to the Applications list.
3. Hide the physical DualSense device.
4. Enable device hiding.
5. Restart DualSense Pass.
6. Open `joy.cpl` again and confirm the game-facing device is the virtual Xbox controller.

## Phase 3.5: Startup behavior

1. Confirm `Open on Windows launch` is checked.
2. Restart Windows or sign out and back in.
3. Confirm DualSense Pass opens automatically.
4. If it does not, check `%APPDATA%\DualSensePass\settings.json` and the `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` registry key.

## Phase 4: Forza validation

1. Keep DualSense Pass running.
2. Launch the game through Xbox/Game Pass.
3. Verify steering, throttle, brake, camera, face buttons, pause/menu, and map behavior.
4. If touchpad press does not open the map, stop the bridge and try `Menu / Start`.
5. Drive for at least five minutes and note:
   - Road texture vibration.
   - Collision vibration.
   - Engine vibration.
   - Brake-trigger resistance on L2.
   - Throttle-trigger resistance on R2.
   - Whether PS button reliably opens Game Bar without interfering with driving.
   - Whether deadzone settings eliminate idle steering/camera drift.
   - Whether the motors feel too weak, harsh, or delayed.

## Known limits

The bridge translates XInput rumble into DualSense-compatible motor output. Native DualSense adaptive triggers and PS5 HD haptic effects require game-specific DualSense output, which XInput-only Game Pass titles generally do not provide.
