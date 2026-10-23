# Chunk 7 batch192: Old Sycamore door seed (cold PASS; static-cut door click PASS in 192d)

Status: **seed qualified by an isolated cold load (s1r); static-cut door click PASS in batch192d.**
The 7B ordinary-door row is a development PASS; exact-package qualification is 7E. Preserved below:
the s1 FAIL, s2's superseded NOT APPLICABLE and batch192c's outer FAIL.

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

## Batch192c: static-cut door click (game PASS, outer FAIL on evidence serialization)

Candidate `06653762` (static-cut admission `b113418a`, review CLEAR, plus a comment-only edit):
frozen worktree `worktrees/chunk7b-b192c-source-06653762`; package
`…c7b-world192c-doorstatic-diagnostic.zip` `a010b6b7…`, 3,670,149 bytes (`Info.json` 329 + DLL
7,931,904 only); manifest `9fca79a1…`; DLL `6b310af0…`, MVID `0b5e2f26-c0b3-4507-b8e9-0642e74e787f`;
Candidate tier 6/0 (225 s); suite `chunk7b-completion-b192c-doorstatic-20261023` `40330450…`. The
root `bin\Release` DLL was `6b310af0…` before and after packaging (the Fast tier had already
built the same source), equal to the package DLL.

No same-candidate cold load was run: the exploration selection's exact property set binds only the
registry seed (ID and SHA-256), and since s1r's `e32c1226` the only product change is the door probe.

**c1 `20261023-c7b-world192c-b192c1-doorstatic-explore`.** WhatIf PASS, then live (about 2.7 min).
Game result **PASS**; outer result **FAIL** (0/1): "native door terminal outcome property set is not
exact. Actual: $id". The probe wrote each door outcome with bare `JObject.FromObject`, which in the
game uses the global `JsonConvert.DefaultSettings` and produced an empty `{"$id":"1"}` shell (a known
serialization fact). The probe had already required every outcome value in game before writing it
(Success, one interaction, one delegated approach, door state changed, rider path suppressed, mount
Move slot restored), but the record cannot show them, so this is not a door PASS. External
`Test-RuntimeResult` (current reader) refuses it for the same reason. Result `bf8dbd30…`, world
evidence `a9f3c68a…`. Mods, save protection, Working and Baseline restored; no process or lock
afterwards.

What the record does show:

| Item | Observed |
| --- | --- |
| Door | `TavernDoor`, `cutMode` static, captured open, enabled, cut enabled, already unlocked, radius 2 m |
| Pair 0 | rider `73681ef2…` executed command `-1466112384` from 7.12 m, moved 5.12 m; selection afterwards rider B `fd213210…` |
| Pair 1 | rider `fd213210…` executed command `-787315200` from 11.80 m, moved 9.86 m; selection afterwards rider A |
| Setup closes | 2 (open start); first pair returned to its start (0.04 m, Success) |
| Cut observations | enabled at all six boundaries: captured (open), input-ready-0 (closed), after-click-0 (open), input-ready-1 (closed), after-click-1 (open), restored (open) |
| Restoration | exact: open, enabled, cut enabled, unlock flag true (observed true before restore) |

**Fix (offline, needs review and a new candidate).** The probe now writes the ten outcome members
explicitly; a source test refuses any bare `JObject.FromObject` in the probe and requires each
member. `Test-Chunk7WorldEvidence` 252/0, `Test-Chunk7DoorCleanupAdapter` 35/0, Fast 3/0, Release
build clean.
## Batch192d: static-cut door click PASS

Candidate `20c4ff91` (explicit outcome serialization, review CLEAR): frozen worktree
`worktrees/chunk7b-b192d-source-20c4ff91`; package `â€¦c7b-world192d-doorstatic-diagnostic.zip`
`a6d4678fâ€¦`, 3,670,210 bytes (`Info.json` 329 + DLL 7,932,416 only); manifest `3253d1daâ€¦`; DLL
`b2acbdebâ€¦`, MVID `b91b23f4-fdcd-461d-8674-fef260161042`; Candidate tier 6/0 (217 s); suite
`chunk7b-completion-b192d-doorstatic-20261023` `c7f77a4eâ€¦` (save and Mods digests unchanged). The root
`bin\Release` DLL was `b2acbdebâ€¦` before and after packaging, equal to the package DLL.

**d1 `20261023-c7b-world192d-b192d1-doorstatic-explore`: PASS.** WhatIf PASS, then live (about 2.6
min). Outer PASS, assertions 9/0; external `Test-RuntimeResult` (current reader) PASS. Result
`29ede123â€¦`, world evidence `28d4c2b7â€¦`. Mods, save protection, Working and Baseline restored; no
Kingmaker process and no transaction lock afterwards.

| Item | Pair 0 | Pair 1 |
| --- | --- | --- |
| Rider / mount | `73681ef2â€¦` / `a6c7a463â€¦` | `fd213210â€¦` / `75f28eb9â€¦` |
| Click command / executor | `-121411968` / rider `73681ef2â€¦` | `557385216` / rider `fd213210â€¦` |
| Distance before / moved | 6.53 m / 4.55 m (radius 2 m) | 9.34 m / 7.38 m |
| Selection after input | rider B `fd213210â€¦` | rider A `73681ef2â€¦` |
| Outcome | Success; 1 interaction; 1 delegated move start, 0 delegated ticks; door state changed; rider path suppressed; mount Move slot restored; no terminal reason | the same |

Door `TavernDoor`, `cutMode` static, captured open, enabled, cut enabled, already unlocked; two
setup closes; the first pair returned through its own mount's command (0.05 m, Success). The cut
was enabled at all six boundaries (captured open, input-ready-0 closed, after-click-0 open,
input-ready-1 closed, after-click-1 open, restored open). Restoration exact: open, enabled, cut
enabled, unlock flag true (observed true before restore).

This is development evidence for the 7B ordinary-door row on a diagnostic candidate: a real
mounted click on an ordinary door by each pair, with a selection change and exact restoration.
Exact-package qualification belongs to 7E.

**Known items from the 20c4ff91 review (recorded, not fixed now):**
- `Chunk7CombatCleanup.cs:40` uses a bare `JObject.FromObject` on a Dictionary.
- `Chunk7OwnedPairFixture.cs:704` `Key(RuntimeRequest)` uses a bare `FromObject`; `RuntimeRequest`
  is public, so probably fine, but unverified natively.
- The empty-shell behaviour was observed for an internal type; the wording elsewhere states it as
  general.
- The new source test's regex is narrow (it matches only a single-argument call without nested
  parentheses).
## Next

Batch192d passed the door proof on the current reader. The 7B ordinary-door row is a development PASS;
exact-package qualification is part of 7E.
