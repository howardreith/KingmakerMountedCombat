using System;
using System.Reflection;
using Kingmaker;
using Kingmaker.UI.SettingsUI;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    // Disposable-fixture policy only. Never writes persisted settings or life state.
    internal sealed class NativeDeathPolicyLease : IDisposable
    {
        private readonly SettingsEntityBool rise;
        private readonly SettingsEntityBool door;
        private readonly FieldInfo cache;
        private readonly object riseRaw;
        private readonly object doorRaw;
        private readonly bool permanent;
        private readonly JObject before;
        private readonly JObject effective;
        private JObject restoration;

        internal NativeDeathPolicyLease(bool permanent)
        {
            this.permanent = permanent;
            var root = SettingsRoot.Instance;
            cache = typeof(SettingsEntityBool).GetField("m_Cached", BindingFlags.Instance | BindingFlags.NonPublic);
            if (root == null || cache?.MetadataToken != 0x04002275 || cache.FieldType != typeof(bool?) ||
                cache.Module.ModuleVersionId != new Guid("07fa1e4d-8618-41b3-9b8d-faa17d3b26f7") ||
                root.GetType().GetField("DeadCompanionsRiseAfterCombat")?.MetadataToken != 0x04007CB5 ||
                root.GetType().GetField("DeathDoor")?.MetadataToken != 0x04007CB4)
                throw new MissingMemberException("Exact native death difficulty setting contract changed.");
            rise = root.DeadCompanionsRiseAfterCombat; door = root.DeathDoor;
            riseRaw = cache.GetValue(rise); doorRaw = cache.GetValue(door);
            try
            {
                before = CaptureState();
                if (permanent) { cache.SetValue(rise, (bool?)false); cache.SetValue(door, (bool?)false); }
                effective = CaptureState();
                VerifyEffective();
            }
            catch { RestoreRaw(); throw; }
        }

        private JObject CaptureSetting(SettingsEntityBool setting) => new JObject {
            ["raw"] = cache.GetValue(setting) == null ? JValue.CreateNull() : new JValue((bool)cache.GetValue(setting)),
            ["value"] = setting.CurrentValue, ["persisted"] = setting.GetSavedValueString()
        };
        private JObject CaptureState() => new JObject {
            ["riseAfterCombat"] = CaptureSetting(rise), ["deathDoor"] = CaptureSetting(door),
            ["trueDeath"] = Game.Instance.Player.Difficulty.TrueDeath,
            ["deathDoorCondition"] = Game.Instance.Player.Difficulty.DeathDoorCondition,
            ["damageToParty"] = Game.Instance.Player.Difficulty.DamageToParty
        };
        internal JObject Capture() => new JObject {
            ["permanentDeathFixture"] = permanent, ["before"] = before.DeepClone(),
            ["effective"] = effective.DeepClone(), ["restoration"] = restoration?.DeepClone()
        };
        internal void VerifyEffective()
        {
            var difficulty = Game.Instance.Player.Difficulty;
            if (rise.GetSavedValueString() != (string)before["riseAfterCombat"]["persisted"] ||
                door.GetSavedValueString() != (string)before["deathDoor"]["persisted"] ||
                difficulty.DamageToParty != (float)before["damageToParty"] ||
                difficulty.TrueDeath != (permanent || (bool)before["trueDeath"]) ||
                difficulty.DeathDoorCondition != (!permanent && (bool)before["deathDoorCondition"]))
                throw new InvalidOperationException("Native death fixture policy changed unexpectedly or touched persisted settings.");
        }
        private void RestoreRaw() { cache.SetValue(rise, riseRaw); cache.SetValue(door, doorRaw); }
        public void Dispose()
        {
            if (restoration != null) return;
            JObject restored;
            RestoreRaw();
            try { restored = CaptureState(); }
            finally { RestoreRaw(); }
            var exact = JToken.DeepEquals(before, restored) && Equals(cache.GetValue(rise), riseRaw) && Equals(cache.GetValue(door), doorRaw);
            restoration = new JObject { ["restored"] = exact, ["state"] = restored };
            if (!exact) throw new InvalidOperationException("Native death fixture did not restore exact cached and persisted settings.");
        }
    }
}
