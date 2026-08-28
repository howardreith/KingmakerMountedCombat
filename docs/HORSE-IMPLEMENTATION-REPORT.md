# Horse implementation report

## Stabilization checkpoint - 2026-08-28

Human review of exact dev.23 supersedes the earlier no-Horse report: a real Ranger Horse appears, is visible, selectable, controllable, mountable, and retains the mounted relation. This confirms the level-up/spawn repair and native Horse foundation. It does not yet prove save/reload, area, ordinary death/recovery, respec, or final presentation.

The supplied side and three-quarter views identify one Horse-profile defect: the rider pelvis is too high and the thigh/foot targets are too wide. Dev.24 changes only `medium-humanoid-horse-v1`: pelvis Y `0.02 -> -0.12`, foot targets X `+/-0.305 -> +/-0.18`, and knee hints X `+/-0.38 -> +/-0.20`, with small Y/Z adjustments recorded in the durable journal. It leaves Mammoth transforms, horse scale, native assets, and every other rider profile untouched. One human visual checkpoint remains mandatory.

Dev.24 also replaces the ambiguous direct-damage qualification with one bounded, exact same-Horse comparison: ordinary hostile stock `UnitAttack`/rule damage versus direct `Damage` assignment. It observes the native life-state event, HP/threshold/state, AwakeUnits scheduling, animation, roster, and reciprocal ownership, and uses only stock resurrection/full restore for recovery. It does not synthesize an event or force death. Complete applicable offline gates pass; live evidence remains pending.

## Explicitly authorized dev.23 test deployment - 2026-08-28T00:55:52Z

Status: INSTALLED FOR FOCUSED HUMAN TEST; automated lifecycle qualification remains `BLOCKED — CRITICAL`.

At the user's explicit direction, exact dev.23 was installed through the guarded deployment helper after its lifecycle limitation was disclosed. Installed version is `0.1.0-phase3b-dev.23`; DLL SHA-256/MVID are `eb946528bd7e0518dee61217de584f5c6ab8413de6242facc79143f9d5c6f9b1` / `511f3511-2392-4f7e-a7ca-643701ced087`, exactly matching immutable package SHA-256 `e2a903503f415fb96c69104731e1ebedc517ff4db45b1014a6aa4e460add67a2`. Installation and independent package-bound verification passed, and foreign Mods remained unchanged.

This is test access, not technical acceptance. The focused human boundary is Ranger selection/commit, persistent visible Horse ownership/control, save/reload, ordinary damage/death/recovery, and respec removal. Mounted profile review remains paused.

## Dev.23 bounded lifecycle blocker - 2026-08-27T23:34:08Z

Status: `BLOCKED — CRITICAL` for complete unmounted Horse qualification; no current Horse Alpha artifact is installable.

The requested Ranger creation/spawn repair is technically demonstrated. Exact dev.23 run `20260827T230400Z-horse-levelup-dev23-passA` used clean published commit `a2f053f86f7993d9e60a43dabb0dc7b7d48c7e00` and passed registration `13/0`. Its Horse row passed every creation, persistent ownership/view, direct control, stock movement, natural-attack, RT Bite, and TB Bite gate reached. Four genuine Ranger commits select Horse at Ranger 4/rank 1, and the same exact Horse remains reciprocal to its owner with the native Large HorseRiding view.

The row nevertheless remains immutable restored `FAIL 21/1`. The exact event-backed lifecycle probe began with a conscious, awake Horse at `11` HP and applied `27` direct damage, but observed no exact `Conscious -> Unconscious|Dead` `IUnitLifeStateChanged` event during 30 seconds. Recovery and respec cleanup were not reached. This is a genuine qualification gap, not enough evidence to claim ordinary combat immortality or to waive lifecycle behavior.

The independent audit passed exact suite/save/Mods/Baseline/Working continuity and zero runtime residue before evidence read. Deployment inspection reports KMC absent. The dev.23 package is retained as immutable diagnostic evidence but was not installed or designated for human testing. Mounted Horse review, Paladin Divine Steed, horse-branch merge, and public release remain frozen pending a newly authorized lifecycle decision.

