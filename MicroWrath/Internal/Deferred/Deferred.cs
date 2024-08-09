using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using MicroWrath.Util;

using UniRx;

namespace MicroWrath.Deferred
{
    interface IDeferred<out A> : IObserver<Unit>
    {
        A Eval();
        IObservable<A> OnEvaluated { get; }
    }

    class Deferred<A> : IDeferred<A>
    {
        readonly Lazy<A> value;
        public Deferred(Func<A> getValue)
        {
            value = new(() => 
            {
                var value = getValue();
                Evaluated(value);
                return value;
            });
        }

        /// <exclude />
        private event Action<A> Evaluated = Functional.Ignore;

        public IObservable<A> OnEvaluated => Observable.FromEvent<A>(
            addHandler: handler => this.Evaluated += handler,
            removeHandler: handler => this.Evaluated -= handler);

        public A Eval() => value.Value;
        public void OnNext(Unit value) => this.Eval();
        public void OnError(Exception error) { }
        public void OnCompleted() { }
    }

    static partial class Deferred
    {
        public static readonly IDeferred<Unit> Empty = new Deferred<Unit>(() => Unit.Default);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDeferred<A> Return<A>(Func<A> get) => new Deferred<A>(get);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDeferred<B> Map<A, B>(this IDeferred<A> context, Func<A, B> map) =>
            Return(() => map(context.Eval()));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDeferred<B> Bind<A, B>(this IDeferred<A> context, Func<A, IDeferred<B>> binder) =>
            Return(() => binder(context.Eval()).Eval());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Func<IDeferred<A>, IDeferred<B>> Lift<A, B>(Func<A, B> f) => context => context.Map(f);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Func<IDeferred<A>, IDeferred<B>, IDeferred<C>> Lift2<A, B, C>(Func<A, B, C> f) =>
            (ca, cb) => ca.Bind(a => cb.Map(b => f(a, b)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDeferred<B> Apply<A, B>(this IDeferred<Func<A, B>> cf, IDeferred<A> ca) =>
            cf.Bind(f => ca.Map(f));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDeferred<C> Apply2<A, B, C>(
            this IDeferred<Func<A, B, C>> cf,
            IDeferred<A> ca,
            IDeferred<B> cb) =>
            cf.Map<Func<A, B, C>, Func<A, Func<B, C>>>(f => (A a) => (B b) => f(a, b))
                .Apply(ca)
                .Apply(cb);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDeferred<(A, B)> Combine<A, B>(this IDeferred<A> context, IDeferred<B> other) =>
            Lift2<A, B, (A, B)>((a, b) => (a, b))(context, other);

        public static IDeferred<IEnumerable<B>> Collect<A, B>(this IEnumerable<IDeferred<A>> source, Func<A, IEnumerable<B>> binder)
        {
            IEnumerable<B> getValues()
            {
                foreach (var value in source.SelectMany(c => binder(c.Eval())))
                    yield return value;
            }

            return Return(getValues);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDeferred<IEnumerable<A>> Collect<A>(this IEnumerable<IDeferred<A>> source) =>
            source.Collect<A, A>(x => [x]);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDeferred<Unit> Ignore<_>(this IDeferred<_> context) => context.Map(_ => Unit.Default);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDeferred<Option<B>> MapOption<A, B>(this IDeferred<Option<A>> context, Func<A, B> f)
            where A : notnull
            where B : notnull =>
            context.Map(value => value.Map(f));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDeferred<Option<B>> BindOption<A, B>(this IDeferred<Option<A>> context, Func<A, IDeferred<B>> f)
            where A : notnull
            where B : notnull =>
            context.Bind(value =>
            {
                var result = value.Map(f);

                if (result.IsSome)
                    return result.Value.Map(Option.Some);

                return Return(() => Option<B>.None);
            });
    }
}