using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Kingmaker.Blueprints;

using MicroWrath.Util.Linq;

namespace MicroWrath.BlueprintInitializationContext
{
    internal static class BlueprintInitializationContextExtension
    {
        /// <inheritdoc cref="BlueprintInitializationContext.ContextInitializer{T}.Combine{TOther}(BlueprintInitializationContext.ContextInitializer{TOther})"/>
        internal static BlueprintInitializationContext.ContextInitializer<TOther> Combine<TOther>(
            this BlueprintInitializationContext.ContextInitializer obj,
            BlueprintInitializationContext.ContextInitializer<TOther> other) =>
            obj.Map(() => new object()).Combine(other).Map(x => x.Right);

        /// <inheritdoc cref="BlueprintInitializationContext.ContextInitializer{T}.GetBlueprint{TBlueprint}(IMicroBlueprint{TBlueprint})"/>
        internal static BlueprintInitializationContext.ContextInitializer<TBlueprint?> GetBlueprint<TBlueprint>(
            this BlueprintInitializationContext.ContextInitializer obj,
            IMicroBlueprint<TBlueprint> blueprint)
            where TBlueprint : SimpleBlueprint =>
            obj.Map(() => new object()).GetBlueprint(blueprint).Map(x => x.Right);

        /// <inheritdoc cref="BlueprintInitializationContext.ContextInitializer{T}.GetBlueprint{TBlueprint}(OwlcatBlueprint{TBlueprint})"/>
        internal static BlueprintInitializationContext.ContextInitializer<TBlueprint> GetBlueprint<TBlueprint>(
            this BlueprintInitializationContext.ContextInitializer obj,
            OwlcatBlueprint<TBlueprint> blueprint)
            where TBlueprint : SimpleBlueprint =>
            obj.Map(() => new object()).GetBlueprint(blueprint).Map(x => x.Right);

        /// <summary>
        /// Combines multiple initializer contexts.
        /// </summary>
        internal static BlueprintInitializationContext.ContextInitializer<IEnumerable<T>> Combine<T>(
            this IEnumerable<BlueprintInitializationContext.ContextInitializer<T>> bpcs)
        {
            var head = bpcs.First();
            var tail = bpcs.Skip(1);

            return tail.Aggregate(
                head.Map(EnumerableExtensions.Singleton),
                (acc, next) => acc.Combine(next).Map(x => x.Left.Append(x.Right)));
        }
    }

    internal partial class BlueprintInitializationContext
    { 
        /// <summary>
        /// Context representing initialization state with no explicit state value.
        /// </summary>
        internal abstract class ContextInitializer
        {
            protected abstract BlueprintInitializationContext InitContext { get; }

            /// <summary>
            /// Maps an action into this <see cref="BlueprintInitializationContext"/>.
            /// </summary>
            /// <param name="action">Mapped action</param>
            /// <returns>Context representing initialization state after evaluation of the mapped action. This context has no explicit state value.</returns>
            public abstract ContextInitializer Map(Action action);

            /// <summary>
            /// Maps a function into this <see cref="BlueprintInitializationContext"/>.
            /// </summary>
            /// <param name="action">Mapped function</param>
            /// <returns>Context representing initialization state after evaluation of the mapped function.</returns>
            public abstract ContextInitializer<TResult> Map<TResult>(Func<TResult> selector);

            /// <summary>
            /// Registers this initializer for execution
            /// </summary>
            public abstract void Register();
        }

        /// <summary>
        /// Context representing initialization state of type <typeparamref name="T"/>.
        /// </summary>
        internal abstract class ContextInitializer<T> : ContextInitializer
        {
            /// <inheritdoc cref="ContextInitializer.Map(Action)"/>
            public abstract ContextInitializer Map(Action<T> action);

            /// <inheritdoc cref="ContextInitializer.Map{TResult}(Func{TResult})"/>
            public abstract ContextInitializer<TResult> Map<TResult>(Func<T, TResult> selector);

