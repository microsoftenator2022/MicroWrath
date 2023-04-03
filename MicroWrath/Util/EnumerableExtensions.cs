using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MicroWrath.Util.Linq
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<T> Singleton<T>(T value)
        {
            yield return value;
        }

        /// <summary>
        /// Selects distinct elements from a sequence, first applying a selector function and using a provided equality comparer
        /// </summary>
        public static IEnumerable<T> DistinctBy<T, U>(this IEnumerable<T> seq, Func<T, U> selector, IEqualityComparer<U> comparer)
        {
            var distinct = new List<U>();

            foreach (var item in seq)
            {
                var selected = selector(item);

                if (!distinct.Contains(selected, comparer))
                {
                    distinct.Add(selected);
                    yield return item;
                }
            }
        }

        /// <summary>
        /// Selects distinct elements from a sequence, first applying a selector function and using the default equality comparer
        /// </summary>
        public static IEnumerable<T> DistinctBy<T, U>(this IEnumerable<T> seq, Func<T, U> selector) => DistinctBy(seq, selector, EqualityComparer<U>.Default);

        internal static IEnumerable<U> SelectIfNotNull<T, U>(this IEnumerable<T> source, Func<T, U?> selector) where T : class
        {
            foreach (var x in source)
            {
                var y = selector(x);
                if (y is not null)
                    yield return y;
            }
        }

        internal static IEnumerable<(int index, T item)> Indexed<T>(this IEnumerable<T> source)
        {
            var index = 0;
            foreach (var item in source)
                yield return (index++, item);
        }

        internal static IEnumerable<T> ToEnumerable<T, TEnumerable>(this TEnumerable source) where TEnumerable : IEnumerable<T> => source;
    }
}
