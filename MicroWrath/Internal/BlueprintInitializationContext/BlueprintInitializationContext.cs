using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UniRx;

using Kingmaker.Blueprints;

using MicroWrath;
using MicroWrath.Constructors;
using MicroWrath.Util;

namespace MicroWrath.BlueprintInitializationContext
{

    internal partial class BlueprintInitializationContext
    {
        private readonly Dictionary<BlueprintGuid, IInitContextBlueprint> Blueprints = new();
        private readonly List<Action> Initializers = new();

        private readonly IObservable<Unit> Trigger;

        private IDisposable? done;
        private void Complete() => done?.Dispose();
        
        private void Register(IBlueprintInit bpContext, IEnumerable<IInitContextBlueprint> blueprints)
        {
            foreach (var bp in blueprints)
                Blueprints[bp.BlueprintGuid] = bp;

            Initializers.Add(bpContext.Execute);
            
            Complete();

            done = Trigger.Subscribe(Observer.Create<Unit>(
                onNext: _ =>
                {
                    foreach (var initAction in Initializers) initAction();

                    foreach (var (guid, mbp) in Blueprints.Select(kvp => (kvp.Key, kvp.Value)))
                    {
                        //MicroLogger.Debug(() => $"Adding blueprint {guid} {mbp.Name}");

                        if (ResourcesLibrary.BlueprintsCache.m_LoadedBlueprints.ContainsKey(guid))
                            MicroLogger.Warning($"BlueprintsCache already contains guid '{guid}'");

                        var bp = ResourcesLibrary.BlueprintsCache.AddCachedBlueprint(guid, mbp.Blueprint);

                        MicroLogger.Debug(() => $"Added {bp.NameSafe()}", bp.ToMicroBlueprint());
                    }

                    Complete();
                    Blueprints.Clear();
                    Initializers.Clear();
                },
                onError: _ => { },
                onCompleted: Complete));
        }

        /// <summary>
        /// Create a new context for blueprint initialization
        /// </summary>
        /// <param name="trigger">The event used to trigger evaluation of this context</param>
        internal BlueprintInitializationContext(IObservable<Unit> trigger) { Trigger = trigger; }

        /// <summary>
        /// Adds a new initializer to the context that adds a new blueprint to the library
        /// </summary>
        /// <typeparam name="TBlueprint">Blueprint type</typeparam>
        /// <param name="assetId">GUID for new blueprint</param>
        /// <param name="name">name for new blueprint</param>
        /// <returns>Blueprint initialization context for additional initialization steps</returns>
        public ContextInitializer<TBlueprint> NewBlueprint<TBlueprint>(string assetId, string name)
            where TBlueprint : SimpleBlueprint, new()
        {
            var microBlueprint = new InitContextBlueprint<TBlueprint>(assetId, name);

            return new BlueprintInit<TBlueprint>(this, new IInitContextBlueprint[] { microBlueprint }, () => microBlueprint.CreateNew());
        }

        public ContextInitializer<TBlueprint> NewBlueprint<TBlueprint>(BlueprintGuid guid, string name)
            where TBlueprint : SimpleBlueprint, new()
        {
            var microBlueprint = new InitContextBlueprint<TBlueprint>(guid, name);

            return new BlueprintInit<TBlueprint>(this, new IInitContextBlueprint[] { microBlueprint }, () => microBlueprint.CreateNew());
        }

        public ContextInitializer<TBlueprint> NewBlueprint<TBlueprint>(GeneratedGuid generatedGuid)
            where TBlueprint : SimpleBlueprint, new() =>
            NewBlueprint<TBlueprint>(generatedGuid.Guid, generatedGuid.Key);
 
        /// <summary>
        /// Adds a new initializer to the context for an existing blueprint
        /// </summary>
        /// <typeparam name="TBlueprint">Blueprint type</typeparam>
        /// <param name="blueprint">IMicroBlueprint blueprint reference</param>
        /// <returns>Blueprint initialization context for additional initialization steps</returns>
        public ContextInitializer<TBlueprint> GetBlueprint<TBlueprint>(IMicroBlueprint<TBlueprint> blueprint)
            where TBlueprint : SimpleBlueprint =>
            new BlueprintInit<TBlueprint>(this, Enumerable.Empty<IInitContextBlueprint>(), () => blueprint.ToReference());

        /// <summary>
        /// Adds an empty initializer to the context
        /// </summary>
        public ContextInitializer Empty => new BlueprintInit<object>(this, Enumerable.Empty<IInitContextBlueprint>(), () => new object());

        public ContextInitializer<TBlueprint> NewBlueprint<TBlueprint>(Func<TBlueprint> initFunc)
            where TBlueprint : SimpleBlueprint, new() =>
            new BlueprintInit<TBlueprint>(this, Enumerable.Empty<IInitContextBlueprint>(), () =>
            {
                var bp = initFunc();

                ResourcesLibrary.BlueprintsCache.AddCachedBlueprint(bp.AssetGuid, bp);

                return bp;
            });

        public ContextInitializer<IEnumerable<(TBlueprint, TState)>> NewBlueprints<TBlueprint, TState>(
            IEnumerable<(BlueprintGuid guid, string name, TState state)> values)
            where TBlueprint : SimpleBlueprint, new() =>
            values.Select(value => NewBlueprint<TBlueprint>(value.guid, value.name).Map(bp => (bp, value.state))).Combine();

        public ContextInitializer<TBlueprint> CloneBlueprint<TBlueprint>(IMicroBlueprint<TBlueprint> blueprint, BlueprintGuid guid, string name)
            where TBlueprint : SimpleBlueprint, new() =>
            NewBlueprint(() => AssetUtils.CloneBlueprint(blueprint.GetBlueprint()!, guid, name, false));

        public ContextInitializer<TBlueprint> CloneBlueprint<TBlueprint>(IMicroBlueprint<TBlueprint> blueprint, string assetId, string name)
            where TBlueprint : SimpleBlueprint, new() =>
            NewBlueprint(() => AssetUtils.CloneBlueprint(blueprint.GetBlueprint()!, BlueprintGuid.Parse(assetId), name, false));

        public ContextInitializer<TBlueprint> CloneBlueprint<TBlueprint>(IMicroBlueprint<TBlueprint> blueprint, GeneratedGuid generatedGuid)
            where TBlueprint : SimpleBlueprint, new() =>
            CloneBlueprint(blueprint, generatedGuid.Guid, generatedGuid.Key);
    }
}
