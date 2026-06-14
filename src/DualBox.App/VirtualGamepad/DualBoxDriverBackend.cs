using System.Buffers.Binary;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using DualBox.Controller;
using DualBox.Core;

namespace DualBox.VirtualGamepad;

public sealed class DualBoxDriverBackend : IXboxBridgeBackend
{
    private static readonly Guid DeviceInterfaceGuid = new("a35610d3-6b1d-4fd1-a661-2370a43325b7");

    private const uint GenericRead = 0x80000000;
    private const uint GenericWrite = 0x40000000;
    private const uint FileShareRead = 0x00000001;
    private const uint FileShareWrite = 0x00000002;
    private const uint OpenExisting = 3;
    private const uint FileAttributeNormal = 0x00000080;
    private const uint DigcfPresent = 0x00000002;
    private const uint DigcfDeviceInterface = 0x00000010;
    private const uint FileDeviceDualBox = 0x8000;
    private const uint MethodBuffered = 0;
    private const uint FileReadData = 0x0001;
    private const uint FileWriteData = 0x0002;
    private const uint IoctlSubmitInput = (FileDeviceDualBox << 16) | (FileWriteData << 14) | (0x801 << 2) | MethodBuffered;
    private const uint IoctlGetFeedback = (FileDeviceDualBox << 16) | (FileReadData << 14) | (0x802 << 2) | MethodBuffered;
    private const byte InputReportId = 0x01;
    private const byte FeedbackReportId = 0x05;

    private readonly SafeFileHandle _handle;
    private XboxGamepadFeedback _lastFeedback = new(0, 0);

    private DualBoxDriverBackend(SafeFileHandle handle)
    {
        _handle = handle;
    }

    public string DisplayName => "DualBox virtual pad driver";

    public event EventHandler<XboxGamepadFeedback>? FeedbackReceived;

    public static bool TryCreate(out DualBoxDriverBackend? backend)
    {
        backend = null;
        var path = FindDevicePath();
        if (path is null)
        {
            return false;
        }

        var handle = CreateFile(
            path,
            GenericRead | GenericWrite,
            FileShareRead | FileShareWrite,
            IntPtr.Zero,
            OpenExisting,
            FileAttributeNormal,
            IntPtr.Zero);

        if (handle.IsInvalid)
        {
            handle.Dispose();
            return false;
        }

        backend = new DualBoxDriverBackend(handle);
        return true;
    }

    public void Connect()
    {
    }

