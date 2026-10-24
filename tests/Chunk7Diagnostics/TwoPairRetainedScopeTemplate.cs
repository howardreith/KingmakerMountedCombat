using System;
using System.Collections.Generic;
using System.Linq;

// Actual-method check: the unchanged product Chunk7PersistenceScenario.RequireRetainedFailureScope
// is spliced in at the SCOPE marker and run against minimal stand-ins for its native collaborators.
namespace Kingmaker
{
    class BlueprintArea { public string AssetGuidThreadSafe; }
    class Game { internal static Game Instance = new Game(); internal BlueprintArea CurrentlyLoadedArea; }
}

namespace KingmakerMountedCombat.Diagnostics
{
    class Row { public string Status; }
    class Persistence { public int FailedAreaPairCount; }
    class Scope
    {
        public int CurrentCalls, DestinationCalls; public bool RefuseCurrent, RefuseDestination; public string DestinationArea; public object DestinationBlueprint;
        internal void RequireCurrent() { CurrentCalls++; if (RefuseCurrent) throw new InvalidOperationException("Captured native area state changed."); }
        internal void RequireDeclaredAreaDestination(string area, object blueprint)
        {
            DestinationCalls++; DestinationArea = area; DestinationBlueprint = blueprint;
            if (RefuseDestination) throw new InvalidOperationException("Declared native destination does not match.");
        }
    }

    internal sealed class Chunk7PersistenceScenario
    {
        internal bool SixPairCase, captured = true, cleanup = true, cleanupProven, pendingHostCustody = true, disposed, cleanupPendingReported = true;
        internal string RetainedFailureArtifactSha256 = "abc";
        internal List<string> errors = new List<string> { "C7 causal retained persistence cleanup failure" };
        internal List<Row> results = new List<Row> { new Row { Status = "PASS" }, new Row { Status = "FAIL" } };
        internal Scope nativeScope = new Scope();
        internal Persistence persistence = new Persistence();
        internal Kingmaker.BlueprintArea replayDestination;
        // Row 58 capital arrival: dispatched into its declared capital but not yet adopted.
        internal bool CapitalArrivalCase, capitalDispatched, capitalAdopted;
        internal Kingmaker.BlueprintArea capitalDestination;
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

        /* SCOPE */
    }

    internal static class Program
    {
        private static int pass, fail;
        private static void Check(string name, Func<bool> check)
        {
            bool ok; try { ok = check(); } catch (Exception error) { Console.WriteLine("ERROR " + name + ": " + error.Message); ok = false; }
            if (ok) { pass++; Console.WriteLine("PASS " + name); } else { fail++; Console.WriteLine("FAIL " + name); }
        }
        private static bool Refused(Chunk7PersistenceScenario engine, string message)
        {
            try { engine.RequireRetainedFailureScope(); return false; }
            catch (InvalidOperationException error) { return error.Message.StartsWith(message, StringComparison.Ordinal); }
        }
        private static Chunk7PersistenceScenario Refused(out Kingmaker.BlueprintArea destination)
        {
            destination = new Kingmaker.BlueprintArea { AssetGuidThreadSafe = "ead426a6c23d39548a670ee515d77df4" };
            var engine = new Chunk7PersistenceScenario { replayDestination = destination };
            engine.persistence.FailedAreaPairCount = 1; engine.nativeScope.RefuseCurrent = true;
            Kingmaker.Game.Instance.CurrentlyLoadedArea = destination;
            return engine;
        }

        private static Chunk7PersistenceScenario Capital(out Kingmaker.BlueprintArea destination)
        {
            destination = new Kingmaker.BlueprintArea { AssetGuidThreadSafe = "ead426a6c23d39548a670ee515d77df4" };
            var engine = new Chunk7PersistenceScenario { CapitalArrivalCase = true, capitalDispatched = true, capitalDestination = destination };
            engine.nativeScope.RefuseCurrent = true;
            Kingmaker.Game.Instance.CurrentlyLoadedArea = destination;
            return engine;
        }

