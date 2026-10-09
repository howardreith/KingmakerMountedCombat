using System;
using System.Linq;
using Kingmaker;
using Kingmaker.Controllers.Clicks.Handlers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Commands;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        private UnitEntityData castingLifeSubject;
        private int castingLifeDamageBefore;
        private bool castingLifeRestoring, castingLifeApplied;
        private UnitMoveTo castingMotionCarrier;
        private UnitAttack castingThreatAttack;
        private JObject castingBoundary;
        private readonly JArray castingMotionSamples = new JArray();
        private bool CastingLifeCase => CastingCase == "C6C-rider-incapacity" || CastingCase == "C6C-mount-incapacity";
        private bool CastingMotionCase => CastingCase == "C6C-movement-policy";
        private bool CastingThreatCase => CastingCase == "C6C-under-threat";
        private bool CastingFullRoundCase => CastingCase == "C6C-full-round";

        private AbilityData ResolveNativeFullRound()
        {
            var candidates = rider.Descriptor.Spellbooks.SelectMany(book => book.GetAllMemorizedSpells()
                .Where(slot => slot.Available).SelectMany(slot => book.GetSpontaneousConversionSpells(slot.Spell)
                    .Where(bp => bp.IsFullRoundAction && slot.Spell.CanBeConvertedTo(bp))
                    .Select(bp => new { Slot = slot, Converted = new AbilityData(slot.Spell, bp) }))).ToArray();
            CastingMeasurement["fullRoundNativeConversions"] = new JArray(candidates.Select(c => new JObject {
                ["originalSlot"] = castingTrace.Identity(c.Slot), ["originalAvailable"] = c.Slot.Available,
                ["ability"] = castingTrace.Ability(c.Converted) }));
            var available = candidates.Where(c => c.Converted.IsAvailableForCast).OrderBy(c => c.Converted.Blueprint.AssetGuid, StringComparer.Ordinal).ToArray();
            if (available.Length == 0) return null;
            castingPrepared = available[0].Slot;
            return available[0].Converted;
        }
        private void ResetCastingBoundary()
        {
            castingLifeSubject = null; castingLifeRestoring = castingLifeApplied = false;
            castingMotionCarrier = null; castingThreatAttack = null; castingMotionSamples.Clear();
            castingBoundary = new JObject { ["case"] = CastingCase };
        }
        private void StartCastingBoundarySetup()
        {
            SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
            if (CastingMotionCase)
            {
                var destination = FindWalkablePoint((CastingMounted ? horse : rider).Position, 6f, .5f);
                castingBoundary["beforeMove"] = CastingState();
                castingBoundary["destination"] = CapturePosition(destination);
                ClickGroundHandler.MoveSelectedUnitsToPoint(destination, false);
                castingBoundary["nativeGroundInputCount"] = 1;
            }
            if (CastingThreatCase)
            {
                // Turn-based: a hostile command run during the rider's turn never starts and is interrupted
                // (frozen 203 TB 6E hostile-attack steps), so the attack is issued on the hostile's own
                // native turn by TickCastingThreatHostileTurn instead.
                if (CastingTb) { castingBoundary["hostileAttackDeferredToHostileTurn"] = true; return; }
                IssueCastingThreatAttack();
            }
        }
        private void IssueCastingThreatAttack()
        {
            if (!target.Commands.Empty) throw new InvalidOperationException("Threat actor owns an unrelated native command.");
            castingBoundary["beforeAttack"] = CastingState();
            castingThreatAttack = new UnitAttack(rider) { CreatedByPlayer = true };
            target.Commands.Run(castingThreatAttack);
            castingBoundary["nativeHostileAttackInputCount"] = 1;
            castingBoundary["hostileActor"] = target.UniqueId;
        }
        // Ends the exact fixture turns until the hostile's own turn and issues the threat attack there.
        private void TickCastingThreatHostileTurn(TurnController turn)
        {
            if (turn == null) return;
            if (turn.Unit == target)
            {
                if (castingThreatAttack != null) return;
                castingBoundary["hostileTurnStatus"] = turn.Status.ToString();
                IssueCastingThreatAttack();
                return;
            }
            EndCastingNativeTurn(turn);
        }
        // UnitMoveTo approaches before IsStarted/IsRunning. Observe the exact
        // live Move carrier and native movement instead; paired ground ownership
        // applies only to the rider's TB partner context.
        private static bool IsCastingMoveReady(UnitMoveTo command, UnitEntityData mover,
            UnitMoveTo moveSlot, bool nativeMoving, bool requiresPairedOwner, bool pairedOwner) =>
            mover != null && command != null && !command.IsFinished &&
            ReferenceEquals(command.Executor, mover) && ReferenceEquals(command, moveSlot) &&
            command.CreatedByPlayer && nativeMoving && (!requiresPairedOwner || pairedOwner);

        private bool CastingBoundaryReady()
        {
            if (CastingMotionCase)
            {
                var mover = CastingMounted ? horse : rider;
                castingMotionCarrier = mover.Commands.Move as UnitMoveTo;
                var nativeMoving = mover.View?.AgentASP?.IsReallyMoving == true;
                if (!IsCastingMoveReady(castingMotionCarrier, mover, mover.Commands.Move,
                    nativeMoving, CastingMounted && CastingTb, combat.HasActiveGroundMovement)) return false;
                castingBoundary["nativeMovingBeforeCast"] = nativeMoving;
                castingBoundary["beforeCastDuringMove"] = CastingState();
                castingBoundary["carrier"] = castingTrace.Identity(castingMotionCarrier); castingBoundary["costCarrier"] = castingCosts.ObjectIdentity(castingMotionCarrier);
                castingBoundary["carrierExecutor"] = castingMotionCarrier.Executor.UniqueId;
            }
            if (CastingThreatCase)
            {
                castingBoundary["riderEngaged"] = rider.CombatState.IsEngaged;
                if (!rider.CombatState.IsEngaged) return false;
                if (castingThreatAttack != null && !castingThreatAttack.IsFinished) castingThreatAttack.Interrupt();
                castingBoundary["nativeAttackTerminalBeforeCast"] = castingThreatAttack != null && castingThreatAttack.IsFinished;
                castingBoundary["beforeThreatCast"] = CastingState();
            }
            return true;
        }
        private void ObserveCastingMotion()
        {
            if (!CastingMotionCase || castingMotionSamples.Count >= 600) return;
            castingMotionSamples.Add(new JObject { ["frame"] = Time.frameCount,
                ["carrier"] = castingTrace.Identity(castingMotionCarrier), ["carrierFinished"] = castingMotionCarrier?.IsFinished,
                ["moverMoveSlotOwnsCarrier"] = ReferenceEquals((CastingMounted ? horse : rider).Commands.Move, castingMotionCarrier),
                ["pairMovement"] = combat.HasActiveGroundMovement,
                ["shellStarted"] = castingShell?.IsStarted, ["shellActed"] = castingShell?.IsActed,
                ["shellFinished"] = castingShell?.IsFinished });
        }
        // The original damage stimulus still refuses an unclamped main character.
        // For this disposable fixture only, the native RuleDealDamage cap bounds
        // every calculated/modifier result before difficulty is applied. Require
        // no temporary HP and a checked native float/difficulty result strictly
        // below death; no LifeState, action, preparation or turn field is written.
        private static int? PlanCastingMainCharacterDamageCap(int hitPoints, int constitution,
            int damageBefore, int temporaryHitPoints, float difficulty)
        {
            if (hitPoints <= 0 || constitution <= 1 || damageBefore < 0 ||
                damageBefore >= hitPoints || temporaryHitPoints != 0 || difficulty <= 0 ||
                float.IsNaN(difficulty) || float.IsInfinity(difficulty) ||
                (long)hitPoints + constitution > int.MaxValue) return null;
            var needed = (long)hitPoints + 1 - damageBefore;
            var requestedValue = Math.Ceiling((needed + 1d) / difficulty);
            if (requestedValue < 1 || requestedValue > int.MaxValue) return null;
            var requested = (int)requestedValue;
            var nativeProduct = requested * difficulty;
            if (float.IsNaN(nativeProduct) || float.IsInfinity(nativeProduct) ||
                nativeProduct >= int.MaxValue) return null;
            var nativeMaximum = Math.Max(1, (int)nativeProduct);
            var projected = (long)damageBefore + nativeMaximum;
            if (projected < hitPoints || projected >= (long)hitPoints + constitution) return null;
            return checked(hitPoints - requested);
        }
        private static bool CastingHealthBoundarySettled(bool riderCommandsEmpty, bool mountCommandsEmpty,
            bool processesSettled, bool abilitiesPending, bool projectilesPending) =>
            riderCommandsEmpty && mountCommandsEmpty && processesSettled && !abilitiesPending && !projectilesPending;
        private static RuleDealDamage CreateCastingIncapacityRule(UnitEntityData source,
            UnitEntityData subject, int requested, bool mainCharacter, int? nativeCap)
        {
            if (source == null || subject == null || ReferenceEquals(source, subject) ||
                requested <= 0 || (mainCharacter && !nativeCap.HasValue))
                throw new InvalidOperationException("Unbounded main-character or invalid incapacity stimulus refused.");
            return new RuleDealDamage(source, subject,
                new DamageBundle(new DirectDamage(new DiceFormula(0, DiceType.Zero), requested))) {
                MinHPAfterDamage = nativeCap
            };
        }
        private void ApplyCastingIncapacity()
        {
            castingLifeSubject = CastingCase == "C6C-rider-incapacity" ? rider : horse;
            var subject = castingLifeSubject; var state = subject.Descriptor.State;
            var factor = Game.Instance.Player.Difficulty.DamageToParty;
            var hp = subject.Stats.HitPoints.ModifiedValue; var con = subject.Stats.Constitution.ModifiedValue;
            var needed = hp + 1 - subject.Damage + subject.Stats.TemporaryHitPoints.ModifiedValue;
            var mainCharacter = subject == Game.Instance.Player.MainCharacter.Value;
            var nativeCap = mainCharacter ? PlanCastingMainCharacterDamageCap(hp, con, subject.Damage,
                subject.Stats.TemporaryHitPoints.ModifiedValue, factor) : null;
            castingBoundary["beforeIncapacity"] = CastingState();
            castingBoundary["subject"] = subject.UniqueId;
            castingBoundary["mainCharacter"] = mainCharacter;
            castingBoundary["nativeDamageCap"] = nativeCap;
            castingBoundary["difficulty"] = factor;
            if (!state.IsConscious || state.IsDead || !(bool)state.AllowDyingCondition || state.Immortality ||
                subject.Descriptor.IsEssentialForGame || (mainCharacter && !nativeCap.HasValue) ||
                target == null || !target.IsInState || !target.IsPlayersEnemy ||
                factor <= 0 || float.IsNaN(factor) || float.IsInfinity(factor) || needed <= 0)
                throw new InvalidOperationException("Casting incapacity stimulus lacks a safe disposable native subject.");
            var requested = checked((int)Math.Ceiling((needed + 1d) / factor));
            if (subject.Damage + requested * factor - subject.Stats.TemporaryHitPoints.ModifiedValue >= hp + con)
                throw new InvalidOperationException("Casting incapacity has no native nonlethal margin.");
            castingLifeDamageBefore = subject.Damage;
            castingBoundary["beforeIncapacity"] = CastingState(); castingBoundary["subject"] = subject.UniqueId;
            castingBoundary["damageBefore"] = castingLifeDamageBefore;
            var rule = CreateCastingIncapacityRule(target, subject, requested, mainCharacter, nativeCap);
            castingBoundary["damageRule"] = castingTrace.Identity(rule);
            // Retain the exact subject/health owner before synchronous native
            // callbacks: a throw after partial damage must not lose restoration.
            castingLifeApplied = true;
            var damage = Rulebook.Trigger(rule);
            castingBoundary["nativeDamageBeforeDifficulty"] = damage.DamageBeforeDifficulty;
            castingBoundary["nativeRuleDamageCap"] = damage.MinHPAfterDamage;
            castingBoundary["nativeRuleDifficulty"] = damage.DifficultyModifier;
            castingBoundary["nativeRuleIsFake"] = damage.IsFake;
            castingBoundary["nativeDamage"] = damage.Damage; castingBoundary["damageAfter"] = subject.Damage;
            castingBoundary["requested"] = requested; castingBoundary["hitPoints"] = hp;
            castingBoundary["deathThreshold"] = hp + con;
        }
        private void ObserveCastingIncapacity()
        {
            if (!castingLifeApplied) return;
            var state = castingLifeSubject.Descriptor.State;
            if (!state.IsConscious) castingBoundary["unconsciousObserved"] = true;
            if (state.IsDead) castingBoundary["deadObserved"] = true;
            castingBoundary["relationshipAfterLife"] = relationship.State.ToString();
            castingBoundary["generationAfterLife"] = relationship.MountedPairGeneration;
        }
        private bool RestoreCastingIncapacity()
        {
            if (!castingLifeApplied) return true;
            var subject = castingLifeSubject; var state = subject.Descriptor.State;
            if (!castingLifeRestoring)
            {
                if (!state.IsConscious) castingBoundary["unconsciousObserved"] = true;
                if (castingBoundary["deadObserved"] == null) castingBoundary["deadObserved"] = state.IsDead;
                castingBoundary["beforeRestore"] = CastingState();
                // Same reversible health-only fixture compensation as the qualified
                // 6A/6B incapacity rows. No action/resource/turn field is written.
                subject.Descriptor.Damage = castingLifeDamageBefore;
                castingLifeRestoring = true;
            }
            var restored = state.IsConscious && !state.IsDead && subject.Damage == castingLifeDamageBefore;
            castingBoundary["damageRestored"] = subject.Damage; castingBoundary["healthRestored"] = restored;
            return restored;
        }
    }
}