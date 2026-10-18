using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic.Commands;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // Owner-seed-derived combat (batch 3 scenarios 3-5) on a disposable copy of the
    // owner-seed-derived Manual. The cold load restored both pairs MOUNTED; AdmitMountedStart
    // verifies them against the producer runtime with the cold consumer's shared check and the
    // scenario then skips its native Mount steps. The diagnostic target is admitted from the
    // derived-copy identity under the owner's 2026-10-17 approval. Scenario 3 adds one ordinary
    // native action by the selected unrelated companion, accounted by native events only.
    internal sealed partial class Chunk7CombatScenario
    {
        internal const string CompanionJustification = "An ordinary native attack on the project's diagnostic target is the companion's own ordinary combat action: it spends only her Standard action and cannot harm the target (durability lease), the pairs or the cave population, and a ground move would not exercise the shared native action-cost boundary.";
        private bool OwnerSeedDerived => inherited != null && request.OwnedPairs?.IsOwnerSeedDerivedCombat == true;
        private UnitEntityData companion;
        private UnitAttack companionAttack;
        private JObject companionAction;
        private int companionTraceOffset;

        private bool MountedStartReady() => persistence != null && persistence.ArchiveRestorePhase == "all-pair-presentation-restored" &&
            !persistence.CombatRestorationPending && !controls.HasUnsettledRelationshipTransition && !Game.Instance.Player.IsInCombat &&
            new[] { ar, am, br, bm }.All(Chunk7NativeFixture.Idle);

        private bool RestoredMounted(UnitEntityData rider, UnitEntityData mount)
        {
            var owner = world.Registry.FindActive(rider);
            return owner != null && ReferenceEquals(owner.Mount, mount) && ReferenceEquals(world.Registry.FindActive(mount), owner) &&
                owner.Boundary.State == RelationshipState.Mounted && ReferenceEquals(rider.Descriptor.Pet, mount) && ReferenceEquals(mount.Descriptor.Master.Value, rider);
        }

        private void AdmitMountedStart()
        {
            Require(OwnerSeedDerived && inherited.Bound, "Mounted-start admission belongs only to the owner-seed-derived combat kinds.");
            inherited.RequirePreserved();
            var saved = persistence.LoadedArchive; var input = request.PersistenceLoad;
            Require(saved != null && saved.SchemaVersion == MountedArchiveData.Schema && saved.SchemaVersion == NativeMountedSaveFormat.SelectedWriterSchema &&
                saved.Pairs.Length == 2 && saved.Participations.Length == 0 && saved.Combat == null && saved.CampaignId == input.GameId && saved.AreaId == input.Area &&
                persistence.SemanticRestoreCount == 4 && persistence.PresentationRestoreCount == 2 && persistence.ArchivePostLoadObservedCount == 4 &&
                controls.NativeCastRequestCount == 0 && persistence.SnapshotCount == 0,
                "Owner-seed-derived combat requires the exact current-schema two-pair exploration graph restored once, with no native input or snapshot.");
            a = world.ForActor(ar); b = world.ForActor(br);
            Require(a != null && b != null && !ReferenceEquals(a, b) && world.Registry.ActiveCount == 2 && world.Registry.Owners.Length == 2 &&
                RestoredMounted(ar, am) && RestoredMounted(br, bm) && a.Relationship.State == RelationshipState.Mounted && b.Relationship.State == RelationshipState.Mounted,
                "Both owner-seed-derived pairs were not restored mounted with reciprocal native custody.");
            var profiles = new JArray();
            foreach (var pair in new[] { new[] { ar, am }, new[] { br, bm } })
            {
                var profile = SupportedMountedProfiles.Resolve(pair[0], pair[1]);
                var record = saved.Pairs.SingleOrDefault(p => p.Rider.Id == pair[0].UniqueId && p.Mount.Id == pair[1].UniqueId);
                Require(profile != null && profile.Id == SupportedMountedProfiles.Horse.Id && record != null && record.ProfileId == profile.Id,
                    "A restored owner-seed-derived pair lost its exact Horse profile.");
                profiles.Add(new JObject { ["riderId"] = pair[0].UniqueId, ["mountId"] = pair[1].UniqueId, ["profileId"] = profile.Id });
            }
            // The cold consumer's shared check: graph equals the producer read-back, owners,
            // generations and slots equal the producer runtime.
            var restored = inherited.VerifyRestoredOwnerSeedPairs(saved, world, controls.CapturePersistentSlots());
            var elapsed = Math.Max(0, (Game.Instance.TimeController.GameTime.Ticks - saved.GameTimeTicks) / (double)TimeSpan.TicksPerSecond);
            var serializer = MountedSaveCodec.CreateSerializer(); var debt = new JArray();
            foreach (var record in saved.Pairs.SelectMany(p => new[] { p.Rider, p.Mount }))
            {
                var actor = new[] { ar, am, br, bm }.Single(x => x.UniqueId == record.Id); var after = MountedPersistenceService.CaptureActor(actor);
                Require(Chunk7WorldScenarioEngine.ManualDebtWithinClock(record, after, elapsed), "Restored owner-seed-derived actor debt was lost or regranted before combat.");
                debt.Add(new JObject { ["actorId"] = record.Id, ["saved"] = JObject.FromObject(record, serializer), ["restored"] = JObject.FromObject(after, serializer) });
            }
            actors = new[] { ar, am, br, bm };
            evidence["actorIds"] = new JArray(actors.Select(x => x.UniqueId));
            evidence["mountedStart"] = new JObject { ["contract"] = "owner-seed-derived-mounted-start-v1", ["restoredFromColdLoad"] = true,
                ["nativeMountInputs"] = 0, ["archivePhase"] = persistence.ArchiveRestorePhase, ["schemaVersion"] = saved.SchemaVersion,
                ["semanticRestores"] = persistence.SemanticRestoreCount, ["presentationRestores"] = persistence.PresentationRestoreCount,
                ["nativePostLoads"] = persistence.ArchivePostLoadObservedCount, ["nativeCastRequests"] = controls.NativeCastRequestCount,
                ["snapshots"] = persistence.SnapshotCount, ["profiles"] = profiles, ["owners"] = restored["owners"], ["slots"] = restored["slots"],
                ["loadedEqualsProducerReadback"] = restored["loadedEqualsProducerReadback"], ["producerProcessId"] = restored["producerProcessId"],
                ["processId"] = System.Diagnostics.Process.GetCurrentProcess().Id,
                ["debtRetained"] = true, ["elapsedSeconds"] = elapsed, ["debt"] = debt, ["world"] = WorldSnapshot("mounted-start") };
            var s = request.OwnedPairs;
            evidence["ownerSeedDerivedSource"] = new JObject { ["contract"] = "owner-seed-derived-combat-source-v1", ["ancestry"] = Chunk7WorldScenarioEngine.OwnerSeedDerivedAncestry,
                ["acquisitionProof"] = false, ["ordinaryProducerAncestry"] = false, ["producerRunId"] = s.SourceRunId, ["producerArtifactSha256"] = s.SourceArtifactSha256,
                ["derivedManualSha256"] = s.SourceArchiveSha256, ["consumerRunId"] = s.CheckpointSourceRunId, ["consumerArtifactSha256"] = s.CheckpointSourceArtifactSha256,
                ["seedId"] = s.SeedId, ["seedSha256"] = s.SeedSha256, ["candidates"] = inherited.OwnerSeedCandidates?.DeepClone() };
        }

        // Derived from the admitted fixture (never a literal) and proven placeable before any
        // fixture mutation; any refusal fails closed with its exact native reason.
        private string AdmitTargetAuthorization(Vector3 position)
        {
            var admitted = inherited != null ? inherited.Bound && (!OwnerSeedDerived || evidence["mountedStart"] != null) : originalParty != null;
            var basis = Chunk7CombatTargetAuthorization.Basis(request, Chunk7WorldScenarioEngine.WorkingHash, admitted);
            var s = request.OwnedPairs; var derived = OwnerSeedDerived;
            var row = new JObject { ["contract"] = "chunk7-combat-target-authorization-v1", ["basis"] = basis,
                ["ownerApproval"] = derived ? Chunk7CombatTargetAuthorization.OwnerApproval : null,
                ["workingSha256"] = s == null ? request.Fixture?.Working?.Sha256 : null,
                ["seedSha256"] = derived ? s.SeedSha256 : null, ["derivedManualSha256"] = derived ? s.SourceArchiveSha256 : null,
                ["producerRunId"] = derived ? s.SourceRunId : null, ["producerArtifactSha256"] = derived ? s.SourceArtifactSha256 : null,
                ["consumerRunId"] = derived ? s.CheckpointSourceRunId : null, ["consumerArtifactSha256"] = derived ? s.CheckpointSourceArtifactSha256 : null,
                ["area"] = Game.Instance.CurrentlyLoadedArea?.AssetGuidThreadSafe,
                ["riderPosition"] = new JArray(ar.Position.x, ar.Position.y, ar.Position.z), ["position"] = new JArray(position.x, position.y, position.z) };
            evidence["targetAuthorization"] = row;
            Require(basis != null && (!derived || basis == Chunk7CombatTargetAuthorization.OwnerSeedDerivedBasis),
                "The diagnostic combat target is not authorized for this admitted fixture.");
            try { DiagnosticCombatTargetService.ValidatePlacement(ar.Position, position); row["placementValidated"] = true; }
            catch (InvalidOperationException error)
            {
                row["placementValidated"] = false; row["placementRefusal"] = error.Message;
                throw new InvalidOperationException("Diagnostic target placement refused before any measured step: " + error.Message, error);
            }
            return basis;
        }

        private UnitEntityData ResolveCompanion()
        {
            var id = request.OwnedPairs.CompanionId; var matches = originalParty.Where(u => u.UniqueId == id).ToArray();
            Require(matches.Length == 1 && !actors.Contains(matches[0]) && matches[0].IsDirectlyControllable && !matches[0].Descriptor.IsPet &&
                Game.Instance.Player.PartyCharacters.Count(v => ReferenceEquals(v.Value, matches[0])) == 1 &&
                targetService.NonPairPartyAiLease.OwnsExactMember(matches[0]),
                "The selected unrelated companion is not one exact leased party character outside both pairs.");
            return matches[0];
        }

        private bool CompanionActionReady()
        {
            if (companion == null) companion = ResolveCompanion();
            return PairIdle(a) && PairIdle(b) && companion.IsInCombat && companion.IsInState && companion.Descriptor.State.IsConscious &&
                companion.Commands.Empty && !companion.AreHandsBusyWithAnimation && companion.CombatState.Cooldown.StandardAction <= .001f && target.IsInState;
        }

        private JObject CompanionWindow(string label)
        {
            var snapshot = WorldSnapshot(label);
            snapshot["companion"] = trace.Snapshot(companion);
            snapshot["pairCommands"] = new JObject { ["a"] = NativeCommands(a), ["b"] = NativeCommands(b) };
            return snapshot;
        }

        private void BeginCompanionAction()
        {
            Game.Instance.IsPaused = true;
            // Her own Run admission inside the paused click must be visible; kept for the rest of the RT case.
            trace.DeclareCommandOwner(companion);
            companionTraceOffset = trace.EventCount;
            companionAction = new JObject { ["contract"] = "owner-seed-unrelated-companion-action-v1", ["kind"] = "ordinary-native-attack",
                ["justification"] = CompanionJustification, ["actorId"] = companion.UniqueId, ["targetId"] = target.UniqueId,
                ["traceSequenceBefore"] = companionTraceOffset, ["before"] = CompanionWindow("companion-before") };
            evidence["companionAction"] = companionAction;
            Require(targetService.NonPairPartyAiLease.BeginDeclaredMemberCommand(companion), "The companion's one declared ordinary command was refused by the AI lease.");
            Chunk7NativeFixture.Select(companion); Game.Instance.DefaultPointerController.ClearPointerMode();
            Require(targetService.BeginExpectedAttackDispatch(target), "Companion native target authorization refused.");
            using (var input = new NativeOrdinaryAttackInput(target)) companionAction["clicked"] = input.Click();
            companionAttack = companion.Commands.Raw.OfType<UnitAttack>().SingleOrDefault(c => c.Target == target);
            Require((bool)companionAction["clicked"] && companionAttack?.Executor == companion && !(companionAttack is MountedPairAttackCommand),
                "The companion's ordinary enemy click did not create her exact stock native command.");
            issuedCommands.Add(companionAttack);
            companionAction["command"] = trace.ObjectIdentity(companionAttack); companionAction["commandType"] = companionAttack.GetType().FullName;
            companionAction["admitted"] = CompanionWindow("companion-admitted");
            Require(JToken.DeepEquals(companionAction["before"]["pairCommands"], companionAction["admitted"]["pairCommands"]),
                "The companion's input changed a pair's native commands.");
            Chunk7NativeFixture.Select(ar); companionAction["selectionAfterInput"] = ar.UniqueId; Game.Instance.IsPaused = false;
        }

        // Exactly the boundary names NativeActorAllocationTrace emits (statically checked by the tests).
        internal static readonly string[] CompanionAccountedBoundaries = { "admission-before", "admission-after", "cost-before", "cost-after",
            "actor-cost-before", "actor-cost-after", "command-interrupt-before", "command-interrupt-after" };
        private const string StockAttackType = "Kingmaker.UnitLogic.Commands.UnitAttack";

        // Native RT party brains keep proposing a UnitAttack on a pair actor's ManualTarget about every
        // 0.35 s; KMC refuses each one in its Run prefix before native Run initializes it. Such a proposal
        // is tolerated only as one closed refused-before-Run pair: one admission-before then one
        // admission-after of a fresh exact UnitAttack identity with no executor, never started, result
        // None, appearing in no other window row and in no pair command slot. Every other pair row is touch.
        internal static JArray RefusedPairProposals(JObject[] accounted, string[] pairIds, int companionCommand, JToken[] pairCommandSets, out int pairTouched)
        {
            var slotted = new HashSet<int>(pairCommandSets.SelectMany(set => set.SelectMany(slot => ((JArray)slot).Select(c => (int)c["identity"]))));
            var pairRows = accounted.Where(e => pairIds.Contains((string)e["commandActor"]) || pairIds.Contains((string)e["state"]?["actor"])).ToArray();
            var refused = new JArray(); var accepted = new HashSet<JObject>();
            foreach (var group in pairRows.Where(e => e["command"]?.Type == JTokenType.Integer).GroupBy(e => (int)e["command"]))
            {
                var rows = group.OrderBy(e => (int)e["sequence"]).ToArray(); var identity = group.Key;
                var all = accounted.Where(e => (int?)e["command"] == identity).ToArray();
                var actor = (string)rows[0]["state"]?["actor"];
                var closed = rows.Length == 2 && all.Length == 2 && (string)rows[0]["boundary"] == "admission-before" && (string)rows[1]["boundary"] == "admission-after" &&
                    identity != companionCommand && !slotted.Contains(identity) && pairIds.Contains(actor) &&
                    rows.All(e => (string)e["state"]?["actor"] == actor && (string)e["commandType"] == StockAttackType &&
                        // The trace assigns a null string, a String-typed JValue, never JTokenType.Null.
                        e["commandActor"] != null && (string)e["commandActor"] == null &&
                        (bool?)e["started"] == false && (bool?)e["acted"] == false && (bool?)e["finished"] == false && (string)e["result"] == "None");
                if (!closed) continue;
                foreach (var row in rows) accepted.Add(row);
                refused.Add(new JObject { ["actorId"] = actor, ["command"] = identity, ["sequenceBefore"] = rows[0]["sequence"], ["sequenceAfter"] = rows[1]["sequence"] });
            }
            pairTouched = pairRows.Count(e => !accepted.Contains(e));
            return refused;
        }

        private bool FinishCompanionAction()
        {
            Require(PairIdle(a) && PairIdle(b) && RestoredOrLiveOwnersUnchanged(), "A pair received work or changed custody during the companion's action.");
            if (!companionAttack.IsFinished || !companion.Commands.Empty || companion.AreHandsBusyWithAnimation) return false;
            // One native Stop with only the companion selected clears her ManualTarget, so her own brain
            // cannot propose a second attack; it is her second and last declared input.
            Game.Instance.IsPaused = true; var stopBefore = trace.EventCount;
            Chunk7NativeFixture.Select(companion); SelectionManager.Instance.Stop(); Chunk7NativeFixture.Select(ar);
            companionAction["nativeStop"] = new JObject { ["selectedId"] = companion.UniqueId, ["selectionAfterStop"] = ar.UniqueId,
                ["traceSequenceBefore"] = stopBefore, ["traceSequenceAfter"] = trace.EventCount,
                ["companionManualTargetCleared"] = companion.CombatState.ManualTarget == null, ["companionCommandsEmpty"] = companion.Commands.Empty };
            companionAction["companionInputs"] = new JArray("ordinary-attack-click", "native-stop");
            Game.Instance.IsPaused = false;
            Require((bool)companionAction["nativeStop"]["companionManualTargetCleared"] && companion.Commands.Empty,
                "The companion's native Stop did not clear her ManualTarget or left her a command.");
            var events = trace.EventsSince(companionTraceOffset); var command = trace.ObjectIdentity(companionAttack);
            var pairIds = actors.Select(x => x.UniqueId).ToArray();
            var accounted = events.Cast<JObject>().Where(e => CompanionAccountedBoundaries.Contains((string)e["boundary"])).ToArray();
            companionAction["after"] = CompanionWindow("companion-after"); companionAction["traceSequenceAfter"] = trace.EventCount;
            var refused = RefusedPairProposals(accounted, pairIds, command,
                new[] { companionAction["before"]["pairCommands"], companionAction["admitted"]["pairCommands"], companionAction["after"]["pairCommands"] }
                    .Select(x => (JToken)new JArray(((JObject)x).Properties().Select(prop => prop.Value))).ToArray(), out var pairTouched);
            var companionRows = accounted.Where(e => (string)e["commandActor"] == companion.UniqueId || (string)e["state"]?["actor"] == companion.UniqueId).ToArray();
            var admissions = companionRows.Where(e => (string)e["boundary"] == "admission-after").ToArray();
            var costBefore = companionRows.Where(e => (string)e["boundary"] == "cost-before" && (int?)e["command"] == command).ToArray();
            var costAfter = companionRows.Where(e => (string)e["boundary"] == "cost-after" && (int?)e["command"] == command).ToArray();
            var foreign = companionRows.Where(e => (int?)e["command"] != command).ToArray();
            companionAction["accountedEvents"] = new JArray(accounted.Select(e => e.DeepClone()));
            companionAction["companionCommandCount"] = admissions.Length; companionAction["companionCostEvents"] = costBefore.Length + costAfter.Length;
            companionAction["pairRefusedProposals"] = refused;
            companionAction["pairAccountedEvents"] = pairTouched; companionAction["foreignCompanionEvents"] = foreign.Length;
            Require(admissions.Length == 1 && (int?)admissions[0]["command"] == command && (string)admissions[0]["commandActor"] == companion.UniqueId &&
                costBefore.Length == 1 && costAfter.Length == 1 && foreign.Length == 0,
                "The companion did not take exactly one ordinary native command with its own single native cost.");
            Require(pairTouched == 0, "A pair actor was admitted, cancelled or charged during the companion's action.");
            Require(JToken.DeepEquals(companionAction["before"]["pairCommands"], companionAction["after"]["pairCommands"]),
                "The companion's action reassigned or cancelled a pair command.");
            foreach (var actor in actors)
            {
                var before = Actor((JObject)companionAction["before"], actor); var after = Actor((JObject)companionAction["after"], actor);
                Require(new[] { "standard", "move", "swift" }.All(f => (double)after[f] <= (double)before[f] + .001) && JToken.DeepEquals(before["reactions"], after["reactions"]),
                    "A pair actor's native expenditure rose during the companion's action.");
            }
            Require(targetService.NonPairPartyAiLease.EndDeclaredMemberCommand(companion), "The companion's declared command did not settle back into the AI lease.");
            companionAction["declaredLeaseCommands"] = targetService.NonPairPartyAiLease.DeclaredMemberCommandCount;
            companionAction["terminal"] = true;
            return true;
        }
        private bool RestoredOrLiveOwnersUnchanged()
        {
            var before = companionAction["before"]["owners"] as JArray; var now = WorldSnapshot("companion-poll")["owners"];
            return before != null && JToken.DeepEquals(new JArray(before.Select(o => new JObject { ["id"] = o["id"], ["generation"] = o["generation"], ["state"] = o["state"] })),
                new JArray(((JArray)now).Select(o => new JObject { ["id"] = o["id"], ["generation"] = o["generation"], ["state"] = o["state"] })));
        }
    }
}
