# Troubleshooting

## App says ViGEmBus is missing

The virtual Xbox controller cannot be created until ViGEmBus is installed. Install the official Nefarius ViGEmBus package, restart Windows if prompted, then run `.\scripts\smoke-test.ps1` again.

## Game sees two controllers

Configure HidHide:

1. Add `DualBox.exe` to the allow list.
2. Hide the physical DualSense device.
3. Enable device hiding.
4. Restart DualBox and the game.

If duplicate input remains, open `joy.cpl` and check whether both the physical DualSense and the virtual Xbox controller are visible.

## Controller is not found

Start with USB. Bluetooth support is coded, but USB is the first validation target because the wired report path is simpler and has fewer Windows Bluetooth stack variables.

Then run:

```powershell
.\scripts\smoke-test.ps1
```

If no Sony HID device appears, try another USB-C cable. Some cables are charge-only.

## Sticks or buttons are wrong

Use `joy.cpl` before testing in-game. If `joy.cpl` shows the wrong mapping, fix the bridge. If `joy.cpl` is correct but the game is wrong, adjust the in-game controls or the touchpad binding.

Expected baseline:

- Cross -> A
- Circle -> B
- Square -> X
- Triangle -> Y
- Create -> Back/View
- Options -> Start/Menu
- Touchpad press -> Back/View by default

## Feedback works in the app but not in-game

The game may not be sending controller feedback events, or the virtual controller may not be the active input device. Confirm HidHide is hiding the physical DualSense, then test feedback in another game or controller tester.

## Feedback feels harsh or delayed

The app uses the built-in racing feedback profile in `RumbleProfile.Racing`. Tune these values:

- `LargeMotorGain` for heavy road/impact feel.
- `SmallMotorGain` for sharper texture.
- `TriggerMotorGain` for trigger feedback strength.
- `TextureFromLargeMotor` for extra surface detail.
- `Deadzone` to suppress idle buzz.
- `Smoothing` to reduce chatter at the cost of response speed.

## Adaptive triggers do not work

Make sure the app's adaptive trigger selector is set to `Racing`. The current trigger profile is local controller-side resistance; it is not game-authored PS5 trigger data.

If the triggers still feel normal, use USB for validation first. Bluetooth trigger output is implemented with the shifted Bluetooth report offsets and CRC framing, but USB has fewer variables.

## Xbox One trigger feedback is missing

The app has four-channel Xbox One-style feedback plumbing. The current packaged ViGEm backend exposes XInput feedback, so trigger feedback values remain zero until the true Xbox One virtual device backend is added.

## Settings reset unexpectedly

The app stores settings at `%APPDATA%\DualBox\settings.json`. Delete that file to reset to defaults: touchpad press as View/Back and adaptive triggers as Racing.

## App does not open when Windows starts

Make sure `Open on Windows launch` is checked. The app registers itself in `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` as `DualBox`, which does not require administrator rights.

If you are running from `dotnet run`, publish the app first with `.\scripts\build-release.ps1`, run the published app once, and leave `Open on Windows launch` checked.

## PS button does not open Game Bar

Make sure `PS button opens Game Bar` is checked. DualBox sends `Windows+G` on the first PS-button press event.

If nothing opens, check Windows Settings and make sure Xbox Game Bar is enabled. Some full-screen games or overlays may intercept the shortcut.

## Stick drift is still visible

Raise the deadzone slider for the drifting stick until `Filtered Xbox sticks` returns to `0, 0` while the stick is untouched. Confirm the same result in `joy.cpl`. Keep the value as low as possible; a large deadzone can make steering and camera movement feel sluggish.
