# Chunk 7B–7D — IN PROGRESS

**b210 pilot fixes (offline; no launch).** b210 on `187282c7`: r09, r32, c1 and c2 PASS. Three fixes follow.
- c3 profile recovery (`8ca622c8`, harness). c3 crashed natively (system commit exhausted). Transaction recovery PASS; two Unity session
  PlayerPrefs counters remain (`b210-c3-incident.json`). New `-AfterTransactionRecovery` mode of `Recover-KmcPersistenceProfile.ps1`:
  - Admission requires: the completed transaction for the snapshot token (combined, Mods and save states all restored, every
    proof true); no profile-recovery receipt; no lock, or only this run's own stale profile-recovery lock.
  - It takes or adopts a 'profile-recovery' lock and re-checks everything through the held handle; restore then runs through
    `Restore-KmcPersistenceStartupSettings` and `Assert-KmcPersistenceProfileUnchanged`.
  - WhatIf against c3: PASS. Executor command (no lock held, no Kingmaker process):
    `Recover-KmcPersistenceProfile.ps1 -RunId 20261026-c7e-b210-c3 -SnapshotSha256 15805cd2e7989b7274b82d813a06d138318aef0f6c107f927a143a4d113bbf27
    -CurrentParamsSha256 ca5d402248723c3578c4a35a21d69b749d7459a4a74c6f406e16f0d2fa9386c4 -CurrentPrefsSha256
    ee15261614aaef3b36fec5d6cadf07878ba4617b7318e4de44bf75a6f3d2eed5 -AfterTransactionRecovery -Confirm:$false`
- s1 reader (`1685ebb1`). Row 66: the mounted `avatarLocalScale` (1.00000024, 1, 1.00000024) is float32 re-parenting rounding.
  Per-axis tolerance 1e-5; every other body field stays exact; 1.001 refuses (`b210-s1-incident.json`).
- r39 cause (`c431df8c`, product diagnostics). A native hostile group (faction b1525b4b…) aggroed at 8.30 m, inside its 8.5 m vision.
  - Before any input, the stress requires every conscious non-player-faction unit to keep its own native `VisionRangeMeters`
    + 3 m (2D) from the 12 pair actors and the 6 waypoints (`evidence.sixPairHostileClearance`); a breach refuses as uncovered.
  - The classifier now judges KMC-issued and pair commands; on r39's combat frame the party's native AI had already queued its own.
  - The reader validates the clearance when present. Known limitation: the record is optional, because older accepted evidence
    predates it. Also not addressed (low): the record's `kmcIssued` flag covers only the harness's own commands.
- Review: CHANGES REQUIRED twice (an unrecoverable stale profile lock, then a second handle on the held lock), then APPROVE.
- Tests: profile protection 130/0, base-game isolation 98/0, F09 Small 69/0, six-pair reader 68/0, orchestration 24/0,
  OwnedPairs 763/0, build 149/0, unit 881/0, Fast 3/0. Pre-existing at HEAD: SmallManualEvidence 70/1.
- New Release DLL `35b3829cb7fc02dff78a31838862969b8e0b05e6dad2593738eb3be1b6da6db5`. Product-diagnostics changes since `fa819113`:
  r09 `5dfdc897`, r39 `ba545808` and `c431df8c`.

**F2 r39 six-pair combat entry now recorded (offline; product diagnostics plus reader; no launch).**
- r39 (`20261025-c7e-final-B15-r39`, row 39, six-pair F11 stress) game 4/1: "Six-pair exploration entered unexpected native combat."
  after sample 5 in cycle 3. The timing and ground positions are identical to b190's, and the evidence held no hostile or initiator data,
  so the cause is unknown and probably nondeterministic (`final2-B15-r39-incident.json`).
- `ba545808`: the guard still fails the run. Before it throws, `RefuseSixPairCombatEntry`
  (`Chunk7WorldScenarioEngine.SixPairCombatEntry.cs`) records `evidence.sixPairCombatEntry` read-only: step, cycle, index, sample,
  input and preparation state, and for each combatant its id, blueprint, faction, group, party, pair-actor, player-faction, awake, fog
  and position, its nearest pair actor and distance, and its group-memory enemies.
- The pure `Chunk7SixPairCombatEntry` classifier names `uncovered-native-hostile-engagement` only when a hostile combatant is
  present, every pair actor is command-free and no KMC input is in flight. A hostile combatant is not a pair actor (the Horse pets
  are outside Player.Party), not a party member and not player faction. Anything else is an unclassified FAIL with the unchanged
  message. The six-pair PASS reader refuses either refused-path key.
- New DLL for F3: Release SHA256 `1ddd700772460e7d2024e9fa8f8b33c876007bbe055c8fd132eef6fab837cd85`. Product-diagnostics changes since
  `fa819113`: r09 (`5dfdc897`) and r39 (`ba545808`).
- Review: CHANGES REQUIRED, then APPROVE. Fixes applied: pets excluded from the hostile count; the orchestration check compiles again
  through a refusal facade; unsettled preparation is counted as in flight. Remaining gap: nothing outside the game runs the real
  capture partial; only the build covers its native calls.
- Tests: Validate-Source 149/0, unit 881/0, OwnedPairsAuthority 762/0, LoadRefusal 71/0, RegisteredDisable 762/0,
  SaveBoundary 115/0, six-pair reader 52/0, six-pair orchestration 22/0, Fast 3/0.

**F2 r32 removal-observer UMM rule and F09 reader pins fixed (offline; harness only; no launch).**
- r32 (`20261025-c7e-final-B14-r32`, row 32, removal observer, no `-BaseGameOnly`) game PASS, outer FAIL: UMM pruned the owner's
  stale `<Mod Id="CheatMenu">` (folder absent) beside the expected KMC→observer replacement and a forward `LastUpdateCheck`; the lock
  stayed held (`final2-B14-r32-incident.json`). The removal-observer startup rule now shares the base-game rule's two native helpers
  (`Step-KmcUmmLastUpdateCheck`, `Remove-KmcUmmAbsentModEntries`): it admits the original or the original minus every entry absent
  from the run's own staged Mods tree (`Get-KmcRunStagedModIds`: the staged manifest sidecar, digest-checked against the transaction
  state, observer staging mode, must include `KmcRemovalObserver`; folder names matched case-insensitively); a prune of a present
  mod, a reorder, a changed attribute or an added entry still refuses; without a sidecar nothing is pruned. Params.xml is still
  restored byte-for-byte. Runner guard and recovery both pass the staged ids. Recovery `-WhatIf` against r32's exact pins PASS,
  `runtime-state` unchanged (`final2-r32-profile-recovery-whatif-fixed.log`). Executor (from a worktree at this HEAD, profile first):
  `Recover-KmcPersistenceProfile.ps1 -RunId 20261025-c7e-final-B14-r32 -SnapshotSha256 aabe22e5d087f55a25a39a5fdb8716a7150200047988723d0a99a4c81d9996c7
  -CurrentParamsSha256 17781485b7aa01b58c781f8605ea396b4d9b2629902d4357691366b4c173309c -CurrentPrefsSha256
  db8ae27195fded87bf1cd1302a1f860e76647c51fe6a0cc0190a2863760ae4c2 -Confirm:$false`, then transaction recovery if the lock remains.
- F09 (s1/r33 WhatIf refusal): the receipt's two reader pins now bind to their archived 6fb1eee8 blobs from any tree (`b4a86012`);
  the current readers also pass the F09 cold evidence; no other receipt pins live reader files.
- Review (r32): no must-fix; should-fix applied (case-insensitive folder presence; observer staging and observer id required).
- Tests: profile protection 94/0, base-game isolation 98/0, F09 reader pins 11/0, Fast 3/0.

**Coordinator decision (October 25): F2 is a discovery pass; F3 re-runs every row on a new candidate.** The runner's source gate is
not changed; r15 and r16 are re-run on F3; the re-evaluation machinery stays as committed (plan §8).

**F2 r09 companion-window fix for F3 (product diagnostics changed: a new DLL; no launch).** r09 (`20261025-c7e-final-B8-r09`,
`owner-seed-realtime-core`) game FAIL 6/1: mount A, already engaging the target before the companion input, made the native
engagement-controller attack of opportunity in the companion window (`UnitCombatEngagementController.Tick →
UnitCombatState.AttackOfOpportunity → UnitCommands.Run`, own reaction, Free) and was counted as a pair touch
(`final2-B8-r09-incident.json`).
- `NativeOpportunityAttacks` accounts such an attack apart, strictly: one closed stock `UnitAttackOfOpportunity` (Free) per
  identity with exactly admission-before/after and cost-before/after of one pair actor, aimed at the provoking target, caller chain
  exactly Run < AttackOfOpportunity < UnitCombatEngagementController (Tick; no KMC frame), finished, its reaction spent once
  (n → n−1, n ≥ 1), no Standard/Move/Swift rise, in no pair command slot; at most the actor's reactions; the window's reaction check
  admits exactly n fewer (or natively restored with no cooldown). Everything else stays a pair touch. Applied to the companion window
  and, consistently, to pair B in the player-click window; the reader derives the same independently (`companionAction.pairNativeOpportunityAttacks`,
  `playerClick.pairBNativeOpportunityAttacks`).
- Residual: the other neighbour-silence checks (`NeverSpentNatively`/`SameActorBudget` in TB, reaction, staged, attack-mode and life
  windows) still refuse a neighbour's native attack of opportunity; none failed in F2 so far.

- Independent review: no must-fix; should-fix applied: the reaction-restoration clause now follows the native RT contract
  (`UnitCombatState.AttackOfOpportunity` sets a 5.4 s cooldown on first use; `UnitCombatCooldownsController` restores the count at
  zero): only when the cooldown recorded at the last attack fully elapsed in game time by the after snapshot and no prepare/clear
  of the actor occurred; the capped caller chain must reach `Kingmaker.Runner.Update`; the actor must already engage the target in
  the before window; cost rows must be native `Success`. Note: an AoO animation still running at a poll would hard-fail
  `PairIdle` (not seen in r09).
- Tests: owned reader 1285/0 (13 new), actual-method harness (refused proposals + AoO accounting) 0 failures (24 new), owned source
  323/0, Fast 3/0, Release build `f39cbbb3…` (a new DLL for F3). Old F2 artifacts now lack the two new fields and are refused (F3 re-runs every row).

**F2 r15 re-evaluation redone on the full offline verdict path; r16 still blocked by the runner's source gate (offline; tools only).**
- Re-review findings (M1 partial verdict path; S2 old FAIL not re-proven; S3 native calls under Stop; S4 evidence root; S5 self-test)
  fixed in `4552f083`, `3219903d`; a fresh review then found the r16 blocker below plus should-fixes, fixed in `134f3a18`, `5dc584ff`.
- `tools/chunk7-7e/final-reevaluation.ps1` (deriver admission) + `final-reevaluate.ps1` (verifier): New re-runs all of
  `Test-RuntimeGameResult -RequirePass`, the base-game native inventory (admission + isolation receipts) and the runner's owned-pairs
  post-game checks on the retained profile; Old re-proves the exact error from the run commit (LF archive, temporary tree, removed).
  Six NAMED stand-ins are recorded in the receipt. The receipt lists every `scripts/` change by blob; nothing under `scripts/` may
  change after its corrected reader; product/platform paths (src, Info, version, solution, fingerprint) must be unchanged.
  `final-reevaluation-selftest.ps1` 15/0.
- r15 receipt `analysis-cache/chunk7-continuation/final2-B4-r15-reader-reevaluation.json` SHA-256 `39c0c08b…` (corrected reader
  `5dc584ff`, reader fix `95a69cfc`); old FAIL reproduced, new PASS, run directory and profile unchanged; the manifest-only receipt is
  kept as `.superseded-1.json`; the intermediate full-path receipt (`3e673648…`) was overwritten, not retained.
- Deriver proof (`final2-r16-derive-reevaluation-proof.log`): r16 derives from `{r01, r15+reevaluation}`; plain, bad-hash, traversal
  and verdict-less maps refuse. Reproduce 73/0, Fast 3/0.
- **Open, needs a decision:** the runner's own source gate (`Assert-KmcChunk7OwnedSourceCompletion`) requires r15's result PASS
  and its base-game receipt flags true, so r16 cannot launch from r15 even with a derived selection; and r16 needs the corrected cold
  reader, which the F2 package (harness `fa819113`) lacks, while a new package breaks the deriver's same-commit predecessor check.
  Options: teach the gate the receipt (reviewed harness change, new package and suite) or re-run r15/r16 on a successor package.

