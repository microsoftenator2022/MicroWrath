using System;
using System.Runtime.CompilerServices;

namespace MicroWrath.Util
{
    public static partial class Functional
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
    }
}
