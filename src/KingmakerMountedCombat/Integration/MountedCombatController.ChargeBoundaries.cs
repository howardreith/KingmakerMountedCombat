using System;
using System.Reflection;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.UI.SettingsUI;
using KingmakerMountedCombat.Domain;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;

namespace KingmakerMountedCombat.Integration
{
    internal sealed partial class MountedCombatController
    {
        private static readonly FieldInfo NativeModeEnabled = NativeCombatActorPersistence.Field(
            typeof(CombatController), "m_Enabled", 0x04000649, typeof(bool));
        // Only the world's primary combat adapter owns the native replay queue.
        // Other pair controllers delegate before touching permits or queue state.
        private MountedChargeBoundaryQueue chargeBoundaries;
        private MountedCombatController BoundaryCoordinator => relationship?.World?.Primary.Combat ?? this;
        private MountedChargeBoundaryQueue ChargeBoundaries => chargeBoundaries ?? (chargeBoundaries = new MountedChargeBoundaryQueue());
        private bool DrainBoundaryOwners(string reason) => relationship?.World?.DrainChargeOwnership(reason) ?? TryDrainChargeOwnership(reason);
        private bool replayingChargeBoundary;
        private CombatController chargeBoundaryPermitController;
        private MountedChargeBoundaryKind chargeBoundaryPermitKind;
        private bool chargeBoundaryPermitValue, chargeBoundaryPermit;
        private UnitEntityData chargeRemovalPermit;
        internal Func<bool> ChargeArchiveWorkerRunning { private get; set; }
        private bool ChargeSerializationBusy => relationship?.World?.SerializationBusy ??
            (relationship?.SaveSerializationSuspended == true || ChargeArchiveWorkerRunning?.Invoke() == true);
        internal bool ChargeNativeBoundaryPending => ReferenceEquals(BoundaryCoordinator, this) ? chargeBoundaries?.Pending == true :
            BoundaryCoordinator.ChargeNativeBoundaryPending;
        private static bool ChargeNativeWorldLoading => LoadingProcess.Instance.IsLoadingInProcess || LoadingProcess.Instance.QueuedNames.Any();
        internal event Action<bool> ChargeNativeModeResumed;
        internal void CompleteChargeNativeMode(bool enabled)
        {
            if (!ReferenceEquals(BoundaryCoordinator, this)) { BoundaryCoordinator.CompleteChargeNativeMode(enabled); return; }
            ChargeNativeModeResumed?.Invoke(enabled);
        }

        // Native Activate refreshes the current setting through the same handler
        // as a real mode change. Enable/Disable return immediately when this
        // exact native field already matches; that refresh owns no transition.
        internal static bool NativeModeChanges(CombatController controller, bool enabled) =>
            (bool)NativeModeEnabled.GetValue(controller) != enabled;

        internal bool AdmitChargeNativeBoundary(CombatController controller, MountedChargeBoundaryKind kind, bool value)
        {
            if (!ReferenceEquals(BoundaryCoordinator, this)) return BoundaryCoordinator.AdmitChargeNativeBoundary(controller, kind, value);
            if (chargeBoundaryPermit && ReferenceEquals(controller, chargeBoundaryPermitController) &&
                kind == chargeBoundaryPermitKind && value == chargeBoundaryPermitValue)
            { chargeBoundaryPermit = false; return true; }
            if (!ChargeSerializationBusy && !ChargeNativeBoundaryPending && DrainBoundaryOwners("native-" + kind)) return true;
            var world = Game.Instance.Player;
            ChargeBoundaries.Enqueue(kind, controller, () => ReferenceEquals(world, Game.Instance?.Player) &&
                ReferenceEquals(controller, Game.Instance.TurnBasedCombatController) &&
                (kind == MountedChargeBoundaryKind.Mode ? SettingsRoot.Instance.EnableTurnBasedMode.CurrentValue == value :
                 kind != MountedChargeBoundaryKind.PartyCombat || world.IsInCombat == value), () =>
            {
                replayingChargeBoundary = true;
                chargeBoundaryPermitController = controller; chargeBoundaryPermitKind = kind;
                chargeBoundaryPermitValue = value; chargeBoundaryPermit = true;
                try
                {
                    if (kind == MountedChargeBoundaryKind.Mode)
                    {
                        controller.HandleTurnBasedModeStateChanged(value);
                    }
                    else controller.HandlePartyCombatStateChanged(value);
                }
                finally { replayingChargeBoundary = false; chargeBoundaryPermit = false; chargeBoundaryPermitController = null; }
            });
            return false;
        }

