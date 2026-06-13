using System.Runtime.InteropServices;

namespace DualSensePass.Core;

public static class GameBarHotkey
{
    private const ushort KeyEventKeyUp = 0x0002;
    private const ushort VirtualKeyLeftWindows = 0x5B;
    private const ushort VirtualKeyG = 0x47;
    private const uint InputKeyboard = 1;

    public static void Open()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        Span<Input> inputs =
        [
            KeyDown(VirtualKeyLeftWindows),
            KeyDown(VirtualKeyG),
            KeyUp(VirtualKeyG),
            KeyUp(VirtualKeyLeftWindows)
        ];

        SendInput((uint)inputs.Length, ref MemoryMarshal.GetReference(inputs), Marshal.SizeOf<Input>());
    }

    private static Input KeyDown(ushort virtualKey)
    {
        return new Input
        {
            Type = InputKeyboard,
            Data = new InputUnion
            {
                Keyboard = new KeyboardInput
                {
                    VirtualKey = virtualKey
                }
            }
        };
    }

    private static Input KeyUp(ushort virtualKey)
    {
        var input = KeyDown(virtualKey);
        input.Data.Keyboard.Flags = KeyEventKeyUp;
        return input;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint inputCount, ref Input inputs, int inputSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public uint Type;
        public InputUnion Data;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public KeyboardInput Keyboard;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInput
    {
        public ushort VirtualKey;
        public ushort ScanCode;
        public ushort Flags;
        public uint Time;
        public nint ExtraInfo;
    }
}
