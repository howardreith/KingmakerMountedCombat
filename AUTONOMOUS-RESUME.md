# Autonomous resume

Updated: 2026-08-13T21:25:00Z

- Exact checkpoint branch / HEAD: `codex/mounted-combat-feasibility` / `92919c83a5dad6f123758b08fab9ad3e219f6423`; movement/boundary hardening is uncommitted.
- Working status at checkpoint: request-byte binding, trigger-aware cleanup, richer movement telemetry, lifecycle/movement/boundary engines, exact-token gates, tests, and durable records are modified or new. No external transaction is active.
- Active version: `0.0.1-feasibility`.
- Last successful gate: complete Release suite at `2026-08-13T21:22Z`: source 21/0, build 1/0, pure/component 54/0, harness 46/0, assembly-backed 47/0 (Kingmaker 36, Wrath 11).
- Current failure/hypothesis: exact filename audit is baseline=1 / Working=0. Canonical baseline is `Manual_298_KMC_AUTOMATION_BASELINE.zks`; rejected near-match is `Manual_299_KMC_AUTOMATION_WORKING_.zks`. No real save archive was opened because the prefilter failed. In addition, bounded exact `LoadRoutine`/DotNetZip review proves that a crash can leave a temp/sidecar/duplicate Working artifact; all schema-v2 launches remain gated until token-owned artifact-family recovery is offline-qualified.
- Exact files being changed: runtime host, movement telemetry, lifecycle/movement/boundary scenario engines, trigger-aware relationship runtime/domain contracts, exact assembly/harness tests, launcher request binding, production project, journal/resume.
- Exact next command: commit the movement/boundary checkpoint, then implement and fault-test token-owned Working-slot-family artifact recovery. Before any real fixture use rerun `Get-ChildItem -LiteralPath 'C:\Users\Howie\AppData\LocalLow\Owlcat Games\Pathfinder Kingmaker\Saved Games' -File | Where-Object Name -Match '^Manual_[0-9]+_KMC_AUTOMATION_(BASELINE|WORKING)\.zks$' | Select-Object Name,Length,LastWriteTimeUtc`.
- Analysis profile: exact bounded decompilation remains only under ignored `analysis-cache`; no proprietary source/assets are committed or packaged.
- Runtime profile: no resumed launch or Mods/save transaction. Scenario-row result remains 1 PASS / 0 FAIL / 24 DEFER — EVIDENCED; live process attempts remain 2 PASS / 2 historical repaired FAIL; movement samples remain 0. Lifecycle, movement, and boundary engines are implemented but not runtime-qualified.
- Package profile: prior qualified diagnostic ZIP SHA-256 `2c77b677bc4af129ccc9e22d136b6e21754007e8ba8144ee2a2304d77fac10b9`; its DLL SHA-256 is `fb3651cd1a32148a0d897ce69dc1834ed94a261ccd9ac85639feacbc89bd4237`, MVID `52ed4032-aa4b-4f9d-b2a9-97771fd72c52`, bound to code commit `e3f71bc902d79c5be3f1a66c6b99396d94d39018`. Current dirty DLL is not a qualification artifact.
- Publication profile: last guarded publication/local-remote equality was `907271af66be134891647756d08aee0ee3954b84`; current branch is ahead and must eventually publish only through the guarded helper.
- External state: all prior live transactions remain durably restored; live Mods digest `e62320bbe7d4b83edc128a62f9f5852b0669c5549859602c335c649318578d47`. Filename metadata only was read; no archive opened, process launched, lock/sentinel created, or save/Mods state changed. Unrestored external state: none.
