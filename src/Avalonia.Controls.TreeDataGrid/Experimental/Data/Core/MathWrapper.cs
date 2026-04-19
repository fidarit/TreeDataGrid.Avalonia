using System;

namespace Avalonia.Experimental.Data.Core;

/// <summary>
/// Provides math utilities not provided in System.Math.
/// </summary>
internal class MathWrapper
{
    // smallest such that 1.0+DoubleEpsilon != 1.0
    internal const double DoubleEpsilon = 2.2204460492503131e-016;

    internal const float FloatEpsilon = 1.192092896e-07F;

    /// <summary>
    /// AreClose - Returns whether or not two doubles are "close".  That is, whether or 
    /// not they are within epsilon of each other.
    /// </summary> 
    /// <param name="value1"> The first double to compare. </param>
    /// <param name="value2"> The second double to compare. </param>
    public static bool AreClose(double value1, double value2)
    {
        //in case they are Infinities (then epsilon check does not work)
        if (value1 == value2)
            return true;
        double eps = (Math.Abs(value1) + Math.Abs(value2) + 10.0) * DoubleEpsilon;
        double delta = value1 - value2;
        return (-eps < delta) && (eps > delta);
    }

    /// <summary>
    /// GreaterThan - Returns whether or not the first double is greater than the second double.
    /// That is, whether or not the first is strictly greater than *and* not within epsilon of
    /// the other number.
    /// </summary>
    /// <param name="value1"> The first double to compare. </param>
    /// <param name="value2"> The second double to compare. </param>
    public static bool GreaterThan(double value1, double value2)
    {
        return (value1 > value2) && !AreClose(value1, value2);
    }

    /// <summary>
    /// IsZero - Returns whether or not the double is "close" to 0.  Same as AreClose(double, 0),
    /// but this is faster.
    /// </summary>
    /// <param name="value"> The double to compare to 0. </param>
    public static bool IsZero(double value)
    {
        return Math.Abs(value) < 10.0 * DoubleEpsilon;
    }
}