**Final F2 r15 reader-only defect fixed and re-evaluated (offline; harness only, `src/` untouched; no launch).** r15
(`20261025-c7e-final-B4-r15`, zero-mounted TB checkpoint save, preview.94) game PASS 4/0, runner FAIL "Retained native actor was
released before or after its first legitimate native round." (`final2-B4-r15-reader-defect.json`).
- Cause and discriminator: `d96a4b06` changed the checkpoint driver's independent-turn label from `independent-native-mount-<id>` to
  `independent-native-actor-<id>` and, in the same commit, the product from chunk7b-preview.54 to .55; the reviewer's sweep of every
  version-bumping commit confirmed .3–.54 write only mount and .55–.94 only actor. The evidence has no label schema, so its own product
  revision (productVersion, DLL-bound) selects one family for every kind, source and cold (`95a69cfc`); the other family and
  unrecognised version schemes refuse; synthetic fixtures keep mount. The only reader of the family is
  `Chunk7CombatPersistenceEvidence.ps1` (r16's cold consumer runs through the same function).
- Re-evaluation (read-only, run directory hashes and write times unchanged): old reader `fa819113` FAIL (same message), new reader
  `95a69cfc` PASS (all scenario evidence). Receipt `analysis-cache/chunk7-continuation/final2-B4-r15-reader-reevaluation.json`
  (SHA-256 `6b4cf9e9…`; result `ee59a838…`, evidence `9bcb9cca…`). The runner FAIL stays.
- Deriver (`2548e293`, `7b5b68a7`): Final mode admits a runner-FAIL predecessor only with a run-map `reevaluation` reference to that
  receipt, checks it against the run, and proves it itself (corrected commit in its clean history, descending from the run's commit
  with no product change, and its own reader PASSing the evidence in a child process). Proof: r16 derives from `{r01, r15+reevaluation}`;
  without the reference "is not PASS"; with a wrong receipt hash refused (`final2-r16-derive-reevaluation-proof.log`). Reproduce 73/0.
- Review: no must-fix on the reader; deriver must-fix (it trusted the receipt's verdicts) fixed by the independent proof; should-fix
  (game-result hash, all restoration flags, one revision helper with explicit refusal) fixed.
- Tests: combat persistence observation 190/0 (16 new label checks), combat persistence reader 55/0, owned reader 1272/0, deriver
  Reproduce 73/0, Fast 3/0. The deriver change after the review was not re-reviewed.

**Final 7E freeze attempt 1 stopped at B1 c4; harness fixed for the refreeze (offline; harness only, `src/` untouched; no launch).**
F1 (`651ae757`, package `cf2a44e7…`): FULL FAIL (retained), B1 c1–c3 PASS, c4 game PASS 7/0 but outer FAIL on the profile guard (a NEW
zero-length native achievement-cache leaf never reached 12288; lock held, Params/PlayerPrefs startup drift unrestored;
`analysis-cache/chunk7-continuation/final-B1-c4-incident.json`). Attempt 1 is superseded, never relabelled (plan §0 preface).
- Existing rule (batch45/contract64 characterisation, `PersistenceProfileProtection.ps1`): the achievement-cache family is admitted only as a
  before/after transition — an existing leaf rewritten at its exact length, a new leaf at the settled size 12288 — and a new zero-length
  leaf only gets a read-only settlement wait (`Wait-KmcPersistenceProfileUnchanged`), then refuses. `Get-KmcPersistenceProfileCacheDelta` is
  the one function behind the runner guard (Restore, Wait) and `Get-KmcPersistenceProfileRecoveryDelta`.
- Narrow extension (`-EmptyNewLeaf Refuse|Admit|Defer`, default Refuse): at most one NEW leaf of the same family, exactly 0 bytes with the
  empty SHA-256, written later than the newest intake leaf and untouched for 60 s, is admitted as `created-empty`, left in place, never
  restored or whitelisted for content. Admit: the runner's Wait once its window expires, and the recovery tool before and after its
  mutation (recorded in `profile-recovery.json`); Defer: inside Restore only (settings are now restored before the profile verdict).
  Native origin: the owner's own zero-length leaves of the family (2026-09-08, 2026-10-09, 2026-10-11) among ~100.
- Recovery `-WhatIf` is a whole-script dry run (`$dryRun` taken before any read; `Get-KmcSha256` ignores a caller's WhatIf).
- c4 needs no pinned incident script: a read-only check against the exact pins (snapshot `909236f1…`, Params `f1d575f5…`, prefs
  `c1a4a8d0…`) admits the cache delta (two same-length rewrites + one `created-empty`), the prefs delta and the base-game UMM delta, and the
  fixed tool's `-WhatIf` printed PASS with `runtime-state` unchanged (`final-c4-profile-recovery-whatif-fixed.log`). The executor runs
  `Recover-KmcPersistenceProfile.ps1 -RunId 20261025-c7e-final-B1-c4 -SnapshotSha256 909236f1d965565946d954a129eab8ea78307e8936d35c34eda450cc6699ed1f
  -CurrentParamsSha256 f1d575f53ac6e91dfc117ead7bd09c1f684757daab35b4209859ce1d6a6892c5 -CurrentPrefsSha256
  c1a4a8d06f7700ba12506504b75fbef51255f8db54586ce62cb75eaf441c6a2f -Confirm:$false`, profile recovery before transaction recovery.
- FULL-inventory harness fixes: `Test-ChargeFixtureContracts.ps1` trace `forceToEnd` (14/0); combat cleanup adapter template `proneRules`
  and `ReleaseFoldedScrollFixtures` (43/0).
- Independent review: no must-fix; should-fix (post-mutation Admit check and recorded leaf in the recovery tool; honest time-bound
  wording; explicit consumer decision) fixed; a one-leaf cap added from its notes. Residuals: a leaf written in the last seconds before
  Wait starts is refused at expiry (fail-closed); receipts with `created-empty` are refused by strict source readers (plan §0, handled
  under §8); the Wait expiry→Admit path is covered by unit tests of the delta, not by a real aged file.
- Tests: profile protection 85/0 (17 new), profile Areas isolation 39/0, base-game isolation 98/0, charge fixture contracts 14/0, combat
  cleanup adapter 43/0, Validate-Source 149/0, Fast 3/0, Release build DLL `fcacbbe7…` / MVID `6ad861eb…` (identical to F1).

**b209 r57 reader defect fixed (offline; reader only, no product change; no launch).** b209 on `99b84c8c`: r57 passed in game 11/0
(target approach 2.057 m → 1.448 m inside its 1.81 m reach; the stand-up drew exactly one AoO, 1 damage; no skip, the healer
preceded the target). The outer verdict FAILed on a reader defect: the life-stimulus filters counted every target→rider-B damage
event in the whole observer stream, and the folded AoO added a second pair (identity 2116413568, frame 8071, 90 → 91).
- `c3a6a1f1`: the stimulus is bound to its own frame (`beforeDamage.world.frame`; no stimulus rule identity is recorded) in the life
  and death chains. Outside the prone fold, and after a death, any later target damage on rider B still refuses. Inside the fold the
  new `Assert-KmcChunk7ProneAttackDamage` admits it only as the stand-up attack: identity-paired before/after in the attack rule's
  own frame inside the stand-up window (after the stimulus), weapon damage (no ability or area source), at most one per attack,
  chained from rider B's stand-up damage and summing to its recorded damage; with no damage rule the recorded damage is unchanged.
- **Re-evaluation of the retained b209 evidence** (read-only; run directory hashes and timestamps unchanged; evidence
  `ee27c48f…`): old reader `99b84c8c` FAIL (the same error), new reader `c3a6a1f1` PASS. The runner verdict FAIL stays retained;
  receipt `analysis-cache/chunk7-continuation/b209-r57-reader-reevaluation.json`.
- Independent review: no must-fix; should-fix (tie the damage to the attack rule's frame and a weapon source; realistic fixture
  frames; four missing regressions) all fixed. Note: evidence without per-event `frame` now refuses (b207+ producers emit it).
- Tests: owned reader 1272/0 (14 new), native life 85/0, combat 53/0, Fast 3/0.

**b208 r57 sequencing fixed (offline; product diagnostics changed; no launch).** b208 on `3b0e4f6a`: m5 PASS (set 0's native
`maybeWeapon` was the empty hand `20375b5a…`, confirming the stored-item fix; BAB 6 gave off [14, 9], on [12, 12, 7]); r57 refused
as uncovered again before the approach could run: native order rider A, rider B, target, healer, 5dd7, mount B, mount A, so rider
B's round-5 turn came before the target's (target reach 0.6096 m + corpulences 1.2 ≈ 1.81 m against 2.058 m, LOS true, memory true).
- Native seam: when rider B's turn comes after the heal, the target has not acted since the heal and does not engage rider B, that
  one turn is given up with no input through the existing native End Turn (`Game.PauseBind` → `CanEndTurnAndNoActing` →
  `ForceToEnd`: Standard 6, Move 3, Swift 6; `End()` keeps them). In TB `UnitProneController` keeps an idle controllable unit prone,
  so the prone state persists natively (recorded right after the forfeit and again at the stand-up). Rider B's next turn is a new
  TurnController prepared natively. The target approaches on its own turn; rider B stands on its next turn. The reverse order (the
  target acted after the heal) and a target already engaging skip nothing. The stand bound is the skipped round + 1 only (heal + 1
  otherwise); the N+6 bound stays.
- Reader `Assert-KmcChunk7ProneRiderSkip`: exactly one forfeited turn, rider B's own current turn (actor and current actor), untouched
  before (TimeMoved and all cooldowns 0, not acted, Preparing/Acting), the native forfeit after (6/3/6, acted, Ending/Ended, world
  record agrees), no command, no attack, no movement over 0.05 m, conscious prone unpaired, no other pair actor spend, the target's
  turn after it, the stand-up on the next turn. The unapproached target-turn record now carries its trace sequence.
- Independent review: no must-fix; 2 should-fix (identify the forfeited turn's unit; a skip followed by an already-engaged target
  was falsely refused) and 3 small points (producer round bound, untouched-turn cooldowns, missing status tests), all fixed.
- Tests: owned reader 1258/0 (23 new), owned authority 756/0, native life 85/0, combat 53/0, components 881/0, Validate-Source
  149/0, deriver Reproduce 73/0, Fast 3/0, Release build (`fcacbbe7…`). Needs the next native pilot (r57).

**b207 development pilot findings fixed (offline; product diagnostics changed; no launch).** b207 ran 7 launches on `e096038a`
(package `b76b1ae1…`, suite `chunk7e-b207-pilots-20261025`): m1–m4 and cr PASS, m5 and r57 refused as uncovered.
- **m5 false refusal (hand sets).** Kingmaker 2.1.7b contract (local metadata, summarized): `WeaponSlot.MaybeWeapon` answers
  `Owner.Body.EmptyHandWeapon` for a slot that is not `Active`; `HandSlot.UpdateActive` sets `Active = !Disabled && set ==
  CurrentHandsEquipmentSet`; `ItemSlot.MaybeItem` is the stored item regardless of `Active`; `BlueprintItemWeapon.IsRanged` is the weapon
  type's `AttackType` Ranged or RangedTouch; the public `UnitBody.CurrentHandEquipmentSetIndex` setter updates both sets' `Active`
  synchronously. The check read the inactive ranged set 0 through `MaybeWeapon` (the empty hand). Fixed: the pure predicate
  `Chunk7OwnedPairsSelection.Mounted6HandSetsAdmitted` over the stored items; `savedSets` records every clause (count, current,
  polymorphed, empty-hand weapon, per set hasItem/active/disabled/item/weapon/weaponRanged/maybeWeapon), also in the refusal text; the
  reader recomputes them (native current-set rule, inactive slots answer the empty hand). Regressions: C# predicate (b207 shape
  admitted, empty-hand answer, a polymorphed rider and 8 mutations refused), 11 reader refusals and a source guard.
- **r57 prone stand-up geometry.** Contract: `UnitCombatEngagementController.TickUnit` engages a conscious enemy from the attacker's
  group memory when `UnitEngagementExtension.IsEngage` holds (CanAct, threat hand, `DistanceTo` < both corpulences + weapon
  `AttackRange.Meters`, LOS, not total concealment); the stand-up provokes through `HandleUnitWillGetUp` → each `EngagedBy` unit's
  `AttackOfOpportunity`; TB movement is costed by `TurnController.TickMovement`. b207 recorded the target engaging nobody (rider B
  2.06 m, mount A 2.75 m); b207 cr shows the same target engaging mount A at 2.00 m but not 2.75/3.50 m. Design chosen: on the
  brain-leased target's own native turn between the heal and rider B's turn (b207 order puts it there), a target not yet engaging
  rider B makes one native `UnitMoveTo` toward it (the TB hostile-command precedent), to a navmesh destination predicted inside reach
  that keeps every current engager's native reach (band: 5 cm clear of rider B, 15 cm inside the entity reach `ItemEntityWeapon.AttackRange`, which includes ReachRange); natively costed (TimeMoved and Move or five-foot meters); no attack and no pair
  spend (FAIL otherwise); rider B never moves. Uncovered before input: no group memory of rider B, no threat hand, cannot act, a
  pending command, no destination band, or already in reach and LOS without engaging; uncovered when the move does not complete (`Success`) or a completed move still does not engage. The target's turn ends natively once it idles (`TurnController.ContinueActing`), so the window is read at the first frame its move has finished, from its own TurnController object. The
  target records now carry `targetRemembersSubject`, `threatReachMeters`, `subjectCorpulence`, `subjectMechanicsDistance`,
  `subjectLineOfSight`. The incapacitation half is unchanged. Reader `Assert-KmcChunk7ProneTargetTurn` (22 new checks).
  **Residual:** b207 did not record which engagement term failed; if the target's group memory lacks rider B, the case still refuses
  as uncovered (now diagnosably) and needs a new decision.
- Plan B17: m2 literal = b207 m2's selection (five native field ids in r01 order plus the Horse); m1 = b207 m1; m3–m5 are deriver tags.
- Deriver tags `m3`–`m5` (b207 templates; m5 selection shape only): Reproduce 73/0 (`final-derive-reproduce-b207-m3m5.log`).
- **Independent review of the working diff: FINDINGS (1 must-fix, 6 should-fix); all fixed before commit.** M1: the approach required the
  target's turn to survive 10 settle frames, but a not-directly-controllable unit's turn ends natively ~0.2 s after it idles; now read at
  the first settled frame and the turn may end natively (recorded `turnEndedNatively`). S1: entity reach instead of blueprint reach. S2:
  the reader recomputes the geometry and the "only distance can change" precondition and types the record. S3: the approach must lie
  between the restoring heal's and the stand-up's trace sequences. S4: destination band margins (no overlap, middle of the band) and a C#
  rider-B position check. S5: a polymorphed rider refuses as uncovered in the predicate. S6: plan wording (four field ids plus the Horse).
  Also fixed while testing: the reader now accepts PowerShell 5.1's decimal JSON numbers (the fixture round-trips through JSON).
  Residuals (notes): the saved-sets and target-approach shapes have never been observed natively; an AoO from a party unit engaged
  mid-path FAILs rather than refusing; the target's own Standard/Swift are recorded, not asserted; the source guard is text-based.
- Tests: owned reader 1235/0 (34 new), owned authority 756/0 (9 new), native life 85/0, combat 53/0, components 881/0,
  Validate-Source 149/0, deriver Reproduce 73/0, Fast 3/0, Release build (`ac8792f5…`). No native run: both fixes need the next pilot.

**Owner decision of October 24 ("I'll make a mod seed"): `KMC_C7_MOUNTED6_SEED` registered and its lineage built (offline; product diagnostics changed; no launch).**
- Seed: master `2ef426c4…` (1,204,811 bytes, byte-identical to the owner's `Manual_308_KMC_C7_MOUNTED6_SEED.zks`, only hashed), intake `f4e8afd3…` read-only beside it, registry entry added. The real registry/master/intake/header gate passes offline (charge and level-6 seeds too; the mounted6 id under another hash is refused).
- Census (party.json read in memory): main Ranger `73681ef2` level 6 (XP 35,022, level 7 pending), BAB 6, Rapid Shot feat and toggle (on), active hand set 1 (longsword `571c56d1…`), set 0 the ranged `4de4658c…`; memorized available level-1 slots Cure Light Wounds `5590652e…` and `3e9d1119…`; one pet `6eb039a1…`, blueprint `4016c7db…` (the KMC Horse), level 6, reciprocal; KMC Horse feature (selected at Ranger 4 from the native Ranger companion selection) and companion rank 6 (the level-6 seed had rank 3; the level-5 feat `8fc01f06…` most plausibly Boon Companion, not resolved by name). Linzi 7 knows Haste. Rider B `fd213210` is level 4 with XP 35,022 (ready 7), Ranger 0. `kmc-mounted-state` is an empty schema-3 archive (no pairs, participations, slots or combat).
- **Suspicious, reported, not admitted as earned:** play time grew only about 2 min 43 s from the charge seed while every party character went from 9,000 to 35,022 XP with no combat recorded, so the XP was almost certainly granted by a foreign tool (the coordinator reports CheatMenu 1.2.3 was in the shared Mods folder). The level-ups are native level-plan actions; money is unchanged (400,057); no impossible stat, foreign type or unknown buff source found (the all-18 base attributes predate this seed; buff `83f1f237…` on all five field units looks like a native camp meal buff). The owner confirmed on 2026-10-25 (AskUserQuestion answer "Yes, CheatMenu XP is fine"): the XP was added with CheatMenu and the levels were taken normally in game; the seed is accepted for the final run with that provenance recorded.
- Admission: the only owner seed admitted with KMC metadata (exact id and hash; the empty schema-3 archive of its own campaign and area) in the cold inventory, the owner-seed setup and both readers; every other seed keeps the Missing path. It reaches only its lineage: Manual producer (`mountAId` pins the pre-owned Horse, verified and never built; rider B built after the exact minimal XP to level 8, its pending levels admitted on this seed only), its `multi-exploration-manual` cold consumer, the new `owner-seed-prepared-casting` (unchanged realtime-casting engine; rider A the one caster of its one prepared Cure Light Wounds slot) and `owner-seed-attack-modes` (BAB ≥ 6; the saved ranged set selected out of combat through the public native setter; an iterative Rapid Shot off full attack required). Every gap refuses as uncovered before mutation. Pair B is required: the Chunk 7 combat engines are strictly two-pair (no single-pair support exists).
- b206 fixes (separate commits): the row-57/69 fold subscenarios registered (with a Validate-Source guard over every `Pass("C7-…")`), the prone-fold healer's scroll commands declared to the non-pair AI lease (at most two sequential declarations of the same member), and the combat cleanup failure record written as an explicit JObject (guarded).
- Save compatibility: `704904fe..HEAD` changes only `Diagnostics` and the project file (no Integration, Domain or codec file), so the b205-made seed's data loads under HEAD's product unchanged.
- Tests: owned reader 1200/0, owned authority 747/0, assisted cold composed 232/0 (scenario 101, reader 131; 13 new mounted6 metadata checks), combat 53/0, native life 85/0, RT 69/0, owned source 323/0, components 881/0, Validate-Source 149/0, deriver Reproduce 70/0, Fast 3/0 (0.48 min). Release build. Plan: 82 launches in 17 batches (B17 m1–m5); matrix rows 77–81.
- **Independent adversarial review of `acd16f41..3860a54f`: FINDINGS (1 must-fix, 4 should-fix); fixed in one follow-up commit.** M1: the producer numbered the single rider-B autosave observation 2 while the reader requires 1 (the fixture had hidden it); C# now numbers by observed builds. S1: attack modes check the Rapid Shot feat and toggle before the mounted6 hand-set switch. S2: the encounter waits until the switch has settled (no scheduled hands update, no hand animation). S3: rider A's `after` progression is re-observed at SETUP-COMPLETE, so the unchanged check compares real observations. S4 not fixed (residual): the cold-scenario template re-implements the metadata admission over its stubs; the real `OwnerMadeEmptyArchive`/`MissingMetadata` depend on game types and are covered only by the native m1 run. Note acted on: the prepared-casting reader refuses the healing spell on another rider. Residuals: the row guard accepts a C7-combat row in any of the three lists; `Test-Chunk7OwnedPairsSource.ps1` has no mounted6 derived-source case; rider A's pending level 7 is untested natively. After the fixes: owned reader 1201/0, RT 69/0, combat 53/0, Fast 3/0 (components 881/0, Validate-Source 149/0).

Owner decisions of October 24, 2026 (the owner answered a direct question; recorded verbatim in the
[7E matrix exclusions](planning/CHUNK7-7E-QUALIFICATION-MATRIX.md) and the [release notes](docs/CHUNK7-RELEASE-NOTES.md)):
- (a) Haste attack variation (C03): "Approve exclusion".
- (b) Main-menu CharGen, the stale level-up Auto families, the legacy Manual 302→303 route, and six-pair
  combat (six pairs qualified for performance and save/load only): "Approve all four" as exclusions.
- (c) The five Chunk 6A historical replay failures (LedgerProtocol, RegressionLedger, LegacyLedger,
  AreaLedger, UnrelatedFullLedger): "Report as classified". They stay FAIL with unresolved cause, separate
  from Chunk 7, no re-qualification; the current behaviour checks must pass.
- (d) Prepared-spell casting and a BAB ≥ 6 full attack: the owner chose to supply a new no-mods seed (Ranger
  level 6, a memorized rested level-1 Ranger spell). That seed (`KMC_C7_LEVEL6_SEED`) could not form a rider A pair,
  so the owner was asked again and answered "I'll make a mod seed": `KMC_C7_MOUNTED6_SEED`, made with the b205
  candidate installed, registered October 24 (rows 77–81, plan B17).

**Independent adversarial review of `2c70df0c..bad38f0c`: FINDINGS (1 must-fix, 7 should-fix); fixed in one follow-up commit.**
- M1 (must-fix): the prone fold keeps the allocation trace recording to N+6, so the row-57 reader's unbounded
  count of the still-unconscious control's prepares would fail. The producer now records
  `traceSequenceAtGrantAfter` with the control's grant count; the reader bounds the count by it and requires it
  for the owner-seed incapacitation case. Regression: the synthetic fold adds an N+2 control prepare.
- S1: the healer prefers companions that can never be the control (essential, immortal or not dying-allowed);
  if the healer is the only eligible control, the case refuses as uncovered (with the control record).
- S2: the healer must be a conscious acting combatant at the fold, or the case refuses as uncovered.
  Not addressed: the healer's reach to rider B is not pre-checked (an interrupted approach is a FAIL).
- S3: healer candidate rows record `commandsEmpty`, `inCombat` and `controlStaticallyIneligible`; the reader
  recomputes eligibility exactly and checks the order.
- S4: the defensive cast now requires rider A itself engaged (the native window keys on the caster); the
  approach targets that and refuses as uncovered otherwise; the reader requires `riderEngaged`.
- S5: exactly one native outcome is required: one scroll cast with rider A's Standard charged, or a cast lost to
  a failed concentration check with no cast (both producer and reader).
- S6: run-map receipt fields must be typed (`readerVerdict` string, `restorationVerified` bool); plan §7 now
  re-derives row 32's link to r31 in the ledger. Residuals noted: the receipt is executor-written and the suite
  snapshot SHA is not compared.
- S7: stale X3 blocker text fixed; plan G5 lists `KMC_C7_LEVEL6_SEED`.
- Unverified IL risks noted by the reviewer, for a pre-freeze check: whether the sticky-touch child command can
  open its own defensive window, and whether touch delivery triggers `RuleAttackWithWeapon`.
Tests: owned reader 1157/0, native life 85/0, removal evidence 64/0, components 877/0, Fast.

**Plan totals (October 24, offline):** 77 native launches in 16 batches (B1–B15 five each; B16: x3 row 75 and
l6 row 76), about 12 h with the offline tiers; the folds keep their rows' launches (r57, cr) and lengthen them
by an estimated 2–4 min each. Deriver reproduce 70/0 (tag `x3` added). Gates: G3, G6, G7 met; G1 (docs and
X1–X6) still to be confirmed at freeze, G2 (F11 arithmetic) open, G4/G5 checked at freeze. The folded halves of
rows 57 and 69 and rows 75–76 have no development run; a first refusal there goes to the owner.

**C11 prone stand-up folded into row 57 (offline; product diagnostics changed; no launch).**
`owner-seed-native-life-cleanup` keeps the incapacitation measurement unchanged; after the surviving mount's
N+1 turn, from round N+2: a party actor outside both pairs that uses the scroll without Use Magic Device
(`BlueprintItemEquipmentUsable.IsUnitNeedUMDForUse`, chosen out of combat and given one disposable four-unit
Cure Light Wounds stack on its own quick slot; never the unconscious control, whose candidates now carry a
`healer` flag) heals rider B by one native scroll click, at most two attempts on its own turns; on rider B's
own turn one ordinary native ground order is its action attempt, the native stand-up spends its Move and the
threatening target must make exactly one native AoO (1 → 0). Producer and the new reader
`Assert-KmcChunk7ProneStandUp` also require one in-place spend per attempt, a native heal on the restoring
attempt, no other attack, no refund, pair A and mount B never spending, rider B unpaired with no pair command
authority, the stack released; a hit that incapacitates rider B again is admitted only with its damage. A
missing healer, a rider not natively prone or a target not threatening it refuses as uncovered before the
input that needs it (an owner decision). Owned reader 1150/0 (28 new), native life 85/0, components 877/0.
The matrix C11 prone exclusion now says covered by row 57.

**F04 defensive casting folded into row 69 (offline; product diagnostics changed; no launch).**
`owner-seed-combat-reaction` (contract v2) keeps the round-N disengagement unchanged, then on rider A's next
turn (N+1) brings pair A back inside the target's threatened area if needed (one ordinary native approach that
must draw no attack) and clicks one disposable native Cure Light Wounds scroll (four units on rider A's own
equipped quick slot, `NativeCastingItemLease`, prepared out of combat) on mount A. The producer and the new
reader `Assert-KmcChunk7ReactionDefensiveCast` require exactly one native casting-defensively hook and rule on
rider A with its DC, roll and outcome; exactly one in-place spend of the exact stack; no attack after a passed
check and at most one native AoO from the target after a failed one (count matching); concentration only with
damage; mount A and pair B never spending; rider A never refunded; the stack released. A target without its
N+1 AoO refuses as uncovered; a cast that acts before the window FAILs (no check). Shared helper
`Chunk7CombatScenario.ScrollFixture.cs` (also used by row 57). Owned reader 1122/0 (25 new checks, success and
both failure shapes plus 22 refusals). The matrix F04 exclusion now says covered by row 69.

**Owner decision (d) seed registered: `KMC_C7_LEVEL6_SEED` (no launch).** Master `b1447ae7…` (1,217,335 bytes,
byte-identical to the owner's `Manual_307_KMC_C7_LEVEL6_SEED.zks`, which was only hashed), intake
`c3e0fc6f…` recorded read-only beside the master, registry entry added; the real registry/master/intake/header
gate passes offline (and still for the charge seed; another hash is refused). Offline census (party.json read
in memory): main Ranger `73681ef2` level 6, BAB 6, Rapid Shot feature and toggle (on), active hand set 1 (the
melee longsword `571c56d1…`), two memorized available level-1 slots (Cure Light Wounds `5590652e…` and
`3e9d1119…`); a native animal companion `7347ab0c…` (blueprint `8a6986e1…`, owner: Smilodon), stored as the
rider's `m_Pet.m_UniqueId` and the pet's `Master.m_UniqueId` (not `m_Master`); Linzi `8265f8cf` level 7 with
Haste known and two level-3 slots. **Consequence:** the owner-seed Horse setup refuses rider A before any
mutation (`Chunk7OwnerSeedSetupPlan`: a petless rider reaching exactly Ranger 4; one pet per master), so no
mounted rider A exists on this seed; every owned two-pair case now refuses the seed before input with that
reason (C# selection and PS reader). Prepared casting and the BAB ≥ 6 mounted full attack therefore stay
uncovered and need a further owner decision; haste stays excluded (decision (a)). Product check: KMC leases
Mount Companion only to a character whose pet is a supported Horse/Mammoth (`ShouldLease`), so a Ranger with
another companion keeps it and is offered nothing to mount — no mishandling found offline. Row 76 (B16) is the
seed's read-only cold census. Tests: owned authority 723/0 (13 new), owned reader 1097/0 (5 new).

**X3 residual closed offline (row 75; no launch).** `pending-schema3-control-manual` is reinstated as the
separate-process cold consumer of row 17's own genuine pending-only Manual (`owner-seed-checkpoint-load`, one
load, zero writes), read by the existing control branches of the owned and combat-persistence readers. Its
October 23 prelaunch refusal is removed; the gate itself stays (`Test-Chunk7RetiredPersistenceCases.ps1` 4/0:
every runner case passes it and the actual runner admits the control to its intake checks). Deriver tag `x3`
(template b37s1, outer-FAIL, selection shape only) reproduces 1/0. Plan batch B16. The matrix exclusion row
now says covered by row 75; the release notes drop the X3 gap.

**G7 met (option (a), harness plus request validators; no launch).** Row 32 now consumes F's own r31:
`Get-KmcOwnerStockCleanupSource` admits a final-campaign `…-c7e-final-B<n>-r31[r]` run only through the
runner's new `-OwnerStockCleanupRunMapPath` (settled `r31` receipt: run id, reader PASS, restoration
verified, result SHA-256 re-hashed), on the consuming run's own commit, DLL, MVID and suite, on the stock
Mammoth input, with its cleanup write's SHA-256; batch65 stays admitted exactly as before, for history.
The request validator, `RuntimeProtocol` and the observer request accept any genuine cleanup hash other
than the stock input with a bounded length (batch65's hash keeps its exact length). Tests: removal
evidence 64/0 (2 positive, 23 refusals), observer request 7/0, components 877/0, Fast 3/0.

The matrix exclusions table no longer says "agreed": every row cites its owner decision or mission text,
and the uncovered rows (C11 prone, F04 defensive check, X3 residual) say they are not approved exclusions.

Claude (offline, no launch, October 24, pre-freeze gates G3/G6 of the [final run plan](planning/CHUNK7-7E-FINAL-RUN-PLAN.md)):
- **G3 met.** `Test-ScenarioDispatchEvidence.ps1` lists the four Chunk 7 fixture/cold families registered by `ee2bdfc9`, `38318d57` and `fbb1116a`, each with its one dedicated reader: PASS 181/0. Harness-only; [X6](docs/CHUNK7-7E-X6-OFFLINE-INVENTORY-2026-10-23.md) now says fixed.
- **G6: selection deriver `tools/chunk7-7e/final-derive.ps1`.** It changes only declared lineage pins and refuses anything else. Reproduce: all 69 selection tags are byte-identical to their template selections (69/0). Cross-lineage re-pins onto b194–b198 runs reproduce those batches' retained selections (13/0). Negative checks refuse non-final run ids, a foreign candidate and a missing predecessor.
- **Rows 33/34 → 48, 50, 51: one pair suffices.** The progression source admission is read-only and records no consumption; the pilots used separate pairs only because they were separate candidates. Offline, one b108 pair was admitted for all three cases (3/3). No batch or launch change.
- **New blocker G7 (row 32).** The runner admits only the batch65 cleanup Manual as the no-DLL source, so row 32 cannot consume F's r31. It needs either a reviewed harness change or an owner-accepted fixture-input exception.
- The owner's FULL clarification is in plan §4: no successful child runs twice, the FULL FAIL stays FAIL, and Chunk 6 results are reported in four separate groups.
- Fast 3/0.
- **Review.** One independent adversarial review covered `2fb2a7cf`, G3 and the deriver: FINDINGS, no must-fix. Three should-fixes are fixed:
  - Row 31's runner parameters are stated.
  - §4 now reuses the FULL prefix transcript and runs only the remaining steps; option (b) is gone, because it would have rerun passed children.
  - Final mode accepts a predecessor only with a settled receipt in `final-runs.json`: the external reader verdict PASS, restoration verified, and a matching result SHA-256. The runner's own status no longer counts.

  Notes acted on: cross-lineage now covers r02, r34, r36, r44, r45 and r54 (21/0); the r41 template is marked outer-FAIL; matrix row 32 points to G7; the closeout records that its audit is dependency-scoped. Reproduce is still 69/0, and the shared-pair proof 3/3.
- **Gates.** G3 met; G6 met. G7 is open and blocks the freeze, as do G1, G2, G4 and G5, which are still to be confirmed at freeze time. No freeze or launch.

Claude (offline, no launch, October 24): the [7E matrix](planning/CHUNK7-7E-QUALIFICATION-MATRIX.md) now lists every agreed case as a numbered row (1–74; new rows 60–74 for the acquired chain c1/c4/c5, X4 x4s/x4c, two-pair native life, Small items, death, attack modes, C11, F04 staged and the RT Charge k1–k4 lineage; rows 42 and 55–56 name their binders), and the uncovered halves are in its exclusions. The executable [final run plan](planning/CHUNK7-7E-FINAL-RUN-PLAN.md) orders 75 launches in 15 batches (about 12 h with the offline tiers); one independent review found a blocker (progression rows need same-candidate rows 33–36 first) and five should-fixes, all fixed. `Test-ScenarioDispatchEvidence.ps1` was reproduced in a temporary worktree: PASS at the preview.211 tag (82/0) and at `ee2bdfc9~1` (165/0), FAIL from `ee2bdfc9` on, classified as a stale family list in the [X6 inventory](docs/CHUNK7-7E-X6-OFFLINE-INVENTORY-2026-10-23.md). The changelog's progression [VERIFY] is resolved (retirement uses the lifecycle cleanup made per-pair in `58abe1a9`; dismissal has `7c6ba455`; no other product commit is progression-specific), and the release notes carry the final uncovered set. Next: the pre-freeze gates G1–G5, then the plan.

Claude [batch205](docs/CHUNK7-BATCH205-7E-COMBAT-PILOTS-2026-10-24.md) froze pilot candidate `704904fe` (package `6dac3bf9…`, suite `chunk7e-b205-pilots-20261024` `60224882…`; Candidate tier 6/0). The DLL `feca06e9…` and MVID `ac3d08b6-…` are byte-identical to b204's. A new package was needed only because the manifest must bind the harness HEAD that holds the corrected reader. Five native launches, no startup retry; every run restored, and the profile check admitted 0 changes on each. These are development pilots, not 7E qualification. **All five combat cases PASSed in game and in the external reader (33/0, 29/0):**
- **Staged** 8/0, the first run read by the corrected reader. Candidate 11 of 12 (8 m at 135°) was selected again, and all ten steps executed with no `planAbandoned`: beyond range 20.30 m, exhaustion 21.70 m, approach 21.70 → 16.87 m. Both scroll windows hold the admitted native shape: one sourced scroll cast, the spends, one sourceless touch delivery and a 0 heal. At the in-range shot the mount did not move, and the 0.467 m seat rotated (the rider moved 0.63 m).
- **Death proper** 9/0: all eight detach conditions true, `survivorSlot` 4.
- **Attack modes** 8/0: off a single attack (AB 12), on a full attack (10/10) mounted and unmounted, and the admitted native `OnTick` interrupt at 2 of 2.
- **C11** 6/0: paired Delay, exactly one native AoO, reactions 1 → 0. The prone half stays uncovered.
- **RT Charge** (k4, b201 k1–k3 lineage) 7/0: 67.5° 8 m placement, Standard 5.96, longsword unchanged.

Standing mission remains IN PROGRESS. Next: the remaining 7E work (F10 breadth, doors, the F11 protocol, then private 7E qualification).


Offline (batch204 diagnosis; reader-only, `fa68c97d` and review fixes `bc3a2b27`; no launch; **product DLL unchanged** `feca06e9…`): both b204 staged reader failures are native shapes the reader misjudged. No product defect was found.
- **Step 1 cast (and step 8).** The second rider-A `cast-after` is native. The scroll ability `5590652e…` (CureLightWoundsCast) carries `AbilityEffectStickyTouch`, whose `TouchDeliveryAbility` is Cure Light Wounds `47808d23…`. The relation was recorded natively in b136, where the F09 producer read it from the installed blueprint. The pinned `Apply` triggers `RuleCastSpell` on the touch ability's data at once when the caster is the target: no child command and no spend. Otherwise it queues a touch-delivery command. Rider A's native damage was 0, so the heal is 0. KMC only clicks the native ability (`IssueStagedCast`). Every preview.211 6D staged scroll window holds the same pair. The 6D reader filtered `cast-after` by the scroll blueprint, so it never counted the delivery. The reader now requires:
  - exactly one sourced scroll cast from the equipped stack (its `cast-before` matched);
  - one spell spend and one item spend, after the cast;
  - exactly one sourceless cast of the pinned touch ability, by and on rider A, after both spends;
  - exactly one heal after it, by and on rider A, no larger than rider A's native damage.

  Any other cast, spend, heal or child command refuses, as does a rider cost on another command.
- **Step 4, the in-range shot.** With the cast fixed, the re-evaluation stopped at "the in-range shot origin moved" (0.63 m). `MountedPairAttackCommand.StartChildAttack` calls `mount.ForceLookAt(target)`, so the stationary mount turned about 84° to face the target. The rider's saddle point (0.467 m from the pivot, same height) rotated with it: the saddle bearing after the turn is −93.0°, and the target's is −93.0°. The reader now requires the same seat radius (≤ 0.75 m) and height about the unmoved mount. A displaced seat must also face the target within 5°.
- **Reader on the immutable b204 st evidence: FAIL (step 1) → PASS.** The legs, both shots, the approach, rounds and cleanup now evaluate and pass. The old FAIL and the original outer FAIL stay recorded. Receipt: `analysis-cache/chunk7-continuation/b204-st-reader-reevaluation.json` (`7c62bddd…`).
- **This is not matrix credit.** Staged needs a fresh native run read by the corrected reader. The DLL did not change, so that run may reuse the b204 product bytes if the suite identity admits the new reader. Otherwise it needs a new suite on the same package.

Checks: owned reader 1092/0, including the exact b204 cast windows (`tests/Chunk7Diagnostics/StagedB204NativeShapes.json`) with 27 mutations and seat-rotation positives and negatives. Also: combat 53/0, staged geometry 185/0 plus reader 22/0, 6D staged 96/0, Fast tier 4/0 (Release build up to date, components 877/0, Validate-Source). PowerShell parser clean; BOM-free LF. One independent adversarial review returned FINDINGS (no must-fix). Fixed in `bc3a2b27`:
- a missing delivery is now refused (it had been admitted as "at most one");
- the seat's bearing is checked;
- the cast-before events are matched.

Next: a native staged run on the corrected reader.
Claude [batch204](docs/CHUNK7-BATCH204-7E-STAGED-PILOT-2026-10-24.md) froze pilot candidate `36945a81` (DLL `feca06e9…`, MVID `ac3d08b6-…`, package `7715bd6d…`, suite `chunk7e-b204-pilots-20261024` `513e07cd…`; Candidate tier 6/0). It ran one native launch of `owner-seed-staged`, with the b203 selection (`d764eb44…`) and no startup retry. The run restored, and the profile check admitted 0 changes. This is a development pilot, not 7E qualification.
- **The whole-plan geometry selected a placement on Spider Cave.** The plan recorded 12 candidates and selected candidate 11 (8 m at 135°). The four `plan-infeasible` candidates (45° to 112.5°) each first refused at step 5 `move-beyond-range` (`no-destination`). The others were `not-walkable` (4), `placement-rise` (2) and `placement-offset` (1). The confirmation matched before input, and the external plan replay passed.
- **Every native leg executed (game PASS 8/0), with no `planAbandoned`.** The steps were move-cast-move, the double move and the in-range RiderRanged shot, then beyond range (ended 20.30 m out), exhaustion (21.71 m), the probe (0 m), the second cast, and the approach shot (21.71 → 16.89 m, RiderRanged Success).
- **The external reader FAILed at step 1:** "Staged step 1 cast-standard-scroll: the native cast failed or duplicated." Rider A's cast window holds two `cast-after` events, neither failed: the scroll ability `5590652e…` (stack 4 → 3) and, after the spend, a sourceless `47808d23…` followed by a 0 heal. Step 8 has the same shape, and b203's step 1 already held it. This is the first staged run to reach the reader's cast check.

Diagnosed offline above (reader-only; old verdict kept). Standing mission remains IN PROGRESS.
Offline (product diagnostics changed; no launch): `owner-seed-staged` now **chooses its geometry for the whole plan up front** (`74fdc951`, review fixes `f8380e9c`). The three Spider Cave failures (b201/b202 exhaustion band unreachable from a 19.24 m edge point; b203 beyond-range leg) came from a leg-by-leg greedy search walking into navmesh edges. The bands, envelope, budgets and the 8-leg limit are unchanged.
- **Selection, before any lease, target placement or input.** Target placements are tried around mount A's start in a fixed, recorded order: 8 m along +Z first (the old point), then the full fan, then 9, 7 and 10 m. Each must be walkable within 0.3 m, at the mount's level and inside the spawn range. A placement is admitted only when the whole declared leg sequence simulates eligible from the start, using native walkability and exact straight traces: move-cast-move, the double move to the second Move and the in-range shot, beyond range, exhaustion to 5.95 s within the 6 s budget (no sub-metre tail), the probe, and the RiderRanged approach. Every banded goal must also keep its band 0.5 m back along the leg (b202 stopped 0.27 m short). Every leg searches the full circle, forward fan first.
- **Record and refusal.** Every candidate, every proposal and each candidate's first refusing leg and term are recorded. If no placement holds, the case refuses before input as uncovered on this fixture.
- **In-combat confirmation.** Before input, the spawned target's envelope and point and the mount's start must match the plan, and the plan is simulated again with the native speed. Legs still use the native result and existing checks; the remainder of the plan is the last term of each proposal, and a fallback to the declared geometry alone is recorded as `planAbandoned`.
- **Reader.** `Assert-KmcChunk7StagedGeometryPlan` replays every candidate from the recorded points and requires the first feasible one. It binds that candidate to the target authorization (and the rider start), checks the envelope, and replays the confirmation against step 0's native start and speed.
- **Recorded b203 points.** Offline, the planner replays candidate 0 through the five native legs b203 completed and refuses it at the round-3 beyond-range leg, before input. With only recorded points, all 64 candidates refuse explicitly. A 12 m disc floor refuses the old placement and selects a 9 m one. Correction: b203's recorded beyond-range terms are **16 not walkable, 4 `below-band`, 1 `trace-inexact`** (the b203 summary's 15/5 was a miscount).

Checks: Release build; `Test-Chunk7StagedGeometry.ps1` 185/0 plus reader 22/0 (the committed reader fixture must equal the regenerated plan); owned reader 1058/0; combat 53/0; RT 69/0; owned source 323/0; Fast tier 3/0; PowerShell parser clean. One independent adversarial review returned FINDINGS (no must-fix): the exhaustion-tail and start-binding should-fixes are fixed in `f8380e9c`. Next: a native staged pilot on a new frozen candidate. Its risks: Spider Cave may admit no placement (an honest uncovered refusal before input); selection uses the out-of-combat mount speed; units are not in the navmesh model; a multi-second single-frame search is possible on bad terrain.

Claude [batch203](docs/CHUNK7-BATCH203-7E-COMBAT-PILOTS-2026-10-24.md) froze pilot candidate `2ffacbda` (DLL `4fd2ebba…`, MVID `b47492d7-…`, package `f3063c24…`, suite `chunk7e-b203-pilots-20261024` `8de0597d…`; Candidate tier 6/0). It re-piloted all five combat cases in five native launches (Batch B 4, Batch C 1), with no startup retry. Every run restored, and the profile check admitted 0 changes on each. These are development pilots, not 7E qualification. RT Charge reused the b201 k1–k3 lineage (k4 WhatIf passed). **Four cases passed in game and in the external reader (33/0, 29/0):**
- **Death proper** 9/0. All eight `deathDetach` conditions were true. `nativeOrderAfterDeath` equals the damage-time order minus the dead rider, and the control's `survivorSlot` 4 matches it.
- **Attack modes** 8/0. Off was a single attack (AB 12). Both on legs were full attacks (AB 10/10, Rapid Shot −2). The unmounted-on `Interrupt` (2 of 2) was recorded one frame after the last shot, over an acted Success. Its caller was native `UnitAttack.OnTick`, the chain was untruncated and held no KMC frame, and the reader admitted it.
- **C11** 6/0. The paired Delay was used and exactly one native AoO was observed. The prone half stays uncovered.
- **RT Charge** 7/0. The 67.5° 8 m placement was selected. Both shells spent Standard 0 → 5.96 (6 − 0.04) with Move and Swift unchanged. The longsword was unchanged.

**Staged FAILed** (6/1) one leg earlier than in b202, at the risk recorded for `7b183f8b`. The round-3 `move-beyond-range` leg found no walkable native destination among 21 proposals: 15 not walkable, 5 `below-band`, 1 `trace-inexact`, and none reached the lookahead. It needs diagnosis before another run. Standing mission remains IN PROGRESS.

Offline (batch202 diagnoses; product diagnostics changed; no launch): the four batch202 failures are diagnosed. No product defect was found. Readers were re-run over the immutable b202 results (old → new verdict below). A reader change on recorded evidence is not matrix credit: every case still needs a fresh native run on one new frozen candidate, because diagnostics compiled into the DLL changed.
- **Death proper** (`80ece0c1`) was a producer/reader order mismatch. `DispatchDeathControl` runs after the death delivery. Native `SetLifeState(Dead)` has then run `LeaveCombat` for the rider (after delivery it reads `inCombat=false`), so the control reads `SortedUnits` without the dead rider: survivor 5 → 4, candidates 2 → 1 and 4 → 3. The reader now derives the control order as the recorded damage-time order minus the dead rider. Contract v2 also records `nativeOrderAfterDeath`, which must equal it. Reader on b202 d1: FAIL → **PASS**.
- **Attack modes** (`02fcb8ee`, `7243be75`, `a52d0059`): the cause is **unknown, not admitted**. Native `UnitAttack.OnAction` returns Success with the last attack. `UnitCommand.Interrupt(bool)` overwrites any result while the last attack's animation runs; the unmounted control ended one frame after its second shot. After the Dismount, rider A left the trace's observed actors, so the interrupt's caller was never recorded. Contract v2 watches the measured command's every `Interrupt(bool)` with its prior state and caller chain, from the click or first observation. A plan-complete `Interrupt` is admitted only as one native overwrite of an acted Success after the last shot, with no KMC frame on its whole (untruncated) stack and no queued-command overwrite. This excludes a KMC caller on the stack, not every KMC state influence. Reader on b202 am: FAIL → FAIL ("…without its recorded native cause").
- **Staged** (`7b183f8b`): the band was **unreachable from the placement**. Recomputed from the 48 recorded proposals: 34 were not walkable. All 14 walkable exact-trace proposals lay inward, the farthest 18.19 m from the target, below the band floor (18.84 m). The beyond-range leg had also ended 19.24 m out, under the reader's envelope + 2.5 m (19.34 m), after stopping 0.27 m short. Both bands are unchanged. The beyond-range goal is now accepted only from its aim (envelope + 3 m) up, and only where an exhaustion leg in the band stays eligible (lookahead). Exhaustion legs prefer such points and fall back to the band alone. Each proposal records its refusing term; the lookahead runs only after the node, trace and length terms pass (`a52d0059`). Reader on b202 st: game FAIL, reader consistent (unchanged).
- **RT Charge** (`69ba2615`) was a reader defect. Native RT `UnitActionController.UpdateCooldowns` (0x06009120) sets only `StandardAction = 6 − TimeSinceStart`. The full-round Move add exists only in the TB branch (`UnitEntityData.UpdateCooldowns`, 0x0600838F). b202 recorded 5.96 = 6 − 0.04 with Move unchanged, which is exactly that native shape. KMC's design (the rider's native full-round shell owns the cost; KMC writes nothing) matches. The reader now requires that exact shape. Reader on b202 k4r: FAIL → **PASS**.

Checks: Release build; Fast tier 3/0 (components 877/0, Validate-Source 147/0). Owned reader 1046/0, native life 85/0, combat 53/0, RT reader 69/0, staged geometry 18/0, owned source 323/0, wiring 261/0, cleanup 47/0, combat cleanup adapter 43/0, observer acquisition 13/0. One independent adversarial review accepted death, staged and RT Charge and attack modes with fixes. Its must-fix was the 14-frame caller cap, now an untruncated chain with a truncation flag. Its should-fixes were the queued-overwrite gate and evaluating geometry last. All three are addressed in `a52d0059`; the stated limits stay. Next: one new frozen candidate re-piloting death proper, attack modes, staged and RT Charge (plus C11 if wanted). The staged lookahead can still refuse at the beyond-range step if no walkable band strip exists on that side of the target.

Claude [batch202](docs/CHUNK7-BATCH202-7E-COMBAT-PILOTS-2026-10-24.md) froze pilot candidate `7aaf0dfb` (DLL `b26bbcd6…`, MVID `2e6d6438-…`, package `1d06a143…`, suite `chunk7e-b202-pilots-20261024` `08c55087…`; Candidate tier 6/0). It re-piloted the five changed cases in six native launches (Batch B 4, Batch C 2, one of them the permitted Steamworks `steam_api64` startup retry). Every run restored. These are development pilots, not 7E qualification. RT Charge reused the b201 k1–k3 lineage under the same-candidate rule (k4 WhatIf passed).
- **C11 reactions PASS** (6/0). The target was waiting on initiative, so rider A took the paired Delay. On resume, the disengagement drew exactly one native AoO (target reactions 1 → 0).
- Death proper: game PASS 9/0, and all eight `deathDetach` conditions are true. The reader failed: "Unmounted death control contract differs." The producer's control `survivorSlot` is 4; the recorded next-round order, which still lists the dead rider, puts mount B at 5.
- Attack modes: game PASS 8/0. Off was a single attack (AB 12); both on legs were full attacks (AB 10/10, Rapid Shot −2). The reader failed: "Attack mode interrupt claims a completed plan." The unmounted-on command ended `Interrupt` with 2 of 2 attacks.
- Staged FAILed at the same round-3 exhaustion leg. Of 48 recorded proposals, 14 were walkable with an exact trace and none was geometry-accepted.
- RT Charge: the 67.5° 8 m candidate was selected. The charge executed (game 7/0): Success, one native attack, longsword unchanged. The reader failed: "Native RT expenditure was not applied at its exact callback." Both charge shells spent Standard 0 → 5.96 with move 0 → 0, but the reader expects a full-round move charge.
- k4r's profile check admitted 3 native achievement-cache rewrites under the existing policy. `Params.xml` and PlayerPrefs were equal after every run.

Each of the four failures needs diagnosis before another run. Standing mission remains IN PROGRESS.

Offline (product diagnostics changed; no launch): the five batch201 failures are diagnosed and their cases changed. One independent adversarial review accepted all five; its should-fixes are addressed in `708c876c`. No product defect was found.
- **Death proper** (`fddddea8`) is a case design defect. Native `SetLifeState(Dead)` runs the dying rider's own `LeaveCombat` first: seq 18 is `HandleUnitLeaveCombat` for that rider only, not a party combat end, and the mount stays in combat. That reaches the stock `RemoveUnit`, then `BeforePairedActorRemoval` (trace 646/659), all before `OnUnitDeath`. The activation is retired and the mount stays pending for the damage round, so no split exists: the `PairedActivationSplit` and split-release-round conjuncts could not hold. The eight detach conditions are now recorded separately (`nativeLife.deathDetach`) and expect this removal shape.
- **Attack modes** (`4ec7b172`) had a wrong case premise. Mounted full attacks are a supported product path. Native TB offers the full-attack cursor only when `EstimateFullAttacks` > 1, and rider A has BAB 4. So with Rapid Shot off the attack is natively a single attack, mounted or not. Each leg's mode now comes from the native estimate, and every Rapid Shot leg must be a full attack. A mounted full attack with Rapid Shot off stays uncovered on this fixture.
- **C11** (`1fb6c2a5`) changes the experiment. If the target is still resolving its initiative, rider A takes one native same-round Delay behind it and is measured on resume. Rider A's turn is never ended, and the gate is unchanged.
- **Staged** (`ff25b49f`): the exhaustion leg searches the full circle with 7, 5 and 3.5 m legs inside the unchanged band. Every proposal is now recorded. The refused b201 leg's proposals were never recorded, so its cause stays unexplained.
- **RT Charge** (`ef2dc138`): the target point is selected from recorded candidates; the party never moves. A candidate needs a walkable point at the mount's level, an exact native straight trace, both ranges and a clear lane. The y gap in the evidence was the rider's seat height, not a terrain drop.

Checks: Fast tier 3/0 per commit (build included). Owned reader 1015/0, RT reader 62/0, owned source 323/0, cleanup 47/0, wiring 167/0. `Test-ScenarioDispatchEvidence.ps1` fails identically at `9cf18707`; that baseline failure is known. Next: native re-pilots of the five cases on one new frozen candidate.

Claude [batch201](docs/CHUNK7-BATCH201-7E-COMBAT-PILOTS-2026-10-24.md) froze pilot candidate `fe5d8e9a` (DLL `16fc8486…`, MVID `07a13203-…`, package `2c4ddd2a…`, suite `chunk7e-b201-pilots-20261024`). It ran eight native launches in two batches of four, all with exact restoration and no startup retry. These are development pilots, not 7E qualification. Batch A (the row-1 lineage) was not needed: combat cases pin the world26 producer to its own recorded candidate, and all four WhatIfs passed. **No combat case passed:**
- Death proper FAILed at "Native death cleanup changed the original split round, retained presentation or granted preparation." The native death also delivered `CombatEnded`, and the failing conjunct is not recorded.
- Attack modes FAILed: the mounted `mounted-off` click was a paired RiderRanged single attack (`fullAttack` false, 1 attack), not a native full attack.
- Staged FAILed at the round-N+2 `move-exhaust` leg: "No walkable native destination satisfied the declared geometry."
- C11 ended in its pre-input **uncovered** refusal: the target has `canActInCombat` false and is waiting on initiative in round 1.
- **The charge seed lineage passed:** cold 4/0, Manual producer 5/0 (`Manual_307` `c0f1e05c…`, world evidence `deeb957a…`), cold consumer 4/0 (persistence `3f6b1071…`).
- `owner-seed-realtime-charge` FAILed before any measured step: "Diagnostic target placement is not on a walkable native node."

Each failure needs diagnosis or a changed experiment before another run. Standing mission remains IN PROGRESS.
Docs (no code): the Chunk 7 player-facing drafts are in `docs/`: [changelog](docs/CHUNK7-CHANGELOG.md), [install/upgrade/rollback/removal](docs/CHUNK7-MIGRATION-ROLLBACK-REMOVAL.md), [owner play checklist](docs/CHUNK7-OWNER-PLAY-CHECKLIST.md), [release notes](docs/CHUNK7-RELEASE-NOTES.md) and [support and configuration](docs/CHUNK7-SUPPORT-CONFIG.md). The player guide links them and carries the settings, logs, capital and migration additions. The matrix deliverables table lists them. Resolved offline: the Saved Games path (owner-confirmed); the Unity log path; UMM log rewrite per launch; the toggle refusal texts; preview.211's handling of newer schemas (source only); and base-game rollback, since the owner's install uses no mods. Uncovered combat behaviors are listed. Still marked: one `[VERIFY]` in the changelog (no product commit cited for progression retirement), plus `[TBD after 7E freeze]` package identities. Relative links check clean.

Offline (investigation only; no code change): prepared-spell casting while mounted is recorded as **uncovered**. The leveled Ranger's level-1 slot holds no spell. The game's only fill route is a spellbook choice plus the camp rest (`RestController.StartCamp`). That rest spends rations, runs skill-rolled camping checks and rolls a random encounter (`TryRollRandomEncounter`) that the harness cannot bound. Writing slots or calling Prepare is forbidden. An honest route would need an owner seed whose Ranger already holds a memorized, rested spell. See the matrix X2 row and exclusions.

Offline (product diagnostics changed; no launch): X2 RT Charge is retargeted to the owner's new seed `KMC_C7_CHARGE_SEED`. The master is `fixtures/owner-supplied/KMC_C7_CHARGE_SEED.master.zks`, SHA-256 `1ec2f3219102d1967e1e1e7f860543550062ef07f6c270be050d26d7059aba41`, 1,238,787 bytes, byte-identical to the owner's `Manual_306_KMC_C7_CHARGE_SEED.zks`. The intake is `KMC_C7_CHARGE_SEED.intake01.json` (`1e95e7ba…`), and the seed is registered in `scripts/runtime/Chunk7OwnerSeedRegistry.json`. Offline facts: same game and area as the NO_MODS seed, no mod types, no KMC references. The main Ranger's `m_CurrentHandsEquipmentSetIndex` is 1, and set 1's primary weapon is blueprint `571c56d11dafbb04094cbaae659974b5` with the masterwork enchantment `6b38844e…` (owner: masterwork longsword). The real registry, master, intake and header gate passes offline. New case: `owner-seed-realtime-charge`. Only that case and its two lineage steps admit the charge seed. The case requires rider A's saved melee hand set and weapon, unchanged. Checks: Fast tier 3/0; owned reader 973/0, RT reader 55/0, authority 710/0, source 323/0, wiring 261/0, DTO 209/0, cleanup 47/0, cleanup adapter 43/0. `Test-ScenarioDispatchEvidence.ps1` fails identically at HEAD `be6cabd1` ("Compound registry differs from reviewed exact families"), which is a known baseline failure.
Executor chain for the charge pilot, all on one candidate, base-game only, restoring exactly:
1. `chunk7-assisted-cold-inventory` / `owner-seed-readonly-inventory`, seed `KMC_C7_CHARGE_SEED`. This is the seed's isolated cold qualification. No later gate consumes it, so record its PASS before step 2.
2. `chunk7-owned-two-pair-campaign` / `owner-seed-persistence-save`, `persistenceCase` `multi-exploration-manual`. Use row 1's selection with the charge seed's id and hash, `expectedNativeWriteCount` 1. Rider A stays the main Ranger `73681ef2`, and `xpAssistance` targets rider B only. It writes `Manual_307_KMC_C7_SEED_WORLD`.
3. `owner-seed-persistence-load` / `multi-exploration-manual` on step 2's Manual. `sourceRunId`, `sourceArtifactSha256` (world evidence) and `sourceArchiveSha256` (the `Manual_307` bytes) come from step 2. `mountAId`, `mountBId` and the full `partyIds` (both Horses included) come from the producer's field world.
4. `owner-seed-realtime-charge`: the same Source* and roles, plus `checkpointSourceRunId` and `checkpointSourceArtifactSha256` from step 3's run and persistence evidence. There is no `companionId`; 1 load and 0 writes.

No acquired chain (c1–c5) is needed.

Offline (product diagnostics changed; no launch): one independent adversarial re-review of the C11 (`9182cd65`), F04 (`869e420b`) and attack-modes (`16609e82`) fixes returned FINDINGS (1 must-fix, 4 should-fix, 5 notes). All are addressed. The must-fix: the attack-modes driver ended rider A's own turn while it waited; it no longer does. The should-fixes: "never spending" is now bounded by native game time, so a faster cooldown drop counts as a refund; pair B (and rider A in C11) may have no native cost boundary inside a window; a surprised rider A turn is ended natively before input; the C11 AoO gates are read again at the disengagement; the C11 victim's damage is bound. Rider A and mount A stay exact inside their own activation (a pre-decided judgment the pilot tests). Checks: Fast tier 3/0; `Test-Chunk7OwnedPairsEvidence.ps1` 959/0, `Test-Chunk7CombatEvidence.ps1` 53/0, `Test-Chunk7OwnedPairsAuthority.ps1` 677/0, `Test-Chunk7OwnedPairsSource.ps1` 321/0, `Test-Chunk7CombatCleanupAdapter.ps1` 43/0.

Offline (product diagnostics changed; no launch): X1 C03 attack modes now compare pair B as never spending in every measurement (no cooldown grows, reaction count unchanged), as C11 and F04 do, because native TB cooldowns of every unit decay after a finished turn. Mount A stays exact while mounted and is compared as never spending in the unmounted control; its native cost calls stay refused. Checks: Fast tier 3/0 (build included, 0.5 min); `Test-Chunk7OwnedPairsEvidence.ps1` 949/0 (8 new), `Test-Chunk7CombatEvidence.ps1` 53/0. The native pilot is pending.

Offline (harness only; product DLL unchanged; no launch): the X5 carry-source residual is closed for new runs. Every normal-player outer result records `normalPlayerRequestSha256`, and the runner refuses a request changed after launch. `Test-RuntimeResult` verifies the hash. The u1 carry-source read and `Assert-KmcChunk7UpgradeRehearsalComplete` require each step's request to be bound. The real b200 steps are accepted only through request SHA-256s pinned by exact run id and result SHA-256 (`legacy-pinned`). Remaining residual: those pins were observed offline today, not written at run time. Checks: `Test-Chunk7UpgradeRehearsal.ps1` 240/0 (after review; the runner's own block runs from its source), `Test-Chunk7NormalPlayerControl.ps1` 52/0, 21 chunk7 wiring suites 0 failures, Fast tier 3/0. See [the X5 row](planning/CHUNK7-7E-QUALIFICATION-MATRIX.md).

