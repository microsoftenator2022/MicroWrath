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
        internal static BlueprintInitializationContext.ContextInitializer<TOther> Combine<TOther>(
            this BlueprintInitializationContext.ContextInitializer obj,
            BlueprintInitializationContext.ContextInitializer<TOther> other) =>
            obj.Map(() => new object()).Combine(other).Map(x => x.Right);

        internal static BlueprintInitializationContext.ContextInitializer<TBlueprint> GetBlueprint<TBlueprint>(
            this BlueprintInitializationContext.ContextInitializer obj,
            IMicroBlueprint<TBlueprint> blueprint)
            where TBlueprint : SimpleBlueprint =>
            obj.Map(() => new object()).Combine(blueprint).Map(x => x.Right);

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
        internal abstract class ContextInitializer
        {
            protected abstract BlueprintInitializationContext InitContext { get; }
            public abstract ContextInitializer Map(Action action);
            public abstract ContextInitializer<TResult> Map<TResult>(Func<TResult> selector);
            public abstract void Register();
        }

        internal abstract class ContextInitializer<T> : ContextInitializer
        {
            public abstract ContextInitializer Map(Action<T> action);
            public abstract ContextInitializer<TResult> Map<TResult>(Func<T, TResult> selector);
            public abstract ContextInitializer<(T Left, TOther Right)> Combine<TOther>(ContextInitializer<TOther> other);

            public virtual ContextInitializer<(T Left, TBlueprint Right)> Combine<TBlueprint>(IMicroBlueprint<TBlueprint> blueprint)
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

            /// <summary>
            /// Registers this initializer for execution
            /// </summary>
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
