# Chunk 7 retired Small calibration cases: the Manual-derived and assisted Small Manual sources

Status: **RETIRED and not selected (October 24, 2026, owner decision on the 7E X6 analysis).**
Both cases keep a prelaunch refusal. No PASS is claimed or transferred. Neither case was ever run
natively, so no evidence record changes.

## What they were

Both are cases of the scenario `chunk7-small-profile-calibration`.

- `compiled-manual-derived-small-horse-rt` (A1) cold-loaded a same-candidate one-owner
  `preserve-acquired-field` Manual descended from the paid Small Ranger Manual-derived acquisition.
  It then ran the Small calibration: per-tick declared-body match, cold body, progression and
  resource equality, native Mount, move, both primaries and Dismount, and the seven pose phases.
- `compiled-assisted-two-owner-small-horse-rt` (A2) did the same from the assisted two-owner
  `preserve-two-acquired-field` Manual.

Both cases require `sourceAcquisition.nativeWrite.saveBeforeFieldSelection == true`:
- product: `Chunk7SmallCalibrationScenario.Manual.cs:109` and `:221-245`;
- reader: `Chunk7SmallCalibrationEvidence.ps1:528` and `:616`.

That is the retired Auto-derived source contract. Since `ed40ef95`, the selected Manual field
acquisition writes after the field selection (`saveAfterFieldSelection`,
`Chunk7ManualDerivedScenario.Build.cs:380`), so no current source can satisfy either case. For A2
this is inferred: the only emitter of `saveBeforeFieldSelection` is the retired ordinary
acquisition, and A2 was never run against an assisted chain.

## Replacements

| Claim | Covered by |
| --- | --- |
| Exact declared Small body each tick: preset, MaleHalfling, handedness, mirroring, scale, Doll set, the 12 ordered equipment slots, visual set, Horse `4016c7db…` at Large size 5 | `compiled-owner-seed-fixture-small-horse-rt` (native 9/9, [batch72](CHUNK7-BATCH72-SMALL-ORDINARY-NATIVE-2026-10-20.md)) |
| Cold body, progression and resource equality to the saved source; no preparation on load | The same F09 case, with 20-field body equality (`Chunk7SmallCalibrationScenario.FixtureManual.cs:93-105`) |
| Native Mount, move, both primaries, Dismount, seven pose phases | The same F09 case; scroll and potion while mounted in `compiled-owner-seed-fixture-small-horse-presentation-rt` |
| Mounted Small Manual write and a separate cold load | Matrix rows 33–34 (`small-fixture-persistence-save`/`-load`). They also pin the literal `small-default-halfling-horse-v1` in the archive |
| Same-candidate paid Small Ranger lineage, mounted and cold-loaded | Owner-seed c4 → c5 → x4s → x4c and rows 58/59. The body is re-verified through `SupportedMountedProfiles.Resolve` at bind and cold bind |

**Lost combination:** the full Small calibration on a *same-candidate* paid Manual-derived source.
- The F09 calibration proves the same calibration on the cross-candidate preview.71 unmounted
  source.
- The owner-seed chain proves same-candidate lineage only through `Resolve`, with no pose phases
  or calibration actions.

No matrix row loses a check; neither case fed one.

## Mechanism

- **Reason and gate.** `Get-KmcChunk7RetiredSmallCalibrationCaseReason` in
  `scripts/runtime/RuntimeHarness.Common.ps1` returns the refusal, naming the replacements.
  `Read-KmcChunk7SmallSelectionFile` (`scripts/runtime/Chunk7SmallCalibrationFixtures.ps1`) calls
  `Assert-KmcChunk7SmallCalibrationCaseSelected` as soon as the guarded read has parsed the
  selection's `case`.
- **Position.** The refusal comes before the selection is validated or admitted. The runner reads
  that file before any source intake, approval, lock, transaction, Mods swap or launch.
- **Why inside the reader.** The case exists only inside the guarded selection file, so the gate
  sits inside its read. It cannot run before the read, as the scenario-level gates do.
- **What stays.** The cases remain in the closed C# sets, and the reader paths remain unchanged.
- **Test.** `scripts/chunk7/Test-Chunk7RetiredSmallCalibrationCases.ps1` runs the actual runner. It
  covers both cases, with minimal and full-shaped selections, with and without `-WhatIf`. It
  expects the exact refusal, with no new evidence directory and no change to the transaction
  sentinel. It checks the gate's position by AST, and checks that the three selected Small cases
  still reach their own intake checks.
- **Umbrella.** It is in the Chunk 7 umbrella in place of `Test-Chunk7SmallManualEvidence.ps1` and
  `Test-Chunk7SmallManualSource.ps1`. Those two positive reader tests remain in the repository,
  unselected. They fail because their fixture embeds the retired source.
