using System;
using System.Linq;
using Kingmaker.UI.Selection;
using KingmakerMountedCombat.Domain;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Chunk7SmallFixtureScenario
    {
        private void BeginCleanup() { if (cleanup) return; cleanup = true; cleanupStart = clock.Elapsed.TotalSeconds; }
        private void PollCleanup()
        {
            try
            {
                RequireAuthority(true);
                if (Busy() || loadProbe?.InSynchronousCallback == true || saveProbe?.InCallback == true ||
                    persistence.DrainForTeardown(0) == OwnedWorkerTeardownVerdict.Refused)
                { Retain("F09 native load/save worker or observer callback retains custody."); return; }
                if (admitted)
                {
                    RequireWorld();
                    var driver = stage >= 3 ? hireDriver : mainDriver;
                    if (cancellation == null && driver != null && driver.TryGetOwnedBuildSession(out var builder))
                        cancellation = new Chunk7NativeBuilderCancellation(builder, () => { RequireAuthority(true); RequireWorld(); }, Busy);
                    if (cancellation != null && !cancellation.TryDrain()) { Retain("F09 owned native builder cancellation retains custody."); return; }
                    if (field != null && !field.TryDrain()) { Retain("F09 owned native field selector retains custody."); return; }
                    RequireNoOwners();
                    if (presentationCaptured)
                    {
                        RequireWorld();
                        SelectionManager.Instance.MultiSelect(originalSelection.Where(value => value.IsInState && value.View != null).Select(value => value.View), false);
                        Require(SelectionManager.Instance.SelectedUnits.SequenceEqual(originalSelection), "F09 initial native selection did not restore exactly.");
                        ummLease?.Dispose(); ummLease = null; game.IsPaused = originalPause;
                        Require(game.IsPaused == originalPause, "F09 original native pause did not restore.");
                    }
                }
                if (errors.Count == 0) Require(saveProven, "F09 cannot complete without its actual unmounted native Manual proof.");
                var unchanged = inputPins.Count == 4;
                try { VerifyInputs(); } catch (Exception error) { unchanged = false; Fail("C7-small-fixture-cleanup", error.ToString()); }
                try { RequireNoEffects(); } catch (Exception error) { Fail("C7-small-fixture-cleanup", error.ToString()); }
                if (saveProbe != null) evidence["nativeSave"] = saveProbe.Capture();
                if (restoration != null) evidence["noPreparation"] = restoration.Capture();
                if (loadProbe != null) evidence["nativeLoad"] = loadProbe.Capture();
                if (admitted) evidence["settledWorld"] = WorldSnapshot();
                // Unpatch independently. The save probe borrows loadProbe's identity
                // registry, so a failed save unpatch must retain that registry.
                var saveReleased = saveProbe == null; Exception failure = null;
                try { saveProbe?.Dispose(); saveReleased = true; } catch (Exception error) { failure = error; Fail("C7-small-fixture-cleanup", error.ToString()); }
                try { restoration?.Dispose(); } catch (Exception error) { failure = failure ?? error; Fail("C7-small-fixture-cleanup", error.ToString()); }
                if (saveReleased) try { loadProbe?.Dispose(); } catch (Exception error) { failure = failure ?? error; Fail("C7-small-fixture-cleanup", error.ToString()); }
                if (failure != null) throw new InvalidOperationException("F09 observer release retains exact custody.", failure);
                Require(loadProbe == null || loadProbe.IdentityReleased, "F09 native observation labels remain retained.");
                evidence["cleanup"] = new JObject { ["custodySettled"] = true, ["nativeWorkerSettled"] = !Busy(),
                    ["observersReleased"] = true, ["worldAdmitted"] = admitted, ["sourceUnchanged"] = unchanged,
                    ["selectorClosed"] = field == null || !field.Owned,
                    ["selectionRestored"] = !presentationCaptured || SelectionManager.Instance.SelectedUnits.SequenceEqual(originalSelection),
                    ["pauseRestored"] = !presentationCaptured || game.IsPaused == originalPause, ["ummLeaseReleased"] = ummLease == null,
                    ["acquiredNativeStateRetained"] = true, ["manualRefunds"] = 0, ["manualCompanionRemovals"] = 0,
                    ["manualFactRemovals"] = 0, ["manualPartyMutations"] = 0, ["authority"] = AuthoritySnapshot() };
                evidence["elapsedSeconds"] = clock.Elapsed.TotalSeconds; settled = true; Pass("C7-small-fixture-cleanup");
                RuntimeLoaderInventory.WriteArtifact(request, EvidenceFileName, EvidenceKind, errors.Count == 0 ? "PASS" : "FAIL", evidence, errors);
                completed = true;
            }
            catch (Exception error) { Fail("C7-small-fixture-cleanup", error.ToString()); Retain(error.Message); }
        }
        private void Retain(string reason)
        {
            if (pending || clock.Elapsed.TotalSeconds - cleanupStart <= 45) return;
            Fail("C7-small-fixture-cleanup", reason + " The failure deadline never relinquishes custody.");
            RuntimeLoaderInventory.WriteArtifact(request, PendingFileName, PendingKind, "FAIL", new JObject { ["reason"] = reason,
                ["cleanupCustodyRetained"] = true, ["cleanupPendingAtHostBoundary"] = true, ["authority"] = AuthoritySnapshot() }, errors); pending = true;
        }
        internal bool DrainForHost()
        {
            if (disposed) return true;
            if (!cleanup) { Fail("C7-small-fixture-interrupted", "F09 fixture interrupted before its terminal native proof."); BeginCleanup(); }
            if (!completed) PollCleanup(); return completed && settled;
        }
        public void Dispose() { if (disposed) return; if (!DrainForHost()) throw new InvalidOperationException("F09 fixture cleanup retains custody."); disposed = true; }
    }
}
