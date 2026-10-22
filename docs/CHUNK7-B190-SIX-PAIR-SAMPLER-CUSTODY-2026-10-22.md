# Chunk 7 — six-pair sampler custody at window boundaries (batch190 preparation)

Status: **IN PROGRESS.** Offline implementation, tests and review only. No native run,
package or suite is recorded here. The standing completion mission remains active.

## Why

[Batch188](CHUNK7-BATCH188-SIX-PAIR-LIFECYCLE-PASS-2026-10-22.md) recorded a per-frame KMC
Update mean of about 5 ms in every state, including zero active pairs (4.99 ms at zero,
5.27 ms at six). Product pair and turn phases accounted for at most 0.19 ms. The six-pair
observer ran full inherited custody (`inherited.RequirePreserved()`) at the top of every
frame, and again inside the per-frame sample check. That call re-hashes the 183 KB staged
source receipt and rebuilds the roster and progression JSON. It runs inside the timed
update: `CompositionRoot.UpdateCore` calls `runtimeAutomation.Update`, which calls the world
engine. So the batch188 numbers measured mostly the observer, not the mod. The two-pair
matched pool already verifies full custody only at window boundaries.

## Change (diagnostics only)

`Diagnostics/Chunk7WorldScenarioEngine.SixPair.cs` is the only product file changed. No
production behavior, pair service or control changes.

- **Contract v2.** The observation is `cold-qualified-six-pair-lifecycle-v2` and declares
  `measuredFramesCarryFullCustody=false`.
- **Top of frame.** Full inherited custody runs only while no sampling window is open.
- **Sample poll.** Full custody runs before a window opens and on the frame where the window
  becomes ready. In between, the measured frames carry only the lightweight guard.
  `RequireSixOwnership(bool fullCustody = true)` keeps its default for every other caller.

One window, frame by frame (verified by the independent review against
`CompositionRoot.Update` and `MountedRuntimeMeasurement`):

| Frame | Timed | Work |
| --- | --- | --- |
| Open | No (`Current` is null when the frame starts) | Full custody twice, the before-capture, then the window opens |
| 30 warmup and 120 measured | Measured only | Lightweight guard only |
| Ready boundary | No (`BeginFrame` returns 0 once `Ready`) | Full custody, after-capture, `Complete`/`Dispose`, then the row is recorded |

A persistent change made during a window is therefore refused at the ready boundary, before
the after-capture and before the row is recorded. A window ended by the lightweight guard or
the deadline still gets full custody during cleanup.

## The limit this introduces

Inside one ~150-frame window, these facts are now checked only at the window's boundaries,
so a change that appears and fully reverts inside a single window would not be seen:

- pet/master links, Horse feature and rank facts;
- class, XP and money arrangement;
- the remote Linzi;
- party roster membership;
- request, authority and loader counts;
- the staged source receipt's hash, length and timestamp.

Still checked on every frame: area, world, actor and view identity; the 54 identity pins;
exact owners and mounted state; settledness; pause; loading, serialization and pending-write
flags; and combat. The windows are idle and issue no input, and the two-pair matched pool
already uses this design. The evidence declares the limit only through the `false` flag, so
this record states it in full.

## Reader

`scripts/runtime/Chunk7SixPairStressEvidence.ps1` stays closed:

- A **v2** match needs the `contract` property, a `[string]` value and an exact
  case-sensitive match. v2 must carry `measuredFramesCarryFullCustody` as the boolean `false`.
- Anything else must be the exact historical **v1** field set and string.
- Refused: an unknown, missing, wrong-case or non-string contract; v1 carrying the v2 field;
  v2 without it; v2 with `$true`, `'false'` or `0`.

The review re-evaluated the only real v1 artifact, batch188
(`20261022-c7b-world188-b188s1-sixslot`):

| Artifact | Reader at HEAD `ebc477ce` | New reader |
| --- | --- | --- |
| Real batch188 v1 evidence | ACCEPT | ACCEPT |
| Same evidence relabelled v2 with `false` | REFUSE | ACCEPT |
| v1 carrying the v2 field | — | REFUSE |

Batch186 and batch187 produced no world evidence. v1 remains acceptable for any candidate,
which is harmless because its label is honest and `performanceAccepted` stays false.

## Verification (offline)

| Check | Result |
| --- | --- |
| Six-pair orchestration, actual partial with native facades | 21/0 |
| Six-pair stress reader, including 6 new v1/v2 checks | 50/0 |
| Six-pair siblings | 8/0 |
| World cleanup adapter | 88/0 |
| Host-owned engines | 62/0 |
| Release build | OK |
| Fast | 3/0 (0.1 min) |
| Harness | 281/0 (3.6 min) |

The orchestration facade counts inherited-custody calls made while a window is open, and
requires zero. Four targeted product mutations were run against the final template:

| Mutation | Result |
| --- | --- |
| M1: no full custody at the ready boundary | Caught |
| M2: full custody moved after the row is recorded | Caught |
| M3: top-of-frame custody unconditional again | Caught |
| M4: full custody on every poll | Caught |

M1 and M2 survived the first version of the test. It required only "not refused while open"
and "eventually refused". The final check also requires the refusal to arrive while the
window is at its ready boundary, with no row recorded.

The independent review was CLEAR with no blockers. Its one SHOULD-FIX (the ordering above)
is applied. Its notes are recorded here: the in-window limit, the observer overhead that
remains on measured frames, and the untested hypothesis below.

## Remaining observer overhead and hypothesis

Some observer work stays inside the measured frames: the per-frame `nativeScope.RequireCurrent()`
scan of `State.Units` against the tracked actors, the 54-pin identity array and the
settledness predicates. The sample limits text already discloses this.

That the missing ~5 ms is the removed custody work is still a hypothesis. Only a native run
on a new candidate can test it. Product bytes change, so batch188's PASS does not transfer.

## Not claimed

No native run, six-pair performance measurement or acceptance, request-free
normal-player controls or HUMAN PLAY. Next: one frozen candidate (qualifier
`c7b-world190-sixmeasure`), Candidate, a new suite and WhatIf, then one zero-write six-pair
lifecycle run on the batch185 fixture.