Offline (product diagnostics changed; no launch): X1 F04 staged moving actions are implemented as the owner-seed case `owner-seed-staged` on the TB rider-first engine. It runs the 6D plans `C6D-move-cast-move` and `C6D-double-move-ranged`, then a ranged approach row: beyond range, native movement exhaustion and its refusal, and a RiderRanged shot from beyond range. Rider A uses its own ranged weapon and a four-unit native Cure Light Wounds scroll stack built on the equipped entity. The defensive-check half is uncovered: there is no engaged cast past one second on this geometry. Checks: Release build clean; Fast tier 3/0; `Test-Chunk7OwnedPairsEvidence.ps1` 941/0 (after review), `Test-Chunk7CombatEvidence.ps1` 53/0, `Test-Chunk7OwnedPairsAuthority.ps1` 677/0, `Test-Chunk7OwnedPairsSource.ps1` 321/0, `Test-Chunk7CombatCleanupAdapter.ps1` 43/0. The native pilot is pending; see [the X1 row](planning/CHUNK7-7E-QUALIFICATION-MATRIX.md).

Offline (product diagnostics changed; no launch): X1 C11 reactions are implemented as the owner-seed case `owner-seed-combat-reaction` on the TB rider-first engine. On rider A's first turn, pair A leaves the diagnostic target's threatened area by one ordinary native ground order. The reader requires exactly one native attack of opportunity by the target, its counter dropping by one, the move charged to mount A only and never refunded, and pair B and the target unchanged. The prone stand-up half is recorded as uncovered: no deterministic native prone source exists on this fixture. Checks: Release build clean; Fast tier 3/0 (components 877/0, Validate-Source 147/0, 0.5 min); `Test-Chunk7OwnedPairsEvidence.ps1` 869/0, `Test-Chunk7CombatEvidence.ps1` 53/0, `Test-Chunk7OwnedPairsAuthority.ps1` 659/0, `Test-Chunk7OwnedPairsSource.ps1` 320/0, `Test-Chunk7CombatCleanupAdapter.ps1` 43/0. The native pilot is pending; see [the X1 row](planning/CHUNK7-7E-QUALIFICATION-MATRIX.md).

Offline (product diagnostics changed; no launch): X1 C03 attack modes are implemented as the owner-seed
case `owner-seed-attack-modes` on the TB rider-first engine. A zero-launch read of Manual 304 confirmed
rider A's native Rapid Shot feat and toggle and an equipped ranged weapon. The case runs native full
attacks with Rapid Shot off and on (mounted), then an unmounted control after a native Dismount, all
switched through the native slot. BAB and haste variation stay uncovered. The native pilot is pending;
see [the X1 row](planning/CHUNK7-7E-QUALIFICATION-MATRIX.md).

Offline (product diagnostics changed; no launch): X1 death proper is implemented as the owner-seed case
`owner-seed-native-death-cleanup` on the native life engine. One labelled native damage reaches HP+Con+1,
and the stock tick kills. The reader requires one `UnitDeath` cleanup inside the death callback, no N+1
slot for the dead rider, an unmounted dead control and the observed cleanup postconditions. An immortal
or essential subject is refused before damage and recorded uncovered. The native pilot is pending; see
[the X1 row](planning/CHUNK7-7E-QUALIFICATION-MATRIX.md).

Offline (harness only, no launch): the X5 row consumers now exist. `Assert-KmcChunk7NormalPlayerRow42Binding`
binds row 42 to one hash-pinned passed ordinary control and refuses every rehearsal step;
`Assert-KmcChunk7UpgradeRehearsalComplete` binds rows 55–56 to one complete u1 → u2 → u3 rehearsal and
accepts the real b200 `x5-b200-20261024` (`Test-Chunk7UpgradeRehearsal.ps1` 222/0; residual since closed for new runs, below). See
[the X5 row](planning/CHUNK7-7E-QUALIFICATION-MATRIX.md).

