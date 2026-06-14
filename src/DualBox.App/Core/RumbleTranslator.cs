using DualBox.Controller;
using DualBox.VirtualGamepad;

namespace DualBox.Core;

public sealed class RumbleTranslator
{
    private readonly RumbleProfile _profile;
    private double _lastSmall;
    private double _lastLarge;

    public RumbleTranslator(RumbleProfile profile)
    {
        _profile = profile;
    }

    public DualSenseRumble Translate(XboxGamepadFeedback feedback)
    {
        var large = Scale(feedback.LeftMotor, _profile.LargeMotorGain);
        var small = Scale(feedback.RightMotor, _profile.SmallMotorGain);
        var leftTrigger = Scale(feedback.LeftTriggerMotor, _profile.TriggerMotorGain);
        var rightTrigger = Scale(feedback.RightTriggerMotor, _profile.TriggerMotorGain);

        small = Math.Clamp(small + large * _profile.TextureFromLargeMotor + rightTrigger * 0.45, 0, 1);
        large = Math.Clamp(large + leftTrigger * 0.35, 0, 1);

        if (large < _profile.Deadzone)
        {
            large = 0;
        }

        if (small < _profile.Deadzone)
        {
            small = 0;
        }

        large = Smooth(_lastLarge, large);
        small = Smooth(_lastSmall, small);
        _lastLarge = large;
        _lastSmall = small;

        return new DualSenseRumble(ToByte(small), ToByte(large));
    }

    public void Reset()
    {
        _lastSmall = 0;
        _lastLarge = 0;
    }

    private double Smooth(double previous, double current)
    {
        return previous + (current - previous) * (1 - _profile.Smoothing);
    }

    private static double Scale(byte value, double gain)
    {
        var normalized = value / 255.0;
        return Math.Clamp(normalized * gain, 0, 1);
    }

    private static byte ToByte(double value)
    {
        return (byte)Math.Clamp(Math.Round(value * 255), 0, 255);
    }
}
