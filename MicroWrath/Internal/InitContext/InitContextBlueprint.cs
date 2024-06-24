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

namespace MicroWrath.InitContext
{
    interface IInitContextBlueprint<out TBlueprint>
        : IInitContext<TBlueprint>, IMicroBlueprint<TBlueprint>
        where TBlueprint : SimpleBlueprint
    { }

    class InitContextBlueprint<TBlueprint>(IInitContext<TBlueprint> context, BlueprintGuid guid)
        : IInitContextBlueprint<TBlueprint>
        where TBlueprint : SimpleBlueprint
    {
        readonly IInitContext<TBlueprint> thisContext = context.Map(blueprint =>
        {
            MicroLogger.Debug(() => $"Adding blueprint {blueprint} guid = {guid}");

            blueprint = (ResourcesLibrary.BlueprintsCache.AddCachedBlueprint(guid, blueprint) as TBlueprint) ?? throw new NullReferenceException();

            blueprint.OnEnable();

            return blueprint;
        });

        public TBlueprint Eval() => thisContext.Eval();
        public IObservable<TBlueprint> OnEvaluated => thisContext.OnEvaluated;

        public BlueprintGuid BlueprintGuid => guid;

        public TBlueprint? GetBlueprint() => thisContext.Eval();
        public void OnNext(Unit value) => thisContext.Eval();
        public void OnError(Exception error) => thisContext.OnError(error);
        public void OnCompleted() => thisContext.OnCompleted();
    }

    [HarmonyPatch]
    static class InitContextBlueprint
    {
        public static IInitContextBlueprint<TBlueprint> Bind<A, TBlueprint>(
            this IInitContext<A> context,
            Func<A, IInitContextBlueprint<TBlueprint>> binder, BlueprintGuid guid)
            where TBlueprint : SimpleBlueprint =>
            new InitContextBlueprint<TBlueprint>(context.Bind(binder), guid);

        public static IInitContextBlueprint<TBlueprint> AddBlueprintDeferred<TBlueprint>(
            this IInitContext<TBlueprint> context,
            BlueprintGuid guid)
            where TBlueprint : SimpleBlueprint =>
            new InitContextBlueprint<TBlueprint>(context, guid);

        public static IInitContextBlueprint<TBlueprint> AddBlueprintDeferred<TBlueprint>(
            this IInitContext<TBlueprint> context,
            IMicroBlueprint<TBlueprint> microBlueprint)
            where TBlueprint : SimpleBlueprint =>
            context.AddBlueprintDeferred(microBlueprint.BlueprintGuid);

        public static IInitContextBlueprint<TBlueprint> AddOnTrigger<TBlueprint>(
            this IInitContext<TBlueprint> context,
            BlueprintGuid guid,
            IObservable<Unit> trigger)
            where TBlueprint : SimpleBlueprint
        {
            var addBlueprint = context.AddBlueprintDeferred(guid);

            trigger.Take(1).Subscribe(addBlueprint);

            return addBlueprint;
        }

        public static IInitContextBlueprint<TBlueprint> AddOnTrigger<TBlueprint>(
            this IInitContext<TBlueprint> context,
            IMicroBlueprint<TBlueprint> microBlueprint,
            IObservable<Unit> trigger)
            where TBlueprint : SimpleBlueprint =>
            context.AddOnTrigger(microBlueprint.BlueprintGuid, trigger);

        [Obsolete]
        public static IInitContextBlueprint<TBlueprint> RegisterBlueprint<TBlueprint>(
            this IInitContext<TBlueprint> context,
            BlueprintGuid guid,
            IObservable<Unit> trigger)
            where TBlueprint : SimpleBlueprint =>
            AddOnTrigger(context, guid, trigger);

        public static IInitContext<TBlueprint> OnDemand<TBlueprint>(
            this IInitContext<TBlueprint> context,
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

        public static IInitContext<TBlueprint> OnDemand<TBlueprint>(
            this IInitContext<TBlueprint> context,
            IMicroBlueprint<TBlueprint> microBlueprint)
            where TBlueprint : SimpleBlueprint =>
            context.OnDemand(microBlueprint.BlueprintGuid);

[HarmonyReversePatch]
        [HarmonyPatch(typeof(BlueprintsCache), nameof(BlueprintsCache.Load))]
        internal static SimpleBlueprint LoadBlueprint(BlueprintsCache instance, BlueprintGuid guid) =>
            throw new NotImplementedException("STUB");

        internal static SimpleBlueprint? LoadBlueprint(BlueprintGuid guid) => LoadBlueprint(ResourcesLibrary.BlueprintsCache, guid);

        internal static TBlueprint? TryGetBlueprint<TBlueprint>(IMicroBlueprint<TBlueprint> microBlueprint)
            where TBlueprint : SimpleBlueprint
        {
            if (microBlueprint is IInitContextBlueprint<TBlueprint> context)
                return context.Eval();

            var blueprint = LoadBlueprint(microBlueprint.BlueprintGuid) as TBlueprint;

            return blueprint ?? ResourcesLibrary.BlueprintsCache.Load(microBlueprint.BlueprintGuid) as TBlueprint;
        }

        internal static TBlueprint TryGetBlueprint<TBlueprint>(OwlcatBlueprint<TBlueprint> blueprint)
            where TBlueprint : SimpleBlueprint =>
            (LoadBlueprint(blueprint.BlueprintGuid) as TBlueprint)!;
    }

