# Chunk 7 batch193: 7E development pilots (X1, X2, X4)

Status: **development pilots; not 7E qualification.** One native run, a FAIL on a diagnostic
defect, with exact restoration. A focused offline fix is committed and awaits independent
review before any further native use. The standing mission remains IN PROGRESS.

## Candidate (development pilot, not the final 7E candidate)

| Item | Identity |
| --- | --- |
| Source | `503d2fd9`, frozen worktree `worktrees/chunk7-7e-b193-source-503d2fd9`; packaged from the clean branch checkout at the same commit |
| Package | `KingmakerMountedCombat-0.1.0-chunk7b-preview.94-c7e-b193-pilots-diagnostic.zip`, `c615cd65…`, 3,670,196 bytes; entries `Info.json` (329) and the DLL (7,932,928) only |
| Manifest | `9c54abb8…`, commit `503d2fd9`, clean |
| DLL / MVID | `d5cbae3c…` / `bed78b12-7930-4697-9e68-f07e6807c0c0` |
| Candidate tier | 6/0 (4.0 min), receipt `analysis-cache/chunk7-continuation/b193-candidate-01.log` (`d76f405a…`) |
| Suite | `chunk7e-b193-pilots-20261023`, `4040c1b7…` |
| Root `bin\Release` DLL | `d5cbae3c…` before and after packaging (an earlier offline build of the same source had already written it) |
| Freeze record | `analysis-cache/chunk7-continuation/b193-freeze.json` |

## Run

### x1s1 `20261023-c7e-b193-x1s1-ownerseed-life`: FAIL (kept)

- **Case:** row 57, `owner-seed-native-life-cleanup`, selection
  `analysis-cache/chunk7-continuation/x1-ownerseed-native-life-selection.json` (`919993c7…`, read-only).
  WhatIf passed first; live run about 2.2 minutes.
- **Passed in game before the fault:** combat fixture, inherited custody, restored mounted start,
  native initiative orders, and `C7-life-safe-native-damage` (the labelled native damage rule
  kept rider B in the living window). Rider B `fd213210` passed the subject precondition
  (conscious, not essential, dying allowed, not immortal), so the behavior is **not** uncovered.
- **Fault:** step 70, `Chunk7CombatScenario.HealthyOwnerUnchanged` threw
  `Sequence contains no matching element`. Owner rows in the world snapshot carry the registry's
  `"N"`-form actor key (`73681ef2b281…`), and the lookup compared it with the hyphenated
  `UniqueId`. No owner row could ever match, so the native-life scenario could not complete on
  any lineage; it had never run natively (X2), which is why this was not seen earlier. The
  external reader already normalizes the same comparison (`Test-KmcChunk7OwnerActorMatch`).
- **Outer result:** FAIL. The current external `Test-RuntimeResult` reads it as a well-formed FAIL.
- **Restoration:** Mods restored, save protection passed, Working restored, Baseline unchanged,
  save write allowlist passed. No Kingmaker process and no lock afterwards.

## Fix (offline, needs independent review)

`src/KingmakerMountedCombat/Diagnostics/Chunk7CombatNativeLife.cs`: the healthy-owner lookup
matches the owner key and the actor `UniqueId` by GUID (`OwnerActorMatch`), on both snapshots.
Nothing else changes; the comparison stays strict (`Single`, then exact `DeepEquals`).
`scripts/chunk7/Test-Chunk7NativeLifeEvidence.ps1` gains a source regression that refuses the
raw string comparison; it was checked against the previous source, which it refuses.
Checks: release build clean, native-life evidence 49/0, Fast 3/0.

## X2: eligibility and routes (zero-launch)

Source examined: the b164s1 two-owner acquired Manual (`Manual_307_KMC_C7_ACQUIRED_FIELD.zks`,
`a5e42cbb…`), riders `73681ef2` (main Ranger) and `7287e2dc` (paid Small Ranger).

| Case | Finding | Disposition |
| --- | --- | --- |
| `realtime-casting` | The scenario requires exactly one rider with an available memorized `5590652e…` healing slot. Main: one level-1 slot, empty and unavailable. Rider B: no memorized slots. Memorizing needs a native rest, which no admitted fixture step performs; the scenario forbids acquiring or refreshing a slot. | **Uncovered on this source** (not waived). Needs a source whose rider already holds the prepared spell. |
| `realtime-charge` | KMC's Charge ability and the diagnostic lease (`Chunk7CombatScenario` sets the setting for this case only and restores it) are present; geometry is decided natively. | Not piloted. The producer-backed cases require the **same candidate** for the whole acquired chain (`Assert-KmcChunk7OwnedSourceCompletion`): b159 cold inventory → b160 manual-derived → b161 acquired cold → b163 cumulative → b164 two-owner Manual, then the charge run. That is six runs on the pilot candidate. |
| `native-life-cleanup` (two-pair) | Same scenario (`chunk7-two-pair-native-life-cleanup`) as row 57, with the same defect. | Covered by the row-57 route once the fix is reviewed; the producer-backed variant also needs the same-candidate chain. |

Selections for `realtime-charge` and `native-life-cleanup` were authored against the b164s1
lineage (`x2-*-selection.json`, read-only) but are **not runnable** on b193 until the chain is
re-run on the same candidate; their source pins name b164s1.

## X4: three area transfers

`multi-exploration-three-area-transfers-manual` is a producer-backed cold case. Its source would be
a same-candidate `persistence-save` / `multi-exploration-manual` Manual (the b165s1 shape) on the
same-candidate b164 chain. The runner also requires three caller-pinned enter points and areas;
the admitted Chunk 7 pins give the route Spider Cave → Oleg's (`fd225a93…` / `ead426a6…`) →
Spider Cave (`00becd53…` / `b10e8651…`) → Oleg's, which satisfies the reader's no-same-area rule.
Not piloted: it needs the chain plus one Manual producer.

## Next

1. Independent review of the native-life fix.
2. A successor pilot candidate; re-run x1s1.
3. Decide whether the X2/X4 pilots run the six-run same-candidate chain now, or inside 7E where
   rows 46–47 already produce its first two links on the final candidate (the cumulative and
   two-owner links would be added as rows).
