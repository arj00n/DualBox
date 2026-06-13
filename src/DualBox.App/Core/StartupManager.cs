using Microsoft.Win32;

namespace DualBox.Core;

public sealed class StartupManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "DualBox";

    public void Apply(bool enabled)
    {
        if (enabled)
        {
            Enable();
        }
        else
        {
            Disable();
        }
    }

    private static void Enable()
    {
        var executablePath = Environment.ProcessPath;

        if (string.IsNullOrWhiteSpace(executablePath))
        {
            return;
        }

        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
        key?.SetValue(ValueName, Quote(executablePath), RegistryValueKind.String);
    }

    private static void Disable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        key?.DeleteValue(ValueName, throwOnMissingValue: false);
    }

    private static string Quote(string path)
    {
        return "\"" + path.Replace("\"", "\\\"") + "\"";
    }
}