Claude [batch200](docs/CHUNK7-BATCH200-7E-X5-2026-10-24.md) froze candidate `b9ce53d5` (DLL `ec20ee8f…`,
unchanged product; package `b11021fa…`, suite `chunk7e-b200-x5-20261024`, observer.6 `c85dc7c1…`) and ran
the **X5 upgrade/rollback rehearsal end to end for the first time** (development pilot, three launches, all
with exact restoration). u1 (preview.211) PASS 27/0, admitting exactly the one pinned Horse registration
line. u2 (upgrade over u1's carried cache) PASS 42/0, strict: no error lines and the known line absent.
u3 (rollback to u1's folder state) PASS 27/0, with the same single admitted line. The reader verified each
receipt hash. This is not 7E qualification. Standing mission remains IN PROGRESS.

Historical: X5 follow-up (offline, harness only; product DLL unchanged): by lead decision, u1 and u3 (both stage the
exact preview.211 package) now admit exactly its one known base-game Horse registration exception line,
once, only for the exact pinned identity, recorded as `admittedOldVersionKnownDefects` (fix `81c191af`).
u2 keeps the strict quiet start. The outer result records the UMM receipt's SHA-256 and the external
reader verifies it and re-judges the receipt on every PASS. A fresh native u1, then u2 and u3, is next; see
[the X5 row](planning/CHUNK7-7E-QUALIFICATION-MATRIX.md).

Claude [batch199](docs/CHUNK7-BATCH199-7E-PILOTS-2026-10-24.md) froze pilot candidate `3842ad94`
(DLL `ec20ee8f…`, suite `chunk7e-b199-pilots-20261024`). Nine native launches, all with exact
restoration. The acquired chain (c1–c5), the X4 producer and row 58 passed again. **Row 59 passed for
the first time** (development pilot): with the lineage area pinned from row 58's record, Bind's readiness
check passed for the three remote story companions. The capital Manual 309 then cold-loaded with exactly
the main pair restored (`all-pair-presentation-restored`, no Mount or preparation) and pair B unmounted
out of scene. The X5 rehearsal stopped at u1 (outer FAIL; observer 27/0): the released preview.211 logs a
Horse blueprint registration exception on a base-game-only install, because it admits only the CotW
seven-option Ranger list. So u1's quiet-start premise fails, and it needs a decision. u2 and u3 did not
run. Standing mission remains IN PROGRESS.

Historical: Claude [batch198](docs/CHUNK7-BATCH198-7E-PILOTS-2026-10-24.md) froze pilot candidate `767feaea`
(DLL `870b6437…`, suite `chunk7e-b198-pilots-20261024`). Eight native launches, all with exact
restoration. The acquired chain (c1–c5) and the X4 producer passed again. **Row 58 passed for the first
time** (development pilot): native Oleg's again sent the three story companions and pair B to remote, the
capital admission recorded them, and Manual 309 `KMC_C7_CAPITAL` was written. Row 59 failed closed on a
diagnostic defect in `ReadSource`: it compares the lineage Manual's write area (Spider Cave) with the load
area (the capital). The fix awaits review; Bind's remote-companion readiness check was not reached. The X5
upgrade rehearsal did not run: observer.6 cannot be packaged because the observer `Info.json` still says
observer.5. Standing mission remains IN PROGRESS.

Historical: Claude [batch197](docs/CHUNK7-BATCH197-7E-PILOTS-2026-10-24.md) froze pilot candidate `c68fabd9`
(DLL `c0fa0ebe…`, suite `chunk7e-b197-pilots-20261024`). Eleven native launches (one Steamworks
startup failure retried once), all with exact restoration. **Row 57, the two-pair native life and the
Small scroll/potion item pilot passed** for the first time; the unmounted unconscious control was
available and matched in both life runs. The acquired chain and the X4 producer passed again. Row 58
failed closed on the known risk: native Oleg's moves the three story companions to remote as well
as the hired pair B, which is beyond the one admitted out-of-scene pair. Row 59 stays blocked pending a
decision. Standing mission remains IN PROGRESS.

Historical: Claude [batch196](docs/CHUNK7-BATCH196-7E-PILOTS-2026-10-24.md) froze pilot candidate `241bbd8b`
(DLL `5a34f1c3…`, suite `chunk7e-b196-pilots-20261024`; executed from its frozen worktree on a local
branch because the root branch moved to a doc-only commit during the freeze). Nine native runs, all
with exact restoration. The acquired chain (c1–c5), the X4 producer and the **X4 three-area cold
pilot on the Manticore route passed**. Row 58 failed closed: the product dismounted the companion pair
in Oleg's exactly as designed, but native Oleg's took the hired companion pair out of the scene, which
falsified the fixture's roster assumption. Row 57 failed on a diagnostic defect: a null paired grant
identity written from a `string` counts as present. The batch stopped there; row 59, the two-pair
native life and the Small item pilot did not run. Both fixes await review. Standing mission remains
IN PROGRESS.

Historical: Claude [batch195](docs/CHUNK7-BATCH195-7E-PILOTS-2026-10-24.md) froze pilot candidate `6b80d9a6` and
passed the acquired chain (c1–c5) and the X4 Manual producer. The three-area cold pilot then hung:
after the first transfer (Spider Cave → Oleg's) KMC refused all-pair area restoration because a unit
was not directly controllable, cleanup fenced the quit, and the run ended restoration-blocked with
the game still running. Recovered under authorization (process stopped by verified PID, transaction
recovered; two Unity session PlayerPrefs remain to restore). Cause: Oleg's is the chapter-1 capital,
where native control excludes non-main units, so the route was invalid; a capital guard and a
Manticore route replace it. The capital refusal is a player-facing defect; a deferral design awaits review. Standing mission remains IN PROGRESS.

Historical: Claude [batch194](docs/CHUNK7-BATCH194-7E-PILOTS-2026-10-23.md) ran the second 7E pilot pass on
candidate `1eecc9bb` (DLL `c7e0258f…`): eleven native runs, all with exact restoration. The acquired
chain (owner-seed cold → Ranger 4 Horse → acquired cold → paid Small Ranger → two-owner Manual)
passed on one candidate, as did the X4 Manual producer (one Steamworks startup failure retried).
Row 57 and the two-pair native life now pass the life callback and healthy neighbour, then fail
because the native turn controller prepares the unconscious rider's own next turn. That was decided
as native behavior the test misjudged; an exact order-derived expectation and an unmounted unconscious
control are implemented offline and await review. RT Charge is a fixture
limit (KMC correctly refuses: the rider has no melee weapon). The three-area cold failed on a Horse
subscriber expectation that missed the Small Horse profile; the fix awaits review. Standing mission
remains IN PROGRESS.

Historical: Claude [batch193](docs/CHUNK7-BATCH193-7E-PILOTS-2026-10-23.md) started the 7E development pilots on
a frozen pilot candidate (`503d2fd9`, DLL `d5cbae3c…`). The row-57 owner-seed incapacitation pilot
FAILed in game on a diagnostic defect: the healthy-owner lookup compared the registry's
"N"-form owner key with the hyphenated `UniqueId`, so native life could never complete (it had
never run natively). The subject precondition and the native damage passed first; restoration was
exact. A focused fix awaits independent review. Zero-launch: `realtime-casting` is uncovered on the
b164 source (no prepared healing slot); `realtime-charge` and the three-area cold need the acquired
chain re-run on the same candidate. Standing mission remains IN PROGRESS.

Historical: Claude [batch192](docs/CHUNK7-BATCH192-DOOR-SEED-COLD-2026-10-23.md) admitted the owner's open-door Old
Sycamore seed: its isolated cold load passed (s1r) after a reviewed fix for a Spider Cave area pin
(s1 FAIL preserved), and owner-seed exploration passed with the door recorded NOT APPLICABLE.
The owner's open `TavernDoor` is 8.45/7.92 m from the mounts and otherwise clean, but its
navmesh cut stays active when open (`DisableNavmeshCutWhenOpen = false`). Static-cut doors are now
admitted (`b113418a`, review CLEAR: the requirement is the mounted click, selection change and
exact restoration, not the cut toggle), so that NOT APPLICABLE is superseded. Batch192c's door outcome serialized as an empty
`{"$id"}` shell (outer FAIL kept). After an explicit-serialization fix (review CLEAR), batch192d
passed: each pair's mounted click on `TavernDoor` succeeded (one interaction, one approach, mount
Move slot restored) with a selection change, the static cut stayed unchanged and restoration was
exact. The 7B ordinary-door row is a development PASS; exact-package qualification is 7E. Standing
mission remains IN PROGRESS.

Historical: Claude [batch191](docs/CHUNK7-BATCH191-REQUEST-FREE-CONTROLS-2026-10-23.md) passed the F11 request-free
KMC ordinary controls under an external observer (attempt s2; s1 is a preserved FAIL whose
Params restoration was recovered exactly). KMC ran from the frozen batch190 package with no
automation request, in an isolated tree holding exactly KMC and the observer. Native Mount on
the rider's own Horse, a mounted ground click (pair moved 4.4/4.0 m together), native Dismount
and an unmounted click all succeeded with the neighbor pair untouched. No automation host,
measurement, telemetry or overlay ran; KMC's own UMM log shows an ordinary quiet start with no
error. Restoration exact. Not performance acceptance or human approval; hotbar slots are not
pre-filled (owner checklist item). Next: F10 behaviour gaps, the door route, the
requirement-to-evidence table, then private 7E. Standing mission remains IN PROGRESS.

Historical: SHOULD-FIX 3 is closed offline: the [six-pair exit reader check](docs/CHUNK7-SIX-PAIR-EXIT-READER-2026-10-22.md)
rebuilds each six-pair lineage's real native evidence as the unattended exit leaves it, pins
every mirrored product statement, and passes the complete unchanged game-result reader and the
runner's base-game check 25/0 (honest exits accepted, 16 refusals each with the reader's own
message); independent review CLEAR. No native exit was exercised. Next: the F11 request-free
normal-player control (a KMC-plus-observer staging mode), then doors, F10 breadth and private 7E.

Historical batch190 checkpoint: Claude [batch190](docs/CHUNK7-BATCH190-SIX-PAIR-MEASUREMENT-PASS-2026-10-22.md) passed one
declared zero-write six-pair lifecycle on frozen candidate `c7b-world190-sixmeasure`
(`62a68e64`): game 6/0, outer PASS, 36 native casts and 0 refusals, sibling order verified after
all three cycles. With full custody moved to window boundaries, the per-state KMC Update medians
were 0.294/0.339/0.404/0.486/0.290 ms at 0/1/2/6/0 active pairs, against about 5 ms in batch188.
The pair phase was unchanged at 0.003–0.173 ms, so batch188's extra ~4.7 ms was the observer's
own work. This is F11 development evidence with an automation request present;
`performanceAccepted` stays false. Restoration was exact with zero cache changes; no game or
lock remains. Next: SHOULD-FIX 3, then the remaining F10/F11/door work and private 7E.
Standing mission remains IN PROGRESS.

Historical batch190 preparation checkpoint: Claude's [batch190 preparation](docs/CHUNK7-B190-SIX-PAIR-SAMPLER-CUSTODY-2026-10-22.md)
moves the six-pair observer's full inherited custody out of the measured frames. That check
re-hashes the 183 KB staged receipt and rebuilds roster and progression on every frame; it now
runs at each sampling window's boundaries, as the two-pair matched pool already does. The
observation contract becomes v2 and declares `measuredFramesCarryFullCustody=false`; the closed
reader accepts the exact v1 or v2 shape. Diagnostics only. Offline: orchestration 21/0 with
all four targeted mutations caught, reader 50/0; independent review CLEAR, its SHOULD-FIX
applied. No native run yet. Next: one frozen candidate and one zero-write six-pair sampling
run, then SHOULD-FIX 3, the remaining F10/F11/door work and private 7E. Standing mission
remains IN PROGRESS.

Historical batch189 checkpoint: Claude [batch189](docs/CHUNK7-BATCH189-SIX-MOUNTED-MANUAL-COLD-PASS-2026-10-22.md) passed both
declared native scenarios on frozen candidate `c7b-world189-sixpersist` (`a2ef0e95`). The
producer mounted all six roles natively (0 refusals) and bound twelve slots. It wrote
`Manual_309_KMC_C7_SIX_MOUNTED_WORLD.zks`, whose read-back holds 6 pairs and 12 slots, and moved
every pair: game 6/0, outer PASS. A separate process then cold-loaded that Manual. It restored
all six pairs with 12 semantic restores, 6 presentations, 0 Prepare and 0 Mount replay, and
moved every pair: game 3/0, outer PASS. The separate-process proof shows archive, owners,
generations and slots equal to the producer's read-back. Both runs restored exactly with zero
cache changes; no game or lock remains. Next: a representative six-pair performance
measurement (F11), SHOULD-FIX 3, then the remaining F10/F11/door work and private 7E. Standing
mission remains IN PROGRESS.

Historical batch189 preparation checkpoint: the [six-mounted native Manual and cold consumer](docs/CHUNK7-B189-SIX-MOUNTED-MANUAL-AND-COLD-2026-10-22.md)
adds two closed owned-pairs cases on the batch185 six-Horse fixture. The producer mounts
all six roles natively, binds twelve slots and writes one Manual309; a separate process
cold-loads it and restores all six pairs with no Prepare or Mount replay. The unattended
FAIL exit now covers both, once every requested native write has completed. Offline:
Fast 3/0, harness 281/0, new focused checks all PASS, 37 of 38 mutations caught;
independent review CLEAR, both SHOULD-FIX items handled. No native run yet. Next: one
frozen candidate, the producer run, then the cold run with its producer pins. Standing
mission remains IN PROGRESS.

Historical batch188 checkpoint: Claude [batch188](docs/CHUNK7-BATCH188-SIX-PAIR-LIFECYCLE-PASS-2026-10-22.md) passed one
declared zero-write six-pair lifecycle scenario on the frozen `c7b-world188-slot`
candidate (`64a8206e`): game 6/0 and outer PASS. The shared native rider parent (16
children) kept its exact child order after all three cycles, where batch187 failed.
36 native Mount/Dismount casts, 0 refusals. Mods, saves and profile were restored
exactly with zero cache changes; no game or lock remains. The unattended FAIL exit was
not exercised. The ~5 ms KMC Update in every state includes this observer's per-frame
verification (pair phase 0.19 ms at six pairs), so it is not a performance measurement.
Next: the six-mounted genuine Manual and an independent cold consumer. Standing
mission remains IN PROGRESS.

Historical batch188 preparation checkpoint: the [sibling-slot correction and unattended six-pair FAIL exit](docs/CHUNK7-B188-SIBLING-SLOT-AND-UNATTENDED-EXIT-2026-10-22.md)
implements the batch187 cause. Each attachment lease owns an inert reservation in its
rider's exact slot, so restores are exact in any dismount order. The pose component is released only after
the attachment verifies. A disposable zero-write six-pair run whose cleanup cannot settle
now publishes an honest FAIL and quits normally once, so restoration needs no owner.
Offline: Fast 3/0 (869 components, 147 source), harness 281/0, focused actual-method
and reader checks all PASS; independent review and focused re-review CLEAR. No new
native run yet. Next: one frozen candidate and one zero-write six-pair lifecycle native
scenario, then the six-mounted Manual/cold route. Standing mission remains IN PROGRESS.

Historical pre-batch188 checkpoint:

Owner requested transfer to Claude. The [batch187 handoff](docs/CLAUDE-HANDOFF-CHUNK7-BATCH187-2026-10-22.md)
records the decisive shared-parent sibling-index failure, unchanged original
native/outer FAILs, exact restoration and next implementation steps. No game
or lock remains. Fresh read-only Mods/save/profile/Params/PlayerPrefs checks
match the run snapshot with zero cache exceptions. The reviewed cleanup JSON
fix passes installed-serializer7/0 and Fast3/0; no new native package/run.
The uncompiled, unreviewed unattended FAIL-exit draft is preserved privately
outside active source. No further native attempt until that path and the causal
attachment correction are reviewed/tested. Standing mission remains IN PROGRESS.

Historical pre-batch187 checkpoint:

[Batch186](docs/CHUNK7-BATCH186-SIX-PAIR-RESTORATION-FAILURE-2026-10-22.md)
failed on native sibling-index restoration during the third six-pair lifecycle
cycle. Its original FAIL remains; owner normal exit followed by guarded combined
and independently reviewed profile recovery restored all surfaces exactly,
zero cache exceptions. No game/lock remains. A bounded read-only lease observer
now passes 15/0 plus orchestration18/0; next is one distinguishing native run on
a new frozen candidate. The standing completion mission remains IN PROGRESS.

Historical pre-batch186 checkpoint: the reviewed [six-pair observer](docs/CHUNK7-SIX-PAIR-OBSERVER-2026-10-22.md)
now has focused source/custody/orchestration/reader/cleanup checks and independent
safety review. No six-pair native action has run yet. Next is one clean frozen
candidate and a declared one-load/zero-save native lifecycle/routing/sampling
scenario; wider progression, controls, doors and private 7E remain selected.

The reviewed [six-pair admission and fixture gate](docs/CHUNK7-SIX-PAIR-ADMISSION-2026-10-22.md)
now selects six native active pairs, with twelve retained owners. Actual
ownership 42/0, archive/failure-latch 20/0, codec 77/0, Fast 3/0 and affected
readers passed offline. The exact b185 checkpoint gate passed 54/0 without
replaying acquisition ancestry. No six-pair native action has run. Next is
the distinct opt-in mounted lifecycle/performance observer and its review,
then guarded native qualification on a new frozen candidate.

The reviewed [batch185 sixth-Horse cold consumer](docs/CHUNK7-BATCH185-SIXTH-HORSE-COLD-2026-10-22.md)
passed one separate-process native load, original game/outer 4/0. It preserved
the genuine batch184 Manual308's twelve field actors/six sourced Horses and
exact remote Linzi progression, with zero saves or resource writes. Mods,
protected saves and profile restored exactly, zero cache exceptions; wrapper
exited0 and no game/lock remains. Six mounted owners, conditional lifecycle/
performance stress and the wider standing mission remain IN PROGRESS.

[Batch184](docs/CHUNK7-BATCH184-SIXTH-HORSE-MANUAL-2026-10-22.md) ran one native
sixth paid-Horse Manual scenario: game/artifact PASS 7/0, one load/one genuine
Manual_308, twelve field actors/six characters and exactly remote Linzi. Its
original outer FAIL remains recorded for a missed external-reader price check.
The reviewed reader-only correction passed raw-artifact 9/0 and neighboring
reader 118/0; separate full immutable game/archive and base-only inventory
re-evaluations PASS with original verdicts/flags unchanged. All transactions and
profile bytes restored exactly, zero cache exceptions, no game/lock. Sixth
cold continuity, mounted breadth, conditional stress and the wider mission
remain IN PROGRESS.

[Batch183](docs/CHUNK7-BATCH183-LINZI-NATIVE-DEPARTURE-2026-10-22.md)
passed the real native Linzi field departure, game 5/0, on the frozen
preview.94 package. The original outer result remains FAIL on the omitted
subscenario name; the reviewed external-reader correction passed separate
immutable game/artifact and native-loader re-evaluations. One load, zero
saves, ten retained actors/five characters, exact remote Linzi and all five
sourced Horses survived. Mods, protected saves and profile bytes restored
exactly, zero cache exceptions; no game or lock remains. This closes only
the disposable native roster transition. Sixth paid acquisition, genuine
Manual/cold, mounted fifth/sixth pair, conditional six-pair stress and the
wider standing mission remain IN PROGRESS.

The reviewed [batch182 fifth-Horse cold consumer](docs/CHUNK7-BATCH182-FIFTH-HORSE-COLD-2026-10-22.md)
passed one guarded separate-process base-only Manual load: native game and
original outer 4/0, eleven field actors, six characters and five distinct
reciprocal sourced Horses. One load, zero saves or resource writes, and no
native preparation. Run/save/Mods/profile bytes restored exactly with zero
accepted cache changes. This closes only the fifth-Horse unmounted Manual/cold
preservation row; ordinary sixth-rider roster transition/build, mounted
fifth/sixth pair, conditional six-pair stress and the wider standing mission
remain **IN PROGRESS**.

The reviewed [batch181 fifth paid Horse Manual](docs/CHUNK7-BATCH181-FIFTH-HORSE-MANUAL-2026-10-22.md)
passed native game and original outer 7/0 on one guarded base-only run.
Five field characters/four native Horses became six characters/five Horses;
the native paid Halfling builder wrote genuine Manual_307 while all four
prior Horse references survived. One load and one save were requested.
Run/save/Mods/profile bytes restored exactly with zero accepted cache changes.
The archive still needs separate cold qualification. Sixth rider, mounted
fifth/sixth pair and conditional six-pair stress remain **IN PROGRESS**.

The reviewed [batch180 native selector observation](docs/CHUNK7-BATCH180-FOURTH-HORSE-SELECTOR-2026-10-22.md)
passed game and original outer 5/0 on a separate disposable load of the genuine
fourth-Horse Manual. Linzi is optional in the opened native selector
(`MustBeInParty=false`), which establishes a feasible sixth-rider roster route
but does not perform removal. One load, zero saves, zero roster switches/Go;
run, save, Mods and profile bytes restored exactly. The reviewed read-only
selector source gate passed actual-receipt and changed-anchor checks 4/0 and
grants no native build authority. The fifth paid Horse in
the open slot, actual sixth selector transition/paid hire, their Manual/cold
preservation and conditional six-pair stress remain **IN PROGRESS**.

The reviewed [F09 nine-actor source gate](docs/CHUNK7-F09-B179-NINE-ACTOR-ROSTER-ROUTE-2026-10-22.md)
pins the genuine b178 Manual and b179 original cold PASS without granting
native fixture authority. Actual-receipt and changed-anchor checks passed 2/0.
The saved field has four sourced Horses, five characters, one open slot and
enough observed funds for a fifth paid hire. Linzi's optional selector status
and a sixth supported rider remain unproved; no fifth/sixth native action ran.

[Batch179](docs/CHUNK7-BATCH179-FOURTH-HORSE-COLD-2026-10-22.md) passed
the separate-process fourth-Horse Manual cold load on a reviewed package:
native game and original outer 4/0, nine field actors, four reciprocal sourced
Horses, one load, zero writes and no native preparation. Run/save/Mods/profile
bytes restored exactly. Batch178's original outer FAIL and its distinct
reader-only PASS remain separate. This closes only the development unmounted
fourth-Horse Manual/cold row. Fifth/sixth supported pairs, conditional six-pair
stress, F11 normal-player controls, doors, remaining 7C/7D and private 7E
remain **IN PROGRESS**.

[Batches177–178](docs/CHUNK7-BATCH177-178-FOURTH-HORSE-MANUAL-2026-10-22.md)
used one corrected fourth-Horse Manual candidate. Batch177 is an original outer
FAIL at Steamworks startup, before a native result. Batch178 passed native game
5/0 and wrote a genuine nine-actor `Manual_306_KMC_C7_FOUR_HORSE_WORLD.zks`;
its original outer result is FAIL on a closed output-number reader omission.
The corrected external reader passed separately on immutable b178 evidence,
and the isolated source/output archives match their pinned hashes. All run,
save, Mods and profile bytes restored exactly. A distinct cold load remains
required before four-Horse persistence or further source ancestry is accepted.

[Batch176](docs/CHUNK7-BATCH176-FOURTH-HORSE-MANUAL-NAME-FAIL-2026-10-22.md)
is an original guarded game/outer **FAIL 4/1**. The fourth native Horse setup
passed again, but the new Manual was refused before `Game.SaveGame` because its
requested name repeated the loaded b171 source name and native `CreateNewSave`
made it unique. One native load, zero saves; run/save/Mods/profile bytes restored
exactly. A distinct output-name correction passed independent review and
Candidate 9/0 but has no corrected native verdict yet. Four-Horse Manual
and cold preservation remain IN PROGRESS, as do the standing mission's other
engineering gates and selected private 7E.

[Batch175](docs/CHUNK7-BATCH175-FOURTH-HORSE-NATIVE-SETUP-2026-10-22.md)
passed one guarded zero-write fourth-Horse native setup from the accepted
batch171/batch174 three-Horse lineage: game and original outer 4/0, nine field
actors, four ordinary Valerie Ranger commits and a fourth sourced reciprocal
Horse. All transaction surfaces and profile bytes restored exactly. This is
assisted in-memory fixture construction; four-Horse Manual/cold, fifth/sixth
pair construction, conditional six-pair stress, F11, doors and private 7E
remain **IN PROGRESS**.

The accepted [batch174](docs/CHUNK7-BATCH174-THIRD-HORSE-COLD-ORIGINAL-PASS-2026-10-22.md)
three-Horse Manual/cold lineage now has an explicit
[F09 eight-actor derivative route](docs/CHUNK7-F09-B174-SIX-PAIR-FIXTURE-ROUTE-2026-10-22.md).
Its implemented read-only source gate passed against the retained original
producer/cold receipts, rejected an altered fixture hash and grants no native
authority. The source inventory identifies a Medium Valerie and a special-body
Small Linzi as the two remaining petless stock companions, with one free
character slot. Linzi's genuine native pet, if acquired, would not by itself
match the shipped default male Halfling mounted profile. Batch175 has since
run the first distinct eight-actor derivative; conditional
six-pair stress, ordinary doors, F11 and private 7E remain IN PROGRESS.

[Batch174](docs/CHUNK7-BATCH174-THIRD-HORSE-COLD-ORIGINAL-PASS-2026-10-22.md) passed a fresh third-Horse separate-process native cold load **game 4/0 and original outer PASS 4/0** on unchanged frozen preview.94 product with a corrected clean external reader. Eight actors and three distinct sourced native Horses survived; one load, zero writes, no preparation and exact run/save/Mods/profile restoration were observed. Batch172/173 original FAILs remain separate. The other F09 breadth, F11, doors, progression and selected private 7E remain **IN PROGRESS**.

[Batch173](docs/CHUNK7-BATCH173-THIRD-HORSE-COLD-2026-10-21.md) passed a genuine separate-process third-Horse native cold load **game 4/0**, preserving eight actors and three distinct Horses with one load, zero writes and exact transaction/profile restoration. Its original outer PID-reader result is **FAIL**; a reviewed external-reader correction and immutable-artifact reader-only PASS remain separate. Batch172's pre-load authority FAIL and b171's Manual producer PASS are unchanged.

[Batch172](docs/CHUNK7-BATCH172-THIRD-HORSE-COLD-AUTHORITY-FAIL-2026-10-21.md) records an original third-Horse cold consumer **FAIL before native load** on preview.94: the exact Manual case was missing from the read-only save authority. The b171 producer remains PASS. A narrow reviewed authority correction passed focused 149/0 and Fast 3/0; batch173 separately verified the corrected native load. All b172 transactions restored; profile bytes were exact.

[Batch171](docs/CHUNK7-BATCH171-F09-THIRD-HORSE-MANUAL-PASS-2026-10-21.md) passed one genuine, guarded native third-Horse Manual producer on preview.94: game 5/0, outer PASS, archived eight-actor source SHA-256 `f70a54865c86a599c3b25f6f2574abde2f663cd07b82472eac4bcbe245ea8c84`. All run/save/Mods transactions restored; 402 accepted native cache changes are not exact profile bytes. The separate-process cold continuation remains required before F09 persistence is qualified. The standing 7B/7C/7D/private 7E mission remains **IN PROGRESS**.

[Batch170](docs/CHUNK7-BATCH170-F09-THIRD-HORSE-MANUAL-FAIL-2026-10-21.md) original native game/outer **FAIL** occurred before load when the new third-Horse Manual reservation used a generic name instead of the selected Small name. Guarded run/save/Mods restoration passed; 552 accepted profile-cache changes are not exact profile bytes. The narrow native-name correction is independently reviewed and Fast 3/0, but has no corrected in-game result. A saved three-Horse derivative and independent cold continuation remain open. The standing 7B/7C/7D/private 7E mission is still IN PROGRESS.

[Batch169](docs/CHUNK7-BATCH169-F09-THIRD-HORSE-2026-10-21.md) original native game **PASS 4/0** establishes a genuine sourced third Horse on a disposable Small-source copy after disclosed XP and four ordinary Ranger commits, preserving the prior two Horses with zero save requests. Original outer **FAIL** from a missing setup-only matched-pool artifact stays separate from an independently reviewed, immutable-input reader-only PASS. All guarded transaction surfaces restored; 26 accepted profile-cache changes mean profile bytes were not exact. A persistent derivative, other Horses, sixth rider and conditional six-pair stress remain **IN PROGRESS**, as do F11 normal-player, doors, remaining 7C/7D and private 7E.

[Batch168](docs/CHUNK7-BATCH168-F09-THIRD-HORSE-FAIL-2026-10-21.md) is an original guarded native game/outer **FAIL**: the exact Small source loaded and retained two original Horses, then a headless stock auto-levelup plan made the diagnostic's `SetClass` UI wrapper throw before Ranger commitment. All transaction surfaces restored, with 25 accepted native profile-cache changes. The narrow controller-level `SelectClass` correction passed Fast 3/0 and independent exact-native review; a new candidate and native verification remain. No third Horse or six-pair PASS is claimed.

The reviewed [F09 third-Horse source diagnostic](docs/CHUNK7-F09-THIRD-HORSE-SOURCE-2026-10-21.md) now has an exact one-load/zero-write batch53 Small Manual route for disclosed native XP assistance and four ordinary Ranger commitments on one story actor, with inherited-pair and source custody retained. Fast 3/0, affected protocol/reader checks and independent review passed offline; no source-specific native third Horse or saved derivative has yet run. The conditional six-pair, F11 normal-player, ordinary door, remaining 7C/7D and selected private 7E gates stay **IN PROGRESS**.

[Batch167](docs/CHUNK7-BATCH167-F09-NATIVE-RESPEC-INVENTORY-2026-10-21.md) passed one guarded clean-owner-seed cold inventory 4/0 on the reviewed read-only respec diagnostic. The three petless level-4 stock companions lack level-up entitlement but are in the native respec selector and satisfy its predicate; cost is zero and the difficulty setting permits it. This is an actionable fixture lead, not a retrain, Horse grant, six-pair or performance PASS. Transaction restoration passed with 27 accepted achievement-cache changes. The standing completion mission remains IN PROGRESS.

Retained batch26 native evidence already passed one petless story companion's disclosed 42,000-XP setup and four ordinary Ranger commitments to a genuine Horse. The batch53 Small source has five field riders/two Horses and an ordinary paid hire at 9,000 inherited XP with `MatchPlayerXpExactly=false`; one field slot remains. The source-specific third-Horse coexistence, other two story Horses, sixth hire and conditional six-pair stress remain unqualified. No special matched-XP hire is selected.

The [F09 native respec-readiness checkpoint](docs/CHUNK7-F09-NATIVE-RESPEC-READINESS-2026-10-21.md) added a bounded read-only inventory of exact stock retrain prerequisites for the unresolved conditional six-pair fixture. Independent review and Candidate 7/0 passed; batch167 subsequently ran its selected owner-seed cold inventory. No native retrain or six-pair stress is claimed. The standing 7B/7C/7D and selected private 7E mission remains IN PROGRESS.

[Batches 163–166](docs/CHUNK7-BATCH163-166-F10-OWNER-CUMULATIVE-COLD-2026-10-21.md) pass the exact owner-seed cumulative Manual 306, two-owner acquired Manual 307, genuine two-pair mounted schema-3 Manual 308, and independent-copy separate-process cold load on one frozen preview.94 product. The four original game/outer outcomes are 7/0, 8/0, 5/0 and 3/0. All guarded transactions restored Mods/Working/protected saves; 29/28/26/27 accepted profile cache changes mean profile bytes are not exact. Batch162's original outer FAIL and its reader-only PASS remain separate. F09 conditional six-pair, F11 normal-player controls, remaining 7C/7D and private 7E remain IN PROGRESS.

[Batches 159–162](docs/CHUNK7-BATCH159-162-F10-OWNER-CUMULATIVE-2026-10-21.md) established the fresh preview.94 one-owner owner-seed lineage through native Manual 305. Batch162's cumulative game passed 7/0 and wrote native Manual 306, but its original outer reader failed on a null synchronous PostLoad GameId; the strict reader correction passed focused and neighboring checks plus separate read-only re-evaluation of the unchanged artifact. The original FAIL remains; a fresh producer with an original outer PASS is required before downstream qualification. All four guarded transactions restored Mods, Working and protected saves, with accepted profile-cache changes. The standing engineering mission remains IN PROGRESS.

The [reviewed one-owner cumulative source correction](docs/CHUNK7-F10-OWNER-SEED-CUMULATIVE-ROUTE-2026-10-21.md) admits genuine owner-seed acquired Manual 305 → native cumulative Manual 306 with exact ancestry and downstream reader/consumer authority. Fast and focused safety/behavior checks pass; it is not yet a native PASS. The legacy 302→303 prelaunch refusal remains, and batch158's two-owner acquired Manual 302 remains a different input. A new exact candidate and same-candidate native chain are next; the standing 7B/7C/7D and private 7E mission remains IN PROGRESS.

[Batch158](docs/CHUNK7-BATCH158-F10-SOURCE-LINEAGE-2026-10-21.md) passed four same-package guarded native scenarios: assisted Manual 300, separate cold, genuine paid Small two-owner Manual 301, and genuine two-owner acquired Manual 302. Every transaction restored Mods, Working and protected saves; accepted native cache changes mean profile bytes were not exact. The fourth output cannot satisfy the legacy cumulative one-owner Manual 302 source contract. The already-qualified one-owner owner-seed route instead produces acquired Manual 305, which the current legacy 302/303 numbering pins refuse. The temporary `legacy-manual` prelaunch gate remains; no Manual 303 was written. A narrow explicit one-owner numbering/lineage correction and its full native continuation remain F10 work. F09/F11/doors and private 7E remain open.

The [F10 two-owner Manual 303 consumer checkpoint](docs/CHUNK7-F10-TWO-OWNER-MANUAL303-CONSUMER-2026-10-21.md) closes the downstream acquired-Manual source/authority/reader path offline, with Fast 3/0 and focused tests passing independent review. Its full Manual 303 reader and native continuation await a genuine same-candidate cumulative producer; no native run or final-package acceptance follows. Broader F09/F10/F11, doors and private 7E remain open.

The [F10 cumulative Manual preparation](docs/CHUNK7-F10-CUMULATIVE-MANUAL-PREPARATION-2026-10-21.md) replaces the impossible legacy level-up Auto wait with an exact Manual 302 input and requested native Manual 303 output. Focused offline checks and independent source/reader review pass; a distinct prelaunch guard retains native refusal until a fresh same-candidate source is ready. No cumulative native PASS or final-package acceptance follows. The downstream two-owner Manual 303 intake, broader F09/F10/F11, doors and private 7E remain open.

The [F09 fixture-entitlement checkpoint](docs/CHUNK7-F09-NATIVE-FIXTURE-ENTITLEMENT-2026-10-21.md) adds a reviewed per-level native `CanLevelUp` guard to the old four-level Horse setup helper. Fast passed 3/0; no native stress or six-pair fixture was produced. The retained five-rider/two-Horse source remains insufficient. F09, F10, request-free F11 and private 7E remain open.

[Batch157](docs/CHUNK7-BATCH157-F11-OBSERVER-OFF-COMPARISON-2026-10-21.md) passed one same-DLL, same-suite owned-seed 0/1/2/0 matched-pool comparator with the private Mono profiler absent: game/outer 8/0, zero writes, full guarded restoration, and 26 accepted cache changes. Its median KMC Update means were 0.198/0.238/0.258/0.206 ms; batch156's profiler-enabled means were 0.226/0.269/0.288/0.218 ms. This bounds observed diagnostic overhead in an automation-request route, while request-free normal-player behavior and performance acceptance remain open. The batch156 original outer FAIL and separate reader-only PASS stay distinct.

[Batch156](docs/CHUNK7-BATCH156-F11-NATIVE-ALLOCATION-2026-10-21.md) measured one guarded base-only F11 native scenario on the reviewed candidate: game PASS 8/0, twelve complete matched windows and an in-game positive allocation-callback handshake. The original outer result is **FAIL** from JSON Double versus product Single frame-delta arithmetic. A reviewed reader-only correction preserves both tolerances and passes on the unchanged raw artifact; focused checks 88/0 and full read-only game-result validation pass. The transaction restored Mods/Working/protected saves; 26 accepted cache changes mean profile bytes are not exact. The observed 0/1/2/0 allocation medians are development evidence, while diagnostics-off normal-player controls, F09 six-pair/doors, broader F10 and private 7E remain open.

The F11 private native allocation observer now has a request-only guarded game adapter and strict external reader. Its run-owned DLL remains outside the Mod overlay; the selected two-pair base-only request pins the helper and installed Mono hashes, proves callback delivery in the actual game process before sampling, and records 12×120 Update windows with per-window safety counters. Independent source review found no blocker; focused matched-pool 87/0, quiet-window 28/0, extracted startup 45/0 plus reader 45/0, and Candidate 7/0 pass offline. No F11 game allocation rate or performance PASS is claimed yet. The next action is a single declared disposable native scenario on a fresh exact candidate, after clean-source packaging, suite admission and guarded WhatIf. The previous offline shim record remains valid only for its isolated host.

The [F11 measurement design](docs/CHUNK7-F11-MEASUREMENT-DESIGN-2026-10-19.md) now records a private native process-lifetime shim, built with portable Zig and tested against the exact installed Mono in an isolated host. Wrong-path, duplicate-start and foreign-thread calls refused; two same-thread windows counted 100/7,400 and 20/1,480 objects/bytes, while a concurrent foreign producer exercised bounded callback drain and a stable closed snapshot. Independent review admits **offline ABI evidence only**. Request-only game loading, exact guarded staging/hash pins, Unity Update-thread bracketing, run-root output and process-exit restoration still require implementation and review. The earlier two-profiler managed-host discriminator remains separate; managed game integration was not admitted. Allocation rate, normal-player controls, conditional six-pair stress and private 7E stay **IN PROGRESS**. The [standing mission table](planning/CHUNK7-COMPLETION-MISSION.md) incorporates batch155 acquisition/mounted-cold evidence and retained six-pair source inventory. No product DLL, package, game, live Mods, save or profile changed for these offline probes.

[Batch155](docs/CHUNK7-BATCH155-ACQUIRED-MOUNTED-COLD-2026-10-21.md) passes a genuine two-pair mounted Manual 5/5 and independent-copy separate-process cold load 3/3 on preview.94, descended from batch154's paid Small acquired Manual. A first WhatIf refused an empty-string owner checkpoint before launch; a strict reader correction passed focused 288/0, Fast 3/0 and independent review, then both guarded native runs passed. Both restored Mods/Working/protected saves; accepted cache changes mean profile bytes were not exact. This is development evidence for the exact acquired lineage, while conditional six-pair, broader F10, F11 and private 7E remain open.

[Batch154](docs/CHUNK7-BATCH154-POSTFIELD-MANUAL-PASS-2026-10-21.md) passes a fresh same-package assisted producer 7/7, separate cold 4/4, paid Small/Horse field Manual 7/7 and corrected post-field two-owner acquired Manual 8/8. A distinct first post-field attempt failed during native Steamworks startup before its atomic result; that original FAIL and batches 151–153 remain separate. All five guarded transactions restored Mods/Working/protected saves, with accepted profile cache changes. The new acquired Manual 302 is save-only; mounted two-pair cold, conditional six-pair stress, F11 and exact-package 7E remain open under the standing mission.

[Batch153](docs/CHUNK7-BATCH153-POSTFIELD-RELATIONSHIP-DIAGNOSIS-2026-10-21.md) passes a fresh same-package assisted producer 7/7, separate cold 4/4 and paid Small Manual 7/7. Its derivative remains an original **FAIL** after native load, but the bounded snapshot isolates the refusal to a Json.NET live-versus-parsed relationship comparison: resources and progression match, and the expected/current reciprocal rows serialize identically. A narrow reviewed ordinal wire comparison and Fast 3/0 pass offline; new-candidate native verification remains. All four transactions restored Mods/Working/protected saves; accepted cache changes mean profile bytes were not exact. No acquired output or mounted cold continuation exists. The standing mission, selected 7C/7D and private 7E remain open.

[Batch152](docs/CHUNK7-BATCH152-POSTFIELD-COLD-FAIL-2026-10-21.md) passes the corrected-bootstrap assisted producer 7/7, separate cold 4/4 and paid Small Manual 7/7 on one preview.94 package. Its post-field derivative is a new original **FAIL** after native PostLoad: the strict prior-pair snapshot differs in a category the original artifact did not isolate. The prior pair's recorded action resources match; a reviewed failure-only diagnostic now captures progression and relationship differences without relaxing the refusal, Fast 3/0. All four transactions restored Mods/Working/protected saves; accepted cache changes mean profile bytes were not exact. No acquired output or fifth mounted cold exists. The standing mission, selected 7C/7D and private 7E remain open.

[Batch151](docs/CHUNK7-BATCH151-POSTFIELD-BOOTSTRAP-FAIL-2026-10-21.md) has same-candidate producer **PASS 7/7**, separate cold **PASS 4/4**, and genuine paid Small Manual **PASS 7/7**. Its post-field acquired derivative is an original **FAIL** before native load because bootstrap still treated every two-owner source as Auto. A reviewed one-line correction uses the request's validated native load type; focused checks pass and a fresh candidate/native rerun remains required. No mounted cold consumer ran. All four transactions restored Mods/Working/protected saves; accepted profile-cache changes mean profile bytes were not exact. The standing mission and selected 7C/7D/private 7E remain active.

[Post-field Manual engineering checkpoint](docs/CHUNK7-POST-FIELD-MANUAL-DERIVATIVE-2026-10-21.md): the acquired-Manual two-owner consumer now has a reviewed closed route from a paid field Manual 301, with the saved four-actor roster, no second selector, immutable-source authority and strict reader. Focused request/authority/reader checks pass; same-candidate native derivative and separate mounted cold remain unqualified. The standing mission and selected 7C/7D/private 7E remain active.

[Batch150](docs/CHUNK7-BATCH150-PAID-SMALL-NATIVE-PASS-2026-10-21.md) closes the paid Small **setup** pilot on a fresh reviewed preview.94 candidate: assisted producer **PASS 7/7**, independent cold **PASS 4/4**, genuine paid Halfling Ranger/Horse field Manual **PASS 7/7** in both game and outer evidence. All three guarded transactions restored Mods, Working, protected saves and profile bytes exactly. The Manual has two native owner/Horse relationships but no KMC mounted records; the acquired-Manual two-owner consumer still needs an exact post-field Manual source mode and separate mounted cold continuation. The original b145/b148/b149 failures remain distinct. The standing mission, F09 breadth, progression, F11 and final 7E remain **IN PROGRESS**.

[Batch149](docs/CHUNK7-BATCH149-PAID-SMALL-2026-10-21.md) records fresh same-package assisted producer **PASS 7/7** and cold **PASS 4/4**. Paid Small reached the genuine native Manual 301 save and game **PASS 7/0**, while the original outer evidence result is **FAIL**: the assisted path incorrectly required an Auto observer. Reader-only continuation found the game's measured body serialized as a reference marker with no required fields. A narrow reader branch and explicit body projection are under focused review/test; a fresh candidate is needed for native evidence. The paid Manual is a native setup archive with no KMC mounted records, so two-pair cold and the rest of the standing mission remain open.

[Batches 146–148](docs/CHUNK7-BATCH146-148-ASSISTED-SMALL-2026-10-21.md) establish fresh same-package assisted Manual producer and independent cold PASSes, then retain b148's paid Small **FAIL** after native paid Halfling Ranger/Horse acquisition: exact native Manual slot 301 was rejected by the save observer before a field save. The reviewed closed-slot correction passes focused tests and Fast, but has not run in the game. All six native transactions restored Mods, Working and protected saves; approved achievement/analytics cache changes mean profile bytes were not exact. The standing mission remains **IN PROGRESS**: paid field Manual, separate cold continuation, F09 breadth, progression, doors, F11 and final 7E remain open.

[Batch145](docs/CHUNK7-BATCH145-ASSISTED-CUMULATIVE-PILOT-2026-10-21.md) establishes a fresh same-candidate assisted producer **PASS 7/7** and separate cold **PASS 4/4** on preview.94, followed by an original paid Small acquisition **FAIL** in native CharGen class-description UI. All three transactions restored exactly with zero cache exceptions. The narrow headless class-selection correction passed Fast 3/0 and independent review; a new candidate/native verification remains open. The downstream two-owner/Small consumer still expects pre-field Auto ancestry and needs a distinct post-field Manual contract. The standing engineering mission remains **IN PROGRESS**; no two-pair, six-pair or final 7E verdict follows from this pilot.

The batch143 assisted Manual gives F09 a new bounded fixture lead: one genuine
Ranger/Horse pair, five unused native character slots, party level 5, quoted
15,000 default-hire XP and exactly one 12,500 paid-hire cost in money. A first
nonfree native hire/Horse pilot on that immutable source needs its own admitted
scenario and evidence; no six-pair stress or cap expansion is claimed.

[Batches 143–144](docs/CHUNK7-BATCH143-144-ASSISTED-MANUAL-COLD-2026-10-21.md)
record a genuine assisted new-campaign final Manual and separate-process cold
game PASS on one frozen preview.94 package. The cold outer reader's original
FAIL remains separate from its reviewed reader-only PASS: synchronous
`Player.PostLoad` saw null `GameId` before the settled world carried the exact
saved campaign ID. Both guarded runs restored Mods, protected saves and profile
bytes exactly. This advances disclosed fixture preparation, not ordinary
acquisition/progression or final-package 7E.

[Batch142](docs/CHUNK7-BATCH142-ASSISTED-DIALOG-IN-CUTSCENE-2026-10-21.md)
retains another distinct assisted native **FAIL**. The prior fix waited on a
cutscene lock even during an ordinary active PC Dialog, so the producer made
one click and hit its unchanged progress bound. The guarded transaction
restored Mods, protected saves and profile bytes exactly. The narrower
predicate retains Cutscene-mode waiting while admitting Dialog-in-cutscene
through existing dialog guards; focused core passed 251/0, Candidate 6/0 and
independent review. Native producer and independent cold consumer remain open.

[Batch141](docs/CHUNK7-BATCH141-ASSISTED-CUTSCENE-MODE-2026-10-21.md)
retains a third distinct assisted native **FAIL**: the terminal Exit reached
native `Cutscene` mode, but its cutscene-lock flag dropped one frame before the
mode changed. The driver incorrectly refused that mode/flag gap. The guarded
run restored Mods, protected saves and profile bytes exactly. A scoped wait on
the native mode or lock passes focused core 250/0, Candidate 6/0 and independent
review; native verification remains open. No final Manual or independent cold
consumer exists.

[Batch140](docs/CHUNK7-BATCH140-ASSISTED-STOP-SETTLEMENT-2026-10-21.md)
keeps the second assisted native run FAIL but distinguishes its cause: the
installed dialog controller retained `m_DialogStopScheduled=true` after the
terminal Exit, while Default mode, cleared dialog/cue, and ready main view were
observed. The run restored Mods, protected saves and profile bytes exactly. A
narrow fixture correction recognizes that completed native stop without writing
the flag or relaxing pending-start/cue/active-dialog guards; focused tests pass
249/0. Native verification, the final Manual and independent cold consumer
remain open.

[Batch139](docs/CHUNK7-BATCH139-ASSISTED-STORY-TRANSITION-DIAGNOSTIC-2026-10-21.md)
retains the first repaired assisted native producer as FAIL: initial new-game
Auto and 37 real story clicks occurred, but after the terminal Exit the game
returned to Default while its combined native transition-pending flag remained
true for 30 seconds. The guarded transaction restored Mods, protected saves and
profile bytes exactly. A reviewed read-only diagnostic now distinguishes the
three native sources of that flag on the next justified run; focused tests pass
248/0, with no new native execution yet. F10 new-character acquisition and its
final Manual/cold chain remain open under the standing completion mission.

[Batch138](docs/CHUNK7-BATCH138-ASSISTED-MANUAL-COMPLETION-2026-10-21.md)
replaces the assisted producer's impossible level-up Auto wait with its
guarded final native Manual after verified Ranger/Horse choices. The exact
two-write built evidence and generated archive path pass focused reader checks;
the historical three-write generated path remains separately readable. Source
validation, wiring, stale-family guard, source and authority checks pass.
Independent review found no active write-authority blocker. At that offline
checkpoint the repaired route had no native execution; batch139 records its
first native FAIL, so F10 fresh-campaign setup and its separate cold consumer
remain unqualified. The standing
[completion mission](planning/CHUNK7-COMPLETION-MISSION.md), F09/F11, ordinary
door and 7E work remain open.

[Batch137](docs/CHUNK7-BATCH137-F11-STARTUP-AND-SIX-PAIR-FEASIBILITY-2026-10-21.md)
passed one guarded, base-only no-save native startup control on a clean-HEAD
package with the same preview.94 DLL bytes: shipped diagnostic overlay off,
no overlay object or startup actions, and zero save/load requests. Its
transaction restored Mods, protected saves and profile bytes exactly. This
does not establish request-free ordinary-world controls or allocation rate.
The retained F09 source has five field riders and two native Horse pets; the
missing six-pair fixture and production two-pair cap remain open.

The [batches 135–136 F09 presentation record](docs/CHUNK7-BATCH135-136-F09-PRESENTATION-2026-10-21.md)
adds a preview.94 game **PASS 11/11** for the selected mounted Halfling/Horse
scroll-to-Horse and potion-self item presentation, with native spending,
healing, pose, Dismount and cleanup. Its original outer result remains **FAIL**
on a time-limited native cooldown flag read at terminal time; a reviewed
corrected reader separately passes on the frozen b136 artifact. Batch135's
earlier game/outer FAIL remains separate. Both transactions restored exactly.
F09 conditional six-pair stress, broader F10, F11, ordinary door and selected
7E remain open. See the [active resume](AUTONOMOUS-RESUME.md) and
[standing mission](planning/CHUNK7-COMPLETION-MISSION.md).

The [batches 131–133 F10 chain](docs/CHUNK7-BATCH131-133-F10-ACQUIRED-COLD-2026-10-21.md)
passed on one exact preview.92 package: owner-seed cold source, original Ranger
Horse acquisition with a genuine field Manual, and a separate acquired Manual
cold consumer preserving the same Ranger/Horse pair. All three game/outer
results passed and restored Mods, saves and profile bytes exactly. This closes
the specific existing-campaign Ranger acquisition and saved-continuation route
in development. General/new-character acquisition, other progression breadth,
F09, F11, ordinary doors and selected final 7E remain open. The original
batch127 outer FAIL remains separate. See the [active resume](AUTONOMOUS-RESUME.md)
and [standing mission](planning/CHUNK7-COMPLETION-MISSION.md); engineering
completion is not claimed.

The [batches 128–129 F10 record](docs/CHUNK7-BATCH128-129-F10-OWNER-FIELD-2026-10-21.md)
establishes a same-candidate owner-seed cold PASS and a genuine Ranger Horse
acquisition/field Manual producer PASS, both native and outer, on preview.92
with exact restoration. The acquired Manual cold consumer has not run. Its
read-only source intake exposed a narrow external-reader mismatch with the
guarded owner's empty persistence-case field; that reader is corrected,
independently reviewed and passes synthetic 30/0 and real source 3/0. A new
clean package/commit must bind a same-candidate cold source and producer
before the separate consumer. The original batch127 outer FAIL stays intact.
The [standing mission](planning/CHUNK7-COMPLETION-MISSION.md) and selected 7E
remain active; engineering completion is not claimed.

The [batches 126–127 F10 record](docs/CHUNK7-BATCH126-127-F10-OWNER-FIELD-2026-10-21.md)
records a preview.92 owner-seed cold PASS followed by native Ranger Horse
acquisition and genuine field Manual save PASS. Batch127's original outer
result remains FAIL: its reader expected a populated GameId during synchronous
PostLoad and retired native round-hook metadata. A corrected external reader
passes separately on the same frozen artifact; it cannot certify the original
outer run as a producer source. The reader and tests were independently
reviewed, with focused producer 118/0 and acquired 150/0. Both native
transactions restored Mods, saves and profile bytes exactly. The next exact
candidate needs a same-candidate cold source, a complete outer producer PASS,
then an independent acquired Manual cold consumer. The historical Auto waits
remain refused; see the [stale-family contract](docs/CHUNK7-STALE-LEVELUP-AUTOSAVE-FAMILIES.md)
and [active resume](AUTONOMOUS-RESUME.md). Automatic approval review still
blocks the guarded push helper's WhatIf, so draft PR #5 is behind these local
checkpoints. Engineering completion is not claimed.

[Batch124](docs/CHUNK7-BATCH124-THORN-RIVER-DOOR-FIXTURE-2026-10-21.md)
passed one guarded preview.89 Thorn River Camp fixture run, game and outer
5/0, with a distinct genuine Manual and exact restoration. Its bounded
after-remount inventory found zero `StandardDoor` candidates; no door was
clicked and ordinary-door qualification remains open. This exhausts the
revealed field destinations on the admitted owner-seed source. The standing
[completion mission](planning/CHUNK7-COMPLETION-MISSION.md) remains active;
F09/F10/F11 and the selected 7E rehearsal still require work.

[Batch123](docs/CHUNK7-BATCH123-TOMB-DOOR-FIXTURE-DEVELOPMENT-2026-10-21.md)
passed one guarded preview.88 Old Cyclops Tomb fixture run, game and outer
5/0, with one genuine Manual and exact restoration. Its sole secret door was
closed, beyond the scan radius and ineligible; ordinary doors remain open.
The actual player reports `Debug.isDebugBuild=false`, so Unity Recorder cannot
measure F11 allocations here. The standing mission remains active.

[Batch122](docs/CHUNK7-BATCH122-F11-MANAGED-MONO-PROBE-2026-10-21.md)
preserves a failed disposable managed-Mono embedding probe: domain creation
succeeded, then the exact installed Mono DLL faulted before callback
registration. Independent review found no safe KMC-managed global-profiler
installation on this evidence. The next lower-risk F11 discriminator is a
bounded `GC.Alloc` Unity Recorder validity and positive-control observation;
the exact installed API is present, but marker availability in this player is
unknown. The two-pair diagnostic now records `Debug.isDebugBuild` after
admission, for the next independently required native case. Focused Fast
passed 4/0; no game, live files or package changed. F11 remains open.

[Batch121](docs/CHUNK7-BATCH121-F11-INSTALLED-MONO-ALLOCATION-PROBE-2026-10-21.md)
proved 100/100 allocation callbacks from Kingmaker's exact installed Mono
DLL in a disposable host. The explicit Mono cleanup variant exited 1 after
callback removal; a process-lifetime variant exited 0 and a second counted
7,400 managed object bytes. No game or product
binary changed. F11 still needs a reviewed opt-in native observation route,
bounded KMC Update allocation data, normal-player controls and final-package
qualification.

[Batch120](docs/CHUNK7-BATCH120-MAMMOTH-FIELD-READER-2026-10-21.md)
ran four declared preview.87 scenarios. The paid Medium Horse mounted Manual,
its separate cold load and paid Mammoth replacement Manual passed. The Mammoth
field game passed Mount, a 1.975 m player-created move, Dismount, exact detached
custody and next-frame natural retirement; its original outer result remains
**FAIL** because the reader required an owner generation before the initial
Mount. The corrected external reader passed the unchanged game evidence 39/0,
with focused checks 23/0. Every transaction restored exactly. F09 breadth,
F11 and final-package 7E remain open; pending-only migration has prior
development evidence subject to final-package reuse. Batch119 below is
historical on its own inputs.

The current preview.87 offline quiet/defaults and diagnostics-off checks pass
28/0 and 29/0; these do not measure allocation rate or native normal-player
behavior. The pinned installed Mono profiler export inventory supports a
isolated investigation; process-global ownership and safe teardown remain
unproven. Old Cyclops Tomb is a revealed/unvisited owner-seed
destination candidate for a new door fixture; no door or safe scene has been
observed there. The two inspected scenes remain local zero-door results.

[Batch119](docs/CHUNK7-BATCH119-MAMMOTH-DISMOUNT-CUSTODY-2026-10-21.md)
completed a four-slot preview.86 diagnostic chain. Its field result remains
**FAIL 3/1**, but the new snapshot proves successful native Dismount dispatch,
no active Mammoth owner, intact pet/master references and a stable Horse pair.
The old assertion counted a detached owner before natural retirement. Reviewed
preview.87 source now checks exact detached rider/Mammoth custody and Unmounted
boundary, then waits for natural retirement before PASS. Fast and focused reader
checks pass; a corrected-candidate native result is pending. Batch118 below
retains its original preview.85 FAIL.

[Batch118](docs/CHUNK7-BATCH118-MEDIUM-MAMMOTH-FIELD-DEVELOPMENT-2026-10-21.md)
ran five declared preview.85 native scenarios. The paid Medium Horse Manual,
separate cold, paid Mammoth replacement Manual, and separate Mammoth cold passed.
The field case cold-loaded and mounted the Mammoth, then moved it 1.953 m through
a successful player-created native click, but **FAILed** the final Dismount
release assertion after its shell settled. Every transaction restored Mods,
Working, protected saves and exact Howie profile bytes. Preview.86 source now
adds one read-only terminal dispatch/owner snapshot for a distinguishing run;
it is not yet a new native result. The standing mission continues; F09 field,
schema migration, F11 and 7E remain open. Batch117 below retains its identity.

[Batch117](docs/CHUNK7-BATCH117-MEDIUM-MAMMOTH-COLD-2026-10-21.md)
passed the four-scenario same-candidate preview.84 Medium chain: mounted Horse
Manual 5/0, separate cold 3/0, genuine paid Horse-to-Mammoth respec/Manual
3/0, and separate Mammoth cold 3/0. The cold consumer retained exact Ranger 10,
rank 7, Large Mammoth, retired-Horse absence and the independent mounted Horse
owner across five frames, with zero save writes. Each guarded transaction
restored Mods, Working, protected saves and exact profile bytes with zero cache
exceptions. F10 replacement/Manual/cold has development PASS; F09 mounted
Mammoth breadth, other progression/removal, F11 and selected private 7E remain
open. The batch116 paragraph below is historical.

[Batch116](docs/CHUNK7-BATCH116-MEDIUM-MAMMOTH-PRODUCER-2026-10-21.md)
passed three guarded native scenarios on preview.83: paid Medium Horse mounted
Manual 5/0, separate cold load 3/0, and genuine rider-B Horse-to-Mammoth
native respec/Manual 3/0. The producer reached Ranger 10, companion rank 7,
and a Large Mammoth while retaining the independent control Horse. All three
transactions restored Mods, saves and profile bytes exactly. F10's separate
Mammoth Manual cold load remains open; current cold-consumer implementation
has focused offline checks and is awaiting a new candidate/native run. F09,
F11 and private 7E remain open. The batch115 paragraph below is history.

[Batch115](docs/CHUNK7-BATCH115-MEDIUM-MOUNTED-DEVELOPMENT-2026-10-21.md)
qualified world119's paid two-pair Medium Horse mounted Manual and separate
process cold load, game/outer 5/0 and 3/0. The cold consumer restored the exact
two-pair archive and wrote no save. Both guarded transactions restored current
Mods, Working, protected saves and profile bytes exactly, with no cache
exceptions. Horse-to-Mammoth replacement, F09/F10/F11, progression and private
7E still require their selected engineering evidence. The batch114 paragraph
below is historical fixture provenance.

[Batch114](docs/CHUNK7-BATCH114-MEDIUM-MANUAL-COLD-2026-10-21.md) passed a fresh
world118 paid Medium Human/Horse unmounted Manual 6/0 and its independent-copy,
separate-process cold load 4/0 with seven actors, reciprocal Horses and zero
cold save writes. Both guarded transactions restored Mods, Working, protected
saves and exact profile bytes. This is a qualified fixture lineage; mounted
Medium Horse/Mammoth and genuine Horse-to-Mammoth replacement/Manual/cold are
still required. F09/F10/F11 and selected private 7E remain open. The earlier
batch113 pending-cold description below is historical.

[Batch112 native](docs/CHUNK7-BATCH112-PAID-MEDIUM-FIXTURE-NATIVE-2026-10-21.md)
qualified the paid Medium Human/Horse unmounted Manual producer, game and outer
6/0 with exact transaction restoration on world117. [Batch113](docs/CHUNK7-BATCH113-MEDIUM-COLD-DEVELOPMENT-2026-10-21.md)
adds a reviewed, offline-tested distinct Medium cold-load case; it has not yet
run natively. Neither result qualifies mounted Medium or Horse-to-Mammoth
replacement. F09/F10/F11 and selected private 7E remain open.

[Batch112](docs/CHUNK7-BATCH112-PAID-MEDIUM-FIXTURE-DEVELOPMENT-2026-10-20.md)
adds a reviewed, offline-tested paid Medium Human/Horse unmounted Manual
producer from the qualified clean owner seed. It is fixture setup only; native
producer, distinct Medium cold admission and replacement qualification remain
open. No new game run or transaction has occurred on these source bytes.

Batch111's [fixture discriminator](docs/CHUNK7-BATCH111-MEDIUM-REPLACEMENT-FIXTURE-DISCRIMINATOR-2026-10-20.md)
found no native Doll/preset on the non-main Medium Horse rider in the accepted
owner-seed inventory. Its proposed Horse-to-Mammoth respec case was withdrawn
before declaration or native launch. A Doll-backed paid Medium producer with a
genuine Manual/cold route is the next F10 source task. The historical
level-up-Auto completion path is still blocked. No product or native evidence
changed; F09/F10/F11 and private 7E remain open.

Batch110 verified the batch109 Small equipment-admission change on frozen
world116: offline Candidate 7/0 and one guarded native/outer mounted Small
save PASS 5/0 with its Medium neighbor, genuine Manual, and exact restoration.
It did not measure allocation rate or normal-player diagnostics-off behavior.
See the [batch110 record](docs/CHUNK7-BATCH110-SMALL-ADMISSION-NATIVE-2026-10-20.md).

Batch109 removed the repeated sort/copy in the mounted Small-body equipment
comparison while preserving exact ordinal multiplicity and slot order. Fast
passed 3/0 and independent read-only review found no concrete regression.
There was no new native run or frozen package; F11 measured allocation rate and
native ordinary-controls qualification remain open. See the
[batch109 record](docs/CHUNK7-BATCH109-F11-SMALL-BODY-COMPARISON-2026-10-20.md).

Batch108 qualified the world115 Small Horse later-level route natively: fresh
mounted Manual and cold PASS 5/0 and 3/0, genuine native level/Manual PASS 3/0,
and separate-process two-pair level cold PASS 3/0. The real source resolver
admitted the exact producer before the cold load; all four guarded transactions
restored Mods, saves and profile bytes. This is development evidence on one
candidate, with final-package reuse still unassessed. F09, F10, F11 and private
7E remain in progress; see the [batch108 record](docs/CHUNK7-BATCH108-SMALL-LATER-LEVEL-NATIVE-2026-10-20.md).

Batch107 adds the bounded Small Horse later-level producer and separate two-pair
cold consumer as a **development checkpoint only**. Fast 3/0, focused
producer/source/reader checks and independent read-only review passed; no new
candidate or native game run exists. The real same-candidate four-slot
Manual/cold/level/cold sequence and source-resolver positive admission remain
required; see the [checkpoint](docs/CHUNK7-BATCH107-SMALL-LATER-LEVEL-DEVELOPMENT-2026-10-20.md).

Batch106 added one six-pair **offline domain** custody stress case. Fast passed
3/0 and the world114 product DLL stayed byte-identical. Native six-pair
fixture, lifecycle and performance qualification remain open; see the
[checkpoint](docs/CHUNK7-BATCH106-SIX-PAIR-DOMAIN-CUSTODY-2026-10-20.md).

The [private player guide](docs/CHUNK7-PLAYER-GUIDE.md) reflects the current
single declared Small default male Halfling/Horse profile and its strict
body/gear/form limits. This is a documentation correction, not a new candidate
or native qualification.

## October 20 — batch105 current-candidate diagnostics-off contracts

[Batch105](docs/CHUNK7-BATCH105-F11-DIAGNOSTICS-OFF-CONTRACTS-2026-10-20.md)
passed an actual world114-DLL quiet/defaults contract 28/0 and current-source
cooldown diagnostics-off contract 29/0. The latter stubs external native
boundaries; neither is native diagnostics-off or allocation-rate proof. F09,
F10, F11 native/player controls and private 7E remain open.

## October 20 — batch104 existing owner-seed Ranger acquisition operation

[Batch104](docs/CHUNK7-BATCH104-F10-EXISTING-ACQUISITION-READER-2026-10-20.md)
adds a separate reader-only, source-specific PASS: the existing owner-seed main
Ranger acquired a sourced Horse on one ordinary native level 3-to-4 commit,
with no XP/money assistance. One positive and five altered-input refusals
passed. The batch53 F09 fixture-only verdict and historical Auto-wait blocker
remain unchanged. New-campaign/general existing support, later leveling,
replacement, F09/F11 breadth and private 7E remain open.

## October 20 — batch103 scoped paid-hire operation from retained F09 evidence

[Batch103](docs/CHUNK7-BATCH103-F10-PAID-HIRE-READER-2026-10-20.md)
applies the existing strict native-choice proof to immutable batch53 paid-hire
events. One reader-only operation PASS and five altered-input refusals establish
paid custom-companion Horse acquisition and four native commits within that
fixture. The F09 fixture-only verdict, original failures and later F10 gates
remain unchanged. No DLL or native run was made. New-campaign/existing-character
acquisition, later leveling, replacement, F09/F11 breadth and private 7E are
still open.

## October 20 — batches101–102 F11 phase measurement and allocation-site correction

[Batches101–102](docs/CHUNK7-BATCH101-102-F11-UPDATE-TIMING-2026-10-20.md)
added reviewed KMC Update phase spans, localized the active-pair increase to
`world.UpdatePairs`, and removed one per-frame managed component array using
the installed Unity list overload. Both one-scenario native comparisons passed
game/outer 8/0 on their exact DLLs, with guarded restoration. They do not
establish allocation rate, causal speedup or final-package F11 acceptance.
F09 breadth, F10 acquisition/leveling/replacement, F11 diagnostics-off and
ordinary controls, and the selected private 7E remain open.

## October 20 — batch100 Small retrain separate cold PASS

[Batch100](docs/CHUNK7-BATCH100-SMALL-RETRAIN-COLD-2026-10-20.md) froze pushed
`e4a455e6` as world112. Fresh mounted Small Manual 5/0, its independent cold
3/0, native Small Horse retrain 3/0 and the genuine one-pair retrain Manual's
separate cold consumer 3/0 passed game and outer checks. All four guarded
transactions restored Mods, protected saves, Working and profile bytes. The
new cold check preserves target/new-pet progression and money, the surviving
Medium control pair, exactly-once native debt restoration and five frames
without a replayed preparation. F10 acquisition/leveling, F09 breadth, F11
and selected private 7E remain **IN PROGRESS**.

## October 20 — batch99 Small retrain native PASS; cold consumer pending

[Batch99](docs/CHUNK7-BATCH99-SMALL-RETRAIN-NATIVE-2026-10-20.md) froze pushed
`7dcf2a65` as world111. Same-candidate mounted Small Manual 5/0, separate
cold 3/0, and native Small Horse retrain 3/0 passed game and outer checks.
Retrain wrote a genuine schema-3 Manual with only the preserved Medium control
pair; native pet feature reapply completed its exact reciprocal rebind.
The retrain Manual still requires an independent cold consumer, and F10 plus
the standing completion mission remain open. Earlier retrain failures keep
their original verdicts.

## October 20 — batch98 explains native pet reapply; correction pending game

[Batches97–98](docs/CHUNK7-BATCH97-98-NATIVE-PET-REAPPLY-2026-10-20.md)
record a no-launch cross-candidate WhatIf refusal, then fresh world110 mounted
Small Manual 5/0 and cold 3/0 PASS. Retrain original **outer FAIL** at the
pet-binding observer has no Manual. Its native trace proves same-source
feature reapply temporarily clears rider pet/ID and horse master before
native reattachment of the identical live horse. Exact restoration passed.
A narrowly scoped observer correction passed independent review, actual-method
pet-binding regression 65/0, installed observer checks 24/0, build and offline
Candidate 7/0; native
verification remain. F10 retrain and the standing mission remain open.

## October 20 — batch96 native credits PASS, later pet binding FAIL

[Batch96](docs/CHUNK7-BATCH96-RETRAIN-PET-SOURCE-2026-10-20.md) froze pushed
`fd2be5a9` as world109. Mounted Small Manual 5/0 and separate cold 3/0
passed. Retrain original **outer FAIL** followed accepted native credits of
411 and 0, finish/day/rest, original rebind and one observed new-horse
`AddPet` source. The rider's current pet then differed before another native
`TryUpdatePet` entry. The intervening owner is unresolved. Exact Mods, saves
and profile restoration passed; no game or lock remains. A bounded read-only
pet-boundary snapshot passed independent review, build, actual hook checks
24/0 and Fast 3/0, pending native verification. F10 retrain and the
standing mission remain open.

## October 20 — batch95 identifies both native respec credits

[Batch95](docs/CHUNK7-BATCH95-RETRAIN-TWO-NATIVE-CREDITS-2026-10-20.md)
froze pushed `0d32cf10` as world108. Fresh mounted Small Manual 5/0 and
separate cold 3/0 passed. Retrain original **outer FAIL** has no terminal game
result or Manual, but its native trace identified a 411 starting-gold grant
and a later zero scroll-refund callback. Exact Mods/saves restored; slot 2
has three accepted achievement-cache changes, so profile bytes are not exact
there. The focused two-credit observer/reader correction passed independent
review, build, focused checks and offline Candidate 7/0; fresh native
verification remains. F10 retrain and the standing mission remain open.

## October 20 — batch94 native credit owner unresolved

[Batch94](docs/CHUNK7-BATCH94-RETRAIN-CREDIT-OWNER-2026-10-20.md) froze pushed
`0e104511` as world107. Mounted Small Manual 5/0 and separate cold 3/0 passed.
Native retrain original **outer FAIL** showed `GainMoney(411)` with a zero
callback-time scroll refund. The 411 starting-gold caller remains a hypothesis
until callsite observation. Exact Mods, saves and profile restoration passed.
A bounded read-only first-four-call snapshot passed independent source review,
build, installed hook checks 24/0 and Fast 3/0; it has no native verdict.
F10 retrain and the standing mission remain open.

## October 20 — batch93 retrain credit precondition FAIL

[Batch93](docs/CHUNK7-BATCH93-RETRAIN-CREDIT-PRECONDITION-2026-10-20.md)
froze pushed `255de34e` as world106. Mounted Small Manual 5/0 and separate
cold 3/0 passed. The native retrain original **outer FAIL** executed the new
`GainMoney` hook but rejected its compound callback-time precondition before
accepting a credit or completing rebind/Manual. The exact transaction restored
Mods, protected saves and profile bytes. A bounded diagnostic boundary snapshot
passed independent source review, build, actual patch construction/removal
24/0 and Fast 3/0; it has no native verdict. F10 retrain and the standing
mission remain open.

## October 20 — reviewed native refund observer; game verification pending

Batch92's original retrain outer **FAIL** remains. A read-only hook on the
installed `Player.GainMoney(long)` observes one callback-time refund with exact
player, amount and before/after money; the external reader requires its exact
position before native finish. Independent review passed, focused evidence
93/0, patch installation/removal 24/0 and offline Candidate 7/0 in 6.47
minutes. No game execution of this correction has occurred. See the
[batch92 record](docs/CHUNK7-BATCH92-RETRAIN-REFUND-DISCRIMINATOR-2026-10-20.md).
F10 retrain and the standing mission remain open.

## October 20 — batch92 retrain refund discriminator, original FAIL

[Batch92](docs/CHUNK7-BATCH92-RETRAIN-REFUND-DISCRIMINATOR-2026-10-20.md)
froze pushed `53e7188d` as world105. Fresh mounted Small Manual 5/0 and
separate cold 3/0 passed. The retrain observer recorded a native-phase 411
money credit between its initial zero-refund estimate and the first native
finish entry; its frozen estimate rejected finish and rebind. The original
outer result remains **FAIL**, with no Manual or terminal game result. Mods,
protected saves, Params and preferences were restored; three achievement
cache changes are recorded exceptions. F10 retrain remains open pending an
exact native credit observation and reviewed observer/reader correction.

## October 20 — batch91 native Small retrain observer/cleanup FAIL

[Batch91](docs/CHUNK7-BATCH91-SMALL-RETRAIN-2026-10-20.md) froze pushed
`25e6f771` as world104. Fresh mounted Small Manual and separate cold load
passed 5/0 and 3/0. Native retrain passed the former class-description
failure and reached Total-phase commit, but observer finish/rebind custody
failed; the 900-second outer result is **FAIL**, with no Manual save or
terminal game result. Exact-run combined and profile recovery restored the
installation. A bounded diagnostic correction has focused build/observer/
pet checks; it has no native verdict. F10 retraining and engineering
completion remain open.

## October 20 — batch90 Small native retrain class-phase FAIL

[Batch90](docs/CHUNK7-BATCH90-SMALL-RETRAIN-2026-10-20.md) froze pushed
`44afb539` as world103. Fresh mounted Small Manual and separate cold consumer
passed 5/0 and 3/0. The native retrain **FAIL** (2/1) entered respec and
accepted body choices, but installed `CharacterBuildController.SetClass`
threw in its visual class-description path before any Manual save. A scoped
native class-selection correction passed independent review and focused
build/reader/wiring checks; it has not run in the game. All three guarded
transactions restored Mods and protected saves, and slot 3 restored profile
bytes exactly. Original FAIL retained; F10 retraining and the standing
completion mission remain open.

## October 20 — batch89 Small native dismissal PASS

[Batch89](docs/CHUNK7-BATCH89-SMALL-DISMISSAL-2026-10-20.md) froze pushed
`1ed428a3` as world102. A fresh mounted Small Manual 5/0, separate cold
3/0, and targeted native dismissal 3/0 all passed game and outer checks.
The observer wrote all 20 native body fields, and guarded Mods, saves and
profile restoration passed. This qualifies F10's selected Small dismissal
development path on that candidate. Batch88's original outer FAIL and all
earlier failures remain separate. Acquisition, leveling, retraining,
replacement, F09 breadth, F11 and private 7E remain open.

## October 20 — batch88 native Small dismissal passed; outer evidence FAIL

[Batch88](docs/CHUNK7-BATCH88-SMALL-DISMISSAL-2026-10-20.md) froze pushed
`92be6bb7` as world101. Fresh mounted Small Manual and separate cold load
passed 5/0 and 3/0. Native dismissal reached terminal game **PASS 3/0**:
one genuine `Player.DismissCompanion` entry/exit, target pair retired, and
control pair preserved. The original outer result remains **FAIL 0/1**.
Its reader expected `NewRound` instead of installed `OnNewRound`; the raw
artifact also lacks body fields (`nativeBodyBefore` is only `{"$id":"1"}`),
so no full reader-only PASS is possible. A bounded reader/compiled observer
correction is under review and needs fresh affected native evidence. Guarded
restoration passed. F10, F11 and selected private 7E remain open.

## October 20 — batch87 native dismissal reached, cleanup still FAIL

[Batch87](docs/CHUNK7-BATCH87-SMALL-DISMISSAL-2026-10-20.md) froze pushed
`25547c97` as world100. Fresh mounted Small Manual and separate cold load
passed 5/0 and 3/0. The targeted dismissal run remains **FAIL**: its pending
session records one completed native dismissal and settled target pair, but
diagnostic cleanup refused the declared still-mounted control owner due to
GUID `N` versus `D` string formatting. Exact wrapper and native-startup profile
restoration passed; no game or lock remains. A narrow object-identity cleanup
correction passed independent review, focused checks and offline Candidate;
it needs native verification. Original batch81–87
FAILs remain distinct; F10 and the standing mission are open.

## October 20 — batch86 Small dismissal invariant distinguished

[Batch86](docs/CHUNK7-BATCH86-SMALL-DISMISSAL-2026-10-20.md) froze pushed
`61625d41` as world99. Fresh mounted Small Manual and separate cold load passed
5/0 and 3/0. Native dismissal **FAIL** after the selector-scoped view gate:
controllability or life state changed, then companion invalidation dismounted
the pair. The captured artifact does not establish whether native dismissal
entered. Exact guarded combined/profile recoveries restored Mods, protected
saves, profile, UMM Params and preferences; no game or lock remains. The
original batch81–86 failures are distinct. A failure-only native-flag and
progression-session observation passed independent review; it needs a fresh
native diagnostic before a further product correction. F10, F11 and selected
private 7E remain open under the standing mission.

## October 20 — batch85 Small selector activity diagnosed

[Batch85](docs/CHUNK7-BATCH85-SMALL-SELECTOR-ACTIVITY-2026-10-20.md)
froze preview.82-label world98. Fresh mounted Small Manual and separate cold
load passed 5/0 and 3/0. The native dismissal **FAIL** before removal: the
Small rider's exact view was inactive in Default mode immediately after the
native party selector opened. Exact combined and independently reviewed
one-run profile recovery restored Mods, saves, profile, UMM Params and
preferences; no game or lock remains. A selector-scoped visibility correction
passed independent review and offline Fast 3/0; native verification, dismissal,
F09/F10/F11 and selected private 7E remain open. Batch81–85 FAILs are distinct.

## October 20 — batch84 Small selector invalidation

[Batch84](docs/CHUNK7-BATCH84-SMALL-DISMISSAL-2026-10-20.md) froze the reviewed
native inventory correction as preview.97. Fresh Small mounted Manual and
separate cold load passed game/outer **5/0** and **3/0**. The native dismissal
slot **FAIL** before removal: opening `ShowPartySelection` preceded a generic
mounted view-activity invalidation and `CompanionInvalidated` dismount. Exact
combined/profile recovery restored all live surfaces; no game or lock remains.
A bounded read-only boundary observation passed independent review and Fast.
Original batch81–84
FAILs remain distinct; F10 dismissal and the standing mission remain open.

## October 20 — batch83 native Small dismissal root cause

[Batch83](docs/CHUNK7-BATCH83-SMALL-DISMISSAL-2026-10-20.md) repeated the
genuine mounted Small Manual and separate cold load on preview.96; both
passed 5/0 and 3/0. Dismissal remains **FAIL**: the installed native method
merged an equipped stackable into another stack while enumerating the same
inventory list, then threw before completion. Exact combined and one-run
profile recovery restored all external surfaces. A scoped native-callsite
auto-merge rewrite passes focused 30/0, Fast 3/0, offline Candidate 6/0 and
pinned Harmony12 patch/unpatch 1/0; independent review passed. Native
qualification remains. The
standing engineering mission is open.

## October 20 — batch82 Small dismissal inventory diagnosis

[Batch82](docs/CHUNK7-BATCH82-SMALL-DISMISSAL-2026-10-20.md) repeated the
genuine mounted Small Manual producer and separate cold consumer on preview.95;
both passed game/outer 5/0 and 3/0. The targeted native dismissal **FAIL**
showed `Player.DismissCompanion` throw a collection-modified exception after
removing the rider from the party but before native completion. The guard kept
custody. Exact combined and independently reviewed one-run profile recovery
restored all live surfaces. A bounded inventory observation is under review;
F10 dismissal and the standing engineering mission remain open.

## October 20 — batch81 F10 Small dismissal diagnostic

[Batch81](docs/CHUNK7-BATCH81-SMALL-DISMISSAL-2026-10-20.md) added an
independently reviewed Small/Horse dismissal route on preview.94. Its fresh
mounted Manual and separate cold consumer passed game/outer **5/0** and **3/0**.
The native dismissal retained cleanup after an owned field-roster mismatch;
the original outer run **FAIL** timed out with no terminal game result. The
exact-run guarded combined and profile recoveries restored Mods, protected
saves, Howie profile, startup Params and session preferences; the original
failure remains. The current candidate adds only a bounded first-mismatch
observation to distinguish probe completion from a different native roster.
F10 dismissal, native level/retrain/replacement, F11 and 7E remain open.

## October 20 - batch80 F11 idle registry retirement

[Batch80](docs/CHUNK7-BATCH80-F11-REGISTRY-RETIREMENT-2026-10-20.md)
preserves detached-owner retirement while avoiding its per-frame snapshot and
error allocations when no owner is detached. Independent review and offline
Fast 3/0/Candidate 7/0 passed. Preview.93's single guarded resident-pool
scenario passed game/outer **8/0** with exact profile, Mods and save restoration.
Twelve windows retained zero KMC logs and stable subscribers; the first
zero-pair window had a GC-associated mean outlier and active-pair timing still
exceeds zero-state spread. Managed allocation rate, diagnostics-off, ordinary
controls, F10, additional F09 breadth, ordinary doors and selected 7E remain open.

## October 20 - batch79 F11 frame-snapshot correction and native comparison

[Batch79](docs/CHUNK7-BATCH79-F11-FRAME-SNAPSHOT-2026-10-20.md) removes
specific repeated pair-array and successful-visit error-list allocations from
the KMC frame path. Independent review, actual-DLL focused 8/0, neighboring
71/0, Fast 3/0 and Candidate 7/0 passed. Preview.92's sole guarded native
matched-pool scenario passed game/outer **8/0** with exact profile/Mods/save
restoration. Twelve windows retained zero KMC logs and stable subscribers;
one/two-pair timing remains above the zero-pair baseline. Managed allocation
rate, diagnostics-off, ordinary-player controls and F11 acceptance remain open.

## October 20 - batch78 repeated F11 resident-pool observation

The [batch78 record](docs/CHUNK7-BATCH78-F11-REPEATED-WINDOWS-2026-10-20.md)
adds reviewed three-window-per-state 0/1/2/0 measurement while preserving
batch73's original v1 reader acceptance. Preview.91 returned native/outer
**PASS 8/0**, twelve complete/released windows, zero KMC logs, stable four
owned subscribers and exact profile/Mods/save restoration. Median KMC Update
means were 0.2326/0.2893/0.3035/0.2484 ms; the last zero-pair window
contained a 122.56 ms outlier alongside full GC. Allocation rate,
diagnostics-off controls and performance acceptance remain open.

## October 20 - batch77 Small mounted cold readiness corrected

The [batches 75–77 record](docs/CHUNK7-BATCH75-77-SMALL-COLD-READINESS-2026-10-20.md)
preserves the original batch74/76 cold FAILs and batch75 cache-related outer
FAIL with exact-run recovery. Preview.89 identified a recursive Json.NET walk of
`PoseVector3.Normalized` in the Small presentation identity. Reviewed commit
`c67beb33` replaces that transient identity with a finite 20-field projection;
focused 8/0, independent supplemental 27/0, Fast 3/0 and Candidate 7/0 passed.
Preview.90 batch77 then passed a fresh genuinely mounted Manual producer 5/0
and separate cold consumer 3/0, both game and outer, with exact profile, Mods
and protected-save restoration. The cold reached `all-pair-presentation-restored`.
F10, ordinary doors, F11, conditional stress and selected private 7E remain open.

## October 20 - batch74 Small mounted persistence route reviewed

The [batch74 route](docs/CHUNK7-BATCH74-SMALL-MOUNTED-PERSISTENCE-2026-10-20.md)
uses the immutable qualified seven-actor Small Manual. Independent review,
focused checks and offline Candidate 7/0 passed. Preview.88's genuine mounted
Small Manual producer returned game/outer **PASS 5/0**; the separate cold
consumer returned original game/outer **FAIL** at its stage-0 readiness
deadline, after four native semantic restorations but before presentation.
Both guarded transactions restored exactly. A bounded diagnostic observation
passed independent review and Fast 3/0; Small cold qualification, F10, ordinary doors, F11 and the
selected private 7E remain open.

## October 20 - batch73 F11 matched-pool development measurement

Preview.87 reused preview.86's identical DLL bytes under a current-HEAD
package and fresh qualification suite. Two guarded native scenarios passed and
restored exactly. The first Manual producer had only 0/1-pair windows; the
second zero-write exploration case produced the intended complete 0/1/2/0
matched-pool artifact with stable subscriptions and zero KMC log messages.
Means were 0.3416/0.4237/0.4575/0.3440 ms. This is one diagnostic-included
sequence, not allocation-rate, diagnostics-off or final performance acceptance.
See the [batch73 record](docs/CHUNK7-BATCH73-F11-MATCHED-POOL-2026-10-20.md).

## October 20 - batch72 native Small-profile verification

Preview.86 ran one guarded F09 scenario on the immutable qualified Small
Manual. Game and outer result both **PASS**, 9/9, for the exact ordinary
Halfling/Horse profile and inherited neighboring pair. Mods, protected saves
and profile bytes restored exactly. This verifies native Mount, motion, both
primary orders, Dismount and bounded pose on that input. Ordinary Small
mounted save/cold load, additional presentation, Medium breadth, progression,
doors, measured 7D and 7E remain open. See the
[batch72 record](docs/CHUNK7-BATCH72-SMALL-ORDINARY-NATIVE-2026-10-20.md).

## October 20 - batch71 ordinary Small-profile implementation

The measured default MaleHalfling/Horse declaration is now the sole ordinary
Small profile and a schema-3 data-eligible profile. Native Horse size is pinned
to the measured ordinal 5; other Small bodies, gear and forms remain refused.
The private F09 lease shares that exact declaration. Fast passed 3/0, Candidate
8/0, affected focused checks and independent review passed. This is an
implementation checkpoint: ordinary mounted save/load, additional presentation
actions and final owner visuals remain open. No native scenario was added in
batch71. See the [batch71 record](docs/CHUNK7-BATCH71-SMALL-ADMISSION-2026-10-20.md).

## October 20 - batch70 F09 native Small calibration

Preview.85's declared F09 run returned game **PASS 9/9** for the exact private
MaleHalfling/Horse calibration pair, including Mount, movement, both primary
orders, Dismount, pose and cleanup. Its original outer result remains **FAIL**:
the external reader compared saved numeric `0.0` against observed `0` as
different canonical strings. A narrow reader-only correction passes 36/0
focused checks and separately accepts the immutable native artifact; the DLL
and original result are unchanged. The guarded transaction and profile restored
exactly. Ordinary Small admission and its save/load qualification remain open,
as do progression, measured 7D, ordinary doors and final 7E. See the
[batch70 record](docs/CHUNK7-BATCH70-SMALL-NATIVE-2026-10-20.md).

## October 20 - batch69 F09 combat-entry discriminator

Preview.84's single F09 native run remained **FAIL** at stage 3, but the new
observer identified the exact change: the neighboring main/Horse pair entered
the owned combat encounter, becoming Prepared with initiative 7/5 while its
action debt, reactions and static custody stayed exact. The guarded transaction
and profile restored exactly. A bounded native combat-entry admission and
external-reader witness passed independent review, the focused reader passed
25/0 and offline Candidate passed 7/0. No Small pose or attack qualification
is claimed. See the
[batch69 record](docs/CHUNK7-BATCH69-SMALL-COMBAT-ENTRY-2026-10-20.md) and
[active resume](AUTONOMOUS-RESUME.md).

## October 20 - batches 66–67 genuine DLL-absent cleanup cold load

Batch65's positive mounted cleanup Manual now has a separate-process,
genuine KMC-DLL-absent cold consumer. [Batches 66–67](docs/CHUNK7-BATCH66-67-DLL-ABSENT-COLD-2026-10-20.md)
retain the first outer FAIL caused by a too-narrow UMM settings reader and the
corrected batch67 native/outer PASS 12/0 as distinct results. The game loaded
the reference-free cleanup archive, continued the world and moved the main
character under ordinary native input with no KMC DLL, assembly, UMM entry or
patch owner. Exact Mods, protected saves, Areas and profile restoration passed.
Ordinary doors, Small mounted, progression, measured 7D and final private 7E
remain under the [standing mission](planning/CHUNK7-COMPLETION-MISSION.md).

## October 20 - batch68 Small calibration FAIL and exact restoration

The first native F09 Manual calibration on preview.83 failed an exact
neighboring main/Horse snapshot check before pose qualification. Its observer
retained custody and the runner timed out. The exact owned game process closed;
guarded combined and profile recovery restored Mods, protected saves, Areas,
UMM parameters and PlayerPrefs. The original FAIL remains. A bounded observer
change records the first changed field while keeping the strict PASS check;
independent review and Candidate 6/0 passed, but one distinct native run is
needed. See the
[batch68 record](docs/CHUNK7-BATCH68-SMALL-CALIBRATION-2026-10-20.md).

## October 20 - reviewed private Small declaration

The stock default MaleHalfling/Horse body and proposed pose are now compiled
behind an exact-pair, live-scope F09 diagnostic gate. Normal Small admission and
saved-profile compatibility remain closed. Independent review, Fast 2/0 and
Candidate 6/0 passed. No native mounted calibration has run on these bytes;
the initial pose remains a hypothesis. See the
[Small calibration record](docs/CHUNK7-SMALL-CALIBRATION.md) and active
[resume action](AUTONOMOUS-RESUME.md). The standing mission remains in progress.

## October 20 - F09 Small calibration consumer preparation

The distinct cross-candidate F09 Manual consumer passed focused independent review. It
preserves batch53's qualified `KMC_C7_SMALL_FIXTURE` provenance and adds a
read-only seven-actor Small calibration route with exact cold body, resources
and neighboring pair checks. Offline source/protocol and synthetic reader
checks pass; the actual Small mounted scenario has not run. The compiled
Small catalogue remains empty. See the
[Small calibration record](docs/CHUNK7-SMALL-CALIBRATION.md) and
[standing mission](planning/CHUNK7-COMPLETION-MISSION.md).

## October 20 - batch65 positive mounted cleanup producer PASS

The reviewed preview.81 owner-stock P07 route passed Candidate 7/0 and one
guarded native run from the qualified batch64 stock Mammoth source. It saved a
genuinely mounted Manual and a new reference-free cleanup Manual. The
[batch65 record](docs/CHUNK7-BATCH65-POSITIVE-CLEANUP-2026-10-20.md) pins both
archives and exact restoration. A separate KMC-DLL-absent cold consumer remains
mandatory, alongside ordinary doors, Small mounted, progression, 7D and
selected private 7E under the
[standing mission](planning/CHUNK7-COMPLETION-MISSION.md).

## October 20 - batch64 stock Mammoth fixture producer PASS

The closed, base-only stock Mammoth producer from the qualified clean owner seed
passed independent review, Candidate 8/0 and one native run 5/0. It created
a genuine `KMC_C7_STOCK_MAMMOTH` Manual with a reciprocal rank-seven Large
stock Mammoth, current empty mounted metadata and no permanent KMC Horse
references. [Batch64](docs/CHUNK7-BATCH64-STOCK-MAMMOTH-FIXTURE-2026-10-20.md)
pins package, archive, evidence and exact restoration. This prepares a fixture;
positive mounted cleanup and KMC-DLL-absent cold load remain open under the
[standing mission](planning/CHUNK7-COMPLETION-MISSION.md), along with ordinary
door, Small mounted, progression, 7D and selected private 7E.

## October 20 - batch63 Manticore Lair door inventory

Preview.79's one guarded Manticore Manual cold diagnostic passed native and
outer runtime 4/0. Its bounded read-only inventory found zero ordinary
`StandardDoor` candidates near either mounted actor. [Batch63](docs/CHUNK7-BATCH63-MANTICORE-DOOR-INVENTORY-2026-10-20.md)
pins the clean package, suite, input and exact restoration. Ordinary door
qualification remains open in another scene, along with positive mounted
cleanup/DLL absence, Small, progression, 7D and selected private 7E.

## October 20 - batch62 Spider Cave door inventory

Preview.78 Candidate passed 7/0. The one guarded cold diagnostic had native
game PASS 3/0 and found zero `StandardDoor` candidates in Spider Cave. Its
original outer FAIL came from a same-candidate assumption in the external area
reader; the corrected reader passed on the unchanged artifacts in a separate
reader-only re-evaluation. Exact restoration passed. The
[batch62 record](docs/CHUNK7-BATCH62-SPIDER-DOOR-INVENTORY-2026-10-20.md)
keeps both verdicts and the local door deferral distinct. Ordinary door,
positive removal/DLL absence, Small, progression, 7D and final private 7E
remain open under the [standing mission](planning/CHUNK7-COMPLETION-MISSION.md).

## October 20 - batches 60–61 Spider Cave Auto and cold

Preview.77's genuine Spider Cave AfterEntry Auto producer and independent
cold-load consumer both passed end to end, 5/0 and 3/0, with two mounted
continuation moves in each process. The [batch60–61 record](docs/CHUNK7-BATCH60-61-AREA-AUTO-COLD-2026-10-20.md)
pins the package, archive, suites, evidence and exact transaction restoration.
Earlier batches 57–59 retain their original FAILs. Ordinary doors, positive
cleanup/DLL absence, Small, progression, 7D and final private 7E remain open
under the [standing mission](planning/CHUNK7-COMPLETION-MISSION.md).

## October 20 - batch59 Spider Cave continuation

The [batch59 record](docs/CHUNK7-BATCH59-AREA-CONTINUATION-2026-10-20.md)
preserves preview.76's original native/outer FAIL: the real Auto and corrected
header observer passed, but the generic two-metre eastward continuation stopped
1.111262 m short in Spider Cave. A bounded native-navmesh fixture selection is
in review; its original movement threshold stays intact. Auto producer and
separate cold qualification remain open.

## October 20 - batch58 Spider Cave Auto header

The [batch58 record](docs/CHUNK7-BATCH58-AREA-SAVE-SERIALIZATION-2026-10-20.md)
preserves preview.75's original native/outer FAIL. The genuine Spider Cave
AfterEntry Auto header saw both resumed owners and one mounted snapshot, but
the staged observer applied the ordinary AI-lease invariant inside the native
save scope. Before/after invariants were sound. A phase-specific diagnostic
correction and reader checks are in progress; corrected native Auto and cold
qualification remain open. Batch57's earlier admission FAIL is separate.

## October 20 - batch57 Manticore return Auto admission

The [batch57 record](docs/CHUNK7-BATCH57-MANTICORE-AUTO-ADMISSION-2026-10-20.md)
preserves preview.74's original native/outer FAIL: the engine requested its
AfterEntry Auto while the SaveRoutine iterator was constructed in Manticore;
the new owned-pair authority had reserved only the Spider Cave destination.
No Auto archive was written. A reviewed narrow admission correction accepts
the exact departure request while requiring destination-only storage. Focused
authority 509/0 and Fast 3/0 pass. Corrected native Auto and separate cold
qualification remain; the standing completion mission remains in progress.

## October 20 - batch55/56 Manticore Manual cold

The [batch55/56 record](docs/CHUNK7-BATCH55-56-MANTICORE-MANUAL-COLD-2026-10-20.md)
preserves the preview.72 diagnostic bind-mode native FAIL and the reviewed
preview.73 correction. The latter loaded the genuine batch54 Manticore Manual
in a separate process: outer/game PASS 4/0 with producer graph, two owners,
four slots and full field roster matched. Both guarded transactions restored
Mods, protected saves and Areas. Area Auto/cold and doors remain open; the
[standing mission](planning/CHUNK7-COMPLETION-MISSION.md) still governs.

## October 20 - Manticore Manual source gate

The [read-only source gate](docs/CHUNK7-MANTICORE-MANUAL-SOURCE-GATE-2026-10-20.md)
admits batch54's exact genuine non-capital Manual, binds the Areas receipt to
durable restoration state, and refuses wrong pins and the original batch48
outer FAIL. Focused custody checks pass. It grants no native authority;
area Auto/cold and doors remain unqualified.

## October 20 - batch54 admitted area fixture

The corrected Areas guard and one declared native preview.71 Manticore
producer passed end to end. It wrote a genuine non-capital Manual, SHA-256
`86a5ff4bc18852f48388156f627ddebf1c9047ecf69f73a1b4b3b92b231f1b30`.
The transaction restored Mods, saves, profile bytes and original Areas
exactly. Earlier batch48 outer FAIL and separate recovery remain intact.
See the [batch54 record](docs/CHUNK7-BATCH54-AREA-FIXTURE-PRODUCER-2026-10-20.md).
Area Auto/cold and doors still require their distinct consumers.

## October 20 - area profile filter correction

Batch48's Manticore outer FAIL is now explained by an Areas-isolation
PowerShell filter that discarded all pre-run profile entries. Its reported
empty achievement leaf was present in the exact intake snapshot. The narrow
safety correction and cache-positive/negative regressions pass focused checks
and independent review; no new native area run has occurred. The old FAIL and
exact-run recovery remain separate. See the
[area filter record](docs/CHUNK7-AREA-PROFILE-FILTER-CORRECTION-2026-10-20.md).
The standing engineering mission remains in progress.

## October 20 - batch53 F09 separate cold load

Preview.71 Candidate passed 6/0. A same-candidate F09 producer wrote a genuine
Manual and its separate cold consumer loaded an independent copy with seven
native actors, five characters, reciprocal Horses, exact progression/money,
one load and zero writes. The consumer's original outer result is **FAIL**
on a too-strict external PostLoad `GameId` assertion; its native game/artifact
are PASS. A reviewed reader-only correction passes on the unchanged capture,
while preserving that original FAIL. Both transactions restored Mods, saves
and profile exactly. See the [batch53 record](docs/CHUNK7-BATCH53-F09-SEPARATE-COLD-2026-10-20.md).
Mounted Small calibration and the remaining [standing mission](planning/CHUNK7-COMPLETION-MISSION.md)
gates remain open.

## October 20 - F09 separate cold consumer in reviewable preview.71 source

The [reviewed F09 cold consumer](docs/CHUNK7-F09-SEPARATE-COLD-CONSUMER-2026-10-20.md)
binds a new native Manual producer to the exact candidate and observes a
separate read-only seven-actor cold load. Focused offline checks and Fast pass;
preview.71 has no native result yet. Batch52's preview.70 producer remains
valid evidence only for its own candidate. The [standing completion mission](planning/CHUNK7-COMPLETION-MISSION.md)
continues through all selected 7B–7E engineering gates.

## October 20 - batch52 genuine Small Manual producer

Preview.70 Candidate passed 6/0. One native Small slot passed all six
declared rows and wrote the genuine `KMC_C7_SMALL_FIXTURE` Manual from the
qualified clean seed, paid native Halfling hire and two native Horses. The
saved archive is schema 3 with zero mounted pairs; the separate cold consumer
and mounted Small calibration remain open. Guarded Mods, saves and profile
restored exactly; game and lock are absent. See the
[batch52 record](docs/CHUNK7-BATCH52-SMALL-MANUAL-PRODUCER-2026-10-20.md).

## October 20 - batch51 Small Manual source descriptor

Preview.69 Candidate passed 6/0. Its single native Small slot passed the
unmounted field and live rig row, then failed before Manual numbering because
the producer compared the manager's source list object by reference with the
loader's separate descriptor. The isolated root contained only the seed; the
original result remains **FAIL** with no Manual. Guarded Mods/save restoration
passed, game and lock are absent, and accepted cache changes are separate.
The narrow source-descriptor correction passed independent review and awaits
native verification. See the
[batch51 record](docs/CHUNK7-BATCH51-SMALL-NATIVE-SOURCE-DESCRIPTOR-2026-10-20.md).

## October 20 - batch50 Small field money assertion

Preview.68 Candidate passed 6/0. Its one native Small slot reached the
completed native paid hire and seven-actor field, clearing the batch49 view
failure, then failed a stale final money assertion that omitted the native
411 starting-gold grant. The original batch50 result remains **FAIL**; no
Small Manual exists. Guarded Mods/save restoration passed, game and lock are
absent, and accepted cache changes are recorded separately. A narrow
assertion correction passed focused adapter checks 64/0 and independent
review; native verification remains. See the
[batch50 record](docs/CHUNK7-BATCH50-SMALL-FIELD-MONEY-2026-10-20.md).

## October 20 - batch49 paid remote view owner

Preview.67 Candidate passed 6/0. The one native batch49 diagnostic remains
**FAIL**, but its first retained evidence identifies the changed view as the
paid level-4 hire: it is the sole remote companion, absent from the scene
unit list, and the exact build driver's actor. Main and Horse remained scene
members. The owned game window was closed normally after exact process proof;
the guarded wrapper restored Mods, saves, settings and profile, with no game
or lock remaining. No Small Manual or atomic game result exists. A narrow
F09-only correction is in review: pin the paid actor's view after completed
native CharGen and exact remote-roster checks. The original world and Horse
fences stay active. Independent review and the focused world-scope test (75/0)
passed; Fast passed 3/0. Native verification is pending on preview.68.
See the [batch49 record](docs/CHUNK7-BATCH49-SMALL-VIEW-OWNER-2026-10-20.md).
The standing mission remains active through selected private 7E.

## October 20 - batch48 failure discriminators

Preview.66 Candidate passed 7/0. Batch48 Manticore game PASS 5/0 again
produced a non-capital Manual, but original outer result remains **FAIL**:
the zero-length achievement-cache leaf was absent from the in-memory intake
inventory even though its serialized display matches a pre-existing name.
Exact-run guarded profile recovery restored settings and released its lock;
the Manual is not an admitted area source. Small's first retained **FAIL**
at native paid-hire stage 3 identified a tracked actor with changed view,
`IsInState=True` and no exact `Game.State.Units` membership. The actor's
role is not yet proved. Its owned window was closed normally after exact
process proof, and guarded restoration settled Mods, saves, settings and
profile; no atomic game result or Manual was produced. The
[batch48 record](docs/CHUNK7-BATCH48-NATIVE-DISCRIMINATORS-2026-10-20.md)
pins both original failures and restoration. A bounded F09-only role/roster
observation is in development; no view guard is relaxed. Positive removal,
area Auto/cold, Small, progression, measured 7D and private 7E remain open
under the [standing mission](planning/CHUNK7-COMPLETION-MISSION.md).

## October 20 - batch47 native follow-up and exact restoration

Preview.65 Candidate passed 6/0. Both declared batch47 native slots retain
their **original outer FAILs**. Manticore game PASS 5/0 again produced a
genuine non-capital Manual, but a zero-length achievement-cache postflight
failure still prevents accepting it as an area fixture; separate exact-run
profile recovery passed. Small reached native setup, then its strict captured
view fence failed and cleanup retained custody. The 900-second outer timer
expired before an atomic game result or Manual. The exact owned game process
was closed normally, guarded combined recovery restored Mods and saves, and
reviewed exact-run profile recovery restored startup Params/PlayerPrefs.
Read-only final state has no game or lock and matches the run snapshot on all
protected surfaces. The [batch47 record](docs/CHUNK7-BATCH47-NATIVE-FOLLOWUP-2026-10-20.md)
pins both failures, causal traces and separate restoration. Next checks need
bounded zero-cache path identity and F09 actor/view/stage observations before
another producer attempt. Area Auto/cold, door, Small, positive removal,
progression, measured 7D and private 7E remain open under the
[standing mission](planning/CHUNK7-COMPLETION-MISSION.md).

## October 19 - batch46 native diagnosis and preview.65 correction

Preview.64 Candidate passed 6/0. Batch46 Manticore game PASS 5/0 produced a
genuine non-capital Manual, but its **original outer result remains FAIL**
because a zero-length native achievement-cache leaf lasted beyond the
ten-second exit check. It settled later; exact-run guarded profile recovery
passed separately. The Manual is not yet an accepted area fixture. Batch46
Small **original game and outer results remain FAIL**: native paid CharGen
charged 8,000 and granted 411 class starting gold, producing net debit 7,589.
The [batch46 record](docs/CHUNK7-BATCH46-NATIVE-FOLLOWUP-2026-10-19.md)
pins both failures and restoration. Preview.65 source checks the installed
native first-level grant and once-only charge, while extending only the
read-only zero-cache settlement wait to sixty seconds. Targeted native
verification and all downstream area, Small, removal, progression, measured
7D and selected private 7E gates remain open under the
[standing mission](planning/CHUNK7-COMPLETION-MISSION.md).

## October 19 — batch45 native follow-up and preview.64 diagnosis

Preview.63 Candidate passed 6/0. Batch45 Manticore game PASS 5/0 produced a
genuine non-capital Manual, but its **original outer result remains FAIL** on
a transient zero-length native achievement-cache leaf at exit. Its exact-run
guarded profile recovery passed separately; the fixture Manual is not yet an
accepted source. Batch45 Small reached native paid CharGen completion but its
**original game and outer results remain FAIL** because the actual balance
did not match the earlier quoted cost; the actual debit was not in the trace.
The [batch45 record](docs/CHUNK7-BATCH45-NATIVE-FOLLOWUP-2026-10-19.md) pins
each result and restoration. Preview.64 adds only a bounded read-only cache
settlement check and a failure-only hire-debit observation; it does not relax
the paid assertion. Focused synthetic profile checks passed 59/0 and
independent review passed. Targeted native verification, area Auto/cold,
positive removal, Small calibration, progression, measured 7D and private 7E
remain open under the [standing mission](planning/CHUNK7-COMPLETION-MISSION.md).

## October 19 — batch44 original native FAILs, reviewed preview.63 correction

Preview.62 Candidate passed 6/0. Batch44's two declared native fixtures both
remained **FAIL** after advancing beyond their batch43 failures: Manticore
reached the destination and remounted both pairs, then rejected its vacated
hotbar cells; Small reached paid Halfling CharGen Total, then checked the
8,000 native hire cost before the success callback charged it. No new Manual,
area Auto/cold, door or Small calibration result exists. The
[batch44 record](docs/CHUNK7-BATCH44-FIXTURE-FOLLOWUP-2026-10-19.md) retains
both original results and separate restoration, including the first run's
exact guarded profile recovery and the second run's admitted native cache
changes. The preview.63 source/reader correction passed independent review,
focused checks and Fast 3/0 offline. Targeted native verification, positive
removal, progression, measured 7D and private 7E remain open under the
[standing mission](planning/CHUNK7-COMPLETION-MISSION.md).

## October 19 — batch43 native fixture FAILs and reviewed correction

Preview.61 Candidate passed 6/0. The declared Manticore fixture slot failed
after one accepted Dismount because its stage-90 assertion required immediate
retirement of the first `Detached` owner; no area transfer or Manual occurred.
The declared F09 Small fixture slot completed native main Ranger 3→4/Horse,
then failed in the paid companion's headless race-description UI after native
race selection; no Small Manual occurred. Both original FAILs and exact
restoration are in the [batch43 record](docs/CHUNK7-BATCH43-FIXTURE-CORRECTIONS-2026-10-19.md).
The reviewed preview.62 source correction passed Fast 3/0; affected native
retries and all downstream area, Small and final gates remain open. The
[standing mission](planning/CHUNK7-COMPLETION-MISSION.md) is unchanged.

## October 19 — reviewed group 61 fixture source

The [group 61 checkpoint](docs/CHUNK7-GROUP61-FIXTURE-CANDIDATE-2026-10-19.md)
adds a guarded Manticore area-fixture route, exact live-profile `Areas`
isolation, and a KMC-alone unmounted Halfling/Horse Manual-fixture route.
Independent reviews and focused checks pass. The first Fast source sweep
failed on a missing F09 artifact allowlist; the corrected sweep passes 147/0.
No group 61 native scenario has run and preview.60 remains the last frozen
candidate. Preview.61 packaging, native fixture runs and every downstream
area, Small, positive-removal, progression and performance gate remain open.

## October 19 — owned progression completion correction

The three owned native level/retrain cases now have a closed separate native
Manual checkpoint after actual choice completion, instead of waiting for a
level-up Auto the installed game never requests. Independent source review
corrected a proposed owner-seed source-name mismatch; the selected source
remains the acquired two-pair `KMC_C7_WORLD` chain. Focused checks, the actual
runner readiness gate and Fast passed offline. The acquired producer remains
stale-Auto blocked, and no native F10 result is claimed. See the
[progression checkpoint](docs/CHUNK7-OWNED-PROGRESSION-MANUAL-COMPLETION-2026-10-19.md)
and [standing mission](planning/CHUNK7-COMPLETION-MISSION.md).

## October 19 — batch42 separate migration consumer

Preview.60 Candidate passed 6/0. Its separate current-format cold consumer
used batch41's accepted schema-2 migrated Manual and returned **original game
PASS** 3/0, including native restoration and continuation. The **original
outer runtime result remains FAIL** because an external reader compared a
later same-round native clock sample with saved time as if it were the
immediate restoration probe. The original runtime-result SHA-256 is
`92e1e1516c76699c375730252c79b6c189eb5163c5d14c2335b9dc86d3d9b720`.
Exact immediate probes match; the later sample shows only bounded elapsed
combat clock and actor cooldown progression. A narrow reader-only correction
passed focused 690/0, combat neighbor 165/0 and independent review (SHA-256
`ad07e6324a5a1da17969f5733e1a9db3d54233a756c5824bcb2099528bc100fc`).
The separate immutable-artifact reader-only PASS receipt is SHA-256
`5413bf331e2200486f365f74a59e681307d2a021648384631e4f26bb3098e2be`;
it does not upgrade the original outer FAIL. All guarded surfaces
were restored exactly, with no cache exception. See the
[active resume](AUTONOMOUS-RESUME.md) and
[batch record](docs/CHUNK7-DIRECTIVE4-REENTRY-CORRECTION-2026-10-19.md).
The selected schema-2 restoration/resave/separate-consumer behavior has
**PASS development evidence** on this candidate; exact final-package reuse
or rehearsal is still required before Chunk 7 engineering completion.

## October 19 — batch41 schema-2 migration resave and separate cold result

Preview.59 Candidate passed 6/0. Batch41's fresh same-candidate schema-3
producer passed 4/0, and its metadata-only schema-2 derivative cold/resave
passed 4/0 with a genuine native schema-3 Manual. The separate current-format
cold consumer's **original result remains FAIL** 2/1: its diagnostic parent
comparison refused equal wire JSON because parsed `Double` and typed `Single`
tokens compare unequal in Newtonsoft. Exact archived parent/child JSON graphs
have zero leaf differences and all save identity pins match. A narrow product
correction passed actual-method synthetic 57/0 and replay on these immutable
evidence files 58/0; independent review passed (SHA-256
`9de8e655f3e2750df2294e83ae758992bf39215ada54d75285fa4c7996a71c0c`). A new-candidate cold retry is
pending. All three guarded transactions reported exact restoration. See the
[live resume record](AUTONOMOUS-RESUME.md) and [batch41 evidence](docs/CHUNK7-DIRECTIVE4-REENTRY-CORRECTION-2026-10-19.md).

## October 19 — batch40 schema-2 native clock correction

The [standing Chunk 7 engineering mission](planning/CHUNK7-COMPLETION-MISSION.md)
selects remaining 7B/7C/7D and readiness-gated private 7E. Batch40 preview.58
schema-3 producer passed 4/0. Its schema-2 derivative cold restored and wrote
a genuine Manual, but the original resave result is FAIL before continuation:
the validator refused paired native TB clock advancement within the same retained
allocation. The output is not a qualified cold parent. Bounded production and
reader corrections passed focused 41/0 and 165/0, exact archived replay and
independent review (receipt SHA-256
`716ce21fcca0f8e1a51c8b07e3c8c9bd702d27da3680d95bcfc07ab603a4d84c`).
Freeze a new candidate and rerun its producer, schema-2 resave and dependent
separate cold consumer. Both batch40 transactions report exact restoration.
See the [live resume record](AUTONOMOUS-RESUME.md) and
[diagnosis](docs/CHUNK7-DIRECTIVE4-REENTRY-CORRECTION-2026-10-19.md).

## October 19 - matched pending-debt diagnosis

Preview.53 batch35 loaded the unchanged schema-3 pending-only source and a
metadata-only schema-2 derivative under matched Howie/base-only conditions.
Both restored rider A's 6/3/6 debt, then native re-entry/preparation cleared it
in the retained round. Both game/runtime verdicts are FAIL; the original
preview.45 schema-2 FAIL remains. The Working observer's real Harmony12 patch
installation defect is repaired and covered offline, but no new Working load
was made pending run-specific containment of the seven `Areas` writes observed
in batch34. See the [matched discriminator](docs/CHUNK7-DIRECTIVE4-MATCHED-DEBT-DISCRIMINATOR-2026-10-19.md).

## October 19 - batch34 Working failure distinguished and restored

The Howie-context base-only owner-seed cold control remains PASS. A separate
base-only legacy Working observer failed before loading because its Harmony
patch has an invalid Boolean return type (batch34 slot2, 0 loads/0 saves).
The existing load-only fixture route then requested the same immutable legacy
Working archive once and failed at native `Player.PostLoad`'s missing
main-character cross-scene lookup before its callback (slot3, 1 load/0 saves,
0/1 assertions). Both original game/runtime results remain FAIL; neither
reached P07 positive cleanup. The same-account control rules out a broadly
broken load environment, while historical base-only FAIL versus installed-mods
world PASS makes an archive dependency plausible but unproved.

Slot3's exact guarded recovery passed after independent safety review:
seven new `Areas` files were preserved in its run-owned backup, and the
profile, UMM Params, three startup PlayerPrefs, Mods and protected saves now
match their pre-run snapshots. A separate postcheck found zero game processes
and no active transaction lock. The recovery changes external safety code and
documentation only; frozen preview.52 need not be rebuilt. Schema-2 pending
debt and the visited non-capital area fixture remain unresolved. See
[the execution diagnosis](docs/CHUNK7-DIRECTIVE4-EXECUTION-DIAGNOSIS-2026-10-19.md)
and [the live resume record](AUTONOMOUS-RESUME.md).

## October 19 - owner-authorized execution context and bounded cold control

The owner enabled full access for this continuation. The effective runner is
`DESKTOP-SRJJ623\Howie` (SID ending `-1001`, medium integrity), a different
account from the earlier `CodexSandboxOffline` sandbox runner. This is a new
execution environment, not evidence that the sandbox account gained directory
move permission. Under Howie, the repository's exact transaction recovery
passed WhatIf and actual: batch33's original 80-file Mods tree and protected
saves match their before digests, the lock is absent, and no game is running.
Batch33's original prelaunch result remains FAIL. It never changed a game
profile or registry surface; the generic recovery restored only Mods and saves.

One declared native control, `20261019-c7b-p07-howie-zero-control-b34s1`,
used frozen preview.52 and an immutable copy of the already qualified
zero-mounted checkpoint selection. Scoped WhatIf passed, then the native
base-only cold load passed 3/0 assertions with one load and zero saves under
this account. The transaction restored Mods digest
`b592f4009833072b60604852ad00660a6d236957712b63ae40f28ed1b7f67cda`
and save inventory digest
`df434471b6f2bb3ade60ecccaa524aa7537e17f63db3d0bcce6d5a7eb70561ab`;
Working remained `5eb4e0b4cbd8d60dc879a02ff71aadfde3f517304754857f0cc68d0f9a93f1c6`.
The profile receipt reports exact restoration and zero accepted cache changes.
This proves a qualified KMC save can load in the current account; it does not
certify the distinct legacy Working archive or P07 cleanup. See
[the execution diagnosis](docs/CHUNK7-DIRECTIVE4-EXECUTION-DIAGNOSIS-2026-10-19.md).

## October 19 - batch33 recovery boundary

The owner reported adding the installed-game parent as a managed writable
root, but it is not effective for the current sandbox session. A one-scenario
`fixture-intake` check passed scoped WhatIf and then failed before game launch
at the same Mods directory move as batch32 slot1. The exact original Mods
tree is intact in the run's transaction backup; the live Mods root is absent
and the lock remains active because same-account guarded rollback also got
Access Denied. The Working archive is unchanged and no P07 load result was
obtained. No further native work may start until the exact guarded recovery
completes under an effective authorized context. The owner has been asked to
activate the exact parent writable root for this sandbox session. See
[the updated diagnosis](docs/CHUNK7-DIRECTIVE4-EXECUTION-DIAGNOSIS-2026-10-19.md)
and [the resume record](AUTONOMOUS-RESUME.md) for the run ID and recovery path.
The batch33 original result remains FAIL.

## October 19 - Directive 4 execution and fixture diagnosis

The current pushed reference is `e67dafa4` on the successor branch; PR #5 is
draft. Read-only comparison of batch32 with the October 12 Working controls
found the same Working ZIP and installed Kingmaker/Harmony12 bytes. The old
base-only load failed at `Player.PostLoad`, while the then-installed mod set
reached the world. Batch32's current six-mod set lacks two mods present in
that successful comparison, and other mod/UMM versions changed. The cause of
the missing main-character cross-scene entity remains unresolved; runner
identity changed in batch32 slot2, while the exited game process SID was not
retained. Slot1's two denied directory moves require a narrow managed sandbox
permission for the installed game parent under the qualified sandbox account.
No new native run occurred. See
[the focused diagnosis](docs/CHUNK7-DIRECTIVE4-EXECUTION-DIAGNOSIS-2026-10-19.md).

The schema-2 derivative receipt proves all 22 non-KMC ZIP members unchanged.
The actual batch19 trace restored rider A's saved debt, then native JoinCombat
and Prepare.Tick cleared it in frame 3839 during the saved round-1 allocation.
Whether native re-entry is a lawful new entitlement or a repairable ownership
error remains open; the original cold result stays FAIL. Batch23 area preflight
found its visited Oleg destination capital and issued no transfer/save. All
batch32 surfaces are restored, no lock/game is active, and the existing
preview.52 binary needs no rebuild for this documentation-only checkpoint.

## October 19 - P07 positive cleanup attempt FAIL; exact profile recovery PASS

The pushed reader checkpoint is `7348dace0242ea11f6ad75fc24ad08d0ec29b86c`.
An unchanged preview.52 DLL (SHA-256
`18f619bd28e872b73470944aaf46671887d91d99ff26f80f30a8fd50019a1433`)
was packaged at that head with the removal observer: main package SHA-256
`e87bed650a2d216217e5f38788facb84331179d12bcdbf46125f5571782e28b8`,
observer package `f1cad1de85d727497768e2493380e133fa5bd11f452f20088a47f438c01837ba`.
Candidate PASS 7/0 (3.71 min), package build PASS 11/0, observer packaging
PASS 7/0. Current external-state suite SHA-256
`92c53c00f9606a542c570eb42478f246a0d7bc367dc0692ab2c9076ebdd0e7eb`;
suite and P07 scoped WhatIf passed.

Batch32 slot1 (`20261019-c7b-p07-head7348-b32s1-removal`) failed before game
launch: the sandbox denied a guarded Mods directory move and rollback. The
repository's recovery WhatIf and elevated guarded recovery then restored the
verified original Mods and Working, closed the lock, and left the game absent.
Slot2 (`20261019-c7b-p07-head7348-b32s2-removal`) used the same guarded
launcher with the needed filesystem permission. It made one exact Working load
request and no save request, then native `SaveManager.LoadRoutine` threw
`InvalidOperationException: Sequence contains no matching element`; the load
pipeline ended before its callback. Original game/runtime results remain FAIL.
The preserved stack localizes the throw to `Player.PostLoad`: the main-character
ID had no matching cross-scene entity in that process. The installed assembly
MVID is `07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`; the cause of the missing
entity is unresolved. Slot2 elevation used a different Windows identity from
the sandboxed earlier PASSes, so their Working-load result cannot be transferred.
No positive removal, cleanup archive or genuine DLL-absent load was produced.

Slot2's combined transaction restored Mods, Baseline, Working and protected
saves exactly, but UMM appended an enabled KMC entry and advanced
`LastUpdateCheck`; three observed native PlayerPrefs keys also changed. The
existing settings guard retained the lock rather than overwrite this new
delta. An exact-run, hash-pinned recovery script passed actual-user WhatIf and
independent safety review (receipt SHA-256
`1a164d18016105f19e81664d0e091dae799aa2891f1a648ce2a0af467366bb40`),
then restored the original parameters and preferences and released the lock.
Recovery receipt is private under the slot2 runtime-evidence root. No game or
transaction remains active. The slot2 native failure and original settings
guard FAIL remain recorded; recovery does not change their verdicts. Schema2
TB debt, area Auto/cold, positive removal/no-DLL and doors remain open; PR #5
stays draft with no merge, release, tag or permanent install.

## October 19 - preview.52 selected native breadth and refusal reader correction

Frozen, guarded-pushed preview.52 is `f5fe6fcda39f809b92f2a6cab34bab4a2a108acb`;
the diagnostic package SHA-256 is
`1b191abb67860fadd707d0f8a32e9dc08e2fbc95842178c9ca1496540d48e4da`.
Package build passed 11/0 and Candidate passed 6/0 on that source. The accepted
owner-seed line now has registered two-owner UMM disable/re-enable and permanent
Horse refusal (`batch27`, nine game subscenarios), a zero-mounted TB retained-owner
Manual (`batch28`, four), and its exact separate-process cold load (`batch30`,
three). All three external readers passed. The cold input is the actual
`692e17881b61db0bb1aefb0f20fb5d545540782896934a71ced13f56e7a1a3a4`
archive with zero active pairs and two retained participations. Original
batch24-26 failures remain FAIL. Batch29's BOM-marked private selection failed
WhatIf before launch; its corrected batch30 selection passed.

Malformed and future metadata probes (`batch31`) each passed five in-game
subscenarios: three real load-entry refusals, native continuation and cleanup.
Both original external results remain FAIL because the reader required object
hash labels in 1..128; `RetainedObjectIdentity` emits any nonzero signed Int32
hash and bounds the retained object *count* to 128. The reader-only correction
preserves nonzero/type/collision and full-state conservation checks. Focused
artifact tests passed 53/0; an independent review passed 34/0 additional
checks and re-read both complete immutable native artifacts as PASS. Review
receipt SHA-256 is
`9c9e2829e1f1eceff73dd7c889b9c5cc2ed67170109cff6c57ed9b55101a7a91`.
This is a corrected-reader verdict, not a rewrite of either terminal FAIL or
a fresh product execution. Exact run hashes, quotas and restoration are in
private batch27-31 ledgers under `analysis-cache/chunk7-continuation/`.

All five launched game processes exited, Mods, protected saves and Working
restored. Batch27 had four policy-accepted native profile-cache changes, so
its profile was not byte-identical; the other selected native runs restored
profile bytes exactly. No game process or transaction lock remains. The
worktree holds only the reviewed reader/test correction and current record
updates. Schema2 TB migration still loses saved debt when native Prepare.Tick
rejoins an unprepared actor; area Auto/cold lacks a qualified non-capital
destination; positive reference-free removal/DLL-absent cold and doors remain
open. PR #5 remains draft; no merge, release, tag or permanent install.

## October 19 - preview.51 game PASS, external batch26 FAIL; preview.52 review IN PROGRESS

Frozen preview.51 `b547f5a5` passed package build 11/0 and Candidate 6/0;
package SHA-256 `23de4014cb5b024b039b6957c7ed77008c87d231c16b8ad72278bc170fbb7867`,
DLL SHA-256 `63aed8761d2b85e84fd59367beddc56860179624611a4f85ca514c59e4eb3719`,
suite SHA-256 `530ef836189bfc05a4719e3429558b43ff9d75385e9eebf78be9969d9f217450`.
Private batch26's one native run completed in the game: registered disable,
re-enable and cleanup all emitted PASS, with game artifact SHA-256
`192360ad10285979af16317283c5eb107d2c6f7cd296aa609719c2c9193a7967`.
The external reader then failed: the removal assessment's world facts were
serialized as only `{"$id":"1"}` under Kingmaker's global Newtonsoft defaults,
so its exact safety facts were absent. Runtime result SHA-256
`83ef4b1586d066f2c2cdab0014b2a1e82649eaeba94dede4c7470369a10e3985`
remains FAIL; the game PASS is not external acceptance. The game exited and
Mods, protected saves, Working and profile bytes were restored exactly; no
runtime lock or accepted cache changes remain.

Current preview.52 uses the project's explicit serializer for those world
facts. The reader also corrects two assumptions using the actual two-Horse
native evidence: eight registered facts before disable and one TB Charge
refusal after A's rejected click. A private counterfactual re-read supplied
synthetic world facts to locate further reader failures; its full-reader PASS
is diagnostic only, never evidence acceptance. Focused reader PASS 100/0,
actual-method adapter PASS 50/0 under hostile global JSON defaults, Fast PASS
3/0 (source 147/0). Independent combined review PASS receipt SHA-256
`cd996fa4ed2c423de81a57d55c8da50d70e485aabcccb8ad1b7c01ddee77e443`.
Fresh native execution is pending. Area, schema2 and
remaining Directive 4 cases remain open; PR #5 is draft.

## October 19 - preview.50 batch25 FAIL; preview.51 frame correction IN PROGRESS

Frozen preview.50 `33e729a8` passed package build 11/0 and Candidate 6/0;
package SHA-256 `b7b85448216915467b01152fbd763a4632186ed41e72c45bfcdd1ed2bcf8e68d`,
DLL SHA-256 `e5d69a657047c3648bc3697d76e0308c0daf3557dea1669dd9619c670067c508`,
suite SHA-256 `75a2ae2f9eb2e66a3d922905d8567fcc65dccd39df45ecd132af687b06749b5f`.
Private batch25's one native disable retry remains FAIL: game evidence SHA-256
`d09b82ea4fa2376316c4cc98756b97317a843bebf8aa50196249b86fe193b41c`,
runtime result SHA-256
`c892cdb5c369a694f833e7cb7a675e2cc4482cd446104b2cb73980ec4e97c05d`.
All six native B JoinCombat/Prepare.Tick/Cooldowns.Clear events and correct
6/3/6 → 0 debt were observed. They occurred in the retry Unity frame 4761
after game time advanced 170000 ticks; the later polling snapshot was frame
4762. The preview.50 diagnostic incorrectly required event frame 4762. The
original FAIL is retained. The game exited; Mods, protected saves, Working and
profile bytes were restored exactly, with no accepted cache changes or lock.

Current preview.51 source/reader permits all causal events in one consistent
Unity frame between retry and later snapshot, only after retry game time and
within the later clock bound. The focused reader passes 97/0, actual-method
adapter 49/0, Fast 3/0 (source 147/0). Independent review01 PASS SHA-256
`fd9e6d2ebeee8ba038b1472bb346cb8c631a6238e29aaed35f6e19608fd03a2a`;
it re-evaluated the immutable native progression component and frame/clock
negatives 7/0. Fresh native execution is pending; no preview.51 native credit
exists. Area,
schema2 and the remaining Directive 4 cases stay open; PR #5 remains draft.

## October 19 - preview.49 batch24 FAIL; preview.50 reviewed source checkpoint

Preview.49 `29661db4` was guarded-pushed and frozen: package SHA-256
`dce8ccec3964d7b09064afc5e2fb2ddeb3355d37558fefe159a9aff8a1974cfe`,
Candidate PASS 6/0, suite SHA-256
`4781fcdb46c6067dde015b2cb8cb6e03f52d80944c3b12f41faa4fc1b2266b8a`.
Private batch24's one native registered-disable run remains FAIL (evidence SHA-256
`3388e1ee17eb4fc87dc5d260d773315df439fe866f98071c923555b6340d309d`).
The callback's first local B failure and same-frame retry conserved all native
resources. On the next native TB frame, game time advanced about 24 ms; three
actors' cooldowns decayed, and B rejoined combat. Installed native
`UnitCombatPrepareController.Tick` then cleared B's 6/3/6 debt and prepared it.
The preview.49 diagnostic lacked this causal observation and failed its strict
later-frame assertion. The game and transaction closed; Mods, protected saves
and Working were restored. Six policy-accepted native cache changes mean profile
bytes were not restored exactly. The original FAIL is retained.

