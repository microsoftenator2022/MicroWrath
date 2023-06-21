using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Blueprints;
using Kingmaker.Modding;

using Owlcat.Runtime.Core.Logging;

using UnityModManagerNet;

namespace MicroWrath
{
    internal static class MicroLogger
    {
        internal enum Severity
        {
            Debug = -1,
            Info = 0,
            Warning = 1,
            Error = 2,
            Critical = 3
        }

        internal readonly record struct Entry(Func<string> Message, Severity Severity = Severity.Info, Exception? Exception = null)
        {
            internal IMicroBlueprint<SimpleBlueprint>? Blueprint { get; init; }
        }

        private static readonly List<Entry> EntryList = new();
        public static IEnumerable<Entry> Entries = EntryList.ToArray();

        private static Severity logLevel =
#if DEBUG
            Severity.Debug;
#else
            Severity.Info;
#endif
        public static void SetLogLevel(Severity severity)
        {
            if (logLevel == severity) return;

            AddEntry(new(() => $"Setting UMM log severity to {severity}"));
            logLevel = severity;
        }

        public static Severity LogLevel
        {
            get => logLevel;
            set => SetLogLevel(value);
        }

        private static UnityModManager.ModEntry? modEntry;
        public static UnityModManager.ModEntry? ModEntry
        {
            get => modEntry;
            set
            {
                var oldModEntry = modEntry;
                modEntry = value;

                if (oldModEntry == null) ReplayLogUmm();
            }
        }

        private static void UmmLog(Entry entry)
        {
            if (entry.Severity < logLevel) return;

            if (ModEntry is null) return;

            var logger = ModEntry.Logger;

            switch (entry.Severity)
            {
                case Severity.Debug:
                    if (entry.Blueprint is null)
                        logger.Log($"[DEBUG] {entry.Message()}");
                    else
                        logger.Log($"[DEBUG][BLUEPRINT {entry.Blueprint.BlueprintGuid}] {entry.Message()}");
                    break;
                case Severity.Info:
                    logger.Log(entry.Message());
                    break;
                case Severity.Warning:
                    logger.Warning(entry.Message());
                    break;
                case Severity.Error:
                    logger.Error(entry.Message());
                    break;
                case Severity.Critical:
                    logger.Critical(entry.Message());
                    break;
            }

            if (entry.Exception is not null)
                logger.LogException(entry.Exception);
        }
        
        private static OwlcatModification? owlcatModification;
        public static OwlcatModification? OwlcatModification
        {
            get => owlcatModification;
            set
            {
                var oldMod = owlcatModification;
                owlcatModification = value;

                if (oldMod is not null)
                    ReplayLogOwlcat();
            }
        }

        private static void OwlLog(Entry entry)
        {
            if (entry.Severity < logLevel) return;

            if (OwlcatModification is null) return;

            var logger = OwlcatModification.Logger;

            switch (entry.Severity)
            {
                case Severity.Debug:
                    if (entry.Blueprint is null)
                        logger.Log($"[DEBUG] {entry.Message()}");
                    else
                        logger.Log($"[DEBUG][BLUEPRINT {entry.Blueprint.BlueprintGuid}] {entry.Message()}");
                    break;
                case Severity.Info:
                    logger.Log(entry.Message());
                    break;
                case Severity.Warning:
                    logger.Warning(entry.Message());
                    break;
                case Severity.Error:
                case Severity.Critical:
                    logger.Error(entry.Message());
                    break;
            }

            if (entry.Exception is not null)
                logger.Exception(entry.Exception);
        }

        public static void AddEntry(Entry entry)
        {
            EntryList.Add(entry);

            UmmLog(entry);
            OwlLog(entry);
        }

        public static void ReplayLogUmm()
        {
            if (ModEntry is null)
            {
                AddEntry(new Entry(() => $"Attempted to replay log, but {nameof(ModEntry)} is null", Severity.Warning));
                return;
            }

            if (Entries.Count() == 0) return;

            UmmLog(new(() => "REPLAY LOG BEGIN"));

            foreach (var entry in Entries) UmmLog(entry);

            UmmLog(new(() => "REPLAY LOG END"));
        }

        public static void ReplayLogOwlcat()
        {
            if (OwlcatModification is null)
            {
                AddEntry(new Entry(() => $"Attempted to replay log, but {nameof(OwlcatModification)} is null", Severity.Warning));
                return;
            }

            if (Entries.Count() == 0) return;

            UmmLog(new(() => "REPLAY LOG BEGIN"));

            foreach (var entry in Entries) OwlLog(entry);

            UmmLog(new(() => "REPLAY LOG END"));
        }

        public static void Clear()
        {
            EntryList.Clear();

            AddEntry(new(() => "Log cleared"));
        }

        public static void Debug(Func<string> message, IMicroBlueprint<SimpleBlueprint>? blueprint, Exception? exception = null) =>
            AddEntry(new(message, Severity.Debug, exception) { Blueprint = blueprint });
        public static void Debug(Func<string> message, Exception? exception = null) => AddEntry(new(message, Severity.Debug, exception));
        public static void Log(string message, Exception? exception = null) => AddEntry(new(() => message, Severity.Info, exception));
        public static void Warning(string messasge, Exception? exception = null) => AddEntry(new(() => messasge, Severity.Warning, exception));
        public static void Error(string message, Exception? exception = null) => AddEntry(new(() => message, Severity.Error, exception));
        public static void Critical(string message, Exception? exception = null) => AddEntry(new(() => message, Severity.Critical, exception));
    }
}