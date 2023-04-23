using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Localization;
using Kingmaker.Localization.Shared;

using UniRx;

namespace MicroWrath
{
    [HarmonyPatch]
    internal static partial class Triggers
    {
        private static event Action BlueprintsCache_InitEvent_Early = () => { };
        private static event Action BlueprintsCache_InitEvent = () => { };
        
        [HarmonyPatch(typeof(BlueprintsCache), nameof(BlueprintsCache.Init))]
        [HarmonyPostfix]
        private static void BlueprintsCache_Init_Patch()
        {
            BlueprintsCache_InitEvent_Early();
            BlueprintsCache_InitEvent();
        }

        public static readonly IObservable<Unit> BlueprintsCache_Init_Early =
            Observable.FromEvent(
                addHandler: handler => BlueprintsCache_InitEvent_Early += handler,
                removeHandler: handler => BlueprintsCache_InitEvent_Early -= handler);

        public static readonly IObservable<Unit> BlueprintsCache_Init =
            Observable.FromEvent(
                addHandler: handler => BlueprintsCache_InitEvent += handler,
                removeHandler: handler => BlueprintsCache_InitEvent -= handler);

        private static event Action<Locale> LocalizationManager_OnLocaleChangedEvent = _ => { };

        [HarmonyPatch(typeof(LocalizationManager), nameof(LocalizationManager.OnLocaleChanged))]
        [HarmonyPrefix]
        private static void SwitchLanguage_Patch() => 
            LocalizationManager_OnLocaleChangedEvent(LocalizationManager.CurrentLocale);

        public static readonly IObservable<Locale> LocaleChanged =
            Observable.FromEvent<Locale>(
                addHandler: handler => LocalizationManager_OnLocaleChangedEvent += handler,
                removeHandler: handler => LocalizationManager_OnLocaleChangedEvent -= handler);
    }
}
