# Phase 2 final closure

Recorded: `2026-08-25T12:00:39Z`

Disposition: Mammoth private-alpha foundation technically qualified and human accepted.

This closes Phase 2. It does not create a public release, claim full native attack integration, claim Wrath parity, or accept future horse work before its own evidence and visual review.

## Accepted product identity

| Field | Value |
|---|---|
| Repository | `howardreith/KingmakerMountedCombat` |
| Closure branch | `codex/mounted-combat-phase2-closure` |
| Version | `0.1.0-phase2b-dev.1` |
| Accepted implementation commit | `1241222459209aea1e6127bedd7d630df3940b99` |
| Accepted packaging/documentation commit | `49c970f9445d435c530a8ce949bdfddee4e1ef03` |
| Human-acceptance record commit | `ac57acd53a315f9c27d32288332aa342b95cb6cf` |
| Immutable package | `C:\Dev\KingmakerMountedCombatLab\artifacts\KingmakerMountedCombat-0.1.0-phase2b-dev.1-round2-manual-regression.zip` |
| Package SHA-256 | `de25bb753a7b902206e474a59d23fe1474b4a7c631aea19caa678fd12962a427` |
| Manifest SHA-256 | `d6992dc8e28431512477f8ace5bdec8c05ada57b690f502aaedb5b94ab618cd8` |
| DLL SHA-256 | `41680c98b90f1d941693e2b42d01c58cbc09f7b197f842acccb2673b4eb630ba` |
| DLL MVID | `2dc5c5b3-559b-4e54-870f-8c76a911ec08` |
| `Info.json` SHA-256 | `d81524ab54de7f4b22ad815ef3e69e4718d13da8c7dc4f328c02a589a35d8003` |
| Package contents | `KingmakerMountedCombat/Info.json`; `KingmakerMountedCombat/KingmakerMountedCombat.dll` |

The closure checkpoint is documentation-only. The final closure-branch commit and guarded remote equality are recorded in a non-circular external closure identity after the commit is created. No post-acceptance source change is included in Phase 2.

## Qualified automated behavior

The final applicable offline gates passed source `21/0`, Release build, component `240/0`, visual/source-order `17/0`, harness/protocol `227/0`, assembly contracts `299/0` (`288/0` Kingmaker and `11/0` exact local Wrath reference), PowerShell parser `26/0`, JSON parser `7/0`, diff check, and prohibited-payload check.

Credited runtime evidence totals `443/0`:

| Runtime row | Mode | Result |
|---|---|---|
| `20260824T132154Z-round2-v52-navmesh-readiness-suite2-rt-passA` | RT A | `81/0` |
| `20260824T134633Z-round2-v52-navmesh-readiness-suite2-rt-passB` | RT B | `81/0` |
| `20260824T141121Z-round2-v52-navmesh-readiness-suite2-tb-passA` | TB A | `111/0` |
| `20260824T143535Z-round2-v52-navmesh-readiness-suite2-tb-passB` | TB B | `111/0` |
| `20260824T233600Z-round2-final-tile-refresh-distance-door` | Distant door | `59/0` |

Together with the earlier Phase 2 matrices, this evidence qualifies:

- RT mounted movement and supported Rider primary melee;
- controllable Mammoth and rider turn-based turns, with rider movement delegated through the Mammoth;
- supported Rider primary melee in turn-based mode;
- coherent RT/TB transitions, selection, portrait, action bar, camera, view, attachment, and pose telemetry;
- independently owned Mammoth primary attack and resource ledger;
- visible deterministic mounted-ranged rejection and stock unmounted-ranged control;
- Wild Shape clean dismount and transformed/reverted view continuity;
- menu pair continuity under the instrumented surfaces;
- Mammoth-only distant-door approach, one rider-owned stock interaction, stock navmesh traversal, target reach, and exact cleanup;
- explicit Dismount and intentional save/load/area transient clean-dismount behavior;
- absence of duplicate movement, command, turn, attack, roll, damage, interaction, opportunity, or resource chains in the qualified rows;
- exact save, Mods, Baseline, Working, process, lock, sentinel, and transaction restoration at every credited audit boundary.

