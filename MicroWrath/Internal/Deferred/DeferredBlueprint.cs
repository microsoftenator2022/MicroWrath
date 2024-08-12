using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Contexts;
using System.Threading.Tasks;

using HarmonyLib;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Modding;

using MicroWrath;
using MicroWrath.Constructors;
using MicroWrath.Util;
using MicroWrath.Util.Linq;

using Owlcat.Runtime.Core;

using UniRx;

namespace MicroWrath.Deferred
{
    /// <summary>
    /// <see cref="IDeferred{TBlueprint}"/> that also implements <see cref="IMicroBlueprint{TBlueprint}"/>.
    /// </summary>
    /// <typeparam name="TBlueprint">Blueprint type</typeparam>
    interface IDeferredBlueprint<out TBlueprint>
        : IDeferred<TBlueprint>, IMicroBlueprint<TBlueprint>, IMicroBlueprintReference<BlueprintReferenceBase, TBlueprint>
        where TBlueprint : SimpleBlueprint
    { }

    /// <summary>
    /// A deferred execution context returning a <typeparamref name="TBlueprint"/>.
    /// Canonical implmentation of <see cref="IDeferredBlueprint{TBlueprint}"/>.
    /// The value of the returned blueprint's <see cref="SimpleBlueprint.AssetGuid"/> is replaced with the <paramref name="guid"/> parameter.
    /// </summary>
    /// <typeparam name="TBlueprint">Blueprint type.</typeparam>
    /// <param name="context">Source <see cref="IDeferred{TBlueprint}"/> context.</param>
    /// <param name="guid">Guid of returned blueprint.</param>
    /// <exception cref="NullReferenceException">If returned blueprint is null.</exception>
    class DeferredBlueprint<TBlueprint>(IDeferred<TBlueprint> context, BlueprintGuid guid)
        : IDeferredBlueprint<TBlueprint>
        where TBlueprint : SimpleBlueprint
    {
        readonly IDeferred<TBlueprint> thisContext = context.Map(blueprint =>
        {
            MicroLogger.Debug(() => $"Adding blueprint {blueprint} guid = {guid}");

            if (blueprint.AssetGuid != guid)
            {
                MicroLogger.Warning($"Blueprint guid {blueprint.AssetGuid} does not match provided guid {guid}. Replacing blueprint guid.");

                blueprint.AssetGuid = guid;
            }

            blueprint = (ResourcesLibrary.BlueprintsCache.AddCachedBlueprint(guid, blueprint) as TBlueprint) ?? throw new NullReferenceException();

            blueprint.OnEnable();

            return blueprint;
        });

        /// <inheritdoc />
        public TBlueprint Eval() => thisContext.Eval();

        /// <inheritdoc />
        public IObservable<TBlueprint> OnEvaluated => thisContext.OnEvaluated;

        /// <inheritdoc />
        public BlueprintGuid BlueprintGuid => guid;

        /// <inheritdoc />
        public TBlueprint? GetBlueprint() => thisContext.Eval();

        /// <exlude />
        bool referenced = false;

        /// <inheritdoc />
        BlueprintReferenceBase IMicroBlueprintReference<BlueprintReferenceBase, TBlueprint>.ToReference()
        {
            if (!referenced) _ = this.OnDemand(this.BlueprintGuid);

            referenced = true;

            return new() { deserializedGuid = this.BlueprintGuid };
        }

        /// <inheritdoc />
        public void OnNext(Unit value) => thisContext.Eval();

        /// <inheritdoc />
        public void OnError(Exception error) => thisContext.OnError(error);

        /// <inheritdoc />
        public void OnCompleted() => thisContext.OnCompleted();
    }

    [HarmonyPatch]
    internal static class DeferredBlueprint
    {
        public static IDeferredBlueprint<TBlueprint> Bind<A, TBlueprint>(
            this IDeferred<A> context,
            Func<A, IDeferredBlueprint<TBlueprint>> binder, BlueprintGuid guid)
            where TBlueprint : SimpleBlueprint =>
            new DeferredBlueprint<TBlueprint>(context.Bind(binder), guid);

