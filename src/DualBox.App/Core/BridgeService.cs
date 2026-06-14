using DualBox.Controller;
using DualBox.VirtualGamepad;

namespace DualBox.Core;

public sealed class BridgeService : IAsyncDisposable
{
    private CancellationTokenSource? _cts;
    private Task? _readLoop;
    private DualSenseHidDevice? _dualSense;
    private IXboxBridgeBackend? _xboxBackend;
    private readonly RumbleTranslator _rumbleTranslator = new(RumbleProfile.Racing);
    private MappingProfile _profile = AppSettings.Default.ToMappingProfile();

    public bool IsRunning => _cts is not null;

    public event EventHandler<string>? Log;
    public event EventHandler<BridgeState>? StateChanged;
    public event EventHandler<DualSenseInputReport>? InputReceived;
    public event EventHandler<XboxGamepadFeedback>? FeedbackReceived;

    public async Task StartAsync(MappingProfile profile)
    {
        if (_cts is not null)
        {
            return;
        }

        _profile = profile;
        var deviceInfo = DualSenseDiscovery.FindFirst()
            ?? throw new InvalidOperationException("No PS5 DualSense controller was found. Plug it in over USB first for the initial test.");

        _dualSense = DualSenseHidDevice.Open(deviceInfo);
        _dualSense.SetTriggerState(GetTriggerState(profile.TriggerProfile));
        _xboxBackend = new XInputCompatibilityBackend();
        _xboxBackend.FeedbackReceived += ForwardFeedback;
        _xboxBackend.Connect();

        _cts = new CancellationTokenSource();
        _readLoop = Task.Run(() => ReadLoop(_cts.Token), _cts.Token);

        LogMessage("Connected to " + deviceInfo.DisplayName);
        LogMessage("Xbox One bridge is online using " + _xboxBackend.DisplayName + ".");
        PublishState(true, deviceInfo.DisplayName);

        await Task.CompletedTask;
    }

    public async Task TestFeedbackAsync()
    {
        if (_dualSense is null)
        {
            throw new InvalidOperationException("Start the bridge before testing feedback.");
        }

        LogMessage("Running Xbox One-style feedback test.");

        try
        {
            _dualSense.SetRumble(smallMotor: 96, largeMotor: 192);
            FeedbackReceived?.Invoke(this, new XboxGamepadFeedback(192, 96, 128, 128));
            await Task.Delay(450, _cts?.Token ?? CancellationToken.None);
        }
        finally
        {
            try
            {
                _dualSense?.SetRumble(0, 0);
            }
            catch
            {
                // Best effort only; the stream may be closing.
            }
        }
    }

    public void ApplyTriggerProfile(DualSenseTriggerProfile profile)
    {
        _dualSense?.SetTriggerState(GetTriggerState(profile));
        LogMessage("Trigger profile set to " + profile);
    }

    public void ApplyProfile(MappingProfile profile)
    {
        _profile = profile;
        _dualSense?.SetTriggerState(GetTriggerState(profile.TriggerProfile));
        LogMessage("Controller profile updated.");
    }

    public async Task StopAsync()
    {
        if (_cts is null)
        {
            return;
        }

        _cts.Cancel();
        _dualSense?.Dispose();

        if (_readLoop is not null)
        {
            try
            {
                await _readLoop;
            }
            catch (OperationCanceledException)
            {
            }
        }

        if (_xboxBackend is not null)
        {
            _xboxBackend.FeedbackReceived -= ForwardFeedback;
            _xboxBackend.Dispose();
        }

        _rumbleTranslator.Reset();
        _xboxBackend = null;
        _dualSense = null;
        _readLoop = null;
        _cts.Dispose();
        _cts = null;

        LogMessage("Bridge stopped.");
        PublishState(false, null);
    }

    private void ReadLoop(CancellationToken token)
    {
        if (_dualSense is null || _xboxBackend is null)
        {
            return;
        }

        while (!token.IsCancellationRequested)
        {
            try
            {
                var report = _dualSense.ReadInput(token);
                if (report is null)
                {
                    continue;
                }

                HandleSystemShortcuts(report);
                _xboxBackend.Apply(report, _profile);
                InputReceived?.Invoke(this, report);
            }
            catch when (token.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                LogMessage("Controller read failed: " + ex.Message);
                return;
            }
        }
    }

    private void ForwardFeedback(object? sender, XboxGamepadFeedback feedback)
    {
        FeedbackReceived?.Invoke(this, feedback);

        try
        {
            var dualSenseRumble = _rumbleTranslator.Translate(feedback);
            _dualSense?.SetRumble(dualSenseRumble.SmallMotor, dualSenseRumble.LargeMotor);
        }
        catch (Exception ex)
        {
            LogMessage("Feedback forwarding failed: " + ex.Message);
        }
    }

    private bool _wasPlayStationPressed;

    private void HandleSystemShortcuts(DualSenseInputReport report)
    {
        if (_profile.PsButtonOpensGameBar && report.PlayStation && !_wasPlayStationPressed)
        {
            GameBarHotkey.Open();
            LogMessage("PS button pressed: sent Windows+G for Game Bar.");
        }

        _wasPlayStationPressed = report.PlayStation;
    }

    private static DualSenseTriggerState GetTriggerState(DualSenseTriggerProfile profile)
    {
        return profile switch
        {
            DualSenseTriggerProfile.Racing => DualSenseTriggerState.Racing,
            _ => DualSenseTriggerState.None
        };
    }

    private void PublishState(bool isRunning, string? deviceName)
    {
        StateChanged?.Invoke(this, new BridgeState(isRunning, deviceName));
    }

    private void LogMessage(string message)
    {
        Log?.Invoke(this, message);
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }
}
