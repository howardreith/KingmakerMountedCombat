# Sprint 9: integrated acceptance and release candidate

**RELEASE CANDIDATE READY - OWNER ACCEPTANCE PENDING.** No merge, tag, publication or installation is selected. Owner acceptance is not produced by an agent, and none is recorded here.

## Identity

| | |
|---|---|
| Integrated baseline | `main` `e14bee6b`, the real merge of PR #13 by owner order on 2026-11-04 |
| Branch | `codex/mounted-combat-sprint9-integration-release`, PR #14. Review head `b5edb677` |
| **Final compiled source** | `63900a4c`, candidate **sprint9h**. Later commits are docs only |
| Version | `0.1.0-chunk7b-preview.102`. Readers branch on `^0.1.0-chunk7b-preview.N$`, so the shape is kept; a release name can differ |
| DLL | SHA-256 `61e1569ef02f043606ef10632f36ada9db68ba9c12863cdfa492809a676f7afb`, MVID `e40ececd-295b-4ea9-95fd-1c75142be287`, 8,869,376 bytes (limit 9 MiB) |
| Package | `artifacts\KingmakerMountedCombat-0.1.0-chunk7b-preview.102-sprint9h-diagnostic.zip`, SHA-256 `6127f86e4e51da3b8db9ca18835f9089550207ccc1a536c41ef40399c11d6ed1`. Holds exactly `Info.json` and the DLL. The `-diagnostic` name is the established route's. The automation host runs only for an explicit command-line request, and `h02` ran with none |
| Suite | `sprint9h-63900a4c-20261104` |
| Earlier candidates | sprint9g `bc088965` DLL `345b9784...` (the reviewed candidate); sprint9f `af9140c9` DLL `afe83532...`; sprint9e `0e234ecb` DLL `72754ce9...`; sprint9d `f27b8a93` DLL `3fe41598...` |
| Negative control | `codex/mounted-combat-sprint8-negative-control` (`10044985`) is local, not an ancestor, has no remote ref and was not packaged |

**Byte differences that matter below.** sprint9g to sprint9h: the Celestial Smite target check, its admission gate, its click message, UMM text, the version, and diagnostics. Every other command, including every other mount command, leaves the gate at its first test. sprint9e to sprint9g: Divine Mount registration no longer requires the mixed-class list, plus one diagnostic assertion. sprint9d to sprint9g: that, plus the Smite admission narrowed to Celestial Smite. None touches saving, movement or the feat. Runs are reported on their own bytes and are not relabelled: only `h01` to `h03` are current-binary execution.

## Mounted Celestial Smite: range and admission (review of PR #14)

**Finding.** Source review of `b5edb677`: an accepted out-of-range mounted Smite waited silently for an unrelated rider action, then could acquire a native approach competing with the pair's movement. No drift was reproduced in play. Native tracing shows the path was open: `TickCommand` ticks an approach once `TickCommandTurnBased` admits the command.

**Behaviour now.** While ridden, Celestial Smite works from where the mount stands. A request that needs an approach is refused, and an unridden Smite keeps its native approach.

| Boundary | What happens |
|---|---|
| Target preview and click | `DivineMountSmiteLogic.CanTarget`, which the native handler reads, refuses a target needing an approach. It uses the ability's own `GetApproachDistance`, mechanics distance and line of sight, the same facts as native `IsUnitEnoughClose`. No command is created. The message reads "Celestial Smite: move the pair into range first." |
| Admission, every tick before start | An unstarted Smite that is no longer in reach, available or targetable ends with its own command, in every phase and mode, before native can tick an approach. The range message appears only for a range refusal |
| Delivery | A target that left reach after native committed takes no mark and the player is told. Native's cost stands at that boundary, as for any ability whose effect finds nothing; nothing is refunded |
| In reach | Unchanged, first action or later: one mark, one daily use, the mount's own Swift |

