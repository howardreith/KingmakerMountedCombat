# Mounted melee repair — handoff (0.2.16-alpha.2 private alpha)

Mission: [docs/missions/mounted-alpha2-melee-pose/MISSION.md](../missions/mounted-alpha2-melee-pose/MISSION.md).
Install, rollback and playtest: [PRIVATE-ALPHA.md](PRIVATE-ALPHA.md). Issue ledger: [ACCEPTANCE_LEDGER.json](ACCEPTANCE_LEDGER.json).

## 1. Outcome

- **Factory:** new foreground enrollment `kmc-codex-mounted-alpha2-melee-pose-20261124102246-64c1` (pinned controller
  `versions/85dece1c…`, project `kingmaker-mounted-combat`, plan `single`); draft PR #64 targeting `main`. The controller's
  final review follows on the records head; its result is recorded in the PR and the final report, not here.
- **Dependency:** PR #61 (Mounted Shield) was merged by the owner's instruction before enrollment (merge commit
  `783b2b3d`, tree identical to the accepted archive `122a9f94`). The new branch starts from that main, so the `--target main`
  route applied; the accepted Shield enrollment, branch and archives are untouched.
- **Package:** `artifacts/KingmakerMountedCombat-0.2.16-alpha.2-mounted-alpha2-ea112a71-diagnostic.zip` (identities in section 4).
- **Native game launched: no.** Zero guarded starts. This session has no desktop-control tool, and the guarded
  save-backed scenarios still require the historical qualification fixture suite (unchanged since the Mounted Shield run);
  the shared machine was also in a peer project's game window. All N01–N10 rows are `not_run`, assigned to the owner playtest.
- **Core blockers:** none known. MMR-07 is an investigated visual limitation (native bite asset present; visibility unobserved).

## 2. Changes and evidence

