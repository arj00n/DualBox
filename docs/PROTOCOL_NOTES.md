# Protocol Notes

These notes record the HID report facts used by DualSense Pass. They are here so the hardware-specific bytes are auditable during Windows testing.

## References

- DS4Windows active source fork: `ds4windowsapp/DS4Windows`
- `DS4Windows/DS4Library/InputDevices/DualSenseDevice.cs`
- `DS4Windows/DS4Library/InputDevices/TriggerEffects.cs`
- ViGEm.NET source: `nefarius/ViGEm.NET`

The DS4Windows files are GPL-licensed. DualSense Pass uses them only as a public protocol reference for report offsets and mode values; code was not copied.

## Output reports

USB output uses report ID `0x02`.

Bluetooth output uses report ID `0x31`, data byte `0x02`, and a CRC32 trailer calculated over a synthetic `0xA2` HID output prefix followed by the report bytes excluding the last four CRC bytes.

## Main motors

USB:

- Byte `3`: right/light/fast motor
- Byte `4`: left/heavy/slow motor

Bluetooth:

- Byte `4`: right/light/fast motor
- Byte `5`: left/heavy/slow motor

## Adaptive triggers

USB:

- Right trigger effect begins at byte `11`.
- Left trigger effect begins at byte `22`.

Bluetooth:

- Right trigger effect begins at byte `12`.
- Left trigger effect begins at byte `23`.

The current racing profile uses section resistance mode `0x02`, with stronger resistance on L2/brake than R2/throttle.
