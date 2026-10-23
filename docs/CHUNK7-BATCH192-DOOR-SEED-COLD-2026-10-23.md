# Chunk 7 batch192: Old Sycamore door seed (cold PASS; door not applicable)

Status: **seed qualified by an isolated cold load; owner-seed exploration PASS with the door
recorded NOT APPLICABLE.** No door click ran. The 7B ordinary-door row stays **OPEN**. The
earlier s1 FAIL is preserved below.

## Seed

`KMC_C7_DOOR_SEED_OLD_SYCAMORE_OPEN`: master
`fixtures/owner-supplied/KMC_C7_DOOR_SEED_OLD_SYCAMORE_OPEN.master.zks`, SHA-256 `90789101…f20a6a`,
4,121,538 bytes, intake `…OPEN.intake01.json` `dd890da9…`, registered in `46eb4dd3`. The
attempt-2 closed-door entry stays registered as history; it was never used natively.
Selections (read-only, `analysis-cache/chunk7-continuation`): cold
`b192-door-seed-cold-selection.json` `fa3e3d01…`; exploration
`b192-door-seed-exploration-selection.json` `1cf4c8d7…` (five-member party, rider B XP
assistance to level 8, zero writes).

## s1 `20261023-c7b-world192-b192s1-doorseed-cold`: FAIL (preserved)

Candidate `46eb4dd3` (package `…c7b-world192-doorseed-diagnostic.zip` `b56494be…`, DLL
`07e1f4b8…`, MVID `9604b1ba…`; suite `chunk7b-completion-b192-doorseed-20261023` `babaf824…`;
freeze record `b192-freeze.json`). The cold scenario threw in `Chunk7StockAreaObservation.Capture`:
the observation hard-coded the Spider Cave seed area `b10e8651…`, and this seed is in the Old
Sycamore (`fb4cffc2…`). The seed's own qualification had recorded `qualified=true` before the
throw; that is not a PASS. Mods, save protection, Working and Baseline restored; no process or
lock afterwards. Evidence: `runtime-evidence/20261023-c7b-world192-b192s1-doorseed-cold`
(result `0f845668…`).

**Fix `e32c1226`** (independent review CLEAR): the cold scenario captures
`stockAreaRouteObservation` only when the loaded area is the Spider Cave seed area
(`Chunk7StockAreaObservation.AppliesTo`); other seed areas record no route claim.

Known hardening items from that review (recorded, not changed now):
- **F1.** The reader treats `stockAreaRouteObservation` as optional for every area, so a Spider
  Cave artifact that omits it is still admitted.
- **F2.** The product omits the observation without an explicit not-applicable marker.
- **F3.** No offline test exercises the skip branch (the facade's `AppliesTo` returns true);
  s1r below is its only check.

## Candidate b192b

| Item | Identity |
| --- | --- |
| Source | `e32c1226`, frozen worktree `worktrees/chunk7b-b192b-source-e32c1226`; packaged from the clean branch checkout at the same commit (`Package.ps1` needs a named branch) |
| Package | `KingmakerMountedCombat-0.1.0-chunk7b-preview.94-c7b-world192b-doorseed-diagnostic.zip`, SHA-256 `b8d87487…fce2d63`, 3,669,713 bytes; payload `Info.json` (329) + DLL (7,930,880) only |
| Manifest | `ca24cd73…`, commit `e32c1226`, clean |
| DLL | `efac5569…ee0bfb`, MVID `a69e2a1f-2819-4646-aa4c-8ff216d7691b` |
| Candidate tier | 6/0 (216 s), receipt `b192b-candidate-01.log` |
| Suite | `chunk7b-completion-b192b-doorseed-20261023`, SHA-256 `6a43af78…`; save digest `4f6375da…` (unchanged by s1), Mods digest `ed57b9d2…` |

**Root `bin\Release` DLL.** Packaging builds into the repo root. Before the b192b packaging the
root DLL was already `efac5569…` (an earlier offline build of the same source); after packaging
it is the same bytes, equal to the package DLL. The first b192 packaging had replaced the root
DLL with `07e1f4b8…`; the hash before that was not recorded. Harness tiers that copy the root
DLL into a worktree (`Test-Harness`) therefore now use the e32c1226 candidate build, which
matches the current branch source; no frozen-root assumption is broken, but the previous root
build (believed to be the batch190 `34e8ed39…` build) is no longer in place.

## s1r `20261023-c7b-world192b-b192s1r-doorseed-cold`: PASS

`chunk7-assisted-cold-inventory`, `owner-seed-readonly-inventory`, base-game only; WhatIf PASS,
then live (about 2 minutes). Outer PASS, assertions 4/0; owner-seed qualification
`qualified=true` with no failures; no `stockAreaRouteObservation` recorded (the skip branch ran).
External `Test-RuntimeResult` exit 0. Mods, save protection, Working and Baseline restored; no
process or lock afterwards. This is the seed's isolated native cold load.

## s2 `20261023-c7b-world192b-b192s2-doorseed-explore`: PASS, door NOT APPLICABLE (superseded)

**Superseded by `b113418a`.** The NOT APPLICABLE verdict stands only under the reader of its own
candidate (`e32c1226`), where that run's outer PASS and external `Test-RuntimeResult` PASS are kept.
The current reader refuses this record: static-cut doors are now admitted, so `TavernDoor`
qualifies and its recorded `eligible=false` no longer holds. The run proves no door click.

`chunk7-owned-two-pair-campaign`, `owner-seed-exploration`, base-game only, the declared
900-second budget (the runner refused 1200). WhatIf PASS, then live (about 2.5 minutes). Outer
PASS, assertions 8/0; external `Test-RuntimeResult` exit 0. Result `1fb2790f…`, world evidence
`bb8481e4…`. Mods, save protection, Working and Baseline restored; no process or lock afterwards.

Door scan at the first continuation (radius 28 m, 3 candidates, none eligible):

| Door | Open | Cut | Cut off when open | Mount distances (m) | Radius | Other flags | Why ineligible |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `TavernDoor` | yes | present | **no** | 8.45 / 7.92 | 2 | interactable, revealed, unlocked, no restriction, trap, dialog or trigger | `DisableNavmeshCutWhenOpen = false` |
| `RootsDoor` | no | present | no | 42.09 / 44.94 | 5 | one `InteractionRestriction`, never unlocked | restricted; out of range; cut not toggled |
| `MiteYardDoor` | no | present | no | 93.10 / 97.40 | 4 | not interactable, unrevealed | out of range; not clickable |

The owner's open door (`TavernDoor`) is in range and otherwise clean, but its navmesh cut is not
switched off when open. The probe's existing predicate requires that switch, because the
qualified behaviour is a mounted path through a doorway whose cut the door toggles. Whether a
door whose cut stays active when open should qualify is a design question, not decided here; the
predicate was not changed. No click, selection change or door restoration occurred.

## Next

Decided: static-cut doors qualify (`b113418a`, review CLEAR). The requirement is a real mounted
click with a selection change and exact restoration; the cut toggle was only the settle observable.
Next is a new candidate from that source and an owner-seed exploration rerun on it. The row stays
OPEN until that run's door proof passes.