The preview.50 source/reader/test change adds exact read-only JoinCombat,
Prepare.Tick and Cooldowns.Clear observations for B and accepts only the
bounded, causally attributed native transition; synchronous disable/retry and
re-enable still require exact conservation. Independent review04 PASS receipt
SHA-256 `2526f755edad4bc1ed393eb56bdccd612c243cf1e32c9bd92d0131d5d621a78e`;
earlier review01–03 FAIL receipts remain. Focused reader PASS 95/0, actual-method
adapter PASS 49/0 and Fast PASS 3/0 (source 147/0). Preview.50 has no native
credit yet. Next: freeze one package/Candidate/suite, guarded WhatIf, then one
affected native disable rerun. Area, schema2 and other Directive 4 cases remain
open. PR #5 is draft; no merge, release, tag or permanent install.

## October 19 - preview.48 batch23 settled; preview.49 diagnostic repair IN PROGRESS

Frozen preview.48 `dc1c1f63` was guarded-pushed; package build PASS 11/0,
Candidate PASS 6/0, and suite `chunk7b-world48-20261019` SHA-256
`b1c9f1a2a4000bdb3d7a70f1ae954caba5a4032b8853e77581a0c5602621a5db`.
The package SHA-256 is
`f6ba06d3114a932f2d4a5cff4f3fc9335a8475c22158c7b485a4183b5ea2343e`.
Private batch23 has three guarded WhatIf PASS and three actual runs: disable
FAIL in 97.17 s before the toggle because native Pause mode was not yet active;
three separate native world loads plus next-frame settlement PASS in 95.46 s;
area Auto FAIL in 86.78 s because Oleg is effectively a capital in this
campaign. The area case refused before transfer or save (zero requests), then
settled cleanup. No area Auto or cold credit is claimed. Exact evidence hashes,
result identities and restoration are in
`analysis-cache/chunk7-continuation/owner-seed-batch23/batch23-ledger.json`.
All three game processes exited, Mods and protected saves were restored, and
no transaction lock remains. The area run retained six policy-accepted native
cache changes; its profile was not byte-for-byte restored and is recorded as
such, rather than described as exact restoration.

