# Sprint 9: integrated acceptance and release candidate

**PLANNING RECORD, written before any Sprint 9 launch. Results are added below as they exist.** No merge, tag, publication or installation is selected. Owner acceptance is not produced by an agent.

## Baseline

| | |
|---|---|
| Integrated `main` | `e14bee6b`, the real merge of PR #13 by owner order on 2026-11-04. Its tree has no diff against the reviewed head `91735ae0` |
| Branch | `codex/mounted-combat-sprint9-integration-release` |
| Last compiled candidate | sprint8m, `3a8f0c66`, DLL `a094ecb2...`, MVID `ba06a57e...`, 8,844,800 bytes |
| Negative control | `codex/mounted-combat-sprint8-negative-control` (`10044985`) is local, not an ancestor of `main`, and has no remote ref. Never packaged |
| Live state at planning | Mods tree empty, no game process, no open budget entry; Steam running |
| Budget | New record `sprint9`, 90 minutes. Sprint 8's 103.42/115 is untouched |

Product files changed since each sprint's final compiled source, from `git diff --name-only` excluding `Diagnostics/`: Sprint 1 53, Sprint 2 46, Sprint 3 43, Sprint 4 39, Sprint 5 27, Sprint 6 23, Sprint 7 8 (all Paladin class work plus one level-entry patch), Sprint 8 0. No Small-rider, seat or rig file has changed since sprint5v. Saving product code has not changed since sprint1q except a 3-line authorization file.

## Candidate decisions made before freezing

- **Version `0.1.0-chunk7b-preview.101`.** Readers branch on `^0.1.0-chunk7b-preview.N$`, so a new version shape would silently change what they check. The release name can differ from the version string.
- **UMM page text is a defect, not a cosmetic edit.** It still tells players Rider Primary and Mount Primary select an attack (retired in Sprint 4) and names only the Ranger Horse and only the KMC Horse as a save dependency. Fixed before the freeze.
- **`docs/CHUNK7-SUPPORT-CONFIG.md` is stale.** It says Charge is off by default, TB Charge is unavailable and one Small profile exists. Replaced by a current support section for this candidate.
- **Package route.** The established `scripts/Package.ps1`. Its `-diagnostic` file name is historical; contents are checked, not inferred from the name: exactly `Info.json` and the DLL. The automation host starts only for an explicit command-line request, which the request-free normal-player control exercises.

## Two narrow additions, both in existing families

- **`integrated-tb-feat-charge-manual`** (persistence producer and separate-process consumer). One rider in one TB fight: real off-turn Mounted Combat borrow, the debt charged once at the next native preparation, a real ordinary Mounted Charge on that turn, then a native Manual save with the second pair independent. Cold: nothing refunded or replayed, native renewal with no second charge, one further legal reaction. Replaces two separate feat and charge save pairs and is the coexistence claim.
- **`sprint8-paladin-tb`** (the small unexecuted neighbor). A native Divine Mount paladin on its Horse in TB: Call, refusal while mounted, Mount, Celestial Smite, a real attack carrying the bonus, Mounted Charge. Also reads the removal assessment for that owner.
- **`sprint3-approach-rt-spells`** selects the existing RT spell rows without the trap row, which cannot pass on this fixture and would otherwise force an aggregate FAIL.

## Outcome map

"Run" = current-binary native check in the named batch. "Retain" = earlier evidence on its own bytes, not current-binary execution, with the reason. Costs are budget minutes from earlier sprints' measurements.

