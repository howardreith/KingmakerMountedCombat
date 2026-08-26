# Horse implementation report

Status: IN PROGRESS

Superseding Tranche B state (2026-08-26T09:47:04Z): dev.7 proves the requested single Bite was healthy but intentionally consumed by Kingmaker's same-target `UnitAttack` merge into an already active native RT attack. The diagnostic waited on the discarded request while native combat killed the target. Dev.8 isolates only the temporary horse's commands at explicit test dispatch and requires reference-identical Standard ownership; no production behavior changes. Full local gates pass and one clean audited aggregate remains before mounted-profile admission.

Superseding Tranche B state (2026-08-26T08:08:44Z): dev.6 proves exact Bite/Hoof/Hoof enumeration and every preceding registration, ownership, progression, control, movement, target, and combat-entry gate. Aggregate `20260826T072604Z-horse-companion-unmounted-dev6-passE` remains historical restored `FAIL 28/1` because its first stock RT Bite did not terminate before the old 180-second global deadline. Dev.7 adds one diagnostic-only 20-second snapshot of the native start/approach boundary; no production behavior changes.

Superseding Tranche B state (2026-08-26T06:33:22Z): dev.5 reached stock combat after all preceding companion and target gates passed. Aggregate `20260826T055857Z-horse-companion-unmounted-dev5-passD` remains historical `FAIL 26/1`: enabled hands caused the empty secondary slot to repeat Bite, yielding Bite/Bite/Hoof/Hoof. Dev.6 adopts the exact native horse no-hands body with ordered Bite/Hoof/Hoof natural limbs. Full offline gates pass; one clean aggregate remains required before mounted-profile admission.

Superseding Tranche B state (2026-08-26T05:10:00Z): dev.4 runtime proved exact native manual-leveling XP settlement with zero duplicate retries, plus horse creation/control/selection and stock movement. Aggregate `20260826T043600Z-horse-companion-unmounted-dev4-passC` remains historical `FAIL 25/1` because the later diagnostic target was derived from the moved horse while the target service correctly validates distance from the owner. Dev.5 repairs only that guarded scenario boundary and requires fresh aggregate evidence for combat/death/respec.

Superseding Tranche B state (2026-08-26T03:40:00Z): dev.3 aggregate `20260826T024500Z-horse-companion-unmounted-dev3-passB` is immutable historical `FAIL 22/1` with exact restoration. Exact installed Call of the Wild behavior disproves the dev.3 `DefaultBuildData` theory: its native animal-companion patch settles progression by assigning exact target XP and raising the native experience event for manual pet leveling, rather than committing class levels synchronously. Dev.4 accepts that exact native handoff or a committed class level, records the disposition, and suppresses duplicate native updates. A fresh dev.4 aggregate remains required.

## Starting point

- branch: `codex/mounted-combat-phase3-horse`
- exact Phase 2 closure base: `ecb89500eb36eabbf889ccda7185843bd1e3e7c5`
- accepted Mammoth implementation: `1241222459209aea1e6127bedd7d630df3940b99`
- inherited Phase 2 product version: `0.1.0-phase2b-dev.1`
- credited Horse Tranche A audit version: `0.1.0-phase3a-dev.2`
- active Horse Tranche B version: `0.1.0-phase3b-dev.7`
- current version-bound offline gates: source `21/0`, Release, component `251/0`, visual/source-order `17/0`, harness `231/0`, assembly `349/0` (`325` Kingmaker + `24` Wrath), PowerShell parser `26/0`, JSON parser `7/0`, diff, and prohibited-payload validation; clean-package and runtime gates remain pending

Phase 2 remains accepted with its documented private-alpha limitations. Horse work does not retroactively claim stock right-click mounted attacks, mounted auto-attack, unified Wrath-style turns, animated Mammoth TB locomotion, or public-release quality.

## Current result

Tranche 0 assembly forensics is complete. Exact Wrath evidence supports a later coordinated shared-turn design but not a broad port during horse development. The Phase 2 separate-turn model remains the functioning fallback.

The exact native `CR1_HorseRiding` file identities have been reverified. First bounded run `20260825T162200Z-horse-native-asset-audit-passA` restored exactly and remains historical uncredited `FAIL 15/5`. Exact installed token `0x06007478` proved its observer defect. The single repaired retry `20260825T180000Z-horse-native-asset-audit-repair-passB` is credited `PASS 21/0`; independent audit passed before evidence read.

