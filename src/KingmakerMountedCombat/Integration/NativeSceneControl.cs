using Kingmaker.AreaLogic.Cutscenes;
using Kingmaker.EntitySystem.Entities;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Integration
{
    // The one reading of native scene control of a unit, shared by fresh Mount/Dismount admission (Bald Hilltop lockout
    // repair) and scene-staging restoration (cutscene continuity repair). Installed 2.1.7b contracts:
    // UnitEntityData.CutceneControlledUnit (lazily created by CutsceneControlledUnit.MarkUnit, never cleared, not saved);
    // CutsceneControlledUnit.GetControllingPlayer (0x06009D6E) reads the active entry (m_Active 0x04006C29);
    // CutscenePlayerView.PlayerData (0x06009DAE); CutscenePlayerData.IsFinished (0x06009D84).
    internal static class NativeSceneControl
    {
        internal static bool IsUnderLiveSceneControl(UnitEntityData unit)
        {
            if (unit == null) return false;
            var holder = unit.CutceneControlledUnit;
            if (holder == null) return false;
            var controller = CutsceneControlledUnit.GetControllingPlayer(unit);
            // Unity null: a destroyed cutscene player view left behind by a missed release is not control.
            var alive = controller != null;
            var data = alive ? controller.PlayerData : null;
            return MountedSceneContinuityPolicy.ActorUnderLiveSceneControl(true, (object)controller != null, alive && data != null,
                data != null && data.IsFinished);
        }

        // Diagnostics only: the native facts behind the decision, without player-facing wording.
        internal static string Describe(UnitEntityData unit)
        {
            if (unit == null) return "none";
            var holder = unit.CutceneControlledUnit;
            if (holder == null) return unit.UniqueId + "{holder=none}";
            var controller = CutsceneControlledUnit.GetControllingPlayer(unit);
            var alive = controller != null;
            var data = alive ? controller.PlayerData : null;
            return unit.UniqueId + "{holder=present;activeEntry=" + ((object)controller != null) + ";playerAlive=" + alive +
                ";cutscene=" + (alive && controller.Cutscene != null ? controller.Cutscene.name : "none") +
                ";finished=" + (data != null && data.IsFinished) + ";paused=" + (data != null && data.Paused) +
                ";live=" + IsUnderLiveSceneControl(unit) + "}";
        }
    }
}
