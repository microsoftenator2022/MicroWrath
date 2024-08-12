using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using MicroWrath.Util;

using UniRx;

namespace MicroWrath.Deferred
{
    /// <summary>
    /// Deferred execution context returning <typeparamref name="A"/>.<br/>
    /// A deferred context is a function or collection of functions whose execution can be invoked at a later time.
    /// The return value may be memoized ie. execution of <see cref="Eval"/> only invokes the underlying function once.<br />
    /// In addition, this evaluation is also an <see cref="IObservable{A}"/> event source.<br />
    /// These contexts may be chained or combined to construct a deferrred "pipeline".<br />
    /// <br />
    /// See also: <seealso cref="Lazy{T}"/>, <seealso cref="IObservable{T}"/>
    /// </summary>
    interface IDeferred<out A> : IObserver<Unit>
    {
        /// <summary>
        /// Evaluate the context.
        /// </summary>
        /// <returns></returns>
        A Eval();

        /// <summary>
        /// Notifies subscribers on context execution.
        /// </summary>
        IObservable<A> OnEvaluated { get; }
    }

    /// <summary>
    /// Deferred execution context returning <typeparamref name="A"/>. Canonical implementation of <see cref="IDeferred{A}"/>.
    /// The return value is memoized ie. execution of <see cref="Eval"/> only invokes the underlying function once.<br />
    /// <br />
    /// See also: <seealso cref="Lazy{T}"/>, <seealso cref="IObservable{T}"/>
    /// </summary>
    /// <typeparam name="A"></typeparam>
    class Deferred<A> : IDeferred<A>
    {
        /// <exclude />
        readonly Lazy<A> value;

        /// <summary>
        /// Constructs a new context from the provided <see cref="Func{A}"/>.
        /// </summary>
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

        /// <inheritdoc />
        public IObservable<A> OnEvaluated => Observable.FromEvent<A>(
            addHandler: handler => this.Evaluated += handler,
            removeHandler: handler => this.Evaluated -= handler);

        /// <inheritdoc />
        public A Eval() => value.Value;

        /// <inheritdoc />
        public void OnNext(Unit value) => this.Eval();

        /// <inheritdoc />
        public void OnError(Exception error) { }

        /// <inheritdoc />
        public void OnCompleted() { }
    }

    /// <summary>
    /// Extensions for <see cref="IDeferred{A}"/> and <see cref="IDeferredBlueprint{TBlueprint}"/>
    /// </summary>
    static partial class Deferred
    {
        /// <summary>
        /// An context returning <see cref="Unit.Default"/>.
        /// </summary>
        public static readonly IDeferred<Unit> Empty = new Deferred<Unit>(() => Unit.Default);

        /// <summary>
        /// Constructs a new context of type <typeparamref name="A"/> from a parameterless function returning <typeparamref name="A"/>
        /// </summary>
        /// <typeparam name="A">Function return type</typeparam>
        /// <param name="get">Function</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDeferred<A> Return<A>(Func<A> get) => new Deferred<A>(get);

        /// <summary>
        /// Applies function to a context of type <typeparamref name="B"/>
        /// </summary>
        /// <typeparam name="A">Function parameter type</typeparam>
        /// <typeparam name="B">Function return type</typeparam>
        /// <param name="context">Source context</param>
        /// <param name="map">Function to apply</param>
        /// <returns>Context of type <typeparamref name="B"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDeferred<B> Map<A, B>(this IDeferred<A> context, Func<A, B> map) =>
            Return(() => map(context.Eval()));

        /// <summary>
        /// Applies function transforming a value of type <typeparamref name="A"/> to a context of <typeparamref name="B"/>
        /// to a context of type <typeparamref name="A"/> and flattens the result.
        /// </summary>
        /// <typeparam name="A">Function parameter type</typeparam>
        /// <typeparam name="B">Returned context type</typeparam>
        /// <param name="context">Source context</param>
        /// <param name="binder">Function transforming a value of type <typeparamref name="A"/> to a context of type <typeparamref name="B"/></param>
        /// <returns>Context of type <typeparamref name="B"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDeferred<B> Bind<A, B>(this IDeferred<A> context, Func<A, IDeferred<B>> binder) =>
            Return(() => binder(context.Eval()).Eval());

        /// <summary>
        /// "Lifts" a function transforming a value of type <typeparamref name="A"/> to a value of type <typeparamref name="B"/>,
        /// returning a function transforming a context of type <typeparamref name="A"/> to a context of type <typeparamref name="B"/>.
        /// </summary>
        /// <typeparam name="A">Function parameter type</typeparam>
        /// <typeparam name="B">Function return type</typeparam>
        /// <param name="f">Function to apply</param>
        /// <returns>Function transforming a context of type <typeparamref name="A"/> to a context of type <typeparamref name="B"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Func<IDeferred<A>, IDeferred<B>> Lift<A, B>(Func<A, B> f) => context => context.Map(f);

        /// <summary>
        /// "Lifst a function <see cref="Lift{A, B}(Func{A, B})"/>. See <see cref="Lift{A, B}(Func{A, B})"/>
        /// </summary>
        /// <typeparam name="A">First function parameter type</typeparam>
        /// <typeparam name="B">Second function parameter type</typeparam>
        /// <typeparam name="C">Function return type</typeparam>
        /// <param name="f">Function to apply</param>
        /// <returns>Function transforming a context of type <typeparamref name="A"/> and a context of type <typeparamref name="B"/>
        /// into a context of type <typeparamref name="C"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Func<IDeferred<A>, IDeferred<B>, IDeferred<C>> Lift2<A, B, C>(Func<A, B, C> f) =>
            (ca, cb) => ca.Bind(a => cb.Map(b => f(a, b)));

