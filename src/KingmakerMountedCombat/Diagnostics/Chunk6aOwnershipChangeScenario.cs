using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.Parts;
using KingmakerMountedCombat.Domain;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        internal const string Chunk6aOwnershipChangeScenario = "chunk6a-ownership-change";
        private const string Chunk6aOwnershipRow = "CM02-ownership-change";
        private static readonly MethodInfo Chunk6aSetMasterMethod = typeof(UnitDescriptor).GetMethod(
            "SetMaster", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            null, new[] { typeof(UnitEntityData) }, null);
        private static readonly FieldInfo Chunk6aWeatherLastBuff = typeof(UnitPartPartyWeatherBuff).GetField(
            "m_LastBuff", BindingFlags.NonPublic | BindingFlags.Instance);

        private bool Chunk6aOwnershipOnly => request.Scenario == Chunk6aOwnershipChangeScenario;
        private JObject chunk6aOwnershipEvidence, chunk6aOwnershipOriginal;
        private UnitCommand chunk6aOwnershipCommand;
        private bool chunk6aOwnershipStimulated, chunk6aOwnershipDetached;
        private int chunk6aOwnershipStimulusCount, chunk6aOwnershipRestoreCount, chunk6aOwnershipCleanupInterruptCount;

        private void TickChunk6aOwnershipChange()
        {
            if (!Chunk6aOwnershipOnly || Chunk6aTurnBased)
                throw new InvalidOperationException("Ownership change requires its isolated RT transaction.");
            if (chunk6aStage == 36)
            {
                if (!Chunk6aIdle || !nativeControls.Evaluate(NativeMountedControlKind.MountCompanion, rider).IsEnabled) return;
                if (!EnsureChunk6aRiderSelection(Chunk6aOwnershipRow)) return;
                var start = CaptureChunk6aGeometry("ownership-pre-click");
                if ((bool)start["isAdjacent"])
                    throw new InvalidOperationException("Ownership-change Mount must start outside transition reach.");
                chunk6aOwnershipOriginal = CaptureChunk6aOwnership();
                chunk6aOwnershipEvidence = new JObject {
                    ["contract"] = "native-ownership-loss-during-exact-mount-approach",
                    ["start"] = start, ["ownershipBefore"] = chunk6aOwnershipOriginal.DeepClone(),
                    ["nativeMethod"] = CaptureChunk6aSetMasterIdentity(), ["diagnosticInterruptCount"] = 0
                };
                observations["chunk6aOwnershipChange"] = chunk6aOwnershipEvidence;
                if (!Chunk6aOwnershipFixtureValid(chunk6aOwnershipOriginal))
                {
                    FailCurrent(Chunk6aOwnershipRow, "Ownership-change fixture lacks the exact reversible native ownership state: " + chunk6aOwnershipOriginal);
                    BeginCleanup(); return;
                }
                chunk6aCommandWindow = new NativeRelationshipCommandProbe(nativeControls, allocationTrace, rider, horse,
                    nativeControls.MountAbility.AssetGuid, CaptureChunk6aCausalState, true);
                var clicked = TryNativeAbilityTargetClick(nativeControls.MountAbility, horse, "chunk6a-ownership-change-click");
                chunk6aCommandWindow.ClickCompleted(clicked);
                if (!clicked)
                {
                    FailCurrent(Chunk6aOwnershipRow, "Exact rider native Mount click was refused: " + playerAction.LastFeedback);
                    BeginCleanup(); return;
                }
                chunk6aOwnershipCommand = chunk6aCommandWindow.Command;
                chunk6aStage = 37; ResetLeafClock(); return;
            }

            var command = chunk6aCommandWindow?.Command;
            if (!chunk6aOwnershipStimulated)
            {
                if (command == null || !ReferenceEquals(command, chunk6aOwnershipCommand) ||
                    command.IsStarted || command.IsActed || command.IsFinished || command.ExecutionProcess != null)
                {
                    FailCurrent(Chunk6aOwnershipRow, "The exact Mount left pending approach before native ownership loss.");
                    BeginCleanup(); return;
                }
                var moved = Chunk6aPlanarDistance((JObject)chunk6aOwnershipEvidence["start"]["riderPosition"], CapturePosition(rider.Position));
                if (!chunk6aCommandWindow.ApproachObserved || !rider.View.AgentASP.IsReallyMoving || moved <= 0.25f) return;
                var geometry = CaptureChunk6aGeometry("ownership-trigger");
                var slot = rider.Commands.GetCommand(UnitCommand.CommandType.Move);
                var selected = SelectionManager.Instance.SelectedUnits;
                var ownershipAtTrigger = CaptureChunk6aOwnership();
                if ((bool)geometry["isAdjacent"] || !ReferenceEquals(slot, command) || selected.Count != 1 || selected[0] != rider ||
                    !JToken.DeepEquals(ownershipAtTrigger, chunk6aOwnershipOriginal))
                {
                    FailCurrent(Chunk6aOwnershipRow, "Ownership change missed the exact selected rider's non-adjacent pending Mount or the original pair changed first.");
                    BeginCleanup(); return;
                }
                chunk6aOwnershipEvidence["trigger"] = new JObject {
                    ["frame"] = Time.frameCount, ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks,
                    ["approachObserved"] = true, ["riderReallyMoving"] = rider.View.AgentASP.IsReallyMoving,
                    ["riderDisplacement"] = moved, ["commandObject"] = RuntimeHelpers.GetHashCode(command),
                    ["moveSlotObject"] = RuntimeHelpers.GetHashCode(slot), ["started"] = command.IsStarted,
                    ["acted"] = command.IsActed, ["finished"] = command.IsFinished, ["geometry"] = geometry
                };
                var stimulus = new JObject {
                    ["contract"] = "one-native-unit-descriptor-set-master-null",
                    ["method"] = CaptureChunk6aSetMasterIdentity(), ["before"] = ownershipAtTrigger.DeepClone(),
                    ["commandBefore"] = CaptureOrdinaryCommand(command), ["frameBefore"] = Time.frameCount,
                    ["gameTicksBefore"] = Game.Instance.TimeController.GameTime.Ticks
                };
                chunk6aOwnershipEvidence["stimulus"] = stimulus;
                chunk6aOwnershipStimulated = true;
                chunk6aOwnershipStimulusCount++;
                horse.Descriptor.SetMaster(null);
                chunk6aOwnershipDetached = rider.Descriptor.Pet == null || horse.Descriptor.Master.Value == null;
                var afterDetach = CaptureChunk6aOwnership();
                stimulus["count"] = chunk6aOwnershipStimulusCount;
                stimulus["after"] = afterDetach.DeepClone();
                stimulus["commandAfter"] = CaptureOrdinaryCommand(command);
                stimulus["frameAfter"] = Time.frameCount;
                stimulus["gameTicksAfter"] = Game.Instance.TimeController.GameTime.Ticks;
                chunk6aOwnershipEvidence["ownershipAfterDetach"] = afterDetach.DeepClone();
                chunk6aOwnershipEvidence["inputsUnchangedAfterDetach"] = Chunk6aOwnershipInputsEqual(chunk6aOwnershipOriginal, afterDetach);
                if (!Chunk6aOwnershipDetachedValid(afterDetach))
                {
                    FailCurrent(Chunk6aOwnershipRow, "One native SetMaster(null) did not produce the exact reciprocal ownership and weather-part loss.");
                    BeginCleanup(); return;
                }
                ResetLeafClock(); return;
            }

            if (command == null || command.IsActed || command.ExecutionProcess != null || relationship.State != RelationshipState.Unmounted)
            {
                FailCurrent(Chunk6aOwnershipRow, "The ownership-invalidated Mount acted, acquired a process, or transitioned instead of ending unacted.");
                BeginCleanup(); return;
            }
            if (!command.IsFinished || !Chunk6aIdle) return;

            var proof = chunk6aCommandWindow.FinishUnacted("unacted-native-ownership-loss-no-cost-or-transition");
            chunk6aOwnershipEvidence["commandProof"] = proof;
            chunk6aOwnershipEvidence["terminal"] = CaptureOrdinaryCommand(command);
            chunk6aOwnershipEvidence["ownershipBeforeRestoration"] = CaptureChunk6aOwnership();
            chunk6aOwnershipEvidence["commandTerminalBeforeRestoration"] = command.IsFinished;
            chunk6aCommandWindow.Dispose(); chunk6aCommandWindow = null;
            var restored = RestoreChunk6aOwnership(false);
            chunk6aOwnershipEvidence["ownershipAfterRestoration"] = CaptureChunk6aOwnership();
            chunk6aOwnershipEvidence["restored"] = restored;
            chunk6aOwnershipEvidence["stimulusCount"] = chunk6aOwnershipStimulusCount;
            chunk6aOwnershipEvidence["restorationCount"] = chunk6aOwnershipRestoreCount;
            chunk6aOwnershipEvidence["diagnosticInterruptCount"] = chunk6aOwnershipCleanupInterruptCount;
            var noResidue = relationship.State == RelationshipState.Unmounted && !playerAction.HasVoluntaryTransitionInFlight &&
                rider.Commands.Empty && horse.Commands.Empty;
            chunk6aOwnershipEvidence["noResidue"] = noResidue;
            var pass = (bool)proof["pass"] && restored && noResidue && chunk6aOwnershipStimulusCount == 1 &&
                chunk6aOwnershipRestoreCount == 1 && chunk6aOwnershipCleanupInterruptCount == 0 &&
                JToken.DeepEquals(chunk6aOwnershipOriginal, chunk6aOwnershipEvidence["ownershipAfterRestoration"]);
            AddRow(Chunk6aOwnershipRow, pass,
                pass ? "One native SetMaster(null) invalidated the exact pending Mount after measured approach; that command ended unacted with no process, cost, transition or residue, then guarded native reattachment restored every captured ownership side effect." :
                "The exact native ownership-loss terminal or guarded restoration proof failed.", chunk6aOwnershipEvidence);
            chunk6aStage = 99; BeginCleanup();
        }

        private JObject CaptureChunk6aSetMasterIdentity() => new JObject {
            ["declaringType"] = Chunk6aSetMasterMethod?.DeclaringType?.FullName,
            ["method"] = Chunk6aSetMasterMethod?.Name,
            ["token"] = Chunk6aSetMasterMethod?.MetadataToken.ToString("X8"),
            ["moduleMvid"] = Chunk6aSetMasterMethod?.Module.ModuleVersionId.ToString()
        };

        private JObject CaptureChunk6aOwnership()
        {
            var riderPet = rider.Descriptor.Pet;
            var master = horse.Descriptor.Master.Value;
            return new JObject {
                ["riderId"] = rider.UniqueId, ["riderObject"] = RuntimeHelpers.GetHashCode(rider),
                ["mountId"] = horse.UniqueId, ["mountObject"] = RuntimeHelpers.GetHashCode(horse),
                ["riderPetId"] = riderPet?.UniqueId, ["riderPetObject"] = riderPet == null ? 0 : RuntimeHelpers.GetHashCode(riderPet),
                ["riderPetUniqueId"] = rider.Descriptor.PetUniqueId,
                ["masterId"] = master?.UniqueId, ["masterObject"] = master == null ? 0 : RuntimeHelpers.GetHashCode(master),
                ["mountIsPet"] = horse.Descriptor.IsPet,
                ["riderGroup"] = CaptureChunk6aOwnershipGroup(rider), ["mountGroup"] = CaptureChunk6aOwnershipGroup(horse),
                ["riderFaction"] = CaptureChunk6aOwnershipFaction(rider), ["mountFaction"] = CaptureChunk6aOwnershipFaction(horse),
                ["riderFactionAttackSource"] = CaptureChunk6aOwnershipFactions(rider.Faction?.AttackFactions),
                ["mountAttackFactions"] = CaptureChunk6aOwnershipFactions(horse.Descriptor.AttackFactions),
                ["mountAttackFactionsPlayerEnemy"] = horse.Descriptor.AttackFactions.IsPlayerEnemy,
                ["riderPlayerFaction"] = rider.IsPlayerFaction, ["mountPlayerFaction"] = horse.IsPlayerFaction,
                ["weather"] = CaptureChunk6aOwnershipWeather(horse)
            };
        }

        private static JObject CaptureChunk6aOwnershipGroup(UnitEntityData actor)
        {
            var group = actor.Group;
            return new JObject {
                ["id"] = actor.GroupId, ["object"] = group == null ? 0 : RuntimeHelpers.GetHashCode(group),
                ["members"] = group == null ? new JArray() : new JArray(group.Select(member => new JObject {
                    ["id"] = member.UniqueId, ["object"] = RuntimeHelpers.GetHashCode(member)
                }).OrderBy(item => (string)item["id"], StringComparer.Ordinal))
            };
        }

        private static JObject CaptureChunk6aOwnershipFaction(UnitEntityData actor)
        {
            var faction = actor.Faction;
            return new JObject { ["guid"] = faction?.AssetGuid,
                ["object"] = faction == null ? 0 : RuntimeHelpers.GetHashCode(faction) };
        }

        private static JArray CaptureChunk6aOwnershipFactions(IEnumerable<BlueprintFaction> factions) =>
            new JArray((factions ?? Enumerable.Empty<BlueprintFaction>()).Where(faction => faction != null)
                .Select(faction => new JObject { ["guid"] = faction.AssetGuid,
                    ["object"] = RuntimeHelpers.GetHashCode(faction) })
                .OrderBy(item => (string)item["guid"], StringComparer.Ordinal)
                .ThenBy(item => (int)item["object"]));

        private static JObject CaptureChunk6aOwnershipWeather(UnitEntityData actor)
        {
            var part = actor.Descriptor.Get<UnitPartPartyWeatherBuff>();
            var lastBuff = part == null || Chunk6aWeatherLastBuff == null ? null : Chunk6aWeatherLastBuff.GetValue(part);
            return new JObject { ["present"] = part != null,
                ["ownerMatches"] = part != null && ReferenceEquals(part.Owner, actor.Descriptor),
                ["ownerObject"] = part?.Owner == null ? 0 : RuntimeHelpers.GetHashCode(part.Owner),
                ["lastBuffPresent"] = lastBuff != null,
                ["lastBuffObject"] = lastBuff == null ? 0 : RuntimeHelpers.GetHashCode(lastBuff) };
        }

        private bool Chunk6aOwnershipFixtureValid(JObject value)
        {
            var weather = (JObject)value["weather"];
            return Chunk6aSetMasterMethod != null && Chunk6aSetMasterMethod.MetadataToken == 0x06001F17 &&
                Chunk6aSetMasterMethod.Module.ModuleVersionId.ToString() == "07fa1e4d-8618-41b3-9b8d-faa17d3b26f7" &&
                Chunk6aWeatherLastBuff != null && (string)value["riderPetId"] == horse.UniqueId &&
                (int)value["riderPetObject"] == RuntimeHelpers.GetHashCode(horse) &&
                (string)value["riderPetUniqueId"] == horse.UniqueId && (string)value["masterId"] == rider.UniqueId &&
                (int)value["masterObject"] == RuntimeHelpers.GetHashCode(rider) && (bool)value["mountIsPet"] &&
                JToken.DeepEquals(value["riderGroup"], value["mountGroup"]) &&
                JToken.DeepEquals(value["riderFaction"], value["mountFaction"]) &&
                JToken.DeepEquals(value["riderFactionAttackSource"], value["mountAttackFactions"]) &&
                (bool)weather["present"] == (bool)value["riderPlayerFaction"] &&
                (!(bool)weather["present"] || (bool)weather["ownerMatches"]) && !(bool)weather["lastBuffPresent"];
        }

        private static bool Chunk6aOwnershipInputsEqual(JObject before, JObject after)
        {
            foreach (var field in new[] { "riderId", "riderObject", "mountId", "mountObject", "riderGroup", "mountGroup",
                "riderFaction", "mountFaction", "riderFactionAttackSource", "mountAttackFactions",
                "mountAttackFactionsPlayerEnemy", "riderPlayerFaction", "mountPlayerFaction" })
                if (!JToken.DeepEquals(before[field], after[field])) return false;
            return true;
        }

        private bool Chunk6aOwnershipDetachedValid(JObject value)
        {
            var weather = (JObject)value["weather"];
            return chunk6aOwnershipStimulusCount == 1 && (string)value["riderPetId"] == null &&
                (int)value["riderPetObject"] == 0 && string.IsNullOrEmpty((string)value["riderPetUniqueId"]) &&
                (string)value["masterId"] == null && (int)value["masterObject"] == 0 && !(bool)value["mountIsPet"] &&
                !(bool)weather["present"] && !(bool)weather["ownerMatches"] && !(bool)weather["lastBuffPresent"] &&
                Chunk6aOwnershipInputsEqual(chunk6aOwnershipOriginal, value);
        }

        private bool RestoreChunk6aOwnership(bool cleanup)
        {
            if (chunk6aOwnershipOriginal == null) return true;
            var before = CaptureChunk6aOwnership();
            if (!chunk6aOwnershipDetached)
                return JToken.DeepEquals(chunk6aOwnershipOriginal, before);
            var vacant = rider.Descriptor.Pet == null && horse.Descriptor.Master.Value == null;
            var inputsUnchanged = Chunk6aOwnershipInputsEqual(chunk6aOwnershipOriginal, before);
            var record = new JObject { ["cleanup"] = cleanup, ["before"] = before.DeepClone(),
                ["vacantReciprocalReferences"] = vacant, ["inputsUnchanged"] = inputsUnchanged,
                ["method"] = CaptureChunk6aSetMasterIdentity(), ["attempted"] = false };
            chunk6aOwnershipEvidence[cleanup ? "cleanupRestoration" : "restoration"] = record;
            if (!vacant || !inputsUnchanged) { record["pass"] = false; return false; }
            record["attempted"] = true; chunk6aOwnershipRestoreCount++;
            horse.Descriptor.SetMaster(rider);
            var after = CaptureChunk6aOwnership();
            var pass = JToken.DeepEquals(chunk6aOwnershipOriginal, after);
            record["count"] = chunk6aOwnershipRestoreCount; record["after"] = after.DeepClone(); record["pass"] = pass;
            if (pass) chunk6aOwnershipDetached = false;
            return pass;
        }

        private bool CaptureChunk6aOwnershipDeadline()
        {
            if (!Chunk6aOwnershipOnly || chunk6aStage < 36 || chunk6aStage > 37) return false;
            if (chunk6aOwnershipEvidence != null)
            {
                chunk6aOwnershipEvidence["deadlineOwnership"] = CaptureChunk6aOwnership();
                chunk6aOwnershipEvidence["deadlineCommand"] = CaptureOrdinaryCommand(chunk6aOwnershipCommand);
                chunk6aOwnershipEvidence["deadlineStimulated"] = chunk6aOwnershipStimulated;
            }
            FailCurrent(Chunk6aOwnershipRow,
                chunk6aOwnershipStimulated ? "The exact ownership-invalidated Mount did not reach a native unacted terminal at the unchanged 30-second deadline." :
                "The exact Mount did not reach the measured non-adjacent approach boundary for ownership invalidation at the unchanged 30-second deadline.");
            return true;
        }

        private void CleanupChunk6aOwnershipChange()
        {
            if (!Chunk6aOwnershipOnly || chunk6aOwnershipOriginal == null) return;
            if (chunk6aOwnershipEvidence != null)
                chunk6aOwnershipEvidence["cleanupBefore"] = CaptureChunk6aOwnership();
            if (chunk6aOwnershipDetached && chunk6aOwnershipCommand != null && !chunk6aOwnershipCommand.IsFinished)
            {
                chunk6aOwnershipEvidence["cleanupCommandBefore"] = CaptureOrdinaryCommand(chunk6aOwnershipCommand);
                chunk6aOwnershipCommand.Interrupt(); chunk6aOwnershipCleanupInterruptCount++;
                chunk6aOwnershipEvidence["cleanupCommandAfter"] = CaptureOrdinaryCommand(chunk6aOwnershipCommand);
            }
            var restored = RestoreChunk6aOwnership(true);
            if (chunk6aOwnershipEvidence != null)
            {
                chunk6aOwnershipEvidence["cleanupInterruptCount"] = chunk6aOwnershipCleanupInterruptCount;
                chunk6aOwnershipEvidence["cleanupRestored"] = restored;
                chunk6aOwnershipEvidence["cleanupAfter"] = CaptureChunk6aOwnership();
            }
            if (!restored) throw new InvalidOperationException("Ownership cleanup refused to overwrite changed native ownership inputs or did not restore every side effect.");
        }
    }
}
