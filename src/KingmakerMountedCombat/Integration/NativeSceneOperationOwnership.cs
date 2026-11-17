using System.Collections.Generic;
using System.Reflection;
using Kingmaker.AreaLogic.Cutscenes;
using Kingmaker.ElementsSystem;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Integration
{
    // Review R10 of 813d00a1: whether the exact native scene placement operation being entered is owned by a scene, read
    // from installed native state at the operation's entry.
    // - CommandMoveUnit.IsFinished and CommandTeleportParty.OnRun receive the CutscenePlayerData that runs them. Natively
    //   they are invoked only by that player's tracks (CommandBase.Run from TrackData.StartCommand and
    //   CutscenePlayerData.Restore; IsFinished from TrackData.Tick, StartCommand and ForceStop), and only for the current
    //   command of a started, unfinished track of one of the player's activated gates (m_ActivatedGates). That is the
    //   ownership asked here: a player that is not finished, with this exact command current on such a track.
    // - TranslocateUnit (an action) receives no player. Every scope of CutsceneParametersContext.ContextData is opened by a
    //   CutscenePlayerData (Start, TickScene, the Paused setter, Restore, Stop, Dispose), and TickScene names itself in it
    //   (Parameters.Cutscene). An action run inside such a scope belongs to that cutscene, unless the scope names a
    //   finished player.
    internal static class NativeSceneOperationOwnership
    {
        private static readonly FieldInfo ActivatedGates =
            typeof(CutscenePlayerData).GetField("m_ActivatedGates", BindingFlags.Instance | BindingFlags.NonPublic);

        internal static bool CommandIsCurrentOfStartedTrack(CommandBase command, CutscenePlayerData player)
        {
            if ((object)command == null || player == null || ActivatedGates == null) return false;
            var gates = ActivatedGates.GetValue(player) as List<CutscenePlayerData.GateData>;
            if (gates == null) return false;
            foreach (var gate in gates)
            {
                var tracks = gate?.StartedTracks;
                if (tracks == null) continue;
                foreach (var track in tracks)
                    if (track != null && !track.IsFinished && ReferenceEquals(track.Command, command)) return true;
            }
            return false;
        }

        internal static bool CommandOwnedByLivePlayer(CommandBase command, CutscenePlayerData player) =>
            MountedSceneOperationOwnership.CommandOwnedByLivePlayer(player != null, player != null && player.IsFinished,
                CommandIsCurrentOfStartedTrack(command, player));

        internal static bool ActionInCutsceneScope()
        {
            var scope = ElementsContext.GetData<CutsceneParametersContext.ContextData>();
            var player = scope?.Context?.Cutscene;
            return MountedSceneOperationOwnership.ActionInCutsceneScope(scope != null, player != null, player != null && player.IsFinished);
        }

        internal static MountedSceneOwnership Decide(MountedScenePlacementSource source, bool foregroundScene, CommandBase command,
            CutscenePlayerData player) =>
            MountedSceneOperationOwnership.Decide(source, foregroundScene,
                source != MountedScenePlacementSource.TranslocateUnit && CommandOwnedByLivePlayer(command, player),
                source == MountedScenePlacementSource.TranslocateUnit && ActionInCutsceneScope());
    }
}
