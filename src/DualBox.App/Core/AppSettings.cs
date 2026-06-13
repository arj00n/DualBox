using DualBox.Controller;

namespace DualBox.Core;

public sealed record AppSettings(
    TouchpadBinding TouchpadPress,
    DualSenseTriggerProfile TriggerProfile,
    bool StartWithWindows,
    bool PsButtonOpensGameBar,
    double LeftStickDeadzone,
    double RightStickDeadzone)
{
    public static AppSettings Default { get; } = new(
        TouchpadPress: TouchpadBinding.Back,
        TriggerProfile: DualSenseTriggerProfile.Racing,
        StartWithWindows: true,
        PsButtonOpensGameBar: true,
        LeftStickDeadzone: StickSettings.Default.LeftDeadzone,
        RightStickDeadzone: StickSettings.Default.RightDeadzone);

    public MappingProfile ToMappingProfile()
    {
        var normalized = Normalize();

        return new MappingProfile(
            normalized.TouchpadPress,
            normalized.TriggerProfile,
            new StickSettings(normalized.LeftStickDeadzone, normalized.RightStickDeadzone),
            normalized.PsButtonOpensGameBar);
    }

    public AppSettings Normalize()
    {
        return this with
        {
            LeftStickDeadzone = ClampDeadzone(LeftStickDeadzone, Default.LeftStickDeadzone),
            RightStickDeadzone = ClampDeadzone(RightStickDeadzone, Default.RightStickDeadzone)
        };
    }

    private static double ClampDeadzone(double value, double fallback)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return fallback;
        }

        return Math.Clamp(value, 0, 0.25);
    }
}
