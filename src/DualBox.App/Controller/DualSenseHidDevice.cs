using System.Buffers.Binary;
using HidSharp;

namespace DualBox.Controller;

public sealed class DualSenseHidDevice : IDisposable
{
    private readonly HidStream _stream;
    private readonly DualSenseConnection _connection;
    private readonly object _writeLock = new();
    private readonly byte[] _readBuffer;
    private DualSenseRumble _rumble;
    private DualSenseTriggerState _triggerState = DualSenseTriggerState.None;

    private DualSenseHidDevice(HidStream stream, DualSenseConnection connection, int inputReportLength)
    {
        _stream = stream;
        _connection = connection;
        _readBuffer = new byte[Math.Max(128, inputReportLength)];
    }

    public static DualSenseHidDevice Open(DualSenseDeviceInfo info)
    {
        var stream = info.Device.Open();
        stream.ReadTimeout = Timeout.Infinite;
        stream.WriteTimeout = 250;
        return new DualSenseHidDevice(stream, info.Connection, info.Device.GetMaxInputReportLength());
    }

    public DualSenseInputReport? ReadInput(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        var bytesRead = _stream.Read(_readBuffer);

        if (bytesRead <= 0)
        {
            return null;
        }

        return DualSenseInputReport.TryParse(_readBuffer.AsSpan(0, bytesRead), out var report)
            ? report
            : null;
    }

    public void SetRumble(byte smallMotor, byte largeMotor)
    {
        lock (_writeLock)
        {
            _rumble = new DualSenseRumble(smallMotor, largeMotor);
            WriteOutputReport();
        }
    }

    public void SetTriggerState(DualSenseTriggerState state)
    {
        lock (_writeLock)
        {
            _triggerState = state;
            WriteOutputReport();
        }
    }

    private void WriteOutputReport()
    {
        var report = _connection == DualSenseConnection.Bluetooth
            ? CreateBluetoothOutputReport(_rumble, _triggerState)
            : CreateUsbOutputReport(_rumble, _triggerState);

        _stream.Write(report);
    }

    private static byte[] CreateUsbOutputReport(DualSenseRumble rumble, DualSenseTriggerState triggers)
    {
        var report = new byte[48];
        report[0] = 0x02;
        report[1] = 0x0F;
        report[2] = 0x55;
        report[3] = rumble.SmallMotor;
        report[4] = rumble.LargeMotor;
        WriteTriggerEffect(report, 11, triggers.Right);
        WriteTriggerEffect(report, 22, triggers.Left);
        report[37] = 0x00;
        report[39] = 0x02;
        report[42] = 0x02;
        report[43] = 0x02;
        report[44] = 0x04;
        return report;
    }

    private static byte[] CreateBluetoothOutputReport(DualSenseRumble rumble, DualSenseTriggerState triggers)
    {
        var report = new byte[78];
        report[0] = 0x31;
        report[1] = 0x02;
        report[2] = 0x0F;
        report[3] = 0x55;
        report[4] = rumble.SmallMotor;
        report[5] = rumble.LargeMotor;
        WriteTriggerEffect(report, 12, triggers.Right);
        WriteTriggerEffect(report, 23, triggers.Left);
        report[38] = 0x00;
        report[40] = 0x02;
        report[43] = 0x02;
        report[44] = 0x02;
        report[45] = 0x04;

        Span<byte> crcInput = stackalloc byte[report.Length - 3];
        crcInput[0] = 0xA2;
        report.AsSpan(0, report.Length - 4).CopyTo(crcInput[1..]);
        var crc = Crc32.Compute(crcInput);
        BinaryPrimitives.WriteUInt32LittleEndian(report.AsSpan(report.Length - 4), crc);
        return report;
    }

    private static void WriteTriggerEffect(byte[] report, int offset, DualSenseTriggerEffect effect)
    {
        report[offset] = effect.Mode;
        report[offset + 1] = effect.StartResistance;
        report[offset + 2] = effect.EffectForce;
        report[offset + 3] = effect.RangeForce;
        report[offset + 4] = effect.NearReleaseStrength;
        report[offset + 5] = effect.NearMiddleStrength;
        report[offset + 6] = effect.PressedStrength;
        report[offset + 9] = effect.ActuationFrequency;
    }

    public void Dispose()
    {
        try
        {
            lock (_writeLock)
            {
                _rumble = new DualSenseRumble(0, 0);
                _triggerState = DualSenseTriggerState.None;
                WriteOutputReport();
            }
        }
        catch
        {
            // Best effort only; the stream may already be gone.
        }

        _stream.Dispose();
    }
}
