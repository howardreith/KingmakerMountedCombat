# Autonomous blockers

Status: BLOCKED — CRITICAL

## Critical blocker: KMC fixture identity unavailable

The exact save root contains 269 files but zero exact filename candidates for `KMC_AUTOMATION_BASELINE` and zero for `KMC_AUTOMATION_WORKING`. Other-project KBP/KMG automation saves exist but are prohibited evidence sources and were not opened, copied, renamed, or loaded. No valued save archive was opened.

There is no conservative autonomous creation path for the required fixture: a new campaign does not produce a directly controllable exact Medium rider with an active rank-7+ Mammoth, while editing or deriving protected/foreign state would violate separation and save safety. Baseline and working fixture identity therefore cannot be distinguished, proving mission §26.2.

Fixture presence alone will not authorize loading. The current runtime protocol intentionally supports only no-save `mod-load-smoke` and rejects `-SaveAccessAllowed`. A future KMC-owned guard must first open only exact filename-prefiltered KMC archives and prove exactly one baseline and one working file, distinct paths, exact internal names, matching intended GameId/GameName/Area, and baseline immutability.

Blocked work: live pair availability, movement, drift, turns, doorway control, selection, formation, lifecycle residue, save/load/area boundaries, and visual classification. This also blocks a truthful final B/C/D architecture decision.

Exact first safe command after project-owned fixtures exist:

```powershell
Get-ChildItem -LiteralPath 'C:\Users\Howie\AppData\LocalLow\Owlcat Games\Pathfinder Kingmaker\Saved Games' -File | Where-Object Name -Match '^Manual_[0-9]+_KMC_AUTOMATION_(BASELINE|WORKING)\.zks$' | Select-Object Name,Length,LastWriteTimeUtc
```

## Evidence-backed constraints

### Exact UMM expectation differs

Status: DEFER — EVIDENCED

Installed UMM is `0.28.2.0`, SHA-256 `75b96e25a3a9fbadb47dd14a4ab490cb8c98143a6242aff3bba6145cd3047f39`, not the mission's expected 0.32.x. Production correctly targets the observed assembly and no upgrade/substitution occurred.

### Visual result unavailable

Status: DEFER — EVIDENCED

The invariant-correct rank-7+ Mammoth and `Spine` anchor hypothesis are mapped, but rider identity and live animation/pose evidence require the missing working fixture. No visual classification is claimed.

### Runtime safety state

Status: PASS

The harness, WhatIf, two final fresh-process smokes, and two guarded recoveries are qualified. Live Mods currently match the transaction baseline/current inventory of 348 files/44 directories/69,797,076 bytes with digest `e62320bbe7d4b83edc128a62f9f5852b0669c5549859602c335c649318578d47`; no KMC sentinel or lock and no Kingmaker/Wrath/UMM process remain. The intake journal's older `e9545a...` hash used an undocumented/different algorithm and is not directly comparable; file count and top-level entries agree, while no intake algorithm/full manifest exists to establish historical hash equality.
