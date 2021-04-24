using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Fyrvall.DataEditor
{
    public static class CollectionExtensions
    {
        public const int INVALID_INDEX = -1;

        public static IEnumerable<T> ForEach<T>(this IEnumerable<T> items, Action<T> function)
        {
            foreach (var item in items) {
                function(item);
            }
            return items;
        }

        public static int GetIndexOfObject<T>(this IList<T> haystack, T needle, int defaultIndex = INVALID_INDEX) where T : class, IComparable
        {
            for (int i = 0; i < haystack.Count; i++) {
                if (haystack[i].Equals(needle)) {
                    return i;
                }
            }

            return defaultIndex;
        }
    }
}