        /// <summary>
        /// Applies a function from a context that transforms a value of type <typeparamref name="A"/> to a value of type <typeparamref name="B"/>
        /// to a context of type <typeparamref name="A"/>
        /// </summary>
        /// <typeparam name="A">Function parameter type</typeparam>
        /// <typeparam name="B">Function return type</typeparam>
        /// <param name="cf">Source function context to apply</param>
        /// <param name="ca">Parameter context</param>
        /// <returns>Context of type <typeparamref name="B"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDeferred<B> Apply<A, B>(this IDeferred<Func<A, B>> cf, IDeferred<A> ca) =>
            cf.Bind(f => ca.Map(f));

        /// <summary>
        /// Applies a context function with 2 arguments. See <see cref="Apply{A, B}(IDeferred{Func{A, B}}, IDeferred{A})"/>.
        /// </summary>
        /// <typeparam name="A">First parameter type</typeparam>
        /// <typeparam name="B">Second parameter type</typeparam>
        /// <typeparam name="C">Return type</typeparam>
        /// <param name="cf">Source function to apply</param>
        /// <param name="ca">First parameter context</param>
        /// <param name="cb">Second parameter context</param>
        /// <returns>Context of type <typeparamref name="C"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDeferred<C> Apply2<A, B, C>(
            this IDeferred<Func<A, B, C>> cf,
            IDeferred<A> ca,
            IDeferred<B> cb) =>
            cf.Map<Func<A, B, C>, Func<A, Func<B, C>>>(f => (A a) => (B b) => f(a, b))
                .Apply(ca)
                .Apply(cb);

        /// <summary>
        /// Combines a context of type <typeparamref name="A"/> with a context of type <typeparamref name="B"/>
        /// </summary>
        /// <typeparam name="A">First context type</typeparam>
        /// <typeparam name="B">Second context type</typeparam>
        /// <param name="context">First context</param>
        /// <param name="other">Second context</param>
        /// <returns>Combined context of a tuple of <typeparamref name="A"/> and <typeparamref name="B"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDeferred<(A, B)> Combine<A, B>(this IDeferred<A> context, IDeferred<B> other) =>
            Lift2<A, B, (A, B)>((a, b) => (a, b))(context, other);

        /// <summary>
        /// Combines a collection of <see cref="IDeferred{A}"/> context into a single context, applying <paramref name="binder"/>
        /// and flattening the results.<br/>
        /// See also: <seealso cref="Collect{A}"/>
        /// </summary>
        /// <typeparam name="A">Source collection context type</typeparam>
        /// <typeparam name="B">Result context collection element type</typeparam>
        /// <param name="source">Source contexts</param>
        /// <param name="binder">Function transforming a value of type <typeparamref name="A"/> into a collection of type <typeparamref name="B"/></param>
        /// <returns>Context of a collection of type <typeparamref name="B"/></returns>
        public static IDeferred<IEnumerable<B>> Collect<A, B>(this IEnumerable<IDeferred<A>> source, Func<A, IEnumerable<B>> binder)
        {
            IEnumerable<B> getValues()
            {
                foreach (var value in source.SelectMany(c => binder(c.Eval())))
                    yield return value;
            }

            return Return(getValues);
        }

        /// <summary>
        /// Combines a collection of <see cref="IDeferred{A}"/> contexts into a single context of type <see cref="IEnumerable{A}"/>.
        /// </summary>
        /// <typeparam name="A">Source collection context type</typeparam>
        /// <param name="source">Source contexts</param>
        /// <returns>Context of a collection of type <typeparamref name="A"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDeferred<IEnumerable<A>> Collect<A>(this IEnumerable<IDeferred<A>> source) =>
            source.Collect<A, A>(x => [x]);

        /// <summary>
        /// Returns a context that evaluates another context and ignores the result
        /// </summary>
        /// <typeparam name="_">Ignored type</typeparam>
        /// <param name="context">Source context</param>
        /// <returns>Context of type <see cref="Unit"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDeferred<Unit> Ignore<_>(this IDeferred<_> context) => context.Map(_ => Unit.Default);

        /// <summary>
        /// Maps a function onto a context of an <see cref="Option{T}"/> of type <typeparamref name="A"/><br/>
        /// See also: <seealso cref="Option.Map{A, B}(Option{A}, Func{A, B})"/>, <seealso cref="Map{A, B}(IDeferred{A}, Func{A, B})"/>
        /// </summary>
        /// <typeparam name="A">Function parameter type</typeparam>
        /// <typeparam name="B">Function return type</typeparam>
        /// <param name="context">Source context</param>
        /// <param name="f">Function to apply</param>
        /// <returns>Context of <see cref="Option{T}"/> of type <typeparamref name="B"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDeferred<Option<B>> MapOption<A, B>(this IDeferred<Option<A>> context, Func<A, B> f)
            where A : notnull
            where B : notnull =>
            context.Map(value => value.Map(f));

        /// <summary>
        /// Applies a function that transforms a value of type <typeparamref name="A"/> to a context of type <typeparamref name="B"/>
        /// to a context of <see cref="Option{T}"/> of type <typeparamref name="A"/>, flattening the result.
        /// </summary>
        /// <typeparam name="A">Function parameter type</typeparam>
        /// <typeparam name="B">Function context return type</typeparam>
        /// <param name="context">Source context</param>
        /// <param name="f">Function to apply</param>
        /// <returns>Context of <see cref="Option{T}"/> of type <typeparamref name="B"/></returns>
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