namespace DualBox.Core;

public sealed record RumbleProfile(
    double LargeMotorGain,
    double SmallMotorGain,
    double TextureFromLargeMotor,
    double Deadzone,
    double Smoothing)
{
    public static RumbleProfile Racing { get; } = new(
        LargeMotorGain: 1.10,
        SmallMotorGain: 0.95,
        TextureFromLargeMotor: 0.18,
        Deadzone: 0.035,
        Smoothing: 0.28);
}
