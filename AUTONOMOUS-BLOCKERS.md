# Autonomous blockers

Status: IN PROGRESS

## Active runtime blocker: native main-menu fixture bootstrap

Status: IN PROGRESS

The first guarded save-backed run, `20260813T225000Z-fixture-intake-pass1`, authorized exactly one load of the qualified Working fixture but failed before the load callback. Kingmaker logged `InvalidOperationException: Sequence contains no matching element` in `Player.PostLoad`; exact local assembly analysis maps this to the main-character identity not being present in deserialized `CrossSceneState.AllEntityData`. The result is a truthful FAIL with `CurrentMode=None`, no loaded area, and no fixture identity verification.

The harness restored every external surface exactly: live Mods digest `c292c5c62a232a0ad7b32ed489139a8d135caa18e38151677f619bf7555c70cb`; save metadata digest `58a54974423316041e29d32b1093c5d6775dbd67fe2b37ffaea3c9ae27de19be`; immutable Baseline SHA-256 `c29d965c9ff5dc0f971659d9ae154877aa4a9a461ca220d1ce28e7c7fd9d2512`; restored Working SHA-256 `a5f7a7fb77f0465df1591360ecd1730e2c28215d83e27aafc70efef3110e6dc5`. The sole runtime-mutated Working was quarantined, not deleted. No process, lock, sentinel, or live residue remains.

Exact tracing found the first implementation defect: `WorkingFixtureLoader` called `Game.LoadGame` directly from the title screen. The native PC path is `MainMenu.LoadGame` -> `EnterGame` -> `LoadBaseMechanics` -> `Game.Initialize` -> `Game.LoadGameFromMainMenu`. The bounded repair now uses that path and fails early when `LoadingProcess` becomes inactive before the registered callback; it passes 3 deterministic watchdog tests and 11 new exact assembly-backed contract checks. The presence of installed-mod data in the fixture is retained only as a secondary hypothesis; no installed-mod overlay will be attempted unless the native bootstrap retry fails and a separate safety/independence audit permits it.

## Cleared blocker: exact KMC fixture identity

Status: PASS

The exact filename audit rerun on `2026-08-13` found exactly one canonical Baseline, `Manual_298_KMC_AUTOMATION_BASELINE.zks`, exactly one canonical Working, `Manual_299_KMC_AUTOMATION_WORKING.zks`, and zero KMC-looking near-matches. The prior trailing-underscore near-match is absent. Other-project KBP/KMG, ordinary, auto, quick, and other saves were not opened, copied, renamed, or loaded.

After repairing and regression-testing a cold-process ZIP assembly-load defect, the guard read only the two canonical archives. It verified exact internal names, distinct non-linked paths, Manual/v1 descriptors, and identical campaign `cvb`, GameId `d41185be-edc1-47c0-b9d5-e1d7a9c8e65f`, and Area `9d1278a2f599b2a4daab53abdfe88d2e`. Baseline SHA-256 is `c29d965c9ff5dc0f971659d9ae154877aa4a9a461ca220d1ce28e7c7fd9d2512`; initial Working SHA-256 is `a5f7a7fb77f0465df1591360ecd1730e2c28215d83e27aafc70efef3110e6dc5`.

The durable qualification record was written once at `runtime-state/fixture-qualification.json`, SHA-256 `c1e33c75004212039258d6d90ba20e43c37c50966c94e685dbad1ca0e653654f`, and a validation-only rerun proved it and both save metadata records byte/time-stable. Its sole writable allowlist identity is `KMC_AUTOMATION_WORKING`; Baseline is immutable and never a repair target. This clears the mission §26.2 fixture-identity blocker.

Remaining work is the guarded live qualification: pair availability, movement, drift, turns, doorway control, selection, formation, lifecycle residue, save/load/area boundaries, visual classification, and the final B/C/D architecture decision.

The independent pre-live recovery gate is PASS offline: exact Kingmaker `LoadRoutine` and installed DotNetZip replacement behavior are covered by scenario-bound direct-child temp/sidecar classification, a durable token-owned plan, quarantine without deletion, same-size/same-time Working detection, idempotent interruption recovery, and exact preflight metadata restoration. Unknown, foreign, hard-link, reparse, excess, and second-canonical-slot drift fails before a move. The real `SaveRoutine` experiment remains removed; Phase 1 save-safety proves cleanup plus no relationship serialization without invoking that unsafe exact-path surface.

Exact first safe command after the Working fixture is recreated manually through Kingmaker with the exact internal name:

```powershell
Get-ChildItem -LiteralPath 'C:\Users\Howie\AppData\LocalLow\Owlcat Games\Pathfinder Kingmaker\Saved Games' -File | Where-Object Name -Match '^Manual_[0-9]+_KMC_AUTOMATION_(BASELINE|WORKING)\.zks$' | Select-Object Name,Length,LastWriteTimeUtc
```

## Evidence-backed constraints

### Exact UMM expectation differs

Status: DEFER — EVIDENCED

Installed UMM is `0.28.2.0`, SHA-256 `75b96e25a3a9fbadb47dd14a4ab490cb8c98143a6242aff3bba6145cd3047f39`, not the mission's expected 0.32.x. Production correctly targets the observed assembly and no upgrade/substitution occurred.

### Visual result unavailable

Status: DEFER — EVIDENCED

The invariant-correct rank-7+ Mammoth and `Spine` anchor hypothesis are mapped, but rider identity and live animation/pose evidence require the missing canonical Working fixture. No visual classification is claimed.

### Runtime safety state

Status: PASS

The harness, WhatIf, two final fresh-process smokes, two guarded historical recoveries, and the new offline Working-slot recovery suite are qualified. Live Mods currently match the transaction baseline/current inventory of 348 files/44 directories/69,797,076 bytes with digest `e62320bbe7d4b83edc128a62f9f5852b0669c5549859602c335c649318578d47`; no KMC sentinel or lock and no Kingmaker/Wrath/UMM process remain. The intake journal's older `e9545a...` hash used an undocumented/different algorithm and is not directly comparable; file count and top-level entries agree, while no intake algorithm/full manifest exists to establish historical hash equality.
