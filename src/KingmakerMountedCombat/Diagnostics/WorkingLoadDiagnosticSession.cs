using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Persistence;
using Newtonsoft.Json.Linq;
using KingmakerMountedCombat.Diagnostics.WorkingLoadPrototype;

namespace KingmakerMountedCombat.Diagnostics
{
    // Host custody is assigned before Install. This owner neither starts a load
    // nor grants completion: it only retains and publishes observer evidence.
    internal sealed class WorkingLoadDiagnosticSession
    {
        internal const string EvidenceFileName = "working-load-deserialization-evidence.json";
        internal const string EvidenceKind = "working-load-deserialization-evidence";
        internal const string PendingFileName = "working-load-deserialization-pending.json";
        internal const string PendingKind = "working-load-deserialization-pending";
        private readonly RuntimeRequest request;
        private readonly NativeWorkingLoadObserver observer;
        private bool pendingWritten, finalWritten;
        private bool loaderAtRelease, taskAtRelease, settledTaskAtRelease, successfulTaskAtRelease;
        internal bool Released => observer.Released;
        internal bool FinalWritten => finalWritten;
        internal WorkingLoadDiagnosticSession(RuntimeRequest request, Game game, SaveManager manager, SaveInfo descriptor)
        {
            if (request == null || request.ObserveWorkingDeserialization != true || request.Validate().Count != 0)
                throw new InvalidOperationException("Working observer requires its exact validated opt-in.");
            this.request = request;
            observer = new NativeWorkingLoadObserver(game, manager, descriptor);
        }
        internal void Install() { observer.Install(); }
        internal bool TryRelease(bool nativeBusy, int authorizedLoadRequests = 0)
        {
            if (Released) return true;
            // An issued load with a missing first hook is unknown custody, not
            // proof that no loader/worker exists. Counts come from the original
            // native authorization prefix, never from the observer itself.
            if (authorizedLoadRequests < 0 || authorizedLoadRequests > 1 || nativeBusy ||
                (authorizedLoadRequests != 0 || observer.LoaderObserved) && !observer.TaskSucceeded) return false;
            loaderAtRelease = observer.LoaderObserved; taskAtRelease = observer.TaskObserved; settledTaskAtRelease = observer.TaskSettled;
            successfulTaskAtRelease = observer.TaskSucceeded;
            return observer.TryRelease(false);
        }
        internal void Pending(bool callbackObserved, bool fixtureVerified, bool nativeBusy, double elapsed, double deadline)
        {
            if (pendingWritten) return;
            var body = Capture(callbackObserved, fixtureVerified, nativeBusy);
            body["firstPendingAtSeconds"] = elapsed; body["pollDeadlineSeconds"] = deadline;
            RuntimeLoaderInventory.WriteArtifact(request, PendingFileName, PendingKind, "FAIL", body,
                new[] { "Working observer custody is retained; this snapshot does not prove native load completion or cleanup." });
            pendingWritten = true;
        }
        internal void PublishFinal(string runtimeStatus, bool callbackObserved, bool fixtureVerified, int nativeLoads, int nativeWrites)
        {
            if (finalWritten) return;
            if (!Released || (runtimeStatus != "PASS" && runtimeStatus != "FAIL") ||
                nativeLoads < 0 || nativeLoads > 1 || nativeWrites != 0 || nativeLoads != 0 && !successfulTaskAtRelease)
                throw new InvalidOperationException("Working observation cannot finalize before verified observer release.");
            var body = Capture(callbackObserved, fixtureVerified, false);
            body["nativeLoadCount"] = nativeLoads; body["nativeWriteCount"] = nativeWrites;
            body["runtimeStatusAtPublication"] = runtimeStatus;
            // A final artifact records the current completion verdict; the outer
            // actual result/exit and restoration remain separately mandatory.
            RuntimeLoaderInventory.WriteArtifact(request, EvidenceFileName, EvidenceKind, runtimeStatus, body, new string[0]);
            finalWritten = true;
        }
        private JObject Capture(bool callbackObserved, bool fixtureVerified, bool nativeBusy)
        {
            var working = request.Fixture.Working;
            return new JObject {
                ["formatVersion"] = 1, ["source"] = new JObject { ["internalName"] = working.InternalName, ["fileName"] = working.FileName, ["sha256"] = working.Sha256 },
                ["gameMvid"] = NativeWorkingLoadContract.GameMvid.ToString(), ["jsonMvid"] = NativeWorkingLoadContract.JsonMvid.ToString(),
                ["observationOnly"] = true, ["nativeResolutionProven"] = false, ["atomicityProven"] = false,
                ["afterLoadCallbackObserved"] = callbackObserved, ["fixtureIdentityVerified"] = fixtureVerified,
                ["nativeBusyAtCapture"] = nativeBusy, ["observerReleased"] = Released,
                ["loaderObserved"] = Released ? loaderAtRelease : observer.LoaderObserved,
                ["taskObserved"] = Released ? taskAtRelease : observer.TaskObserved,
                ["taskSettled"] = Released ? settledTaskAtRelease : observer.TaskSettled,
                ["taskSucceeded"] = Released ? successfulTaskAtRelease : observer.TaskSucceeded,
                ["retainedNativeIdentityCount"] = observer.Observation.RetainedCount,
                ["faults"] = new JArray(observer.Observation.Faults.OrderBy(x => (int)x).Select(x => x.ToString())),
                ["events"] = new JArray(observer.Observation.Events.Select(Event))
            };
        }
        private static JObject Event(ObservationEvent row)
        {
            return new JObject { ["sequence"] = row.Sequence, ["threadId"] = row.ThreadId, ["phase"] = row.Phase,
                ["lane"] = row.Lane.HasValue ? row.Lane.Value.ToString() : null, ["valueIdentity"] = row.ValueIdentity,
                ["scene"] = row.Scene == null ? null : Scene(row.Scene), ["serializer"] = row.Serializer == null ? null : Serializer(row.Serializer) };
        }
        private static JObject Scene(SceneObservation row)
        {
            return new JObject { ["playerIdentity"] = row.PlayerIdentity, ["sceneIdentity"] = row.SceneIdentity,
                ["currentSceneIdentity"] = row.CurrentSceneIdentity, ["mainId"] = row.MainId, ["mainIdComplete"] = row.MainIdComplete,
                ["entityCount"] = row.EntityCount, ["endEntityCount"] = row.EndEntityCount,
                ["complete"] = row.Complete, ["atomicityProven"] = row.AtomicityProven, ["matchingPrefixCount"] = row.MatchingPrefixCount,
                ["prefix"] = new JArray((row.Prefix ?? new EntityObservation[0]).Select(e => new JObject {
                    ["identity"] = e.Identity, ["id"] = e.Id, ["type"] = e.Type, ["textComplete"] = e.TextComplete,
                    ["holdingSceneMatches"] = e.HoldingSceneMatches, ["mainIdMatches"] = e.MainIdMatches.HasValue ? new JValue(e.MainIdMatches.Value) : JValue.CreateNull() })) };
        }
        private static JObject Serializer(SerializerObservation row)
        {
            return new JObject { ["identity"] = row.Identity, ["converterCount"] = row.ConverterCount,
                ["contractResolverType"] = row.ContractResolverType, ["existingReferenceResolverType"] = row.ExistingReferenceResolverType,
                ["converterTypes"] = new JArray(row.ConverterTypes ?? new string[0]), ["complete"] = row.Complete,
                ["collectionsEnablementAvailable"] = row.CollectionsEnablementAvailable,
                ["collectionsEnabled"] = row.CollectionsEnablementAvailable ? new JValue(row.CollectionsEnabled) : JValue.CreateNull() };
        }
    }
}