    public void Apply(DualSenseInputReport input, MappingProfile profile)
    {
        Span<byte> report = stackalloc byte[14];
        var buttons = ToButtonBits(input, profile);

        report[0] = InputReportId;
        BinaryPrimitives.WriteInt16LittleEndian(report[1..3], InputMath.StickByteToXInputAxis(input.LeftX, deadzone: profile.Sticks.LeftDeadzone));
        BinaryPrimitives.WriteInt16LittleEndian(report[3..5], InputMath.StickByteToXInputAxis(input.LeftY, invert: true, deadzone: profile.Sticks.LeftDeadzone));
        BinaryPrimitives.WriteInt16LittleEndian(report[5..7], InputMath.StickByteToXInputAxis(input.RightX, deadzone: profile.Sticks.RightDeadzone));
        BinaryPrimitives.WriteInt16LittleEndian(report[7..9], InputMath.StickByteToXInputAxis(input.RightY, invert: true, deadzone: profile.Sticks.RightDeadzone));
        report[9] = input.LeftTrigger;
        report[10] = input.RightTrigger;
        BinaryPrimitives.WriteUInt16LittleEndian(report[11..13], buttons);
        report[13] = ToHat(input.Dpad);

        if (!DeviceIoControl(_handle, IoctlSubmitInput, report, (uint)report.Length, Span<byte>.Empty, 0, out _, IntPtr.Zero))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "DualBox driver input submit failed.");
        }

        PublishFeedbackIfChanged();
    }

    public void Dispose()
    {
        _handle.Dispose();
    }

    private void PublishFeedbackIfChanged()
    {
        Span<byte> output = stackalloc byte[5];

        if (!DeviceIoControl(_handle, IoctlGetFeedback, Span<byte>.Empty, 0, output, (uint)output.Length, out var returned, IntPtr.Zero) ||
            returned < output.Length ||
            output[0] != FeedbackReportId)
        {
            return;
        }

        var feedback = new XboxGamepadFeedback(output[1], output[2], output[3], output[4]);
        if (feedback == _lastFeedback)
        {
            return;
        }

        _lastFeedback = feedback;
        FeedbackReceived?.Invoke(this, feedback);
    }

    private static ushort ToButtonBits(DualSenseInputReport input, MappingProfile profile)
    {
        ushort buttons = 0;
        Set(ref buttons, 0, input.Cross);
        Set(ref buttons, 1, input.Circle);
        Set(ref buttons, 2, input.Square);
        Set(ref buttons, 3, input.Triangle);
        Set(ref buttons, 4, input.LeftShoulder);
        Set(ref buttons, 5, input.RightShoulder);
        Set(ref buttons, 6, input.Create || (input.TouchpadButton && profile.TouchpadPress == TouchpadBinding.Back));
        Set(ref buttons, 7, input.Options || (input.TouchpadButton && profile.TouchpadPress == TouchpadBinding.Start));
        Set(ref buttons, 8, input.LeftStickButton);
        Set(ref buttons, 9, input.RightStickButton);
        Set(ref buttons, 10, (input.PlayStation && !profile.PsButtonOpensGameBar) || (input.TouchpadButton && profile.TouchpadPress == TouchpadBinding.Guide));
        Set(ref buttons, 11, input.Mute);
        return buttons;
    }

    private static void Set(ref ushort buttons, int bit, bool isPressed)
    {
        if (isPressed)
        {
            buttons |= (ushort)(1 << bit);
        }
    }

    private static byte ToHat(DualSenseDpad dpad)
    {
        return (dpad.Up, dpad.Right, dpad.Down, dpad.Left) switch
        {
            (true, false, false, false) => 0,
            (true, true, false, false) => 1,
            (false, true, false, false) => 2,
            (false, true, true, false) => 3,
            (false, false, true, false) => 4,
            (false, false, true, true) => 5,
            (false, false, false, true) => 6,
            (true, false, false, true) => 7,
            _ => 8
        };
    }

    private static string? FindDevicePath()
    {
        var deviceInterfaceGuid = DeviceInterfaceGuid;
        var infoSet = SetupDiGetClassDevs(ref deviceInterfaceGuid, null, IntPtr.Zero, DigcfPresent | DigcfDeviceInterface);
        if (infoSet == new IntPtr(-1))
        {
            return null;
        }

        try
        {
            var interfaceData = new SpDeviceInterfaceData
            {
                CbSize = Marshal.SizeOf<SpDeviceInterfaceData>()
            };

            if (!SetupDiEnumDeviceInterfaces(infoSet, IntPtr.Zero, ref deviceInterfaceGuid, 0, ref interfaceData))
            {
                return null;
            }

            SetupDiGetDeviceInterfaceDetail(infoSet, ref interfaceData, IntPtr.Zero, 0, out var requiredSize, IntPtr.Zero);
            if (requiredSize == 0)
            {
                return null;
            }

            var detailBuffer = Marshal.AllocHGlobal((int)requiredSize);
            try
            {
                Marshal.WriteInt32(detailBuffer, IntPtr.Size == 8 ? 8 : 6);

                if (!SetupDiGetDeviceInterfaceDetail(infoSet, ref interfaceData, detailBuffer, requiredSize, out _, IntPtr.Zero))
                {
                    return null;
                }

                return Marshal.PtrToStringUni(detailBuffer + 4);
            }
            finally
            {
                Marshal.FreeHGlobal(detailBuffer);
            }
        }
        finally
        {
            SetupDiDestroyDeviceInfoList(infoSet);
        }
    }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern SafeFileHandle CreateFile(
        string fileName,
        uint desiredAccess,
        uint shareMode,
        IntPtr securityAttributes,
        uint creationDisposition,
        uint flagsAndAttributes,
        IntPtr templateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool DeviceIoControl(
        SafeFileHandle device,
        uint ioControlCode,
        ReadOnlySpan<byte> inBuffer,
        uint inBufferSize,
        Span<byte> outBuffer,
        uint outBufferSize,
        out uint bytesReturned,
        IntPtr overlapped);

    [DllImport("setupapi.dll", SetLastError = true)]
    private static extern IntPtr SetupDiGetClassDevs(
        ref Guid classGuid,
        string? enumerator,
        IntPtr hwndParent,
        uint flags);

    [DllImport("setupapi.dll", SetLastError = true)]
    private static extern bool SetupDiEnumDeviceInterfaces(
        IntPtr deviceInfoSet,
        IntPtr deviceInfoData,
        ref Guid interfaceClassGuid,
        uint memberIndex,
        ref SpDeviceInterfaceData deviceInterfaceData);

    [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool SetupDiGetDeviceInterfaceDetail(
        IntPtr deviceInfoSet,
        ref SpDeviceInterfaceData deviceInterfaceData,
        IntPtr deviceInterfaceDetailData,
        uint deviceInterfaceDetailDataSize,
        out uint requiredSize,
        IntPtr deviceInfoData);

    [DllImport("setupapi.dll", SetLastError = true)]
    private static extern bool SetupDiDestroyDeviceInfoList(IntPtr deviceInfoSet);

    [StructLayout(LayoutKind.Sequential)]
    private struct SpDeviceInterfaceData
    {
        public int CbSize;
        public Guid InterfaceClassGuid;
        public int Flags;
        public IntPtr Reserved;
    }
}
