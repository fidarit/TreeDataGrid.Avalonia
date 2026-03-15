using System.Runtime.CompilerServices;

namespace Avalonia;


internal static class Double
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsFinite(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
