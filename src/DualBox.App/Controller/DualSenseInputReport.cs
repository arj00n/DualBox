namespace DualBox.Controller;

public sealed record DualSenseInputReport(
    byte LeftX,
    byte LeftY,
    byte RightX,
    byte RightY,
    byte LeftTrigger,
    byte RightTrigger,
    DualSenseDpad Dpad,
    bool Square,
    bool Cross,
    bool Circle,
    bool Triangle,
    bool LeftShoulder,
    bool RightShoulder,
    bool LeftTriggerButton,
    bool RightTriggerButton,
    bool Create,
    bool Options,
    bool LeftStickButton,
    bool RightStickButton,
    bool PlayStation,
    bool TouchpadButton,
    bool Mute)
{
    public static bool TryParse(ReadOnlySpan<byte> raw, out DualSenseInputReport report)
    {
        report = default!;

        if (raw.Length < 12)
        {
            return false;
        }

        var reportId = raw[0];
        var offset = reportId switch
        {
            0x01 => 1, // USB input report.
            0x31 => 2, // Bluetooth input report includes an extra tag byte before controls.
            _ => -1
        };

        if (offset < 0 || raw.Length <= offset + 9)
        {
            return false;
        }

        var buttons0 = raw[offset + 7];
        var buttons1 = raw[offset + 8];
        var buttons2 = raw[offset + 9];

        report = new DualSenseInputReport(
            raw[offset],
            raw[offset + 1],
            raw[offset + 2],
            raw[offset + 3],
            raw[offset + 4],
            raw[offset + 5],
            DualSenseDpad.FromNibble((byte)(buttons0 & 0x0F)),
            (buttons0 & 0x10) != 0,
            (buttons0 & 0x20) != 0,
            (buttons0 & 0x40) != 0,
            (buttons0 & 0x80) != 0,
            (buttons1 & 0x01) != 0,
            (buttons1 & 0x02) != 0,
            (buttons1 & 0x04) != 0,
            (buttons1 & 0x08) != 0,
            (buttons1 & 0x10) != 0,
            (buttons1 & 0x20) != 0,
            (buttons1 & 0x40) != 0,
            (buttons1 & 0x80) != 0,
            (buttons2 & 0x01) != 0,
            (buttons2 & 0x02) != 0,
            (buttons2 & 0x04) != 0);

        return true;
    }

    public IEnumerable<string> ActiveButtonNames()
    {
        if (Square) yield return "Square";
        if (Cross) yield return "Cross";
        if (Circle) yield return "Circle";
        if (Triangle) yield return "Triangle";
        if (LeftShoulder) yield return "L1";
        if (RightShoulder) yield return "R1";
        if (LeftTriggerButton) yield return "L2";
        if (RightTriggerButton) yield return "R2";
        if (Create) yield return "Create";
        if (Options) yield return "Options";
        if (LeftStickButton) yield return "L3";
        if (RightStickButton) yield return "R3";
        if (PlayStation) yield return "PS";
        if (TouchpadButton) yield return "Touchpad";
        if (Mute) yield return "Mute";
        if (Dpad.Up) yield return "D-pad Up";
        if (Dpad.Down) yield return "D-pad Down";
        if (Dpad.Left) yield return "D-pad Left";
        if (Dpad.Right) yield return "D-pad Right";
    }
}

public readonly record struct DualSenseDpad(bool Up, bool Down, bool Left, bool Right)
{
    public static DualSenseDpad FromNibble(byte value)
    {
        return value switch
        {
            0 => new DualSenseDpad(true, false, false, false),
            1 => new DualSenseDpad(true, false, false, true),
            2 => new DualSenseDpad(false, false, false, true),
            3 => new DualSenseDpad(false, true, false, true),
            4 => new DualSenseDpad(false, true, false, false),
            5 => new DualSenseDpad(false, true, true, false),
            6 => new DualSenseDpad(false, false, true, false),
            7 => new DualSenseDpad(true, false, true, false),
            _ => new DualSenseDpad(false, false, false, false)
        };
    }
}
