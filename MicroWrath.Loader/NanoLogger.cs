﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Owlcat.Runtime.Core.Logging;

using UnityModManagerNet;

namespace MicroWrath.Loader
{
    using static MicroWrath.Loader.LoggerCommon;

    internal static class LoggerCommon
    {
        internal static string FormatMessage(string message) => $"[Loader] {message}";
    }

    internal interface INanoLogger
    {
        void Log(string message);
        void Warn(string message);
        void Error(string message);
        void Exception(Exception exception);
    }

    internal class UmmLogger : INanoLogger
    {
        private readonly UnityModManager.ModEntry.ModLogger logger;

        internal UmmLogger(UnityModManager.ModEntry modEntry)
        {
            logger = modEntry.Logger;
        }

        public void Error(string message) => logger.Error(FormatMessage(message));
        public void Exception(Exception exception) => logger.LogException(exception);
        public void Log(string message) => logger.Log(FormatMessage(message));
        public void Warn(string message) => logger.Warning(FormatMessage(message));
    }

    internal class OwlLogger : INanoLogger
    {
        private readonly LogChannel logger;

        internal OwlLogger(LogChannel logger)
        {
            this.logger = logger;
        }

        public void Error(string message) => logger.Error(FormatMessage(message));
        public void Exception(Exception exception) => logger.Exception(exception);
        public void Log(string message) => logger.Log(FormatMessage(message));
        public void Warn(string message) => logger.Warning(FormatMessage(message));
    }
}