## Dev.19 registration failure and dev.20 narrow repair - 2026-08-27T16:01:32Z

Status: IN PROGRESS - no current Horse Alpha artifact; mounted review is paused.

The clean dev.19 package reached the guarded live process but failed before Ranger level-up. Run `20260827T143000Z-horse-levelup-dev19-passA` is immutable `FAIL 4/14`: the production service rejected the live `BlueprintFeatureSelection.Features` field because it was not the exact seven-entry stock array. Independent audit passed exact suite/save/Mods/Baseline/Working restoration, all three transaction records, and zero runtime residue.

This failure narrows the contract. Installed Kingmaker's selectable `Items` enumerate `AllFeatures`; the separate live `Features` field is not the Items-authoritative seven-entry array. Dev.20 leaves `Features` reference/content untouched and applies KMC's append-once, compare-before-restore lease only to exact `AllFeatures`. It retains exact single registration in the canonical list and GUID dictionary, base-game DLC entitlement, `Animal Companion — Horse`, one eligible Items entry, native AddPet, correct Ranger-4 rank-1/pet-level-2 behavior, and activation/pet/master telemetry. It changes no mounted profile, Mammoth behavior, command/action ownership, or persistence policy.

Dev.20 passes source `21/0`, Release build, component `254/0`, visual/source-order `17/0`, harness `232/0`, and exact assembly `357/0`. It still requires one clean unmounted package/suite/runtime result and independent audit before installation for focused human Ranger level-up/save/reload testing.

## Human creation failure and bounded stabilization - 2026-08-27T14:16:40Z

Status: IN PROGRESS - dev.17 is no longer a current Horse Alpha artifact; mounted review is paused.

The human Ranger test saw and selected the dev.17 Horse option but did not obtain a visible companion. The exact package and failure evidence remain preserved. The prior automated creation claim was not sufficient because it inserted the rank and Horse facts directly instead of executing native level-up.

Dev.18 corrected that coverage and produced decisive, independently restored evidence: four real Ranger `LevelUpController` commits exposed and selected `Hunter's Bond -> Ranger companion -> Horse`; after the fourth commit, the owner had the exact Horse feature and a non-null exact pet. The immutable run `20260827T123600Z-horse-levelup-dev18-observation` remains `FAIL 17/2`, but both failures were diagnostic: Ranger level 4 grants effective companion rank 1, not rank 4, and native selection records use Ranger progression level 4 rather than total character level 11. Native AddPet maps rank 1 to pet target level 2.

Dev.19 is the bounded attributable repair. It registers each KMC blueprint exactly once in both the canonical list and GUID dictionary; updates and restores both Ranger `Features` and `AllFeatures`; assigns base-game DLC entitlement; presents `Animal Companion — Horse`; validates exactly one eligible selection item; observes feature/pet/master identity in normal play; and tests the correct Ranger-4 rank/progression result. It does not change AddPet spawn semantics, mounted behavior, the Mammoth profile, or persistence policy. Current offline gates pass source `21/0`, Release, component `254/0`, visual `17/0`, harness `232/0`, and assembly `357/0`. One clean unmounted package/suite/runtime result is required before dev.19 can be installed for focused human save/reload testing.

The older “Final technical qualification” section below is historical and superseded at the player-facing companion-creation boundary. It is not a merge or test authorization.

## Final technical qualification - 2026-08-27T03:40:27Z

Status: PASS (technical) - private-alpha package installed; human visual/gameplay review required.

### Exact identity

