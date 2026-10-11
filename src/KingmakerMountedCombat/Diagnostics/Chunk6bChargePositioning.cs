using System;
using System.Linq;
using Kingmaker;
using Kingmaker.Controllers.Clicks.Handlers;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using KingmakerMountedCombat.Domain;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        private Vector3? chargeFixtureOrigin;
        private int chargePositioningCase = -1;
        private UnitMoveTo chargePositioningCommand;
        private NativeActorAllocationTrace chargePositioningTrace;
        private NativeOutsideCombatGroundProbe chargePositioningResources;
        private NativeCommandPathProbe chargePositioningPath;
        private JObject chargePositioningEvidence;

        private JObject ChargePositioningState() => new JObject
        {
            ["riderId"] = rider.UniqueId, ["mountId"] = horse.UniqueId,
            ["mountPosition"] = CapturePosition(horse.Position),
            ["frame"] = Time.frameCount, ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks,
            ["generation"] = relationship.MountedPairGeneration, ["relationship"] = relationship.State.ToString(),
            ["riderCommandsEmpty"] = rider.Commands.Empty, ["mountCommandsEmpty"] = horse.Commands.Empty,
            ["inCombat"] = rider.IsInCombat || horse.IsInCombat || Game.Instance.Player.IsInCombat,
            ["nativeTurnBased"] = TurnBased.Controllers.CombatController.IsInTurnBasedCombat(),
            ["controllerInitialized"] = Game.Instance.TurnBasedCombatController.Initialized,
            ["chargeOwned"] = combat.HasChargeOwnership, ["commandActive"] = combat.HasActiveCommand,
            ["mountMoving"] = horse.View.AgentASP.IsReallyMoving, ["mountPathPresent"] = horse.View.AgentASP.Path != null,
            ["mountCharging"] = horse.View.AgentASP.IsCharging, ["speedOverride"] = horse.View.AgentASP.MaxSpeedOverride
        };

        private bool TickChargeFixturePositioning()
        {
            if (Chunk6bChargeTb || chargePositioningCase == chunk6bChargeCase) return true;
            var game = Game.Instance;
            if (rider.IsInCombat || horse.IsInCombat || game.Player.IsInCombat ||
                TurnBased.Controllers.CombatController.IsInTurnBasedCombat() || game.TurnBasedCombatController.Initialized ||
                combat.HasChargeOwnership || combat.HasActiveCommand) return false;
            if (chargePositioningCommand != null)
            {
                if (!chargePositioningCommand.IsFinished || !rider.Commands.Empty || !horse.Commands.Empty ||
                    horse.View.AgentASP.IsReallyMoving) return false;
                chargePositioningEvidence["after"] = ChargePositioningState();
                chargePositioningEvidence["terminalCommand"] = CaptureOrdinaryCommand(chargePositioningCommand);
                chargePositioningEvidence["createdByPlayer"] = chargePositioningCommand.CreatedByPlayer;
                chargePositioningEvidence["resourceWindow"] = chargePositioningResources.Capture();
                chargePositioningEvidence["path"] = chargePositioningPath.Capture();
                // The external reader owns resource/path acceptance. The fixture
                // cannot proceed to another row after a failed native arrival.
                if (chargePositioningCommand.Result != UnitCommand.ResultType.Success ||
                    HorizontalDistance(horse.Position, chargeFixtureOrigin.Value) > MountedCombatSpatialPolicy.DiagnosticPlacementTolerance)
                    throw new InvalidOperationException("Charge fixture native return did not reach its original staging point.");
                DisposeChargePositioningObservers();
                chargePositioningCommand = null;
                chargePositioningCase = chunk6bChargeCase;
                ResetLeafClock();
                return true;
            }
            if (horse.View.AgentASP.IsReallyMoving || horse.View.AgentASP.Path != null ||
                !rider.Commands.Empty || !horse.Commands.Empty) return false;
            if (!chargeFixtureOrigin.HasValue)
            {
                chargeFixtureOrigin = horse.Position;
                Chunk6bChargeMeasurement["fixtureOrigin"] = CapturePosition(chargeFixtureOrigin.Value);
            }
            chargePositioningEvidence = new JObject
            {
                ["contract"] = "native-return-to-original-charge-fixture-origin",
                ["case"] = Chunk6bChargeCaseId, ["destination"] = CapturePosition(chargeFixtureOrigin.Value),
                ["before"] = ChargePositioningState(), ["moved"] = false
            };
            Chunk6bChargeMeasurement["positioning-" + Chunk6bChargeCaseId] = chargePositioningEvidence;
            if (HorizontalDistance(horse.Position, chargeFixtureOrigin.Value) <= MountedCombatSpatialPolicy.DiagnosticPlacementTolerance)
            {
                chargePositioningEvidence["after"] = ChargePositioningState();
                chargePositioningCase = chunk6bChargeCase;
                return true;
            }
            if (allocationTrace != null)
                throw new InvalidOperationException("Previous charge row retained its allocation observer during native fixture return.");
            SelectionManager.Instance.SelectUnit(rider.View, true, true, false);
            var selected = SelectionManager.Instance.SelectedUnits;
            chargePositioningEvidence["selectedIds"] = new JArray(selected.Select(unit => unit.UniqueId));
            if (selected.Count != 1 || selected[0] != rider)
                throw new InvalidOperationException("Charge fixture return could not select its exact rider.");
            // This new observer counts grants in this window only; no native
            // preparation, cooldown, turn or resource is changed or reset.
            chargePositioningTrace = new NativeActorAllocationTrace(rider, horse, combat);
            chargePositioningResources = new NativeOutsideCombatGroundProbe(chargePositioningTrace, rider, horse, horse);
            chargePositioningPath = new NativeCommandPathProbe(horse);
            chargePositioningEvidence["moved"] = true;
            ClickGroundHandler.MoveSelectedUnitsToPoint(chargeFixtureOrigin.Value, false);
            chargePositioningCommand = horse.Commands.Move as UnitMoveTo;
            chargePositioningResources.Bind(chargePositioningCommand);
            chargePositioningPath.Bind(chargePositioningCommand);
            chargePositioningEvidence["admittedCommand"] = CaptureOrdinaryCommand(chargePositioningCommand);
            return false;
        }

        private void RetireChargeRowAllocationTrace(string caseId)
        {
            if (Chunk6bChargeTb || allocationTrace == null) return;
            Chunk6bChargeMeasurement["allocationTrace-" + caseId] = allocationTrace.Capture();
            allocationTrace.Dispose(); allocationTrace = null;
        }

        private void DisposeChargePositioningObservers()
        {
            try { chargePositioningPath?.Dispose(); }
            finally
            {
                chargePositioningPath = null;
                try { chargePositioningResources?.Dispose(); }
                finally
                {
                    chargePositioningResources = null;
                    chargePositioningTrace?.Dispose(); chargePositioningTrace = null;
                }
            }
        }

        private void CleanupChargeFixturePositioning()
        {
            try
            {
                if (chargePositioningPath != null) chargePositioningEvidence["abortedPath"] = chargePositioningPath.Capture();
                if (chargePositioningResources != null) chargePositioningEvidence["abortedResources"] = chargePositioningResources.Capture();
            }
            finally
            {
                try { chargePositioningCommand?.Interrupt(); }
                finally { DisposeChargePositioningObservers(); }
            }
        }
    }
}
