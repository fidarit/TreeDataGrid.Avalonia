using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Avalonia.Media;
using Avalonia.Platform;

namespace Avalonia.Utilities
{
    public static class MathWrapper
    {
        internal const double DoubleEpsilon = 2.2204460492503131e-016;

        public static bool AreClose(double value1, double value2)
        {
            if (value1 == value2)
                return true;
            double eps = (Math.Abs(value1) + Math.Abs(value2) + 10.0) * DoubleEpsilon;
            double delta = value1 - value2;
            return (-eps < delta) && (eps > delta);
        }

        public static bool GreaterThan(double value1, double value2)
        {
            return (value1 > value2) && !AreClose(value1, value2);
        }

        public static bool IsZero(double value)
        {
            return Math.Abs(value) < 10.0 * DoubleEpsilon;
        }
    }
}
