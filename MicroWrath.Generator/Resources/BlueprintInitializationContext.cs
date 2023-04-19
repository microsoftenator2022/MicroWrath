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

    internal partial class BlueprintInitializationContext
    {
        private readonly List<IInitContextBlueprint> Blueprints = new();
        private readonly List<Action> Initializers = new();

        private readonly IObservable<Unit> Trigger;

        private IDisposable? done;
        private void Complete() => done?.Dispose();
        
        private void Register(IBlueprintInit bpContext)
        {
            Initializers.Add(bpContext.Execute);
            
            Complete();

            done = Trigger.Subscribe(Observer.Create<Unit>(
                onNext: _ =>
                {
                    foreach (var bp in Blueprints) bp.CreateNew();
                    foreach (var initAction in Initializers) initAction();

                    Complete();
                    Blueprints.Clear();
                    Initializers.Clear();
                },
                onError: _ => { },
                onCompleted: Complete));
        }

        internal BlueprintInitializationContext(IObservable<Unit> trigger) { Trigger = trigger; }

        public IBlueprintInitializationContext<IMicroBlueprint<TBlueprint>> AddBlueprint<TBlueprint>(string assetId, string name)
            where TBlueprint : SimpleBlueprint, new()
        {
            var microBlueprint = new InitContextBlueprint<TBlueprint>(assetId, name);

            Blueprints.Add(microBlueprint);

            return new BlueprintInit<IMicroBlueprint<TBlueprint>>(this, new IInitContextBlueprint[] { microBlueprint }, () => microBlueprint);
        }

        private interface IBlueprintInit { void Execute(); }

        private class BlueprintInit<T> : IBlueprintInitializationContext<T>, IBlueprintInit
        {
            private readonly BlueprintInitializationContext InitContext;
            internal readonly IInitContextBlueprint[] Blueprints;

            private readonly Func<T> InitFunc;
            internal bool HasValue { get; private set; } = false;

            private T GetValue()
            {
                value = InitFunc();
                HasValue = true;

                return value;
            }

            private T? value;
            internal T Value
            {
                get
                {
                    if (!HasValue) return GetValue();

                    return value!;
                }
            }

            void IBlueprintInit.Execute() => GetValue();

            internal BlueprintInit(BlueprintInitializationContext initContext, IInitContextBlueprint[] blueprints, Func<T> initFunc)
            {
                InitContext = initContext;
                InitFunc = initFunc;
                Blueprints = new IInitContextBlueprint[blueprints.Length];
                blueprints.CopyTo((Span<IInitContextBlueprint>)Blueprints);
            }

            internal BlueprintInit(BlueprintInitializationContext initContext, IEnumerable<IInitContextBlueprint> blueprints, Func<T> getValue)
                : this(initContext, blueprints.ToArray(), getValue) { }

            private BlueprintInit<TResult> With<TResult>(Func<TResult> getValue) => new(InitContext, Blueprints, getValue);

            IBlueprintInitializationContext<TResult> IBlueprintInitializationContext<T>.Select<TResult>(Func<T, TResult> selector) =>
                With(() => selector(Value));

            IBlueprintInitializationContext<TResult> IBlueprintInitializationContext.Select<TResult>(Func<TResult> selector) =>
                With(() =>
                { 
                    GetValue();
                    return selector();
                });

            IBlueprintInitializationContext IBlueprintInitializationContext<T>.Select(Action<T> action) =>
                With<object>(() =>
                {
                    action(Value);
                    return new();
                });

            IBlueprintInitializationContext IBlueprintInitializationContext.Select(Action action) =>
                With<object>(() =>
                {
                    GetValue();
                    action();
                    return new();
                });

            public void Register() => InitContext.Register(this);
        }
    }
}
