# Sprint 9: integrated acceptance and release candidate

**RELEASE CANDIDATE READY - OWNER ACCEPTANCE PENDING.** No merge, tag, publication or installation is selected. Owner acceptance is not produced by an agent, and none is recorded here.

## Identity

| | |
|---|---|
| Integrated baseline | `main` `e14bee6b`, the real merge of PR #13 by owner order on 2026-11-04. Its tree has no diff against the reviewed head `91735ae0` |
| Branch | `codex/mounted-combat-sprint9-integration-release` |
| Final compiled source | `bc088965`, candidate sprint9g. Later commits are docs only |
| Version | `0.1.0-chunk7b-preview.101`. Readers branch on `^0.1.0-chunk7b-preview.N$`, so the shape is kept; a release name can differ |
| DLL | SHA-256 `345b9784ccd3aa20d331394cbe48b4a6719e4fd0642a10a50186792b3ff29c83`, MVID `6054e0d8-ff14-44f9-bd5e-f287049c8090`, 8,860,160 bytes (limit 9 MiB) |
| Package | `artifacts\KingmakerMountedCombat-0.1.0-chunk7b-preview.101-sprint9g-diagnostic.zip`, SHA-256 `3f85b86f38181df49d23c28ba97ab3648d89a5bd513bfdaedb0db6a46d6dd567`. Holds exactly `Info.json` and the DLL. The `-diagnostic` name is the established route's. The automation host runs only for an explicit command-line request, and `f01` ran with none |
| Suite | `sprint9g-bc088965-20261104` |
| Earlier candidates | sprint9d `f27b8a93` DLL `3fe41598...`; sprint9e `0e234ecb` DLL `72754ce9...`; sprint9f `af9140c9` DLL `afe83532...` |
| Negative control | `codex/mounted-combat-sprint8-negative-control` (`10044985`) is local, not an ancestor, has no remote ref and was not packaged |

**Byte differences that matter below.** sprint9e to sprint9g: Divine Mount registration no longer requires the mixed-class list, plus one diagnostic assertion. sprint9d to sprint9g: that, plus the Smite admission narrowed from any mount Swift ability to Celestial Smite, and a range check on the first-action path. None touches saving, movement or the feat. These runs are reported on their own bytes and are not relabelled.

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

"Current" = sprint9g. Runs are under `runtime-evidence\`, prefix `20261104-sprint9-` unless named. Every run in the sprint, pass or fail, reports `modsRestored` and `saveProtectionPassed` true.

| Outcome | Evidence | Status |
|---|---|---|
| Base-game load, no auto-start, IDs register once, no KMC error | `f01` on sprint9f, no automation request, observer confirms. Differs from current by one diagnostic assertion | **Run** |
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
| Mount path, actual pet progression, RT Call and Smite, fallen mount | `g18` | **Run** |
| **TB Call, mounted refusal, first-action Smite on the mount's Swift, End Turn, Charge, the mount's real attack at +9 for 9 HD, removal scan, dismount** | `e01` on sprint9e | **Run** |
| Paladin save continuity | S8 `t02`, `t03` | Retained |
| Mixed class, celestial tiers | S8 `q05`, `t01` | Retained |
| Removal with no permanent references, mod absent | `20261104-c7e-final-B902-r31`, `-r32`. The validator requires that run-name form | **Run** |
| Removal refused with references | `e01` scan names the bond and Horse. Feat and Dog detection is source and Sprint 8 review, not rerun | **Run** for the bond and Horse, scan only: the refusal button itself was not clicked on a dependent campaign |
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
- **Named unobserved risk.** A first-action Smite beyond range waits, with no message, until the rider acts. Once the turn is acting it is admitted, and native rules may then send the mount on its own approach, which could compete with the rider's order. Source review only: no row exercised it, and the remaining balance sits at the launch guard. A later candidate can refuse an out-of-range Smite outright if the owner hits this or wants it closed first.
- In `e01` the mounted Call read as refused for the right reason, but the owner had no uses left, so "nothing spent" there is vacuous. RT `g18` holds it with uses available.
- The Call reads as unavailable for one frame before its owner is selected. Delivered normally; players always select first.
- A close camera in the diagnostic captures clips through geometry. Medium and normal views are clean.
- Sprint 4 to Sprint 8 adaptations stand as documented: rider-charge model, smite target and one-day mark, Intelligence, template SR, companion death.

## Reviews

Independent source reviews before live use: pre-freeze diff (10 findings, row and reader corrections made), delta (reader strictness, re-evaluable), Smite repair (narrowed to Celestial Smite, range check added). The final product delta, after sprint9g's runs, left the out-of-range risk above, and asked whether a null list could hide Call of the Wild adding its list late. `g19` closes that on current: with CotW staged, the list holds the Horse, Riding Dog and both Divine Mount entries.

## Budget and restoration

Record `sprint9`: **79.73 of 90** minutes, 29 launches, 66 entries, no adjustment. Sprint 8's 103.42/115 is untouched. Batches `sprint9:b1`, `b1b`, `b1c`, `e1` to `e3`, `f1`, `g1` to `g3`. The balance is at the launch guard's admission floor, so no optional row followed.

Offline on sprint9g: Candidate 9 of 9, 3.55 minutes, with the three focused reader suites.

At closeout: Mods tree empty, the same as each run's captured start; no game process; no lock; no candidate installed; protected saves restored per each result.

## Failed evidence retained

`a01` row name unregistered; `a02`, `a03` Smite never started, then first action; `e02` reader, re-evaluated; `e08` base-game registration; `20261104-c7e-final-B901-r31` stale assertion, after removal itself passed; `-B901-r32` refused because its source had not passed. All under `runtime-evidence\`.

Owner route: [owner play script](SPRINT9-OWNER-PLAY-SCRIPT.md). Notes: [release notes](SPRINT9-RELEASE-NOTES.md).
