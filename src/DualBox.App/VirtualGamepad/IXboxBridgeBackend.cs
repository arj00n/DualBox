using DualBox.Controller;
using DualBox.Core;

namespace DualBox.VirtualGamepad;

public interface IXboxBridgeBackend : IDisposable
{
    string DisplayName { get; }
    event EventHandler<XboxGamepadFeedback>? FeedbackReceived;

    void Connect();
    void Apply(DualSenseInputReport input, MappingProfile profile);
}

