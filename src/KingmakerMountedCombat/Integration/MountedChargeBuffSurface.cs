using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;

namespace KingmakerMountedCombat.Integration
{
    // A bounded interoperability input contract, not a replacement Charge buff.
    // Keep the actual blueprint/actions. Unknown lifetime mutations fail closed.
    // The external evidence reader remains the gameplay acceptance authority.
    internal sealed class MountedChargeBuffSurface
    {
        internal const string RootGuid = "f36da144a379d534cad8e21667079066";
        internal const string TossGuid = "6683a35444eb42ddbd21f87c3441a50a";
        internal const string HellfireGuid = "b0439659723f4a8da680965c78a8fbf5";
        internal const string FrightfulGuid = "61aff33f69d84391b49782fb976cf870";
        internal const string FlamingGuid = "30f90becaaac51f41bf56641966c4121";
        internal const string BurstGuid = "3f032a3cd54e57649a0cdad0434bf221";
        private const string BloodrageGuid = "30e6b21b6baa49669b33adf46806fdfa";
        private const string CotwMvid = "8caab254-aacf-4811-8093-44b9184e6e53";
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        internal string Id { get; private set; }
        internal BlueprintBuff Root { get; private set; }
        internal BlueprintBuff[] Children { get; private set; } = new BlueprintBuff[0];
        internal BlueprintItemEnchantment[] Enchantments { get; private set; } = new BlueprintItemEnchantment[0];
        internal ContextActionRemoveBuff[] RemovalActions { get; private set; } = new ContextActionRemoveBuff[0];

        internal static MountedChargeBuffSurface Read(BlueprintBuff root)
        {
            Require(!ReferenceEquals(root, null) && root.AssetGuid == RootGuid, "root identity");
            var components = root.ComponentsArray;
            Require(components != null && (components.Length == 3 || components.Length == 6), "root component inventory");
            var augmented = components.Length == 6;
            var offset = augmented ? 1 : 0;
            RequireBase(components.Skip(offset).Take(3).ToArray());
            var result = new MountedChargeBuffSurface { Root = root, Id = augmented ? "native-cotw-1.14.4c-2.1" : "native-base" };
            if (!augmented) return result;

            var actions = Exact<AddFactContextActions>(components[0]);
            var bonus = Exact<AddContextStatBonus>(components[4]);
            Require(bonus.Stat == StatType.AC && (int)bonus.Descriptor == 25 && bonus.Multiplier == 1 &&
                Value(bonus.Value, 1, 0, 0), "context armor modifier");
            Rank(Exact<ContextRankConfig>(components[5]), 12, 9, 0, false, 0, true, 2, 3, 4,
                "48ac8db94d5de7645906c7d0ad3bcfbd", "4a76470cab5144159e37f55b25b074d2");
            Require(Actions(actions.Activated).Length == 3 && Actions(actions.Deactivated).Length == 2 &&
                Actions(actions.NewRound).Length == 0, "root action counts");
            var applied = new[]
            {
                Activation(actions.Activated.Actions[0], TossGuid, "4f8d33348b184125a8b81363232535c0"),
                Activation(actions.Activated.Actions[1], HellfireGuid, "d26ca0ac64874157aad34ef664b116a9", BloodrageGuid),
                Activation(actions.Activated.Actions[2], FrightfulGuid, "2ba88b87439e456cb382392ba07ffa96", BloodrageGuid)
            };
            result.Children = applied.Select(action => action.Buff).ToArray();
            foreach (var child in result.Children)
                Require((int)child.Stacking == 0, "child must retain native Replace stacking");
            result.RemovalActions = actions.Deactivated.Actions.Select(Exact<ContextActionRemoveBuff>).ToArray();
            for (var i = 0; i < 2; i++)
                Require(!result.RemovalActions[i].ToCaster && ReferenceEquals(result.RemovalActions[i].Buff, result.Children[i + 1]),
                    "exact root deactivation target");
            ValidateToss(result.Children[0]);
            result.Enchantments = ValidateHellfire(result.Children[1]);
            ValidateFrightful(result.Children[2]);
            return result;
        }

        private static void RequireBase(BlueprintComponent[] components)
        {
            var armor = Exact<AddStatBonus>(components[0]);
            var condition = Exact<AddCondition>(components[1]);
            var attack = Exact<AttackOfOpportunityAttackBonus>(components[2]);
            Require((int)armor.Descriptor == 0 && armor.Stat == StatType.AC && armor.Value == -2 && !armor.ScaleByBasicAttackBonus,
                "native armor modifier");
            Require(condition.Condition == UnitCondition.StealthForbidden, "native condition");
            Require(attack.NotAttackOfOpportunity && attack.AttackBonus == 1 && (int)attack.Descriptor == 0 &&
                Value(attack.Value, 0, 2, 0), "native attack modifier");
        }

