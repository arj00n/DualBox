# DualBox Virtual Pad Driver

DualBoxVirtualPad is the WDK/KMDF driver scaffold for the true Xbox One bridge path.

## Shape

- `drivers/DualBoxVirtualPad`: KMDF driver project
- VHF exposes the virtual HID gamepad to Windows
- `IOCTL_DUALBOX_SUBMIT_INPUT` feeds controller state from the app into the driver
- `IOCTL_DUALBOX_GET_FEEDBACK` lets the app read the latest feedback report
- Feedback model: left motor, right motor, left trigger motor, right trigger motor
- The WPF app tries this driver backend first, then falls back to ViGEm/XInput

## Build

Use a Windows machine with:

- Visual Studio 2022
- Windows 11 SDK
- Windows Driver Kit

```powershell
.\scripts\build-driver.ps1
```

## Test Install

On a Windows test machine:

```powershell
bcdedit /set testsigning on
shutdown /r /t 0
```

After reboot, build the driver and install the INF from the output package with Device Manager or `pnputil`.

## Next Work

1. Validate the VHF descriptor in Device Manager and `joy.cpl`.
2. Install the test-signed driver and confirm the app picks `DualBox virtual pad driver`.
3. Capture feedback reports from Forza and tune the DualSense translation.
4. Move from test signing to Microsoft-signed packaging.
