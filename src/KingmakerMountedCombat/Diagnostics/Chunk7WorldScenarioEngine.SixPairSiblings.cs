using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Chunk7WorldScenarioEngine
    {
        // Read-only proof of the rider sibling slot contract. Batch187 restored six
        // riders of one native parent first-in-first-out; every native parent of the
        // six rider views must list exactly the same children, in the same order,
        // after each settled lifecycle as before the first mount.
        private readonly Dictionary<Transform, int[]> sixSiblingOrders = new Dictionary<Transform, int[]>();
        private int sixSiblingOrderCycles;

        private void CaptureSixSiblingOrder()
        {
            Require(sixSiblingOrders.Count == 0, "Six-pair sibling order can be captured only once.");
            for (var index = 0; index < 6; index++)
            {
                var parent = SixRiderParent(index);
                if (!sixSiblingOrders.ContainsKey(parent)) sixSiblingOrders.Add(parent, SiblingIds(parent));
            }
            sixObservation["riderParents"] = sixSiblingOrders.Count;
            sixObservation["riderParentChildren"] = sixSiblingOrders.Values.Sum(ids => ids.Length);
            sixObservation["siblingOrderVerifiedCycles"] = 0;
        }

        private void RequireSixSiblingOrder()
        {
            foreach (var entry in sixSiblingOrders)
            {
                Require(entry.Key != null && SiblingIds(entry.Key).SequenceEqual(entry.Value),
                    "Six-pair settled cycle changed a native rider parent's exact child order.");
            }
            for (var index = 0; index < 6; index++)
            {
                Require(sixSiblingOrders.ContainsKey(SixRiderParent(index)), "Six-pair rider did not return to its captured native parent.");
            }
            sixObservation["siblingOrderVerifiedCycles"] = ++sixSiblingOrderCycles;
        }

        private Transform SixRiderParent(int index)
        {
            var view = SixRider(index).View;
            Require(view != null && view.transform.parent != null, "Six-pair rider view has no native parent.");
            return view.transform.parent;
        }

        private static int[] SiblingIds(Transform parent)
        {
            var ids = new int[parent.childCount];
            for (var index = 0; index < ids.Length; index++) ids[index] = parent.GetChild(index).GetInstanceID();
            return ids;
        }
    }
}