        private static ContextActionApplyBuff Activation(GameAction action, string childGuid, params string[] facts)
        {
            var conditional = Exact<Conditional>(action);
            var checker = conditional.ConditionsChecker;
            Require(checker != null && (int)checker.Operation == 0 && checker.Conditions != null &&
                checker.Conditions.Length == facts.Length && Actions(conditional.IfFalse).Length == 0 &&
                Actions(conditional.IfTrue).Length == 1, "activation conditional");
            for (var i = 0; i < facts.Length; i++)
            {
                var condition = Exact<ContextConditionHasFact>(checker.Conditions[i]);
                Require(!condition.Not && GuidOf(condition.Fact) == facts[i], "activation fact identity");
            }
            var apply = Exact<ContextActionApplyBuff>(conditional.IfTrue.Actions[0]);
            Apply(apply, childGuid, true, 0);
            return apply;
        }

        private static void Apply(ContextActionApplyBuff action, string guid, bool permanent, int rank)
        {
            Require(GuidOf(action.Buff) == guid && action.AsChild && action.Permanent == permanent && !action.IsFromSpell &&
                action.IsNotDispelable && !action.ToCaster && !action.UseDurationSeconds && action.DurationSeconds == 0f,
                "buff action identity and ownership flags");
            var duration = action.DurationValue;
            Require(duration != null && (int)duration.Rate == 0 && (int)duration.DiceType == 0 &&
                Value(duration.DiceCountValue, 0, 0, 0) && Value(duration.BonusValue, 1, 0, rank), "buff duration input");
        }

        private static AddInitiatorAttackWithWeaponTrigger Trigger(BlueprintComponent component, bool melee)
        {
            var trigger = Exact<AddInitiatorAttackWithWeaponTrigger>(component);
            Require(trigger.OnlyHit && trigger.CheckWeaponRangeType == melee && (int)trigger.RangeType == 0 &&
                !trigger.WaitForAttackResolve && !trigger.OnlyOnFullAttack && !trigger.OnlyOnFirstAttack &&
                !trigger.OnlyOnFirstHit && !trigger.CriticalHit && !trigger.OnlySneakAttack &&
                ReferenceEquals(trigger.WeaponType, null) && !trigger.CheckWeaponCategory && !trigger.ActionsOnInitiator &&
                !trigger.ReduceHPToZero && !trigger.CheckDistance && !trigger.AllNaturalAndUnarmed && !trigger.DuelistWeapon &&
                Actions(trigger.Action).Length == 1, "native target-only attack listener");
            return trigger;
        }

        private static void ValidateToss(BlueprintBuff buff)
        {
            Require(buff.ComponentsArray.Length == 1, "Toss component inventory");
            var trigger = Trigger(buff.ComponentsArray[0], true);
            var trip = Exact<ContextActionCombatManeuver>(trigger.Action.Actions[0]);
            Require((int)trip.Type == 1 && !trip.IgnoreConcealment && !trip.ReplaceStat && !trip.UseKineticistMainStat &&
                !trip.UseCastingStat && !trip.UseCasterLevelAsBaseAttack && !trip.UseBestMentalStat && !trip.BatteringBlast &&
                Actions(trip.OnSuccess).Length == 1, "Toss native consequence graph");
            // Native damage is an irreversible attack consequence; it owns no charge
            // lifetime fact. Keep all native damage settings rather than recreate them.
            Exact<ContextActionDealDamage>(trip.OnSuccess.Actions[0]);
        }

