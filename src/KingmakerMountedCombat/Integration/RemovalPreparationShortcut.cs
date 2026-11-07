using KingmakerMountedCombat.Logging;
using UnityEngine;

namespace KingmakerMountedCombat.Integration
{
    // The player's route to removal preparation now that the mod has no options page. A deliberate chord (Ctrl+Shift+F11)
    // asks through the native message box, runs the same MountedRemovalPreparation the old UMM button ran, and reports its
    // verdict in that box. The preparation's own refusals, cleanup and archive checks are unchanged; this only starts it.
    internal sealed class RemovalPreparationShortcut
    {
        internal const string Chord = "Ctrl+Shift+F11";
        internal const string Prompt = "Prepare a clean save before removing Mr. Bard's Kingmaker Mounted Combat? Every mounted pair " +
            "is dismounted, then a new save named " + MountedRemovalPreparation.CleanupSaveName + " is written and checked. " +
            "The mod stays installed.";
        private readonly MountedRemovalPreparation removal;
        private readonly NativeTextDialog dialog;
        private readonly IModLogger logger;
        private bool confirmed, awaiting, reportPending;

        internal RemovalPreparationShortcut(MountedRemovalPreparation removal, NativeTextDialog dialog, IModLogger logger)
        {
            this.removal = removal;
            this.dialog = dialog;
            this.logger = logger;
        }

        // Called every frame. The confirmation and the report open from here, never inside the box's own close callbacks.
        internal void Update()
        {
            if (confirmed) { confirmed = false; Begin(); }
            if (awaiting && removal.State != RemovalPreparationState.Saving) { awaiting = false; reportPending = true; }
            if (reportPending && !NativeTextDialog.IsShown) { reportPending = false; dialog.OpenMessage(removal.Status, null); }
            if (awaiting || reportPending || !ChordDown() || NativeTextDialog.IsShown || NativeTextDialog.SelectedUi() != null) return;
            Request();
        }

        internal bool Request() => dialog.OpenConfirm(Prompt, yes => { if (yes) confirmed = true; });

        private void Begin()
        {
            logger.Info("Removal preparation requested with " + Chord + ".");
            if (removal.Begin()) awaiting = true;
            else reportPending = true;
        }

        private static bool ChordDown() =>
            (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) &&
            (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && Input.GetKeyDown(KeyCode.F11);
    }
}
