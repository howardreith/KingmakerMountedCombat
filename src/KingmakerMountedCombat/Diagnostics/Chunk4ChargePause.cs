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
                ["requestFrame"] = requestFrame, ["inputFrame"] = Time.frameCount,
                ["beforePaused"] = Game.Instance.IsPaused,
                ["gameTimeBefore"] = Game.Instance.TimeController.GameTime.Ticks,
                ["actorsBefore"] = CaptureChunk4ChargePauseActors()
            };
        }

        private void ObserveChunk4ChargePausedInput(JObject proof)
        {
            proof["afterInputPaused"] = Game.Instance.IsPaused;
            proof["gameTimeAfterInput"] = Game.Instance.TimeController.GameTime.Ticks;
            proof["actorsAfterInput"] = CaptureChunk4ChargePauseActors();
            if (!(bool)proof["afterInputPaused"] || (long)proof["gameTimeBefore"] != (long)proof["gameTimeAfterInput"] ||
                !Chunk4ChargePauseResourcesEqual(proof["actorsBefore"], proof["actorsAfterInput"]))
                throw new InvalidOperationException("Native paused Charge input changed actor costs, positions or game time.");
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
