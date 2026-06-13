namespace DualSensePass.Core;

public static class InputMath
{
    public static StickOutput ToXInputStick(byte x, byte y, double deadzone)
    {
        return new StickOutput(
            StickByteToXInputAxis(x, deadzone: deadzone),
            StickByteToXInputAxis(y, invert: true, deadzone: deadzone));
    }

    public static short StickByteToXInputAxis(byte value, bool invert = false, double deadzone = 0.07)
    {
        deadzone = Math.Clamp(deadzone, 0, 0.35);
        var normalized = (value - 128) / 127.0;

        if (invert)
        {
            normalized *= -1;
        }

        if (Math.Abs(normalized) < deadzone)
        {
            return 0;
        }

        normalized = Math.Clamp(normalized, -1.0, 1.0);
        return (short)(normalized < 0 ? normalized * 32768 : normalized * 32767);
    }
}
