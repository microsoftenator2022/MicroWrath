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

        //internal static IEnumerable<U> SelectIfNotNull<T, U>(this IEnumerable<T> source, Func<T, U?> selector) where T : class
        //{
        //    foreach (var x in source)
        //    {
        //        var y = selector(x);
        //        if (y is not null)
        //            yield return y;
        //    }
        //}

        public static IEnumerable<(int index, T item)> Indexed<T>(this IEnumerable<T> source)
        {
            var index = 0;
            foreach (var item in source)
                yield return (index++, item);
        }

        //internal static IEnumerable<T> ToEnumerable<T, TEnumerable>(this TEnumerable source) where TEnumerable : IEnumerable<T> => source;

        public static IDictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<(TKey key, TValue value)> source) =>
            source.ToDictionary(kv => kv.key, kv => kv.value);

        public static IDictionary<TKey, TValue> ToDictionary<TKey, TValue>
            (this IEnumerable<(TKey key, TValue value)> source, IEqualityComparer<TKey> keyComparer) =>
            source.ToDictionary(kv => kv.key, kv => kv.value, keyComparer);

        /// <summary>
        /// Appends a value to an array
        /// </summary>
        /// <param name="array">The array to append to</param>
        /// <param name="value">The value to append</param>
        /// <returns>A new array containing the values of the original array and the new value</returns>
        public static T[] Append<T>(this T[] array, T value)
        {
            var newArray = new T[array.Length + 1];

            array.CopyTo(newArray.AsSpan());

            newArray[array.Length] = value;

            return newArray;
        }

        public static IEnumerable<T> AppendValue<T>(this IEnumerable<T> source, T value)
        {
            foreach (var item in source)
                yield return item;

            yield return value;
        }

        public static T[] Concat<T>(this T[] arrayA, T[] arrayB)
        {
            var newArray = new T[arrayA.Length + arrayB.Length];

            arrayA.CopyTo(newArray.AsSpan(0, arrayA.Length));
            arrayB.CopyTo(newArray.AsSpan(arrayA.Length, arrayB.Length));

            return newArray;
        }

        public static IEnumerable<IEnumerable<T>> ChunkBySize<T>(this IEnumerable<T> source, int chunkSize)
        {
            var chunk = new T[chunkSize];
            var i = 0;

            foreach (var element in source)
            {
                chunk[i] = element;

                i++;
                if (i == chunkSize)
                {
                    yield return chunk;
                    chunk = new T[chunkSize];
                    i = 0;
                }
            }

            if (i > 0 && i < chunkSize) yield return chunk.Take(i);
        }

        //public static IEnumerable<T> FindSequence<T>(this IEnumerable<T> source, IEnumerable<Func<T, bool>> predicateSequence)
        //{
        //    var sEnumerator = source.GetEnumerator();
        //    var fsEnumerator = predicateSequence.GetEnumerator();

        //    var i = 0;
        //    while (fsEnumerator.MoveNext())
        //    {
        //        if (!sEnumerator.MoveNext()) return Enumerable.Empty<T>();

        //        if (fsEnumerator.Current(sEnumerator.Current))
        //            return source.Skip(1).FindSequence(predicateSequence);

        //        i++;
        //    }

        //    return source.Take(i);
        //}

        public static IEnumerable<T> FindSequence<T>(this IEnumerable<T> source, int length, IEnumerable<Func<T, bool>> predicateSequence)
        {
            var i = 0;
            foreach (var result in predicateSequence.Zip(source, (f, x) => f(x)))
            {
                if (!result) return source.Skip(1).FindSequence(length, predicateSequence);

                i++;

                if (i >= length) return source.Take(i);
            }

            return Enumerable.Empty<T>();
        }

        public static IEnumerable<T> FindSequence<T>(this IEnumerable<T> source, IEnumerable<Func<T, bool>> predicateSequence) =>
            source.FindSequence(predicateSequence.Count(), predicateSequence);

        public static IEnumerable<T> FindSequence<T>(this IEnumerable<T> source, int length, Func<IEnumerable<T>, bool> predicate)
        {
            var subSeq = source.Take(length);
            if (subSeq.Count() < length) return Enumerable.Empty<T>();

            if (predicate(subSeq)) return subSeq;

            return source.Skip(1).FindSequence(length, predicate);
        }

        public static IEnumerable<T> EmptyIfNull<T>(this T? item) where T : class
        {
            if (item == null)
                yield break;
            
            yield return item;
        }

        public static IEnumerable<T> SkipIfNull<T>(this IEnumerable<T?> source) where T : class =>
            source.SelectMany(EmptyIfNull);

        public static IEnumerable<T> Generate<TSource, T>(this TSource state, Func<TSource, Option<(T, TSource)>> generator)
        {
            var next = generator(state);

            if (next.IsNone)
                yield break;

            (var value, state) = next.Value!;

            yield return value;

            foreach (var item in Generate(state, generator))
                yield return item;
        }
    }
}
