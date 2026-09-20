using System;
using System.Reflection;
using Kingmaker;
using Kingmaker.RuleSystem.Rules;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    // Observe the native target-independent result separately from engagement,
    // concealment and the installed difficulty's minimum attack-bonus rule.
    internal static class NativeAttackBonusEvidence
    {
        private static readonly FieldInfo InnerRule = typeof(RuleCalculateAttackBonus).GetField(
            "m_InnerRule", BindingFlags.Instance | BindingFlags.NonPublic);

        internal static JObject Capture(RuleAttackRoll roll)
        {
            if (roll.AttackBonusRule == null) return null;
            if (InnerRule?.MetadataToken != 0x04004AC0 ||
                InnerRule.FieldType != typeof(RuleCalculateAttackBonusWithoutTarget))
                throw new MissingFieldException("Exact native attack-bonus observation contract changed.");
            var inner = (RuleCalculateAttackBonusWithoutTarget)InnerRule.GetValue(roll.AttackBonusRule);
            return new JObject {
                ["innerResult"] = inner.Result, ["result"] = roll.AttackBonusRule.Result,
                ["concealment"] = roll.AttackBonusRule.ConcealmentBonus,
                ["flanking"] = roll.AttackBonusRule.FlankingBonus,
                ["shootIntoCombat"] = roll.AttackBonusRule.ShootIntoCombatBonus,
                ["bab"] = roll.Initiator.Stats.BaseAttackBonus.ModifiedValue,
                ["playerFaction"] = roll.Initiator.IsPlayerFaction,
                ["trueDeath"] = Game.Instance.Player.Difficulty.TrueDeath
            };
        }
    }
}
