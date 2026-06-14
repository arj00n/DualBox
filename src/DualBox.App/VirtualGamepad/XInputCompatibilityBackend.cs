using DualBox.Controller;
using DualBox.Core;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;

namespace DualBox.VirtualGamepad;

public sealed record XboxGamepadFeedback(
    byte LeftMotor,
    byte RightMotor,
    byte LeftTriggerMotor = 0,
    byte RightTriggerMotor = 0)
{
    public bool HasTriggerFeedback => LeftTriggerMotor > 0 || RightTriggerMotor > 0;
}

public sealed class XInputCompatibilityBackend : IXboxBridgeBackend
{
    private readonly ViGEmClient _client = new();
    private readonly IXbox360Controller _controller;

    public string DisplayName => "XInput compatibility backend";

    public event EventHandler<XboxGamepadFeedback>? FeedbackReceived;

    public XInputCompatibilityBackend()
    {
        _controller = _client.CreateXbox360Controller();
        _controller.AutoSubmitReport = false;
        _controller.FeedbackReceived += OnFeedbackReceived;
    }

    public void Connect()
    {
        _controller.Connect();
    }

    public void Apply(DualSenseInputReport input, MappingProfile profile)
    {
        _controller.SetAxisValue(Xbox360Axis.LeftThumbX, InputMath.StickByteToXInputAxis(input.LeftX, deadzone: profile.Sticks.LeftDeadzone));
        _controller.SetAxisValue(Xbox360Axis.LeftThumbY, InputMath.StickByteToXInputAxis(input.LeftY, invert: true, deadzone: profile.Sticks.LeftDeadzone));
        _controller.SetAxisValue(Xbox360Axis.RightThumbX, InputMath.StickByteToXInputAxis(input.RightX, deadzone: profile.Sticks.RightDeadzone));
        _controller.SetAxisValue(Xbox360Axis.RightThumbY, InputMath.StickByteToXInputAxis(input.RightY, invert: true, deadzone: profile.Sticks.RightDeadzone));
        _controller.SetSliderValue(Xbox360Slider.LeftTrigger, input.LeftTrigger);
        _controller.SetSliderValue(Xbox360Slider.RightTrigger, input.RightTrigger);

        _controller.SetButtonState(Xbox360Button.A, input.Cross);
        _controller.SetButtonState(Xbox360Button.B, input.Circle);
        _controller.SetButtonState(Xbox360Button.X, input.Square);
        _controller.SetButtonState(Xbox360Button.Y, input.Triangle);
        _controller.SetButtonState(Xbox360Button.LeftShoulder, input.LeftShoulder);
        _controller.SetButtonState(Xbox360Button.RightShoulder, input.RightShoulder);
        _controller.SetButtonState(Xbox360Button.LeftThumb, input.LeftStickButton);
        _controller.SetButtonState(Xbox360Button.RightThumb, input.RightStickButton);
        _controller.SetButtonState(Xbox360Button.Back, input.Create || (input.TouchpadButton && profile.TouchpadPress == TouchpadBinding.Back));
        _controller.SetButtonState(Xbox360Button.Start, input.Options || (input.TouchpadButton && profile.TouchpadPress == TouchpadBinding.Start));
        _controller.SetButtonState(
            Xbox360Button.Guide,
            (input.PlayStation && !profile.PsButtonOpensGameBar) ||
            (input.TouchpadButton && profile.TouchpadPress == TouchpadBinding.Guide));

        _controller.SetButtonState(Xbox360Button.Up, input.Dpad.Up);
        _controller.SetButtonState(Xbox360Button.Down, input.Dpad.Down);
        _controller.SetButtonState(Xbox360Button.Left, input.Dpad.Left);
        _controller.SetButtonState(Xbox360Button.Right, input.Dpad.Right);
        _controller.SubmitReport();
    }

    private void OnFeedbackReceived(object? sender, Xbox360FeedbackReceivedEventArgs e)
    {
        FeedbackReceived?.Invoke(this, new XboxGamepadFeedback(e.LargeMotor, e.SmallMotor));
    }

    public void Dispose()
    {
        _controller.FeedbackReceived -= OnFeedbackReceived;
        _controller.Disconnect();
        _client.Dispose();
    }
}
