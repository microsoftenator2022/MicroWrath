using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using HarmonyLib;

using Kingmaker.Localization;
using Kingmaker.Localization.Shared;

using Newtonsoft.Json;

using UnityModManagerNet;

namespace MicroLocalization
{
    public static class Main
    {
        private static UnityModManager.ModEntry ModEntry;
        private static Harmony Harmony;
        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            ModEntry = modEntry;

            Harmony = new Harmony(modEntry.Info.Id);
            
            Harmony.PatchAll();
            return true;
        }

        [HarmonyPatch]
        public static class Patch
        {
            [HarmonyPatch(typeof(LocalizationManager), "OnLocaleChanged")]
            [HarmonyPrefix]
            [HarmonyAfter("AlternateHumanTraits", "MiscTweaksAndFixes", "AlternateRacialTraits")]
            private static void SwitchLanguage_Patch()
            {
                DumpStrings();
            }
        }

        private static void DumpStrings()
        {
            foreach (var ass in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    if (ass is null) continue;
                
                    var types = ass.GetTypes();
                    if (types is null) continue;

                    if (types.FirstOrDefault(t => t.Name == "LocalizedStrings") is Type t &&
                        t.GetMethod("GetLocalizationPack") is MethodInfo mi)
                    {
                        ModEntry.Logger.Log($"Getting strings from {ass}");

                            var assDir = Path.GetDirectoryName(ass.Location);

                            var strings = new Dictionary<Locale, LocalizationPack>();

                            foreach (Locale locale in Enum.GetValues(typeof(Locale)))
                            {
                                if (mi.Invoke(null, new object[] { locale }) is LocalizationPack pack)
                                {
                                    if (pack.m_Strings.Count == 0) continue;

                                    ModEntry.Logger.Log($"{pack.m_Strings.Count} strings for {locale}");

                                    var filePath = Path.Combine(
                                        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                                        $"{ass.GetName().Name}.LocalizedStrings.{locale}.json");

                                    if (File.Exists(filePath)) 
                                    {
                                        ModEntry.Logger.Log($"{filePath} exists");

                                        if (LocalizationManager.CurrentLocale == locale)
                                            LocalizationManager.CurrentPack.AddStrings(
                                                JsonConvert.DeserializeObject<LocalizationPack>(
                                                    File.ReadAllText(filePath)));

                                        continue;
                                    }

                                    File.WriteAllText(filePath, JsonConvert.SerializeObject(pack, Formatting.Indented));

                                    ModEntry.Logger.Log($"Saved to {filePath}");
                                }
                            }
                        
                    }
                }
                catch (Exception e)
                {
                    ModEntry.Logger.LogException(e);
                }
            }
        }
    }
}
