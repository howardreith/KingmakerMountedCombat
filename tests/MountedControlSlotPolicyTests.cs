using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // F2 CTRL-01/02/03: the public mount-state control, its intent, and the hotbar migration and placement decisions.
    internal static class MountedControlSlotPolicyTests
    {
        internal static void Register(TestRunner runner)
        {
            runner.Run("public Mount / Dismount stays leased through every relationship state", PublicControlPersists);
            runner.Run("public Mount / Dismount runs the shell for the current state, captured at the click", IntentFollowsState);
            runner.Run("exact old Mount, Dismount and Mounted Charge slots become the public controls", RepairsExactOldSlots);
            runner.Run("saved control kinds migrate to the canonical controls and records are not slots", MigratesSavedKinds);
            runner.Run("initial placement takes the first empty main-bar slot and never overwrites", PlacesOnlyInEmptyMainBar);
            runner.Run("initial placement happens once, after restoration, with the native autofill setting", PlacesOnce);
        }

        private static void PublicControlPersists()
        {
            // Unmounted owner with a supported mount, with the feature on and off: the control stays, as Mount.
            TestRunner.True(Lease(true, true, false, false, false), "Unmounted owner lost the public control.");
            TestRunner.True(Lease(false, true, false, false, false), "Disabling the feature removed the public control from the bar.");
            // The same rider, mounted or faulted, keeps the same control, as Dismount.
            TestRunner.True(Lease(true, true, true, false, true), "Mounting removed the public control.");
            TestRunner.True(Lease(false, true, true, false, true), "A mounted rider lost the public control with the feature off.");
            TestRunner.True(Lease(true, true, false, true, true), "A faulted rider lost the public control.");
            // Never the mount, and never a unit without a supported mount.
            TestRunner.True(!Lease(true, false, true, false, false), "The carried mount was given the public control.");
            TestRunner.True(!Lease(true, false, false, false, false), "A unit without a supported mount was given the public control.");
            // The delivery shells keep their exact historical lease.
            TestRunner.True(NativeMountedControlPolicy.ShouldLease(NativeMountedControlKind.MountCompanion, true, true, false, false, true, false),
                "The hidden Mount shell lost its unmounted lease.");
            TestRunner.True(!NativeMountedControlPolicy.ShouldLease(NativeMountedControlKind.MountCompanion, true, true, true, false, true, false),
                "The hidden Mount shell was leased to a mounted rider.");
            TestRunner.True(NativeMountedControlPolicy.ShouldLease(NativeMountedControlKind.Dismount, false, false, true, false, true, false),
                "The Dismount escape hatch was lost.");
        }

        private static bool Lease(bool featureEnabled, bool ownerHasSupportedMount, bool mounted, bool faulted, bool unitIsRider) =>
            NativeMountedControlPolicy.ShouldLease(NativeMountedControlKind.MountState, featureEnabled, false,
                ownerHasSupportedMount, mounted, faulted, unitIsRider, false);

        private static void IntentFollowsState()
        {
            TestRunner.Equal(NativeMountedControlKind.MountCompanion,
                NativeMountedControlPolicy.ResolveMountStateIntent(false, false, false), "Unmounted owner did not get Mount.");
            TestRunner.Equal(NativeMountedControlKind.Dismount,
                NativeMountedControlPolicy.ResolveMountStateIntent(true, false, true), "Mounted rider did not get Dismount.");
            TestRunner.Equal(NativeMountedControlKind.Dismount,
                NativeMountedControlPolicy.ResolveMountStateIntent(false, true, true), "Faulted rider lost the Dismount escape.");
            TestRunner.Equal(NativeMountedControlKind.MountCompanion,
                NativeMountedControlPolicy.ResolveMountStateIntent(true, false, false), "A non-rider was offered Dismount.");
            // Each click creates a command of ONE shell; the shell kinds stay distinct so a queued Mount cannot be
            // delivered as a Dismount, or the reverse.
            TestRunner.True(NativeMountedControlPolicy.IsRelationshipShell(NativeMountedControlKind.MountCompanion) &&
                NativeMountedControlPolicy.IsRelationshipShell(NativeMountedControlKind.Dismount) &&
                !NativeMountedControlPolicy.IsRelationshipShell(NativeMountedControlKind.MountState),
                "The public control was treated as a deliverable relationship shell.");
            TestRunner.True(!NativeMountedControlPolicy.OwnsPendingControl(NativeMountedControlKind.MountState, true, false, false),
                "The public control claimed a pending native command of its own.");
        }

        private static void RepairsExactOldSlots()
        {
            TestRunner.Equal(MountedControlSlotRepair.MountState,
                MountedControlSlotPolicy.RepairLiveSlot(NativeMountedControlKind.MountCompanion), "Old Mount slot not repaired.");
            TestRunner.Equal(MountedControlSlotRepair.MountState,
                MountedControlSlotPolicy.RepairLiveSlot(NativeMountedControlKind.Dismount), "Old Dismount slot not repaired.");
            TestRunner.Equal(MountedControlSlotRepair.StockCharge,
                MountedControlSlotPolicy.RepairLiveSlot(NativeMountedControlKind.MountedCharge), "Old Mounted Charge slot not repaired.");
            foreach (var kind in new[] { NativeMountedControlKind.None, NativeMountedControlKind.MountState,
                NativeMountedControlKind.RiderPrimary, NativeMountedControlKind.MountPrimary })
                TestRunner.Equal(MountedControlSlotRepair.Keep, MountedControlSlotPolicy.RepairLiveSlot(kind),
                    kind + " was rewritten by the alias repair.");
        }

        private static void MigratesSavedKinds()
        {
            foreach (var kind in new[] { MountedControlSlotPolicy.SavedMountCompanion, MountedControlSlotPolicy.SavedDismount,
                MountedControlSlotPolicy.SavedMountState })
                TestRunner.Equal(MountedControlSlotRepair.MountState, MountedControlSlotPolicy.RestoreSavedSlot(kind),
                    "Saved kind " + kind + " did not restore as the public control.");
            TestRunner.Equal(MountedControlSlotRepair.StockCharge,
                MountedControlSlotPolicy.RestoreSavedSlot(MountedControlSlotPolicy.SavedMountedCharge), "Saved Mounted Charge did not become Charge.");
            TestRunner.Equal(MountedControlSlotRepair.Keep,
                MountedControlSlotPolicy.RestoreSavedSlot(MountedControlSlotPolicy.SavedRiderPrimary), "Retired helper restore changed.");
            // Historical kinds keep their numbers; F2's are new.
            TestRunner.True(MountedControlSlotPolicy.SavedMountCompanion == (int)NativeMountedControlKind.MountCompanion &&
                MountedControlSlotPolicy.SavedDismount == (int)NativeMountedControlKind.Dismount &&
                MountedControlSlotPolicy.SavedMountedCharge == (int)NativeMountedControlKind.MountedCharge &&
                MountedControlSlotPolicy.SavedMountState == (int)NativeMountedControlKind.MountState,
                "A saved control kind number moved.");
            // Rows: a placement record is index 0 only and does not collide with a real slot at index 0.
            TestRunner.True(MountedControlSlotPolicy.IsValidSavedRow(0, MountedControlSlotPolicy.SavedPlacementRecord) &&
                !MountedControlSlotPolicy.IsValidSavedRow(3, MountedControlSlotPolicy.SavedPlacementRecord),
                "Placement record index is not fixed.");
            TestRunner.True(MountedControlSlotPolicy.IsValidSavedRow(127, MountedControlSlotPolicy.SavedMountState) &&
                !MountedControlSlotPolicy.IsValidSavedRow(128, MountedControlSlotPolicy.SavedMountState) &&
                !MountedControlSlotPolicy.IsValidSavedRow(0, 0) && !MountedControlSlotPolicy.IsValidSavedRow(0, 8),
                "Saved row bounds changed.");
            TestRunner.True(MountedControlSlotPolicy.SavedRowKey("a", 0, MountedControlSlotPolicy.SavedPlacementRecord) !=
                MountedControlSlotPolicy.SavedRowKey("a", 0, MountedControlSlotPolicy.SavedMountState),
                "A placement record collides with the control's slot at index 0.");
            TestRunner.True(MountedControlSlotPolicy.SavedRowKey("a", 4, MountedControlSlotPolicy.SavedMountState) ==
                MountedControlSlotPolicy.SavedRowKey("a", 4, MountedControlSlotPolicy.SavedMountedCharge),
                "Two controls may claim one slot.");
        }

        private static void PlacesOnlyInEmptyMainBar()
        {
            var bar = new bool[140];
            bar[0] = false; bar[1] = false; bar[2] = true; bar[5] = true; bar[20] = true;
            TestRunner.Equal(2, MountedControlSlotPolicy.SelectInitialPlacementIndex(bar), "Not the first empty main-bar slot.");
            var full = new bool[140];
            full[14] = true; full[139] = true;
            TestRunner.Equal(-1, MountedControlSlotPolicy.SelectInitialPlacementIndex(full),
                "A full main bar spilled the control into another panel or overwrote a slot.");
            TestRunner.Equal(-1, MountedControlSlotPolicy.SelectInitialPlacementIndex(null), "A missing bar was written.");
            TestRunner.Equal(14, MountedControlSlotPolicy.MainBarSlotCount, "Main bar size drifted from the installed contract.");
        }

        private static void PlacesOnce()
        {
            TestRunner.True(MountedControlSlotPolicy.ShouldAttemptInitialPlacement(true, false, true, true), "First acquisition was not placed.");
            TestRunner.True(!MountedControlSlotPolicy.ShouldAttemptInitialPlacement(true, true, true, true),
                "A recorded actor (placed, migrated or removed by the player) was placed again.");
            TestRunner.True(!MountedControlSlotPolicy.ShouldAttemptInitialPlacement(true, false, false, true),
                "Placement ran ahead of the load's own restored bindings.");
            TestRunner.True(!MountedControlSlotPolicy.ShouldAttemptInitialPlacement(true, false, true, false),
                "Placement ignored the native autofill setting.");
            TestRunner.True(!MountedControlSlotPolicy.ShouldAttemptInitialPlacement(false, false, true, true),
                "A unit without the control was placed.");
        }
    }
}