## Human-confirmed behavior

The user exercised the installed package and confirmed mounting, dismounting, movement, menus, Wild Shape/revert visibility, distant-door approach/open, save/load/area clean dismount, TB rider-turn delegation and controllability, both KMC primary controls, and no observed duplicate command/attack/damage behavior. The exact record is `docs/PHASE-2-HUMAN-ACCEPTANCE.md`.

Human acceptance supplies the ordinary gameplay and presentation judgment that internal ownership fields cannot prove. It does not broaden the supported control model.

## Accepted limitations and unimplemented features

The accepted limitations are enumerated in `docs/PHASE-2-KNOWN-LIMITATIONS.md`. In particular, stock attack/right-click and mounted auto-attack are absent; the overlay controls are required; rider and Mammoth retain separate TB turns; Mammoth TB locomotion may slide; mounted ranged combat is absent; mounting is transient; and Mammoth presentation is private-alpha quality.

Persistent mounting, automatic remount, Small riders, mounted spellcasting, mounted feats, explicit mounted attacks of opportunity, mounted charge, enemy riders, and additional mount species were not implemented in Phase 2.

## Historical failed evidence

All failed packages, observations, and evidence remain preserved and uncredited. The final door investigation established that five raw native path-object replacements correlate with stock `TileHandler.LastUpdateFrame` advancement while the reference-identical command remains healthy and reaches the target. Raw identity replacement is retained as telemetry and retired as a standalone product-failure gate. Failure-correlated or unattributed churn remains fatal.

No historical failed run is rewritten as PASS by the human acceptance.

## External state

The final automated audit passed before evidence inspection and proved exact protected save, Baseline, Working, Mods, transaction, process, lock, sentinel, and staging restoration. The accepted package was subsequently installed through the guarded deployment helper for manual regression. That exact deployment remains intentionally present and byte-verified; it is an installed test artifact, not restoration residue.

The first closure-gate harness invocation detected the user's still-open responsive Steam-parented Kingmaker process and failed closed before any live runtime transaction. The test runner was stopped and the user exited Kingmaker normally. A fresh read-only check then proved zero relevant process and zero live lock, sentinel, staging deployment, nonterminal transaction, or Git operation. The clean harness rerun passed `227/0`; the interrupted invocation is uncredited.

## Handoff

Phase 2 integration into `main` is authorized through a merge-preserving pull request. No public release is authorized. The horse branch must start from the exact integrated closure commit, or from the exact published closure commit only if local policy blocks main integration. Horse work must retain the Mammoth implementation as its regression baseline and use an independent horse profile.

Local policy rejected the authenticated GitHub PR/merge workflow before any remote mutation. At that boundary, remote `main` was `72dcbeb19d03985509f1ed71d3550dfb74f0ac15`, exactly the closure merge base, and the guarded-published closure head was `a135fc7553b9e2a5cc82ae2181e9cd5198f57afb`. No unexpected divergence was present. The final policy-record commit is bound in the external closure identity after publication.

The exact pending integration procedure is:

1. Create a pull request with base `main` and head `codex/mounted-combat-phase2-closure`.
2. Reverify the exact final closure SHA from the external closure identity and verify `main` has not unexpectedly diverged from the recorded base.
3. Merge with **Create a merge commit**. Do not squash, rebase, force, delete historical branches, or create a release.
4. Verify the exact closure commit is an ancestor of the resulting remote `main` and that all closure documents are present.

Per the authorized fallback, `codex/mounted-combat-phase3-horse` is created from the exact final guarded-published closure commit and does not wait on the policy-blocked merge.

Phase 2 status: `PASS` — private-alpha foundation accepted and closed.
