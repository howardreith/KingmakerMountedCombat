# Autonomous resume

Updated: 2026-08-13T19:05:53Z

- Exact checkpoint branch / HEAD: `codex/mounted-combat-feasibility` / `e3f71bc902d79c5be3f1a66c6b99396d94d39018`.
- Working status at checkpoint: final planning/report/blocker/resume/journal updates are in progress; production/runtime code is committed and clean at the checkpoint HEAD.
- Active version: `0.0.1-feasibility`.
- Last successful gate: two consecutive fresh-process `mod-load-smoke` runs from exact commit/package, each with exact platform/no-save assertions and live Mods restoration.
- Current blocker: zero exact KMC baseline candidates and zero exact KMC working candidates; mission §26.2 prevents any fixture-backed load or movement/lifecycle/visual qualification.
- Exact files being changed: `AUTONOMOUS-BLOCKERS.md`, `AUTONOMOUS-RESUME.md`, `MOUNTED-COMBAT-JOURNAL.md`; all five `docs/PHASE-*.md` reports; and `planning/ARCHITECTURE-OPTIONS.md`, `ASSEMBLY-CONTRACT-MATRIX.md`, `ASSET-RIG-ANIMATION-INVENTORY.md`, `KINGMAKER-WRATH-TYPE-MAP.json`, `MOUNTED-SUBSYSTEM-DEPENDENCY-GRAPH.md`, `REFERENCE-PROVENANCE.md`, `RISK-AND-KILL-CRITERIA.md`, and `RUNTIME-SCENARIO-MATRIX.md`.
- Exact next command once project-owned fixtures are made available: `Get-ChildItem -LiteralPath 'C:\Users\Howie\AppData\LocalLow\Owlcat Games\Pathfinder Kingmaker\Saved Games' -File | Where-Object Name -Match '^Manual_[0-9]+_KMC_AUTOMATION_(BASELINE|WORKING)\.zks$' | Select-Object Name,Length,LastWriteTimeUtc`.
- Analysis profile: exact bounded decompilation remains only under ignored `analysis-cache`; no proprietary source/assets are committed or packaged.
- Runtime profile: scenario-row result 1 PASS / 0 FAIL / 24 DEFER — EVIDENCED; live process attempts 2 PASS / 2 repaired FAIL; movement samples 0. Evidence IDs and transactions are preserved under ignored lab runtime roots.
- Package profile: diagnostic ZIP SHA-256 `2c77b677bc4af129ccc9e22d136b6e21754007e8ba8144ee2a2304d77fac10b9`; DLL SHA-256 `fb3651cd1a32148a0d897ce69dc1834ed94a261ccd9ac85639feacbc89bd4237`; MVID `52ed4032-aa4b-4f9d-b2a9-97771fd72c52`; package binds commit `e3f71bc902d79c5be3f1a66c6b99396d94d39018`.
- External state: all four live transactions are durably restored; live Mods digest `e62320bbe7d4b83edc128a62f9f5852b0669c5549859602c335c649318578d47`; save metadata digest `b7ebd08e982f2af3d529ed775fca5893838d1a47f767f0c5116daf11bb586f14`; no lock/sentinel/game process; unrestored external state: none.