    static partial class InitContext
    {
        public static IInitContext<TBlueprint> GetBlueprint<TBlueprint>(IInitContextBlueprint<TBlueprint> context)
            where TBlueprint : SimpleBlueprint => context;

        public static IInitContext<TBlueprint?> GetBlueprint<TBlueprint>(IMicroBlueprint<TBlueprint> blueprint)
            where TBlueprint : SimpleBlueprint =>
            new InitContext<TBlueprint?>(() => InitContextBlueprint.TryGetBlueprint(blueprint));

        public static IInitContext<TBlueprint> GetBlueprint<TBlueprint>(OwlcatBlueprint<TBlueprint> blueprint)
            where TBlueprint : SimpleBlueprint =>
            new InitContext<TBlueprint>(() => InitContextBlueprint.TryGetBlueprint(blueprint));

        public static IInitContext<TBlueprint> NewBlueprint<TBlueprint>(string assetId, string name)
            where TBlueprint : SimpleBlueprint, new()
        {
            var context = new InitContext<TBlueprint>(() =>
            {
                MicroLogger.Debug(() => $"Create new {typeof(TBlueprint)} {assetId} {name}");

                return Construct.New.Blueprint<TBlueprint>(assetId, name);
            });

            return context;
        }

        public static IInitContext<TBlueprint> NewBlueprint<TBlueprint>(BlueprintGuid guid, string name)
            where TBlueprint : SimpleBlueprint, new() =>
            NewBlueprint<TBlueprint>(guid.ToString(), name);

        public static IInitContext<TBlueprint> NewBlueprint<TBlueprint>(GeneratedGuid generatedGuid, string? name = null)
            where TBlueprint : SimpleBlueprint, new() =>
            NewBlueprint<TBlueprint>(generatedGuid.Guid, name ?? generatedGuid.Key);

        public static IInitContext<TBlueprint> CloneBlueprint<TBlueprint>(
            IMicroBlueprint<TBlueprint> blueprint,
            BlueprintGuid guid,
            string name)
            where TBlueprint : SimpleBlueprint, new() =>
            GetBlueprint(blueprint).Map(blueprint => AssetUtils.CloneBlueprint(blueprint!, guid, name));

        public static IInitContext<TBlueprint> CloneBlueprint<TBlueprint>(
            OwlcatBlueprint<TBlueprint> blueprint,
            BlueprintGuid guid,
            string name)
            where TBlueprint : SimpleBlueprint, new() =>
            GetBlueprint(blueprint).Map(blueprint => AssetUtils.CloneBlueprint(blueprint, guid, name, false));

        public static IInitContext<TBlueprint> CloneBlueprint<TBlueprint>(
            IMicroBlueprint<TBlueprint> blueprint,
            string assetId,
            string name)
            where TBlueprint : SimpleBlueprint, new() =>
            CloneBlueprint(blueprint, BlueprintGuid.Parse(assetId), name);

        public static IInitContext<TBlueprint> CloneBlueprint<TBlueprint>(
            OwlcatBlueprint<TBlueprint> blueprint,
            string assetId,
            string name)
            where TBlueprint : SimpleBlueprint, new() =>
            CloneBlueprint(blueprint, BlueprintGuid.Parse(assetId), name);

        public static IInitContext<TBlueprint> CloneBlueprint<TBlueprint>(
            IMicroBlueprint<TBlueprint> blueprint,
            GeneratedGuid generatedGuid,
            string? name = null)
            where TBlueprint : SimpleBlueprint, new() =>
            CloneBlueprint(blueprint, generatedGuid.Guid, name ?? generatedGuid.Key);

        public static IInitContext<TBlueprint> CloneBlueprint<TBlueprint>(
            OwlcatBlueprint<TBlueprint> blueprint,
            GeneratedGuid generatedGuid,
            string? name = null)
            where TBlueprint : SimpleBlueprint, new() =>
            CloneBlueprint(blueprint, generatedGuid.Guid, name ?? generatedGuid.Key);

        public static IInitContext<(TContext, TBlueprint?)> Combine<TContext, TMicroBlueprint, TBlueprint>(
            this IInitContext<TContext> context,
            TMicroBlueprint blueprint)
            where TMicroBlueprint : IMicroBlueprint<TBlueprint>
            where TBlueprint : SimpleBlueprint =>
            context.Combine(InitContext.GetBlueprint(blueprint));

        public static IInitContext<(TContext, TBlueprint)> Combine<TContext, TBlueprint>(
            this IInitContext<TContext> context,
            OwlcatBlueprint<TBlueprint> blueprint)
            where TBlueprint : SimpleBlueprint =>
            context.Combine(InitContext.GetBlueprint(blueprint));
    }
}