The resulting decision is exact: `PonySummoned` is a separate Medium `Pony_02` prefab/mesh/rig with no `Chest` or stirrup transforms. It shares stock movement and broad animation infrastructure, but not the riding horse prefab, mesh, skeleton/view family, footprint, or seat geometry. The pony will not be resized. The Large `HorseRiding` native view remains the authority for the KMC horse companion and mounted profile.

The first registration run `20260825T195200Z-horse-companion-registration-passA` is preserved historical `FAIL 12/1` with exact restoration. Its only failure was an observer assumption: stock Mammoth and Dog blueprint class components also begin at zero, and native `AddPet` performs rank-driven runtime leveling. Dev.2 corrects that comparison, makes stock UnitAttack enumerate Bite then two Hooves, and removes only an exact KMC horse after native respec deactivation clears ownership.

The first aggregate run `20260825T222800Z-horse-companion-unmounted-passA` is immutable historical `FAIL 22/1`; its immediate independent audit passed exact external restoration before gameplay evidence was read. Registration passed `13/0`, and the unmounted row passed creation, reciprocal ownership, direct control, rank-4 upgrade, native view/statistics, selection, and exact cleanup. Its sole failure observed character level `1` where installed `AddPet` maps rank 4 to level 4.

Installed `AddPet`/`AddClassLevels` inspection identifies a narrow non-exception boundary that can explain the observation: an activation-stack `DefaultBuildData` context diverts levels into a plan instead of committing the live descriptor. Dev.3 keeps native spawn/ownership/rank/upgrade logic, then permits at most one later exact native `TryUpdatePet` after that context is absent. Exact horse identity, reciprocal ownership, expected rank deficit, and a zero prior-attempt count are all mandatory. The aggregate suite records activation/deferred levels and context state so the retry can confirm or reject that theory while remaining fail-closed. A fresh immutable dev.3 package and one audited retry are pending.

The dev.3 retry rejected that explanation: activation was already outside `DefaultBuildData` and the deferred stock update still left class level `1`. Exact installed Call of the Wild analysis established the actual contract. Its `AddPet.TryLevelUpPet` prefix handles the exact animal-companion class by assigning native target XP and raising the gain-experience event, preserving manual level-up selection while returning without synchronous class-level commitment. Dev.4 therefore treats exact target XP as a successful native manual-leveling handoff, treats committed level as the stock alternative, and never retries after either settlement. No foreign mod is patched and no level or XP is directly mutated by KMC.

Dev.5 then proved the corrected target boundary and native combat entry, but exact `UnitAttack.AllAttacks` exposed Bite/Bite/Hoof/Hoof. Exact Kingmaker code and the credited native horse audit establish why: enabled hands enumerate both primary and secondary attack counts, while `CR1_HorseRiding` disables hands. Dev.6 uses null hand weapons and ordered additional natural limbs Bite/Hoof/Hoof. Runtime now proves that topology exactly. Its next stock RT Bite command remained unfinished to the old global deadline; exact installed command code narrows that to a native pre-start/approach boundary, and dev.7 observes that boundary without changing production behavior.

## Implementation ledger

| Area | Status | Evidence |
|---|---|---|
| Wrath command/turn model | PASS | `planning/WOTR-MOUNTED-COMMAND-MODEL.md` |
| Horse native file identity | PASS | `planning/HORSE-PONY-ASSET-AUDIT.md` |
| Pony comparison | PASS | credited audited run `20260825T180000Z-horse-native-asset-audit-repair-passB`, `21/0` |
| KMC blueprint trio | PASS (offline) | corrected Mammoth/Dog bootstrap comparison; historical run passed 12/13 production assertions |
| Ranger selection | PASS (runtime observed) | historical restored run proved exact 7→8→7→8 lease even though aggregate status was 12/1 |
| Unmounted horse | IN PROGRESS | dev.6 historical restored `28/1`; Bite/Hoof/Hoof proved; one bounded dev.7 RT start/approach observation pending |
| Mounted horse profile | TODO | blocked on unmounted qualification |
| Target-selected Mount | TODO | design/implementation authorized |
| Mammoth regression | TODO | required only after relevant shared changes |
| Paladin Divine Steed | DESIGN ONLY | `docs/PALADIN-DIVINE-STEED-DESIGN.md` |

The dev.2 package and restored partial runtime observation are historical inputs. No technically qualified horse playtest package exists yet.
