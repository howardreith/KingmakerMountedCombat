# Autonomous resume

Updated: 2026-08-13T20:57:34Z

- Exact checkpoint branch / HEAD: `codex/mounted-combat-feasibility` / `85ea08113eb875150edd1511f045f226f7a913d0`; the save-backed host/launcher/lifecycle checkpoint is uncommitted.
- Working status at checkpoint: schema-v2 host/launcher/recovery, save authorization counters, Working loader, Pause-safe runtime/lifecycle hooks, lifecycle engine, tests, journal/resume/blocker updates are modified or new. No external transaction is active.
- Active version: `0.0.1-feasibility`.
- Last successful gate: complete Release suite at `2026-08-13T20:55Z`: source 21/0, build 1/0, pure/component 54/0, harness 46/0, assembly-backed 33/0.
- Current blocker: exact filename audit is baseline=1 / working=0. Canonical baseline is `Manual_298_KMC_AUTOMATION_BASELINE.zks`; rejected near-match is `Manual_299_KMC_AUTOMATION_WORKING_.zks`. No real save archive was opened because the prefilter failed.
- Exact files being changed: schema-v2 runtime schema; runtime launcher/recovery/common/validator and harness tests; composition root; runtime host/save authorization/Working loader/lifecycle engine; relationship service/runtime/lifecycle subscriber; production project; save-authorization tests; durable records.
- Exact next implementation action while the filename remains blocked: commit the orchestration/lifecycle checkpoint, then implement bounded movement/selection/formation/doorway telemetry and screenshot capture. Exact first external command before any real fixture use remains `Get-ChildItem -LiteralPath 'C:\Users\Howie\AppData\LocalLow\Owlcat Games\Pathfinder Kingmaker\Saved Games' -File | Where-Object Name -Match '^Manual_[0-9]+_KMC_AUTOMATION_(BASELINE|WORKING)\.zks$' | Select-Object Name,Length,LastWriteTimeUtc`.
- Analysis profile: exact bounded decompilation remains only under ignored `analysis-cache`; no proprietary source/assets are committed or packaged.
- Runtime profile: no resumed launch or Mods/save transaction. Scenario-row result remains 1 PASS / 0 FAIL / 24 DEFER — EVIDENCED; live process attempts 2 PASS / 2 repaired FAIL; movement samples 0. The save-backed lifecycle engine is implemented but not runtime-qualified. Total harness is 46/0.
- Package profile: prior qualified diagnostic ZIP SHA-256 `2c77b677bc4af129ccc9e22d136b6e21754007e8ba8144ee2a2304d77fac10b9`; its DLL SHA-256 is `fb3651cd1a32148a0d897ce69dc1834ed94a261ccd9ac85639feacbc89bd4237`, MVID `52ed4032-aa4b-4f9d-b2a9-97771fd72c52`, bound to code commit `e3f71bc902d79c5be3f1a66c6b99396d94d39018`. Current dirty DLL is not a qualification artifact.
- Publication profile: last guarded publication/local-remote equality was `907271af66be134891647756d08aee0ee3954b84`; current branch is ahead and must eventually publish only through the guarded helper.
- External state: all four prior live transactions remain durably restored; live Mods digest `e62320bbe7d4b83edc128a62f9f5852b0669c5549859602c335c649318578d47`. Filename metadata only was read; no archive opened, process launched, lock/sentinel created, or save/Mods state changed. Unrestored external state: none.
