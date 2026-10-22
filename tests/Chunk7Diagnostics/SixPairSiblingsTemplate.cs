// Compiles the unchanged six-pair sibling-order partial against a minimal native
// hierarchy facade. It certifies the read-only capture/compare contract only.
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace UnityEngine
{
    class Transform
    {
        private static int next;
        private readonly int id = ++next;
        internal Transform parentNode;
        internal readonly List<Transform> children = new List<Transform>();
        public Transform parent => parentNode;
        public int childCount => children.Count;
        public Transform GetChild(int index) => children[index];
        public int GetInstanceID() => id;
        internal void SetParent(Transform value)
        {
            parentNode?.children.Remove(this);
            parentNode = value;
            value?.children.Add(this);
        }
    }
}

namespace KingmakerMountedCombat.Diagnostics
{
    using UnityEngine;
    class View { public Transform transform = new Transform(); }
    class Actor { public View View = new View(); }

    internal sealed partial class Chunk7WorldScenarioEngine
    {
        internal readonly Actor[] riders = Enumerable.Range(0, 6).Select(index => new Actor()).ToArray();
        internal readonly JObject sixObservation = new JObject();
        private Actor SixRider(int index) => riders[index];
        private static void Require(bool condition, string error) { if (!condition) throw new InvalidOperationException(error); }
        internal void Capture() => CaptureSixSiblingOrder();
        internal void RequireOrder() => RequireSixSiblingOrder();
    }
}

internal static class Program
{
    private static int pass, fail;

    private static void Check(string name, Func<bool> check)
    {
        try { if (!check()) throw new Exception("predicate false"); pass++; Console.WriteLine("PASS " + name); }
        catch (Exception exception) { fail++; Console.WriteLine("FAIL " + name + ": " + exception.Message); }
    }

    private static bool Refuses(Action action, string fragment)
    {
        try { action(); return false; }
        catch (InvalidOperationException exception) { return exception.Message.Contains(fragment); }
    }

    // Batch187's original order: ten others with riders at captured {0,7,5,4,8,9}.
    private static KingmakerMountedCombat.Diagnostics.Chunk7WorldScenarioEngine Engine(out UnityEngine.Transform parent)
    {
        var engine = new KingmakerMountedCombat.Diagnostics.Chunk7WorldScenarioEngine();
        parent = new UnityEngine.Transform();
        for (var index = 0; index < 10; index++) new UnityEngine.Transform().SetParent(parent);
        var ordinals = new[] { 0, 7, 5, 4, 8, 9 };
        for (var index = 5; index >= 0; index--)
        {
            var rider = engine.riders[index].View.transform;
            rider.SetParent(parent);
            parent.children.Remove(rider);
            parent.children.Insert(ordinals[index], rider);
        }
        return engine;
    }

    private static int Main()
    {
        UnityEngine.Transform parent;
        Check("actual partial captures one shared native parent before the first mount", () =>
        {
            var engine = Engine(out parent); engine.Capture();
            return (int)engine.sixObservation["riderParents"] == 1 && (int)engine.sixObservation["riderParentChildren"] == 16 &&
                (int)engine.sixObservation["siblingOrderVerifiedCycles"] == 0;
        });
        Check("actual partial requires and counts each exactly restored settled cycle", () =>
        {
            var engine = Engine(out parent); engine.Capture();
            engine.RequireOrder(); engine.RequireOrder(); engine.RequireOrder();
            return (int)engine.sixObservation["siblingOrderVerifiedCycles"] == 3;
        });
        Check("actual partial refuses a permuted shared parent", () =>
        {
            var engine = Engine(out parent); engine.Capture();
            var rider = engine.riders[1].View.transform; parent.children.Remove(rider); parent.children.Insert(11, rider);
            return Refuses(engine.RequireOrder, "exact child order") && (int)engine.sixObservation["siblingOrderVerifiedCycles"] == 0;
        });
        Check("actual partial refuses an appended unrelated child", () =>
        {
            var engine = Engine(out parent); engine.Capture(); new UnityEngine.Transform().SetParent(parent);
            return Refuses(engine.RequireOrder, "exact child order");
        });
        Check("actual partial refuses a rider left under another parent", () =>
        {
            var engine = Engine(out parent); engine.Capture();
            var anchor = new UnityEngine.Transform(); engine.riders[2].View.transform.SetParent(anchor);
            return Refuses(engine.RequireOrder, "exact child order") || Refuses(engine.RequireOrder, "captured native parent");
        });
        Check("actual partial captures each distinct rider parent once", () =>
        {
            var engine = Engine(out parent); var other = new UnityEngine.Transform();
            engine.riders[5].View.transform.SetParent(other); new UnityEngine.Transform().SetParent(other);
            engine.Capture();
            return (int)engine.sixObservation["riderParents"] == 2 && (int)engine.sixObservation["riderParentChildren"] == 17;
        });
        Check("actual partial refuses a parentless rider view", () =>
        {
            var engine = Engine(out parent); engine.riders[0].View.transform.SetParent(null);
            return Refuses(engine.Capture, "no native parent");
        });
        Check("actual partial captures only once", () =>
        {
            var engine = Engine(out parent); engine.Capture();
            return Refuses(engine.Capture, "captured only once");
        });
        Console.WriteLine("SIX-PAIR SIBLINGS PASS=" + pass + " FAIL=" + fail);
        return fail == 0 ? 0 : 1;
    }
}
