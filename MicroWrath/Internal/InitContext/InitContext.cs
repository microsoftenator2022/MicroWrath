using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using MicroWrath.Util;

using UniRx;

namespace MicroWrath.InitContext
{
    interface IInitContext<out A> : IObserver<Unit>
    {
        A Eval();
        IObservable<A> OnEvaluated { get; }
    }

    class InitContext<A> : IInitContext<A>
    {
        readonly Lazy<A> value;
        public InitContext(Func<A> getValue)
        {
            value = new(() => 
            {
                var value = getValue();
                Evaluated(value);
                return value;
            });
        }

        private event Action<A> Evaluated = Functional.Ignore;

        public IObservable<A> OnEvaluated => Observable.FromEvent<A>(
            addHandler: handler => this.Evaluated += handler,
            removeHandler: handler => this.Evaluated -= handler);

        public A Eval() => value.Value;
        public void OnNext(Unit value) => this.Eval();
        public void OnError(Exception error) { }
        public void OnCompleted() { }
    }

    static partial class InitContext
    {
        public static readonly IInitContext<Unit> Empty = new InitContext<Unit>(() => Unit.Default);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IInitContext<A> Return<A>(Func<A> get) => new InitContext<A>(get);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IInitContext<B> Map<A, B>(this IInitContext<A> context, Func<A, B> map) =>
            Return(() => map(context.Eval()));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IInitContext<B> Bind<A, B>(this IInitContext<A> context, Func<A, IInitContext<B>> binder) =>
            Return(() => binder(context.Eval()).Eval());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Func<IInitContext<A>, IInitContext<B>> Lift<A, B>(Func<A, B> f) => context => context.Map(f);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Func<IInitContext<A>, IInitContext<B>, IInitContext<C>> Lift2<A, B, C>(Func<A, B, C> f) =>
            (ca, cb) => ca.Bind(a => cb.Map(b => f(a, b)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IInitContext<B> Apply<A, B>(this IInitContext<Func<A, B>> cf, IInitContext<A> ca) =>
            cf.Bind(f => ca.Map(f));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IInitContext<C> Apply2<A, B, C>(
            this IInitContext<Func<A, B, C>> cf,
            IInitContext<A> ca,
            IInitContext<B> cb) =>
            cf.Map<Func<A, B, C>, Func<A, Func<B, C>>>(f => (A a) => (B b) => f(a, b))
                .Apply(ca)
                .Apply(cb);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IInitContext<(A, B)> Combine<A, B>(this IInitContext<A> context, IInitContext<B> other) =>
            Lift2<A, B, (A, B)>((a, b) => (a, b))(context, other);

        public static IInitContext<IEnumerable<B>> Collect<A, B>(this IEnumerable<IInitContext<A>> source, Func<A, IEnumerable<B>> binder)
        {
            IEnumerable<B> getValues()
            {
                foreach (var value in source.SelectMany(c => binder(c.Eval())))
                    yield return value;
            }

            return Return(getValues);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IInitContext<IEnumerable<A>> Collect<A>(this IEnumerable<IInitContext<A>> source) =>
            source.Collect<A, A>(x => [x]);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IInitContext<Unit> Ignore<_>(this IInitContext<_> context) => context.Map(_ => Unit.Default);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IInitContext<Option<B>> MapOption<A, B>(this IInitContext<Option<A>> context, Func<A, B> f)
            where A : notnull
            where B : notnull =>
            context.Map(value => value.Map(f));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IInitContext<Option<B>> BindOption<A, B>(this IInitContext<Option<A>> context, Func<A, IInitContext<B>> f)
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