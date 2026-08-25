# Horse implementation report

Status: IN PROGRESS

## Starting point

- branch: `codex/mounted-combat-phase3-horse`
- exact Phase 2 closure base: `ecb89500eb36eabbf889ccda7185843bd1e3e7c5`
- accepted Mammoth implementation: `1241222459209aea1e6127bedd7d630df3940b99`
- inherited Phase 2 product version: `0.1.0-phase2b-dev.1`
- credited Horse Tranche A audit version: `0.1.0-phase3a-dev.2`
- active Horse Tranche B version: `0.1.0-phase3b-dev.2`
- current version-bound offline gates: source `21/0`, Release, component `248/0`, visual/source-order `17/0`, harness `231/0`, assembly `340/0` (`316` Kingmaker + `24` Wrath), PowerShell parser `26/0`, JSON parser `7/0`, diff, and prohibited-payload validation; clean package and runtime gates remain pending

Phase 2 remains accepted with its documented private-alpha limitations. Horse work does not retroactively claim stock right-click mounted attacks, mounted auto-attack, unified Wrath-style turns, animated Mammoth TB locomotion, or public-release quality.

## Current result

Tranche 0 assembly forensics is complete. Exact Wrath evidence supports a later coordinated shared-turn design but not a broad port during horse development. The Phase 2 separate-turn model remains the functioning fallback.

The exact native `CR1_HorseRiding` file identities have been reverified. First bounded run `20260825T162200Z-horse-native-asset-audit-passA` restored exactly and remains historical uncredited `FAIL 15/5`. Exact installed token `0x06007478` proved its observer defect. The single repaired retry `20260825T180000Z-horse-native-asset-audit-repair-passB` is credited `PASS 21/0`; independent audit passed before evidence read.

The resulting decision is exact: `PonySummoned` is a separate Medium `Pony_02` prefab/mesh/rig with no `Chest` or stirrup transforms. It shares stock movement and broad animation infrastructure, but not the riding horse prefab, mesh, skeleton/view family, footprint, or seat geometry. The pony will not be resized. The Large `HorseRiding` native view remains the authority for the KMC horse companion and mounted profile.

The first registration run `20260825T195200Z-horse-companion-registration-passA` is preserved historical `FAIL 12/1` with exact restoration. Its only failure was an observer assumption: stock Mammoth and Dog blueprint class components also begin at zero, and native `AddPet` performs rank-driven runtime leveling. Dev.2 corrects that comparison, makes stock UnitAttack enumerate Bite then two Hooves, and removes only an exact KMC horse after native respec deactivation clears ownership.

One bounded `horse-companion-unmounted-suite` now exercises corrected registration, live creation/progression/ownership, selection/movement, RT/TB natural attack cardinality, death/recovery, respec, uninstall surface, non-horse isolation, and exact internal restoration. It is offline-green but has not yet run from a clean published package. Actual disk save/reload is explicitly reserved for final manual review because guarded automation may not enter Kingmaker's crash-unsafe temporary-save write path.

## Implementation ledger

| Area | Status | Evidence |
|---|---|---|
| Wrath command/turn model | PASS | `planning/WOTR-MOUNTED-COMMAND-MODEL.md` |
| Horse native file identity | PASS | `planning/HORSE-PONY-ASSET-AUDIT.md` |
| Pony comparison | PASS | credited audited run `20260825T180000Z-horse-native-asset-audit-repair-passB`, `21/0` |
| KMC blueprint trio | PASS (offline) | corrected Mammoth/Dog bootstrap comparison; historical run passed 12/13 production assertions |
| Ranger selection | PASS (runtime observed) | historical restored run proved exact 7→8→7→8 lease even though aggregate status was 12/1 |
| Unmounted horse | IN PROGRESS | aggregate runtime engine and strict validator offline-green; clean live run pending |
| Mounted horse profile | TODO | blocked on unmounted qualification |
| Target-selected Mount | TODO | design/implementation authorized |
| Mammoth regression | TODO | required only after relevant shared changes |
| Paladin Divine Steed | DESIGN ONLY | `docs/PALADIN-DIVINE-STEED-DESIGN.md` |

No horse package or runtime claim exists yet.
