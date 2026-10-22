using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerMountedCombat.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // One selected six-rider experiment. No native references are retained and
    // no hierarchy operation is performed. Lines survive a failed host drain.
    internal sealed class SixPairAttachmentObservation : IDisposable
    {
        private static SixPairAttachmentObservation current;
        private readonly IModLogger logger;
        private readonly Dictionary<string, int> cycles;
        private readonly HashSet<string> restoreStarted = new HashSet<string>();
        private readonly HashSet<string> restoreFinished = new HashSet<string>();
        private int count;
        private bool stopped;
        internal static bool IsActive => current != null && !current.stopped;

        internal SixPairAttachmentObservation(IEnumerable<string> riders, IModLogger logger)
        {
            var ids = riders.ToArray();
            if (current != null || ids.Length != 6 || ids.Any(string.IsNullOrEmpty) || ids.Distinct().Count() != 6)
                throw new InvalidOperationException("Attachment observation requires one exact six-rider scope.");
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            cycles = ids.ToDictionary(id => id, id => 0);
            current = this;
        }

        internal static void Record(string phase, string rider, Transform node,
            Transform capturedParent, int capturedIndex, Transform anchor)
        {
            var scope = current;
            if (scope == null || scope.stopped || rider == null || !scope.cycles.ContainsKey(rider)) return;
            // Observation must never replace an attach/restore return or exception.
            try { scope.Write(phase, rider, node, capturedParent, capturedIndex, anchor); }
            catch { scope.stopped = true; }
        }

        private void Write(string phase, string rider, Transform node,
            Transform capturedParent, int capturedIndex, Transform anchor)
        {
            if (phase == "acquire-before") cycles[rider]++;
            var cycle = cycles[rider];
            if (cycle < 1 || cycle > 3 || count >= 96) { stopped = true; return; }
            var key = rider + ":" + cycle;
            if (phase == "restore-before" && !restoreStarted.Add(key)) return;
            if ((phase == "restore-after" || phase == "restore-failed") && !restoreFinished.Add(key)) return;
            if (phase == "acquire-before")
            {
                capturedParent = node.parent;
                capturedIndex = node.GetSiblingIndex();
            }
            var row = new JObject {
                ["ordinal"] = ++count, ["phase"] = phase, ["rider"] = rider, ["cycle"] = cycle,
                ["frame"] = Time.frameCount, ["nodeId"] = Id(node), ["anchorId"] = Id(anchor),
                ["capturedParentId"] = Id(capturedParent), ["capturedParentDestroyed"] = Destroyed(capturedParent),
                ["capturedIndex"] = capturedIndex, ["capturedParentChildren"] = capturedParent == null ? -1 : capturedParent.childCount,
                ["currentParentId"] = Id(node.parent), ["currentIndex"] = node.GetSiblingIndex(),
                ["currentParentChildren"] = node.parent == null ? -1 : node.parent.childCount,
                ["sceneHandle"] = node.gameObject.scene.handle, ["sceneRootCount"] = node.gameObject.scene.rootCount
            };
            logger.Info("KMC_SIX_ATTACHMENT " + row.ToString(Formatting.None));
            if (phase == "restore-failed" || phase == "acquire-failed") stopped = true;
        }

        private static int Id(Transform value) => value == null ? 0 : value.GetInstanceID();
        private static bool Destroyed(Transform value) => !ReferenceEquals(value, null) && value == null;

        public void Dispose()
        {
            if (ReferenceEquals(current, this)) current = null;
            stopped = true; cycles.Clear(); restoreStarted.Clear(); restoreFinished.Clear();
        }
    }
}
