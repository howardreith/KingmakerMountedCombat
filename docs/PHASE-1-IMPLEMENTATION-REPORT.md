# Phase 1 implementation report

Status: BLOCKED — CRITICAL

## Delivered, default-off diagnostic implementation

- Independent net47/C# 7.3/AnyCPU UMM mod identity: `KingmakerMountedCombat` / `KingmakerMountedCombat.dll`, version `0.0.1-feasibility`.
- Explicit relationship coordinator with `Unmounted`, `Validating`, `Mounting`, `Mounted`, `Dismounting`, `Faulted`, and `Disposed` states; one-pair invariant; rollback; idempotent/best-effort cleanup; retryable residue ownership.
- Exact Mammoth-only Kingmaker adapter requiring a distinct Medium controllable rider, exact reciprocal active companion, conscious/alive pair, larger current mount size, default game mode, no combat, valid views/stock agents, and no pre-existing override.
- Mount-authoritative movement adapter: rider stock movement stopped/disabled, one owned avoidance lease, project-owned rider override, `Spine.TransformPoint(offset)` plus anchor rotation, entity/view synchronization, and residual telemetry.
- Eight exact-token/MVID-gated Harmony12 entry/control guards for ground command routing, selection, stop/hold, continuous movement invalidation, and save/load cleanup. No global movement-tick replacement exists.
- EventBus and UMM lifecycle cleanup for combat, death/life state, area unload, view replacement, disable, unload, session stop, and exceptions.
- Diagnostic UI actions and settings. The unsafe movement experiment defaults off; no combat bonuses, attacks, charge, feats, AI riders, persistence, or automatic remount were added.
- Schema-v2 request/result contracts bind exact baseline/Working descriptors, campaign and area identity, operation counters, named mission rows, and a validated evidence-manifest SHA. Schema v1 remains isolated for the no-save smoke.
- A filename-first fixture guard, exact internal-header validator, Working-only write allowlist, token-scoped Working transaction, bounded DotNetZip artifact-family quarantine, and idempotent recovery preserve baseline immutability and exact Working restoration.
- Frame-driven lifecycle, movement, selection, formation, doorway-control, and boundary engines emit structured telemetry and bounded evidence. They are offline-qualified but have not run against a real pair.
- Deterministic component runner, assembly-contract verifier, source/package validators, clean build/package scripts, and original guarded runtime harness.

## Harness implementation

The harness binds request/result to branch, commit, version, DLL SHA/MVID, exact platform hashes, run token, process identity, timestamps, fixture identities, and evidence-manifest hash. It freezes and validates the package; pre-stages outside live Mods; uses an exclusive JSON lock plus token-matched live sentinel; verifies the original backup before mutation; quarantines loader-created files; waits for a stable zero-process interval; restores in `finally`; verifies live Mods and authorized Working content plus protected-save metadata; rejects stale/unknown state; never kills processes; and supports a proven zero-mutation WhatIf path. Exact filename, path, header, GameId, GameName, Area, link/reparse, baseline-immutability, and Working-write checks fail closed before a load. Current fixture recovery and save-backed orchestration are qualified only with synthetic/offline cases because the canonical Working filename is absent.

UMM creates a byte-identical `.cache` beside the project DLL. Runtime-mutated sentinel-owned trees are preserved in per-run quarantine and never treated as the original backup. Two early smoke failures exposed this behavior, Owlcat's global Newtonsoft settings, Unity's unusable `Application.version`, and a post-exit process race. Each defect has a regression test or guarded scenario; both failed transactions were exactly recovered.

## Verification

- Source validation: 21 PASS / 0 FAIL.
- Clean Release build: PASS.
- Pure/component tests: 56 PASS / 0 FAIL.
- Guarded harness/protocol aggregate: 58 PASS / 0 FAIL.
- Assembly-backed checks: 47 PASS / 0 FAIL (Kingmaker 36, Wrath 11).
- Package validation: 10 PASS / 0 FAIL.
- WhatIf purity: PASS.
- Final fresh-process scaffold smoke: 2 PASS / 0 FAIL from the same commit/package.
- Historical repaired runtime attempts: 0 PASS / 2 FAIL, preserved as evidence.

Prior no-save-qualified diagnostic artifact: `C:\Dev\KingmakerMountedCombatLab\artifacts\KingmakerMountedCombat-0.0.1-feasibility-diagnostic.zip`, SHA-256 `2c77b677bc4af129ccc9e22d136b6e21754007e8ba8144ee2a2304d77fac10b9`; manifest SHA-256 `77cbbeac8c06ebcb39b1e4ea9ecec5aa2675d93dd32a54225e28d539515248f1`; packaged DLL SHA-256 `fb3651cd1a32148a0d897ce69dc1834ed94a261ccd9ac85639feacbc89bd4237`, MVID `52ed4032-aa4b-4f9d-b2a9-97771fd72c52`. It contains exactly project-owned `Info.json` and `KingmakerMountedCombat.dll` and binds source commit `e3f71bc902d79c5be3f1a66c6b99396d94d39018`. It proves the earlier no-save scaffold only; it does not package or runtime-qualify the current fixture guard, recovery, protocol, or scenario-engine hardening.

## Not delivered or claimed

No pair was mounted in a live campaign. There is no movement, drift, doorway, corner, formation, selection, mounted cleanup, save/load, area-transition, or visual proof. The code is a default-off feasibility prototype, not mounted combat and not a public release.

The exact blocker is canonical filename identity: one baseline candidate exists, but no canonical Working candidate exists; the trailing-underscore near-match is rejected. The KMC-only descriptor guard, schema-v2 host, Working transaction/recovery, and scenario engines are implemented and offline-qualified. They deliberately fail before archive inspection or launch until the filename audit reaches exactly one candidate per role with no KMC-looking near-match, after which the guard must still prove distinct paths, exact internal names, matching GameId/GameName/Area, baseline immutability, and Working-only authorization.
