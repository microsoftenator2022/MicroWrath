using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UniRx;

using Kingmaker.Blueprints;

using MicroWrath;
using MicroWrath.Constructors;
using MicroWrath.Util;

namespace MicroWrath
{
    internal interface IBlueprintInitializationContext
    {
        IBlueprintInitializationContext Select(Action action);
        IBlueprintInitializationContext<TResult> Select<TResult>(Func<TResult> selector);
        void Register();
    }

    internal interface IBlueprintInitializationContext<out T> : IBlueprintInitializationContext
    {
        IBlueprintInitializationContext Select(Action<T> action);
        IBlueprintInitializationContext<TResult> Select<TResult>(Func<T, TResult> selector);
    }

    internal partial class BlueprintInitializationContext : IObserver<Unit>
    {
        private bool registered = false;
        public bool IsRegistered => registered;

        private readonly IInitContextBlueprint Blueprint;
        private Action? BlueprintInitializer;

        private IDisposable? done;
        void IObserver<Unit>.OnNext(Unit value)
        {
            BlueprintInitializer?.Invoke();
            done?.Dispose();
        }

        void IObserver<Unit>.OnError(Exception error) { }
        void IObserver<Unit>.OnCompleted() => done?.Dispose();

        private void Register(IBlueprintInit bpContext)
        {
            BlueprintInitializer = bpContext.Execute;
            
            if (registered) done?.Dispose();

            done = Triggers.BlueprintsCache_Init.Subscribe(this);
            registered = true;
        }

        private BlueprintInitializationContext(IInitContextBlueprint blueprint)
        {
            Blueprint = blueprint;
        }

        public static IBlueprintInitializationContext<TBlueprint> AddBlueprint<TBlueprint>(string assetId, string name)
            where TBlueprint : SimpleBlueprint, new()
        {
            var blueprint = new InitContextBlueprint<TBlueprint>(assetId, name);
            var context = new BlueprintInitializationContext(blueprint);

            return new BlueprintInit<TBlueprint>(context, blueprint.CreateNew);
        }

        private interface IBlueprintInit { void Execute(); }

        private class BlueprintInit<T> : IBlueprintInitializationContext<T>, IBlueprintInit
        {
            private readonly BlueprintInitializationContext InitContext;
            private readonly Func<T> GetValue;
            internal bool Executed { get; private set; } = false;
            private T? value;
            internal T Value
            {
                get
                {
                    if (!Executed)
                        value = GetValue();

                    return value!;
                }
            }

            void IBlueprintInit.Execute() => GetValue();

            internal BlueprintInit(BlueprintInitializationContext initContext, Func<T> getValue)
            {
                InitContext = initContext;
                GetValue = getValue;
            }

            IBlueprintInitializationContext<TResult> IBlueprintInitializationContext<T>.Select<TResult>(Func<T, TResult> selector) =>
                new BlueprintInit<TResult>(InitContext, () =>
                {
                    return selector(Value);
                });

            IBlueprintInitializationContext<TResult> IBlueprintInitializationContext.Select<TResult>(Func<TResult> selector) =>
                new BlueprintInit<TResult>(InitContext, () =>
                { 
                    var _ = Value;
                    return selector();
                });

            IBlueprintInitializationContext IBlueprintInitializationContext<T>.Select(Action<T> action) =>
                new BlueprintInit<object>(InitContext, () =>
                {
                    action(Value);
                    return new();
                });

            IBlueprintInitializationContext IBlueprintInitializationContext.Select(Action action) =>
                new BlueprintInit<object>(InitContext, () =>
                {
                    var _ = Value;
                    action();
                    return new();
                });

            public void Register()
            {
                InitContext.Register(this);
            }
        }
    }
}
