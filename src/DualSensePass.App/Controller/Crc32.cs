namespace DualSensePass.Controller;

internal static class Crc32
{
    private const uint Polynomial = 0xEDB88320;

    public static uint Compute(ReadOnlySpan<byte> bytes, uint seed = 0xFFFFFFFF)
    {
        var crc = seed;

        foreach (var value in bytes)
        {
            crc ^= value;

            for (var i = 0; i < 8; i++)
            {
                crc = (crc & 1) != 0 ? (crc >> 1) ^ Polynomial : crc >> 1;
            }
        }

        return ~crc;
    }
}
