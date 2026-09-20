using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Kingmaker.PubSubSystem;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    // Reads the exact installed subscription lists at a settled fixture boundary.
    // Never calls Cleanup, Subscribe, Unsubscribe or mutates a native collection.
    internal static class Chunk4SubscriptionSnapshot
    {
        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        internal static JObject Capture()
        {
            var global = Exact(typeof(EventBus), "GlobalSubscribers", 0x04004C2F).GetValue(null);
            var pooled = Exact(global.GetType(), "m_Listeners", 0x04004C39).GetValue(global);
            var listeners = Exact(pooled.GetType(), "m_Dictionary", 0x04004C34).GetValue(pooled) as IDictionary;
            if (listeners == null) throw new InvalidOperationException("Native listener table no longer implements its inspected dictionary contract.");
            var rows = new JArray();
            var seen = new HashSet<object>();
            var executing = false;
            foreach (DictionaryEntry entry in listeners)
            {
                var list = entry.Value;
                executing |= (bool)Exact(list.GetType(), "Executing", 0x04004C3B).GetValue(list);
                var subscribers = Exact(list.GetType(), "List", 0x04004C3C).GetValue(list) as IEnumerable;
                if (subscribers == null) throw new InvalidOperationException("Native subscriber list is not enumerable.");
                foreach (var subscriber in subscribers)
                {
                    if (subscriber == null || subscriber.GetType().Assembly != typeof(Chunk4SubscriptionSnapshot).Assembly) continue;
                    seen.Add(subscriber);
                    rows.Add(new JObject { ["interface"] = ((Type)entry.Key).FullName,
                        ["type"] = subscriber.GetType().FullName, ["identity"] = RuntimeHelpers.GetHashCode(subscriber) });
                }
            }
            return new JObject { ["assemblyMvid"] = typeof(EventBus).Assembly.ManifestModule.ModuleVersionId.ToString(),
                ["executing"] = executing, ["uniqueOwnedSubscribers"] = seen.Count, ["ownedInterfaceEntries"] = rows.Count,
                ["entries"] = rows };
        }

        private static FieldInfo Exact(Type type, string name, int token)
        {
            var field = type.GetField(name, Flags);
            if (field == null || field.MetadataToken != token) throw new MissingFieldException(type.FullName, name);
            return field;
        }
    }
}
