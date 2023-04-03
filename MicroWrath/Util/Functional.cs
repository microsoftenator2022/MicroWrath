using System;
using System.Collections.Generic;
using System.Linq;

namespace MicroWrath.Util
{
    internal static class Functional
    {
        public static T Identity<T>(T x) => x;
        public static T UpCast<TParam, T>(TParam x) where TParam : T => (T)x;
        public static void Ignore<T>(T _) { }

        public static Func<B, C> PartialApply<A, B, C>(this Func<A, B, C> f, A x) => (B y) => f(x, y);
        public static Func<B, C, D> PartialApply<A, B, C, D>(this Func<A, B, C, D> f, A x) => (B y, C z) => f(x, y, z);
    }
}
