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
                if (!target.Commands.Empty) throw new InvalidOperationException("Threat actor owns an unrelated native command.");
                castingBoundary["beforeAttack"] = CastingState();
                castingThreatAttack = new UnitAttack(rider) { CreatedByPlayer = true };
                target.Commands.Run(castingThreatAttack);
                castingBoundary["nativeHostileAttackInputCount"] = 1;
                castingBoundary["hostileActor"] = target.UniqueId;
            }
        }
        private bool CastingBoundaryReady()
        {
            if (CastingMotionCase)
            {
                castingMotionCarrier = (CastingMounted ? horse : rider).Commands.Move as UnitMoveTo;
                if (castingMotionCarrier == null || !castingMotionCarrier.IsRunning || CastingMounted && !combat.HasActiveGroundMovement) return false;
                castingBoundary["beforeCastDuringMove"] = CastingState();
                castingBoundary["carrier"] = castingTrace.Identity(castingMotionCarrier); castingBoundary["costCarrier"] = castingCosts.ObjectIdentity(castingMotionCarrier);
                castingBoundary["carrierExecutor"] = castingMotionCarrier.Executor.UniqueId;
            }
            if (CastingThreatCase)
            {
                castingBoundary["riderEngaged"] = rider.CombatState.IsEngaged;
                if (!rider.CombatState.IsEngaged) return false;
                if (castingThreatAttack != null && !castingThreatAttack.IsFinished) castingThreatAttack.Interrupt();
                castingBoundary["nativeAttackTerminalBeforeCast"] = castingThreatAttack.IsFinished;
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
        private void ApplyCastingIncapacity()
        {
            castingLifeSubject = CastingCase == "C6C-rider-incapacity" ? rider : horse;
            var subject = castingLifeSubject; var state = subject.Descriptor.State;
            var factor = Game.Instance.Player.Difficulty.DamageToParty;
            var hp = subject.Stats.HitPoints.ModifiedValue; var con = subject.Stats.Constitution.ModifiedValue;
            var needed = hp + 1 - subject.Damage + subject.Stats.TemporaryHitPoints.ModifiedValue;
            if (!state.IsConscious || state.IsDead || !(bool)state.AllowDyingCondition || state.Immortality ||
                subject.Descriptor.IsEssentialForGame || subject == Game.Instance.Player.MainCharacter.Value ||
                !target.IsPlayersEnemy || factor <= 0 || float.IsNaN(factor) || float.IsInfinity(factor) || needed <= 0)
                throw new InvalidOperationException("Casting incapacity stimulus lacks a safe disposable native subject.");
            var requested = checked((int)Math.Ceiling((needed + 1d) / factor));
            if (subject.Damage + requested * factor - subject.Stats.TemporaryHitPoints.ModifiedValue >= hp + con)
                throw new InvalidOperationException("Casting incapacity has no native nonlethal margin.");
            castingLifeDamageBefore = subject.Damage;
            castingBoundary["beforeIncapacity"] = CastingState(); castingBoundary["subject"] = subject.UniqueId;
            castingBoundary["damageBefore"] = castingLifeDamageBefore;
            var damage = Rulebook.Trigger(new RuleDealDamage(target, subject,
                new DamageBundle(new DirectDamage(new DiceFormula(0, DiceType.Zero), requested))));
            castingLifeApplied = true;
            castingBoundary["damageRule"] = castingTrace.Identity(damage);
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