**Native evidence on sprint9h.** Long range here reads 32.08 m in play, including corpulence. Far targets were placed at 35 to 43 m as disclosed setup.

| Case | Run | Observed |
|---|---|---|
| Far click in Preparing | `h01` | `canTarget` false, click false, message shown, no command. Use 1, Swift 0, no mark, mount moved 0.00 m, still Preparing |
| Target moved between click and start | `h01` | Click accepted, then refused at the gate. No command left, use 1, Swift 0, no mark, moved 0.00 m, still Preparing |
| Far click during the pair's accepted move, Acting | `h01` | Refused with the message. The same `UnitMoveTo` kept running and ended Success after 4.97 m; nothing spent |
| In reach, first action in Preparing | `h01` | Use 1 to 0, mark, mount Swift 6, rider Standard 0. End Turn then reached the next turn with the mark and no use returned |
| In reach while Acting | `h01` | Use 1 to 0, mark on the new target, previous mark retired, mount Swift 6, rider unaffected. Disclosed setup: a native rest returned the daily use |
| Charge and mount attack after | `h01` | One lawful charge rule, rider 6/3. The mount's real attack carried +9 at 9 HD |
| Far click in real time, live brain | `h03` | Refused with the message, nothing spent. The pair's own attack and move commands were still in place |
| In reach in real time | `h03` | Use spent, mark, +14 at 14 HD, Charisma to hit |
| Unmounted control | `h01` | The same far target stays targetable: `needsApproach` false, `canTarget` true. This reads prediction; the unmounted approach itself is native code KMC does not enter, and was not walked |

Offline: `MountedSmiteNeedsApproach` covers unmounted, beyond range, exact range, blocked line and in reach. Reader negatives cover an accepted far click, mount movement, a spent use, a silent refusal, a resident request, a stale mark, a replaced pair move and a lost unmounted approach.

**Not natively exercised:** a refusal during an accepted attack or Charge or a held touch in TB (by code, a click refusal never reaches `Run`, and a gate refusal interrupts only its own command; `h03` shows an accepted attack surviving in real time); a blocked line of sight; the late refusal at delivery, which needs a target to leave within the instant between start and delivery.

**Failed originals.** None in this revision: `h01` to `h03` passed first time. The evidence that the earlier path was unsafe is source trace, and `a02`, `a03` hold the earlier failures.

## Defects this sprint found and fixed

| Defect | How found | Failing evidence | Repair |
|---|---|---|---|
| **Divine Mount never registered on a base-game install.** The Paladin safely kept the stock bond, but the Sprint 8 feature did not exist without Call of the Wild, which is the owner's own configuration | Request-free base-game control | `e08`, FAIL: "The mixed-class companion choice has no companion list" | The list is optional. Native record in `f01`: the base-game choice carries `PrerequisitePet:NoCompanion=False`, so it is offered only to someone who already has a companion, with nothing to append |
| **A mounted Divine Mount could not use Celestial Smite in turn-based combat.** The click was accepted and the command never started, in Preparing or Acting, with no message | New TB neighbor row | `a02`, `a03` | The paired gate admits exactly this command. Native availability and the mount's own Swift cost are unchanged |
| **UMM page text** named the retired Rider Primary and Mount Primary and only the Horse as a save dependency | Source read while planning | none native | Text corrected |
| **Support documentation** described Charge as off by default and TB Charge as unavailable | Source read | none native | Current notes in [the release notes](SPRINT9-RELEASE-NOTES.md); pointers added to the older pages |

Stale checks corrected, none weakening a product claim:

- **`S3-ranged-distant` reader.** It had always stopped at the trap row before reaching this check, and it counted a live rider's swing made before the hostile retreated. `r16` from Sprint 3 shows the identical pattern.
- **`pair-stock-attack-order` assertion.** It required exactly one rider dispatch; Sprint 4's continued intent legitimately dispatches twice. The one-rule and no-duplicate checks remain.
- **`Test-ScenarioDispatchEvidence.ps1`.** Fails identically on the accepted Sprint 8 tree: a stale hard-coded list outside the gates. Classified as pre-existing and left unchanged.

