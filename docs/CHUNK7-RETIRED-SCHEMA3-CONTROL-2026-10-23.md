# Chunk 7 retired persistence case: the schema-3 pending control

Status: **RETIRED and not selected (October 23, 2026, 7E blocker X3).** Its prelaunch refusal
remains. No PASS is claimed or transferred, and its batch35–37 FAILs keep their records.

## What it was

`pending-schema3-control-manual` cold-loaded the unchanged schema-3 pending-only Manual with no
migration. It was the matched control of the
[batch35 discriminator](CHUNK7-DIRECTIVE4-MATCHED-DEBT-DISCRIMINATOR-2026-10-19.md): run beside the
schema-2 derivative, it showed that both loads lost rider A's retained debt to the same native
`JoinCombat` → `UnitCombatPrepareController.Tick` re-entry, so the defect was not a schema-2
conversion fault. Its reader expects two debt snapshots and the scope
`exact-schema3-control-cold-after-presentation`.

## Why it is superseded

| Control claim | Covered by (7E matrix rows) |
| --- | --- |
| Separate native re-entry from schema conversion | Diagnosis closed by the [re-entry correction](CHUNK7-DIRECTIVE4-REENTRY-CORRECTION-2026-10-19.md) and batches 41–42 |
| A schema-3 pending-only Manual restores the saved debt on a cold load | Row 19, `owned-schema2-pending-current-manual`: a third-process cold load of the current-format Manual written natively after migration, with the corrected reader |
| The pending-only producer archive is exact | Row 17, `legacy-mounted-pending-tb-manual` save |
| Migration of the same state | Row 18, the schema-2 derivative native resave |

Row 19 loads a schema-3 archive produced by the native resave rather than the original producer
archive, and the two are not equivalent. **Residual gap:** the control was the only cold load of a
pending-only archive written by the live producer with genuine PairId and generation. Row 19 loads
row 18's resave, which went through the schema-2 adapter, so its owners restore at generation 1.
Rows 13–14 (TB debt, save then cold) cover a cold load of a live-producer archive, but only in the
non-pending-only TB-debt shape. The retirement stands because the control was a discriminator, not
a requirement, and its diagnosis is closed; this residual gap is recorded rather than claimed as
covered.

## Mechanism

`Get-KmcChunk7RetiredPersistenceCaseReason` in `scripts/runtime/RuntimeHarness.Common.ps1` returns
the refusal; the runner calls `Assert-KmcChunk7PersistenceCaseSelected $PersistenceCase` right after
the scenario readiness gate, before selection intake, approval, lock, transaction, Mods swap or
launch. The case stays in the closed sets and readers so historical artifacts remain readable.
`scripts/chunk7/Test-Chunk7RetiredPersistenceCases.ps1` checks the exact refusal through the actual
runner, its position, and that every other runner persistence case and the selected pending-only
chain still pass the gate.