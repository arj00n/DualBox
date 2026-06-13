namespace DualBox.Controller;

public enum DualSenseTriggerProfile
{
    Off,
    Racing
}

public readonly record struct DualSenseTriggerEffect(
    byte Mode,
    byte StartResistance,
    byte EffectForce,
    byte RangeForce,
    byte NearReleaseStrength,
    byte NearMiddleStrength,
    byte PressedStrength,
    byte ActuationFrequency)
{
    public static DualSenseTriggerEffect None { get; } = new(0, 0, 0, 0, 0, 0, 0, 0);

    public static DualSenseTriggerEffect SectionResistance(byte startResistance, byte effectForce, byte rangeForce)
    {
        return new DualSenseTriggerEffect(
            Mode: 0x02,
            StartResistance: startResistance,
            EffectForce: effectForce,
            RangeForce: rangeForce,
            NearReleaseStrength: 0,
            NearMiddleStrength: 0,
            PressedStrength: 0,
            ActuationFrequency: 0);
    }
}

public readonly record struct DualSenseTriggerState(DualSenseTriggerEffect Left, DualSenseTriggerEffect Right)
{
    public static DualSenseTriggerState None { get; } = new(DualSenseTriggerEffect.None, DualSenseTriggerEffect.None);

    public static DualSenseTriggerState Racing { get; } = new(
        Left: DualSenseTriggerEffect.SectionResistance(startResistance: 0x44, effectForce: 0xB8, rangeForce: 0xFF),
        Right: DualSenseTriggerEffect.SectionResistance(startResistance: 0x22, effectForce: 0x88, rangeForce: 0xD0));
}
