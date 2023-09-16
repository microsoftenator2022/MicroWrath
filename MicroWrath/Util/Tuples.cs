using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MicroWrath.Util
{
    public static partial class Functional
    {
        /// <summary>
        /// Adds an element to a N-tuple, returning a N+1-tuple
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (A, B, T) With<A, B, T>((A, B) tuple, T item)
        {
            var (a, b) = tuple;
            return (a, b, item);
        }

        /// <inheritdoc cref="With{A, B, T}(ValueTuple{A, B}, T)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (A, B, C, T) With<A, B, C, T>((A, B, C) tuple, T item)
        {
            var (a, b, c) = tuple;
            return (a, b, c, item);
        }

        /// <inheritdoc cref="With{A, B, T}(ValueTuple{A, B}, T)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (A, B, C, D, T) With<A, B, C, D, T>((A, B, C, D) tuple, T item)
        {
            var (a, b, c, d) = tuple;
            return (a, b, c, d, item);
        }

        /// <inheritdoc cref="With{A, B, T}(ValueTuple{A, B}, T)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (A, B, C, D, E, T) With<A, B, C, D, E, T>((A, B, C, D, E) tuple, T item)
        {
            var (a, b, c, d, e) = tuple;
            return (a, b, c, d, e, item);
        }

        /// <inheritdoc cref="With{A, B, T}(ValueTuple{A, B}, T)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (A, B, C, D, E, F, T) With<A, B, C, D, E, F, T>((A, B, C, D, E, F) tuple, T item)
        {
            var (a, b, c, d, e, f) = tuple;
            return (a, b, c, d, e, f, item);
        }

        /// <inheritdoc cref="With{A, B, T}(ValueTuple{A, B}, T)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (A, B, C, D, E, F, G, T) With<A, B, C, D, E, F, G, T>((A, B, C, D, E, F, G) tuple, T item)
        {
            var (a, b, c, d, e, f, g) = tuple;
            return (a, b, c, d, e, f, g, item);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (A, B, C) Expand3<A, B, C>(((A, B), C) tuple)
        {
            var ((a, b), c) = tuple;

            return (a, b, c);
        }

        /// <inheritdoc cref="Flatten{A, B, C}(ValueTuple{ValueTuple{A, B}, C})"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (A, B, C) Expand<A, B, C>(this ((A, B), C) tuple) => Expand3(tuple);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (A, B, C, D) Expand4<A, B, C, D>((((A, B), C), D) tuple)
        {
            var ((a, b), c, d) = Expand3(tuple);

            return (a, b, c, d);
        }

        /// <inheritdoc cref="Flatten{A, B, C}(ValueTuple{ValueTuple{A, B}, C})"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (A, B, C, D) Expand<A, B, C, D>(this (((A, B), C), D) tuple) => Expand4(tuple);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (A, B, C, D, E) Expand5<A, B, C, D, E>(((((A, B), C), D), E) tuple)
        {
            var ((a, b), c, d, e) = Expand4(tuple);

            return (a, b, c, d, e);
        }

        /// <inheritdoc cref="Flatten{A, B, C}(ValueTuple{ValueTuple{A, B}, C})"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (A, B, C, D, E) Expand<A, B, C, D, E>(this ((((A, B), C), D), E) tuple) => Expand5(tuple);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (A, B, C, D, E, F) Expand6<A, B, C, D, E, F>((((((A, B), C), D), E), F) tuple)
        {
            var ((a, b), c, d, e, f) = Expand5(tuple);

            return (a, b, c, d, e, f);
        }

        /// <inheritdoc cref="Flatten{A, B, C}(ValueTuple{ValueTuple{A, B}, C})"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (A, B, C, D, E, F) Expand<A, B, C, D, E, F>(this (((((A, B), C), D), E), F) tuple) =>
            Expand6(tuple);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (A, B, C, D, E, F, G) Expand7<A, B, C, D, E, F, G>(((((((A, B), C), D), E), F), G) tuple)
        {
            var ((a, b), c, d, e, f, g) = Expand6(tuple);

            return (a, b, c, d, e, f, g);
        }

        /// <inheritdoc cref="Flatten{A, B, C}(ValueTuple{ValueTuple{A, B}, C})"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (A, B, C, D, E, F, G) Expand<A, B, C, D, E, F, G>(this ((((((A, B), C), D), E), F), G) tuple) =>
            Expand7(tuple);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (A, B, C, D, E, F, G, H) Expand8<A, B, C, D, E, F, G, H>((((((((A, B), C), D), E), F), G), H) tuple)
        {
            var ((a, b), c, d, e, f, g, h) = Expand7(tuple);

            return (a, b, c, d, e, f, g, h);
        }

        /// <inheritdoc cref="Flatten{A, B, C}(ValueTuple{ValueTuple{A, B}, C})"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (A, B, C, D, E, F, G, H) Expand<A, B, C, D, E, F, G, H>(this (((((((A, B), C), D), E), F), G), H) tuple) =>
            Expand8(tuple);

        /// <summary>
        /// Flattens a tuple
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (A, B, C) Flatten<A, B, C>(this ((A, B), C) tuple)
        {
            var ((a, b), c) = tuple;

            return (a, b, c);
        }

        /// <inheritdoc cref="Flatten{A, B, C}(ValueTuple{ValueTuple{A, B}, C})"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (A, B, C) Flatten<A, B, C>(this (A, (B, C)) tuple)
        {
            var (a, (b, c)) = tuple;

            return (a, b, c);
        }

        /// <inheritdoc cref="Flatten{A, B, C}(ValueTuple{ValueTuple{A, B}, C})"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (A, B, C, D) Flatten<A, B, C, D>(this ((A, B), C, D) tuple)
        {
            var ((a, b), c, d) = tuple;

            return (a, b, c, d);
        }

        /// <inheritdoc cref="Flatten{A, B, C}(ValueTuple{ValueTuple{A, B}, C})"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (A, B, C, D) Flatten<A, B, C, D>(this (A, (B, C), D) tuple)
        {
            var (a, (b, c), d) = tuple;

            return (a, b, c, d);
        }

        /// <inheritdoc cref="Flatten{A, B, C}(ValueTuple{ValueTuple{A, B}, C})"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (A, B, C, D) Flatten<A, B, C, D>(this (((A, B), C), D) tuple)
        {
            var (x, d) = tuple;

            var (a, b, c) = x.Flatten();

            return (a, b, c, d);
        }

        /// <inheritdoc cref="Flatten{A, B, C}(ValueTuple{ValueTuple{A, B}, C})"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (A, B, C, D) Flatten<A, B, C, D>(this (A, B, (C, D)) tuple)
        {
            var (a, b, (c, d)) = tuple;

            return (a, b, c, d);
        }

        /// <inheritdoc cref="Flatten{A, B, C}(ValueTuple{ValueTuple{A, B}, C})"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (A, B, C, D) Flatten<A, B, C, D>(this (A, (B, (C, D))) tuple)
        {
            var (a, x) = tuple;

            var (b, c, d) = x.Flatten();

            return (a, b, c, d);
        }

        /// <inheritdoc cref="Flatten{A, B, C}(ValueTuple{ValueTuple{A, B}, C})"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (A, B, C, D, E) Flatten<A, B, C, D, E>(this (A, B, C, (D, E)) tuple)
        {
            var (a, b, c, (d, e)) = tuple;

            return (a, b, c, d, e);
        }

        /// <inheritdoc cref="Flatten{A, B, C}(ValueTuple{ValueTuple{A, B}, C})"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (A, B, C, D, E) Flatten<A, B, C, D, E>(this (A, B, (C, (D, E))) tuple)
        {
            var (a, b, x) = tuple;

            var (c, d, e) = x.Flatten();

            return (a, b, c, d, e);
        }

        /// <inheritdoc cref="Flatten{A, B, C}(ValueTuple{ValueTuple{A, B}, C})"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (A, B, C, D, E) Flatten<A, B, C, D, E>(this (A, (B, (C, (D, E)))) tuple)
        {
            var (a, x) = tuple;

            var (b, c, d, e) = x.Flatten();

            return (a, b, c, d, e);
        }

        /// <inheritdoc cref="Flatten{A, B, C}(ValueTuple{ValueTuple{A, B}, C})"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (A, B, C, D, E) Flatten<A, B, C, D, E>(this ((A, B), C, D, E) tuple)
        {
            var ((a, b), c, d, e) = tuple;

            return (a, b, c, d, e);
        }

        /// <inheritdoc cref="Flatten{A, B, C}(ValueTuple{ValueTuple{A, B}, C})"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (A, B, C, D, E) Flatten<A, B, C, D, E>(this (A, (B, C), D, E) tuple)
        {
            var (a, (b, c), d, e) = tuple;

            return (a, b, c, d, e);
        }

        /// <inheritdoc cref="Flatten{A, B, C}(ValueTuple{ValueTuple{A, B}, C})"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (A, B, C, D, E) Flatten<A, B, C, D, E>(this (A, B, (C, D), E) tuple)
        {
            var (a, b, (c, d), e) = tuple;

            return (a, b, c, d, e);
        }

        /// <inheritdoc cref="Flatten{A, B, C}(ValueTuple{ValueTuple{A, B}, C})"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (A, B, C, D, E) Flatten<A, B, C, D, E>(this (((A, B), C), D, E) tuple)
        {
            var (x, d, e) = tuple;

            var (a, b, c) = x.Flatten();

            return (a, b, c, d, e);
        }

        /// <inheritdoc cref="Flatten{A, B, C}(ValueTuple{ValueTuple{A, B}, C})"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (A, B, C, D, E) Flatten<A, B, C, D, E>(this ((((A, B), C), D), E) tuple)
        {
            var (x, e) = tuple;

            var (a, b, c, d) = x.Flatten();

            return (a, b, c, d, e);
        }

        /// <inheritdoc cref="Flatten{A, B, C}(ValueTuple{ValueTuple{A, B}, C})"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (A, B, C, D, E) Flatten<A, B, C, D, E>(this ((A, B), (C, D), E) tuple)
        {
            var ((a, b), (c, d), e) = tuple;

            return (a, b, c, d, e);
        }

        /// <inheritdoc cref="Flatten{A, B, C}(ValueTuple{ValueTuple{A, B}, C})"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (A, B, C, D, E) Flatten<A, B, C, D, E>(this (A, (B, C), (D, E)) tuple)
        {
            var (a, (b, c), (d, e)) = tuple;

            return (a, b, c, d, e);
        }

        /// <inheritdoc cref="Flatten{A, B, C}(ValueTuple{ValueTuple{A, B}, C})"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (A, B, C, D, E) Flatten<A, B, C, D, E>(this ((A, B), C, (D, E)) tuple)
        {
            var ((a, b), c, (d, e)) = tuple;

            return (a, b, c, d, e);
        }

        /// <inheritdoc cref="Flatten{A, B, C}(ValueTuple{ValueTuple{A, B}, C})"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (A, B, C, D, E) Flatten<A, B, C, D, E>(this (((A, B), C), (D, E)) tuple)
        {
            var (x, (d, e)) = tuple;
            var (a, b, c) = x.Flatten();

            return (a, b, c, d, e);
        }

        /// <inheritdoc cref="Flatten{A, B, C}(ValueTuple{ValueTuple{A, B}, C})"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (A, B, C, D, E) Flatten<A, B, C, D, E>(this ((A, B), (C, (D, E))) tuple)
        {
            var ((a, b), x) = tuple;
            var (c, d, e) = x.Flatten();

            return (a, b, c, d, e);
        }

        /// <inheritdoc cref="Flatten{A, B, C}(ValueTuple{ValueTuple{A, B}, C})"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] ToArray<T>(this ((T, T), T) tuple)
        {
            var ((a, b), c) = tuple;

            return [a, b, c];
        }

        /// <summary>
        /// Returns an array containing elements of a tuple, where all values are <typeparamref name="T"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] ToArray<T>(this (((T, T), T), T) tuple)
        {
            var (((a, b), c), d) = tuple;

            return [a, b, c, d];
        }

        /// <inheritdoc cref="ToArray{T}(ValueTuple{ValueTuple{T, T}, T})"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] ToArray<T>(this ((((T, T), T), T), T) tuple)
        {
            var ((((a, b), c), d), e) = tuple;

            return [a, b, c, d, e];
        }

        /// <inheritdoc cref="ToArray{T}(ValueTuple{ValueTuple{T, T}, T})"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] ToArray<T>(this (((((T, T), T), T), T), T) tuple)
        {
            var (((((a, b), c), d), e), f) = tuple;

            return [a, b, c, d, e, f];
        }

        /// <inheritdoc cref="ToArray{T}(ValueTuple{ValueTuple{T, T}, T})"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] ToArray<T>(this ((((((T, T), T), T), T), T), T) tuple)
        {
            var ((((((a, b), c), d), e), f), g) = tuple;

            return [a, b, c, d, e, f, g];
        }

        /// <inheritdoc cref="ToArray{T}(ValueTuple{ValueTuple{T, T}, T})"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] ToArray<T>(this (((((((T, T), T), T), T), T), T), T) tuple)
        {
            var (((((((a, b), c), d), e), f), g), h) = tuple;

            return [a, b, c, d, e, f, g, h];
        }
    }
}