- branch: `codex/mounted-combat-phase3-horse`
- accepted horse implementation commit: `04a86870322f136bc3d7423b2e0ef31cf06d4145` (local and remote equal when the package was created)
- version: `0.1.0-phase3b-dev.17`
- package: `C:\Dev\KingmakerMountedCombatLab\artifacts\KingmakerMountedCombat-0.1.0-phase3b-dev.17-diagnostic.zip`
- package SHA-256: `5d61b8febad67637954ad52f7e0bf8f6081fc2ffa87266407721fc00b4d5585e`
- manifest SHA-256: `75bb23b5289cce77799f8001f6966346cd324ac80c787fe1bcf49ea4e0963ced`
- DLL SHA-256: `505c5c983ad94bbfc7e287284743427bc331d90fd6d4f9d6aacc16fe653e6875`
- DLL MVID: `4d9fff51-a040-41d4-b642-6f433c7a4b6a`
- `Info.json` SHA-256: `ae8693f40687afd68598a19833529fb7362c2d04fd09812124b7a4b2cd246744`
- package contents: `KingmakerMountedCombat/Info.json` (335 bytes) and `KingmakerMountedCombat/KingmakerMountedCombat.dll` (1,339,904 bytes)

The coherent closure documentation/harness commit is `a5c5d513f0cb4e281803a0e7c895119377610611`. Final local/remote branch equality is recorded at handoff after the following ledger-binding commit. The package remains bound to implementation commit `04a8687`; later documentation and harness-parser commits do not change its bytes or expand the accepted production implementation.

### Native assets and owned content

- Kingmaker native riding horse: `CR1_HorseRiding`, GUID `9e9e75c484e68734487e609714565202`, Large, view asset `5e0b93738ad54dd4ba101b3513ac4590`, `HorseRiding.prefab`.
- Native pony: `PonySummoned`, GUID `3f95557fc806db741b500a5735990841`, Medium, view asset `447d2907feec82545b3773fbb4709588`, `Pony_02`. It is a separate prefab/mesh/rig family, has no riding-horse `Chest` or stirrup geometry, and was not resized or reused.
- KMC blueprints: `AnimalCompanionUnitHorse` `4016c7db400ab721ff125aef9e65e202`; `AnimalCompanionFeatureHorse` `7db7c50677e39f09feef56f3831fc723`; `AnimalCompanionUpgradeHorse` `98e651899e6278d938de77af1d69bd32`.
- Horse contract: Large, speed 50, abilities 16/13/15/2/12/6, Bite `35dfad6517f401145af54111be04d6cf`, Hoof `b0e472a49ff2a294f93faa3ab757a4a5`, natural topology Bite/Hoof/Hoof. Exact native/Call of the Wild compatibility accepts committed stock levels or the native target-XP manual-leveling handoff; KMC does not duplicate the update.
- Ranger integration appends one KMC option without replacing stock choices. Runtime proves exact `7->8->7->8` enable/disable/re-enable behavior and native `AddPet`/`SetMaster` ownership.
- Mounted profile: independent `medium-humanoid-horse-v1`, native `Chest` anchor, authored stirrup evidence L `(0.305183,-0.122728452,-0.04402188)` and R `(-0.3051833,-0.122732729,-0.0440214761)`. No Mammoth offsets or pose values are reused.

No Wrath code, asset, model, animation, assembly, or blueprint was shipped. Exact read-only Wrath inspection indicates a literal shared turn controller with separate rider/mount ledgers and paired commands; KMC retains the functioning separate-turn overlay model for this alpha. A broad generic control-model port was neither required nor authorized.

### Qualification result

Stable suite `20260827T005000Z-horse-mounted-dev17-suite1` has snapshot SHA-256 `5337b241cd43839e2a00d46cb6d9a60038b0d28b2e663b9a1be99b0e10134058`; exact WhatIf purity and live admission passed. Guarded run `20260827T014000Z-horse-mounted-dev17-passF` produced immutable in-game `PASS 51/0`: registration `13/0`, horse aggregate `38/0`. Its game result SHA-256 is `42d599476b9f0b4b67a072fc80a03ffde948fd8d9a75d7870f0497b6a2386723`; evidence manifest SHA-256 is `a2c7a87bf42662d8a926f5ba24e23641b15144cb979bb3dd50c35492b38ed09c`. Independent audit ran before evidence interpretation and passed exact suite/save/Mods/Baseline/Working identities, all restoration booleans, and zero process/lock/sentinel/transaction/live-deployment residue.

