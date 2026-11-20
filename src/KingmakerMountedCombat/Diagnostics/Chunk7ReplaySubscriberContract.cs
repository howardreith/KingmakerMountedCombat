using System;
using System.Collections.Generic;
using System.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    // One row of an owned EventBus inventory: the interface it is listed under, the subscriber's type and the
    // collision-free label of the subscriber instance (its weak-reference serial).
    internal struct Chunk7ReplaySubscriberRow
    {
        internal readonly string Interface, Type;
        internal readonly long Subject;

        internal Chunk7ReplaySubscriberRow(string @interface, string type, long subject)
        {
            Interface = @interface; Type = type; Subject = subject;
        }
    }

    // The KMC-owned global EventBus subscribers a settled repeated world holds, as one reviewed revision.
    //
    // Repeated-world evidence names this revision. The external reader (scripts/runtime/Chunk7RepeatedWorldEvidence.ps1)
    // keeps its own reviewed copy of every revision it accepts, so neither side derives the expected set from an
    // inventory. A change to a subscriber's EventBus interfaces, or a new process-lifetime subscriber, changes this
    // set: it needs a new revision here and in the reader. scripts/chunk7/Test-Chunk7ReplaySubscriberContract.ps1
    // compares both declarations with the compiled types as the installed EventBus registers them.
    //
    // Diagnostic evidence protocol only: nothing here is read by gameplay code or written to a save.
    //
    // The first repeated-world evidence (chunk7b previews up to .99) listed 13 lifecycle interfaces and the two
    // control interfaces. It never carried a revision name; the reader knows that set as r1. Later builds added the
    // defence and tack services and two lifecycle interfaces without a revision, so no evidence describes them.
    internal static class Chunk7ReplaySubscriberContract
    {
        internal const string Revision = "kmc-global-subscribers-r2";
        internal const string HorseFactType = Integration + "HorseCompanionAddPet";
        internal const string HorseFactInterface = Events + "ISceneHandler";
        private const string Events = "Kingmaker.PubSubSystem.";
        private const string Integration = "KingmakerMountedCombat.Integration.";

        // Constructed once by CompositionRoot and subscribed for the life of the process: one instance each.
        internal static readonly IReadOnlyList<KeyValuePair<string, string[]>> ProcessOwned = new[]
        {
            Owned("MountedLifecycleSubscriber", "IAreaLoadingStagesHandler", "IGameModeHandler", "IInGameHandler",
                "IPartyCombatHandler", "IPartyHandler", "IPartyLeaveAreaHandler", "ISceneHandler", "ITeleportHandler",
                "ITurnBasedModeEnabledHandler", "IUnitCombatHandler", "IUnitFallProneHandler", "IUnitFinallyDeadHandler",
                "IUnitHandler", "IUnitLifeStateChanged", "IUnitViewAttachedUIHandler"),
            Owned("NativeMountedControlService", "IAbilityTargetSelectionUIHandler", "IClickActionHandler"),
            Owned("MountedCombatDefenseService", "IUnitCombatHandler", "IUnitNewCombatRoundHandler"),
            Owned("RidingDogTackService", "ISceneHandler", "IUnitViewAttachedUIHandler")
        };

        internal static int ProcessOwnedEntries => ProcessOwned.Sum(owner => owner.Value.Length);

        private static KeyValuePair<string, string[]> Owned(string type, params string[] interfaces) =>
            new KeyValuePair<string, string[]>(Integration + type, interfaces.Select(name => Events + name).ToArray());

        // Null when the rows are exactly this revision beside the given number of Horse facts; otherwise the first
        // difference. One native HorseCompanionAddPet ISceneHandler subscriber exists per KMC Horse owner.
        internal static string Violation(IReadOnlyList<Chunk7ReplaySubscriberRow> rows, int horseFacts)
        {
            if (rows == null) throw new ArgumentNullException(nameof(rows));
            if (horseFacts < 1 || horseFacts > 2) return "expected Horse owners " + horseFacts + " is outside the one or two a repeated world holds";
            var processSubjects = new HashSet<long>();
            foreach (var owner in ProcessOwned)
            {
                var mine = rows.Where(row => row.Type == owner.Key).ToArray();
                var listed = mine.Select(row => row.Interface).OrderBy(name => name, StringComparer.Ordinal).ToArray();
                var expected = owner.Value.OrderBy(name => name, StringComparer.Ordinal).ToArray();
                if (!listed.SequenceEqual(expected, StringComparer.Ordinal))
                    return owner.Key + " lists " + listed.Length + " of " + expected.Length + " interfaces: missing [" +
                        string.Join(",", expected.Except(listed, StringComparer.Ordinal)) + "], unexpected [" +
                        string.Join(",", listed.Except(expected, StringComparer.Ordinal)) + "]";
                var instances = mine.Select(row => row.Subject).Distinct().ToArray();
                if (instances.Length != 1) return owner.Key + " is held by " + instances.Length + " instances instead of one";
                if (!processSubjects.Add(instances[0])) return owner.Key + " shares its instance with another process-owned subscriber";
            }
            var horses = rows.Where(row => row.Type == HorseFactType).ToArray();
            if (horses.Length != horseFacts) return "Horse fact subscribers " + horses.Length + " differ from Horse owners " + horseFacts;
            if (horses.Any(row => row.Interface != HorseFactInterface)) return "a Horse fact subscriber is listed under an interface other than " + HorseFactInterface;
            if (horses.Select(row => row.Subject).Distinct().Count() != horseFacts) return "two Horse fact rows name one subscriber instance";
            if (horses.Any(row => processSubjects.Contains(row.Subject))) return "a Horse fact subscriber shares a process-owned instance";
            var foreign = rows.Where(row => row.Type != HorseFactType && ProcessOwned.All(owner => owner.Key != row.Type))
                .Select(row => row.Type).Distinct().OrderBy(name => name, StringComparer.Ordinal).ToArray();
            return foreign.Length == 0 ? null : "subscriber types outside the reviewed revision: " + string.Join(",", foreign);
        }
    }
}
