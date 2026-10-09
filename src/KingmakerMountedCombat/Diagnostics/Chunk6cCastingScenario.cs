using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.Controllers.Clicks.Handlers;
using Kingmaker.Controllers.Combat;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // Stock rider casting/item baseline. No mounted casting adapter, native cost
    // override, process replay or staged movement authority is introduced here.
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        internal static bool IsChunk6cCastingScenario(string scenario) =>
            scenario == "chunk6c-casting-rt" || scenario == "chunk6c-casting-tb" ||
            scenario == "chunk6c-casting-unmounted-rt" || scenario == "chunk6c-casting-unmounted-tb";
        private bool IsChunk6cCasting => IsChunk6cCastingScenario(request.Scenario);
        private bool CastingTb => request.Scenario.EndsWith("-tb", StringComparison.Ordinal);
        private bool CastingMounted => !request.Scenario.Contains("-unmounted-");
        // The fixture Druid has no memorized level-0 slot: an orison's AbilityData.IsAvailable is
        // false and UnitUseAbility.OnAction fails before RuleCastSpell (preview.200 native fact;
        // native Spellbook.Memorize sets SpellSlot.Available=false until rest). Spellbook casts
        // therefore use the three memorized level-1 slots (CLW quickened by the rod, Snowball, the
        // spontaneous summon conversion); every other cast uses the exact CLW scroll stack.
        internal const int ScrollStackCount = 10;
        // Two units keep the exact potion entity in its slot after the one measured drink: a
        // single unit would be removed natively and the foreign quick-slot refill would move an
        // original potion out of another party member's slot (preview.200 native fact).
        internal const int PotionStackCount = 2;
        private const string Heal = "5590652e1c2225c4ca30c4a699ab3649";
        private const string Snowball = "9f10909f0be1f5141bf1c102041f93d9";
        internal static readonly string[] CastingCases = { "C6C-quickened-self", "C6C-standard-self",
            "C6C-standard-friendly", "C6C-standard-hostile", "C6C-scroll-interrupt-after",
            "C6C-invalid-target", "C6C-cancel-before", "C6C-interrupt-before",
            "C6C-potion-self", "C6C-scroll-friendly", "C6C-full-round", "C6C-movement-policy", "C6C-rider-incapacity", "C6C-mount-incapacity", "C6C-under-threat" };
        private NativeCastingItemTrace castingTrace;
        private NativeActorAllocationTrace castingCosts;
        private readonly List<NativeCastingItemLease> castingItems = new List<NativeCastingItemLease>();
        private NativeCastingItemLease castingRod, castingPotion, castingScroll;
        private NativeCastingOriginalSlots castingOriginalSlots;
        private int castingCaseIndex, castingStage, castingEventOffset, castingCostOffset, castingSetupCostOffset;
        private AbilityData castingAbility;
        private SpellSlot castingPrepared;
        private UnitUseAbility castingShell;
        private UnitEntityData castingTarget;
        private JObject castingCaseFacts;
        private bool castingMountClick, castingInterrupted, castingCleanupStarted;
        // Bounded leaf-clock restarts per row on native turn changes (turn-based), and the bounded
        // turn-based remount after a life row (see TickCastingTurnBasedRemount).
        internal const int CastingTurnResetLimit = 16;
        internal const int CastingRemountAttemptLimit = 3;
        private int castingTurnResets, castingRemountAttempts, castingRemountIdleFrames;
        private UnitMoveTo castingRemountOrder;
        private TurnController castingRemountTurn;
        private int castingStableFrames;
        private TurnController castingEndedTurn, castingSeenTurn;
        private string CastingCase => CastingCases[castingCaseIndex];
        private JObject CastingMeasurement => (JObject)observations[CastingMeasurementKeyFor(request.Scenario)];

        private void BeginChunk6cCasting()
        {
            if (!settings.EnablePairedActivation || settings.EnableUnifiedMountedTurn ||
                settings.EnablePairedCommandScheduler || settings.EnableDiagnosticOverlay || playerAction.OverlayPresent)
                throw new InvalidOperationException("Casting baseline requires the accepted single paired authority.");
            CaptureIdleFixturePartyForCleanup();
            castingTrace = new NativeCastingItemTrace(rider, horse);
            castingCosts = new NativeActorAllocationTrace(rider, horse, combat);
            castingCosts.BoundaryObserved += ObserveCastingNativeCost;
            CastingMeasurementInit();
            castingOriginalSlots = new NativeCastingOriginalSlots(rider, castingTrace);
            // Retain each owner before native acquisition so partial equip failures
            // stay reachable through the existing bounded fixture cleanup path.
            castingRod = AcquireCastingItem(NativeCastingItemLease.LesserQuickenRod);
            castingPotion = AcquireCastingItem(NativeCastingItemLease.CurePotion, PotionStackCount);
            castingScroll = AcquireCastingItem(NativeCastingItemLease.CureScroll, ScrollStackCount);
            observations["chunk6cCasting"]["items"] = new JArray(castingItems.Select(i => i.Evidence));
            step = Phase3dHorseStep.Phase3gControls;
            ResetLeafClock();
        }
        private void CastingMeasurementInit()
        {
            observations["chunk6cCasting"] = new JObject {
                ["contract"] = "native-mounted-casting-items-v1", ["mode"] = CastingTb ? "TB" : "RT",
                ["mounted"] = CastingMounted, ["rider"] = rider.UniqueId, ["mount"] = horse.UniqueId,
                ["mountBlueprint"] = horse.Blueprint.AssetGuid, ["cases"] = new JArray(CastingCases),
                ["initialPrepared"] = CastingPreparedInventory(), ["snapshots"] = new JArray(),
                ["policy"] = "Stock UnitUseAbility owns every cost, resource, targeting and process."
            };
        }
        private NativeCastingItemLease AcquireCastingItem(string blueprint, int count = 1)
        {
            var lease = new NativeCastingItemLease(rider, castingTrace, blueprint, castingOriginalSlots);
            castingItems.Add(lease);
            lease.Acquire(count);
            return lease;
        }
        private JArray CastingPreparedInventory() => new JArray(rider.Descriptor.Spellbooks.SelectMany(b => b.GetAllMemorizedSpells())
            .Select(s => new JObject { ["slot"] = castingTrace.Identity(s), ["available"] = s.Available,
                ["ability"] = castingTrace.Ability(s.Spell) }));
        private AbilityData ResolveCastingAbility(string blueprint, bool prepared)
        {
            castingPrepared = null;
            if (prepared)
            {
                var matches = rider.Descriptor.Spellbooks.SelectMany(b => b.GetAllMemorizedSpells())
                    .Where(s => s.Available && s.Spell.Blueprint.AssetGuid == blueprint).ToArray();
                if (matches.Length != 1) throw new InvalidOperationException("Fixture has no unique available native prepared spell: " + blueprint);
                castingPrepared = matches[0]; return castingPrepared.Spell;
            }
            var spells = rider.Descriptor.Spellbooks.SelectMany(b => b.GetKnownSpells(0))
                .Where(s => s.Blueprint.AssetGuid == blueprint).ToArray();
            if (spells.Length != 1) throw new InvalidOperationException("Fixture has no unique native cantrip: " + blueprint);
            return spells[0];
        }
        // Every item-sourced row casts from the exact equipped disposable stack while at least two
        // units remain, so the native spend decrements the stack in place and never removes the
        // entity or invokes the foreign quick-slot refill. The ability must source that entity.
        private AbilityData ExactCastingItemAbility(NativeCastingItemLease lease)
        {
            lease.RequireExactlyEquipped("Item-sourced casting row " + CastingCase, 2);
            var ability = lease.Item.Ability?.Data;
            if (ability == null || !ReferenceEquals(ability.SourceItem, lease.Item))
                throw new InvalidOperationException("Native item ability does not source the exact equipped fixture item.");
            return ability;
        }
        private JObject CastingState() => new JObject {
            ["frame"] = Time.frameCount, ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks,
            ["relationship"] = relationship.State.ToString(), ["generation"] = relationship.MountedPairGeneration,
            ["relationshipRider"] = relationship.Rider?.UniqueId, ["relationshipMount"] = relationship.Mount?.UniqueId,
            ["rider"] = castingCosts.Snapshot(rider), ["mount"] = castingCosts.Snapshot(horse),
            ["riderCommandsEmpty"] = rider.Commands.Empty, ["mountCommandsEmpty"] = horse.Commands.Empty,
            ["activePairCommand"] = combat.HasActiveCommand, ["pairMovement"] = combat.HasActiveGroundMovement,
            ["projectilesPending"] = NativeSaveEffectBoundary.HasUnresolvedProjectiles(), ["riderLife"] = CaptureChunk6aPendingIncapacityActor(rider), ["mountLife"] = CaptureChunk6aPendingIncapacityActor(horse), ["prepared"] = CastingPreparedInventory(), ["ability"] = castingTrace.Ability(castingAbility),
            ["slotAvailable"] = castingPrepared?.Available, ["slot"] = castingTrace.Identity(castingPrepared),
            ["items"] = new JArray(castingItems.Select(i => i.Snapshot())),
            ["shell"] = castingShell == null ? null : castingTrace.Shell(castingShell),
            ["processesSettled"] = castingTrace.ProcessesSettled,
            ["nativeAbilitiesPending"] = NativeSaveEffectBoundary.HasUnresolvedAbilities()
        };
        private void TickChunk6cCasting()
        {
            var game = Game.Instance; var controller = game.TurnBasedCombatController; var turn = controller.CurrentTurn;
            if (game.IsPaused) { game.IsPaused = false; return; }
            // A new native turn restarts the leaf clock, but only a bounded number of times per row:
            // a turn-cycling stall must surface as a leaf deadline with its progress record instead of
            // the artifact-less 300-second process deadline (frozen 203 unmounted TB).
            if (CastingTb && !ReferenceEquals(turn, castingSeenTurn)) { castingSeenTurn = turn; if (++castingTurnResets <= CastingTurnResetLimit) ResetLeafClock(); }
            CastingMeasurement["progress"] = new JObject { ["stage"] = castingStage, ["caseIndex"] = castingCaseIndex,
                ["case"] = CastingCase, ["rider"] = castingCosts.Snapshot(rider), ["mount"] = castingCosts.Snapshot(horse) };
            if (castingStage == 0)
            {
                if (!rider.Commands.Empty || !horse.Commands.Empty || rider.AreHandsBusyWithAnimation) return;
                SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                if (!CastingMounted && relationship.State != RelationshipState.Unmounted)
                    throw new InvalidOperationException("Unmounted casting control entered mounted.");
                // The reversible fixture AI leases own both actors BEFORE the exploration
                // Mount. Mount captures the mount's raw AI and every lifecycle Dismount
                // restores that captured value; a lease acquired after Mount captured the
                // already-disabled AI, so the forced rider-incapacity Dismount re-enabled
                // ordinary mount attacks and native settlement never arrived (preview.199).
                // Isolating first makes the relationship capture and restore the isolated
                // state; only the lease restores the true original at fixture cleanup.
                var isolationReady = PrepareUnmountedHorseAiIsolation() && PrepareCombatMountRiderAiIsolation();
                var pairMounted = relationship.State == RelationshipState.Mounted;
                if (CastingEntryMayRequestMount(CastingMounted, pairMounted, isolationReady, castingMountClick))
                    castingMountClick = TryNativeAbilityTargetClick(nativeControls.MountAbility, horse, "casting-exploration-mount");
                if (!CastingEntryReady(CastingMounted, pairMounted, isolationReady)) return;
                if (turnBasedModeProbe == null) turnBasedModeProbe = new NativeModeTransitionProbe(CastingTb);
                if (!turnBasedModeProbe.TemporaryValueIsCurrent) { turnBasedModeProbe.DispatchTemporaryValueIfRequired(); return; }
                if (CastingTb && pairedAutomaticEndProbe == null) pairedAutomaticEndProbe = new NativeAutomaticEndProbe(false);
                BeginTarget(6f, "casting-native-baseline"); ruleProbe.Arm(target, false);
                castingCosts.BeginEncounter(request.RunId + ":casting"); castingStage = 1; ResetLeafClock(); return;
            }
            if (castingStage == 1)
            {
                if (!rider.IsInCombat || !horse.IsInCombat) return;
                if (WaitForNativeCastingPrincipal(CastingTb, turn?.Unit == rider,
                    rider.CombatState.CanActInCombat, () => EndCastingNativeTurn(turn))) return;
                if (CastingTb && (!rider.HasStandardAction() || !rider.HasSwiftAction()))
                { EndCastingNativeTurn(turn); return; }
                if (!rider.Commands.Empty || !horse.Commands.Empty || rider.AreHandsBusyWithAnimation ||
                    game.HandsEquipmentController.IsUpdateScheduledFor(rider)) return;
                if (!CastingTb && (rider.CombatState.Cooldown.StandardAction > .001f || rider.CombatState.Cooldown.SwiftAction > .001f)) return;
                if (CastingMounted && relationship.State != RelationshipState.Mounted)
                {
                    if (CastingTb) { TickCastingTurnBasedRemount(turn); return; }
                    if (!castingMountClick) castingMountClick = TryNativeAbilityTargetClick(nativeControls.MountAbility, horse, "casting-native-remount-after-life");
                    return;
                }
                ResetCastingBoundary(); castingMountClick = false; castingTurnResets = 0;
                castingRemountOrder = null; castingRemountTurn = null; castingRemountAttempts = 0; castingRemountIdleFrames = 0;
                castingShell = null; castingAbility = null; castingPrepared = null;
                castingTrace.BeginCase(CastingCase); castingSetupCostOffset = castingCosts.EventCount;
                if (CastingMotionCase || CastingThreatCase) { StartCastingBoundarySetup(); castingStage = 4; ResetLeafClock(); return; }
                BeginCastingCase(); return;
            }
            if (castingStage == 4)
            {
                if (!CastingBoundaryReady()) { if (CastingTb && CastingThreatCase) TickCastingThreatHostileTurn(turn); return; }
                if (CastingTb && CastingThreatCase && (turn?.Unit != rider || !rider.HasStandardAction())) { EndCastingNativeTurn(turn); return; }
                castingBoundary["setupCostEvents"] = castingCosts.EventsSince(castingSetupCostOffset);
                BeginCastingCase(); return;
            }
            if (castingStage == 2)
            {
                ObserveCastingMotion();
                if (CastingLifeCase && !castingLifeApplied && castingShell != null && castingShell.IsRunning && !castingShell.IsActed)
                    ApplyCastingIncapacity();
                ObserveCastingIncapacity();
                if (!castingInterrupted && CastingCase == "C6C-interrupt-before" && castingShell != null && castingShell.IsRunning && !castingShell.IsActed)
                {
                    castingCaseFacts["interruptionBefore"] = CastingState(); castingInterrupted = true;
                    castingShell.Interrupt(); castingCaseFacts["interruptionAfter"] = CastingState();
                }
                if (castingShell != null && !castingShell.IsFinished) return;
                if (!rider.Commands.Empty || !horse.Commands.Empty || !castingTrace.ProcessesSettled ||
                    NativeSaveEffectBoundary.HasUnresolvedAbilities() || NativeSaveEffectBoundary.HasUnresolvedProjectiles()) return;
                if (++castingStableFrames < 10) return;
                if (CastingLifeCase) {
                    castingBoundary["settledBeforeHealthRestore"] = CastingState();
                    if (!RestoreCastingIncapacity()) return;
                }
                // The converted full-round row's exact native summons are fixture residue once the cast
                // settles: a live summon owns a foreign turn-based turn and joins the leased party group
                // before the next row (frozen 202 TB stages 2/4 refused End Turn on a foreign actor).
                // Release them through the same native destruction the drain uses and advance only once
                // nothing of them remains; the row records that release.
                if (CastingFullRoundCase)
                {
                    var summonsReleased = ReleaseCastingSummons();
                    castingCaseFacts["summonCleanup"] = CaptureCastingSummonCleanup();
                    if (!summonsReleased) return;
                }
                castingCaseFacts["after"] = CastingState();
                castingCaseFacts["boundary"] = castingBoundary.DeepClone();
                castingCaseFacts["motionSamples"] = castingMotionSamples.DeepClone();
                castingCaseFacts["events"] = castingTrace.EventsSince(castingEventOffset);
                castingCaseFacts["costEvents"] = castingCosts.EventsSince(castingCostOffset);
                castingCaseFacts["interrupted"] = castingInterrupted;
                // The producer reports bounded facts and structural completeness;
                // the external reader owns substantive PASS/FAIL for every row.
                AddRow(CastingCase, castingTrace.Complete && castingCosts.Complete,
                    "Observed normal native rider casting/item input and terminal ownership.", castingCaseFacts);
                if (castingCaseIndex == 0) { castingRod.ReleaseOwnedItem(); CastingMeasurement["rodCleanup"] = castingRod.Evidence.DeepClone(); }
                castingCaseIndex++; castingMountClick = false; castingTurnResets = 0;
                if (castingCaseIndex == CastingCases.Length) { BeginCleanup(); return; }
                castingStage = 1; ResetLeafClock();
            }
        }
        // A unit-only spell cannot select empty ground. Enemy targeting is not
        // inherently invalid in the native CanTarget/OnClick contract.
        private static GameObject CastingInputObject(string caseId, GameObject targetView) =>
            caseId == "C6C-invalid-target" ? null : targetView;

        // Target roles: the hostile rows click the exact hostile; the friendly, post-commit interruption,
        // scroll heal and threatened rows click the mount; every other row targets the rider. The
        // threatened row targets the mount because the native casting-defensively window opens only for
        // a Standard shell still running after one second while engaged (UnitUseAbility.OnTick,
        // 0x06002734; TryCastingDefensively, 0x0600273B, exempts wand sources only): a self-targeted
        // CLW acts after ~0.55 s, the touch cast at the mount after ~1.5 s (frozen 202 RT/TB stages).
        internal static string CastingTargetRole(string caseId) =>
            caseId == "C6C-standard-hostile" || caseId == "C6C-invalid-target" ? "hostile" :
            caseId == "C6C-standard-friendly" || caseId == "C6C-scroll-interrupt-after" ||
            caseId == "C6C-scroll-friendly" || caseId == "C6C-under-threat" ? "mount" : "rider";

        private void BeginCastingCase()
        {
            castingTrace.BeginCase(CastingCase); castingEventOffset = castingTrace.EventCount; castingCostOffset = castingCosts.EventCount;
            castingShell = null; castingPrepared = null; castingInterrupted = false; castingStableFrames = 0;
            var targetRole = CastingTargetRole(CastingCase);
            castingTarget = targetRole == "hostile" ? target : targetRole == "mount" ? horse : rider;
            // Instruments: the rod quickens the memorized CLW slot; Snowball is the memorized hostile
            // spell; the full-round row converts the remaining slot; the potion row drinks the potion;
            // every other row casts CLW from the exact native scroll stack (unit-only, touch range).
            castingAbility = CastingFullRoundCase ? ResolveNativeFullRound() :
                CastingCase == "C6C-standard-hostile" ? ResolveCastingAbility(Snowball, true) :
                CastingCase == "C6C-quickened-self" ? ResolveCastingAbility(Heal, true) :
                ExactCastingItemAbility(CastingCase == "C6C-potion-self" ? castingPotion : castingScroll);
            if (CastingFullRoundCase && castingAbility == null)
            {
                castingCaseFacts = new JObject { ["case"] = CastingCase, ["availability"] = "none-native",
                    ["conversions"] = CastingMeasurement["fullRoundNativeConversions"].DeepClone(), ["inputCount"] = 0,
                    ["before"] = CastingState(), ["afterInput"] = CastingState() };
                castingStage = 2; ResetLeafClock(); return;
            }
            if (castingAbility == null || castingAbility.Caster.Unit != rider)
                throw new InvalidOperationException("Native ability/item caster is not the exact rider.");
            if (CastingCase == "C6C-scroll-interrupt-after" || CastingCase == "C6C-potion-self" || CastingCase == "C6C-scroll-friendly")
                WoundCastingSubject(castingTarget);
            castingCaseFacts = new JObject { ["case"] = CastingCase, ["target"] = castingTarget.UniqueId,
                ["before"] = CastingState(), ["inputCount"] = 0 };
            SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
            var handler = Game.Instance.SelectedAbilityHandler;
            handler.SetAbility(castingAbility);
            var invalidTarget = CastingCase == "C6C-invalid-target";
            if (invalidTarget && castingAbility.TargetAnchor != AbilityTargetAnchor.Unit)
                throw new InvalidOperationException("Refusal fixture requires a native unit-only spell.");
            var inputObject = CastingInputObject(CastingCase, castingTarget.View.gameObject);
            var inputPoint = invalidTarget ? FindWalkablePoint(rider.Position, 4f, .5f) : castingTarget.Position;
            castingCaseFacts["targetKind"] = invalidTarget ? "empty-ground" : "unit";
            castingCaseFacts["target"] = invalidTarget ? null : castingTarget.UniqueId;
            var resolved = handler.GetTarget(inputObject, inputPoint, castingAbility);
            castingCaseFacts["resolvedTarget"] = resolved?.Unit?.UniqueId; castingCaseFacts["resolvedPoint"] = resolved == null ? null : CapturePosition(resolved.Point); castingCaseFacts["targetPoint"] = CapturePosition(inputPoint);
            castingCaseFacts["canTarget"] = resolved != null && castingAbility.CanTarget(resolved);
            castingCaseFacts["selected"] = ReferenceEquals(handler.Ability, castingAbility);
            castingCaseFacts["selectionAfter"] = new JArray(SelectionManager.Instance.SelectedUnits.Select(u => u.UniqueId));
            if (CastingCase == "C6C-cancel-before")
            {
                handler.SetAbility(null); castingCaseFacts["cancelledSelection"] = handler.Ability == null;
            }
            else
            {
                castingCaseFacts["clicked"] = handler.OnClick(inputObject, inputPoint, 0, false, false);
                castingCaseFacts["inputCount"] = 1;
                var shells = rider.Commands.Raw.Concat(rider.Commands.Queue).OfType<UnitUseAbility>()
                    .Where(c => c.Spell.Blueprint == castingAbility.Blueprint).Distinct().ToArray();
                castingCaseFacts["admittedShellCount"] = shells.Length;
                if (shells.Length > 1) throw new InvalidOperationException("One normal input admitted multiple same-spell shells.");
                castingShell = shells.SingleOrDefault(); castingCaseFacts["costShell"] = castingCosts.ObjectIdentity(castingShell);
            }
            castingCaseFacts["afterInput"] = CastingState();
            castingStage = 2; ResetLeafClock();
        }
        private void ObserveCastingNativeCost(string boundary, UnitEntityData actor, UnitCommand command)
        {
            if (cleanupStarted || castingCaseIndex >= CastingCases.Length || CastingCase != "C6C-scroll-interrupt-after" ||
                castingInterrupted || boundary != "cost-after" || actor != rider || command != castingShell) return;
            castingCaseFacts["interruptionBefore"] = CastingState();
            castingInterrupted = true;
            // The native acted transition has already charged. Interrupt only the
            // exact shell through its native API; never stop/tick/refund its process.
            command.Interrupt(); castingCaseFacts["interruptionAfter"] = CastingState();
        }
        private void WoundCastingSubject(UnitEntityData subject)
        {
            var factor = Game.Instance.Player.Difficulty.DamageToParty;
            if (!subject.Descriptor.State.IsConscious || subject.Damage < 0 || factor <= 0 || float.IsNaN(factor) || float.IsInfinity(factor))
                throw new InvalidOperationException("Heal stimulus lacks intact disposable subject and finite native difficulty.");
            if (subject.Damage > 0) return; // Reuse an existing nonlethal wound; never erase it to create a heal result.
            var amount = (int)Math.Ceiling(3d / factor);
            if (amount * factor >= subject.Stats.HitPoints.ModifiedValue - 2) throw new InvalidOperationException("Heal stimulus has no safe native margin.");
            Rulebook.Trigger(new RuleDealDamage(target, subject, new DamageBundle(new DirectDamage(new DiceFormula(0, DiceType.Zero), amount))));
        }
        // Fixture input progression only. A rider is normally unable to act on
        // another actor's native turn; that fact must not block its lawful End Turn.
        // The caller's existing input helper still validates the exact native actor.
        internal static bool WaitForNativeCastingPrincipal(bool turnBased, bool principalTurn,
            bool principalCanAct, Action advanceNativeTurn)
        {
            if (turnBased && !principalTurn) { advanceNativeTurn(); return true; }
            return !principalCanAct;
        }
        // Fixture entry ordering: both reversible AI leases must own the pair before the
        // exploration Mount is requested, and a mounted baseline proceeds only once the
        // native Mount has settled. Pure decisions; the caller owns every side effect.
        internal static bool CastingEntryMayRequestMount(bool mounted, bool pairMounted, bool aiIsolationReady, bool mountRequested) =>
            mounted && !pairMounted && aiIsolationReady && !mountRequested;
        internal static bool CastingEntryReady(bool mounted, bool pairMounted, bool aiIsolationReady) =>
            aiIsolationReady && (!mounted || pairMounted);
        // Bounded raw facts for a 6C leaf deadline: the exact case, its live boundary and
        // the pair's command/AI/life state. Observation only; nothing is mutated.
        private JObject CaptureChunk6cCastingDeadlineProgress()
        {
            var progress = new JObject { ["stage"] = castingStage, ["caseIndex"] = castingCaseIndex,
                ["case"] = castingCaseIndex < CastingCases.Length ? CastingCase : null,
                ["mountClickRequested"] = castingMountClick, ["castingCleanupStarted"] = castingCleanupStarted };
            try
            {
                progress["boundary"] = castingBoundary?.DeepClone();
                progress["caseFacts"] = castingCaseFacts?.DeepClone();
                progress["state"] = castingTrace == null || castingCosts == null ? null : CastingState();
                progress["mountRawAi"] = AiBackingField == null ? JValue.CreateNull() : new JValue((bool)AiBackingField.GetValue(horse));
                progress["riderRawAi"] = AiBackingField == null ? JValue.CreateNull() : new JValue((bool)AiBackingField.GetValue(rider));
                progress["mountEffectiveAi"] = horse.IsAIEnabled; progress["riderEffectiveAi"] = rider.IsAIEnabled;
                progress["mountCommands"] = CaptureCastingCommandSurface(horse);
                progress["riderCommands"] = CaptureCastingCommandSurface(rider);
                progress["mountAiIsolation"] = CaptureUnmountedHorseAiIsolation();
                progress["riderAiIsolation"] = CaptureCombatMountRiderAiIsolation();
            }
            catch (Exception exception) { progress["captureError"] = exception.GetType().Name + ": " + exception.Message; }
            return progress;
        }
        private static JObject CaptureCastingCommandSurface(UnitEntityData actor) => new JObject {
            ["inCombat"] = actor.IsInCombat, ["empty"] = actor.Commands.Empty,
            ["raw"] = new JArray(actor.Commands.Raw.Select(c => c == null ? JValue.CreateNull() : new JValue(
                c.GetType().Name + ":" + (c.IsFinished ? "finished" : c.IsRunning ? "running" : "pending")))),
            ["queue"] = new JArray(actor.Commands.Queue.Select(c => new JValue(c.GetType().Name))) };
        // Turn-based remount after a life row. The product admits the combat Mount transition only on
        // an ACTING rider turn (CM01-combat-mount-preparing-refused: frozen 203 mounted TB clicked Mount
        // one frame after the rider's new turn prepared and the shell was interrupted at once), and a
        // native turn becomes Acting only through a real order. The fixture therefore issues one bounded
        // native ground order first (the 6A acting-turn setup pattern) and clicks Mount once the turn is
        // Acting. Every order and click is recorded with its native outcome, a shell that finished
        // unmounted releases the latch for a bounded retry, and exhausted attempts fail the row explicitly.
        private JArray CastingRemounts => (JArray)(CastingMeasurement["remounts"] ?? (CastingMeasurement["remounts"] = new JArray()));
        private void TickCastingTurnBasedRemount(TurnController turn)
        {
            if (turn?.Unit != rider) { EndCastingNativeTurn(turn); return; }
            if (!rider.Commands.Empty || !horse.Commands.Empty || combat.HasActiveCommand || rider.View?.AgentASP?.IsReallyMoving == true)
            { castingRemountIdleFrames = 0; return; }
            if (castingMountClick)
            {
                // The admitted click settled without a mounted pair: a refused native attempt.
                if (++castingRemountIdleFrames < 10) return;
                var last = (JObject)CastingRemounts[CastingRemounts.Count - 1];
                last["outcome"] = "finished-unmounted"; last["outcomeFrame"] = Time.frameCount;
                last["shell"] = CaptureNativeAbilityShell(lastNativeAbilityShell); last["after"] = CastingState();
                castingMountClick = false; castingRemountIdleFrames = 0;
                if (castingRemountAttempts >= CastingRemountAttemptLimit)
                    throw new InvalidOperationException("Casting fixture could not remount in turn-based combat after " +
                        castingRemountAttempts + " native attempts: " + CastingRemounts.ToString(Newtonsoft.Json.Formatting.None));
                return;
            }
            if (turn.Status == TurnController.TurnStatus.Preparing)
            {
                if (castingRemountOrder != null && ReferenceEquals(castingRemountTurn, turn)) return;
                SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
                var destination = FindWalkablePoint(rider.Position, 0.75f, .25f);
                var order = new JObject { ["kind"] = "acting-ground-order", ["frame"] = Time.frameCount, ["turnStatus"] = turn.Status.ToString(),
                    ["destination"] = CapturePosition(destination), ["before"] = CastingState() };
                ClickGroundHandler.MoveSelectedUnitsToPoint(destination, false);
                castingRemountOrder = rider.Commands.Move as UnitMoveTo; castingRemountTurn = turn;
                order["admitted"] = castingRemountOrder != null && castingRemountOrder.Executor == rider && castingRemountOrder.CreatedByPlayer;
                CastingRemounts.Add(order);
                return;
            }
            if (!turn.IsActing) return;
            var attempt = new JObject { ["kind"] = "mount-click", ["attempt"] = ++castingRemountAttempts, ["frame"] = Time.frameCount,
                ["turnStatus"] = turn.Status.ToString(), ["before"] = CastingState() };
            castingMountClick = TryNativeAbilityTargetClick(nativeControls.MountAbility, horse, "casting-native-remount-after-life-" + castingRemountAttempts);
            attempt["clicked"] = castingMountClick; castingRemountIdleFrames = 0;
            CastingRemounts.Add(attempt);
            if (!castingMountClick && castingRemountAttempts >= CastingRemountAttemptLimit)
                throw new InvalidOperationException("Casting fixture's turn-based remount click was not admitted after " +
                    castingRemountAttempts + " attempts: " + CastingRemounts.ToString(Newtonsoft.Json.Formatting.None));
        }
        private void EndCastingNativeTurn(TurnController turn)
        {
            var actor = turn?.Unit;
            if (actor == null || ReferenceEquals(turn, castingEndedTurn) || !actor.IsDirectlyControllable ||
                !actor.Commands.Empty || actor.AreHandsBusyWithAnimation || !turn.CanEndTurnAndNoActing() ||
                Game.Instance.TurnBasedCombatController.WaitingForUI || GetPendingNextUnit(Game.Instance.TurnBasedCombatController) != null) return;
            if (actor != rider && actor != horse && !(targetService.NonPairPartyAiLease.OwnsExactMember(actor) && targetService.NonPairPartyAiLease.ValidateActive()))
                throw new InvalidOperationException("Casting fixture refused End Turn on a foreign actor.");
            castingCosts?.Record("casting-end-turn-input-before", actor);
            Game.Instance.PauseBind();
            castingCosts?.Record("casting-end-turn-input-after", actor); castingEndedTurn = turn; ResetLeafClock();
        }
        private static bool CastingSummonInWorld(UnitEntityData unit) => Game.Instance.State.Units.Any(x => ReferenceEquals(x, unit));
        private JArray CaptureCastingSummonCleanup() => new JArray(castingTrace.Summons.Select(u => new JObject {
            ["unit"] = u.UniqueId, ["inState"] = u.IsInState, ["worldContains"] = CastingSummonInWorld(u) }));
        // Destroys the fixture's exact native summons through the engine's own entity destroyer and
        // reports whether any of them still remains (CastingSummonResidue owns that predicate).
        private bool ReleaseCastingSummons()
        {
            foreach (var unit in castingTrace.Summons) { if (unit.IsInState) unit.Destroy(); }
            Game.Instance.EntityDestroyer.Tick();
            return !CastingSummonResidue.Remains(castingTrace.Summons, u => u.IsInState, CastingSummonInWorld);
        }
        // Cleanup in turn-based combat: the native encounter ends only at a turn boundary, so the exact
        // idle fixture turns (rider, mount, idle player-party members) are ended natively while the party
        // is still in combat, a bounded number of times (frozen 203 TB stages 2, 10 and 12 waited the whole
        // bounded cleanup with the party in combat). Foreign actors are never touched.
        private int castingCleanupTurnEnds;
        private TurnController castingCleanupEndedTurn;
        private void TryEndCastingCleanupTurn()
        {
            if (!CastingTb || !CombatController.IsInTurnBasedCombat() || !Game.Instance.Player.IsInCombat || castingCleanupTurnEnds >= CastingTurnResetLimit) return;
            var controller = Game.Instance.TurnBasedCombatController; var turn = controller.CurrentTurn; var actor = turn?.Unit;
            if (actor == null || ReferenceEquals(turn, castingCleanupEndedTurn) || !actor.IsDirectlyControllable || !actor.Commands.Empty ||
                actor.AreHandsBusyWithAnimation || !turn.CanEndTurnAndNoActing() || controller.WaitingForUI || GetPendingNextUnit(controller) != null) return;
            if (actor != rider && actor != horse && !(actor.Group != null && actor.Group == rider.Group && actor.Group.IsPlayerParty)) return;
            castingCleanupTurnEnds++; castingCleanupEndedTurn = turn;
            CastingMeasurement["cleanupTurnEnds"] = castingCleanupTurnEnds;
            Game.Instance.PauseBind();
        }
        private bool DrainCastingFixture()
        {
            TryEndCastingCleanupTurn();
            if (castingTrace == null) return true;
            if (!castingCleanupStarted) { castingCleanupStarted = true; castingCosts.BoundaryObserved -= ObserveCastingNativeCost; }
            if (castingShell != null && !castingShell.IsFinished) castingShell.Interrupt();
            if (castingThreatAttack != null && !castingThreatAttack.IsFinished) castingThreatAttack.Interrupt();
            ObserveCastingIncapacity();
            if (!CastingHealthBoundarySettled(rider.Commands.Empty, horse.Commands.Empty, castingTrace.ProcessesSettled,
                NativeSaveEffectBoundary.HasUnresolvedAbilities(), NativeSaveEffectBoundary.HasUnresolvedProjectiles())) return false;
            if (!RestoreCastingIncapacity()) return false;
            var summonsReleased = ReleaseCastingSummons();
            CastingMeasurement["summonCleanup"] = CaptureCastingSummonCleanup();
            if (!summonsReleased) return false;
            var itemFailures = new List<Exception>();
            for (var i = castingItems.Count - 1; i >= 0; i--)
            {
                try { castingItems[i].ReleaseOwnedItem(); }
                catch (Exception exception) { itemFailures.Add(exception); }
            }
            // Every exact lease remains retained for the existing bounded retry.
            // One failed item cannot prevent independent item cleanup or its evidence.
            CastingMeasurement["items"] = new JArray(castingItems.Select(i => i.Evidence.DeepClone()));
            if (itemFailures.Count != 0) throw new AggregateException("Casting fixture item cleanup remains incomplete.", itemFailures);
            castingOriginalSlots.Restore();
            foreach (var item in castingItems) item.Dispose();
            CastingMeasurement["originalSlots"] = castingOriginalSlots.Evidence.DeepClone();
            CastingMeasurement["items"] = new JArray(castingItems.Select(i => i.Evidence.DeepClone()));
            CastingMeasurement["final"] = CastingState();
            castingCosts.Dispose(); castingTrace.Dispose();
            CastingMeasurement["costTrace"] = castingCosts.Capture(); CastingMeasurement["castTrace"] = castingTrace.Capture();
            castingCosts = null; castingTrace = null;
            return true;
        }
    }
}