The run proves unmounted creation, progression handoff, ownership, selection, movement, RT/TB Bite, death/recovery, respec, cleanup, and isolation. It also proves target-selected Mount, the independent horse profile, mounted RT/TB movement and transitions, exact Rider primary, exact Horse primary, explicit dismount, and cleanup. Rider primary retains rider actor/command/resource ownership and exact one attack/roll/damage chain. Horse primary retains horse actor/command/resource ownership, Bite at `AdditionalLimb[0]`, terminal Success, one child, one attack/roll/damage chain, and zero repaths.

The original external wrapper result for that run remains immutable historical `FAIL 0/1`: its strict PASS parser omitted five observations already emitted by the game. The parser now validates those properties and rejects altered values; the unchanged game result passes direct schema revalidation `33/0` and `39/0`, and the complete harness passes `232/0`. No second game run was performed or needed for this external-only defect.

Same-package targeted Mammoth run `20260827T030300Z-mammoth-primary-dev17-passA` passes `62/0`, with result/manifest SHA-256 `91994552d5abf43060e814aa3c4fd1ec9e47ba5c4740d9075bae55e4cc7a513d` / `8e4b85f144950d60cd1dcd233fd04837afe24bd84bbf363fb363facb0cdc8450`. Mammoth remains exact `PrimaryHand` weapon `5d7d23f5e35254d4bb087f7476163509` owner, with one attack/roll/damage for 25 damage, Mammoth-only Standard cost, unchanged rider resources, zero repaths/duplicates, healthy presentation, exact cleanup, and independently audited restoration.

All final applicable offline gates pass against the closure tree: source `21/0`, Release build, component `254/0`, visual/source-order `17/0`, harness/protocol `232/0`, assembly `349/0` (`325` Kingmaker + `24` Wrath), PowerShell parser `28/0`, JSON parser `7/0`, diff check, package validation `10/0`, and prohibited-payload validation.

### Accepted limits and human boundary

This private alpha does not provide stock right-click mounted attacks, mounted auto-attack, a unified Wrath-style turn, mounted ranged combat, persistent mounting, automatic remount, Paladin Divine Steed, or public-release quality. Rider/Horse overlay controls remain required, rider and horse keep separate TB turns, and mounting remains transient. Actual disk save/reload companion persistence, ordinary physical-pointer targeting, seat/stirrup/leg posture, locomotion and turn/stop/reverse feel, clipping/doorway appearance, selection/camera/action-bar feel, menus/fog flash, Wild Shape/revert presentation, and complete ordinary gameplay flow remain human-gated.

The exact package is already installed locally through the guarded deployment helper and verified byte-identical. A save that selected the KMC horse must be respecced to a stock companion and saved unmounted before uninstall. Exact commands and the focused checklist are in `docs/HORSE-PLAYTEST.md`. Paladin work remains design only in `docs/PALADIN-DIVINE-STEED-DESIGN.md`.

Superseding technical checkpoint (2026-08-27T00:31:21Z): dev.16's exact package/suite/run are `631a3e04d45b3f07847b9f0650e060155d3eca9648f9ad5c4715fb75e6fa9273` / `20260826T230300Z-horse-mounted-dev16-suite1` / `20260826T230500Z-horse-mounted-dev16-passE`. The independently audited restored result is historical `FAIL 43/1`; registration passed `13/0`, horse behavior passed `30/1`, and the exact mounted Rider-primary command passed actor, command, resource, terminal, cardinality, and duplicate gates. Horse primary alone was rejected as `NoEligibleWeapon`. Exact source and prior body evidence establish the cause: the resolver returned the horse's qualified first additional-limb Bite, but admission and child validation still required Mammoth's primary-hand slot kind. Dev.17 accepts only natural non-ranged primary hand or additional-limb index `0`, retains exact slot/weapon identity, and rejects secondary/later/ranged/non-natural candidates. Mammoth remains on the unchanged primary-hand path. Offline gates pass source/Release/component/visual/harness/assembly `21/Release/254/17/232/349`, parsers `28/0` and `7/0`, diff, and prohibited payload. One clean audited behavioral-repair aggregate remains before horse-alpha qualification.

