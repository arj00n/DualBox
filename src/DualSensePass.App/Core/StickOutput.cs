namespace DualSensePass.Core;

public readonly record struct StickOutput(short X, short Y)
{
    public bool IsCentered => X == 0 && Y == 0;
}
