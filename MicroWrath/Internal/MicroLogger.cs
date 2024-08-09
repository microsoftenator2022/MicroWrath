using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Blueprints;
using Kingmaker.Modding;
using Kingmaker.Utility;

using Owlcat.Runtime.Core.Logging;

using UnityModManagerNet;

namespace MicroWrath
{
    /// <summary>
    /// Log wrapper for UMM or Owlcat logger.
    /// </summary>
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

        /// <exclude />
        private static readonly List<Entry> EntryList = new();

        public static IEnumerable<Entry> Entries = EntryList.ToArray();

        /// <exclude />
        private static Severity logLevel =
#if DEBUG
            Severity.Debug;
#else
            default;
#endif
        /// <summary>
        /// Sets the minimum log severity. Events with lower severity will not be printed to the log.
        /// </summary>
        public static void SetLogLevel(Severity severity)
        {
            if (logLevel == severity) return;

            AddEntry(new(() => $"Setting UMM log severity to {severity}"));
            logLevel = severity;
        }

        /// <summary>
        /// Gets or sets the log severity level
        /// </summary>
        public static Severity LogLevel
        {
            get => logLevel;
            set => SetLogLevel(value);
        }

        /// <exclude />
        private static UnityModManager.ModEntry? modEntry;

        /// <summary>
        /// For UMM mods. Returns <see cref="UnityModManager.ModEntry"/> or null if this is not a UMM mod.
        /// </summary>
        public static UnityModManager.ModEntry? ModEntry
        {
            get => modEntry;
            set
            {
                var oldModEntry = modEntry;
                modEntry = value;

                if (modEntry is null)
                    return;

                if (oldModEntry is null || modEntry != oldModEntry) ReplayLogUmm();
            }
        }

        /// <exclude />
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

        /// <exclude />
        private static OwlcatModification? owlcatModification;

        /// <summary>
        /// For OwlMods. Returns <see cref="OwlcatModification"/> or null if this is not an OwlMod.
        /// </summary>
        public static OwlcatModification? OwlcatModification
        {
            get => owlcatModification;
            set
            {
                var oldMod = owlcatModification;
                owlcatModification = value;

                if (owlcatModification is null)
                    return;

                if (oldMod is null || owlcatModification != oldMod)
                    ReplayLogOwlcat();
            }
        }

        /// <exclude />
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

        /// <summary>
        /// Adds an entry to the log.
        /// </summary>
        public static void AddEntry(Entry entry)
        {
            EntryList.Add(entry);

            UmmLog(entry);
            OwlLog(entry);
        }

        /// <summary>
        /// Replays the log. Used to log message from before the logger is initialized.
        /// </summary>
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

        /// <inheritdoc cref="ReplayLogUmm"/>
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

        /// <summary>
        /// Clears log entries.
        /// </summary>
        public static void Clear()
        {
            EntryList.Clear();

            AddEntry(new(() => "Log cleared"));
        }

        /// <summary>
        /// Create a new log entry of <see cref="Severity.Debug"/> severity with an associated blueprint and optional exception.
        /// Takes a <see cref="Func{T}"/> for message generation to limit performance impact when Logger severity is greater than <see cref="Severity.Debug"/>
        /// </summary>
        /// <param name="message">Log message.</param>
        /// <param name="blueprint">Associated blueprint.</param>
        /// <param name="exception">Associated exception.</param>
        public static void Debug(Func<string> message, IMicroBlueprint<SimpleBlueprint>? blueprint, Exception? exception = null) =>
            AddEntry(new(message, Severity.Debug, exception) { Blueprint = blueprint });

        /// <summary>
        /// Create a new log entry of <see cref="Severity.Debug"/> severity with an optional associated exception.
        /// Takes a <see cref="Func{T}"/> for message generation to limit performance impact when Logger severity is greater than <see cref="Severity.Debug"/>
        /// </summary>
        /// <param name="message">Log message.</param>
        /// <param name="exception">Associated exception.</param>
        public static void Debug(Func<string> message, Exception? exception = null) => AddEntry(new(message, Severity.Debug, exception));

        /// <summary>
        /// Create a new log entry of <see cref="Severity.Debug"/> severity using a pooled <see cref="StringBuilder"/> with an optional associated exception.
        /// </summary>
        /// <param name="messageBuilder">Message builder function.</param>
        /// <param name="exception">Associated exception.</param>
        public static void Debug(Action<StringBuilder> messageBuilder, Exception? exception = null)
        {
            AddEntry(new(() =>
            {
                using var psb = PooledStringBuilder.Request();
                psb.Reset();

                var sb = psb.Builder;
                messageBuilder(sb);

                var s = sb.ToString();

                psb.Reset();

                return s;
            }, Severity.Debug, exception));
        }

        /// <summary>
        /// Create a new log entry of <see cref="Severity.Info"/> severity with an optional associated exception.
        /// </summary>
        /// <param name="message">Log message.</param>
        /// <param name="exception">Associated exception.</param>
        public static void Log(string message, Exception? exception = null) => AddEntry(new(() => message, Severity.Info, exception));

        /// <summary>
        /// Create a new log entry of <see cref="Severity.Warning"/> severity with an optional associated exception.
        /// </summary>
        /// <param name="message">Log message.</param>
        /// <param name="exception">Associated exception.</param>
        public static void Warning(string message, Exception? exception = null) => AddEntry(new(() => message, Severity.Warning, exception));

        /// <summary>
        /// Create a new log entry of <see cref="Severity.Error"/> severity with an optional associated exception.
        /// </summary>
        /// <param name="message">Log message.</param>
        /// <param name="exception">Associated exception.</param>
        public static void Error(string message, Exception? exception = null) => AddEntry(new(() => message, Severity.Error, exception));

        /// <summary>
        /// Create a new log entry of <see cref="Severity.Critical"/> severity with an optional associated exception.
        /// </summary>
        /// <param name="message">Log message.</param>
        /// <param name="exception">Associated exception.</param>
        public static void Critical(string message, Exception? exception = null) => AddEntry(new(() => message, Severity.Critical, exception));
    }
}