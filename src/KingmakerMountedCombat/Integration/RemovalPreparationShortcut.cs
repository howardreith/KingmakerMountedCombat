using System;
using System.Reflection;
using Kingmaker.UI;
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
        // A Yes is acted on only in the frames right after it. UMM stops calling Update while the mod is disabled, so a Yes
        // given then must not start a cleanup save whenever the mod is next enabled.
        private const int ConfirmationFrames = 5;
        // Native KeyboardAccess's own test before every binding: a selected text field holds the keyboard.
        private static readonly MethodInfo InputFieldSelected = typeof(KeyboardAccess).GetMethod("IsInputFieldSelected",
            BindingFlags.Static | BindingFlags.NonPublic) is MethodInfo method && method.MetadataToken == 0x06002DFB ? method : null;
        private readonly MountedRemovalPreparation removal;
        private readonly NativeTextDialog dialog;
        private readonly IModLogger logger;
        private int confirmedFrame = -1;
        private bool awaiting, reportPending;

        internal RemovalPreparationShortcut(MountedRemovalPreparation removal, NativeTextDialog dialog, IModLogger logger)
        {
            this.removal = removal;
            this.dialog = dialog;
            this.logger = logger;
        }

        // Called every frame. The confirmation and the report open from here, never inside the box's own close callbacks.
        // A failure here is reported and contained: it must not reach the update boundary, whose cleanup disables the mod.
        internal void Update()
        {
            try
            {
                if (confirmedFrame >= 0)
                {
                    var age = Time.frameCount - confirmedFrame;
                    confirmedFrame = -1;
                    if (age <= ConfirmationFrames) Begin();
                    else logger.Warning("Removal preparation confirmation expired before it could run; nothing was saved.");
                }
                if (awaiting && removal.State != RemovalPreparationState.Saving) { awaiting = false; reportPending = true; }
                if (reportPending && !NativeTextDialog.IsShown) { reportPending = false; dialog.OpenMessage(removal.Status, null); }
                if (awaiting || reportPending || !ChordDown() || NativeTextDialog.IsShown || TextFieldSelected()) return;
                Request();
            }
            catch (Exception exception)
            {
                confirmedFrame = -1; awaiting = false; reportPending = false;
                logger.Exception("Removal preparation shortcut", exception);
                try { if (!NativeTextDialog.IsShown) dialog.OpenMessage("Not prepared: " + exception.GetType().Name + ". See the mod log.", null); }
                catch (Exception) { }
            }
        }

        internal bool Request() => dialog.OpenConfirm(Prompt, yes => { if (yes) confirmedFrame = Time.frameCount; });

        private void Begin()
        {
            logger.Info("Removal preparation requested with " + Chord + ".");
            if (removal.Begin()) awaiting = true;
            else reportPending = true;
        }

        private static bool TextFieldSelected() => InputFieldSelected == null || (bool)InputFieldSelected.Invoke(null, null);

        private static bool ChordDown() =>
            (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) &&
            (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && Input.GetKeyDown(KeyCode.F11);
    }
}