        private static BlueprintItemEnchantment[] ValidateHellfire(BlueprintBuff buff)
        {
            Require(buff.ComponentsArray.Length == 2, "Hellfire component inventory");
            var result = new List<BlueprintItemEnchantment>();
            var expected = new[] { FlamingGuid, BurstGuid };
            for (var i = 0; i < 2; i++)
            {
                var component = buff.ComponentsArray[i];
                Require(!ReferenceEquals(component, null) && component.GetType().Assembly.ManifestModule.ModuleVersionId.ToString() == CotwMvid,
                    "pinned optional COTW assembly");
                Require(component.GetType() == component.GetType().Module.ResolveMethod(0x060014FD).DeclaringType,
                    "exact optional weapon enchantment component");
                Require(!(bool)Field(component, "only_non_magical") && !(bool)Field(component, "lock_slot") &&
                    !(bool)Field(component, "in_off_hand") && Empty(Field(component, "allowed_types")) &&
                    Value((ContextValue)Field(component, "value"), 0, 1, 0), "weapon enchantment configuration");
                var items = ((IEnumerable)Field(component, "enchantments")).Cast<BlueprintItemEnchantment>().ToArray();
                Require(items.Length == 1 && GuidOf(items[0]) == expected[i], "weapon enchantment identity");
                var nativeComponent = typeof(BlueprintBuff).Module.ResolveMethod(i == 0 ? 0x0600895E : 0x0600895B).DeclaringType;
                Require(items[0].ComponentsArray.Length == 1 && !ReferenceEquals(items[0].ComponentsArray[0], null) &&
                    items[0].ComponentsArray[0].GetType() == nativeComponent, "exact native enchantment listener inventory");
                var listener = items[0].ComponentsArray[0];
                Require(Convert.ToInt32(Field(listener, "Element")) == 0, "native fire consequence");
                if (i == 0)
                {
                    var dice = Field(listener, "EnergyDamageDice");
                    Require((int)Field(dice, "m_Rolls") == 1 && Field(dice, "m_Dice").ToString() == "D6", "native Flaming dice");
                }
                else Require(Field(listener, "Dice").ToString() == "D10", "native FlamingBurst dice");
                result.Add(items[0]);
            }
            return result.ToArray();
        }

        private static void ValidateFrightful(BlueprintBuff buff)
        {
            Require(buff.ComponentsArray.Length == 2, "Frightful component inventory");
            var trigger = Trigger(buff.ComponentsArray[0], false);
            Apply(Exact<ContextActionApplyBuff>(trigger.Action.Actions[0]), "25ec6cb6ab1845c48a95f9c20b034220", false, 2);
            Rank(Exact<ContextRankConfig>(buff.ComponentsArray[1]), 1, 1, 2, true, 1, false, 0, 0, 0,
                "cf217eb4f8504d67aad37464cee966f8", null);
        }

        private static void Rank(ContextRankConfig rank, int source, int progression, int type, bool useMin, int min,
            bool useMax, int max, int start, int step, string classGuid, string archetype)
        {
            Require(Convert.ToInt32(Field(rank, "m_Type")) == type && Convert.ToInt32(Field(rank, "m_BaseValueType")) == source &&
                Convert.ToInt32(Field(rank, "m_Progression")) == progression && (int)Field(rank, "m_StartLevel") == start &&
                (int)Field(rank, "m_StepLevel") == step && (bool)Field(rank, "m_UseMin") == useMin &&
                (!useMin || (int)Field(rank, "m_Min") == min) && (bool)Field(rank, "m_UseMax") == useMax &&
                (!useMax || (int)Field(rank, "m_Max") == max) && !(bool)Field(rank, "m_ExceptClasses") &&
                Convert.ToInt32(Field(rank, "m_Stat")) == 0 && Field(rank, "m_Feature") == null &&
                Field(rank, "m_CustomProperty") == null && Empty(Field(rank, "m_FeatureList")) &&
                Empty(Field(rank, "m_CustomProgression")), "context rank metadata");
            var classes = ((IEnumerable)Field(rank, "m_Class")).Cast<BlueprintScriptableObject>().ToArray();
            Require(classes.Length == 1 && GuidOf(classes[0]) == classGuid && GuidOf((BlueprintScriptableObject)Field(rank, "Archetype")) == archetype,
                "context rank class/archetype identity");
        }

        private static bool Value(ContextValue value, int type, int number, int rank) => value != null &&
            (int)value.ValueType == type && value.Value == number && (int)value.ValueRank == rank &&
            (int)value.ValueShared == 0 && (int)value.Property == 0 && ReferenceEquals(value.CustomProperty, null);
        private static GameAction[] Actions(ActionList list) => list?.Actions ?? new GameAction[0];
        private static bool Empty(object value) => value == null || value is IEnumerable items && !items.Cast<object>().Any();
        private static string GuidOf(BlueprintScriptableObject blueprint) => ReferenceEquals(blueprint, null) ? null : blueprint.AssetGuid;
        private static object Field(object value, string name)
        {
            var field = value.GetType().GetField(name, Fields);
            if (field == null) throw new MissingFieldException(value.GetType().FullName, name);
            return field.GetValue(value);
        }
        private static T Exact<T>(object value) where T : class
        {
            Require(!ReferenceEquals(value, null) && value.GetType() == typeof(T), "exact " + typeof(T).Name);
            return (T)value;
        }
        private static void Require(bool condition, string detail)
        {
            if (!condition) throw new InvalidOperationException("Mounted Charge buff surface differs: " + detail + ".");
        }
    }
}
