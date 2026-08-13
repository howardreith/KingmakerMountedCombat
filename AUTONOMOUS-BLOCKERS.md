# Autonomous blockers

Status: BLOCKED — CRITICAL

## Critical blocker: exact KMC Working filename unavailable

The resumed exact filename audit at `2026-08-13T19:51Z` found exactly one canonical baseline, `Manual_298_KMC_AUTOMATION_BASELINE.zks`, but zero canonical working candidates. Filename-only metadata shows one rejected near-match, `Manual_299_KMC_AUTOMATION_WORKING_.zks`, whose trailing underscore is outside the documented exact regex. Other-project KBP/KMG, ordinary, auto, quick, and other saves were not opened, copied, renamed, or loaded. Neither KMC archive was opened because the filename gate failed first.

Exact Kingmaker `SaveManager.PrepareSave` evidence shows a fresh exact `KMC_AUTOMATION_WORKING` name produces no trailing underscore. A trailing underscore can reflect an originally non-alphanumeric suffix or a stale filename after overwrite. It is therefore not evidence of the exact internal name and cannot be silently accepted or renamed by automation.

The original KMC fixture guard, proactive in-process Working-only authorization, direct exact-Working loader, schema-v2 request/result protocol, combined Working/Mods transaction, stable recovery, and lifecycle/movement/boundary engines are implemented and offline-qualified. The complete current gate is source 21/0, pure/component 54/0, harness 46/0, and assembly-backed 47/0. The content-free protected-save inventory intentionally does not open or hash non-KMC archives, so an adversarial same-length/same-timestamp external rewrite is outside its standalone detection power; proactive engine-boundary denial complements it. The live guard cannot inspect descriptors or write durable qualification until the exact filename audit reaches 1/1.

Blocked work: live pair availability, movement, drift, turns, doorway control, selection, formation, lifecycle residue, save/load/area boundaries, and visual classification. This also blocks a truthful final B/C/D architecture decision.

An independent pre-live safety gate is also IN PROGRESS: exact Kingmaker `LoadRoutine` and installed DotNetZip replacement behavior can leave a run-created direct-child temp, sidecar, or duplicate canonical Working file after interruption. No schema-v2 launch is permitted until the outer transaction can preclassify that bounded Working-slot artifact family, persist an owned quarantine plan, recover idempotently without deletion, and prove the exact preflight save metadata digest in synthetic crash-window tests. The real `SaveRoutine` experiment was removed; Phase 1 save-safety proves cleanup plus no relationship serialization without invoking that unsafe exact-path surface.

Exact first safe command after the Working fixture is corrected manually through Kingmaker:

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
