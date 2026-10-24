# Chunk 7 batch190 — six-pair lifecycle with boundary custody: measurement PASS

Status: development **PASS** for one declared zero-write native scenario on one frozen
candidate. It records a representative six-pair KMC Update measurement as F11 development
evidence. `performanceAccepted` stays false. The standing completion mission remains
**IN PROGRESS**. No engineering-completion, performance-acceptance or HUMAN PLAY claim follows.

## Frozen inputs

| Input | Identity |
| --- | --- |
| Source commit | `62a68e645444ff9b4fafe6a1bd8b21e5dd7886f5` ([preparation record](CHUNK7-B190-SIX-PAIR-SAMPLER-CUSTODY-2026-10-22.md)) |
| Version / qualifier | `0.1.0-chunk7b-preview.94` / `c7b-world190-sixmeasure` |
| ZIP SHA-256 | `ee84e9333434ed2032badd34d200be3a19cd5ccdc4904b423b25c6b92f7ce417` |
| DLL SHA-256 | `34e8ed3944059e996212f4dbd2ce2a6c364cd47826f21d2ac02ddb8e55bcc577` |
| DLL MVID | `57fc82b9-f41e-44b4-b228-1315816108a3` |
| Package manifest | `d10bbb63ef5322e2c6eb47a28fc7d7410e816b89713b7d245770d2f9e4779516` |
| Suite | `chunk7b-completion-b190-sixmeasure-20261022` |
| Suite SHA-256 | `b0207210df230219a4b3200510abd9f3f807bb693b163957bd73645386571e8c` |
| Selection | unchanged batch188 `six-horse-cold-fixture-lifecycle-stress`, SHA-256 `6d064b70…060c` |

Before launch, the package was built once from the clean pushed commit. Candidate
(`-SkipBuild -AssemblyTarget Kingmaker`) passed 6/0 in 3.72 min, and the DLL was unchanged
afterwards. The suite was frozen with save and Mods digests equal to batch189's. The
development-scoped runner WhatIf passed with the unchanged loader dependency digest
`db2c3901…8921`. The run was launched as an orphaned process with an exit receipt, and no
owner action occurred.

## Run — `20261022-c7b-world190-b190s1-sixmeasure`

- **Game:** PASS 6/0 in 108.8 s and 4,563 frames; 1 load and 0 saves. Rows:
  - `C7-world-fixture`
  - `C7-six-native-mounts`
  - `C7-six-selection-routing`
  - `C7-six-cycles`
  - `C7-six-sampling`
  - `C7-world-cleanup`
- **Observation:** contract `cold-qualified-six-pair-lifecycle-v2`,
  `measuredFramesCarryFullCustody=false`, accepted by the closed reader.
- **Inputs:** 36 native Mount/Dismount casts and 0 refusals; six routed ground moves.
- **Sibling order:** one shared rider parent with 16 children. Its exact child order was verified
  after all three settled cycles.
- **Persistence:** zero save snapshots, semantic restores and presentation restores.
- **Cleanup:** settled; identity scope released. The unattended exit was not exercised.
- **Receipts:** game result `ad0dccdf0810c72d556496fafde2743b4cae9224469923e1cb7fa2c3c1935815`;
  outer result `74743d29…5c56`; evidence manifest `75b0714f…b237`; world evidence
  `5f6fb2d29f0585503fcab7d98393addfe1a00f761520d664586c66f578d7a74e`.

## Measurement (F11 development observation, not acceptance)

Fifteen windows, three per state, each 30 warmup and 120 measured frames. The table gives
the median of the three window means per state, with batch188 for comparison. Batch188 used the
same fixture, selection and lifecycle, but ran full custody on every measured frame.

| State | Active pairs | KMC Update, batch188 | KMC Update, batch190 | Pair phase, batch190 |
| --- | --- | --- | --- | --- |
| 0 | 0 | 4.99 ms | 0.294 ms | 0.003 ms |
| 1 | 1 | 5.19 ms | 0.339 ms | 0.046 ms |
| 2 | 2 | 5.20 ms | 0.404 ms | 0.102 ms |
| 3 | 6 | 5.27 ms | 0.486 ms | 0.173 ms |
| 4 | 0 | 5.08 ms | 0.290 ms | 0.003 ms |

- **The hypothesis held.** Removing full custody from the measured frames removed about 4.7 ms
  in every state. The product's pair phase is unchanged between the two runs (0.003, 0.050,
  0.108 and 0.174 ms in batch188), so the removed cost was the observer's, not the mod's.
- **Six-pair increment.** Six active pairs cost about 0.19 ms more per frame than none. The pair
  phase accounts for 0.17 ms of that.
- **Return to baseline.** After all six dismount, the cost returns to the zero-pair level
  (0.290 against 0.294 ms).
- **One GC pause.** Window 2 of state 1 has a mean of 1.417 ms against a median of 0.342 ms.
  That comes from a single 128.5 ms frame in which the GC count rose from 67 to 68 and the
  managed heap fell by 130 MB. A process-wide collection landed inside the timed KMC Update.
  The state's median of means excludes it.
- **Whole frame.** Frame P50 was 11.01 ms and P95 11.5–11.8 ms in every state.
- **Logs and subscriptions.** Zero KMC log messages in every window. The subscription set (8
  owned subscribers, 21 interface entries) was unchanged across every window. Owner counts were
  exact, with no failed constructions.
- **Heap.** Process-wide retained heap grew by 11–16 MB per window, slightly more at six pairs
  (about 15.5 MB) than at zero (about 12.3 MB). This is retained memory, not allocated bytes,
  and includes the game's own work.

### Limits

These numbers still include some observer work on measured frames: the per-frame native scope
scan of `State.Units`, the 54-pin identity array and the settledness predicates. Compared with
batch157's two-pair matched pool (0.198, 0.238 and 0.258 ms at 0, 1 and 2 pairs, profiler
absent), these values are 0.10–0.15 ms higher at 0–2 pairs. That is consistent with this
observer's remaining guard and the larger twelve-actor world, but it was not separately
measured. This is one run on one machine, with an automation request present. It is not
diagnostics-off or request-free normal-player evidence, and it is not performance acceptance.

## Restoration

- Outer PASS, with Mods restored, save protection passed, baseline immutable, Working restored
  and the save write-allowlist passed.
- The restored protected-save inventory digest is `df434471…61ab`, equal to the digest before
  the run.
- Run and transaction records are `restored`.
- Profile bytes were restored exactly, with zero accepted cache changes.
- Base-game isolation: stage verified, dependencies unchanged, process exited.
- No Kingmaker process, transaction lock or wrapper remains.

## What this closes and what it does not

F09's six-pair stress now has both halves as development evidence on exact candidates: the
lifecycle (batch188, again here) and a representative KMC Update measurement through six active
pairs. F11 performance acceptance stays open. It needs a justified threshold or baseline
comparison, decided before any judgement, on the final package. Request-free diagnostics-off
normal-player controls are also still open.

Still open:

- SHOULD-FIX 3: the unattended exit's real result through the full game-result reader.
- Remaining F10, F11 and door work.
- Final-package private 7E.
