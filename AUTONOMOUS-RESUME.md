# Autonomous resume

Updated: 2026-08-13T19:23:33Z

- Exact checkpoint branch / HEAD: `codex/mounted-combat-feasibility` / `907271af66be134891647756d08aee0ee3954b84`.
- Working status at checkpoint: the final evidence/report commit is published with local/remote equality; only this resume record and the final publication journal checkpoint are pending their closing commit. Production/runtime code and all other reports are committed.
- Active version: `0.0.1-feasibility`.
- Last successful gate: the complete Release suite reran at `2026-08-13T19:20Z` with source 21/0, pure/component 34/0, harness 23/0, and assembly-backed 29/0; the guarded push helper then proved local/remote equality at `907271af66be134891647756d08aee0ee3954b84`.
- Current blocker: zero exact KMC baseline candidates and zero exact KMC working candidates; mission §26.2 prevents any fixture-backed load or movement/lifecycle/visual qualification.
- Exact files being changed: `AUTONOMOUS-RESUME.md` and `MOUNTED-COMBAT-JOURNAL.md` only.
- Exact next command once project-owned fixtures are made available: `Get-ChildItem -LiteralPath 'C:\Users\Howie\AppData\LocalLow\Owlcat Games\Pathfinder Kingmaker\Saved Games' -File | Where-Object Name -Match '^Manual_[0-9]+_KMC_AUTOMATION_(BASELINE|WORKING)\.zks$' | Select-Object Name,Length,LastWriteTimeUtc`.
- Analysis profile: exact bounded decompilation remains only under ignored `analysis-cache`; no proprietary source/assets are committed or packaged.
- Runtime profile: scenario-row result 1 PASS / 0 FAIL / 24 DEFER — EVIDENCED; live process attempts 2 PASS / 2 repaired FAIL; movement samples 0. Evidence IDs and transactions are preserved under ignored lab runtime roots.
- Package profile: diagnostic ZIP SHA-256 `2c77b677bc4af129ccc9e22d136b6e21754007e8ba8144ee2a2304d77fac10b9`; DLL SHA-256 `fb3651cd1a32148a0d897ce69dc1834ed94a261ccd9ac85639feacbc89bd4237`; MVID `52ed4032-aa4b-4f9d-b2a9-97771fd72c52`; package binds commit `e3f71bc902d79c5be3f1a66c6b99396d94d39018`.
- Publication profile: guarded helper `-WhatIf` and publication completed for `907271af66be134891647756d08aee0ee3954b84`; subsequent read-only helper verification showed the same remote SHA. A normal Git stderr progress record initially made the wrapper report failure after Git had already pushed; the policy helper was repaired and its SHA-256 is `57551c6c4359b4fbb8a0c5b0ba6f0e5e50553dc62c3a88e2aaa0dc92a6f72846`.
- External state: all four live transactions are durably restored; live Mods digest `e62320bbe7d4b83edc128a62f9f5852b0669c5549859602c335c649318578d47`; save metadata digest `b7ebd08e982f2af3d529ed775fca5893838d1a47f767f0c5116daf11bb586f14`; no lock/sentinel/game process; unrestored external state: none.
