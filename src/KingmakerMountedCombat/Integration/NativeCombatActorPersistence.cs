using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Kingmaker;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Controllers.Brain.Blueprints;
using Kingmaker.Controllers.Combat;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using UnityEngine;

namespace KingmakerMountedCombat.Integration
{
    internal static class NativeCombatActorPersistence
    {
        private static readonly FieldInfo LastMove = Field(typeof(UnitCombatState), "m_LastUsageOfMoveActionTime", 0x04005E90, typeof(TimeSpan));
        private static readonly FieldInfo LastDeflect = Field(typeof(UnitCombatState), "m_LastDeflectArrowTime", 0x04005EA4, typeof(TimeSpan));
        private static readonly FieldInfo ExecutedAttacks = Field(typeof(UnitCombatState), "<ExecutedAttackNumber>k__BackingField", 0x04005E99, typeof(int));
        private static readonly FieldInfo PreventNext = Field(typeof(UnitCombatState), "<PreventAttacksOfOpporunityNextFrame>k__BackingField", 0x04005EA5, typeof(bool));
        private static readonly FieldInfo Engaged = Field(typeof(UnitCombatState), "m_EngagedUnits", 0x04005E91, typeof(Dictionary<UnitEntityData, TimeSpan>));
        private static readonly FieldInfo EngagedBy = Field(typeof(UnitCombatState), "m_EngagedBy", 0x04005E92, typeof(Dictionary<UnitEntityData, TimeSpan>));
        private static readonly FieldInfo AiCooldowns = Field(typeof(CombatAiData), "m_ActionCooldowns", 0x0400150D, typeof(Dictionary<BlueprintAiAction, int>));
        private static readonly FieldInfo AiCounts = Field(typeof(CombatAiData), "m_ActionCounts", 0x0400150E, typeof(Dictionary<BlueprintAiAction, int>));

        internal static SavedNativeActor CaptureDebt(UnitEntityData unit)
        {
            var state = unit.CombatState;
            return new SavedNativeActor
            {
                Id = unit.UniqueId, Standard = state.Cooldown.StandardAction, Move = state.Cooldown.MoveAction,
                Swift = state.Cooldown.SwiftAction, Initiative = state.Cooldown.Initiative,
                Reaction = state.Cooldown.AttackOfOpportunity, ReactionsRemaining = state.AttackOfOpportunityCount,
                LastSurpriseTicks = state.LastSurpriseActionTime.Ticks
            };
        }

        internal static void RestoreDebt(UnitEntityData unit, SavedNativeActor saved)
        {
            var state = unit.CombatState;
            state.Cooldown.StandardAction = saved.Standard;
            state.Cooldown.MoveAction = saved.Move;
            state.Cooldown.SwiftAction = saved.Swift;
            state.Cooldown.Initiative = saved.Initiative;
            state.Cooldown.AttackOfOpportunity = saved.Reaction;
            state.AttackOfOpportunityCount = saved.ReactionsRemaining;
            state.LastSurpriseActionTime = TimeSpan.FromTicks(saved.LastSurpriseTicks);
        }

        internal static SavedCombatActor Capture(UnitEntityData unit)
        {
            var state = unit.CombatState;
            var cooldowns = (Dictionary<BlueprintAiAction, int>)AiCooldowns.GetValue(state.AIData);
            var counts = (Dictionary<BlueprintAiAction, int>)AiCounts.GetValue(state.AIData);
            var point = state.ReturnPosition;
            return new SavedCombatActor
            {
                Native = CaptureDebt(unit), InCombat = state.IsInCombat, Prepared = state.Prepared,
                HitThisRound = state.HitThisRound, InitiativeRoll = state.Initiative,
                CachedInitiative = Game.Instance.TurnBasedCombatController.GetCachedInitiative(unit),
                InitiativeRandom = state.InitiativeRandom, ExecutedAttacks = state.ExecutedAttackNumber,
                LastMoveTicks = ((TimeSpan)LastMove.GetValue(state)).Ticks,
                LastDeflectTicks = ((TimeSpan)LastDeflect.GetValue(state)).Ticks,
                StoryImmunity = (long)state.StoryModeBuffImmunity,
                EnergyDrainImmunity = state.StoryModeEnergyDrainImmuniy,
                PreventNextReaction = (bool)PreventNext.GetValue(state),
                ReturnPoint = point.HasValue ? new[] { point.Value.x, point.Value.y, point.Value.z } : null,
                ReturnYaw = state.ReturnOrientation,
                DisengageTargets = state.DisengageAttackTargets.Select(u => u.UniqueId).ToArray(),
                AiActions = cooldowns.Keys.Concat(counts.Keys).Distinct().Select(action => new SavedAiAction
                {
                    BlueprintId = action.AssetGuidThreadSafe,
                    Cooldown = cooldowns.TryGetValue(action, out var remaining) ? remaining : 0,
                    Count = counts.TryGetValue(action, out var count) ? count : 0
                }).ToArray(),
                // Native AI uses Unity Time.time, not the persistent game clock.
                // Save a duration so an old process deadline cannot stall the new one.
                AiDelayTicks = TimeSpan.FromSeconds(Math.Max(0f, state.AIData.NextCommandTime - Time.time)).Ticks
            };
        }

