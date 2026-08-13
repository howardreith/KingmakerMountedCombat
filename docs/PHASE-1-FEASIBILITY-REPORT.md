# Phase 1 feasibility report

Status: BLOCKED — CRITICAL

## Outcome

The contract-first, scaffold, and offline implementation portions of Phase 1 are complete. Exact local evidence supports a bounded Architecture B experiment—authoritative mount movement plus rider logical/view proxy—but the movement vertical slice cannot be qualified because the exact filename audit reports one canonical baseline and zero canonical Working candidates. The baseline is `Manual_298_KMC_AUTOMATION_BASELINE.zks`; `Manual_299_KMC_AUTOMATION_WORKING_.zks` is a rejected trailing-underscore near-match and provides no authority to inspect, rename, or load it. No architecture kill criterion has fired, so neither `PROCEED RECOMMENDED` nor `PIVOT RECOMMENDED` would be truthful.

## Exact targets

- Kingmaker: Steam app `640820`, build `6757524`, product `2.1.7b`; `Assembly-CSharp.dll` 7,262,208 bytes, SHA-256 `3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`, MVID `07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`.
- Installed UMM: `0.28.2.0` (materially different from the mission's expected 0.32.x), SHA-256 `75b96e25a3a9fbadb47dd14a4ab490cb8c98143a6242aff3bba6145cd3047f39`.
- Harmony12: `1.2.0.1`, SHA-256 `aa1cd48317254985d8b700cc74953477d1b40c3022ce9aa4c95ed2b8327e1292`.
- Kingmaker Unity core: SHA-256 `3a76df7f709d465e3273502e08edbffb536b1c2f78c3a132b8668e59fddd2803`, MVID `bd5ffe06-494e-4588-a068-c8443cc48c47`.
- Wrath read-only reference: Steam product `2.7.0x`; `Assembly-CSharp.dll` 11,891,712 bytes, SHA-256 `2cb7160b7154d4ffacc77b9c51b1eb26199e1294300f04fdfc073367b2ef8953`, MVID `90a9869c-2792-4c7b-bfb7-5a8b33da7c82`; Unity core SHA-256 `b040598ec99a249779661605123e1a09aeb845c2d7cf623940ab07a775440365`.

Read-only source references: TabletopTweaks-Base `221decc02265c780685650ea987bca0a9eeaf49a` (MIT); AutoMount `59bd24fd49d9e4868e2b88d8d0019f21e6f69cf3` (MIT-form text with placeholder notice, no copying); DragonFixes `cf74881ebc2e3762bdab720275fc903d44377764` (MIT); pathfinder-wotr-multiplayer `6402b9ba9dd503dfb6054e595919fbd8992c4a21` (no local license, observation only); harness snapshot source `c06793d2238577093b96a2dc3172839070e7d69a` (no included license, concepts only). No reference source is reused in production.

## What Wrath actually does

Wrath's mounted subsystem is cross-cutting engine behavior, not a cosmetic `Mount()` call. Reciprocal serialized rider/saddled parts own the relationship; `SaddledUnitController` delegates rider intent to an ordinary mount command; rider path approach is suppressed; rider avoidance is disabled; `UnitMoveController` copies mount position/orientation to rider entity and view every tick; selection and formation exclude/redirect the mount; stop/hold/target/action handling recognizes the pair; save load repairs orphan halves. Ordinary hostile attacks against a rider are retargeted to the mount. This is not Architecture A with two unconstrained actors.

## Kingmaker equivalence and required work

Kingmaker has no rider part, saddled part, mounted controller, paired command, mounted IK, or mounted persistence repair. It does have usable narrow primitives:

- exact active pet/master, controllability, size, combat, and view contracts for pair validation;
- mount stock pathfinding/footprint and rider `AgentOverride`;
- reference-counted `AvoidanceDisabled` and reversible stock-agent state;
- exact ground-command recipient, selection, stop/hold, continuous-movement, save/load, and lifecycle seams;
- normal entity/view commit and a pair-local `LateUpdate` presentation adapter.

Relationship state, command projection, rider synchronization, cleanup ownership, telemetry, and visual attachment therefore require original implementation. The default-off relationship, fixture-bound protocol, exact descriptor guard, Working-only transaction recovery, lifecycle/movement/boundary scenario engines, and evidence-manifest binding are implemented and offline-qualified. Combat/action coupling is deliberately simplified to immediate cleanup. Persistence is rejected for Phase 1. The filename gate stopped before any real descriptor or archive was opened, so none of this current implementation is live-qualified.

Coverage is recorded as 21 responsibility rows in the assembly matrix, 15 machine-readable map entries, and 11 dependency-closure concerns (10 PASS, 1 view/presentation IN PROGRESS). The current offline gate is source validation 21 PASS / 0 FAIL, build 1 PASS / 0 FAIL, pure/component 56 PASS / 0 FAIL, guarded harness/protocol 58 PASS / 0 FAIL, and assembly-backed qualification 47 PASS / 0 FAIL (Kingmaker 36, Wrath 11).

## Candidate and presentation

`CR1_HorseRiding` is native, Large, and includes stirrups, but exact reverse references prove it is not an animal companion; it was rejected. The only selected candidate is `AnimalCompanionUnitMammoth` (`e7aa96d15a45238438ae4cfb476f6bb9`) through its exact `AddPet` feature. It is normally Large at companion rank 7; current runtime size remains authoritative. Its native `Spine` is only a bounded anchor hypothesis. No rider stable ID/body/skeleton, offset, animation stability, clipping, or visual classification was obtainable.

## Architecture disposition

Static weighted scores are A/B/C/D = `41/66/77/89`. A is rejected for Phase 2 because it conflicts with exact Wrath behavior and requires an unsafe cross-cutting surface. B is the only provisional Phase 1 experiment because its scoped seams exist. C and D remain pivots if B later fails a kill criterion. No final Phase 2 architecture recommendation is issued without pair runtime evidence.

## Runtime and safety evidence

The diagnostic mod loaded in two consecutive fresh Kingmaker processes from commit `e3f71bc902d79c5be3f1a66c6b99396d94d39018`. Each proved exact game/UMM/Harmony/KMC identity, `Unmounted`, experiment disabled, no loaded area, zero save/load requests, no errors, stable exit, unchanged protected-save metadata, and exact live Mods restoration. Two earlier FAIL runs exposed and then regression-tested JSON, UMM cache, and post-exit race defects; guarded recovery restored the transaction-before Mods tree after each.

No mounted pair was created. Movement samples, maximum residual/drift, doorway control, corners, formation, selection, lifecycle cleanup residue, save/load behavior while mounted, and visual presentation are not measured.

## Critical blocker

The exact filename audit reports one canonical baseline, `Manual_298_KMC_AUTOMATION_BASELINE.zks`, and zero canonical Working candidates. The only Working-looking filename is the rejected `Manual_299_KMC_AUTOMATION_WORKING_.zks` near-match. Foreign KBP/KMG fixtures are prohibited, filenames alone do not establish internal identity, and neither KMC archive nor any valued archive was opened. The implemented descriptor guard therefore cannot inspect or durably qualify the fixture pair. Mission §26.2 stops the investigation before fixture-backed runtime work because baseline and Working identity cannot be distinguished safely.

Load-bearing details: `planning/ASSEMBLY-CONTRACT-MATRIX.md`, `planning/MOUNTED-SUBSYSTEM-DEPENDENCY-GRAPH.md`, `planning/KINGMAKER-WRATH-TYPE-MAP.json`, `planning/ASSET-RIG-ANIMATION-INVENTORY.md`, and `planning/RISK-AND-KILL-CRITERIA.md`.
