# Horse implementation report

Status: IN PROGRESS

## Starting point

- branch: `codex/mounted-combat-phase3-horse`
- exact Phase 2 closure base: `ecb89500eb36eabbf889ccda7185843bd1e3e7c5`
- accepted Mammoth implementation: `1241222459209aea1e6127bedd7d630df3940b99`
- inherited Phase 2 product version: `0.1.0-phase2b-dev.1`
- credited Horse Tranche A audit version: `0.1.0-phase3a-dev.2`
- active Horse Tranche B version: `0.1.0-phase3b-dev.1`
- current version-bound offline gates: source `21/0`, Release, component `247/0`, visual/source-order `17/0`, harness `230/0`, assembly `335/0` (`311` Kingmaker + `24` Wrath), parsers `26/0` and `7/0`

Phase 2 remains accepted with its documented private-alpha limitations. Horse work does not retroactively claim stock right-click mounted attacks, mounted auto-attack, unified Wrath-style turns, animated Mammoth TB locomotion, or public-release quality.

## Current result

Tranche 0 assembly forensics is complete. Exact Wrath evidence supports a later coordinated shared-turn design but not a broad port during horse development. The Phase 2 separate-turn model remains the functioning fallback.

The exact native `CR1_HorseRiding` file identities have been reverified. First bounded run `20260825T162200Z-horse-native-asset-audit-passA` restored exactly and remains historical uncredited `FAIL 15/5`. Exact installed token `0x06007478` proved its observer defect. The single repaired retry `20260825T180000Z-horse-native-asset-audit-repair-passB` is credited `PASS 21/0`; independent audit passed before evidence read.

The resulting decision is exact: `PonySummoned` is a separate Medium `Pony_02` prefab/mesh/rig with no `Chest` or stirrup transforms. It shares stock movement and broad animation infrastructure, but not the riding horse prefab, mesh, skeleton/view family, footprint, or seat geometry. The pony will not be resized. The Large `HorseRiding` native view remains the authority for the KMC horse companion and mounted profile.

The original KMC horse unit/feature/upgrade trio and exact Ranger append/restore transaction are implemented offline. The horse uses the native Large riding view, explicit companion mechanics, Bite1d4 plus two Hoof1d4 attacks, stock `AddPet` ownership, and a KMC-owned rank-4 Strength/Constitution upgrade. A bounded registration scenario and independent semantic validator are ready. Runtime registration, actual companion creation/progression/lifecycle, unmounted qualification, mounted profile work, target-selected Mount, and final packaging remain in progress.

## Implementation ledger

| Area | Status | Evidence |
|---|---|---|
| Wrath command/turn model | PASS | `planning/WOTR-MOUNTED-COMMAND-MODEL.md` |
| Horse native file identity | PASS | `planning/HORSE-PONY-ASSET-AUDIT.md` |
| Pony comparison | PASS | credited audited run `20260825T180000Z-horse-native-asset-audit-repair-passB`, `21/0` |
| KMC blueprint trio | PASS (offline) | original definitions compile and exact installed contracts pass; runtime audit pending |
| Ranger selection | PASS (offline) | exact seven-option array plus reference-exact append/restore lease; runtime audit pending |
| Unmounted horse | TODO | no credited runtime rows |
| Mounted horse profile | TODO | blocked on unmounted qualification |
| Target-selected Mount | TODO | design/implementation authorized |
| Mammoth regression | TODO | required only after relevant shared changes |
| Paladin Divine Steed | DESIGN ONLY | `docs/PALADIN-DIVINE-STEED-DESIGN.md` |

No horse package or runtime claim exists yet.
