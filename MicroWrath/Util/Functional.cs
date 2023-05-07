using System;
using System.Runtime.CompilerServices;

namespace MicroWrath.Util
{
    public static class Functional
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Identity<T>(T x) => x;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Ignore<T>(T _) { }

        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static U Apply<T, U>(this T obj, Func<T, U> f) => f(obj);

        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T UpCast<TParam, T>(TParam x) where TParam : T => (T)x;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static U Downcast<T, U>(this T obj) where U : T => (U)obj!;

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
    }
}
