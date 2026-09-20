using System;
using System.Linq;
using Kingmaker;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        private int chunk4ChargePauseRequestFrame = -1;
        private JObject chunk4ChargePausedInput;
        private int chunk4RecoveryPauseRequestFrame = -1;
        private JObject chunk4RecoveryPausedInput;

        private bool AwaitChunk4ChargePause(ref int requestFrame)
        {
            if (requestFrame < 0)
            {
                requestFrame = Time.frameCount;
                Game.Instance.IsPaused = true;
                return false;
            }
            return Game.Instance.IsPaused && Time.frameCount > requestFrame;
        }

        private JObject CaptureChunk4ChargePauseActors() => new JObject {
            ["rider"] = CaptureOrdinaryActor(rider), ["mount"] = CaptureOrdinaryActor(horse),
            ["actor"] = CaptureOrdinaryActor(chunk4ChargeActor)
        };

        private JObject BeginChunk4ChargePausedInput(int requestFrame)
        {
            if (!Game.Instance.IsPaused || Time.frameCount <= requestFrame)
                throw new InvalidOperationException("Charge input preceded actual native Pause mode.");
            return new JObject {
                ["kind"] = "native-real-time-pause",
                ["requestFrame"] = requestFrame, ["inputFrame"] = Time.frameCount,
                ["beforePaused"] = Game.Instance.IsPaused,
                ["gameTimeBefore"] = Game.Instance.TimeController.GameTime.Ticks,
                ["actorsBefore"] = CaptureChunk4ChargePauseActors()
            };
        }

        private JObject BeginChunk4ChargeTbInput()
        {
            var game = Game.Instance;
            var turn = game.TurnBasedCombatController.CurrentTurn;
            var principal = chunk4ChargeActor == horse && relationship.State == Domain.RelationshipState.Mounted ? rider : chunk4ChargeActor;
            if (!TurnBased.Controllers.CombatController.IsInTurnBasedCombat() || game.IsPaused ||
                turn?.Unit != principal || turn.Status != TurnBased.Controllers.TurnController.TurnStatus.Preparing && !turn.IsActing)
                throw new InvalidOperationException("Charge input lost the native TB planning/acting principal.");
            // Native Game.DoStartMode rejects global Pause in TB. Observe real
            // synchronous inputs here; only RT claims a held paused frame.
            return new JObject {
                ["kind"] = "native-turn-based-input", ["inputFrame"] = Time.frameCount,
                ["beforePaused"] = game.IsPaused, ["turnBefore"] = turn.Unit.UniqueId,
                ["turnStatusBefore"] = turn.Status.ToString(),
                ["gameTimeBefore"] = game.TimeController.GameTime.Ticks,
                ["actorsBefore"] = CaptureChunk4ChargePauseActors()
            };
        }

        private void ObserveChunk4ChargePausedInput(JObject proof)
        {
            proof["afterInputFrame"] = Time.frameCount;
            proof["afterInputPaused"] = Game.Instance.IsPaused;
            proof["gameTimeAfterInput"] = Game.Instance.TimeController.GameTime.Ticks;
            proof["actorsAfterInput"] = CaptureChunk4ChargePauseActors();
            if (Chunk4ChargeTb)
            {
                proof["turnAfter"] = Game.Instance.TurnBasedCombatController.CurrentTurn?.Unit?.UniqueId;
                proof["turnStatusAfter"] = Game.Instance.TurnBasedCombatController.CurrentTurn?.Status.ToString();
                if (!JToken.DeepEquals(proof["turnBefore"], proof["turnAfter"]))
                    throw new InvalidOperationException("Synchronous TB Charge input changed the native turn principal.");
            }
            if ((bool)proof["afterInputPaused"] != !Chunk4ChargeTb ||
                (int)proof["inputFrame"] != (int)proof["afterInputFrame"] ||
                (long)proof["gameTimeBefore"] != (long)proof["gameTimeAfterInput"] ||
                !Chunk4ChargePauseResourcesEqual(proof["actorsBefore"], proof["actorsAfterInput"]))
                throw new InvalidOperationException("Native Charge input changed actor costs, positions, mode or game time.");
        }

        private bool FinishChunk4ChargePausedHold(JObject proof)
        {
            if (Time.frameCount <= (int)proof["inputFrame"]) return false;
            proof["heldFrame"] = Time.frameCount;
            proof["heldPaused"] = Game.Instance.IsPaused;
            proof["gameTimeAfterHold"] = Game.Instance.TimeController.GameTime.Ticks;
            proof["actorsAfterHold"] = CaptureChunk4ChargePauseActors();
            if (!(bool)proof["heldPaused"] || (long)proof["gameTimeBefore"] != (long)proof["gameTimeAfterHold"] ||
                !Chunk4ChargePauseResourcesEqual(proof["actorsBefore"], proof["actorsAfterHold"]))
                throw new InvalidOperationException("Charge input spent or moved an actor while native Pause remained active.");
            return true;
        }

        private static bool Chunk4ChargePauseResourcesEqual(JToken before, JToken after) =>
            new[] { "rider", "mount", "actor" }.All(actor => new[] { "id", "standard", "move", "swift", "position" }
                .All(field => JToken.DeepEquals(before[actor][field], after[actor][field])));
    }
}
