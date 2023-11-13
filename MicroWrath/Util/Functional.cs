using System;
using System.Runtime.CompilerServices;

namespace MicroWrath.Util
{
    /// <summary>
    /// Functional utilities
    /// </summary>
    public static partial class Functional
    {
#pragma warning disable CS1591
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
        public static Func<B, C> PartialApply<A, B, C>(Func<A, B, C> f, A a) =>
            (B b) => f(a, b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Func<B, C, D> PartialApply<A, B, C, D>(Func<A, B, C, D> f, A a) =>
            (B b, C c) => f(a, b, c);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Func<B, C, D, E> PartialApply<A, B, C, D, E>(Func<A, B, C, D, E> f, A a) =>
            (B b, C c, D d) => f(a, b, c, d);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Func<B, C, D, E, F> PartialApply<A, B, C, D, E, F>(Func<A, B, C, D, E, F> f, A a) =>
            (B b, C c, D d, E e) => f(a, b, c, d, e);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Func<B, C, D, E, F, G> PartialApply<A, B, C, D, E, F, G>(Func<A, B, C, D, E, F, G> func, A a) =>
            (B b, C c, D d, E e, F f) => func(a, b, c, d, e, f);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Func<B, C, D, E, F, G, H> PartialApply<A, B, C, D, E, F, G, H>(Func<A, B, C, D, E, F, G, H> func, A a) =>
            (B b, C c, D d, E e, F f, G g) => func(a, b, c, d, e, f, g);

        public static Func<A, Func<B, C>> Curry<A, B, C>(this Func<A, B, C> func) =>
            a => b => func(a, b);
        public static Func<A, B, Func<C, D>> Curry<A, B, C, D>(this Func<A, B, C, D> func) =>
            (a, b) => c => func(a, b, c);
        public static Func<A, B, C, Func<D, E>> Curry<A, B, C, D, E>(this Func<A, B, C, D, E> func) =>
            (a, b, c) => d => func(a, b, c, d);
        public static Func<A, B, C, D, Func<E, F>> Curry<A, B, C, D, E, F>(this Func<A, B, C, D, E, F> func) =>
            (a, b, c, d) => e => func(a, b, c, d, e);
        public static Func<A, B, C, D, E, Func<F, G>> Curry<A, B, C, D, E, F, G>(this Func<A, B, C, D, E, F, G> func) =>
            (a, b, c, d, e) => f => func(a, b, c, d, e, f);
        public static Func<A, B, C, D, E, F, Func<G, H>> Curry<A, B, C, D, E, F, G, H>(this Func<A, B, C, D, E, F, G, H> func) =>
            (a, b, c, d, e, f) => g => func(a, b, c, d, e, f, g);

#pragma warning restore
    }
}
