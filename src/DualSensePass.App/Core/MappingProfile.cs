using DualSensePass.Controller;

namespace DualSensePass.Core;

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