        private static int Main()
        {
            Check("a refused area restore in its declared destination is admitted through the exact destination check", () =>
            {
                var engine = Refused(out var destination); engine.RequireRetainedFailureScope();
                return engine.nativeScope.DestinationCalls == 1 && engine.nativeScope.CurrentCalls == 0 &&
                    engine.nativeScope.DestinationArea == destination.AssetGuidThreadSafe && ReferenceEquals(engine.nativeScope.DestinationBlueprint, destination);
            });
            Check("a destination that fails the exact check is refused", () =>
            { var engine = Refused(out _); engine.nativeScope.RefuseDestination = true; return Refused(engine, "Declared native destination does not match."); });
            Check("without a refused restore the original world is required", () =>
            { var engine = Refused(out _); engine.persistence.FailedAreaPairCount = 0; return Refused(engine, "Captured native area state changed.") && engine.nativeScope.DestinationCalls == 0; });
            Check("another current area keeps the original world check", () =>
            {
                var engine = Refused(out _); Kingmaker.Game.Instance.CurrentlyLoadedArea = new Kingmaker.BlueprintArea { AssetGuidThreadSafe = "ead426a6c23d39548a670ee515d77df4" };
                return Refused(engine, "Captured native area state changed.") && engine.nativeScope.DestinationCalls == 0;
            });
            Check("no replay destination keeps the original world check", () =>
            { var engine = Refused(out _); engine.replayDestination = null; return Refused(engine, "Captured native area state changed."); });
            Check("an unchanged world passes the original check", () =>
            { var engine = Refused(out _); engine.persistence.FailedAreaPairCount = 0; engine.nativeScope.RefuseCurrent = false; engine.RequireRetainedFailureScope(); return engine.nativeScope.CurrentCalls == 1; });
            Check("a six-pair case is not a two-pair retained failure", () =>
            { var engine = Refused(out _); engine.SixPairCase = true; return Refused(engine, "No committed retained two-pair persistence failure owns this world."); });
            Check("an uncommitted retained artifact is refused", () =>
            { var engine = Refused(out _); engine.RetainedFailureArtifactSha256 = null; return Refused(engine, "No committed retained two-pair persistence failure owns this world."); });
            Check("a failure without a failed row is refused", () =>
            { var engine = Refused(out _); engine.results.RemoveAt(1); return Refused(engine, "No committed retained two-pair persistence failure owns this world."); });
            Check("proven cleanup is not a retained failure", () =>
            { var engine = Refused(out _); engine.cleanupProven = true; return Refused(engine, "No committed retained two-pair persistence failure owns this world."); });
            Check("an absent native scope is refused", () =>
            { var engine = Refused(out _); engine.nativeScope = null; return Refused(engine, "Captured native world authority is absent."); });
            Check("an unadopted capital arrival in its declared capital is admitted through the exact destination check", () =>
            {
                var engine = Capital(out var destination); engine.RequireRetainedFailureScope();
                return engine.nativeScope.DestinationCalls == 1 && engine.nativeScope.CurrentCalls == 0 &&
                    engine.nativeScope.DestinationArea == destination.AssetGuidThreadSafe && ReferenceEquals(engine.nativeScope.DestinationBlueprint, destination);
            });
            Check("an adopted capital arrival keeps the current world check", () =>
            { var engine = Capital(out _); engine.capitalAdopted = true; return Refused(engine, "Captured native area state changed.") && engine.nativeScope.DestinationCalls == 0; });
            Check("an undispatched capital arrival keeps the original world check", () =>
            { var engine = Capital(out _); engine.capitalDispatched = false; return Refused(engine, "Captured native area state changed.") && engine.nativeScope.DestinationCalls == 0; });
            Check("a capital arrival outside its declared capital keeps the original world check", () =>
            {
                var engine = Capital(out _); Kingmaker.Game.Instance.CurrentlyLoadedArea = new Kingmaker.BlueprintArea { AssetGuidThreadSafe = "ead426a6c23d39548a670ee515d77df4" };
                return Refused(engine, "Captured native area state changed.") && engine.nativeScope.DestinationCalls == 0;
            });
            Check("another case with a capital destination keeps the original world check", () =>
            { var engine = Capital(out _); engine.CapitalArrivalCase = false; return Refused(engine, "Captured native area state changed.") && engine.nativeScope.DestinationCalls == 0; });
            Console.WriteLine("TWO-PAIR RETAINED SCOPE PASS=" + pass + " FAIL=" + fail);
            return fail == 0 ? 0 : 1;
        }
    }
}
