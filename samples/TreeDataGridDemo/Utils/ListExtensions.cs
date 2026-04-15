using System;
using System.Collections.Generic;

namespace TreeDataGridDemo.Utils
{
    internal static class ListExtensions
    {
        public static int FindIndex<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            int i = 0;
            foreach (var item in source)
            {
                if (predicate(item))
                    return i;
                i++;
            }
            return -1;
        }
    }
}
