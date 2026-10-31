# Sprint 2 resume record (2026-10-31, seventh iteration)

Machine: DESKTOP-SRJJ623. Worktree `C:\Dev\KingmakerMountedCombatLab\worktrees\sprint2-movement-doors`,
branch `codex/mounted-combat-sprint2-movement-doors` (the request validator requires the
`codex/mounted-combat-` prefix). The root repository stays on `main` `05a9efd0` with its frozen
`bin\Release` DLL untouched.

## Custody

No native game, executor, transaction or lock remains. Retained under `runtime-evidence`: r01..r08, r09/r10 (PASS on
`809573ad…`), r13/r14/r16, r17..r21 (sixth iteration; r21 accepted natively, re-evaluated PASS by reader `d1667bcd`),
r22 (`20261031-sprint2-r22-traversal-core-dungeon-door-clock`, PASS on sprint2v: the real-time door exercise of the
corrected clock) and r23 (`20261031-sprint2-r23-owner-dungeon-door-tb-single-pair`, refused before load: autosave type
`Auto` against the authority's `Manual` pin). Immutable inputs: the owner seeds in `fixtures\owner-supplied\`
(Old Sycamore `90789101…`, surface `1fda72b1…`, dungeon floor `4c1db37e…` registered as
`KMC_S2_DUNGEON_FLOOR_CANDIDATE`, type `Auto`), the `KMC_AUTOMATION_WORKING` fixture with retained dependencies
`analysis-cache\sprint1\retained-dependencies-final211.json`. Candidate sprint2v (`3ad92495`, suite
`sprint2v-3ad92495-20261031`); sprint2u's artifact name is taken by the oversize refusal and must not be reused. Budget
record `runtime-state\workflow-budgets\sprint2.json`: allowance 160 (adjustments +30, +20, +20), 143.58 spent, 103
entries, batches `sprint2:completion-1` to `-7-tb`.

## Next action

1. Final review of draft PR #7 (disposition PARTIAL). Owner play acceptance stays separate. No merge, release or
   installation is part of this workflow.
2. Turn-based door control is the only outcome not demonstrated. Required input: an owner-made **Manual** save on a
   floor or area where a revealed, interactable door with `StandardDoor.NotInCombat` false stands within 28 m of the
   mounted pair (not the dwarf dungeon door class; not Old Sycamore's tavern door). Then: register it in
   `scripts/runtime/Chunk7OwnerSeedRegistry.json` (type `Manual`), copy `analysis-cache\sprint2\owner-surface-dungeon-door-tb.json`
   with its seed and party identities, add the route to the launch wrapper (`owner-surface-door` pattern) and run; the
   stage selects the door itself and refuses early with the inventory when none qualifies. If the owner prefers the
   existing autosave, the alternative is a deliberate decision to admit native type `Auto` in
   `PersistenceSaveAuthorization`'s owner-seed pins (a persistence safety contract; not changed here).
3. Any launch needs a new explicit allowance (16.42 min remain; a launch needs its 600 s outer timeout at admission)
   and its own declared batch id.
4. Tooling facts: the package manifest binds the exact HEAD (a docs commit also invalidates a WhatIf against an older
   package); `Package.ps1` refuses a diagnostic DLL above 8 MiB (the assembly is exactly at the bound: trim literals or
   split the qualification assembly before adding more diagnostics); suite freeze in-process with `-Confirm:$false`;
   package/suite charged as imported `kind=other` entries; the wrapper's `*>` logs are UTF-16; the batch guard keys on
   the scenario.

Prior records: [AUTONOMOUS-RESUME before Sprint 2](docs/history/AUTONOMOUS-RESUME-before-sprint2-2026-10-30.md).
