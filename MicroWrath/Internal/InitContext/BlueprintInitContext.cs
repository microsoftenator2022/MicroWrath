using System;
using System.Collections.Generic;
using System.Linq;
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
    static class BlueprintInitContext
    {
        //public static IInitContextBlueprint<TBlueprint> RegisterBlueprint<TBlueprint>(
        //    this IInitContext<TBlueprint> context,
        //    BlueprintGuid guid)
        //    where TBlueprint : SimpleBlueprint
        //{
        //    var icb = new InitContextBlueprint<TBlueprint>(context, guid);

        //    Add(guid, icb);

        //    return icb;
        //}

        //public static IInitContextBlueprint<TBlueprint> RegisterBlueprint<TBlueprint>(
        //    this IInitContext<TBlueprint> context,
        //    IMicroBlueprint<TBlueprint> microBlueprint)
        //    where TBlueprint : SimpleBlueprint =>
        //    context.RegisterBlueprint(microBlueprint.BlueprintGuid);

        //public static IInitContextBlueprint<TBlueprint> RegisterBlueprint<TContext, TBlueprint>(
        //    this IInitContext<TContext> context,
        //    BlueprintGuid guid,
        //    Func<TContext, TBlueprint> selector)
        //    where TBlueprint : SimpleBlueprint =>
        //    context.Map(selector).RegisterBlueprint(guid);

        //public static IInitContextBlueprint<TBlueprint> RegisterBlueprint<TContext, TBlueprint>(
        //    this IInitContext<TContext> context,
        //    IMicroBlueprint<TBlueprint> microBlueprint,
        //    Func<TContext, TBlueprint> selector)
        //    where TBlueprint : SimpleBlueprint =>
        //    context.Map(selector).RegisterBlueprint(microBlueprint);

        public static IInitContextBlueprint<TBlueprint> Bind<A, TBlueprint>(
            this IInitContext<A> context,
            Func<A, IInitContextBlueprint<TBlueprint>> binder, BlueprintGuid guid)
            where TBlueprint : SimpleBlueprint =>
            new InitContextBlueprint<TBlueprint>(context.Bind(binder), guid);

        public static IInitContextBlueprint<TBlueprint> RegisterBlueprint<TBlueprint>(
            this IInitContext<TBlueprint> context,
            BlueprintGuid guid,
            IObservable<Unit> trigger)
            where TBlueprint : SimpleBlueprint
        {
            var icb = new InitContextBlueprint<TBlueprint>(context, guid);
            trigger.Take(1).Subscribe(icb);

            return icb;
        }

        static readonly Dictionary<BlueprintGuid, IInitContextBlueprint<SimpleBlueprint>> initContextBlueprints = [];

        static void Add(BlueprintGuid guid, IInitContextBlueprint<SimpleBlueprint> blueprint)
        {
            initContextBlueprints.Add(guid, blueprint);
        }

        [HarmonyPatch(typeof(BlueprintsCache), nameof(BlueprintsCache.Load))]
        [HarmonyPostfix]
        static void BlueprintsCache_Load_Postfix(BlueprintGuid guid, ref SimpleBlueprint? __result)
        {
            if (initContextBlueprints.TryGetValue(guid, out var blueprint))
            {
                initContextBlueprints.Remove(guid);
                __result = blueprint.Eval();
            }
        }
    }

    static partial class InitContext
    {
        public static IInitContext<TBlueprint?> GetBlueprint<TBlueprint>(IMicroBlueprint<TBlueprint> blueprint)
            where TBlueprint : SimpleBlueprint =>
            new InitContext<TBlueprint?>(() => blueprint.GetBlueprint() ?? throw new NullReferenceException());

        public static IInitContext<TBlueprint> GetBlueprint<TBlueprint>(OwlcatBlueprint<TBlueprint> blueprint)
            where TBlueprint : SimpleBlueprint =>
            new InitContext<TBlueprint>(() => blueprint.Blueprint);

        public static IInitContext<TBlueprint> NewBlueprint<TBlueprint>(string assetId, string name)
            where TBlueprint : SimpleBlueprint, new()
        {
            MicroLogger.Debug(() => $"Create new {typeof(TBlueprint)} {assetId} {name}");

            var context = new InitContext<TBlueprint>(() => Construct.New.Blueprint<TBlueprint>(assetId, name));

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