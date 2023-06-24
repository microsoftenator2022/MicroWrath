﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MicroWrath.Util
{
    public static partial class Functional
    {
        private static (A, B, C) Expand3<A, B, C>(((A, B), C) tuple)
        {
            var ((a, b), c) = tuple;

            return (a, b, c);
        }

        public static (A, B, C) Expand<A, B, C>(this ((A, B), C) tuple) => Expand3(tuple);

        private static (A, B, C, D) Expand4<A, B, C, D>((((A, B), C), D) tuple)
        {
            var ((a, b), c, d) = Expand3(tuple);

            return (a, b, c, d);
        }

        public static (A, B, C, D) Expand<A, B, C, D>(this (((A, B), C), D) tuple) => Expand4(tuple);

        private static (A, B, C, D, E) Expand5<A, B, C, D, E>(((((A, B), C), D), E) tuple)
        {
            var ((a, b), c, d, e) = Expand4(tuple);

            return (a, b, c, d, e);
        }

        public static (A, B, C, D, E) Expand<A, B, C, D, E>(this ((((A, B), C), D), E) tuple) => Expand5(tuple);

        private static (A, B, C, D, E, F) Expand6<A, B, C, D, E, F>((((((A, B), C), D), E), F) tuple)
        {
            var ((a, b), c, d, e, f) = Expand5(tuple);

            return (a, b, c, d, e, f);
        }

        public static (A, B, C, D, E, F) Expand<A, B, C, D, E, F>(this (((((A, B), C), D), E), F) tuple) =>
            Expand6(tuple);

        private static (A, B, C, D, E, F, G) Expand7<A, B, C, D, E, F, G>(((((((A, B), C), D), E), F), G) tuple)
        {
            var ((a, b), c, d, e, f, g) = Expand6(tuple);

            return (a, b, c, d, e, f, g);
        }

        public static (A, B, C, D, E, F, G) Expand<A, B, C, D, E, F, G>(this ((((((A, B), C), D), E), F), G) tuple) =>
            Expand7(tuple);

        private static (A, B, C, D, E, F, G, H) Expand8<A, B, C, D, E, F, G, H>((((((((A, B), C), D), E), F), G), H) tuple)
        {
            var ((a, b), c, d, e, f, g, h) = Expand7(tuple);

            return (a, b, c, d, e, f, g, h);
        }

        public static (A, B, C, D, E, F, G, H) Expand<A, B, C, D, E, F, G, H>(this (((((((A, B), C), D), E), F), G), H) tuple) =>
            Expand8(tuple);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (A, B, C) Flatten<A, B, C>(this ((A, B), C) tuple)
        {
            var ((a, b), c) = tuple;

            return (a, b, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (A, B, C) Flatten<A, B, C>(this (A, (B, C)) tuple)
        {
            var (a, (b, c)) = tuple;

            return (a, b, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (A, B, C, D) Flatten<A, B, C, D>(this ((A, B), C, D) tuple)
        {
            var ((a, b), c, d) = tuple;

            return (a, b, c, d);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (A, B, C, D) Flatten<A, B, C, D>(this (A, (B, C), D) tuple)
        {
            var (a, (b, c), d) = tuple;

            return (a, b, c, d);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (A, B, C, D) Flatten<A, B, C, D>(this (((A, B), C), D) tuple)
        {
            var (x, d) = tuple;

            var (a, b, c) = x.Flatten();

            return (a, b, c, d);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (A, B, C, D) Flatten<A, B, C, D>(this (A, B, (C, D)) tuple)
        {
            var (a, b, (c, d)) = tuple;

            return (a, b, c, d);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (A, B, C, D) Flatten<A, B, C, D>(this (A, (B, (C, D))) tuple)
        {
            var (a, x) = tuple;

            var (b, c, d) = x.Flatten();

            return (a, b, c, d);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (A, B, C, D, E) Flatten<A, B, C, D, E>(this (A, B, C, (D, E)) tuple)
        {
            var (a, b, c, (d, e)) = tuple;

            return (a, b, c, d, e);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (A, B, C, D, E) Flatten<A, B, C, D, E>(this (A, B, (C, (D, E))) tuple)
        {
            var (a, b, x) = tuple;

            var (c, d, e) = x.Flatten();

            return (a, b, c, d, e);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (A, B, C, D, E) Flatten<A, B, C, D, E>(this (A, (B, (C, (D, E)))) tuple)
        {
            var (a, x) = tuple;

            var (b, c, d, e) = x.Flatten();

            return (a, b, c, d, e);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (A, B, C, D, E) Flatten<A, B, C, D, E>(this ((A, B), C, D, E) tuple)
        {
            var ((a, b), c, d, e) = tuple;

            return (a, b, c, d, e);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (A, B, C, D, E) Flatten<A, B, C, D, E>(this (A, (B, C), D, E) tuple)
        {
            var (a, (b, c), d, e) = tuple;

            return (a, b, c, d, e);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (A, B, C, D, E) Flatten<A, B, C, D, E>(this (A, B, (C, D), E) tuple)
        {
            var (a, b, (c, d), e) = tuple;

            return (a, b, c, d, e);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (A, B, C, D, E) Flatten<A, B, C, D, E>(this (((A, B), C), D, E) tuple)
        {
            var (x, d, e) = tuple;

            var (a, b, c) = x.Flatten();

            return (a, b, c, d, e);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (A, B, C, D, E) Flatten<A, B, C, D, E>(this ((((A, B), C), D), E) tuple)
        {
            var (x, e) = tuple;

            var (a, b, c, d) = x.Flatten();

            return (a, b, c, d, e);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (A, B, C, D, E) Flatten<A, B, C, D, E>(this ((A, B), (C, D), E) tuple)
        {
            var ((a, b), (c, d), e) = tuple;

            return (a, b, c, d, e);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (A, B, C, D, E) Flatten<A, B, C, D, E>(this (A, (B, C), (D, E)) tuple)
        {
            var (a, (b, c), (d, e)) = tuple;

            return (a, b, c, d, e);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (A, B, C, D, E) Flatten<A, B, C, D, E>(this ((A, B), C, (D, E)) tuple)
        {
            var ((a, b), c, (d, e)) = tuple;

            return (a, b, c, d, e);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (A, B, C, D, E) Flatten<A, B, C, D, E>(this (((A, B), C), (D, E)) tuple)
        {
            var (x, (d, e)) = tuple;
            var (a, b, c) = x.Flatten();

            return (a, b, c, d, e);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (A, B, C, D, E) Flatten<A, B, C, D, E>(this ((A, B), (C, (D, E))) tuple)
        {
            var ((a, b), x) = tuple;
            var (c, d, e) = x.Flatten();

            return (a, b, c, d, e);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] ToArray<T>(this ((T, T), T) tuple)
        {
            var ((a, b), c) = tuple;

            return new[] { a, b, c };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] ToArray<T>(this (((T, T), T), T) tuple)
        {
            var (((a, b), c), d) = tuple;

            return new[] { a, b, c, d };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] ToArray<T>(this ((((T, T), T), T), T) tuple)
        {
            var ((((a, b), c), d), e) = tuple;

            return new[] { a, b, c, d, e };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] ToArray<T>(this (((((T, T), T), T), T), T) tuple)
        {
            var (((((a, b), c), d), e), f) = tuple;

            return new[] { a, b, c, d, e, f };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] ToArray<T>(this ((((((T, T), T), T), T), T), T) tuple)
        {
            var ((((((a, b), c), d), e), f), g) = tuple;

            return new[] { a, b, c, d, e, f, g };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] ToArray<T>(this (((((((T, T), T), T), T), T), T), T) tuple)
        {
            var (((((((a, b), c), d), e), f), g), h) = tuple;

            return new[] { a, b, c, d, e, f, g, h };
        }
    }
}
