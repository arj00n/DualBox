namespace DualSensePass.Core;

public sealed record StickSettings(double LeftDeadzone, double RightDeadzone)
{
    public static StickSettings Default { get; } = new(0.07, 0.07);
}
