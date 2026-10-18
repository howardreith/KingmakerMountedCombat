# Chunk 7 known product debt

Bounded known issues found by native runs. They are recorded for a successor product candidate and are not fixed by the diagnostic changes that found them.

## RT party-brain attack proposals after a mounted pair attack (2026-10-18)

Found by run `20261018-c7b-world29-ownerseed-b4s2d-rt` (preview.29). The offline diagnosis is `analysis-cache/chunk7-continuation/owner-seed-batch04/rt-companion-diagnosis01.json`.

After a real-time mounted attack, the attacking pair actors keep `CombatState.ManualTarget` set to the target. Both the native `UnitCommands.UpdateCombatTarget` and KMC's acceptance path (`MountedCombatController.cs:788`) set it, and KMC clears it only on the stock-intent path. The native party brain (`AiBrainController`) then proposes a `UnitAttack` on that target about every 0.3-0.4 s while combat lasts, even with AI disabled (`UseWhenAIDisabled` actions). KMC's `UnitCommands.Run` prefix refuses each proposal before native Run, so no cost, command or pair state changes. The refusal still has two visible effects in real play:

- **Warning spam.** While the rider is not selected, each Horse proposal takes the hidden-mount refusal path (`MountedCombatController.cs:903-915`). That path calls `ShowStockFeedback()`, so the player sees "Select the rider to attack, or use the mount on its own turn." about every 0.35 s.
- **Lost player clicks.** Each rider-A proposal reaches the observed-request check, and `ClearObservedStockRequest()` (`MountedCombatController.cs:929`) clears the last observed player hostile click. A real player click that falls between an AI proposal and its own Run can be refused as unobserved.

Successor-candidate direction, not decided: stop leaving `ManualTarget` set after a direct paired attack, or refuse AI-originated proposals silently without clearing the observed player request. Either change alters the product identity and needs its own candidate and qualification.

Diagnostics impact: the owner-seed RT companion window now tolerates these proposals only as closed refused-before-Run pairs and records them as `pairRefusedProposals`. After Dismount A, rider A is no longer refused by KMC and still carries `ManualTarget`, so its brain may admit a native attack before the unmounted-control click (watch RT stages 5-7).

## Chunk 6A rule R3 superseded; its historical probe still asserts it (2026-10-18)

The owner's October 18 decision admits a voluntary turn-based Mount/Dismount in the rider's prepared (Preparing) turn (`06ec7180`). The Chunk 6A diagnostic probe `Chunk6aCombatMountScenario` row `CM01-combat-mount-preparing-refused`, the released 6A ledgers and the 6A readers and registration tests that list it still describe the superseded rule. Re-running the Chunk 6A turn-based scenarios on preview.33 or later will fail that row by design until the 6A gate set is revised to the admitted contract. The released Chunk 6 records are left unchanged as history.

`scripts/Test-Chunk6aLedger.ps1` already exits 1 on the successor: `Chunk6aHarnessIdentity.ps1` pins every `scripts/runtime/*.ps1` file to the recorded 6A reader revision, and many Chunk 7 readers have changed since. This predates the R3 change.