        /// <summary>
        /// When this context executes, add the provided blueprint to the library. Replaces any existing blueprint with the same guid.
        /// </summary>
        /// <typeparam name="TBlueprint">Blueprint type</typeparam>
        /// <param name="context">Source context</param>
        /// <param name="guid">Guid of blueprint to add</param>
        /// <returns><see cref="IDeferredBlueprint{TBlueprint}"/> context that returns the blueprint after it is added</returns>
        public static IDeferredBlueprint<TBlueprint> AddBlueprintDeferred<TBlueprint>(
            this IDeferred<TBlueprint> context,
            BlueprintGuid guid)
            where TBlueprint : SimpleBlueprint =>
            new DeferredBlueprint<TBlueprint>(context, guid);

        /// <param name="context">Source context</param>
        /// <param name="microBlueprint"><see cref="IMicroBlueprint{TBlueprint}"/> with guid of blueprint to add</param>
        /// <inheritdoc cref="AddBlueprintDeferred{TBlueprint}(IDeferred{TBlueprint}, BlueprintGuid)" />
        public static IDeferredBlueprint<TBlueprint> AddBlueprintDeferred<TBlueprint>(
            this IDeferred<TBlueprint> context,
            IMicroBlueprint<TBlueprint> microBlueprint)
            where TBlueprint : SimpleBlueprint =>
            context.AddBlueprintDeferred(microBlueprint.BlueprintGuid);

        /// <summary>
        /// Executes this context and adds the resulting blueprint to the library in response to an <see cref="IObservable{Unit}"/>.
        /// </summary>
        /// <typeparam name="TBlueprint">Blueprint type</typeparam>
        /// <param name="context">Source context</param>
        /// <param name="guid">Guid of blueprint to add</param>
        /// <param name="trigger"><see cref="IObservable{Unit}"/> to observe</param>
        /// <returns><see cref="IDeferredBlueprint{TBlueprint}"/> context that returns the blueprint after it is added</returns>
        public static IDeferredBlueprint<TBlueprint> AddOnTrigger<TBlueprint>(
            this IDeferred<TBlueprint> context,
            BlueprintGuid guid,
            IObservable<Unit> trigger)
            where TBlueprint : SimpleBlueprint
        {
            var addBlueprint = context.AddBlueprintDeferred(guid);

            trigger.Take(1).Subscribe(addBlueprint);

            return addBlueprint;
        }

        /// <param name="context">Source context</param>
        /// <param name="microBlueprint"><see cref="IMicroBlueprint{TBlueprint}"/> with guid of blueprint to add</param>
        /// <param name="trigger"><see cref="IObservable{Unit}"/> to observe</param>
        /// <inheritdoc cref="AddOnTrigger{TBlueprint}(IDeferred{TBlueprint}, BlueprintGuid, IObservable{Unit})" />
        public static IDeferredBlueprint<TBlueprint> AddOnTrigger<TBlueprint>(
            this IDeferred<TBlueprint> context,
            IMicroBlueprint<TBlueprint> microBlueprint,
            IObservable<Unit> trigger)
            where TBlueprint : SimpleBlueprint =>
            context.AddOnTrigger(microBlueprint.BlueprintGuid, trigger);

        /// <summary>
        /// Add this blueprint to library on demand via <see cref="Triggers.BlueprintLoad_Prefix"/>
        /// </summary>
        /// <typeparam name="TBlueprint">Blueprint type</typeparam>
        /// <param name="context">This <see cref="IDeferredBlueprint{TBlueprint}"/></param>
        /// <param name="guid">Blueprint's guid</param>
        /// <returns><see cref="IDeferredBlueprint{TBlueprint}"/> that adds the blueprint to library</returns>
        public static IDeferredBlueprint<TBlueprint> OnDemand<TBlueprint>(
            this IDeferred<TBlueprint> context,
            BlueprintGuid guid)
            where TBlueprint : SimpleBlueprint =>
                context
#if DEBUG
                .Map(blueprint =>
                {
                    MicroLogger.Debug(() => $"{nameof(OnDemand)} {guid} {blueprint.name}");

                    return blueprint;
                })
#endif
                .AddOnTrigger(
                    guid,
                    Triggers.BlueprintLoad_Prefix
                        .Where(g => guid == g)
                        .Select(_ => Unit.Default));

