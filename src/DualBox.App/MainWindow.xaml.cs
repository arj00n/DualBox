using System.Windows;
using System.Windows.Controls;
using DualBox.Core;
using DualBox.Diagnostics;
using DualBox.Controller;
using DualBox.VirtualGamepad;

namespace DualBox;

public partial class MainWindow : Window
{
    private readonly BridgeService _bridge = new();
    private readonly SettingsStore _settingsStore = new();
    private bool _isLoadingSettings = true;

    public MainWindow()
    {
        InitializeComponent();

        _bridge.Log += (_, message) => Dispatcher.Invoke(() => AddLog(message));
        _bridge.StateChanged += (_, state) => Dispatcher.Invoke(() => ApplyState(state));
        _bridge.InputReceived += (_, report) => Dispatcher.Invoke(() => ApplyInput(report));
        _bridge.FeedbackReceived += (_, feedback) => Dispatcher.Invoke(() => ApplyFeedback(feedback));

        Loaded += (_, _) => CheckPrerequisites();
        Closing += async (_, _) => await _bridge.StopAsync();
        LoadSettings();
    }

    private void CheckPrerequisites()
    {
        var prereqs = PrerequisiteChecker.Check();
        PrereqText.Text = prereqs.ToSummary();

        foreach (var warning in prereqs.Warnings)
        {
            AddLog(warning);
        }
    }

    private async void StartButton_OnClick(object sender, RoutedEventArgs e)
    {
        StartButton.IsEnabled = false;

        try
        {
            var settings = GetCurrentSettings().Normalize();
            _settingsStore.Save(settings);
            await _bridge.StartAsync(settings.ToMappingProfile());
        }
        catch (Exception ex)
        {
            AddLog("Start failed: " + ex.Message);
            StartButton.IsEnabled = true;
            StopButton.IsEnabled = false;
        }
    }

    private async void StopButton_OnClick(object sender, RoutedEventArgs e)
    {
        StopButton.IsEnabled = false;
        await _bridge.StopAsync();
    }

    private async void RumbleButton_OnClick(object sender, RoutedEventArgs e)
    {
        RumbleButton.IsEnabled = false;

        try
        {
            await _bridge.TestFeedbackAsync();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            AddLog("Feedback test failed: " + ex.Message);
        }
        finally
        {
            RumbleButton.IsEnabled = _bridge.IsRunning;
        }
    }

    private TouchpadBinding GetTouchpadBinding()
    {
        if (TouchpadMappingCombo.SelectedItem is not ComboBoxItem item || item.Tag is not string tag)
        {
            return TouchpadBinding.Back;
        }

        return Enum.TryParse<TouchpadBinding>(tag, out var value) ? value : TouchpadBinding.Back;
    }

    private DualSenseTriggerProfile GetTriggerProfile()
    {
        if (TriggerProfileCombo.SelectedItem is not ComboBoxItem item || item.Tag is not string tag)
        {
            return DualSenseTriggerProfile.Racing;
        }

        return Enum.TryParse<DualSenseTriggerProfile>(tag, out var value) ? value : DualSenseTriggerProfile.Racing;
    }

