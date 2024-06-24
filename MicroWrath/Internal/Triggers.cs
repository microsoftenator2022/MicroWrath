using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Localization;
using Kingmaker.Localization.Shared;

using UniRx;

namespace MicroWrath
{
    [HarmonyPatch]
    internal static partial class Triggers
    {
        private static event Action LocalizationManager_Init_PostfixEvent = () => { };

        [HarmonyPatch(typeof(LocalizationManager), nameof(LocalizationManager.Init))]
        [HarmonyPostfix]
        private static void LocalizationManager_Init_Prefix_Patch()
        {
            var timer = new Stopwatch();

            MicroLogger.Debug(() => $"Trigger {nameof(LocalizationManager_Init_Postfix)}");
            timer.Restart();

            LocalizationManager_Init_PostfixEvent();

            timer.Stop();
            MicroLogger.Debug(() => $"Trigger {nameof(LocalizationManager_Init_Postfix)} completed in {timer.ElapsedMilliseconds}ms");
        }

        public static readonly IObservable<Unit> LocalizationManager_Init_Postfix =
            Observable.FromEvent(
                addHandler: handler => LocalizationManager_Init_PostfixEvent += handler,
                removeHandler: handler => LocalizationManager_Init_PostfixEvent -= handler);

        private static event Action BlueprintsCache_Init_PrefixEvent = () => { };
        private static event Action BlueprintsCache_InitEvent_Early = () => { };
        private static event Action BlueprintsCache_InitEvent = () => { };
        
        [HarmonyPatch(typeof(BlueprintsCache), nameof(BlueprintsCache.Init))]
        [HarmonyPostfix]
        private static void BlueprintsCache_Init_Postfix_Patch()
        {
            var timer = new Stopwatch();

            MicroLogger.Debug(() => $"Trigger {nameof(BlueprintsCache_Init_Early)}");
            timer.Restart();

            BlueprintsCache_InitEvent_Early();

            timer.Stop();
            MicroLogger.Debug(() => $"Trigger {nameof(BlueprintsCache_Init_Early)} completed in {timer.ElapsedMilliseconds}ms");

            MicroLogger.Debug(() => $"Trigger {nameof(BlueprintsCache_Init)}");
            timer.Restart();

            BlueprintsCache_InitEvent();

            MicroLogger.Debug(() => $"Trigger {nameof(BlueprintsCache_Init)} completed in {timer.ElapsedMilliseconds}ms");
            timer.Stop();
        }

        [HarmonyPatch(typeof(BlueprintsCache), nameof(BlueprintsCache.Init))]
        [HarmonyPrefix]
        private static void BlueprintsCache_Init_Prefix_Patch()
        {
            var timer = new Stopwatch();
            MicroLogger.Debug(() => $"Trigger {nameof(BlueprintsCache_Init_Prefix)}");

            BlueprintsCache_Init_PrefixEvent();
            
            timer.Stop();
            MicroLogger.Debug(() => $"Trigger {nameof(BlueprintsCache_Init_Prefix)} completed in {timer.ElapsedMilliseconds}ms");
        }

        public static readonly IObservable<Unit> BlueprintsCache_Init_Prefix =
            Observable.FromEvent(
                addHandler: handler => BlueprintsCache_Init_PrefixEvent += handler,
                removeHandler: handler => BlueprintsCache_Init_PrefixEvent -= handler);

        public static readonly IObservable<Unit> BlueprintsCache_Init_Early =
            Observable.FromEvent(
                addHandler: handler => BlueprintsCache_InitEvent_Early += handler,
                removeHandler: handler => BlueprintsCache_InitEvent_Early -= handler);

        public static readonly IObservable<Unit> BlueprintsCache_Init =
            Observable.FromEvent(
                addHandler: handler => BlueprintsCache_InitEvent += handler,
                removeHandler: handler => BlueprintsCache_InitEvent -= handler);

        private static event Action<Locale> LocalizationManager_OnLocaleChangedEvent = _ => { };

        public static readonly IObservable<Locale> LocaleChanged =
            Observable.FromEvent<Locale>(
                addHandler: handler => LocalizationManager_OnLocaleChangedEvent += handler,
                removeHandler: handler => LocalizationManager_OnLocaleChangedEvent -= handler);

        [HarmonyPatch(typeof(LocalizationManager), nameof(LocalizationManager.OnLocaleChanged))]
        [HarmonyPrefix]
        private static void SwitchLanguage_Patch()
        {
            var timer = new Stopwatch();

            MicroLogger.Debug(() => $"Trigger {nameof(LocaleChanged)}");
            timer.Restart();

            LocalizationManager_OnLocaleChangedEvent(LocalizationManager.CurrentLocale);
            timer.Stop();
            MicroLogger.Debug(() => $"Trigger {nameof(LocaleChanged)} completed in {timer.ElapsedMilliseconds}ms");
        }

        private static event Action<BlueprintGuid> BlueprintLoad_PrefixEvent = _ => { };

        public static readonly IObservable<BlueprintGuid> BlueprintLoad_Prefix =
            Observable.FromEvent<BlueprintGuid>(
                addHandler: handler => BlueprintLoad_PrefixEvent += handler,
                removeHandler: handler => BlueprintLoad_PrefixEvent -= handler);

        //public static IObservable<Unit> BlueprintLoad_Prefix_ByGuid(BlueprintGuid guid) =>
        //    BlueprintLoad_Prefix.Where(loadGuid => loadGuid == guid).Select(_ => Unit.Default);

        [HarmonyPatch(typeof(BlueprintsCache), nameof(BlueprintsCache.Load))]
        [HarmonyPrefix]
        private static void BlueprintsCache_Load(BlueprintGuid guid)
        {
            //MicroLogger.Debug(() => $"Trigger {nameof(BlueprintsCache)}.{nameof(BlueprintsCache.Load)}({guid})");
            BlueprintLoad_PrefixEvent(guid);
        }
    }
}