        internal bool AdmitChargeActorRemoval(CombatController controller, UnitEntityData unit)
        {
            if (!ReferenceEquals(BoundaryCoordinator, this)) return BoundaryCoordinator.AdmitChargeActorRemoval(controller, unit);
            if (ReferenceEquals(unit, chargeRemovalPermit) && ReferenceEquals(controller, chargeBoundaryPermitController))
            { chargeRemovalPermit = null; return true; }
            if (unit != null && (ChargeBoundaries.Contains(MountedChargeBoundaryKind.RiderRemoval, unit) ||
                ChargeBoundaries.Contains(MountedChargeBoundaryKind.MountRemoval, unit))) return false;
            var pairCombat = relationship?.World?.ForActor(unit)?.Combat ?? this;
            var owner = pairCombat.chargeOwner;
            var exactRider = owner?.Rider ?? pairCombat.relationship?.PairOwner?.Rider ?? pairCombat.relationship?.Rider;
            var exactMount = owner?.Mount ?? pairCombat.relationship?.PairOwner?.Mount ?? pairCombat.relationship?.Mount;
            if (unit == null || unit != exactRider && unit != exactMount) return true;
            var kind = unit == exactRider ? MountedChargeBoundaryKind.RiderRemoval : MountedChargeBoundaryKind.MountRemoval;
            if (!ChargeSerializationBusy && pairCombat.TryDrainChargeOwnership("native-pair-actor-removal")) return true;
            var world = Game.Instance.Player;
            ChargeBoundaries.Enqueue(kind, unit, () => ReferenceEquals(world, Game.Instance?.Player) &&
                ReferenceEquals(controller, Game.Instance.TurnBasedCombatController) && controller.Initialized &&
                (!unit.IsInState || !unit.IsInCombat || unit.Descriptor.State.IsDead), () =>
            {
                var method = typeof(CombatController).GetMethod("RemoveUnit", BindingFlags.Instance | BindingFlags.NonPublic,
                    null, new[] { typeof(UnitEntityData) }, null);
                if (method == null || method.MetadataToken != 0x06000BE6 ||
                    method.Module.ModuleVersionId != new Guid("07fa1e4d-8618-41b3-9b8d-faa17d3b26f7"))
                    throw new InvalidOperationException("Exact native actor-removal boundary changed.");
                chargeBoundaryPermitController = controller; chargeRemovalPermit = unit;
                try { method.Invoke(controller, new object[] { unit }); }
                finally { chargeRemovalPermit = null; chargeBoundaryPermitController = null; }
            });
            return false;
        }

        private void ResumeChargeNativeBoundaries()
        {
            if (!ReferenceEquals(BoundaryCoordinator, this) || !ChargeNativeBoundaryPending || ChargeSerializationBusy) return;
            if (!chargeBoundaries.TryDrain(() => !ChargeSerializationBusy && DrainBoundaryOwners("deferred-native-boundary")) && chargeBoundaries.Fault != null)
                LastFeedback = "Native mode/combat transition retained after failure: " + chargeBoundaries.Fault;
        }

        internal void RetainDestroyedActorBoundary(UnitEntityData actor, Action retire)
        {
            if (actor == null) return;
            if (!ReferenceEquals(BoundaryCoordinator, this)) { BoundaryCoordinator.RetainDestroyedActorBoundary(actor, retire); return; }
            var pairCombat = relationship?.World?.ForActor(actor)?.Combat ?? this;
            var owner = pairCombat.chargeOwner;
            var ownsActor = owner != null && (actor == owner.Rider || actor == owner.Mount);
            if (!ChargeSerializationBusy && !ChargeNativeBoundaryPending &&
                (!ownsActor || pairCombat.TryDrainChargeOwnership("pair-unit-destroyed"))) { retire(); return; }
            // Retirement is exact-reference bookkeeping. It is still required if
            // another lifecycle callback has already dissolved the relationship.
            ChargeBoundaries.Enqueue(MountedChargeBoundaryKind.DestroyedActor, actor, () => true, retire);
        }

        internal JObject CaptureChargeNativeBoundaries() => !ReferenceEquals(BoundaryCoordinator, this) ?
            BoundaryCoordinator.CaptureChargeNativeBoundaries() : new JObject
        {
            ["pending"] = ChargeNativeBoundaryPending, ["replaying"] = replayingChargeBoundary,
            ["completed"] = chargeBoundaries?.Completed ?? 0, ["superseded"] = chargeBoundaries?.Superseded ?? 0,
            ["fault"] = chargeBoundaries?.Fault, ["retryError"] = chargeBoundaries?.RetryError
        };
    }
}
