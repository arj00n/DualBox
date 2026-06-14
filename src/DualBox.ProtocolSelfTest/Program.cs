using DualBox.Controller;
using DualBox.Core;
using DualBox.VirtualGamepad;

var tests = new ProtocolTests();
tests.ParseUsbInputReport();
tests.ParseBluetoothInputReport();
tests.ShapeRacingRumble();
tests.FilterStickDrift();

Console.WriteLine("Protocol self-test passed.");

internal sealed class ProtocolTests
{
    public void ParseUsbInputReport()
    {
        var raw = new byte[64];
        raw[0] = 0x01;
        WriteControls(raw, offset: 1);

        Assert(DualSenseInputReport.TryParse(raw, out var report), "USB report should parse.");
        AssertParsedControls(report, "USB");
    }

    public void ParseBluetoothInputReport()
    {
        var raw = new byte[78];
        raw[0] = 0x31;
        raw[1] = 0x7A;
        WriteControls(raw, offset: 2);

        Assert(DualSenseInputReport.TryParse(raw, out var report), "Bluetooth report should parse.");
        AssertParsedControls(report, "Bluetooth");
    }

    public void ShapeRacingRumble()
    {
        var translator = new RumbleTranslator(RumbleProfile.Racing);
        var shaped = translator.Translate(new XboxGamepadFeedback(LeftMotor: 255, RightMotor: 90));

        Assert(shaped.LargeMotor > 0, "Large DualSense motor should be active.");
        Assert(shaped.SmallMotor > 0, "Small DualSense motor should be active.");
        Assert(shaped.SmallMotor > 90, "Racing profile should blend road texture into the small motor.");

        translator.Reset();
        var idle = translator.Translate(new XboxGamepadFeedback(0, 0));
        Assert(idle.LargeMotor == 0 && idle.SmallMotor == 0, "Zero rumble should stay zero.");
    }

    public void FilterStickDrift()
    {
        var centered = InputMath.ToXInputStick(130, 127, deadzone: 0.07);
        Assert(centered.IsCentered, "Small stick noise should be filtered.");

        var moving = InputMath.ToXInputStick(190, 127, deadzone: 0.07);
        Assert(!moving.IsCentered && moving.X > 0, "Real stick movement should pass through.");
    }

    private static void WriteControls(byte[] raw, int offset)
    {
        raw[offset] = 12;
        raw[offset + 1] = 245;
        raw[offset + 2] = 34;
        raw[offset + 3] = 210;
        raw[offset + 4] = 111;
        raw[offset + 5] = 222;
        raw[offset + 7] = 0x10 | 0x20 | 0x01;
        raw[offset + 8] = 0x01 | 0x02 | 0x04 | 0x08 | 0x10 | 0x20 | 0x40 | 0x80;
        raw[offset + 9] = 0x01 | 0x02 | 0x04;
    }

    private static void AssertParsedControls(DualSenseInputReport report, string name)
    {
        Assert(report.LeftX == 12, $"{name} LeftX.");
        Assert(report.LeftY == 245, $"{name} LeftY.");
        Assert(report.RightX == 34, $"{name} RightX.");
        Assert(report.RightY == 210, $"{name} RightY.");
        Assert(report.LeftTrigger == 111, $"{name} L2 analog.");
        Assert(report.RightTrigger == 222, $"{name} R2 analog.");
        Assert(report.Square && report.Cross, $"{name} face buttons.");
        Assert(report.Dpad.Up && report.Dpad.Right && !report.Dpad.Down && !report.Dpad.Left, $"{name} diagonal D-pad.");
        Assert(report.LeftShoulder && report.RightShoulder, $"{name} shoulders.");
        Assert(report.LeftTriggerButton && report.RightTriggerButton, $"{name} trigger buttons.");
        Assert(report.Create && report.Options, $"{name} create/options.");
        Assert(report.LeftStickButton && report.RightStickButton, $"{name} stick buttons.");
        Assert(report.PlayStation && report.TouchpadButton && report.Mute, $"{name} system buttons.");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