            /// <summary>
            /// Product of two initialization states.
            /// </summary>
            /// <returns>Context representing the combined initialization state. The state value is the product of the states of the inputs.</returns>
            public abstract ContextInitializer<(T Left, TOther Right)> Combine<TOther>(ContextInitializer<TOther> other);

            /// <summary>
            /// Adds an <see cref="IMicroBlueprint{TBlueprint}"/> blueprint reference to this initializer. The blueprint may not be initialized within this context.
            /// </summary>
            public virtual ContextInitializer<(T Left, TBlueprint? Right)> GetBlueprint<TBlueprint>(IMicroBlueprint<TBlueprint> blueprint)
                where TBlueprint : SimpleBlueprint =>
                this.Combine(InitContext.GetBlueprint(blueprint));

            /// <summary>
            /// Adds an <see cref="OwlcatBlueprint{TBlueprint}"/> blueprint reference to this initializer. This blueprint is assumed to be initialized.
            /// </summary>
            public virtual ContextInitializer<(T Left, TBlueprint Right)> GetBlueprint<TBlueprint>(OwlcatBlueprint<TBlueprint> blueprint)
                where TBlueprint : SimpleBlueprint =>
                this.Combine(InitContext.GetBlueprint(blueprint));
        }

        private interface IBlueprintInit
        {
            void Execute();
        }

        private interface IBlueprintInit<T> : IBlueprintInit
        {
            Func<T> InitFunc { get; }
        }

        private class BlueprintInit<T> : ContextInitializer<T>, IBlueprintInit<T>
        {
            private readonly BlueprintInitializationContext initContext;
            protected override BlueprintInitializationContext InitContext => initContext;
            internal readonly IInitContextBlueprint[] Blueprints;

            private Lazy<T> lazyValue;

            private T GetValue() => lazyValue.Value;

            public Func<T> InitFunc => GetValue;

            void IBlueprintInit.Execute() => GetValue();

            internal BlueprintInit(BlueprintInitializationContext initContext, IInitContextBlueprint[] blueprints, Func<T> initFunc)
            {
                this.initContext = initContext;
                lazyValue = new(initFunc);
                Blueprints = new IInitContextBlueprint[blueprints.Length];
                blueprints.CopyTo((Span<IInitContextBlueprint>)Blueprints);

            }

            internal BlueprintInit(BlueprintInitializationContext initContext, IEnumerable<IInitContextBlueprint> blueprints, Func<T> getValue)
                : this(initContext, blueprints.ToArray(), getValue) { }

            private BlueprintInit<TResult> With<TResult>(Func<TResult> getValue) => new(initContext, Blueprints, getValue);


            public override ContextInitializer<TResult> Map<TResult>(Func<T, TResult> selector) =>
                With(() => selector(GetValue()));

            public override ContextInitializer<TResult> Map<TResult>(Func<TResult> selector) =>
                With(() =>
                {
                    GetValue();
                    return selector();
                });

            public override ContextInitializer Map(Action<T> action) =>
                With<object>(() =>
                {
                    action(GetValue());
                    return new();
                });

            public override ContextInitializer Map(Action action) =>
                With<object>(() =>
                {
                    GetValue();
                    action();
                    return new();
                });

            public override void Register() => initContext.Register(this, Blueprints);

            public override ContextInitializer<(T, TOther)> Combine<TOther>(ContextInitializer<TOther> other)
            {
                IEnumerable<IInitContextBlueprint> blueprints = this.Blueprints;

                if (other is BlueprintInit<TOther> otherBpInit)
                    blueprints = blueprints.Concat(otherBpInit.Blueprints);

                return new BlueprintInit<(T, TOther)>(initContext, blueprints, () => (this.InitFunc(), ((IBlueprintInit<TOther> )other).InitFunc()));
            }
        }
    }
}
