using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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

        //private static Expression<Func<A, B>> Lift<A, B>(Func<A, B> f) => x => f(x);

        //private static Expression<Func<B>> Apply<A, B>(Expression<Func<A, B>> ef, Expression<Func<A>> ex) =>
        //    (Expression<Func<B>>)Expression.Lambda(Expression.Invoke(ef, Expression.Invoke(ex)));

        //private static Expression<Func<B>> Map<A, B>(Func<A, B> f, Expression<Func<A>> ex)
        //{
        //    var lifted = Lift(f);

        //    return Apply(lifted, ex);
        //}

        //private static Expression<Func<A>> Return<A>(A x)
        //{
        //    Expression<Func<A>> expr = () => x;
        //    return expr;
        //}

        //private static Expression<Func<A>> Return<A>(Func<A> fa) => Return(fa());

        //private static Expression<Func<B>> Bind<A, B>(Func<A, Expression<Func<B>>> f, Expression<Func<A>> ex)
        //{
        //    var lifted = Lift(f);
        //    var expr = Apply(lifted, ex);
        //    return expr.Compile()();
        //}
    }
}