        /// <param name="context">This <see cref="IDeferredBlueprint{TBlueprint}"/></param>
        /// <param name="microBlueprint"><see cref="IMicroBlueprint{TBlueprint}"/> to get guid from.</param>
        /// <inheritdoc cref="OnDemand{TBlueprint}(IDeferred{TBlueprint}, BlueprintGuid)"/>
        public static IDeferredBlueprint<TBlueprint> OnDemand<TBlueprint>(
            this IDeferred<TBlueprint> context,
            IMicroBlueprint<TBlueprint> microBlueprint)
            where TBlueprint : SimpleBlueprint =>
            context.OnDemand(microBlueprint.BlueprintGuid);

        /// <exclude />
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(BlueprintsCache), nameof(BlueprintsCache.Load))]
        internal static SimpleBlueprint LoadBlueprint(BlueprintsCache instance, BlueprintGuid guid) =>
            throw new NotImplementedException("STUB");

        /// <summary>
        /// Load a blueprint from the library, skipping any triggers defined by this mod.
        /// </summary>
        /// <param name="guid">Guid of blueprint to load</param>
        /// <returns>Blueprint or null if the blueprint is not present in the library</returns>
        internal static SimpleBlueprint? LoadBlueprintDirect(BlueprintGuid guid) => LoadBlueprint(ResourcesLibrary.BlueprintsCache, guid);

        /// <summary>
        /// If the provided <see cref="IMicroBlueprint{TBlueprint}"/> is a <see cref="IDeferredBlueprint{TBlueprint}"/>, evaluate the context and return the result.
        /// Otherwise, tries to load the blueprint from the library, skipping any triggers as with <see cref="LoadBlueprintDirect(BlueprintGuid)"/>.
        /// </summary>
        /// <typeparam name="TBlueprint">Blueprint type</typeparam>
        /// <param name="microBlueprint">Blueprint to load</param>
        /// <returns>Blueprint or null if the blueprint is not found</returns>
        internal static TBlueprint? TryGetBlueprint<TBlueprint>(IMicroBlueprint<TBlueprint> microBlueprint)
            where TBlueprint : SimpleBlueprint
        {
            if (microBlueprint is IDeferredBlueprint<TBlueprint> context)
                return context.Eval();

            var blueprint = LoadBlueprintDirect(microBlueprint.BlueprintGuid) as TBlueprint;

            return blueprint ?? ResourcesLibrary.BlueprintsCache.Load(microBlueprint.BlueprintGuid) as TBlueprint;
        }

        /// <typeparam name="TBlueprint">Blueprint type</typeparam>
        /// <param name="blueprint">Blueprint to load</param>
        /// <inheritdoc cref="LoadBlueprintDirect(BlueprintGuid)" />
        internal static TBlueprint TryGetBlueprintDirect<TBlueprint>(OwlcatBlueprint<TBlueprint> blueprint)
            where TBlueprint : SimpleBlueprint =>
            (LoadBlueprintDirect(blueprint.BlueprintGuid) as TBlueprint)!;
    }

    static partial class Deferred
    {
        /// <summary>
        /// No-op. Can be used to upcast a blueprint type
        /// </summary>
        /// <typeparam name="TBlueprint">Blueprint type</typeparam>
        /// <returns><paramref name="context"/></returns>
        public static IDeferred<TBlueprint> GetBlueprint<TBlueprint>(IDeferredBlueprint<TBlueprint> context)
            where TBlueprint : SimpleBlueprint => context;

        /// <summary>
        /// See <see cref="DeferredBlueprint.TryGetBlueprint{TBlueprint}(IMicroBlueprint{TBlueprint})"/>
        /// </summary>
        /// <typeparam name="TBlueprint">Blueprint type</typeparam>
        /// <param name="blueprint">Blueprint to load</param>
        /// <returns>Context containing the a blueprint or null if it is not found</returns>
        public static IDeferred<TBlueprint?> GetBlueprint<TBlueprint>(IMicroBlueprint<TBlueprint> blueprint)
            where TBlueprint : SimpleBlueprint =>
            new Deferred<TBlueprint?>(() => DeferredBlueprint.TryGetBlueprint(blueprint));

        /// <summary>
        /// See <see cref="DeferredBlueprint.TryGetBlueprintDirect{TBlueprint}(OwlcatBlueprint{TBlueprint})"/>
        /// </summary>
        /// <typeparam name="TBlueprint">Blueprint type</typeparam>
        /// <param name="blueprint">Blueprint to load</param>
        /// <returns>Blueprint</returns>
        public static IDeferred<TBlueprint> GetBlueprint<TBlueprint>(OwlcatBlueprint<TBlueprint> blueprint)
            where TBlueprint : SimpleBlueprint =>
            new Deferred<TBlueprint>(() => DeferredBlueprint.TryGetBlueprintDirect(blueprint));

        public static IDeferred<TBlueprint> NewBlueprint<TBlueprint>(string assetId, string name)
            where TBlueprint : SimpleBlueprint, new()
        {
            var context = new Deferred<TBlueprint>(() =>
            {
                MicroLogger.Debug(() => $"Create new {typeof(TBlueprint)} {assetId} {name}");

                return Construct.New.Blueprint<TBlueprint>(assetId, name);
            });

            return context;
        }

        /// <summary>
        /// Creates a <see cref="IDeferred{A}"/> context that constructs a new blueprint<br/>
        /// See also: <seealso cref="Construct.New.Blueprint{TBlueprint}(string, string)"/>, <seealso cref="Construct.New.Blueprint{TBlueprint}(GeneratedGuid)"/>
        /// </summary>
        /// <typeparam name="TBlueprint">New blueprint type</typeparam>
        /// <param name="guid">New blueprint guid</param>
        /// <param name="name">New blueprint name</param>
        /// <returns>Context of type <typeparamref name="TBlueprint"/></returns>
        public static IDeferred<TBlueprint> NewBlueprint<TBlueprint>(BlueprintGuid guid, string name)
            where TBlueprint : SimpleBlueprint, new() =>
            NewBlueprint<TBlueprint>(guid.ToString(), name);

        /// <inheritdoc cref="NewBlueprint{TBlueprint}(BlueprintGuid, string)"/>
        public static IDeferred<TBlueprint> NewBlueprint<TBlueprint>(GeneratedGuid generatedGuid, string? name = null)
            where TBlueprint : SimpleBlueprint, new() =>
            NewBlueprint<TBlueprint>(generatedGuid.Guid, name ?? generatedGuid.Key);

        /// <summary>
        /// Creates a context that clones a blueprint.<br/>
        /// See also: <seealso cref="AssetUtils.CloneBlueprint{TBlueprint}(TBlueprint, BlueprintGuid, string?, bool)"/>
        /// </summary>
        /// <typeparam name="TBlueprint">Blueprint type</typeparam>
        /// <param name="blueprint">Original blueprint</param>
        /// <param name="guid">New blueprint guid</param>
        /// <param name="name">New blueprint name</param>
        /// <returns>Context of type <typeparamref name="TBlueprint"/></returns>
        public static IDeferred<TBlueprint> CloneBlueprint<TBlueprint>(
            IMicroBlueprint<TBlueprint> blueprint,
            BlueprintGuid guid,
            string name)
            where TBlueprint : SimpleBlueprint, new() =>
            GetBlueprint(blueprint).Map(blueprint => AssetUtils.CloneBlueprint(blueprint!, guid, name));

        /// <inheritdoc cref="CloneBlueprint{TBlueprint}(IMicroBlueprint{TBlueprint}, BlueprintGuid, string)"/>
        public static IDeferred<TBlueprint> CloneBlueprint<TBlueprint>(
            OwlcatBlueprint<TBlueprint> blueprint,
            BlueprintGuid guid,
            string name)
            where TBlueprint : SimpleBlueprint, new() =>
            GetBlueprint(blueprint).Map(blueprint => AssetUtils.CloneBlueprint(blueprint, guid, name, false));

        /// <param name="blueprint">Original blueprint</param>
        /// <param name="assetId">New blueprint guid</param>
        /// <param name="name">New blueprint name</param>
        /// <inheritdoc cref="CloneBlueprint{TBlueprint}(IMicroBlueprint{TBlueprint}, BlueprintGuid, string)"/>
        public static IDeferred<TBlueprint> CloneBlueprint<TBlueprint>(
            IMicroBlueprint<TBlueprint> blueprint,
            string assetId,
            string name)
            where TBlueprint : SimpleBlueprint, new() =>
            CloneBlueprint(blueprint, BlueprintGuid.Parse(assetId), name);

        /// <param name="blueprint">Original blueprint</param>
        /// <param name="assetId">New blueprint guid</param>
        /// <param name="name">New blueprint name</param>
        /// <inheritdoc cref="CloneBlueprint{TBlueprint}(IMicroBlueprint{TBlueprint}, BlueprintGuid, string)"/>
        public static IDeferred<TBlueprint> CloneBlueprint<TBlueprint>(
            OwlcatBlueprint<TBlueprint> blueprint,
            string assetId,
            string name)
            where TBlueprint : SimpleBlueprint, new() =>
            CloneBlueprint(blueprint, BlueprintGuid.Parse(assetId), name);

        /// <param name="blueprint">Original blueprint</param>
        /// <param name="generatedGuid">New blueprint guid</param>
        /// <param name="name">New blueprint name</param>
        /// <inheritdoc cref="CloneBlueprint{TBlueprint}(IMicroBlueprint{TBlueprint}, BlueprintGuid, string)"/>
        public static IDeferred<TBlueprint> CloneBlueprint<TBlueprint>(
            IMicroBlueprint<TBlueprint> blueprint,
            GeneratedGuid generatedGuid,
            string? name = null)
            where TBlueprint : SimpleBlueprint, new() =>
            CloneBlueprint(blueprint, generatedGuid.Guid, name ?? generatedGuid.Key);

        /// <param name="blueprint">Original blueprint</param>
        /// <param name="generatedGuid">New blueprint guid</param>
        /// <param name="name">New blueprint name</param>
        /// <inheritdoc cref="CloneBlueprint{TBlueprint}(IMicroBlueprint{TBlueprint}, BlueprintGuid, string)"/>
        public static IDeferred<TBlueprint> CloneBlueprint<TBlueprint>(
            OwlcatBlueprint<TBlueprint> blueprint,
            GeneratedGuid generatedGuid,
            string? name = null)
            where TBlueprint : SimpleBlueprint, new() =>
            CloneBlueprint(blueprint, generatedGuid.Guid, name ?? generatedGuid.Key);

        /// <summary>
        /// Combines a context of type <typeparamref name="TContext"/> with a <typeparamref name="TMicroBlueprint"/> reference
        /// </summary>
        /// <typeparam name="TContext">Source context type</typeparam>
        /// <typeparam name="TMicroBlueprint"><see cref="IMicroBlueprint{TBlueprint}"/> type</typeparam>
        /// <typeparam name="TBlueprint">Blueprint type</typeparam>
        /// <param name="context">Source context</param>
        /// <param name="blueprint"><see cref="IMicroBlueprint{TBlueprint}"/> reference</param>
        /// <returns>Context of a tuple of <typeparamref name="TContext"/> and <typeparamref name="TBlueprint"/>.
        /// <typeparamref name="TBlueprint"/> value is null if the dereference fails</returns>
        public static IDeferred<(TContext, TBlueprint?)> Combine<TContext, TMicroBlueprint, TBlueprint>(
            this IDeferred<TContext> context,
            TMicroBlueprint blueprint)
            where TMicroBlueprint : IMicroBlueprint<TBlueprint>
            where TBlueprint : SimpleBlueprint =>
            context.Combine(Deferred.GetBlueprint(blueprint));

        /// <summary>
        /// Combines a context of type <typeparamref name="TContext"/> with a <see cref="OwlcatBlueprint{TBlueprint}"/> reference
        /// </summary>
        /// <typeparam name="TContext">Source context type</typeparam>
        /// <typeparam name="TBlueprint">Blueprint type</typeparam>
        /// <param name="context">Source context</param>
        /// <param name="blueprint"><see cref="OwlcatBlueprint{TBlueprint}"/> reference</param>
        /// <returns>Context of a tuple of <typeparamref name="TContext"/> and <typeparamref name="TBlueprint"/>.
        /// </returns>
        public static IDeferred<(TContext, TBlueprint)> Combine<TContext, TBlueprint>(
            this IDeferred<TContext> context,
            OwlcatBlueprint<TBlueprint> blueprint)
            where TBlueprint : SimpleBlueprint =>
            context.Combine(Deferred.GetBlueprint(blueprint));
    }
}
