using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        internal readonly record struct Entry(Func<string> Message, Severity Severity = Severity.Info, Exception? Exception = null);

        private static readonly List<Entry> EntryList = new();
        public static IEnumerable<Entry> Entries = EntryList.ToArray();

        private static Severity UmmLogLevel =
#if DEBUG
            Severity.Debug;
#else
                Severity.Info;
#endif
        public static void SetUmmLogLevel(Severity severity)
        {
            if (UmmLogLevel == severity) return;

            AddEntry(new(() => $"Setting UMM log severity to {severity}"));
            UmmLogLevel = severity;
        }

        private static UnityModManager.ModEntry? modEntry;
        public static UnityModManager.ModEntry? ModEntry
        {
            get => modEntry;
            set
            {
                var oldModEntry = modEntry;
                modEntry = value;

                if (oldModEntry == null) ReplayLog();
            }
        }

        private static void UmmLog(Entry entry)
        {
            if (entry.Severity < UmmLogLevel) return;

            if (ModEntry is null) return;

            var logger = ModEntry.Logger;

            switch (entry.Severity)
            {
                case Severity.Debug:
                    logger.Log($"[DEBUG] {entry.Message()}");
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

        public static void AddEntry(Entry entry)
        {
            EntryList.Add(entry);

            UmmLog(entry);
        }

        public static void ReplayLog()
        {
            if (ModEntry is null)
            {
                AddEntry(new Entry(() => "Attempted to replay log, but ModEntry is null", Severity.Warning));
                return;
            }

            if (Entries.Count() == 0) return;

            UmmLog(new(() => "REPLAY LOG BEGIN"));

            foreach (var entry in Entries) UmmLog(entry);

            UmmLog(new(() => "REPLAY LOG END"));
        }

        public static void Clear()
        {
            EntryList.Clear();

            AddEntry(new(() => "Log cleared"));
        }

        public static void Debug(Func<string> message, Exception? exception = null) => AddEntry(new(message, Severity.Debug, exception));
        public static void Log(string message, Exception? exception = null) => AddEntry(new(() => message, Severity.Info, exception));
        public static void Warning(string messasge, Exception? exception = null) => AddEntry(new(() => messasge, Severity.Warning, exception));
        public static void Error(string message, Exception? exception = null) => AddEntry(new(() => message, Severity.Error, exception));
        public static void Critical(string message, Exception? exception = null) => AddEntry(new(() => message, Severity.Critical, exception));
    }
}