Current uncommitted preview.49 corrects the registered-disable diagnostic to
use Kingmaker's actual quiescent TB combat boundary instead of requiring Pause.
Installed `Game.DoStartMode(Pause)` refuses Pause during active TB combat, even
after its setter queues it. The diagnostic still requires unchanged native
actor debt, preparation, clock, round, owners and controls across synchronous
callback and later frame. Its reader now requires TB mode and no native Pause.
The first independent review rejected a wait-for-Pause draft and remains FAIL;
corrected-source independent review PASS receipt SHA-256
`dbdfce391a6c52e32bdba44aa608e106e5d675fb8a312962dc3f18e83bab5ff6`.
Changed-byte native execution is pending. Focused
reader PASS 65/0, actual-method disable adapter PASS 48/0, final Fast PASS 3/0
(source 147/0). An initial Fast invocation used an invalid `-AssemblyTarget`
for Fast and failed before checks; its log is retained. Schema-2 TB debt,
capital-safe area continuity,
positive removal/no-DLL, real refusal/menu and other remaining Directive 4
cases stay open. PR #5 remains draft; no merge, release, tag or permanent install.

## October 19 - preview.47 native batch22 FAIL; preview.48 source IN PROGRESS

Frozen, guarded-pushed preview.47 `10cbba57` passed package build 11/0 and
Candidate 6/0; package SHA-256 `a76cf9dc50b747ad4deb553faf942d8d22ca3c0abcc3e2538a3773f5c6aedf5f`.
All three declared batch22 original results remain FAIL in
`analysis-cache/chunk7-continuation/owner-seed-batch22/batch22-ledger.json`.
Disable's only failed prelude clause was `paused=false` after native RemoveUnit;
the pending survivor passed. Three native loads passed in game, but the external
reader first confused native presentation D-form UniqueIds with compact registry
owner IDs. Its corrected re-evaluation then found a separate final settlement
gap: rider movement components remained listed in the same frame Unity
scheduled their destruction. Area AfterEntry Auto returned to Oleg, restored
pair A and refused pair B's direct-control eligibility; no Auto wrote.
The timeout left restoration blocked until exact-PID normal close and guarded
combined/profile recovery both passed. Owner Mods, saves and profile are restored.