        internal static SavedEngagement[] CaptureEngagements(IEnumerable<UnitEntityData> units) =>
            units.SelectMany(unit => ((Dictionary<UnitEntityData, TimeSpan>)Engaged.GetValue(unit.CombatState))
                .Select(edge => new SavedEngagement { From = unit.UniqueId, To = edge.Key.UniqueId, SinceTicks = edge.Value.Ticks })).ToArray();

        internal static void RestoreActor(UnitEntityData unit, SavedCombatActor saved, long savedGameTicks)
        {
            var state = unit.CombatState;
            // Native JoinCombat binds group ownership and the command-event
            // subscription. It performs no turn preparation or round callbacks.
            if (saved.InCombat && !unit.Descriptor.State.IsFinallyDead) state.JoinCombat();
            RestoreDebt(unit, saved.Native);
            state.Prepared = saved.Prepared;
            state.HitThisRound = saved.HitThisRound;
            state.Initiative = saved.InitiativeRoll;
            state.InitiativeRandom = saved.InitiativeRandom;
            ExecutedAttacks.SetValue(state, saved.ExecutedAttacks);
            LastMove.SetValue(state, TimeSpan.FromTicks(saved.LastMoveTicks));
            LastDeflect.SetValue(state, TimeSpan.FromTicks(saved.LastDeflectTicks));
            long validDescriptors = 0;
            foreach (SpellDescriptor value in Enum.GetValues(typeof(SpellDescriptor))) validDescriptors |= (long)value;
            if ((saved.StoryImmunity & ~validDescriptors) != 0)
                throw new InvalidDataException("Saved native spell descriptor flags are not supported.");
            state.StoryModeBuffImmunity = (SpellDescriptor)saved.StoryImmunity;
            state.StoryModeEnergyDrainImmuniy = saved.EnergyDrainImmunity;
            PreventNext.SetValue(state, saved.PreventNextReaction);
            state.ReturnPosition = saved.ReturnPoint == null ? (Vector3?)null :
                new Vector3(saved.ReturnPoint[0], saved.ReturnPoint[1], saved.ReturnPoint[2]);
            state.ReturnOrientation = saved.ReturnYaw;
            var available = unit.Brain == null ? new Dictionary<string, BlueprintAiAction>(StringComparer.Ordinal) :
                unit.Brain.AvailableActions.Select(a => a.Blueprint).Distinct()
                    .ToDictionary(a => a.AssetGuidThreadSafe, StringComparer.Ordinal);
            var cooldowns = (Dictionary<BlueprintAiAction, int>)AiCooldowns.GetValue(state.AIData);
            var counts = (Dictionary<BlueprintAiAction, int>)AiCounts.GetValue(state.AIData);
            foreach (var action in saved.AiActions)
                if (!available.ContainsKey(action.BlueprintId))
                    throw new InvalidDataException("A saved AI action is absent from the loaded actor's native brain.");
            cooldowns.Clear(); counts.Clear();
            foreach (var action in saved.AiActions)
            {
                var blueprint = available[action.BlueprintId];
                cooldowns.Add(blueprint, action.Cooldown);
                counts.Add(blueprint, action.Count);
            }
            var elapsed = Math.Max(0L, Game.Instance.TimeController.GameTime.Ticks - savedGameTicks);
            var delay = Math.Max(0L, saved.AiDelayTicks - elapsed);
            state.AIData.NextCommandTime = delay == 0 ? 0f : Time.time + (float)(delay / (double)TimeSpan.TicksPerSecond);
        }

        internal static void RestoreReferences(SavedCombatData saved, IDictionary<string, UnitEntityData> actors)
        {
            foreach (var row in saved.Actors)
            {
                var state = actors[row.Native.Id].CombatState;
                state.DisengageAttackTargets.Clear();
                foreach (var id in row.DisengageTargets) state.DisengageAttackTargets.Add(actors[id]);
                ((Dictionary<UnitEntityData, TimeSpan>)Engaged.GetValue(state)).Clear();
                ((Dictionary<UnitEntityData, TimeSpan>)EngagedBy.GetValue(state)).Clear();
            }
            foreach (var edge in saved.Engagements)
            {
                var from = actors[edge.From]; var to = actors[edge.To]; var time = TimeSpan.FromTicks(edge.SinceTicks);
                ((Dictionary<UnitEntityData, TimeSpan>)Engaged.GetValue(from.CombatState)).Add(to, time);
                ((Dictionary<UnitEntityData, TimeSpan>)EngagedBy.GetValue(to.CombatState)).Add(from, time);
            }
        }

        internal static FieldInfo Field(Type owner, string name, int token, Type valueType)
        {
            var field = owner.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (owner.Assembly.ManifestModule.ModuleVersionId != new Guid("07fa1e4d-8618-41b3-9b8d-faa17d3b26f7") ||
                field == null || field.MetadataToken != token || field.FieldType != valueType)
                throw new MissingFieldException("Native persistence field differs: " + owner.FullName + "." + name);
            return field;
        }
    }
}
