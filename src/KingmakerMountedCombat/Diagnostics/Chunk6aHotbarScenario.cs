using System;
using Kingmaker;
using Kingmaker.UI.ActionBar;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Phase3dHorseScenarioTranche
    {
        internal const string Chunk6aHotbarScenario = "chunk6a-hotbar-approach";
        private bool Chunk6aHotbarOnly => request.Scenario == Chunk6aHotbarScenario;
        private NativeMountActionBarUiLease chunk6aHotbarUi;
        private NativeMountActionBarProbe chunk6aHotbarProbe;
        private ActionBarSlot chunk6aHotbarSlot;
        private JObject chunk6aHotbarInput;
        private bool PrepareChunk6aHotbar()
        {
            if (!Chunk6aHotbarOnly) return true;
            if (!NativeMountActionBarUiLease.IsReady(rider)) return false;
            if (chunk6aHotbarUi == null)
            {
                chunk6aHotbarUi = new NativeMountActionBarUiLease(rider);
                observations["chunk6aHotbarUi"] = chunk6aHotbarUi.Capture();
                return false; // Wait for the actual native group to populate.
            }
            chunk6aHotbarSlot = chunk6aHotbarUi.FindMountSlot();
            observations["chunk6aHotbarUi"] = chunk6aHotbarUi.Capture();
            return chunk6aHotbarSlot != null;
        }
        private bool InvokeChunk6aHotbar()
        {
            if (!Chunk6aHotbarOnly || Chunk6aTurnBased) throw new InvalidOperationException("Hotbar input belongs to its isolated RT allocation.");
            lastNativeAbilityShell = null;
            chunk6aHotbarProbe = new NativeMountActionBarProbe(rider, horse, chunk6aHotbarSlot, CaptureChunk6aCausalState);
            try
            {
                var clicked = chunk6aHotbarProbe.Invoke();
                var command = rider.Commands.GetCommand(UnitCommand.CommandType.Move) as UnitUseAbility;
                if (clicked && command?.Executor == rider && command.Target?.Unit == horse && command.Spell?.Blueprint == nativeControls.MountAbility)
                    lastNativeAbilityShell = command;
                return clicked && lastNativeAbilityShell != null;
            }
            finally
            {
                chunk6aHotbarInput = chunk6aHotbarProbe.Capture();
                observations["chunk6aHotbarInput"] = chunk6aHotbarInput.DeepClone();
                chunk6aHotbarProbe.Dispose(); chunk6aHotbarProbe = null;
            }
        }
        private void FinishChunk6aHotbar(JObject commandProof)
        {
            if (!Chunk6aHotbarOnly) return;
            var pass = chunk6aHotbarInput != null && (bool)chunk6aHotbarInput["complete"] && (bool)commandProof["pass"];
            AddRow("CM06-hotbar-path", pass,
                "One exact native registered action-bar slot selected Mount and its native target click admitted the same command through terminal delivery; external replay verifies nested callbacks.",
                new JObject { ["input"] = chunk6aHotbarInput, ["commandProof"] = commandProof.DeepClone() });
        }
        private void CleanupChunk6aHotbar()
        {
            if (chunk6aHotbarProbe != null)
            {
                observations["chunk6aHotbarInputAbort"] = chunk6aHotbarProbe.Capture();
                chunk6aHotbarProbe.Dispose(); chunk6aHotbarProbe = null;
            }
            if (chunk6aHotbarUi != null)
            {
                try { chunk6aHotbarUi.Dispose(); }
                finally { observations["chunk6aHotbarUi"] = chunk6aHotbarUi.Capture(); chunk6aHotbarUi = null; }
            }
        }
    }
}