Current uncommitted preview.48 source re-pauses the registered-disable
diagnostic after native removal, distinguishes the two GUID producer surfaces,
waits a native frame before repeated-world settlement, preflights both area
pairs before attaching either, and records/refuses a dynamically capital
destination before area dispatch. Fast PASS 3/0 (source 147/0), repeated reader
106/0, owner-seed repeated 29/0, owned reader 680/0, owner-seed area reader
48/0, cleanup actual-method adapter 99/0, and new area preflight actual-method
adapter 6/0. Reader independent review PASS receipt SHA-256
`7345a0a6ab0fd5f1e2d2e928954fcc1e08262bfd93ab5ac81b8c52a131e2d5f0`;
area/source review PASS receipt SHA-256
`0a312558344c60424e9c15a88be4bdc1f3f4500cb7491d0c018fd1011b966da4`.
Its earlier missing-test FAIL is preserved. No preview.48 native credit exists.

Remaining: changed-byte disable/three-load native retries, capital/area
measurement and area cold if viable, schema2 TB debt repair and third-process
cold, P07 refusal/menu/no-DLL, doors and other Directive 4 cases. PR #5 is
draft. No merge, release, tag or permanent installation is selected.

## October 19 - preview.46 native batch21 FAIL; preview.47 source IN PROGRESS

