using System;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic.Commands;
using KingmakerMountedCombat.Domain;
using Newtonsoft.Json.Linq;
namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Chunk7SmallCalibrationScenario
    {
        private bool TryRelease(string name, Action operation)
        {
            try { operation(); return true; }
            catch (Exception error) { Fail("C7-small-cleanup", name + ": " + error); return false; }
        }
        private bool EndEncounter()
        {
            AssertAuthority();
            if (Busy() || !CommandsSettled() || world.UnifiedTurn.HasUnsettledPreparation || controls.HasUnsettledRelationshipTransition) return false;
            var targetGone = targetService == null;
            if (targetService != null) TryRelease("owned target", () => targetGone = targetService.DestroyAndVerify());
            foreach (var value in ControlledActors.Where(value => value.IsInState && value.IsInCombat))
                TryRelease("native combat participant", () => value.LeaveCombat());
            TryRelease("native entity drain", () => Game.Instance.EntityDestroyer.Tick());
            if (!targetGone || Game.Instance.Player.IsInCombat || Game.Instance.TurnBasedCombatController.Initialized) return false;
            var released = TryRelease("target subscriptions", () => { targetService?.Dispose(); targetService = null; });
            released &= TryRelease("pair AI lease", () => { if (pairAi != null && !pairAi.LastRestoreVerified)
                { pairAi.ReassertAfterNativeReset(new[] { actor, pet }); pairAi.Restore(new[] { actor, pet }); } });
            return released && (pairAi == null || pairAi.LastRestoreVerified);
        }
        private void PollCleanup()
        {
            try
            {
                if (completed) return;
                if (Busy() || persistence.DrainForTeardown(0) == OwnedWorkerTeardownVerdict.Refused)
                { Retain("A native load/worker/serialization owner retains calibration custody."); return; }
                if (!ReleaseManualLoadObservation()) { Retain("Owned cold observer has not unpatched."); return; }
                if (!captured)
                {
                    evidence["cleanup"] = new JObject { ["custodySettled"] = true, ["neverAcquiredNativeUi"] = true };
                    settled = true;
                    RuntimeLoaderInventory.WriteArtifact(request, EvidenceFileName, EvidenceKind, "FAIL", evidence, errors); completed = true; return;
                }
                // Exact original world, actors, archive and authority precede every
                // UI, pause, command, target, selection and native settings write.
                AssertAuthority();
                if (field != null && !field.TryDrain()) { Retain("Owned native party selector has not closed."); return; }
                Game.Instance.IsPaused = false;
                var commands = true;
                foreach (var command in issued.Where(value => value != null && !value.IsFinished))
                    commands &= TryRelease("owned native command", () => command.Interrupt());
                foreach (var value in world.Pairs)
                    commands &= TryRelease("pair command", () => value.Combat.Cancel("Small calibration cleanup"));
                if (!commands || !CommandsSettled() || world.UnifiedTurn.HasUnsettledPreparation || controls.HasUnsettledRelationshipTransition)
                { Retain("Native shell/process/preparation retains cleanup before actor/target mutation."); return; }
                if (!EndEncounter()) { Retain("Owned native target/combat/AI cleanup remains pending."); return; }
                foreach (var value in world.Pairs)
                    TryRelease("pair relationship", () => Require(value.Relationship.Dismount(CleanupTrigger.Exception).Succeeded, "Exact pair retained cleanup obligations."));
                if (world.Registry.Owners.Length != 0 || world.Pairs.Any(value => !value.Relationship.Runtime.NoPreparedPairResidue || value.Relationship.PairOwner != null) ||
                    !CommandsSettled() || world.UnifiedTurn.HasUnsettledPreparation)
                { Retain("Pair ownership/view/preparation residue remains pending."); return; }
                if (trace != null)
                {
                    if (evidence["allocationTrace"] == null) evidence["allocationTrace"] = trace.Capture();
                    if (!TryRelease("allocation observer", () => { trace.Dispose(); trace = null; })) { Retain("Owned allocation observation has not unpatched."); return; }
                }
                if (privateSmallCalibrationLease != null && !TryRelease("private Small calibration admission", () =>
                    { privateSmallCalibrationLease.Dispose(); privateSmallCalibrationLease = null; }))
                { Retain("Private Small calibration admission has not closed."); return; }
                if (!TryRelease("native mode", () => { mode?.Dispose(); mode = null; })) { Retain("Native mode lease has not restored."); return; }
                AssertUnchangedProgression();
                SelectionManager.Instance.MultiSelect(originalSelection.Where(value => value.IsInState && value.View != null).Select(value => value.View), false);
                Require(SelectionManager.Instance.SelectedUnits.SequenceEqual(originalSelection), "Original native selection did not restore exactly.");
                ummLease?.Dispose(); ummLease = null; Game.Instance.IsPaused = pause;
                Require(Hash(sourcePath) == SourceArtifactHash && Hash(archivePath) == SourceArchiveHash,
                    "Calibration changed its read-only source archive/artifact.");
                evidence["cleanup"] = new JObject { ["custodySettled"] = true, ["nativeWorkerSettled"] = !Busy(),
                    ["selectorClosed"] = field == null || !field.Owned, ["selectionRestored"] = true, ["pauseRestored"] = Game.Instance.IsPaused == pause,
                    ["acquiredNativeStateRetained"] = true, ["sourceUnchanged"] = true, ["nativeWrites"] = 0,
                    ["manualPartyMutations"] = 0, ["manualRefunds"] = 0, ["manualFactRemovals"] = 0, ["manualCompanionRemovals"] = 0,
                    ["ownerCount"] = world.Registry.Owners.Length, ["nativeCommandsSettled"] = CommandsSettled(), ["poseResidueAbsent"] = true,
                    ["targetRemoved"] = targetService == null, ["observerReleased"] = trace == null,
                    ["modeRestored"] = mode == null, ["pairAiRestored"] = pairAi == null || pairAi.LastRestoreVerified, ["authority"] = AuthoritySnapshot() };
                Pass("C7-small-cleanup"); settled = true; evidence["elapsedSeconds"] = clock.Elapsed.TotalSeconds;
                RuntimeLoaderInventory.WriteArtifact(request, EvidenceFileName, EvidenceKind, errors.Count == 0 ? "PASS" : "FAIL", evidence, errors); completed = true;
            }
            catch (Exception error) { Fail("C7-small-cleanup", error.ToString()); Retain("Small calibration cleanup retains exact ownership: " + error.Message); }
        }
    }
}
