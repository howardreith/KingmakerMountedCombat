using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerMountedCombat.Logging;
using Newtonsoft.Json.Linq;
using UberLogger;

namespace KingmakerMountedCombat.Diagnostics
{
    // Passive capture of the engine's own UberLogger error channel while a runtime automation
    // request is active. The shipped game forwards nothing from that channel to the Unity player
    // log, so a native deserialization or post-load failure (for example a dropped collection
    // element) otherwise leaves no record. This sink only reads: it never filters, rethrows or
    // alters engine logging, and it keeps a bounded tail.
    internal sealed class NativeEngineLogTail : ILogger, IDisposable
    {
        private const int Capacity = 64, MessageLimit = 2000, FrameLimit = 12;
        private readonly object sync = new object();
        private readonly LinkedList<JObject> entries = new LinkedList<JObject>();
        private readonly IModLogger logger;
        private int dropped, observed;
        private bool registered;

        internal NativeEngineLogTail(IModLogger logger) { this.logger = logger; }

        internal void Register()
        {
            if (registered) return;
            Logger.AddLogger(this, false);
            registered = true;
        }

        public void Log(LogInfo logInfo)
        {
            if (logInfo == null || logInfo.Severity == LogSeverity.Message) return;
            var frames = new JArray();
            if (logInfo.Callstack != null)
                foreach (var frame in logInfo.Callstack.Take(FrameLimit))
                    frames.Add(Bound(frame == null ? null : frame.GetFormattedMethodName()));
            var message = Bound(logInfo.Message);
            var row = new JObject { ["severity"] = logInfo.Severity.ToString(), ["channel"] = logInfo.Channel,
                ["timeStamp"] = logInfo.TimeStamp, ["message"] = message, ["callstack"] = frames };
            lock (sync)
            {
                observed++;
                entries.AddLast(row);
                while (entries.Count > Capacity) { entries.RemoveFirst(); dropped++; }
            }
            if (logInfo.Severity == LogSeverity.Error) logger?.Warning("Native engine log (" + logInfo.Severity + "): " + message);
        }

        public void Destroy() { }

        internal JObject Capture()
        {
            lock (sync)
                return new JObject { ["contract"] = "native-engine-log-tail-v1", ["capacity"] = Capacity, ["observed"] = observed,
                    ["dropped"] = dropped, ["entries"] = new JArray(entries.ToArray()) };
        }

        public void Dispose()
        {
            if (!registered) return;
            try { Logger.RemoveLogger(this); } catch (Exception) { }
            registered = false;
        }

        private static string Bound(string value) => value == null ? null : value.Length <= MessageLimit ? value : value.Substring(0, MessageLimit);
    }
}