Frozen, guarded-pushed preview.46 `40439d09` passed Candidate 7/0; package
SHA-256 `3237bd2edc33aa61ce409086bc439abf04733807d1960fbb18f00d824c819571`.
All three bounded batch21 native runs FAIL and remain immutable. Registered
disable stopped before the toggle at a composite diagnostic prelude. Repeated
load correctly replaced Player/area/actors but the observer rejected two
fresh per-fact Horse subscribers; native counts were 8/4/4/1. Area AfterEntry
Auto timed out before an Auto write; the retained cleanup artifact lacks a
native-ready observation, so the blocked native stage is unresolved. Its
game exited normally through guarded exact-PID close; combined transaction
and separate profile recovery passed. No game or lock remains; the owner's
Mods, protected saves, UMM parameters and session preferences were restored
exactly. The original runtime result still records blocked restoration/FAIL,
with the separate recovery receipt proving later restoration.

An uncommitted preview.47 diff repairs the subscriber-lifetime check for both
the older one-Horse and owner-seed two-Horse archives and adds failure-only
disable/area diagnostics. Fast PASS 3/0 and focused repeated reader 104/0,
owned reader 59/0, owner-seed reader 29/0, repeated adapter 76/0, disable
adapter 47/0 and registered reader 64/0. The first repeated-world review FAIL
found the older-fixture regression; corrected-source independent review and the
separate failure-diagnostics review now PASS. Fresh native runs remain. Schema2
TB cold debt loss, area Auto/cold, disable/removal, P07 refusal/menu/no-DLL and doors remain
unaccepted or unrun. PR #5 is draft; Directive 4 remains IN PROGRESS.

## October 19 - preview.45 native results and bounded next repair IN PROGRESS

Preview.45 was guarded-pushed at `8685fb5f` and frozen: package SHA-256
`3a5a4b6cc14939ec8f9dec0cad0af68cbdb2623db238d3805aa7200ddf24c8f7`,
DLL SHA-256 `bc7c5d1e06671a90ee775b7f189917a902d6eb738a685cef53ccdaa89239a834`,
Candidate PASS 7/0. Private owner-seed batches19/20 retain their exact mixed
verdicts. Pending current3 source, schema1 source, schema1 cold/current3 resave,
and schema1 third-process cold PASS. Schema2 cold FAIL: native Prepare.Tick
cleared rider A's saved 6/3/6 debt after native rejoin. Area AfterEntry Auto
FAIL/TIMEOUT before an Auto write; three native loads FAIL/TIMEOUT on the
second in-place Game.LoadGame; registered disable FAIL at a diagnostic prelude
that required exploration idle inside TB combat. Dependent schema2 third cold
and area cold were not run. All external state was restored by completed
wrappers or separate guarded transaction/profile recovery; no game or lock
remains. Original timeout results remain FAIL.

The preview.46 source group is **unfrozen**. Its registered-disable prelude now checks
the exact quiescent TB combat membership (actual-method adapter 47/0). The
repeated-load harness now uses the installed native disposal entry
`Game.LoadGameFromMainMenu`, which disposes the prior area and Player before
delegation while retaining `Game.State`. The scenario, inherited fixture and
reader require the exact retained State and fresh Player, area/scene state and
actors (actual-method adapter 76/0, epoch 95/0, repeated reader 101/0, owned
reader 59/0, owner-seed repeated reader 29/0). A bounded read-only area
readiness observation now also captures the latest native-loading boundary to
identify the next blocked stage if loading itself never settles.
Fast build/components/source validation passed 3/0, with source validation
147/0, on the complete preview.46 diff. The initial incomplete version-triad
check failed 146/1 and remains recorded. No native credit
transfers to these changed bytes. Independent reviews of disable, repeated-load
and area diagnostics passed on exact source hashes; fresh affected native
qualification remains. Schema2 still needs a resource-safe repair, and P07,
refusal/menu, no-DLL, area cold and doors remain unrun. PR #5 stays draft; no
merge, release, tag or permanent installation is selected.

## October 19 - preview.44 native qualification and preview.45 repair IN PROGRESS

The unchanged preview.44 candidate at `8a13f714` passed Candidate 7/7 and was
used for the selected owner-seed continuation. Its current-schema-3 pending
producer passed (batch16 slot1). Actual schema-2 cold load failed after native
restoration when rider A's spent debt was cleared during the next combat window;
the exact native writer is still under investigation (slot2). Schema-1 source
failed in a diagnostic hotbar observer capped below the installed 140-slot native
array (slot4). The area Auto producer was refused before game launch because a
request hashtable was passed to the strict DTO guard (batch17 slot2). The
repeated-load case passed its first world, then failed at the second native
restoration count boundary and timed out during cleanup (batch17 slot4). These
four failed results retain FAIL; dependent consumers were not launched.

After the timed-out process was closed gracefully, guarded transaction recovery
restored Mods and saves, and an independently reviewed exact recovery restored
UMM parameters and two native session preferences. All three transaction states
are restored; no game or runtime lock remains. The original wrapper result still
reports blocked restoration, with a separate PASS recovery receipt. See the
private `analysis-cache/chunk7-continuation/codex-takeover/AUTONOMOUS-RESUME-RUNTIME44.md`
and batch16/17 ledgers for exact hashes and failures.

Preview.45 source composition has focused applied hotbar 187/0, area request
29/0, combat-debt probe 61/0, external debt reader 76/0, composed owned-pair
reader 680/0, repeated-world actual-method adapter 75/0, owned replay epochs
91/0, and repeated-world reader 100/0. The first product build exposed a
diagnostic `Time` namespace typo, corrected to `UnityEngine.Time`; the subsequent
Release build and source validation passed (DLL SHA-256
`bc7c5d1e06671a90ee775b7f189917a902d6eb738a685cef53ccdaa89239a834`).
Earlier failed checks, including the original 679/1 fixture run, remain FAIL.
No preview.45 package, Candidate, or native PASS exists yet. PR #5 remains
draft; no merge, release, tag or installation is authorized.

## October 19 - preview.44 selected source connection IN PROGRESS

Reviewed group44 source is applied after closure HEAD `7c1bcadc`; no preview.44
native PASS yet. Focused C#775/0 plus actual505-source compile PASS; merged
PS501/0; required applied assisted236/0. Earlier FAILs are preserved. Exact
scope, private review/check receipts, substitutions and next native dependencies
are in [the active checkpoint](docs/CHUNK7-CODEX-DIRECTIVE4-CHECKPOINT-2026-10-18.md).
No game/transaction is live. Finish the clean source/package review checkpoint,
guarded-push, one proper candidate build/Candidate and suite, then selected
migration/area/disable/refusal/removal batches. Do not stop at this source commit.
No merge/release/tag/permanent installation, full7E or HUMAN PLAY is selected.

## October 19 - preview.43 native batches settled; Directive 4 IN PROGRESS

Frozen source `3cc8f0b051d1a5ff15879489722efa2eec8c1f45` is guarded-pushed on
`codex/mounted-combat-chunk7-successor`; PR #5 remains draft. Preview.43 was
built once and reused for Candidate PASS 6/6 (224.1519907 s). Package SHA-256
`9e1b6bdc09c592c8ca7585d7c4137f1bb4b213adb43606d0978064a972d98e3e`,
DLL `bb2a65d3d2afd8e5379a54e0a1db1978145ef2c2ede39ca517a236bd24fa26e4`,
MVID `fd6820e0-d020-4311-a139-add433a38775`.

| Native run suffix | Result | Seconds | Finding |
| --- | --- | ---: | --- |
| b14s1-worker | PASS | 96.4207832 | Selected-A cancel/drain and ordinary replacement completed. |
| b14s2-barrier | PASS | 94.3399028 | Nonselected B unsettled Mount blocked save globally; settled native write passed. |
| b14s3-pendingsave | FAIL | 95.2917790 | Historical writer correctly preserved loaded schema3; source fixture needs current3 write plus metadata-only schema2 derivative. |
| b15s1-schema1save | FAIL | 88.3024618 | Diagnostic observer cast a null unrelated actor hotbar collection before writing. |

Full run prefix: `20261019-c7b-world43-ownerseed-`. All four actual runs have
five restoration flags true, all three transactions restored, exact profile
restoration, no game and no lock. Both failed producers and all earlier FAILs
remain immutable. Their dependent cold loads were never launched. Batches14/15
are FAIL overall; the two P07 slots also remain unrun pending current-format
observer connection. Ledgers: `analysis-cache/chunk7-continuation/owner-seed-batch14/`
and `owner-seed-batch15/`.

Worker evidence SHA `cde1aea0547859f2f32126e413bd5d94254e3f886d59000cb5e2e957d3e8c37d`;
barrier `fb9aa27b1f0d322d54fcd8bb177526d26c94465722f16b71bfe6f645d805cc66`;
pending FAIL `fd954c29ff48518bf75e37ebefd62dc9b564ced7ba94474c0ae9df3fcc7ff135`;
schema1 FAIL `f0b8eb0e02c63d1b8f0579d1c99379d8a4c0494141e9eb2f25fde5b701888b28`.
Accepted native output hashes: worker `7ffac650b55429bb27a6df22d818e8c54496f126441fe12076f9ed587b174572`,
barrier `b17267eefa4b6275e5daa2f1bb2c1df54a5546e67f9b2a85df13067fc34e1176`.

Preview.43 execution freeze is closed. Next source group is IN PROGRESS:
null-aware hotbar identity observation; current pending producer and faithful
historical schema2 derivative; controlled visited-area AfterEntry Auto and its
actual cold load; representative repeated load; registered two-owner disable,
permanent Horse refusal, current positive cleanup and genuine DLL-absent cold;
real native malformed/future refusal and save-menu stability. Private draft
checks do not establish native acceptance. Review the composed source before
one new clean candidate build, Candidate and scoped preflight/native batches.
Do not weaken future-data preservation, load/write quotas or restoration guards.
No merge, release, tag, permanent installation, full7E or HUMAN PLAY is selected.
The prior preview.41 TB and zero save/cold PASS retains its original inputs.

## October 18 — preview.42 batch 13 settled; preview.43 source IN PROGRESS

Published source `b9631449357b2ef191256ded54d21c975c12cd64` on the successor branch.
One preview.42 package build was reused for Candidate **PASS 6/6** in
221.2420399 seconds. Package SHA-256
`286cc78595f59393739c917c86a02db7a3d1d3d9a9331531bac361a6c5c7bc95`,
DLL SHA-256 `11e36f03fd8d58763516ca7291508c4bd367ad1ea47e8f12c67a2904b5de288e`,
MVID `bfb6ec50-babc-4f92-9d2b-c6a4dd070b5c`. Exact private receipts are in
`analysis-cache/chunk7-continuation/owner-seed-batch13/`.

Batch 13 is **FAIL**: three actual runs, one PASS and two preserved FAILs;
slots 3/4 were DEFER — EVIDENCED and never launched.

- `20261018-c7b-world42-ownerseed-b13s1-worker`: FAIL, 90.8167054 s, game PASS.
  The restored-start driver left all six field actors selected. The strict
  selected-A native-save evidence contract rejected it. Add normal Select(A)
  after world/authority admission, with a regression; preserve this FAIL.
- `20261018-c7b-world42-ownerseed-b13s2-barrier`: FAIL, 90.4350347 s, game PASS.
  Normal B Dismount retired B and its live slot bindings, but four recognized
  hotbar templates remained. Aggregate facts 6→4 cannot independently exclude
  an active B fact masked by an absent A fact. Independent review requires a
  per-actor/per-kind native fact inventory and fresh binary/native retry.
- `20261018-c7b-world42-ownerseed-b13s5-inventory`: PASS, 84.6206638 s,
  evidence SHA `5a339ee104ccf2aa8062501d53b760816e1b9af59681121bfc090e10683966a3`.
  Native ToIndoor/ToOutdoor both resolve to the current SpiderCave area, not a
  second area. MapExit resolves GlobalMapArea; all three use BeforeExit.
  This was read-only discovery, with zero travel/interaction/save requests.

All actual runs have five restoration flags true, all three transaction phases
restored, profile bytes restored exactly, and no remaining game or lock. Failed
outputs are not accepted cold sources. Native barriers/worker/migration are still
unaccepted. The original preview.41 TB and zero save/cold PASS results retain
their original inputs.

Preview.43 source checks are now PASS on the affected inputs: C# authority431,
worker adapter82, preparation/facts39, current-parent guards56, schema1/schema2
guards29/26, actual fixture bind-mode48, source147; PS owned reader680,
source247, boundary218, schema1 reader54 and actual derivative input14, all
with zero failures in their final runs. Affected product compile03 PASS in
26.6666074 s. Exact commands, durations, hashes and retained failures are in
`codex-takeover/group43/`; C# summary03 SHA
`ef98c536fda77d7c2ad300af4884540938adb6d7be1df249b3bb1239ef1417ee`.

Independent review found and closed the actual schema1-current fixture-mode
guard omission, external result-row registration and source-schema evidence
gaps. A generated in-memory projection of failed worker evidence also exposed
compact owner IDs versus native hyphenated actor IDs; the reader now uses the
existing strict native identity comparator. The original worker FAIL stays
unchanged, and its synthetic projection has no native credit. Preparation may
retain four templates; completed save/cold boundaries still require exactly
their saved live slots. The final review receipt is
`owner-seed-review/d4-group43-review01.json` and is required before qualification.

Private batches14/15 declare worker/barrier retries and both migration chains
(historical input, native schema3 resave, separate third-process actual schema3
load). Existing positive P07 cleanup is not runnable unchanged: its diagnostic
driver still reads the legacy schema1/2 format after the current schema3 writer.
Those two removal slots are unrun DEFER - EVIDENCED pending a bounded current
observer/reader connection; no Working incompatibility is inferred. Area route
preparation must respect actual GlobalMap cleanup policy. No native run or
Candidate packaging is claimed by these source checks.

Next coherent group (preview.43) is IN PROGRESS: worker selection setup, exact
control-fact evidence, schema1 source/historical migration/native resave and
both schema1/schema2 third-process loads of actual schema3 native output. Root
is sole native executor. Review, focused checks, one clean candidate build,
Candidate and scoped WhatIf precede fresh bounded native batches. Area transfer,
repeated lifecycle, registered disable/permanent Horse refusal, positive cleanup
and genuine no-DLL load, native malformed/future refusal and opportunistic doors
remain required. No merge/release/permanent installation/full7E/HUMAN PLAY.

## Active Directive 4 checkpoint — October 18, Codex takeover

Continue [Directive 4](docs/CHUNK7-OWNER-DIRECTIVES-2026-10-16.md#assignment-directive-4).
Its endpoint is still **IN PROGRESS**. Small, progression certification, six-pair
stress/performance, full 7E, merge/release and HUMAN PLAY remain separate gates.

The active successor branch now occupies the dedicated repository
`C:\Dev\KingmakerMountedCombatLab\repo\KingmakerMountedCombat`. Use the original
`codex-policy/Push-KingmakerMountedCombat.ps1`; copied-helper execution was rejected
by automatic approval review even after owner approval. The original guarded helper
successfully published `6652c7fe1e8adb0edb61c28f520275bf5f708b65`. The former successor
worktree is preserved detached at that source as the preview.41 frozen product.

**New accepted result:** `20261018-c7b-world41-ownerseed-b12s1-tbload`, preview.41,
terminal **PASS** in 104.4654715 seconds. Codex independently reviewed Claude's
`f1e54002` fix, then cold-loaded the accepted batch-11 TB producer in another process.
Completed A's saved `Boundary.Movement.MetresStepped=2.286` stayed exactly `2.286`
at both shared-world and presentation restoration. Native continuation and cleanup
passed. Mods, Working, Baseline and protected saves were restored; profile bytes were
restored exactly and the runtime lock was released.

**Retained failure:** `20261018-c7b-world41-ownerseed-b12s2-zerosave`, terminal
**FAIL** in 115.7495532 seconds, despite game PASS. The external reader treated the
zero-mounted save boundary as though the preceding attacks were already detached;
rider B was actually still mounted and its own Horse paid the native approach.
Restoration passed. The failed producer cannot seed a cold load. The reader correction
has focused 126/0 checks and final independent review PASS after three retained FAIL
reviews closed malformed scalar, identity-format and conflicting-owner cases. Immutable
raw re-evaluation is diagnostic only. No native transaction is live at this checkpoint.

**Fresh zero checkpoint accepted:** b12s4 native Manual producer PASS in
126.9279735 s and b12s5 separate-process cold load PASS in 119.7310995 s on the
same frozen preview.41 product, with harness/reader `2b3d6702`. The save has zero
mounted pairs and two retained participation owners. Exact restoration, native
continuation and each Horse's independent native turn at N+1 passed. Original b12s2 remains
FAIL, its dependent b12s3 was never launched, and b12s5 uses accepted b12s4 only.
Batch 12 is settled with no live transaction. Source work now connects worker
cancellation, nonselected-owner save fencing and legacy pending-survivor migration.

The next source checkpoint is preview.42: explicit owner-seed worker replacement,
nonselected-owner Mount save fencing and legacy schema-2 pending survivor paths,
plus read-only stock transition discovery. Focused checks pass (authority401,
boundary authority115, actual adapters78/26, owned reader584, source205,
boundary reader204, combat source29 plus nested29/55/126, route19 and existing
cold composition210; no failures in these final runs). Initial failed generated
fixtures and the corrected live-versus-migrated owner comparison remain recorded.
Independent source review found no additional defect. Candidate packaging and
all five declared native batch-13 cases remain TODO at this source checkpoint.
Earlier Directive 4 evidence remains on its original inputs: RT click integrity and
first-action Mount in both orders (preview.35); Quick/Auto save plus cold load
(preview.36); RT debt save/load (previews.37/.38); TB debt producer (preview.40).
The [handoff](docs/CODEX-HANDOFF-CHUNK7-DIRECTIVE4-2026-10-18.md) records those exact
batch/slot identities. Pending survivor, nonselected-owner barrier, worker cancel,
area/Auto continuity, disable/removal and schemas 1/2 still require their selected
source connections and native evidence. Doors remain opportunistic.

Preview.41 was built once: DLL MVID `daf5ccc5-3a74-40e6-9563-8e8fa28daa4f`, package
SHA-256 `291d8fb4747d1f1e307a68c259a0d6631e5ecb37909ab8fa0c3bb7f9ed252709`.
Candidate passed 6/6 in 388.1997642 seconds (834 components, 147 source checks,
281 harness checks, paired contracts, manifest checks and 751 Kingmaker contracts).
Its unchanged package is reused through the existing frozen-product guard; a fresh
suite binds the new execution root. Private batch, review, freeze and suite identities
are in `analysis-cache/chunk7-continuation/owner-seed-batch12/`.

## History — Directive 3 checkpoint (preserved below)

## Current state (October 18, 2026)

**Governing instructions.** The owner's [directives of October 16–17](docs/CHUNK7-OWNER-DIRECTIVES-2026-10-16.md), including Directive 3 (the PR #5 review and the persistence/combat assignment), and the [validation policy's development-acceleration section](docs/VALIDATION-POLICY.md#development-acceleration-owner-directives-of-october-16-17-2026). The active development workflow uses:
- the approved owner-seed route;
- a dependency-scoped development audit (`-DevelopmentScopedAudit`), labelled as not a whole-lab purity proof;
- focused behaviour tests;
- bounded batches of at most five declared native scenarios.

The old lifetime limit of 25 attempts and the mandatory whole-lab rehash are historical, not current admission requirements. All historical attempt ids and failures are preserved.

**Branch roles:**
- **This branch, `codex/mounted-combat-chunk7-successor`,** is the unified product plus harness line. It is reviewed as draft PR #5, which is not to be merged, released or installed for human play. Product and harness identities are recorded per run: one package per committed head, pinned by commit, DLL SHA-256 and MVID, plus a fresh qualification suite.
- **`codex/mounted-combat-chunk7-preview10-harness`** holds the root records: directives, the dated checkpoint history and earlier campaign documents. It carries no successor code.
- **The private lab, `C:\Dev\KingmakerMountedCombatLab`,** holds fixtures (the immutable owner-seed master and its intake), runtime state and evidence, the batch ledgers and the review receipts. None of these are in Git.

**What works**, summarised in [owner-seed evidence](docs/CHUNK7-OWNER-SEED-EVIDENCE.md):
- The owner seed `KMC_C7_NO_MODS_OWNER_SEED` (SHA-256 `3405d7c8…821b924f`) is qualified by the accepted cold load `20261017-c7b-world18-ownerseed-s4-cold`.
- One two-pair movement, selection and control slice was accepted in `20261017-c7b-world22-ownerseed-b2s4-twopair` (preview.22, `162a6d14`). It covered disclosed native setup, both native Mounts, independent moves, an independent Dismount with the other pair retained, unmounted control and remount cycles, with verified restoration.
- **Two-pair Manual persistence** (October 18), on preview.26 `20e23743`:
  - `20261018-c7b-world26-ownerseed-b3s1d-save` wrote one genuine owner-seed-derived native Manual whose schema-3 read-back matches both live pairs.
  - `20261018-c7b-world26-ownerseed-b4s1-load` cold-loaded an independent copy of it in a separate process and restored both owners with the producer's identities and generations.
  - Restoration was verified both times.
  - Product fix on the way: KMC refused to load its own all-pair saves (`0ccda7cc`).
- **Two-pair combat** (October 18), on preview.34 `bba4edde`. All three run on cold-load-restored mounted pairs against the runtime-only stock Mammoth target:
  - RT with Valerie's ordinary native action: `20261018-c7b-world34-ownerseed-b6s5m-rt`, PASS, 10/10. Concurrent independent primaries, each paid by its own actor. The companion took exactly one ordinary command with its own cost, and neither pair was touched.
  - TB mount-before-rider: `20261018-c7b-world34-ownerseed-b6s4m-tbmount`, PASS, 12/12.
  - TB rider-before-mount: `20261018-c7b-world34-ownerseed-b7s1-tbrider`, PASS, 12/12.
  - Each TB run covered:
    - each pair's full native activation;
    - native renewal in the next round;
    - a native Dismount as the first action of a prepared turn;
    - a native rider-removal forfeit with a pending-only survivor;
    - isolated cleanup failure.
  - Restoration was verified for every run.
- **Product change, owner decision of October 18:** a voluntary turn-based Mount/Dismount is now admitted in the rider's prepared (Preparing) turn (`06ec7180`). This supersedes Chunk 6A rule R3, which made the transition unavailable as a turn's first action.

**Not proven:**
- acquisition (setup is disclosed assistance);
- door behaviour (no qualifying door at the seed location);
- Quick/Auto/area/removal/migration persistence breadth;
- Small calibration;
- stress/performance;
- human review.

The Chunk 6A probe row `CM01-combat-mount-preparing-refused` still asserts the superseded R3 and will fail if re-run. See [known product debt](docs/CHUNK7-KNOWN-PRODUCT-DEBT.md).

**Directive 3 endpoint status:** reached on the owner-seed route. The pieces are:
- the reusable owner-seed-derived two-pair native Manual;
- its successful separate-process cold load with both owners restored;
- the targeted RT/TB combat slice showing independent actor expenditure.

Details are in [owner-seed evidence](docs/CHUNK7-OWNER-SEED-EVIDENCE.md). Nothing is merged, released, installed for human play or claimed as HUMAN PLAY acceptance.

**Known not-ready families:** ordinary acquisition, manual derived, native progression and assisted fixture production still wait for a level-up autosave that the installed game never requests. Do not select them until their completion contract is repaired.

## History — state before October 16, 2026 (preserved as written)

The [sustained mission](planning/CHUNK7-CONTINUATION-MISSION.md) is stopped at the owner’s October14 quota/push request. [Safe stopping point, exact continuity and resume](docs/CHUNK7-OWNER-STOP-2026-10-14.md). No game, runtime transaction, lock, sentinel or test session remains. [Claude takeover guide](docs/CLAUDE-HANDOFF-CHUNK7-2026-10-14.md) is prepared for the next owner-invoked session. The breadth candidate is not ready. Root owns integration and every live transaction on `codex/mounted-combat-chunk7-preview10-harness`, origin `https://github.com/howardreith/KingmakerMountedCombat.git`.

Preview10 is frozen from published product `0e1ce2d92a368866ce0803d070a6f3b7b96bd971`. It was built once in48.5995196 seconds (source147/0, package11/0). Actual Candidate03 passed all six stages in214.1538249 seconds on harness `b8db761b3d175aebc701a270a6016838b46a3768`: components817/0, source147/0, Harness271/0, paired-activation contracts, manifest11/0 and Kingmaker contracts701/0. Candidate01/02 remain FAIL; the two external contract repairs changed no product/package bytes. Suite WhatIf passed113.3220773 seconds and append-only Create passed44.08256 seconds. [Exact artifact, source, review and suite identities](docs/CHUNK7-PREVIEW10-CANDIDATE.md).

The reviewed implementation connects bounded assisted campaign preparation, separate read-only cold inventory, and an explicit qualified assisted-source route to ordinary acquisition. Passive startup telemetry records actual Unity frames and elapsed time. It changes no action or timeout. The three consumer review repairs preserve main XP from readiness, bind inherited-pair checks to admitted custody, and require exact cold profile restoration. [Behavior, focused checks and retained failures](docs/CHUNK7-ASSISTED-FIXTURE-SUPPORT.md).

Nine of25 native attempts are used, all final FAIL and settled. Preview9 failed before any load request; startup cause remains unexplained. Separate recovery restored Mods, protected saves, profile/settings and dependencies exactly, with zero accepted cache changes, then released the lock. [Exact receipts and unchanged failure](docs/CHUNK7-NATIVE-OBSERVATIONS-2026-10-12.md). No preview10 native transaction has started.

The full read-only preview10 WhatIf was stopped incomplete at the owner’s request; its actual terminal exit-1, transient immediate-stop failure and later proven process absence are preserved. It grants no purity PASS or native admission. Current Mods/saves exactly match the admitted suite. Approved synthetic progression archive checks passed19/0 in18.8470646s. Two unfinished source drafts are pushed as inactive patch backups, explicitly uncompiled/unexecuted and not integration-ready. On resume, review those drafts; any native10 requires a fresh numbered reviewed full audit against current inputs.

Native capacity two, offline six, retained twelve; native writer schema3 with explicit schema1/2 adapters. Medium Horse/Mammoth remain supported; Small catalogue is EMPTY pending body/pose proof. RT Charge is default-off; TB Charge cost-free refusal and defensive feat remain deferred. Required RT/TB action/debt, persistence/area/removal, ordinary progression, Small/six-pair, matched measurements and unmounted/diagnostics-off controls remain open. No7E, merge, release, permanent installation or HUMAN PLAY acceptance is selected.

The October14 remote foundation rewrite029089→92c8daf4 has identical tree `1c15365fc9039db2829e3c268ef4a77819db9bac`. Local foundation, unknown/other worktrees and frozen artifacts remain preserved. [Preview9](docs/CHUNK7-PREVIEW9-CANDIDATE.md) retains its original identities.
