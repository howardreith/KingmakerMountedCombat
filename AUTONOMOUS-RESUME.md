# F3 resume — final review stop (2026-11-06, completion)

Worktree C:\Dev\KingmakerMountedCombatLab\worktrees\finalize-f3; branch codex/kmc-finalize-f3-movement-economy-hud; draft PR #18. Read CURRENT.md and docs/FINALIZE-F3-REVIEW-HANDOFF.md.
- **State:** ENGINEERING COMPLETE. TB r05 and RT r04 PASS.
- **Allowance:** 5 of 6 starts used, at 51.47/90 min. The sixth start was a contingency for a diagnosed defect and is not scheduled; the F3 launch helper refuses a seventh.
- **Not authorized:** merge, release, image publication, or F4.

Completion facts:
- **Native full attack in TB.** It needs the attack variant cycled to Full with the native right-click before the left click. The fixture's pair-B rider, at level 11, plans four strikes.
- **Panel.** The prediction panel's slots follow their `TurnController`. The mount's private states only advance on the mount's own command events, so the fill recomputes them on every rider-selected ground hover. `ComputeCurrentActionStates` is a turn-start snapshot: any started Move or conversion counts as gone, and a used step still counts as available.
- **Cleanup.** In r05 it went Pause, Default, then native leave-combat, and EscMode did not recur. r03's opener is unidentified.

# F3 resume — review stop (2026-11-06, superseded above)

Worktree C:\Dev\KingmakerMountedCombatLab\worktrees\finalize-f3; branch codex/kmc-finalize-f3-movement-economy-hud. Read CURRENT.md and docs/FINALIZE-F3-REVIEW-HANDOFF.md. All four F3 starts were used at that stop; the completion mission later raised the ceiling to six.

Facts for a successor:
- The Working fixture's second party member has no pet. The Sprint 6 Ranger acquisition needs four levels of experience granted first.
- A row step bound must stay under the tranche's 30 s leaf deadline, or a stall loses its facts.
- With the rider selected, the TB preview previously read the rider's TurnController. PairedMovementContext now routes it to the mount.
- Native: a partial five-foot step leaves a residual step. A move does not empty movement (`DoesCommandEmptiesMovement` is false).
- Native `UsedStandardAction` ignores the two-move conversion; `HasStandardAction` does not.
- The fixture rider's TB stock attack is a single attack unless it approaches.
- r03's cleanup stalled in EscMode with the party still in combat; the cause is unknown.

First command: Get-Content C:\Dev\KingmakerMountedCombatLab\runtime-state\workflow-budgets\finalize-f3.json.

# F1 resume — final review stop

Worktree C:\Dev\KingmakerMountedCombatLab\worktrees\finalize-f1; branch codex/kmc-finalize-f1-creation-ui-icons; draft PR16. Read CURRENT.md and docs/FINALIZE-F1-REVIEW-HANDOFF.md. Preserve newer work. Do not restart intake or historical campaigns.

State: F1d run 20261105-finalize-f1d-native-01 passed and restored. All four F1 starts are used. Nothing runs from this resume: no launch, allowance increase, merge, release or F2 is authorized. A review correction that changes product bytes needs a new owner decision, package, suite and start; do not relabel run evidence onto it.

Facts for a successor:
- WhatIf stops before Test-RuntimeRequest. scripts/Test-FinalizeF1Finish.ps1 validates the next request's exact shape directly.
- The package allowlist excludes UnityEngine.UI, so the product reaches those members by exact token through reflection.
- The observer's refresh-scroll probe returns -99 because no parent ScrollRect is found from the layer. It gives no scroll evidence.
- Creator Next requires all ability and skill points spent. UMM's Ctrl+F10 needs real scan codes under synthetic input. Check the class list's scroll before clicking; it differs in a second creator.
- The publishing helper registers images by exact path and SHA, and each new image needs owner approval; see docs/evidence/finalize-f1/publishing-authority.json.

First command: Get-Content C:\Dev\KingmakerMountedCombatLab\runtime-state\workflow-budgets\finalize-f1.json.
