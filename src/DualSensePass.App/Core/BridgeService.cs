using DualSensePass.Controller;
using DualSensePass.VirtualGamepad;

namespace DualSensePass.Core;

public sealed class BridgeService : IAsyncDisposable
{
    private CancellationTokenSource? _cts;
    private Task? _readLoop;
    private DualSenseHidDevice? _dualSense;
    private VirtualXboxController? _virtualXbox;
    private readonly RumbleTranslator _rumbleTranslator = new(RumbleProfile.Racing);
    private MappingProfile _profile = AppSettings.Default.ToMappingProfile();

    public bool IsRunning => _cts is not null;

    public event EventHandler<string>? Log;
    public event EventHandler<BridgeState>? StateChanged;
    public event EventHandler<DualSenseInputReport>? InputReceived;
    public event EventHandler<XboxRumble>? RumbleReceived;

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
        _virtualXbox = new VirtualXboxController();
        _virtualXbox.RumbleReceived += ForwardRumble;
        _virtualXbox.Connect();

        _cts = new CancellationTokenSource();
        _readLoop = Task.Run(() => ReadLoop(_cts.Token), _cts.Token);

        LogMessage("Connected to " + deviceInfo.DisplayName);
        LogMessage("Virtual Xbox 360 controller is online.");
        PublishState(true, deviceInfo.DisplayName);

        await Task.CompletedTask;
    }

    public async Task TestRumbleAsync()
    {
        if (_dualSense is null)
        {
            throw new InvalidOperationException("Start the bridge before testing rumble.");
        }

        LogMessage("Running DualSense rumble test.");

        try
        {
            _dualSense.SetRumble(smallMotor: 96, largeMotor: 192);
            RumbleReceived?.Invoke(this, new XboxRumble(192, 96));
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

        if (_virtualXbox is not null)
        {
            _virtualXbox.RumbleReceived -= ForwardRumble;
            _virtualXbox.Dispose();
        }

        _rumbleTranslator.Reset();
        _virtualXbox = null;
        _dualSense = null;
        _readLoop = null;
        _cts.Dispose();
        _cts = null;

        LogMessage("Bridge stopped.");
        PublishState(false, null);
    }

    private void ReadLoop(CancellationToken token)
    {
        if (_dualSense is null || _virtualXbox is null)
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
                _virtualXbox.Apply(report, _profile);
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

    private void ForwardRumble(object? sender, XboxRumble rumble)
    {
        RumbleReceived?.Invoke(this, rumble);

        try
        {
            var dualSenseRumble = _rumbleTranslator.Translate(rumble);
            _dualSense?.SetRumble(dualSenseRumble.SmallMotor, dualSenseRumble.LargeMotor);
        }
        catch (Exception ex)
        {
            LogMessage("Rumble forwarding failed: " + ex.Message);
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
