using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MicroWrath.Util.Linq
{
    /// <summary>
    /// <see cref="IEnumerable{T}"/> extensions
    /// </summary>
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Sequence containing exactly one item
        /// </summary>
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

        /// <summary>
        /// Add item index to sequence
        /// </summary>
        /// <typeparam name="T">Element type</typeparam>
        /// <param name="source">Source sequence</param>
        /// <returns>Sequence of index/item pairs</returns>
        public static IEnumerable<(int index, T item)> Indexed<T>(this IEnumerable<T> source)
        {
            var index = 0;
            foreach (var item in source)
                yield return (index++, item);
        }

        //internal static IEnumerable<T> ToEnumerable<T, TEnumerable>(this TEnumerable source) where TEnumerable : IEnumerable<T> => source;

        /// <summary>
        /// Creates a dictionary from a sequence of Key/Value pairs
        /// </summary>
        
        /// <typeparam name="TKey">Key type</typeparam>
        /// <typeparam name="TValue">Value type</typeparam>
        /// <param name="source">Source sequence</param>
        public static IDictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<(TKey key, TValue value)> source) =>
            source.ToDictionary(kv => kv.key, kv => kv.value);

        /// <summary>
        /// Creates a dictionary from a sequence of Key/Value pairs using a provided <see cref="IEqualityComparer{T}"/>
        /// </summary>
        /// <param name="source">Source sequence</param>
        /// <param name="keyComparer">Key equality comparer</param>
        public static IDictionary<TKey, TValue> ToDictionary<TKey, TValue>
            (this IEnumerable<(TKey key, TValue value)> source, IEqualityComparer<TKey> keyComparer) =>
            source.ToDictionary(kv => kv.key, kv => kv.value, keyComparer);

        /// <summary>
        /// Appends a value to an array
        /// </summary>
        /// <param name="array">The array to append to</param>
        /// <param name="value">The value to append</param>
        /// <returns>New array containing the values of the original array and the new value</returns>
        public static T[] Append<T>(this T[] array, T value)
        {
            var newArray = new T[array.Length + 1];

            array.CopyTo(newArray.AsSpan());

            newArray[array.Length] = value;

            return newArray;
        }

        /// <summary>
        /// Appends a value to a sequence
        /// </summary>
        /// <param name="source">Source sequence</param>
        /// <param name="value">Value to append</param>
        public static IEnumerable<T> AppendValue<T>(this IEnumerable<T> source, T value)
        {
            foreach (var item in source)
                yield return item;

            yield return value;
        }

        /// <summary>
        /// Concatenates two arrays
        /// </summary>
        public static T[] Concat<T>(this T[] arrayA, T[] arrayB)
        {
            var newArray = new T[arrayA.Length + arrayB.Length];

            arrayA.CopyTo(newArray.AsSpan(0, arrayA.Length));
            arrayB.CopyTo(newArray.AsSpan(arrayA.Length, arrayB.Length));

            return newArray;
        }

        /// <summary>
        /// Concatenate arrays
        /// </summary>
        /// <param name="source">Initial array</param>
        /// <param name="arrays">Arrays to append</param>
        /// <returns>Concatenated array of <typeparamref name="T"/> values</returns>
        public static T[] Concat<T>(this T[] source, params T[][] arrays)
        {
            var totalLength = arrays.Select(a => a.Length).Sum() + source.Length;

            var newArray = new T[totalLength];

            source.CopyTo(newArray.AsSpan(0, source.Length));

            var offset = source.Length;

            foreach (var array in arrays)
            {
                array.CopyTo(newArray.AsSpan(offset, array.Length));
                offset += array.Length;
            }

            return newArray;
        }

        /// <summary>
        /// Divides input sequence into chunks of at most <paramref name="chunkSize"/>
        /// </summary>
        /// <returns>Sequence of sequences of <paramref name="chunkSize"/> elements. The last chunk will contain at most <paramref name="chunkSize"/> elements</returns>
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

        /// <summary>
        /// Finds a subsequence within a larger sequence by applying a sequence of predicates, providing a maximum match length
        /// </summary>
        /// <param name="source">Source sequence</param>
        /// <param name="length">Number of elements to match</param>
        /// <param name="predicateSequence">Sequence of match predicates</param>
        /// <returns>Matched sequence if found or empty sequence</returns>
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

        /// <summary>
        /// Finds a subsequence within a larger sequence by applying a sequence of predicates
        /// </summary>
        /// <param name="source">Source sequence</param>
        /// <param name="predicateSequence">Sequence of match predicates</param>
        /// <returns>Matched sequence if found or empty sequence</returns>
        public static IEnumerable<T> FindSequence<T>(this IEnumerable<T> source, IEnumerable<Func<T, bool>> predicateSequence) =>
            source.FindSequence(predicateSequence.Count(), predicateSequence);

        /// <summary>
        /// Finds a subsequence within a larger sequence by applying a predicate on a subsequences
        /// </summary>
        /// <param name="source">Source sequence</param>
        /// <param name="length">Number of elements to match</param>
        /// <param name="predicate">Subsequence predicate</param>
        /// <returns>Matched sequence if found or empty sequence</returns>
        public static IEnumerable<T> FindSequence<T>(this IEnumerable<T> source, int length, Func<IEnumerable<T>, bool> predicate)
        {
            var subSeq = source.Take(length);
            if (subSeq.Count() < length) return Enumerable.Empty<T>();

            if (predicate(subSeq)) return subSeq;

            return source.Skip(1).FindSequence(length, predicate);
        }

        /// <summary>
        /// Creates a sequence from a single nullable value
        /// </summary>
        /// <returns>Sequence containing single element <paramref name="item"/> or empty sequence if <paramref name="item"/> is null</returns>
        public static IEnumerable<T> EmptyIfNull<T>(this T? item) where T : class
        {
            if (item == null)
                yield break;
            
            yield return item;
        }

        /// <summary>
        /// Skips null values in a sequence
        /// </summary>
        /// <typeparam name="T">Element (nullable) reference type</typeparam>
        /// <param name="source">Source sequence</param>
        /// <returns>Source sequence, skipping null values, if the source sequence is empty or only contains null values, returns an empty sequence</returns>
        public static IEnumerable<T> SkipIfNull<T>(this IEnumerable<T?> source) where T : class =>
            source.SelectMany(EmptyIfNull);

        /// <summary>
        /// Generates a sequence using a provided generator function.
        /// This function is not eagerly evaluated and therefore the resulting sequence length is unbounded
        /// </summary>
        /// <typeparam name="TSource">Source type</typeparam>
        /// <typeparam name="T">Output element type</typeparam>
        /// <param name="state">Initial (seed) state</param>
        /// <param name="generator">Generator function</param>
        /// <returns>Generated sequence</returns>
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
        /// <summary>
        /// Adds the elements of the specified <see cref="IDictionary{TKey, TValue}"/> to the specified <see cref="IDictionary{TKey, TValue}"/>.s
        /// </summary>
        public static void AddRange<TKey, TValue>(this IDictionary<TKey, TValue> dict, IDictionary<TKey, TValue> other)
        {
            foreach (var entry in other)
            {
                dict.Add(entry.Key, entry.Value);
            }
        }

        /// <summary>
        /// Returns non-null elements of a sequence of reference type
        /// </summary>
        public static IEnumerable<T> NotNull<T>(this IEnumerable<T?> source) where T : class =>
            source.SelectMany(x => x.EmptyIfNull());
    }
}