Superseding technical checkpoint (2026-08-26T22:58:55Z): dev.15's exact package/suite/run are `03240f13eab6848bdbd6010ea862622ebb0493439b83b9e917ccc452a07c7ada` / `20260826T213500Z-horse-mounted-dev15-suite1` / `20260826T214000Z-horse-mounted-dev15-passD`. The independently audited restored result is historical `FAIL 41/1`; registration passed `13/0`, horse behavior passed `28/1`, and target-selected Mount, the independent horse profile, RT/TB mounted movement, and transition retention all passed. The sole failure was the diagnostic invoking Rider primary while native post-TB mode remained `Pause`; production rejected the click exactly as `LifecycleBoundary`. Dev.16 releases that scenario-owned pause before the unchanged production click, and cleanup restores the original pause. No production behavior changed. Offline gates pass source/Release/component/visual/harness/assembly `21/Release/253/17/232/349`, parsers `28/0` and `7/0`, diff, and prohibited payload. One clean audited aggregate remains required before horse-alpha qualification.

Superseding technical checkpoint (2026-08-26T21:30:54Z): dev.14's exact package/suite/run are `c281da9f01f419aeddbdc5e78cd0f67f00e9b67266374e89c7d53e0321f7924a` / `20260826T200700Z-horse-mounted-dev14-suite1` / `20260826T201000Z-horse-mounted-dev14-passC`. The independently audited restored result is historical `FAIL 33/1`; registration passed `13/0`, horse behavior passed `20/1`, and the sole failure was a diagnostic deadlock at native post-TB `Pause` before exploration remount. Dev.15 releases that scenario-owned pause before requesting stock combat departure and waiting for the unchanged production Mount gate; the existing cleanup lease restores the original pause. No production behavior changed. Offline gates pass source/Release/component/visual/harness/assembly `21/Release/253/17/232/349`, parsers `28/0` and `7/0`, diff, and prohibited payload. The single final clean audited aggregate remains required before this report can claim horse-alpha qualification.

Historical status below: IN PROGRESS (superseded by final technical qualification above)

Superseding horse-alpha state (2026-08-26T20:02:37Z): dev.13 exact-package aggregate is historical restored `FAIL 33/1`: registration `13/0`, aggregate `20/1`. Both native attacks now pass exactly in RT and TB with one attack/roll/damage chain, zero unexpected pair attacks, and zero post-dispatch turn restarts. The only failure occurred later because the diagnostic attempted target-selected Mount in the same frame it requested native combat and mode exit; production correctly rejected that unsafe frame. Dev.14 waits for the exact visible/enabled production Mount availability, bounded to 20 seconds. Production horse/Mammoth behavior is unchanged; one clean audited horse-mounted aggregate remains.

Superseding horse-alpha state (2026-08-26T18:27:24Z): the exact dev.12 horse companion, independent mounted profile, and target-selected Mount implementation are clean, packaged, and published at `2c996e8e91868a33d57577d21f66b0cf9cda300a`. Aggregate `20260826T171000Z-horse-mounted-dev12-passA` is historical restored `FAIL 32/1`: registration `13/0`, aggregate `19/1`, and every reached contract through exact TB command admission passed. Its only failure was caused by the diagnostic's post-dispatch turn restart interrupting its own still-unstarted Bite after Kingmaker's private queued handoff landed. Dev.13 waits for the native queue to establish a real turn before requesting horse/rider ownership and requires zero post-dispatch turn restarts. Production horse/Mammoth behavior is unchanged; one clean audited horse-mounted aggregate remains.

Superseding Tranche B state (2026-08-26T15:03:02Z): dev.10 package/suite/WhatIf identities are clean and exact, and aggregate `20260826T142426Z-horse-companion-unmounted-dev10-passJ` is historical restored `FAIL 32/1`. Registration passes `13/0`; unmounted behavior reaches `19/1`; the TB Bite is reference-identical Standard on the exact ready horse turn. The sole failure occurs afterward because the controller consumes a preexisting queued next-unit handoff and replaces that first diagnostic turn before command start. Dev.11 requires two-frame stable turn ownership and reasserts public `StartTurn(horse)` at most once only after observing replacement. No production behavior changes. Full local gates pass; one clean audited aggregate remains before mounted-profile admission.