## Outcome map and results

"Run" below means executed in Sprint 9 on the named binary. `g` runs are on sprint9g, `e` on sprint9e, `a` on sprint9d, `f` on sprint9f and `h` on the final sprint9h. Unmarked runs are retained on their own bytes, justified because sprint9h changes only the Smite boundary above. Runs are under `runtime-evidence\`, prefix `20261104-sprint9-` unless named. Every run in the sprint, pass or fail, reports `modsRestored` and `saveProtectionPassed` true.

| Outcome | Evidence | Status |
|---|---|---|
| Base-game load, no auto-start, IDs register once, no KMC error | **`h02` on final sprint9h**, no automation request, observer confirms, zero KMC error lines | **Run, current** |
| Conflict fails clearly | Candidate offline on current; S8 `a04` live drift retained; `g19` inventory reruns the drift on current | **Run** |
| Genuine Auto overwrite with area transition, cold | `e03`, `e04` on sprint9e | **Run** |
| Quick rotation | `e05` on sprint9e; cold retained from S1 `r07` | **Run**, cold retained |
| Manual new slot and cold | `a05`, `a06`, `g20`, `g21` | **Run** |
| Manual overwrite | S1 `r08`; save code unchanged since | Retained |
| Authentic earlier-release upgrade | `e06`, `e07` on sprint9e: preview.211 Quick `83e92e1a...` with its exact CotW and CMI, resaved `62c901f4...`, then cold in a separate process | **Run** |
| Door, movement, turns, formation | `g11` | **Run** |
| Area transition | `e03` | **Run** |
| Distant friendly touch, RT | `e02` on sprint9e: original FAIL, re-evaluated PASS, `analysis-cache\sprint9\20261104-sprint9-e02-spells-rt-reader-reevaluation.json` | **Run** |
| Distant friendly touch, TB | S3 `r17` | Retained, not run: the balance went to higher-value rows |
| Retarget, retired helpers, Haste, Stop, Hold, replacement | `g13` | **Run** |
| Hold-on-move neighbors | S4 `r08` | Retained |
| TB ordinary actors and resources | `g14` | **Run** |
| Multiple-pair isolation | `g15` isolation row; `a05`, `a06` reader demands both independent owners | **Run** |
| Feat availability, RT defense, renewal | `g15` | **Run** |
| **Coexistence: one rider, one TB fight: off-turn borrow, debt paid once at preparation, real Charge, Manual save; cold: no refund or replay, no second debt charge, a further legal reaction** | `a05`, `a06` on sprint9d | **Run** |
| Charge RT | `g16` | **Run** |
| Charge queue, lane, mount-first, Pounce, reforce | S7 `q02`, `q03`, `a03`, `a06` | Retained |
| Female Small rig and geometry extreme on the Dog, normal camera, actions, acquisition, progression | `g17` | **Run** |
| Small dismount and cold load | `g20`, `g21` | **Run** |
| Same bodies on the Horse, full pose matrix | S5 `h3`; no rig file changed since | Retained |
| Paladin level five, both bonds, old character, retrain | `g19` | **Run**, driven by the native controller |
| Mount path, actual pet progression, RT Call and Smite, fallen mount | **`h03` on final sprint9h** | **Run, current** |
| **TB Call, mounted refusal, first-action and Acting Smite, far refusals, End Turn, Charge, the mount's real attack at +9 for 9 HD, removal scan, dismount** | **`h01` on final sprint9h** | **Run, current** |
| Paladin save continuity | S8 `t02`, `t03` | Retained |
| Mixed class, celestial tiers | S8 `q05`, `t01` | Retained |
| Removal with no permanent references, mod absent | `20261104-c7e-final-B902-r31`, `-r32`. The validator requires that run-name form | **Run** |
| Removal refused with references | `h01` scan names the bond and Horse. Feat and Dog detection is source and Sprint 8 review, not rerun | **Run** for the bond and Horse, scan only: the refusal button itself was not clicked on a dependent campaign |
| On-screen level-up, action list, overall appearance | Never clicked by an agent | **Owner script** |
| **Trap** | TRAP-01 has never executed. Bounded inspection of all nine owner seeds this sprint: nearest active trap 54.6 m at another elevation (Old Sycamore); the dungeon floor's are 87 to 92 m behind hostiles | **Owner item, unresolved** |
| **TB door** | Never demonstrated; no reachable combat-usable door in existing inputs | **Limitation** |
| **Clean uninstall from a dependent save** | Not claimed | **Limitation** |
| **Authentic older mounted save with no CotW** | The only authentic archive needs CotW and CMI | **Limitation** |

## Base game versus Call of the Wild

| | Base game | With CotW and CMI |
|---|---|---|
| Registration, no errors | **Run** (`f01`) | **Run** |
| Mount, move, dismount, save, cold load on no-mods owner seeds | **Run** (`e03` to `e05`, `f01`) | **Run** |
| Divine Mount, Riding Dog and feat gameplay rows | **Not run**: the Working fixture cannot load without CotW | **Run** |
| Pet progression | **Unverified.** Stock `AddPet` should level and rest the pet itself, which would refill Smite. This is the owner's configuration, so it is in the owner script | Manual native levels, **Run** |

## Accepted limits, stated for review

- Agent rows use native controllers and the ordinary click handler, not on-screen clicks.
- Of a mount's own abilities, only Celestial Smite is admitted on the paired TB turn. Others wait for a dismount, as they always have.
- While ridden, Celestial Smite does not approach: move the pair into range and retry. This is the chosen bounded behaviour, not a general ability-approach system.
- In `h01` the mounted Call read as refused for the right reason, but the owner had no uses left, so "nothing spent" there is vacuous. RT `h03` holds it with uses available.
- The Call reads as unavailable for one frame before its owner is selected. Delivered normally; players always select first.
- A close camera in the diagnostic captures clips through geometry. Medium and normal views are clean.
- Sprint 4 to Sprint 8 adaptations stand as documented: rider-charge model, smite target and one-day mark, Intelligence, template SR, companion death.

## Reviews

Independent source reviews before live use: pre-freeze diff (10 findings, row and reader corrections made), delta (reader strictness, re-evaluable), Smite repair (narrowed to Celestial Smite). The next product delta left the out-of-range risk now closed above, and asked whether a null list could hide Call of the Wild adding its list late; `g19` closes that, with CotW staged, the list holds the Horse, Riding Dog and both Divine Mount entries. The range correction's own delta review (6 findings: Acting message unasserted, range message on non-range refusals, silent late refusal, lease refresh, unrecorded destroy, RT evidence) was addressed before `h01`.

## Budget and restoration

Record `sprint9`: **91.73 of 105** minutes after the one authorized +15 adjustment (90 to 105) on the same record, 32 launches, no reset. 79.73 before this revision, 12 in it: Fast, the Candidate gate, package and suite, and three launches. Sprint 8's 103.42/115 is untouched. Batches through `sprint9:h1`.

Offline on sprint9h: Candidate 7 of 7, 3.56 minutes, with the Paladin reader suite.

At closeout: Mods tree empty, the same as each run's captured start; no game process; no lock; no candidate installed; protected saves restored per each result.

## Failed evidence retained

`a01` row name unregistered; `a02`, `a03` Smite never started, then first action; `e02` reader, re-evaluated; `e08` base-game registration; `20261104-c7e-final-B901-r31` stale assertion, after removal itself passed; `-B901-r32` refused because its source had not passed. All under `runtime-evidence\`.

Owner route: [owner play script](SPRINT9-OWNER-PLAY-SCRIPT.md). Notes: [release notes](SPRINT9-RELEASE-NOTES.md).
