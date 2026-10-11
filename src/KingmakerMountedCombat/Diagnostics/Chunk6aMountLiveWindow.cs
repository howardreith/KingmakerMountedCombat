using System.Runtime.CompilerServices;
using Kingmaker;
using Kingmaker.UnitLogic.Commands;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // Per-frame observation of the rider's native movement flags while the exact combat Mount
    // shell is live (admitted and not yet finished). Recorded for the retained
    // CM03-rider-other-action analysis, which needs the frame at which the rider's Move
    // availability changed relative to the shell's approach. Observation only: nothing here
    // reads a value in order to act on it.
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        private const int Chunk6aMountLiveSampleLimit = 1200;
        private readonly JArray chunk6aMountLiveSamples = new JArray();
        private JObject chunk6aMountLiveWindow;

        private void SampleChunk6aMountLiveWindow(UnitUseAbility shell, TurnController turn)
        {
            if (chunk6aMountLiveWindow == null)
            {
                chunk6aMountLiveWindow = new JObject
                {
                    ["contract"] = "per-frame-rider-move-flags-while-the-exact-mount-shell-is-live",
                    ["limit"] = Chunk6aMountLiveSampleLimit,
                    ["truncated"] = false,
                    ["samples"] = chunk6aMountLiveSamples
                };
                observations["chunk6aMountLiveWindow"] = chunk6aMountLiveWindow;
            }
            if (chunk6aMountLiveSamples.Count >= Chunk6aMountLiveSampleLimit)
            {
                chunk6aMountLiveWindow["truncated"] = true;
                return;
            }
            var riderTurn = turn?.Unit == rider ? turn : null;
            var cooldown = rider.CombatState?.Cooldown;
            chunk6aMountLiveSamples.Add(new JObject
            {
                ["frame"] = Time.frameCount,
                ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks,
                ["commandObject"] = RuntimeHelpers.GetHashCode(shell),
                ["started"] = shell.IsStarted,
                ["acted"] = shell.IsActed,
                ["finished"] = shell.IsFinished,
                ["result"] = shell.Result.ToString(),
                ["timeSinceStart"] = shell.TimeSinceStart,
                ["executionProcess"] = shell.ExecutionProcess != null,
                ["unitEnoughClose"] = shell.IsUnitEnoughClose,
                ["riderReallyMoving"] = rider.View?.AgentASP?.IsReallyMoving,
                ["riderHasMove"] = rider.HasMoveAction(),
                ["riderMoveRestricted"] = rider.IsMoveActionRestricted(),
                ["riderUsedStandard"] = rider.UsedStandardAction(),
                ["riderHasStandard"] = rider.HasStandardAction(),
                ["riderStandard"] = cooldown?.StandardAction,
                ["riderMove"] = cooldown?.MoveAction,
                ["turnActor"] = turn?.Unit?.UniqueId,
                ["turnStatus"] = turn?.Status.ToString(),
                ["movementLimit"] = riderTurn?.CurrentMovementLimit.ToString(),
                ["singleActionMove"] = riderTurn?.EnabledSingleActionMove,
                ["fiveFootStep"] = riderTurn?.EnabledFiveFootStep,
                ["remainingNativeTime"] = riderTurn?.GetRemainingTime(),
                ["timeMoved"] = riderTurn?.TimeMoved,
                ["pairDistance"] = rider.DistanceTo(horse),
                ["shellState"] = nativeControls.DescribeRelationshipShellState(rider),
                ["feedback"] = playerAction.LastFeedback
            });
        }
    }
}