Superseding Tranche B state (2026-08-26T13:26:50Z): dev.9 aggregate `20260826T124422Z-horse-companion-unmounted-dev9-passI` is historical restored `FAIL 31/1`, but registration passes `13/0`, the unmounted row reaches `18/1`, and RT Bite passes exact `1/1/1`, forced D20 `3`, zero unexpected pair attacks, and `16` damage. The only failure is TB Bite `0/0/0` after scenario dispatch on the first merely admissible turn frame. Exact native turn contracts require the diagnostic to wait for Prepared/CanAct, unpaused, empty commands, idle hands/equipment, an available Standard action, a live attackable target, and exact selection before dispatch. Dev.10 binds that surface, removes the redundant post-prepare scenario interruption, requires exact Standard ownership and terminal success, and changes no production behavior. Full local gates pass; one clean audited aggregate remains before mounted-profile admission.

Superseding Tranche B state (2026-08-26T11:20:50Z): dev.8 proves the exact temporary-horse command now owns Standard and completes one correct RT Bite chain (`1/1/1`, `15` damage). Its sole compound assertion still failed because the horse validator required exactly one D20 event even though credited stock Mammoth critical evidence records four D20 events for one exact attack chain. Dev.9 adopts the established `>= 1` forced-roll contract, serializes RT/TB forced-roll and unexpected-pair counters, retains zero duplicate attacks, and changes no production behavior. Full local gates pass; one clean audited aggregate remains before mounted-profile admission.

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
- active horse-alpha version: `0.1.0-phase3b-dev.17`
- final version-bound offline gates: source `21/0`, Release, component `254/0`, visual/source-order `17/0`, harness `232/0`, assembly `349/0` (`325` Kingmaker + `24` Wrath), PowerShell parser `28/0`, JSON parser `7/0`, diff, package, and prohibited-payload validation

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

Dev.7 then established that Kingmaker merged the requested same-target attack into an already active native command. Dev.8 isolated the temporary horse at the explicit diagnostic dispatch boundary and proved reference-identical Standard ownership plus one completed RT Bite, one roll, and one damage event. Dev.9 aligned the horse gate with the already qualified `ForcedD20Count >= 1` contract and proved exact RT Bite `1/1/1`, forced D20 `3`, zero unexpected pair attacks, and `16` damage. Dev.10 proved the complete TB readiness and exact Standard admission boundary, then exposed a preexisting queued native next-unit handoff replacing the first diagnostic turn. Dev.11 requires stable turn ownership across two frames and reasserts the exact horse turn once only after that replacement is observed.

## Implementation ledger

| Area | Status | Evidence |
|---|---|---|
| Wrath command/turn model | PASS | `planning/WOTR-MOUNTED-COMMAND-MODEL.md` |
| Horse native file identity | PASS | `planning/HORSE-PONY-ASSET-AUDIT.md` |
| Pony comparison | PASS | credited audited run `20260825T180000Z-horse-native-asset-audit-repair-passB`, `21/0` |
| KMC blueprint trio | PASS | dev.17 registration `13/0`; exact KMC GUID/object identities and topology |
| Ranger selection | PASS | dev.17 exact `7->8->7->8` transaction and native ownership |
| Unmounted horse | PASS (technical) | dev.17 creation/progression/ownership/control/movement/RT/TB Bite/death/recovery/respec/cleanup; actual disk save/reload human-gated |
| Mounted horse profile | PASS (technical) | dev.17 independent profile, RT/TB movement/transitions, Rider/Horse primaries, dismount/cleanup; visual feel human-gated |
| Target-selected Mount | PASS (technical) | dev.17 arm then exact eligible click; overlay fallback retained; ordinary pointer feel human-gated |
| Mammoth regression | PASS | `20260827T030300Z-mammoth-primary-dev17-passA`, `62/0` |
| Paladin Divine Steed | DESIGN ONLY | `docs/PALADIN-DIVINE-STEED-DESIGN.md` |

All earlier failed packages and observations remain immutable historical inputs. The exact technically qualified dev.17 package is ready and installed for mandatory human review.