| Issue | Cause / supported explanation | Change (commit) | Current tests/evidence | Native/owner |
| --- | --- | --- | --- | --- |
| MMR-01 seating | The animated saddle freezes `seatFromSource` once at attachment from the rider's pelvis in that frame, so the height follows the rider's animation when Mount Companion fires (cast, approach walk blending out) or a view's first frame after load; a remount from idle seats lower. Frame lease, release and replacement were verified non-accumulating. | Horse calibrates from a declared basis: the measured preview.3 pelvis−Chest `(-0.1892, 0.8601, 0.0175)` mount-root-local, the basis `HorseAnimatedSeatCorrection` was tuned against (`Domain/AnimatedSaddleCalibration.cs`); Mammoth keeps the observed sample, Small profiles their fixed path; the seat log line reports `seatCalibration` and the observed sample (`1594475b`). | Component test MMR-01 through the production policy and `AnimatedSaddlePosition`. | Pending (N01, N10). |
| MMR-02 TB approach | Installed `TurnController.IgnoreClick` drops a click unless `PathVisualizer.IsCurrentPathForPoint(rider position, approach point, approach radius)`; with the rider selected the visualized locomotion is the mount's, so an ordinary attack on a hostile just beyond reach can be dropped before any pair intent exists (a target in reach needs no path: "moving closer works"). Inferred from IL, not a trace. | Postfix on the existing `IgnoreClick` patch admits only that dropped click: rider boundary context, rider selected, commands empty, predictions current, not terrain, no ability, hostile living target, and `CanMovePairedMount` true (`Domain/PairedAttackClickAdmissionPolicy.cs`, `b395fbf1`). The pair command's existing approach then uses only the mount's remaining movement. | Component test MMR-02; contract checks of tokens `06000C2F`, `04000691`, `0400068B` and that IgnoreClick calls `IsCurrentPathForPoint` (`06007018`). | Pending (N02, N03). |
| MMR-03 horse participation | Default paired TB passed `mountIsLegalActor = !turnBased ∥ MountActor ∥ UseLegacyUnifiedTurn`: a rider click deliberately excluded the horse. | `UnifiedMountedStockAttackPolicy.MountJoinsIntent`: under paired activation an ordinary (non-automatic) intent includes the horse only while it is addressable on that activation (`PairedActivation.CanAddress`); `DecideNext` still needs its Standard action and the dispatcher rechecks capability/range (`b395fbf1`). | Component test MMR-03 with genuine `PairedActivation` states: rider then horse; spent, ended, adopted-used, other boundary, suspended, ending, split and unprepared horses refused; explicit/automatic/separate-turn unchanged. | Pending (N04). |
| MMR-04 RT automatic engagement | Most likely the accepted automatic intent stalls on readiness: the pair command dispatched while its actor waits initiative sits unstarted (native `ShouldStartCommand`; its approach radius is infinite) and KMC waited during cooldowns. A refused or absent native proposal (Hold, AI off) was not excluded by evidence. | Real-time readiness excludes an actor still waiting initiative (`UnitCombatState.CanActInCombat` only in combat) and the intent approaches meanwhile (MMR-05). No new targeting: the rider brain's native proposal remains the target (`5ac516ef`). | Component test MMR-04/05; installed `CanActInCombat` contract. | Pending (N05). |
| MMR-05 RT command response | Movement waited for strike readiness in two layers (KMC `Wait`; native start gate before the wrapper's delegated move). | `DecideNext` returns `ApproachWhileWaiting` in real time when no actor may strike, the melee target is out of the rider's reach and approach is permitted (not ranged, not a held automatic intent); the controller owns one non-player mount move (radius from the shared `CalculateDelegatedMoveApproachRadius`), replans on target drift, yields to any other mount command, and interrupts it before dispatch and on every intent end (`5ac516ef`). | Component test MMR-04/05: approach while waiting; dispatch only when ready; in-reach/ranged/held wait; live command, TB and default callers unchanged. | Pending (N06). |
| MMR-06 horse Trip | Installed data: stock Mammoth `AddFacts` = `[TrippingBite, NaturalArmor7, TripDefenseFourLegs]`; `TrippingBite` (`f957b444…`) = `ManeuverOnAttack(Bite, Trip)`; inert on a Mammoth (no bite), fires on the KMC Horse's bite. Dog and Wolf carry the same fact legitimately. | The Horse drops inherited intrinsic attack-maneuver facts at registration (logged) and an existing horse loses only a sourceless inherited one once on load (`HorseCompanionBlueprintService.InheritedFacts.cs`, `bd4cc5e0`). | `Test-MountedAlpha2NativeContract.ps1`: installed offsets for Mammoth/Dog/Wolf/TrippingBite/component; genuine-object filter keeps order and the stock array; a progression-sourced copy, natural armor and trip defense are kept. | Pending (N07). |
| MMR-07 bite animation | Installed horse animation bundle (`resource_2f179560…`, SHA-256 `3edb2116…`): `HorseAnimationSet_Bite` is a `UnitAnimationActionSpecialAttack` with one clip `BiteAttack`; `HorseAnimationSet_Hoof` has `Right_Hoof_Attack`/`Left_Hoof_Attack`. The ordinary Bite/Hoof/Hoof routine plays the stock Bite handle (adopted, never replaced). Native `UnitAttack.ConfigureAnimations` gives a routine one `SpeedScale` over all its clips, a plausible reason a short bite reads as invisible. | No animation or timing change (native owns it). The `Mounted combat command terminal` line now reports `horseBite={action, source, speedScale, routineAttacks, acted, finished, interrupted}` (`1fcde471`). | Contract: bundle identity, handle `SpeedScale`. Bundle decode by a read-only analysis reader (outside the repository); identifiers only, no assets copied. | Pending (N08). |
| Mounted Shield | Unchanged feature. | None. | `Test-MountedShieldNativeContract.ps1` 114/0, selection rollback 68/0, icon `fd519874…` in the packaged DLL. | Owner baseline E04/E05 (+2) retained; N09 pending. |

## 3. Validation and limitations

- **Controller focused gate** `verify --gate focused -- -Tier Fast -Focused Test-MountedAlpha2Focused.ps1` on `ea112a71`:
  PASS in 84.2 s, receipt `gate-focused-ea112a718195-d43d449b6c4-ae9890c77f2-20261124T155542472057Z`: build on the pinned UMM
  0.31.1 reference, components 1529/0 (includes the new MMR-01/02/03/04-05 tests), Validate-Source 153/0, and 13 focused
  children: alpha-2 native contract 43/0, presentation contracts 239/0, Mounted Shield native contract 114/0, selection
  rollback 68/0, Mounted Archery 54/0, Ranger selection 183/0, replay subscriber 29/0, localization keys, UMM build reference
  9/0, native loader 101/0, frozen source 23/0, UMM bindings 25/0 against 0.31.1 and 0.33.0. Offline only. Budget
  `mounted-alpha2-melee-pose-20261124`: 1.4 of 90 min after it.
- **Package:** `scripts/Package.ps1 -ArtifactQualifier mounted-alpha2-ea112a71` with Validate-Package 11/0; read back below.
- **Known baseline failures (not selected; identical on the accepted Shield build `122a9f94`):** `Test-MountedAttackStallRepair.ps1`
  (Run: parameter count mismatch) and `Test-WorldOwnershipContracts.ps1` (missing field `pairSaveReady`).
- **Not run:** any game start, fixture creation, gameplay, visual or save/load observation (N01–N10). No native
  transaction ran, so no restoration was needed. The pre-native supplemental review was not used (no admissible native route).
- **Detached checks are not gameplay:** the component tests exercise the production policies; the contract script reads
  installed data and runs compiled code on genuine objects created without Unity.

## 4. Exact identities

- Base: main `783b2b3dea3303e10cc4379dfa0a36ceac66a76b` (PR #61 merge; tree = accepted Shield archive `122a9f94`).
- Packaged and gated source: `ea112a71819520e9df915eff721ba42d042792bd`. Later commits are records only.
- ZIP: `KingmakerMountedCombat-0.2.16-alpha.2-mounted-alpha2-ea112a71-diagnostic.zip`, 4,284,987 bytes, SHA-256
  `b9168c6225bc34598e172aefdce124c1e4fdcc876dbc9dd012c203313a6dc2e7`; Info.json Version `0.2.16-alpha.2`, ManagerVersion `0.31.1`.
- DLL: SHA-256 `16c844db1e5b51a086b9ee33951339fcdecb674bd2f462336e2435eb5385112c`, MVID `00c67828-d35b-4bcf-ad82-6675a35f92f0`.
- Embedded Mounted Shield icon: SHA-256 `fd519874e9d231946aad68c46f93a4e0a3a2ea04d4037807e6c7a4d36629f9d0`.
- Previous alpha (rollback): `0.2.16-alpha.1`, ZIP `d7143ba3d8b297f5eacc2a71e9c5f4fd0ae1df57a4cbbdb52ff515c656b91545`.

## 5. Owner decisions

Playtest per PRIVATE-ALPHA.md; merge/release decisions are the owner's. No merge, tag, release or installation was done
for this alpha.