| Outcome | Supported configuration | Latest evidence and dependency since | Decision | Route and prerequisites | Cost, batch |
|---|---|---|---|---|---|
| Base-game load, no auto-start, IDs register once | Kingmaker 2.1.7, UMM, KMC only | F7 r42 on preview.96; much changed since | **Run** | `chunk7-normal-player-control`, `-BaseGameOnly`, observer package, no KMC request, source six-Horse Manual; `-TimeoutSeconds 900` | 3, b3 |
| Conflicts fail clearly | Another mod editing the Ranger list or the bond slot | S8 a04 live drift (sprint8k); registration refusal offline | Retain, plus offline on candidate | Candidate tier | 0 |
| Genuine Auto overwrite and area transition | Two owned pairs, area event | S1 r11/r12; save code unchanged | **Run** | `chunk7-owned-two-pair-campaign / multi-area-autosave`, owner seed, `-EngineShapedSaveRoot`, area target, existing Auto source | 5.5, b2 |
| Quick rotation | Same | S1 r06/r07 | **Run** producer; retain cold r07 | `multi-exploration-rotation-quick` | 2.5, b2 |
| Manual overwrite | Same | S1 r08; save path unchanged | Retain | Not re-run: new-slot Manual is exercised by three cold chains | 0 |
| Meaningful mounted save, cold consumer | Working fixture, CotW and CMI | S6 c01, S7 a05, S8 t03 | **Run** | `integrated-tb-feat-charge-manual` save then load | 6.5, b1 |
| Authentic earlier-release upgrade | preview.211 Quick `83e92e1a...` with exact CotW and CMI | S1 r13-r2, r17; inputs verified present by hash | **Run** | `persistence-p05-load -UpgradeChain`, `quick` then `manual`, retained dependency selection | 5, b2 |
| Doors, ordinary movement, mount and dismount | Mammoth pair, RT | S2 r22 (sprint2v) | **Run** | `chunk4-traversal-core`, Working fixture | 3, b3 |
| TB door | | Never demonstrated; S2 proved no reachable combat-usable door in inputs | **Limitation** | Owner script names it; no route revived | 0 |
| Distant friendly touch | Mammoth pair, RT and TB | S3 r16, r17, r18; attack and movement changed since | **Run** both | `sprint3-approach-rt-spells`, `sprint3-approach-tb` | 5, b1 and b3 |
| Trap | | TRAP-01 never executed natively. Bounded inspection in this sprint: every owner seed's nearest active trap is at least 54.6 m away at another elevation | **Owner item** | Owner script; no speculative exploration | 0 |
| RT retarget, retired helpers, Haste, Stop, Hold, replacement | Mammoth pair | S4 r03, r08; S7 changed the pair attack command | **Run** | `sprint4-natural-rt` | 3, b4 |
| Hold-on-move neighbors | | S4 r08 (sprint4i) | Retain unless reserve remains | `sprint4-intent-rt` | 2.5, optional |
| TB ordinary actors and resources | Both actors | S4 r05 | **Run** | `sprint4-natural-tb` | 2.5, b4 |
| Multiple-pair isolation | Two pairs | S6 `S6-rt-isolation`, S7 a04 | **Run** inside the integrated case and the feat row | Reader demands both independent owners | 0 extra |
| Feat availability, RT defense, renewal | Riders with Mobility | S6 r07 | **Run** | `sprint6-feat-rt` | 2.5, b4 |
| Feat TB debt, Charge TB | | S6 t02, S7 a02, q03 | **Run** through the integrated case; rest retained | Forced-hit, Pounce, mount-first and reforce unchanged since S7 | 0 extra |
| Charge RT | Mammoth pair | S7 a01 (sprint7i) | **Run** | `sprint7-charge-rt` | 2.5, b4 |
| Charge queue, lane | | S7 q02 (sprint7l); no combat product change since | Retain unless reserve remains | `sprint7-charge-queue-rt` | 2.5, optional |
| Small female rig and geometry extreme, normal camera, Riding Dog acquisition | Halfling and Gnome on the Dog | S5 h2 (sprint5v DLL); no rig change since | **Run** | `sprint5-dog-rt`: also yields captures | 5.5, b5 |
| Same on the Horse, full pose matrix | | S5 h3 (sprint5v) | Retain | No rig change | 0 |
| Small dismount and cold load | Female Halfling, Dog | S5 f3/f4 (sprint5n) | **Run** | `riding-dog-exploration-manual` save then load | 5.5, b5 |
| Paladin level five, weapon, retrain, old character | Hired companion, CotW | S8 a04 (sprint8k) | **Run** | `sprint8-paladin-choice-rt` | 3, b5 |
| Mount path, pet progression, Call, Smite, Riding fallen mount | | S8 t04 (sprint8m) | **Run** | `sprint8-paladin-mount-rt` | 3.5, b5 |
| TB Call and Smite | | Never run | **Run** | `sprint8-paladin-tb` | 3, b1 |
| Paladin save continuity | | S8 t02/t03 (sprint8m) | Retain unless reserve remains; only UMM text and diagnostics differ | `paladin-bond-exploration-manual` | 6, optional |
| Mixed class, celestial tiers | | S8 q05 (sprint8l), t01 (sprint8m) | Retain | t01 exercised a mixed owner on sprint8m | 0 |
| Removal of a campaign with no permanent references | Stock Mammoth, mod absent | F7 r31, r32 | **Run** | `persistence-p07-save/prepare-removal`, then `p07-load/removal-no-dll`, `-OwnerStockRemoval`, run map; run id must match `*-c7e-final-B*-r31` | 5, b3 |
| Removal refused with references | Horse, Dog, feat, bond | Offline and S8 scan | **Run** inside `sprint8-paladin-tb` | | 0 extra |
| Clean uninstall from a dependent save | | **Not claimed** | Limitation | Rollback is the pre-upgrade save with its matching package | 0 |
| On-screen level-up, action list, appearance | | Never clicked by an agent | **Owner script** | At most three sessions | 0 |

Not selected: every old case; six-pair combat (no registry or world-lifetime product change since its last evidence); a pure-base-game playthrough. Base-game support is stated separately: registration and mounted control are run, but Paladin pet progression and smite refill without Call of the Wild are unobserved and are disclosed as unverified, not supported.

## Launch and budget map

| Step | What | Estimate |
|---|---|---|
| b1 pre-freeze | Package, suite; `integrated` save and load, `sprint8-paladin-tb`, `sprint3-approach-rt-spells` | 2.5 + 14.5 |
| Freeze | Candidate tier once, then package and suite if HEAD moved | 7.5 |
| b2 saves | Area Auto, its cold, Quick rotation, upgrade producer and consumer | 13 |
| b3 install, world | Normal-player control, removal save and absent load, door, TB touch | 13.5 |
| b4 combat | S4 RT, S4 TB, feat RT, Charge RT | 10.5 |
| b5 small, paladin | Dog row, Dog save and cold, Paladin mount, Paladin choice | 17.5 |
| **Total planned** | 23 launches | **about 78** |
| Reserve | Defects, optional rows | about 12 |

A b1 run on a DLL that the freeze leaves byte-identical counts as current-binary. A docs commit needs a new package, not new evidence. If the reserve goes, the optional rows stay retained and the matrix says so.
