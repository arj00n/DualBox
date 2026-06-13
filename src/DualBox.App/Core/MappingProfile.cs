using DualBox.Controller;

namespace DualBox.Core;

public enum TouchpadBinding
{
    Back,
    Start,
    Guide,
    None
}

public sealed record MappingProfile(
    TouchpadBinding TouchpadPress,
    DualSenseTriggerProfile TriggerProfile,
    StickSettings Sticks,
    bool PsButtonOpensGameBar);