    private void SettingsCombo_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SaveSettingsFromUi();
    }

    private void SettingsInput_OnChanged(object sender, RoutedEventArgs e)
    {
        SaveSettingsFromUi();
    }

    private void SettingsSlider_OnChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        SaveSettingsFromUi();
    }

    private void SaveSettingsFromUi()
    {
        if (_isLoadingSettings)
        {
            return;
        }

        var settings = GetCurrentSettings().Normalize();
        UpdateDeadzoneLabels(settings);
        _settingsStore.Save(settings);
        new StartupManager().Apply(settings.StartWithWindows);

        if (_bridge.IsRunning)
        {
            _bridge.ApplyProfile(settings.ToMappingProfile());
        }
    }

    private void ApplyState(BridgeState state)
    {
        StatusText.Text = state.IsRunning ? "Bridge running" : "Idle";
        DeviceText.Text = state.DeviceName ?? "No controller connected";
        StartButton.IsEnabled = !state.IsRunning;
        StopButton.IsEnabled = state.IsRunning;
        RumbleButton.IsEnabled = state.IsRunning;
        TouchpadMappingCombo.IsEnabled = !state.IsRunning;
    }

    private void LoadSettings()
    {
        _isLoadingSettings = true;

        try
        {
            var settings = _settingsStore.Load().Normalize();
            SelectComboTag(TouchpadMappingCombo, settings.TouchpadPress.ToString());
            SelectComboTag(TriggerProfileCombo, settings.TriggerProfile.ToString());
            StartWithWindowsCheck.IsChecked = settings.StartWithWindows;
            PsGameBarCheck.IsChecked = settings.PsButtonOpensGameBar;
            LeftDeadzoneSlider.Value = settings.LeftStickDeadzone;
            RightDeadzoneSlider.Value = settings.RightStickDeadzone;
            UpdateDeadzoneLabels(settings);
            new StartupManager().Apply(settings.StartWithWindows);
        }
        finally
        {
            _isLoadingSettings = false;
        }
    }

    private AppSettings GetCurrentSettings()
    {
        return new AppSettings(
            GetTouchpadBinding(),
            GetTriggerProfile(),
            StartWithWindowsCheck.IsChecked == true,
            PsGameBarCheck.IsChecked == true,
            LeftDeadzoneSlider.Value,
            RightDeadzoneSlider.Value).Normalize();
    }

    private static void SelectComboTag(ComboBox combo, string tag)
    {
        foreach (var item in combo.Items.OfType<ComboBoxItem>())
        {
            if (string.Equals(item.Tag as string, tag, StringComparison.OrdinalIgnoreCase))
            {
                combo.SelectedItem = item;
                return;
            }
        }
    }

    private void ApplyInput(DualSenseInputReport report)
    {
        LeftStickText.Text = $"Left stick: {report.LeftX}, {report.LeftY}";
        RightStickText.Text = $"Right stick: {report.RightX}, {report.RightY}";
        var settings = GetCurrentSettings();
        var left = InputMath.ToXInputStick(report.LeftX, report.LeftY, settings.LeftStickDeadzone);
        var right = InputMath.ToXInputStick(report.RightX, report.RightY, settings.RightStickDeadzone);
        FilteredStickText.Text = $"Filtered Xbox sticks: L {left.X}, {left.Y} | R {right.X}, {right.Y}";
        DriftStatusText.Text = left.IsCentered && right.IsCentered
            ? "Drift status: filtered output centered"
            : "Drift status: filtered output still moving";
        TriggerText.Text = $"Triggers: L2 {report.LeftTrigger}, R2 {report.RightTrigger}";
        LeftTriggerBar.Value = report.LeftTrigger;
        RightTriggerBar.Value = report.RightTrigger;
        ButtonText.Text = "Buttons: " + string.Join(", ", report.ActiveButtonNames());
    }

    private void UpdateDeadzoneLabels(AppSettings settings)
    {
        LeftDeadzoneText.Text = $"Left deadzone: {settings.LeftStickDeadzone:P0}";
        RightDeadzoneText.Text = $"Right deadzone: {settings.RightStickDeadzone:P0}";
    }

    private void ApplyFeedback(XboxGamepadFeedback feedback)
    {
        RumbleText.Text =
            $"Last feedback: left {feedback.LeftMotor}, right {feedback.RightMotor}, LT {feedback.LeftTriggerMotor}, RT {feedback.RightTriggerMotor}";
    }

    private void AddLog(string message)
    {
        LogList.Items.Insert(0, $"{DateTime.Now:HH:mm:ss}  {message}");

        while (LogList.Items.Count > 120)
        {
            LogList.Items.RemoveAt(LogList.Items.Count - 1);
        }
    }
}
