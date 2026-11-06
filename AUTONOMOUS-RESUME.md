# F4 resume — final review stop after the entrance/reload finishing mission (2026-11-06)

Worktree C:\Dev\KingmakerMountedCombatLab\worktrees\finalize-f4; branch codex/kmc-finalize-f4-orders-ai-transitions; draft PR #19. Read CURRENT.md and the current section of docs/FINALIZE-F4-REVIEW-HANDOFF.md.
- **State:** ENGINEERING PARTIAL.
  - The r08 entrance is PASS.
  - The reload is not run: 8 of 8 starts are used, at 64.47 of 75 min.
  - WORLD-02 is input blocked.
- **Not authorized:** merge, release, publication, a new start, or F5.

Next run, if the owner authorizes one start:
- The scratch helper is `f4run.ps1 -Kind dungeon-entry-auto -SourceRunId 20261106-finalize-f4f-dungeon-08 -SourceSha256 b8e4109ea5bd9deae8e8e77b7b0318e1afa0d42895071d1f3a52a29c284540f5`. It is pinned to f4f; a new ceiling needs the same append-only record entry and a helper change.
- The consumer WhatIf `20261106-finalize-f4f-dungeon-reload-whatif` already passed.

Facts for a successor:
- The product writes the schema-3 archive member (`Pairs[]`). `NativeMountedSaveStorage.Read` / `persistence.LoadedData` is the legacy view and is not Current for schema 3. Use `ReadArchive` / `LoadedArchiveData`, as the cold restore path does.
- The launch guard reserves the 600 s outer timeout, so each start needs at least 10 minutes of budget left.
- `@()` around a comma-wrapped .NET List from a function counts the list as one element. Enumerate it with `foreach`.

# F4 resume — final review stop after the completion mission (2026-11-06, superseded above)

Worktree C:\Dev\KingmakerMountedCombatLab\worktrees\finalize-f4; branch codex/kmc-finalize-f4-orders-ai-transitions; draft PR #19. Read CURRENT.md and docs/FINALIZE-F4-REVIEW-HANDOFF.md.
- **State:** ENGINEERING PARTIAL. 6 of 6 starts are used (47.88/75 min). The launch helper refuses a seventh start.
- **Not authorized:** merge, release, publication, a new start, or F5.

Facts for a successor:
- Native `GroupManager.Go` runs `SetGroup`, `Show(false)` and then the go action (the AfterEntry `LoadArea`) in one call. The hub's selector component is destroyed by then, so read it only through Unity's `== null`.
- The workflow batch guard refuses a third equivalent attempt in a batch after two failures. A materially changed candidate runs under a new batch id. A refusal charges no start.
- The Working fixture floor has native hostiles that can join a row's fight and keep the pair in combat. The Sprint 4 melee row then waits for out-of-combat forever.
- Native Hold (`AiMovementForbidden`) filters brain proposals to targets in reach with LOS. A completed player move arms KMC's player-move hold; Stop clears it, and an AI off/on cycle clears the Stop suppression.
- The P07 dungeon entry declares one leaf `Auto_1.zks` with Area = floor and AdmissionArea = hub, write cap 1 and load cap 1. The Working fixture's baseline and working areas are both the floor `9d1278a2…`.

# F4 resume — review stop (2026-11-06, superseded above)

Worktree C:\Dev\KingmakerMountedCombatLab\worktrees\finalize-f4; branch codex/kmc-finalize-f4-orders-ai-transitions. Read CURRENT.md and docs/FINALIZE-F4-REVIEW-HANDOFF.md.
- **State:** ENGINEERING PARTIAL. All 3 F4 starts are used (23.85/75 min). The launch helper refuses a fourth start.
- **Not authorized:** merge, release, publication, a new start, or F5.

Facts for a successor:
- Registering a new tranche scenario takes more than the Sprint 4 list. The `-clike 'sprint4-*'` prefix dispatches in Chunk6aNativeMammothEvidence.ps1 and ChildEntryPreambleEvidence.ps1 need it, and so do both row lists: RuntimeHarness.Common.ps1 near 4223 and RuntimeProtocol.cs near 1566.
- Owner-surface isolation is zero-write. The native after-entry autosave of a dungeon entry is a fatal violation there.
- The roguelike hub holds 2 LootComponent (ChestForStash) and no trap.
- After an F4 stage moves the pair, the rider-on-foot comparison leg may find no route.
- `UnitAttack.Target` has a private setter, so the continuation sets it through the exact token 0x0600266C.

First command: Get-Content C:\Dev\KingmakerMountedCombatLab\runtime-state\workflow-budgets\finalize-f4.json.

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
