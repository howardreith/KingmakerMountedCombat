## 2026-10-09 - Chunk 6 final consolidation on frozen preview.205 (Claude)

6B IMPLEMENTATION STABLE - RT SUPPORTED; TB DELIVERY DEFER - EVIDENCED. 6C, 6D, 6E: ENGINEERING COMPLETE -
OWNER ACCEPTANCE PENDING on frozen preview.205. 6F: executed in full on that candidate; the combined 6A ledger
is 68 PASS / 18 BLOCKED / 1 FAIL and does not pass the completion gate.

Frozen 205 (source 558e6f93b6e21c12b6d5dc15eb44f6deb57302df, tree 868a2c0d..., package
cbb35b5088a87eb2d673455f084de380ed13131695e31b634711699c33304bf0, DLL 10fbb85ca3445d3ea510e5113127deae29ed36561d555c7f9f5b30903518ad20,
MVID 0c9fd64d-89be-494a-b985-3ac1c1bd91e2, suite 20261009-chunk6de-staged-q 380de2c3..., freeze candidate205-freeze.json
e5550560..., observer-augmented candidate205b-freeze.json ee878446..., purity p1 PASS exit 0 in 99.0 min with the sentinel,
ledger-shaped receipt analysis-cache/chunk6a-causal/purity-receipt-preview205.json 43b410ff...).
- Twelve-stage batch, preview205-campaign-closure.json 0d5eb9881fe050447afdaf3782b2c1b3dc455b6a287e3c48912570b9f031cd2b:
  Stage 1/2/3/4 (6C mounted RT, mounted TB, unmounted RT, unmounted TB) native 17/0, 15/15 rows external PASS, envelope
  PASS, restored; Stage 9/10 (6D RT/TB) native 7/0, 5/5 rows PASS, envelope PASS, restored; Stages 5-8 persistence PASS;
  Stage 11/12 (6E RT/TB) native 6/0 and restored, swift rows external FAIL under the frozen 205 reader only (RT: the rule
  compared the settled cooldown with the charge instead of the cost boundary; TB: an out-of-turn Swift input is admitted
  into a shell that never starts - finished Success without running, or interrupted by the next pair preparation - with no
  cost, cast or spend). The turn-based cleanup stall of 203/204 is explained and closed: at cleanup the game sat in the
  Pause mode with no unit in combat and a stale Player.IsInCombat; restoring the pause captured at setup ended every TB stage.
- Final consolidation: final-plan205.json superseded after two stages (its 182 reference matrix ran the whole RT charge
  matrix in one 300-second stage; final205-campaign-closure.json e5782d16...); final-plan205b.json (84 stages, cohort 6B
  matrix from native-plan188.json): 27 PASS, stage 28 (p07 no-DLL cold load) refused twice - no removal observer bound,
  then the stage identity already observed (final205b-campaign-closure.json f0f9c6bb...); final-plan205c.json (58 stages,
  prefix c6-final205-s, observer bound, 26 PASS runs and the 12 batch stages bound): 50 PASS, 8 FAIL retained
  (final205c-campaign-closure.json db07d84bba733c675eb8ab6e0ac447247944a0eda72094b1174c465806f6918a).
- Retained failures: stages 27, 28, 38, 39, 40, 42 (6A allocation rider/mount first, rider-other-action,
  unrelated-candidate-between, dismount-after-rider-expenditure, dismount-immediately-after-mount; all turn-based) passed
  natively and were refused by Chunk6aPreCombatPositioningEvidence.ps1's forward-route bound while the v2 ground plan
  proved the candidate through its reciprocal origin boundary; stage 35 chunk6a-mount-spent-standard-tb failed natively
  ("Native rider ground setup did not settle on the same Acting turn"; the ground order was issued on a Preparing turn) and
  its reviewed worker was stopped by the executor after 47 minutes inside the post-run JSON member walk of the 2.9 MB
  failed-run result, with evidence and restoration already complete; stage 43 chunk4-area-cleanup failed natively (the real
  Game.ReloadArea was dispatched, no native AreaUnloading delivery arrived within 45 s, the rider kept its movement-agent
  component because the cleanup trigger never fired).
- 204 Stage 8: attempts 1-2 refused (worktree dirtied by the next candidate's edits; stash rewrote LF reader bytes),
  attempt 3 native PASS, outer FAIL on a zero-size achievements.dat profile leaf; its transaction lock released through
  Recover-KingmakerRuntimeTransaction.ps1 (recover205-stale-lock-1).

Harness-only corrections after all native work (product unchanged; version stays 0.1.0-chunk6d-preview.205):
- d4c76cf5208f29dd3e7c31359b94aa746e10fa18: 6E never-started turn-based Swift shells are the native refusal; the real-time
  Swift charge is read at the cost boundary with decay admitted; the 6A positioning reader measures the v2 reciprocal
  origin-boundary proof like the producer and the shared plan reader; Test-KmcExternalReaderRefusal admits the positioning
  reader's "TB positioning ..." refusals into the re-evaluation contract. Tests 6E 82/0, positioning 37/0, re-evaluation 32/0.
- 71d25ecf6e71add8d4381714a3e03afe879bc61b: Test-NativeForcedDetach.ps1 follows the dispatch module move and the extended
  schema chain (the FULL umbrella had refused it on two stale anchors).
- Harness evaluation through the repository scenario dispatch over all three closures:
  reader206-preview205-harness-evaluation3.json c2b7d69ed2717d11e305e1d2620ae6eb69449d7ae0579d7197a842ef08d1d780 (evaluation2
  1fba7b56... pre-commit, evaluation1 superseded): the five 6E swift rows and the six positioning refusals become PASS;
  nothing else changes.
- Gates (preview206-offline-gates.json): build/source 147/0, components 719/0, compiled observer 204/0, 6C 287/0, 6D 83/0,
  6E 82/0, positioning 37/0, FAST 12/0, CANDIDATE 16/0 (harness 271/0, assembly 701/0), FULL 1/0 (preview206-full3, 24.5
  min; full1 stopped by the executor before the last harness edit, full2 failed on the stale anchors; both retained).

Final combined 6A ledger (docs/chunk6a-ledger.json; Build-FinalLedger.ps1 over candidate205-freeze.json with
native-plan205.json, final-plan205b-bound.json and final-plan205c.json; regression roles bound through the regression
binder, native-PASS reader-refused runs through the re-evaluation contract; Test-Chunk6aLedger.ps1 record checks 87/0,
40 retained failures bound): 68 PASS (CM03-rider-before-mount-slot, CM03-mount-slot-before-rider, CM03-rider-other-action,
CM03-early-end-turn, CM03-next-round-activation, CM03-unrelated-candidate-between, CM05-after-rider-expenditure and
CM05-immediately-after-mount through re-evaluation), 18 BLOCKED (CM02-left-area, CM02-view-agent-lost,
CM02-loading-cutscene, CM02-generation-change, CM04-turn-end, CM04-mode-exit, CM04-area-session-transition,
CM04-rider-death, CM04-mount-death, CM04-injected-exception, CM07-cold-load, CM07-save-slot-routes,
CM07-unsettled-save-deferred, CM07-area-reload, CM07-schema-unchanged - no implemented scenario, owner decision or
implementation required; CM03-mount-spent-standard - native fixture failure; CM08-persistence-suite - its CM07 members;
CM08-disable-removal-readiness - the Chunk 5 persistence ledger is not bound), 1 FAIL (CM08-area-restoration -
chunk4-area-cleanup). The -Completion gate does not pass on this ledger.

Awaiting the owner: acceptance of 6C/6D/6E on frozen preview.205; decisions on the 15 unimplemented 6A ids, the
chunk4-area-cleanup and mount-spent-standard-tb failures and the Chunk 5 ledger binding; HUMAN PLAY acceptance.
No merge, PR, tag, release, permanent install, protected-save write or HUMAN PLAY acceptance under this continuation.

## 2026-10-09 - preview204 closed (twelve stages); coherent205 turn-based fixture, record and cleanup corrections (Claude)

6B IMPLEMENTATION STABLE - RT SUPPORTED; TB DELIVERY DEFERRED; FINAL QUALIFICATION
DEFERRED TO CHUNK6 CONSOLIDATION. Charge stays default-off; its TB refusal is not
delivery. 6C IN PROGRESS (two qualified RT stages, twice); 6D IN PROGRESS (RT qualified); 6E ran natively
twice with no qualified stage; 6F incomplete.

Frozen 204 (source 0e45a832cfe9056e808873596c0ce4c73a507c5a, package 6bad2cc126004b5605130654fd33c6c9d9da0475b47b46b0c7bdbbe6cb2b0985,
suite 20261009-chunk6de-staged-p, freeze 6d561cfc..., plan 1f8a9c65...) is closed
(preview204-campaign-closure.json 613ec6aa33a58553fa5d6934e82e38a80a3679e888bc8c4d300099df4e18848d):
- Purity p1 PASS exit 0 with the sentinel after 91.8 minutes (orphaned controller; ledger-shaped receipt
  analysis-cache/chunk6a-causal/purity-receipt-preview204.json).
- Stage 1 mounted RT and Stage 3 unmounted RT: native 17/0, external 15/15 rows PASS, envelope PASS,
  worker 0/game 0, restored exactly (qualified a second time on an independent candidate).
- Stage 9 6D RT: native 7/0, external 5/5 rows PASS, envelope PASS, restored - the first qualified 6D stage
  (move-cast-move, cast-then-move, double-move-ranged through the product's RiderRanged pair command,
  movement-exhausted on the 6 s move cooldown, auto-stop-boundary).
- Stage 2 mounted TB: 14/15 (rider-incapacity and mount-incapacity now PASS: the TB lost-spell charge, the
  paired turn-end mount debt and the acting-turn remount all held); under-threat FAIL: the boundary saw
  riderEngaged true before the hostile's turn (engaged since earlier rows), so the deferred hostile attack was
  never issued; the native defensive check itself happened (dc 17, roll 20, 1.52 s).
- Stage 4 unmounted TB: 13/15; rider-incapacity FAIL on the exact TB mount-debt rule (the separately-turned
  mount's Standard decayed 4.00 -> 3.76 during the rider's lost cast, the rider's own delta: TB cooldowns of
  every unit decay with game time); mount-incapacity: the rider restored from its life row was still prone
  at its Acting turn, the row's cast was admitted and interrupted in the same frame by the native stand-up
  (move 0 -> 3) and no stimulus was applied (the reader threw on the missing beforeIncapacity).
- Stage 10 6D TB: 4/5; double-move-ranged FAIL only on the replay rule: the retained-Standard cast waited for
  the rider's next native turn and that turn's pair preparation (prepare-before, cooldown clear, prepare-after
  under a new turn identity) was counted as a replay.
- Stage 11 6E RT and Stage 12 6E TB: 1/4 each; every swift record carried only its rod and instrument keys:
  IssueStagedCast re-assigned the already parented keyed JObject to its owner, which stores a clone and
  orphans the record the cast facts were written into. TB facts retained: out-of-turn and attack-window
  Swift inputs were refused natively with sw 4.0 remaining (the paired turn end spends the Swift too); the
  hostile attack issued on the hostile's turn ran 3.37 s then ended in Interrupt at its turn end.
- Every TB casting-family stage (2, 4, 10, 12) ended in the cleanup deadline with playerInCombat true,
  nativeTurnBased false, nativeControllerInitialized false, targetClean true, the allocation party restored
  and cleanupTurnEnds empty (the 204 turn ending never ran: TB was already off at cleanup); the Mammoth
  paired-activation restoration was refused "outside combat" as a consequence.
- Stages 5-7 persistence PASS. Stage 8 settled-tb-load: attempt 1 refused "Worktree changed" (the next
  candidate's edits were applied before the batch ended), attempt 2 refused "Frozen identity changed"
  (git stash re-checked out LF reader files as CRLF), attempt 3 native PASS 1/1, outer FAIL: a new zero-size
  achievements.dat profile leaf (the profile holds dozens of such native cache leaves). All attempts retained.
- 0 qualified TB stages; every result retained; no production casting/action/resource/turn defect.

Native facts pinned (frozen 204 and IL, Assembly-CSharp MVID 07fa1e4d): TB cooldowns of every unit decay with
game time while a command runs; a unit restored from unconsciousness stays prone in TB until it acts on its own
turn and the native stand-up spends the Move action while dropping the triggering command (a Mount click
"finished unmounted", a cast interrupted at admission); hostiles keep the rider engaged across rows; Newtonsoft
clones a JObject re-parented onto its own owner; UnitCombatJoinController.Tick (0x06009360) recomputes
Player.IsInCombat on every tick it runs and UnitCombatLeaveController.Tick (0x06009366) advances the party
group's LeaveCombatTimer by GameDeltaTime only while the default game mode is active (in TB only while passing);
UnitEntityData.Dispose leaves combat before removing the unit from its group.

Working 205 (one coherent tranche, no production policy change):
- 6C fixture: TB threat readiness requires the hostile attack issued on the hostile's own turn; the unmounted
  TB rider that is prone at stage 1 receives one bounded native ground order per rider turn (recorded under
  standUps, at most 3) before the row.
- 6E/6D producer: KeepKeyedRecord reuses the existing keyed child; IssueStagedCast and the swift step never
  re-parent a record.
- Casting-family cleanup: RestoreCastingFamilyPause restores the captured pause state once the mode and
  target restorations are done (the same restoration the Mammoth engine performs at Finish) and the
  cleanupPending record carries gamePaused, originalPause, currentMode, gameTicks, gameDeltaTime,
  partyGroupInCombat, partyLeaveCombatTimer, nativePassing, nativeHasEnemyInCombat, nativeHadEnemyAtSomePoint
  and the units still in combat.
- 6C reader: without a paired turn end the mount's debt may only decay in either mode.
- Shared replay rule (6C/6D/6E): scoped by the cost event's turn identity; a later turn's single native
  preparation per actor is admitted, the row's own turn and any further preparation remain replays.
- Reader-only corrections evaluated against the immutable 204 evidence
  (reader205-preview204-evaluation1.json c7270fadfd0a30a284a3058ae6a4421e9bbdbf3055b594818b77c59ff7ca923a):
  Stage 4 rider-incapacity FAIL->PASS; Stage 10 double-move-ranged FAIL->PASS; nothing else changed.
- Regressions: 6C replay-count unit checks (next-turn preparation admitted, same-turn and repeated
  preparations counted), the unmounted TB life row with mount decay (accepted) and with a rising mount debt
  (refused); 6D TB row spanning into the next native turn (accepted) and a repeated preparation (refused);
  compiled checks for the stand-up ordering, KeepKeyedRecord use and the cleanup pause restoration order.
- Version stamps 0.1.0-chunk6d-preview.205.

Observed gates (preview205-offline-gates.json): build/source 147/0; components 719/0; compiled observer
204/0; casting reader 287/0; 6D reader 83/0; 6E reader 72/0; FAST 11/0; CANDIDATE 15/0 (harness 271/0,
assembly 701/0). The first casting/staged/reaction reader receipts failed on a mis-expanded script path
(exit -196608) and are retained; the reader2 receipts ran the same scripts on the same source.

No 205 package/suite/purity/native credit at this source checkpoint.
Next: coherent commit, guarded push, package (chunk6de-staged-q), suite, freeze 205 with the 12-stage plan,
one orphaned read-only purity, then the batch with the TB stages first (2, 4, 10, 12), then 9, 11, 1, 3, 5-8;
no worktree edit until the last stage has run. 6F afterwards on the final candidate: FULL once, the final
consolidation plan (the remaining 82 stages bound to the 12 already run on the same frozen candidate), the 6A
ledger built through the repository binders; the 15 unimplemented 6A ids stay BLOCKED until implemented or
owner-decided.

## 2026-10-09 - preview203 closed (twelve stages); coherent204 turn-based and 6D/6E corrections (Claude)

6B IMPLEMENTATION STABLE - RT SUPPORTED; TB DELIVERY DEFERRED; FINAL QUALIFICATION
DEFERRED TO CHUNK6 CONSOLIDATION. Charge stays default-off; its TB refusal is not
delivery. 6C IN PROGRESS (two qualified RT stages); 6D/6E ran natively once; 6F incomplete.

Frozen203 (source10f681cb67e6e55b69081295bcb486bcecc4bbc5, package cc376d448b436adb7d2367d3b77d11dd6eae9a70785e6b616e0bb3e0ab96cc5f,
suite20261009-chunk6de-staged-o, freeze5797aa99..., plan64c0f80a...) is closed
(preview203-campaign-closure.json dae0e17a6fcdc3e10725cc084cba4d4f18c1cac637b19ed2ba7de8f1eff17296):
- Purity o1 PASS exit0 with the sentinel after91.1 minutes (orphaned controller; ledger-shaped receipt
  analysis-cache/chunk6a-causal/purity-receipt-preview203.json 905fb06f...).
- Stage1 mountedRT and Stage3 unmountedRT: native PASS17/0, external15/15 rows PASS, envelope PASS,
  worker0/game0, restored exactly - the first qualified6C stages. Under-threat reached the native
  casting-defensively check (dc17 roll17 success) at1.51 s on the mount target; rider-incapacity
  reproduced the lost-spell spend (dc34 roll8, scroll5->4, no cast, no cost); the full-round summon
  was released before the next row.
- Stage2 mountedTB: rows1-12 external PASS (the summon release works in TB), then rider-incapacity
  external FAIL: the paired turn end marks the mount's remaining actions spent (standard0->6 at
  turn-end-after) and the native action controller charges the force-finished shell its Standard at
  the finish frame (cost-before/after with the command end); then the remount after the life row was
  refused one frame after the rider's new turn prepared (combat Mount needs an Acting rider turn),
  leaf deadline at case index13, cleanup deadline, outer FAIL.
- Stage4 unmountedTB: the artifact-less300-second in-process deadline; no tranche artifact and the
  game log was not retained (every later stage copies output_log.txt into the lab).
- Stages5-8 persistence pairs: PASS.
- Stages9-10 6D RT/TB: all5 rows produced natively; external FAIL on the real-time Standard rule
  (settled state instead of the cost boundary), the ranged step (the product admits the mounted
  rider's ranged click as its RiderRanged pair command, charges one Standard and resolves it; the
  stock refusal code belongs to the bypass path only) and the exhaustion rows (HasMoveAction false does
  not end native movement; the Mammoth kept moving from3.68 to5.32 s of its6 s move budget);
  rangedWeaponReleased was never written because the shared tranche cleanup releases the lease first;
  the TB stage ended in a cleanup deadline.
- Stages11-12 6E RT/TB: all4 rows produced natively; external FAIL on two producer/reader key
  mismatches (the swift record lost its instrument/rod keys when the cast record replaced it; rows lack
  hostileActor), on the RT reaction-window row (Entangle cast beside the hostile standing next to the
  mount made the rider and mount save) and on both TB hostile-attack rows (the hostile attack never
  started inside the step: hostiles act only on their own TB turn); TB cleanup deadline.
- 0 qualified6D/6E stages; every result retained; no production casting/action/resource/turn defect.

Native facts added (frozen203): a TB force-finished shell is charged its Standard and reports acted
without a cast process; the paired turn end spends the mount; combat Mount is refused while the rider's
turn is Preparing ("finished preparing"); a TB encounter ends only at a turn boundary; the product routes
the mounted rider's ranged click into MountedPairAttackCommand RiderRanged; the native TB movement budget
is the6 s move cooldown; hostiles act only on their own TB turn.

Working204 (one coherent tranche, no production policy change):
- 6C reader: TB lost-spell Standard charge (exactly one native cost pair on the shell, +6) and the
  paired turn-end mount debt (unchanged at turn-end-before, never above turn-end-after); the lost shell
  is finished with no cast process. Fixture: leaf clock restarts at most16 times per row on turn
  changes (a turn-cycling stall surfaces as a leaf deadline with its progress record); TB remount after a
  life row through one native ground order on the Preparing turn and the Mount click on the Acting turn
  (every attempt recorded, bounded at3); the threat attack is issued on the hostile's own TB turn; the
  casting-family cleanup ends idle fixture turns (bounded) while the party is still in TB combat.
- 6D reader: charge read at the cost boundary and RT cooldown decay admitted; the product's RiderRanged
  pair command (one Standard, mount uncharged, finished Success); exhaustion on the6 s move cooldown
  (legs up to8). Producer: exhaustion legs continue to the cooldown budget; Standard/Swift steps wait
  for a rider turn holding the action; rangedWeaponReleased recorded with its releasing owner.
- 6E producer: the keyed swift record is kept (instrument/rod facts), rows name the hostile, the
  ground instrument is cast beyond the hostile away from the pair (distances recorded; reader requires
  ≥8 m from rider and mount), hostile attacks are issued on the hostile's TB turn.
- Reader-only corrections evaluated against the immutable203 Stage1-3, 9-12 evidence
  (reader204-preview203-evaluation2.json 70f353ab7bffada5e9bb4185781bdaf5daf66605a9b39a4f441d123efd94c256):
  mounted TB rider-incapacity FAIL->PASS; 6D RT move-cast-move, cast-then-move, double-move-ranged and
  auto-stop-boundary FAIL->PASS; 6D TB auto-stop-boundary FAIL->PASS; the fixture-dependent rows stay
  FAIL until the204 native run.
- Regressions: 6C reader negatives for the TB charge and turn-end rules; 6D negatives for the pair
  command and the cooldown exhaustion; 6E ground-distance negatives; compiled checks for the bounded
  restarts, the remount, the hostile-turn threat and the cleanup turn ending.
- Version stamps0.1.0-chunk6d-preview.204.

Observed gates (preview204-offline-gates.json): build/source147/0; components719/0; compiled
observer201/0; casting reader281/0; 6D reader81/0; 6E reader72/0; FAST11/0; CANDIDATE15/0
(harness271/0, assembly701/0). The first FAST and the first casting/staged reader receipts predate the
final reader adjustment and are retained as superseded passes.

No204 package/suite/purity/native credit at this source checkpoint.
Next: coherent commit, guarded push, package (chunk6de-staged-p), suite, freeze204 with the12-stage
plan (6C mounted TB first, unmounted TB, mounted RT, unmounted RT, the two settled persistence pairs,
then chunk6d-staged-rt/tb and chunk6e-reaction-rt/tb), one orphaned read-only purity, then the batch.
6F afterwards on the final candidate: FULL once, the final consolidation plan (the remaining82 stages
bound to the12 already run on the same frozen candidate), the6A ledger built through the repository
binders; the15 unimplemented6A ids stay BLOCKED until implemented or owner-decided.

## 2026-10-09 - preview202 closed (eight stages); coherent203: 6C corrections + 6D staged/6E reaction families (Claude)

6B IMPLEMENTATION STABLE - RT SUPPORTED; TB DELIVERY DEFERRED; FINAL QUALIFICATION
DEFERRED TO CHUNK6 CONSOLIDATION. Charge stays default-off; its TB refusal is not
delivery. 6C IN PROGRESS; 6D/6E offline-ready, native NOT RUN; 6F incomplete.

Frozen202 (source1f1cecb686fdf0ca68784c1f62da422f27b8b4c0, package e736b9cd808f9211a46e493f4e8fac28889e72a5c73e14558345a06b1cc06a5a,
suite20261009-chunk6c-casting-n, freeze650925c6..., plan3255b1be...) is closed
(preview202-campaign-closure.json e02aa6e5f3f3a31709c8fe27226d2fbacb428df57b9182578d5dac2448b988cd):
- Purity n1 ABORTED - NO PURITY VERDICT (the Invoke-CimMethod controller kept the launching shell
  as its parent and the tool host's low-memory process-tree kill took it after ~110 minutes;
  purity202-n1-interruption.json d5e13b34...). Purity n2 PASS exit0 with the sentinel after
  92.6 minutes through the orphaned Start-DetachedController launch (purity202-n2-process.json
  2f0d8683...). Every native stage was bound to that receipt.
- Stage1 mountedRT / Stage3 unmountedRT: native PASS17/0 (15 rows + fixture/restoration),
  worker1/game0, transaction restored exactly; external13/15 rows PASS; envelope FAIL on
  C6C-rider-incapacity ("refused ... spent a charge": the incapacity damage landed inside the running
  scroll shell, the native concentration check failed dc34 roll26 and the engine spent one scroll
  charge, count5->4, with no cast and no cost) and C6C-under-threat ("native defensive check outcome
  unobserved": the self-targeted CLW acted after0.55 s, inside the native one-second window).
- Stage2 mountedTB / Stage4 unmountedTB: rows1-11 external PASS (quickened-self through full-round),
  then "Casting fixture refused End Turn on a foreign actor" at case index11 (C6C-movement-policy
  readiness): the converted full-round summon55702771 joined the player party group and owned its
  own TB turn, so the non-pair member d17c8fd0's turn arrived with changed leased membership;
  cleanup deadline (player still in combat) and the outer Mammoth restoration failed; worker1/game0,
  transaction restored exactly. Mount-independent (identical in both TB stages).
- Stages5-8 (persistence p04 mounted-casting-items save/cold load, p02 casting-items save/cold
  load): PASS, worker0/game0.
- 0 qualified6C stages; every result retained; no production casting/action/resource/concentration
  defect.

Native facts (pinned IL, Assembly-CSharp MVID07fa1e4d, lab *203.il.txt dumps):
- UnitUseAbility.OnTick (0x06002734) opens the casting-defensively window only for a Standard shell
  still running after one second (TimeSinceStart > 1, !IsActed, provoking spell, non-potion source,
  executor in combat and engaged); TryCastingDefensively (0x0600273B) exempts wand sources only
  (UsableItemType Other=0 Wand=1 Scroll=2 Potion=3); a failed check provokes the native attack of
  opportunity. Measured: self CLW ~0.55 s, touch cast at another unit ~1.5 s, Snowball ~1.5 s,
  potion ~1.24 s, converted summon5.5 s RT /2.5 s TB.
- UnitConcentrationController.Tick (0x060090F7) routes damage to a running UnitUseAbility standard
  command into MakeConcentrationCheck (0x06002739); on failure FailIfConcentrationCheckFailed
  (0x06002736) force-finishes the shell before it acts and spends through AbilityData.Spend
  (0x06002B60: ItemEntity.SpendCharges, then SpendFromSpellbook). TB charges the action at the action
  frame (cost-before/after coincide with action-before; a never-acted shell carries no cost).

Working203 (one coherent tranche, no production policy change):
- C6C-under-threat targets the mount (CastingTargetRole: hostile rows click the hostile; friendly,
  post-commit interruption, scroll heal and threatened rows click the mount; the rest the rider).
- The reader models the native lost-spell path for the threatened and rider-incapacity rows: exactly
  one concentration-rule-after (required for rider-incapacity), on failure no cast, no cost in either
  mode, the shell force-finished, exactly one spell spend and one in-place scroll charge of the exact
  leased entity; on success nothing is spent. The threatened row requires the mount target, the cast
  still running after one second and the defensive outcome exactly once; other rows admit no check.
- The full-round row releases its exact native summons before the next row (ReleaseCastingSummons,
  CastingSummonResidue.Remains under component tests, the drain reuses it); the reader requires the
  row-end summonCleanup naming every summoned unit with inState/worldContains false.
- Reader-only correction evaluated against the immutable202 Stage1-4 evidence before any native
  run (reader203-preview202-evaluation1.json 14abe5a72f6d2f967a06c98120c5a709db0d48ba0387a3e93e38927c71148640):
  rider-incapacity FAIL->PASS on both RT stages; under-threat and full-round FAIL for the new
  requirements the202 fixture could not have produced; nothing else changed.
- 6D staged-action family (Chunk6StagedScenario.cs, Chunk6dStagedEvidence.ps1, schema45, contract
  native-mounted-staged-actions-v1): C6D-move-cast-move, cast-then-move, double-move-ranged,
  movement-exhausted, auto-stop-boundary on the casting fixture with native remaining movement
  (TB legs until UsedTwoMoveAction/no move action, RT exactly two); the stock mounted ranged attack is
  the product's MountedRangedUnsupported refusal, cost-free.
- 6E reaction-feasibility family (Chunk6eReactionEvidence.ps1, schema46, contract
  native-mounted-reaction-feasibility-v1): C6E-swift-on-own-turn, swift-out-of-turn,
  attack-on-mount-observed, reaction-window; the rod-quickened Swift instrument on the first
  available memorized level-1 slot, the hostile attack on the mount observed through
  Chunk4IncomingRuleObserver; rider debt only through admitted shells.
- Registry/dispatch: eight compound roots, 24 schema registrations (44/45/46), launcher ValidateSet,
  child-entry preamble, source validation of the compiled case/plan lists against the readers.
- Regressions: casting reader negatives for every new rule (lost spell without spend, double spend,
  charged force-finish, live shell, self-targeted or sub-second threatened cast, live or foreign
  summon residue); CastingSummonResidueTests; compiled checks for the target roles and the
  release-before-record call order; 6D/6E readers76/0 and70/0.
- Version stamps0.1.0-chunk6d-preview.203.

Observed gates (preview203-offline-gates.json 635090ca039eae51bc1f727820086eb51187582b47bf6ba6ad1f9fabb24c38f9):
build/source147/0; components719/0; compiled observer197/0; casting reader274/0; 6D reader76/0;
6E reader70/0; FAST11/0; CANDIDATE15/0 (harness271/0, assembly701/0). Retained: the three
*-reader1 receipts failed in the observed wrapper's launch arguments (script name not expanded; no
test ran) and were rerun as reader2.

No203 package/suite/purity/native credit at this source checkpoint.
Next: coherent commit, guarded push, package (chunk6de-staged-o), suite, freeze203 with the
generated12-stage plan (6C mounted RT first, mounted TB, unmounted RT/TB, the two settled
persistence pairs, then chunk6d-staged-rt/tb and chunk6e-reaction-rt/tb), one orphaned read-only
purity, then the batch. 6F afterwards on the final candidate: FULL once, final package/suite/purity,
the94-stage consolidation plan (New-FinalConsolidationPlan.ps1) and the6A ledger; the15 unimplemented
6A ids (CM02 left-area/view-agent-lost/loading-cutscene/generation-change, CM04 turn-end/mode-exit/
area-session-transition/rider-death/mount-death/injected-exception, CM07 cold-load/save-slot-routes/
unsettled-save-deferred/area-reload/schema-unchanged) stay BLOCKED until implemented or owner-decided.

## 2026-10-09 - preview201 closed; coherent202 disposable stacks on the exact equipped unit (Claude)

6B IMPLEMENTATION STABLE - RT SUPPORTED; TB DELIVERY DEFERRED; FINAL QUALIFICATION
DEFERRED TO CHUNK6 CONSOLIDATION. Charge stays default-off; its TB refusal is not
delivery. 6C IN PROGRESS; 6D-6F remain incomplete.

Frozen201 (source0c5c8d3f9dbcf3dd794ac72b5f544e54e9352a37, tree
50110c4526c2e38e77bb20cba23ba5a005f86ca4, packagebbf555908ca1a4e99bf85fb6c378c59418f408468f40855b7b80dac4b2ea652b,
DLL38c1ff6cf835da721436838f567e233bda11a75ca7101efd6af04b64d2d37f55 /
MVID85e9b16f-35aa-417c-98f8-5f8719621409, suite20261009-chunk6c-casting-m
fbb748bd4ed0b52435cd2c33e7b940e01687ff348577a9fc08ee1851ffabe633) is closed:
- Purity m1 PASS: detached controller, child22704, exit0 with the PASS sentinel after90.4 minutes
  (purity201-m1-process.json f84c4fc958d2699923fd79ae93e3fab6842b1ca9f731103950a28da09b37a083).
- Stage1 c6c-casting201-m-mounted-rt FAILED natively inside fixture setup before any6C row:
  "Native equip did not retain the exact fixture item" from NativeCastingItemLease.Acquire(count10)
  (producer2 rows: CM01-native-mammoth-fixture FAIL, CM01-native-mammoth-restoration PASS; frames118,
  39.2s). Worker1/game23328 exit0; transaction restored exactly (modsRestored, saveProtection,
  baselineImmutable, workingRestored, allowlist all true; empty restoration/observation errors).
- Native facts (Assembly-CSharp 3b6450ff..., read-only IL): ItemSlot.InsertItem (0x06007C7C) splits every
  stackable item to one unit before taking slot ownership (ItemEntity.Split(1), 0x06007B63; identity
  is kept only when Count == n), so the slot received a new split entity while the count-10 stack
  stayed in the inventory. ItemEntity.SpendCharges (0x06007B74) decrements a slot stack in place
  (count-1, Charges back to1) while a single consumed unit is removed through ItemsCollection.Remove ->
  Extract -> ItemSlot.RemoveItem(bool) (0x06007C7D), the one-bool overload the installed BagOfTricks
  patch refills from the native inventory. The frozen200 Stage1 rows show that refill moving ORIGINAL
  fixture items into the disposable slots after the single potion and single scroll were consumed
  (original potion -510858112 from another party member's quick slot; original scroll 458352640 from
  the rider's quick slot1); the cohort restore returned the occupants but left the other-owner potion
  with a null HoldingSlot inside its original slot (double reference created by the foreign refill),
  which the occupant-only Restored predicate did not see. No production casting, action, resource or
  concentration defect is established.
- Stages2-4 BLOCKED-unrun (same fixture setup), stages5-8 NOT RUN. Closure
  preview201-campaign-closure.json efe8bf56a4ae5a670054d9d8f542857326bedf6dd87ee2ab8d3d0c197ad25c05.

Working202 (one coherent tranche, no production policy change):
- NativeCastingItemLease.Acquire equips the exact single unit first (Split(1) at count1 keeps
  identity; postconditions exact slot, holding slot, count1) and then builds the bounded disposable
  stack on that equipped entity with ItemEntity.IncrementCount (count, slot, holding slot and
  collection re-verified). The scroll stack stays x10; the potion becomes x2 so the one measured
  drink decrements in place instead of removing the unit and invoking the foreign refill. The first
  release snapshot (beforeCleanup) is no longer overwritten by the Dispose re-entry.
- Chunk6cCastingScenario resolves every item-sourced row through ExactCastingItemAbility: the lease
  must be exactly equipped with at least two units and the native AbilityData must source that exact
  entity; otherwise the row refuses before input. Spellbook rows and the full-round conversion are
  unchanged. Same15 rows/four baselines/eight stages/schema44/30-second leaf.
- CastingFixtureSlotSnapshot/NativeCastingOriginalSlots: Restored now requires occupant identity
  AND the native HoldingSlot link; Restore relinks a dangling original through the same exact native
  removal and insertion (resources untouched). Component tests cover both.
- External rules (Chunk6cCastingEvidence.ps1): each item row's source item must be the exact equipped
  lease entity of the same identity registry (never an original or a refill); every completed item
  cast decrements the exact stack in place (exactSlot before/after, count-1, charges1, at least two
  units before); refused scroll rows leave the stack unchanged; envelope-level, each disposable item
  was equipped as one exact unit, its stack was built on that entity, and its count before cleanup
  equals the requested count minus the observed native spends of that entity while still in its slot.
- Regressions: compiled IL-order check that Acquire adds, equips and only then stacks; PotionStackCount
  bounds; the case entry resolves item abilities only through the guard; reader positives/negatives
  for foreign identity, non-decrementing or displaced stacks, single-unit spends, refused-row changes
  and conservation; the immutable199 Stage1 chain re-evaluation still preserves its FAIL.
- Version stamps0.1.0-chunk6c-preview.202.

Offline gates: build/source146/0; components714/0; compiled observer154/0; casting reader252/0
(reader1-3 retained: synthetic refusal rows and a PowerShell5.1 ConvertFrom-Json array wrapper in the
test fixture, not reader or product defects); FAST12/0; CANDIDATE16/0 (harness271/0, assembly701/0).

No202 package/suite/purity/native credit at this source checkpoint.
Next: coherent commit, guarded push, package (chunk6c-casting-n), suite, freeze202, one read-only
detached purity, then the eight-stage batch with the failed mountedRT first. Then6D/6E (design notes
CHUNK6D-STAGED-ACTIONS-DESIGN-20261008.md, CHUNK6E-REACTION-FEASIBILITY-DESIGN-20261008.md) and6F.
No merge/PR/tag/release/permanent install/protected-save write/foreign-mod change/HUMAN PLAY acceptance.

## 2026-10-09 - preview200 closed; coherent201 casting instruments and external rule corrections (Claude)

6B IMPLEMENTATION STABLE - RT SUPPORTED; TB DELIVERY DEFERRED; FINAL QUALIFICATION
DEFERRED TO CHUNK6 CONSOLIDATION. Charge stays default-off; its TB refusal is not
delivery. 6C IN PROGRESS; 6D-6F remain incomplete.

Frozen200 (source3542dc0c211eb5e0ce47b74ace4127b3e9f48f67, tree
e89a03dc8038a6e2b07c9dc905baac79cdf66978, package4c7d320cff4d90d23883ceb06843689d3a44356521d3e75560b69a1c922c620b,
DLL322f733e6002c0eb029ba40e1c4ff724657ebb8dbd130259dd054eaad7a6b71c /
MVID201bdf32-402d-4ed4-b98d-0d9b81355325, suite20261008-chunk6c-casting-l
4672c26b749dee050a41378bd429da3eb137d5ac3febe6f7f1cfea144c18602b) is closed:
- Purity l1 ABORTED - NO PURITY VERDICT: the Claude Code tool host stopped the hosting background
  task for system memory pressure after about97 minutes; controller and child absent, no receipt,
  exit UNKNOWN (purity200-l1-interruption.json). Purity l2 PASS: detached controller, child30204,
  exit0 with the PASS sentinel after90.6 minutes (purity200-l2-process.json
  c7b03f4917ec3eb0e4828226c1a98d5f695816eb85af03a63755e4a41c89fabe).
- Stage1 c6c-casting200-l-mounted-rt: the native producer completed all15 rows for the first time
  (PASS17/0, frames6290, 112.5s): the AI-lease-before-Mount repair worked - rider incapacity, mount
  incapacity and under-threat settled without a leaf deadline. The outer envelope FAILED at the
  generic registration-audit reader ("PASS horse companion registration audit requires exactly one
  manifested artifact"): the four Mammoth-engine6C roots were listed as audited scenarios although
  Chunk6aMammothScenarioEngine never runs that audit (chunk6a-mammoth-mount-* PASS runs are read
  without it). Worker1/game6772 exit0; transaction restored exactly (modsRestored, saveProtection,
  baselineImmutable, workingRestored, allowlist all true; empty restoration/observation errors).
- Offline external row evaluation of the immutable Stage1 rows:8 PASS /7 FAIL. Five Guidance rows
  (quickened-self, standard-self, standard-friendly, movement-policy, under-threat) failed on the
  native fact that Guidance is not castable by the fixture Druid: the spellbook has no memorized
  level-0 slot (initialPrepared lists three level-1 slots), AbilityData.IsAvailable is false while
  IsAvailableForCast is true, UnitUseAbility.OnAction fails at IL_0008-IL_001C (pinned contract) with
  Result=Fail, no RuleCastSpell, no process and the action still charged; native Spellbook.Memorize
  sets SpellSlot.Available=false until rest, so no lawful runtime memorization exists. The two life
  rows failed only on the pair-replay rule: the incapacitated actor's native combat exit clears its
  cooldowns (combat-clear-before/clear-before/clear-after/combat-clear-after nested for the subject).
  No production casting, action, resource or concentration defect is established.
- Stages2-4 BLOCKED-unrun (shared fixture instruments), stages5-8 NOT RUN (no concrete change;
  return in the successor batch). Closure preview200-campaign-closure.json
  926ad819361a1fcd0a3b39c70661b06c061f5e58d358debfa2f55ff522ebe605.

Working201 (one coherent tranche, no production policy change):
- Instruments: C6C-quickened-self quickens the memorized CLW slot through the rod; standard-hostile
  keeps Snowball; full-round keeps the spontaneous summon conversion; every other cast (standard-self,
  standard-friendly, scroll-interrupt-after, invalid-target, cancel-before, interrupt-before,
  scroll-friendly, movement-policy, rider/mount incapacity, under-threat) casts CLW from one exact
  native scroll stack (count10 through ItemEntity.IncrementCount at creation, released as one item).
  C6C-prepared-interrupt-after is renamed C6C-scroll-interrupt-after (post-commit interruption keeps
  the cost and the spent charge); the historical name stays registered so199/200 envelopes remain
  readable. Same15 rows/four baselines/eight stages/schema44/30-second leaf.
- External rules: the reader pins the native IsAvailable predicate, exact instrument identity
  (spellbook slot vs scroll stack vs potion), exactly one native spend per completed item cast and
  none for refusals, counts a Cooldowns.Clear as a replay only outside the actor's own combat-exit
  window, requires the partner never to leave combat during the subject incapacity, and requires the
  quickened slot to be spent once. The four6C roots leave the registration-audit list, and source
  validation now holds that list equal to the compiled HorseCompanionRegistrationScenarioPolicy.
- Regressions: reader fixtures and new negatives (unavailable orison, foreign instrument, missing or
  duplicated spend, bare clear, prepare inside the exit window, partner exit, settled-window exit);
  compiled checks that the Guidance GUID is no longer an instrument, the row list and the bounded
  stack count; the immutable199 Stage1 chain re-evaluation still preserves its FAIL.
- Version stamps0.1.0-chunk6c-preview.201.

Offline gates: build/source146/0; components712/0; compiled observer150/0; casting reader228/0
(reader1 retained: the renamed row made the immutable199 envelope unknown until the historical name
was re-registered); FAST12/0; CANDIDATE16/0 (harness271/0, assembly701/0).

No201 package/suite/purity/native credit at this source checkpoint.
Next: coherent commit, guarded push, package (chunk6c-casting-m), suite, freeze201, one read-only
detached purity, then the eight-stage batch with the failed mountedRT first. Then6D/6E (design notes
CHUNK6D-STAGED-ACTIONS-DESIGN-20261008.md, CHUNK6E-REACTION-FEASIBILITY-DESIGN-20261008.md) and6F.
No merge/PR/tag/release/permanent install/protected-save write/foreign-mod change/HUMAN PLAY acceptance.

## 2026-10-08 - preview199 closed; coherent200 fixture AI-lease ordering and reader repair (Claude)

6B IMPLEMENTATION STABLE - RT SUPPORTED; TB DELIVERY DEFERRED; FINAL QUALIFICATION
DEFERRED TO CHUNK6 CONSOLIDATION. Charge stays default-off; its TB refusal is not
delivery. 6C IN PROGRESS; 6D-6F incomplete. Claude continues the owner's Chunk6
mission from the published takeover checkpoint 4a840a0543bfd803a6c63135731c385da7ac9e75
(fetched, upstream/remote equal, clean; no game, lock or transaction at intake).

Frozen199 source85186ae3af3fac0a3aa466625188fa53f64d6f71, its package/suite/proof
and the closed campaign (4 PASS/1 fixture FAIL/3 BLOCKED-unrun) remain immutable.
No199 artifact, verdict or receipt was replaced.

Verified cause of the Stage1 C6C-rider-incapacity timeout (frozen evidence):
cleanup.unmountedHorseAiIsolation.states[0] recorded rawAiBefore=false and
effectiveAiBefore=false for the Mammoth, i.e. the fixture lease was acquired after
the exploration Mount had already captured the enabled AI and disabled it. The
relationship restores its captured value on every lifecycle Dismount, so the forced
rider-incapacity Dismount re-enabled the mount: the deadline snapshot shows the
Mammoth inCombat with nativeRoundAttacks=2 while the rider lay prone at damage47,
and the settlement wait (rider/mount commands empty) could not arrive. The lease
later "restored" false, the mounted-disabled value, not the true original.

Working200 (one coherent tranche, no production policy change):
- Chunk6cCastingScenario stage0 acquires both reversible AI leases BEFORE the
  exploration Mount through two pure gates, CastingEntryMayRequestMount and
  CastingEntryReady; the relationship now captures and restores the isolated state
  and only the lease restores the true original at fixture cleanup. Same15 rows/
  four baselines/eight stages/schema44/30-second leaf; no deadline, threshold,
  action, resource, turn, preparation, cleanup or save policy change.
- A bounded6C leaf-deadline capture (case, boundary, case facts, pair command/AI/
  life state, both lease captures) replaces the empty leafDeadlineProgress of199.
- Both generic envelope readers consult one pure registry
  (ScenarioDispatchEvidence.ps1: Get-KmcCompoundRuntimeScenarios) so a compound6C
  request root with child case rows and no self-named aggregate is read
  structurally; individual scenarios keep the exactly-once rule. No duplicated
  registry root, no fabricated aggregate, no Common/launcher change.
- Regressions: ScopedDiagnosticAiLease ordering tests (lease-before-Mount survives a
  forced Dismount; lease-after-Mount reproduces the199 defect), compiled gate tests
  plus an IL-order check that TickChunk6cCasting owns mount then rider isolation
  before its first native Mount click, dispatch registry checks, and compound/
  individual envelope checks against both readers and the immutable199 Stage1 game
  result: the frozen reader reproduces the masking refusal, the corrected chain
  reaches the dedicated6C validator with the raw leaf-deadline FAIL (14/1) and
  refuses PASS promotion; artifact bytes unchanged.
- Version stamps0.1.0-chunk6c-preview.200 (Info.json, BuildIdentity.cs, version.json).

Offline gates (receipts bound in lab20261005-6br/preview200-offline-gates.json,
SHA256 23b325ca512e9f432e3ee9854e187d8d9426838c42d54050e11018727d17e822):
build/source145/0; components712/0; compiled observer146/0; casting reader212/0;
dispatch56/0; FAST12/0; CANDIDATE16/0 with harness271/0 and Kingmaker assembly701/0;
684 non-documentation inputs bound at the gate (preview200-candidate-inputs-terminal.json).
Retained failures: fast1 stopped at Test-PersistenceValidationEvidence.ps1, a
parameterized regression that the tier runner cannot invoke standalone (not a
product or reader defect); casting-reader1 attempted the live launcher reader on
the frozen request, which Test-RuntimeRequest pins to the current version.json, so
the interpretation chain after that launch-time guard is evaluated directly.

Reader identity: Test-RuntimeGameResult.ps1/Test-RuntimeResult.ps1 are not pure
reader paths under the unchanged Test-KmcChunk6aPureReaderPath whitelist, so the
correction rides the200 product candidate; its separate reader identity and the
immutable199 Stage1 evaluation record are written after publication
(Record-ReaderEvaluation200.ps1). The native Stage1 verdict remains FAIL.

No200 package/suite/purity/native credit at this source checkpoint.
Next: coherent commit, guarded push, package (chunk6c-casting-l), suite, freeze200,
one read-only purity, then the eight-stage batch with the failed mountedRT first.
Then continue6D/6E/6F. No6B replay or TB-Charge experiment. No merge/PR/tag/release/
permanent install/protected-save write/foreign-mod change/HUMAN PLAY acceptance.
One mutating executor.

## 2026-10-08 - preview199 closed; transfer to Claude at owner's request

6B remains IMPLEMENTATION STABLE - RT SUPPORTED; TB DELIVERY DEFERRED; FINAL
QUALIFICATION DEFERRED TO CHUNK6 CONSOLIDATION. Charge stays default-off; its TB
refusal is not delivery. 6C is IN PROGRESS; 6D-6F remain incomplete. Codex stops
development here at the owner's request. No preview200 source or candidate exists.

Frozen199 product source: 85186ae3af3fac0a3aa466625188fa53f64d6f71; tree:
5e3fd08810f35d65d6a75cd066675f2a8f4f0534; version0.1.0-chunk6c-preview.199.
The publication following this closure contains documentation only; it must never
replace the frozen product commit in package, suite or proof receipts.
Package SHA2567ef4e05fa3cda94e64e6cc21408fd64e92717e2604268142ff6b89176781a0ad;
manifest4de2bc375e0e777abe4edcb28687fbf3cd71020c641a2f7875a85dd0703e100d;
DLLf0295fd938a4892c1cf6833e24235d94fd68d5d06e888898ae9c69a23253a6e0;
MVIDc01cef6e-03ba-45b0-a8c0-fcec6ac0e475.
Suite20261008-chunk6c-casting-k:
552e17733d52b55d8765654937c75b8078bc446040268db62cd763865b2365f4.
Schema44; compiled producer and external reader identities remain separately bound.

Purity retry2 PASS: independently observed child28036 exit0; 98.92729min.
purity199-k2-process.json SHA256dce63913f5d9cfedc8f3827775dbe3906d4c72083dfd3d63fda901ac7d175809.
Original proof1 lost terminal observation in a daemon restart and remains
UNKNOWN / ABORTED - NO PURITY VERDICT; no original receipt or log was replaced.

| Stage | Run suffix | Outcome | External assertions | Native assertions | Worker/game exit |
| --- | --- | --- | --- | --- | --- |
| 1 | mounted-rt | fixture FAIL | 0/1 | 14/1 | 1/0 |
| 2 | mounted-tb | BLOCKED-unrun | - | - | - |
| 3 | unmounted-rt | BLOCKED-unrun | - | - | - |
| 4 | unmounted-tb | BLOCKED-unrun | - | - | - |
| 5 | settled-rt-save | PASS | 7/0 | 7/0 | 0/0 |
| 6 | settled-rt-load | PASS | 3/0 | 3/0 | 0/0 |
| 7 | settled-tb-save | PASS | 7/0 | 7/0 | 0/0 |
| 8 | settled-tb-load | PASS | 3/0 | 3/0 | 0/0 |

All run IDs start c6c-casting199-k-. Five actual guarded transactions restored
human105, protected saves/Working, Mods/foreign Mods, UMM/settings and required
profile dimensions; restorationErrors and observationErrors are empty. Fresh
suite audit verifies275 save files and358 Mods files byte-identically. All game
exits are independently observed0; no game, proof, build, runtime lock, install
transaction or recovery debt remains. No emergency recovery was needed.
Normal guarded restore uses its existing recovery/quarantine plan. Whole-profile
byte identity is not asserted beyond the exact profile receipts and cache policy.

The capped native damage safely made the disposable Druid unconscious; the cap
itself passed. The C6C-rider-incapacity row then exceeded its unchanged30-second
leaf while ordinary Mammoth attacks continued. Twelve successful structural
baseline leaves do not qualify the failed complete stage. Final cleanup proved
exact shell terminal, containers empty, processes/effects settled and unmounted;
the exact shell terminal time before the deadline is not established.
No production casting defect is established.

Concrete next hypothesis: the diagnostic mount-AI lease is acquired after Mount.
Mount captures original enabled AI, the later lease captures already-disabled AI,
and lifecycle Dismount restores the earlier enabled AI. Acquiring the existing
scoped lease before Mount may preserve isolation through Dismount. This is not
implemented or proven. Verify actual order and add a behavior-focused regression;
retain native command/combat settlement, exact health-owner compensation and
unchanged deadlines. Do not synthesize actions or force completion.

A separate generic reader defect treats a compound6C request as an individual
one and demands a nonexistent named root, masking the native failure. Finding:
PREVIEW199-ENVELOPE-READER-FINDING-20261008.json,
SHA256028cfb142b49f4819dc4aac6720092862996dc6a72442e94d2c74f068874e6ad.
No correction/re-evaluation is applied. Fix interpretation narrowly with complete
envelope tests and a new reader identity; do not mutate artifacts or invent PASS.
The actual compiled fixture repair requires a coherent successor candidate.

Exact199 gates: build/source145/0; components710/0; compiled observer135/0;
casting158/0; persistence79/0; safety271/0; assembly701/0; FAST12/0;
CANDIDATE16/0; package11/0. All685 gate inputs unchanged.
No new test/build/package/purity cycle was run for this documentation closure.

Lab records under analysis-cache/chunk6-continuation/20261005-6br:
preview199-campaign-closure.json SHA2566f437ccd962f0f2b665fb914f3fb8aed798f78ee995cc54ce8674b3b4789b4d2;
preview199-cost-summary.json SHA256182b10a2def09da3566010caae1af585dc2a7c57858dca6aa8339dce64294938;
preview199-cost-reconciliation.json clarifies the final acknowledgement interval.
Measured: CANDIDATE346.708118sec; package6.489405sec; successful purity5935.637637sec;
five native producer windows309.118076sec; restoration/verification36.650373sec;
outer setup/cases/restoration625.953500sec. Intervals overlap; implementation and
approval effort were not separately measured. Four persistence stages repeat
qualified behavior; cap observation is new but the baseline still fails.

Complete final branch/HEAD/tree, publication equality, receipt hashes, processes,
retained failures and next commands:
C:/Dev/KingmakerMountedCombatLab/analysis-cache/chunk6-continuation/CODEX-CHUNK6-HANDOFF-TO-CLAUDE-2026-10-08.md.
Ready prompt: [Claude takeover](CLAUDE-CHUNK6-TAKEOVER-2026-10-08.md).

Next read-only command:
rg -n 'PrepareUnmountedHorseAiIsolation|RequestPhase3dHorseMountedRelationship|C6C-rider-incapacity|mountAiBackingWasEnabled|Individual runtime scenario' src/KingmakerMountedCombat scripts/runtime
Continue6C first, then6D staged native movement,6E bounded native reaction feasibility,
and6F final combined qualification under the existing charter. Do not repeat6B,
reopen TB Charge or replay closed199/proofs. Preserve all historical failures.
No merge/PR/tag/release/permanent install/protected human-save write, foreign-mod
change or HUMAN PLAY acceptance occurred or is authorized.


## 2026-10-08 - preview198 closed; bounded199 lifecycle fixture repair

6B IMPLEMENTATION STABLE — RT SUPPORTED; TB DELIVERY DEFERRED; FINAL QUALIFICATION
DEFERRED TO CHUNK6 CONSOLIDATION. Charge remains default-off; its TB refusal is
not delivery. 6C IN PROGRESS;6D-6F incomplete.

Frozen198 source acc627daf4ed2fd1686ead51ae4c5b19ab7a0b37, tree
de4f2031e0e033fa2c06ff13377c13624b7cd26d remains immutable, fetched/remote-equal
and clean at closure. Purity PASS94.41612min, actual child exit0. Campaign:
4 qualified PASS/1 fixture FAIL/3 shared-fixture BLOCKED-unrun; all5 transactions
restored exactly with empty restoration/observation errors and normal game exits0.
Human105/275 protected saves/358 Mods unchanged; no game or mutable transaction.

The native movement readiness repair worked: the motion row requested and settled
a rider cast while the exact mount carrier was observed moving. Twelve structural
baseline rows receive no complete external-stage qualification. The next row
correctly refused damage to the Druid main-character subject. No incapacity
damage was applied. Exception cleanup also completed before native combat ended,
so the outer paired configuration restore was refused. These are fixture/setup
defects; no product casting defect is established. Both RT/TB settled save-cold
pairs pass on exact198 (7/0,3/0,8/0,3/0).

Working199 uses the native RuleDealDamage.MinHPAfterDamage fence with a checked
native float/difficulty maximum, zero temporary HP and a strict window below
death for this disposable main-character subject. Unclamped main-character
damage remains refused. Original consciousness/dying/immortality/essential/enemy
and non-main safety checks remain. No native LifeState/action/preparation/turn
field is fabricated. The sole external validator requires the actual cap,
unchanged native difficulty, non-fake rule and observed safe unconscious window.
The exact health owner is retained before synchronous native callbacks; fixture
health restoration waits for command/process/effect settlement. Casting cleanup
also waits for actual native combat end under the unchanged30-second leaf budget.
Same15 rows/four baselines/eight stages/schema44. No production casting, movement,
cost/resource, cleanup or persistence policy patch; native199 remains unqualified.

PASS199 build/source145/0, components710/0 via FAST, compiled observer135/0,
casting reader158/0, persistence reader79/0 and FAST12/0;685 inputs unchanged.
CANDIDATE1 PASS16/0; safety271/0 and native assembly701/0,685 inputs unchanged.
Initial editor stopped at a wrong BuildIdentity path after
preserved partial edits; the real file was located and completed. Observer1's
readonly native actor fields were mistaken for properties; observer2 passes with
unchanged identity assertions. All original failures remain immutable.

198 closure939ab1b1b6388649f18c428da3c848612b69ffc55c313a73fbef08f47f26599e;
cost750fa80d4788f8dfa657282edc075ffd5ae49eb926b30e49d652a5d12221e7b8.
Closure's final console formatting failed after both complete records were written.
Independent append-only reconciliationa1aff97638ad942edd8c4a5b7d3437d76b963545f81d2d692c6b0ba412c5d53f
rehashes every original artifact/receipt and retains that failure.
Existing timestamps: proof94.41612min; native setup/cases279.0208s; restoration
39.0942s. One new raw movement behavior, zero newly qualified baseline stage types,
four repeated settled persistence stages. Implementation/approval effort is not
separately measured. No profiling subsystem or additional mandatory matrix.

Next: coherent commit/guarded publication, one199 freeze/proof and the failed
mountedRT first in the existing batch. Finish6C then continue6D-6F. No6B replay
or newTB experiment. No merge/PR/tag/release/permanent install/protected-save write/
foreign-mod change/HUMAN PLAY acceptance. One mutating executor.
## 2026-10-08 - preview197 closed; bounded198 native movement fixture repair

6B IMPLEMENTATION STABLE: RT supported/default-off Charge; TB delivery DEFER -
EVIDENCED with its qualified cost-free refusal. Refusal is not delivery.
6C IN PROGRESS;6D-6F incomplete. Frozen197 source
b91204f9f81057e95a234f732344fd994080dfd5 remains immutable:4 PASS/1 fixture
FAIL/3 BLOCKED-unrun. Purity PASS95.13673min. All5 transactions restored,
game exits0, empty restoration/observation errors; human105/275 protected saves/
358 Mods unchanged. Both RT/TB settled casting/item save-cold pairs PASS.
No game or runtime transaction remains.

The corrected refusal completed. Eleven native structural baseline rows receive
no complete external-stage qualification. The motion row never requested a cast:
it awaited UnitMoveTo.IsRunning (post-approach startup) while native movement had
already carried the pair~6m; RT also incorrectly required a TB-only ground owner.
Working198 fixes only fixture readiness: exact live player Move carrier/executor/
slot plus observed native movement, TB paired owner only when applicable.
One raw movement-before-cast fact is checked by the sole external validator.
Same15 rows/four baselines/eight stages/schema44, unchanged deadlines/thresholds.
No product casting/action/resource/turn/preparation/movement/cleanup/save policy patch.

PASS198: build/source145/0, components710/0, compiled observer95/0, reader153/0,
persistence79/0, safety271/0, assembly698/0, FAST12/0 and CANDIDATE16/0.
685 gate inputs unchanged. Observer1/2 test reflection/terminal-stimulus FAILs
remain retained; observer3 PASS. No198 package/suite/purity/native credit yet.
197 closure467281e7a50b4b227eda349fec670984b7a0c5a4300208398267b0ca870d8477;
cost4163088bace0a2872aed02400070775ea94100c4e767f4089ea28b9708dd1dcb.
Latest full checkpoint: lab analysis-cache/chunk6-continuation/
CODEX-CHUNK6-CONTINUATION-HANDOFF-2026-10-08-PREVIEW197-CLOSED-198-REPAIR.md.
Next: coherent guarded publication, one198 freeze/purity and failed mountedRT first
in the ready6C batch. Continue6C then6D-6F. No6B replay or newTB experiment.
No merge/PR/tag/release/permanent install/protected-save write/foreign-mod change/
HUMAN PLAY acceptance. One mutating executor.
## 2026-10-08 - preview196 closed; bounded197 native refusal fixture repair

6B remains IMPLEMENTATION STABLE: RT supported/default-off Charge; TB delivery
DEFER - EVIDENCED with its qualified cost-free refusal. 6C IN PROGRESS;6D-6F incomplete.
Frozen196 source a9ba8f629aa3bce6a1d2239c112f01c81a2759c1 remains immutable:
4 qualified PASS /1 fixture/setup FAIL /3 BLOCKED-unrun. All5 transactions restored,
empty restorationErrors, observed game exits0; human105,275 protected saves and
358 Mods unchanged. No game or mutable runtime transaction remains.
Purity PASS95.35307min; both settled RT/TB casting/item save-cold pairs pass.
The failed baseline completed five native structural rows, which do not receive
complete external stage qualification. Its intended Guidance-on-enemy refusal
instead admitted an unstarted native shell and hit the unchanged30-second leaf.
Native CanTarget permits that target; the fixture assumption was wrong.

Working197 uses the native unit-only spell's empty-ground target refusal.
Ordinary unit input is retained. No product casting/action/resource/turn/cleanup/
save or substantive acceptance-policy change, schema44 and existing eight stages
unchanged. Actual compiled/native boundary regression84/0, casting reader151/0,
persistence reader79/0 and FAST12/0 pass;685 inputs unchanged.
First build197-1 and observer probes1/2/3 remain failed immutable evidence.
CANDIDATE1 PASS16/0, including components710/0, source145/0, safety271/0 and
assembly698/0;685 inputs unchanged. No197 package/suite/purity/native credit.
Closure27ccb2ca3752f696fe686fdc4c1978150fc716cdad0bb8e381505131ba735f00;
exact receipts/costs under lab analysis-cache/chunk6-continuation/20261005-6br.
Next: CANDIDATE, coherent guarded publication, one freeze/proof and ready6C batch.
Continue6C then6D-6F. No merge/PR/tag/release/permanent install/protected-save write/
foreign-mod change/HUMAN PLAY acceptance. One mutating executor.
## 2026-10-08 - preview195 closed; bounded196 full fixture admission repair

6B remains implementation stable: RT supported/default-off Charge; TB delivery
DEFER - EVIDENCED with its qualified cost-free refusal. 6C IN PROGRESS;6D-6F incomplete.
Frozen195 source9d5eb700a7b62f2851336a94d961c68625551326 remains immutable:
4 qualified PASS /1 fixture/setup FAIL /3 BLOCKED-unrun; all5 actual transactions
restored exactly, empty restorationErrors, game exits0. Human105,275 protected
saves and358 Mods are unchanged; no game or runtime transaction remains.
Both RT and TB settled casting/item save-cold pairs pass on195. Four persistence
runs have exact whole-profile audit receipts; the short failed baseline has none.
No additional whole-profile claim is made for that short transaction.

The first baseline stopped at stage0 before any casting row: the outer pre-target
rider AI scenario list omitted6C even though the nested exact-pair check allowed it.
All four baselines share that guard, so three dependent launches were not repeated.
Working196 adds6C to that exact scenario admission, tests its compiled decisions,
and verifies the full compiled native fixture guard calls the tested gate.
Other native preconditions, exact pair checks and AI lease/restoration are unchanged.
No product casting/action/resource/turn/cleanup/save or acceptance-policy change.
Focused build1 PASS; observer/admission65/0, casting151/0, persistence79/0.
FAST1 PASS12/0;CANDIDATE1 PASS16/0,685 inputs unchanged. Components710/0,
source145/0, safety harness271/0 and assembly698/0 pass.
No196 package/suite/purity/native credit.
Closure11f27b27ff850e21833e1c83f79e4dfb4779cd85316019f8cc3235d6ee9e501d:
lab analysis-cache/chunk6-continuation/20261005-6br/preview195-campaign-closure.json.
Next: targeted gates, coherent guarded publication, one compiled freeze/proof;
failed mounted RT baseline first, then the same existing eight-stage6C scope.
No188 or TB-Charge replay; continue6C-6F. No merge/PR/tag/release/permanent
install/protected-human-save write/foreign-mod mutation/HUMAN PLAY acceptance.
## 2026-10-08 - preview194 closed; bounded195 ready for offline tiers

6B remains implementation stable: RT supported/default-off Charge, TB delivery
DEFER - EVIDENCED with the qualified cost-free refusal. 6C IN PROGRESS;6D-6F incomplete.

Frozen194 source69946c89217d9625b04756899babe050454461f8/treeab208ecaf64ef94afa81dc7c711758dac1975acf is preserved.
Packageb4733f149238ac368a91df5fcb945a4936a3cad761c5cff762e1b88eb80969b4;
DLL28a6764961d5edac9180fefe1fe4267038aa0ad50f5d342226c85ab9d897b104,
MVIDa9d1bdba-b2fd-45ac-88f1-3eb32eec3b7b. Suite20261008-chunk6c-casting-f/
07b27d536f96512b58dd070ebf93af25d3e5b0b02e23382922e52287cdb39913.
Purity PASS actual child20628 exit0,94.32974min; process receipt SHA
4cdb2fe9433ea5739b63f9729e2c62a2b02acf9cc07792138509d74738a87ab7.

Campaign0 qualified PASS/3 FAIL/5 BLOCKED-unrun. RT source native7/0 passed:
the normal exact rod toggle, native item disposal and original-slot restoration worked.
Its original external FAIL is retained: the reader demanded Mounted before Mount and
mistook the normal empty RT pair-identity descriptor for live turn ownership.
Reader re-evaluation3 now passes the complete immutable envelope, with exact product,
producer, fixture, launcher, safety/restoration and artifact hashes unchanged.
ReaderSHAfada86b2fa70513bffee20833a0c489d9cf96641d22806500649a942eee09aa7;
receipt61108b53b4c0fe7dc3a9bf193c33ef33c931c9b7f53172aa0886ba61bb3876c7.
Original worker exit1/result FAIL remain unchanged; no cold prerequisite was reused.
Evaluation1's missing module and evaluation2's exact RT-descriptor failure are retained.

Mounted RT failed2/1 before casting: the exact6C pair was missing from the shared
diagnostic AI-isolation relationship predicate. TB source failed6/1 at stage1:
checking rider CanActInCombat before advancing another actor's native turn made
the existing EndFixtureTurn input unreachable. Neither is a casting product defect.
Only the independent TB source followed the fully restored RT baseline failure.
Cold2/8 are blocked by source verdicts; baseline4-6 by the shared fixture defect.

All3 transactions restored with empty errors, game exits0, worker exits1,
no recovery, no live game or transaction. Human105,275 saves and358 Mods match.
RT/TB sources have exact whole-profile audit receipts; the short baseline has no
profile-cache artifact, so no additional whole-profile claim is made.
Closurec3872d983755b2b4c33a63d8ab6930ae25597691b5553ae5fd9e555e2d3a918e:
lab analysis-cache/chunk6-continuation/20261005-6br/preview194-campaign-closure.json.

Working195 adds only exact6C diagnostic pair admission, a small shared fixture wait
decision that requests the existing guarded native End Turn before rider readiness,
and the phase-aware read-only persistence interpretation. No resource, preparation,
turn, movement, native action, gameplay or save policy changes. The same8-stage
batch remains; no cases added, wrappers changed or historical verdict rewritten.
Focused build1PASS, compiled observer/admission57/0, casting151/0, persistence79/0.
Detached predicate tests keep exact compiled decisions and mock only relationship
inputs; foreign rider/mount and faulted state refuse. Foreign native turns progress
while the rider cannot act; owned/unavailable and RT cases request no extra turn.
Real RT whole-envelope evidence and synthetic full-envelope refusals pin the reader.

FAST1 PASS12/0;CANDIDATE1 PASS16/0,685 code/script/test/version inputs unchanged.
Components710/0, source145/0, safety harness271/0 and assembly698/0 pass.
No195 package/suite/purity/native verdict exists. Next: coherent guarded publication,
one195 package/suite/proof,
then the unchanged reviewed parameterized runner against8 stages, failed source first.
Continue6C-6F. No6C stability, merge/PR/tag/release/permanent installation,
protected-human-save write, foreign-mod mutation or HUMAN PLAY acceptance.

## 2026-10-08 - preview193 terminal fixture failure; preview194 source-complete

6B remains implementation stable: RT supported/default-off Charge; TB delivery DEFER - EVIDENCED with the qualified cost-free refusal. No188 replay or TB experiment. 6C IN PROGRESS;6D-6F incomplete.

Frozen193 source9b0223d92188c18350822bed4ef93ab85c6da3ce/tree708ca54ae5fce74f4e6252dce7c91e6aefc8266b is preserved. Package dd3d542f466bc513d3cb82f89bc9b761ebd03b28997fd5308326b054947df5e0; manifest c39886410c7eeff4a5e11a34a4e7f2dcc6cb5c2182e129a5e41c9134a1bf2d0f; DLL4a9e680da5d0e166cd4374562b1ad660d69f10ad2ea76deebba2139fc4557a2b/MVID844c6157-44d1-4de9-b643-44f0e2bc6c46; suite20261008-chunk6c-casting-e/1ddcdc165f89aa29c8ef3de6198e93314d861450ecd5a6ccd86acc4c0fa40399.

Purity PASS: original retained OS handle observed child25036/start2026-10-08T05:08:01.0865480Z exit0 at06:42:22.6313886Z,94.36min. Exact receipt purity193-e1-process.json/ebc6c46ddfb180fe327c2fad8d96572060941bfc810e61d4b2e6625a7e6a6c85; closure da8a7a39da08b97958f262cc19f1cd6c0e4e1e2cf646628847136f2c28613527. No proof replay or unknown-to-zero substitution.

Campaign closed0 qualified PASS/1 FAIL/7 BLOCKED-unrun. Stage1 c6c-casting193-e-settled-rt-save failed7/1 in shared disposable-item cleanup after native casting, potion, save and ordinary continuation. Worker29052/start06:44:10.7731417Z exit1; game27032/start06:44:53.5373238Z exit0. Request6ab07fbf411f3333d10b649186602454b21ca3441b83e6a06d0b7b21f749da19; tokenba68c4f6d553036c4ea2816599afe150c941a97448a1a0053a8c7ff4d7ebc624. Failed source archive is preserved, not cold eligible. Dependent shared-fixture stages were not replayed.

Closure preview193-campaign-closure.json/834d24d990146ba737429e8de4453563bb9e33eed9ee495583555d19dbb2a7bc binds the eight-row disposition and every actual receipt. Human105,275 protected saves,358 Mods/UMM/settings/profile restored exactly with empty restorationErrors, normal game0, no recovery or active game/transaction. Suite save digest511077a04981fe3657d47eb0fc8727f67ea5602f755575c6b4aae1142974a426 and Mods digestc4e783ebd7776e3dc298528438ebf5097fc7c61b483881d49cf4a14a965a721c are unchanged. The inherited handoff command typo '-Tail5' should be '-Tail 5'; no evidence was rewritten.

The new distinguishing fact is retained native activation failing Active/IsOn despite exact rod and potion absence from inventory/slots. Bounded pinned native IL and the detached exact disposal projection prove native disposal does not clear IsOn. Working194 calls the normal native IsOn setter on only the exact owned activation/source item/UnitDescriptor before native unequip. Foreign source or changed owner refuses before mutation. Existing Active/IsOn/item residency/rod buff/original cohort-slot postconditions remain. Three bounded retained activation fields answer this exact failure. No production casting, action, resource, turn, preparation, relationship, persistence or safety policy changes.

The declared scroll now uses the exact original native fixture AssetGuid cd635d5720937b044a354dba17abad8d, rather than the previously unobserved 32-character declaration. Factory, reader identity and synthetic fixture agree; no tolerance or spending rule changed. Native lookup/use still requires qualification.

Focused regression33/0 preserves both pinned native disposal bodies and disposed-state assertions using the existing Harmony IL copier. Unity component disposal/destruction/logging are deliberately refusing detached boundaries; the full native toggle copies only its exact external clock read. Exact production helper guards, readiness/off state, disposal, unchanged charges and repeated release are tested. No installed assembly, game process or singleton is patched/mutated. Six initial probe failures (Unity ECall and Game initializer boundary) remain immutable. Automatic approval review rejected an edit removing disposal calls/assertions; that edit did not execute. The safer external-boundary mock preserved coverage.

FAST1 PASS12/0;CANDIDATE1 PASS16/0 with all source/script/test/version inputs unchanged: components710/0, observer33/0, casting151/0, settled72/0, source145/0, harness271/0, assembly698/0. Build1 passed. Exact diff/review: lab analysis-cache/chunk6-continuation/20261005-6br/preview194-fixture.diff and PREVIEW194-NATIVE-FIXTURE-REPAIR-20261008.json; final gate binding preview194-candidate-inputs-terminal.json.

No194 package/suite/purity/native result exists at this source checkpoint. Next after coherent commit: invoke C:/Dev/KingmakerMountedCombatLab/codex-policy/Push-KingmakerMountedCombat.ps1 -RepositoryRoot (Get-Location).Path -Confirm:$false; fetch/equality/clean; Package.ps1 -ArtifactQualifier chunk6c-casting-f; new suite/freeze and one required purity; same reviewed parameterized runner and8 stages, failed RT source first. Entry/exit0/7 probes bind unchanged helper bytes. Continue6C then6D-6F while safe.

No merge/PR/tag/release/permanent install/protected-human-save write/foreign-mod mutation/HUMAN PLAY acceptance. Historical failures and frozen candidates remain immutable.
## 2026-10-08 - preview192 terminal fixture failure; preview193 source-complete

6B remains implementation stable: RT supported/default-off Charge; TB delivery DEFER - EVIDENCED with the qualified cost-free refusal. No188 replay or TB experiment. 6C IN PROGRESS;6D-6F incomplete.

Frozen192 source39b1e102e9aa09b6cf3eb7edcdaf20eb5e45b557/treeaee9ce92ceb70a556be267163a62451211703ea3 is preserved. Package3ec3a2094daaabcc8792b70607c3727d61d454ff1ee326b1ed56f7cc10146ce6; manifestb531069b089589055c31a85143432a51205b60a0ef63072a0a09c89d5f88e1a6; DLL601a8f6393b540dfaf76b4c86762d050ee607507b33f1eeb1d3656bb8571531e/MVIDb4d5cf6e-f9e0-49ec-b836-a35230cd31b3; suite20261007-chunk6c-casting-d/e0061b55fbb5326e19a70a03bf088d95c4ed746e3e629a9853845099bd34e851.

Purity PASS: actual child27092/start2026-10-08T02:45:09.4198048Z exited0 at04:19:30.2730773Z,94.35min. Exact independent terminal receipt purity192-d1-process-observed.json/be2f68396003e9e8f01bca9da3600e7263a8c22630d495cea372ef2a5ee4574e; closurec16b7cb11c7968cacf680742ab36e894937cf08d71a90f9e334ffa573e1c419d. Original controller exited1 because it named a nonexistent getter; this is separately retained, never substituted for the observed child exit. Its timestamp-label mistake is preserved with a separate factual reconciliation.

Campaign closed0 qualified PASS/1 FAIL/7 BLOCKED-unrun. Stage1 c6c-casting192-d-settled-rt-save failed7/1 in disposable-item cleanup after native casting, potion use, save and ordinary continuation. Worker25776/start04:21:16.7756019Z exited1; game19168/start04:22:05.9131550Z exited0. Request54e2bdf67b887b2aef9aa1d22ec3b10f4bf5c0dde1d4c447c6118f63f77fc57f; token2bdc3667499126e62a50117790096dccf9a13418c7c39882deb4494fc00b4c9a. No6C native gate or cold-load credit; failed source archive is immutable and not eligible as a prerequisite.

Closure preview192-campaign-closure.json/dbfe3195db795d167609d71fee7f6df23b5daca11808c2925035373820a34042 binds all evidence and restoration. Human105/275 protected saves/358 Mods/UMM/settings/profile restored byte-identically with empty restorationErrors, normal game0, no recovery or active game/transaction. Suite save inventory511077a04981fe3657d47eb0fc8727f67ea5602f755575c6b4aae1142974a426 and Mods inventoryc4e783ebd7776e3dc298528438ebf5097fc7c61b483881d49cf4a14a965a721c remained exact.

The distinguishing native fact is original potion-1019547776 moving from holdingSlot-526014976, outside the rider's five slots, into the consumed owned potion's slot. The owned potion had no collection/holding slot/residency. Rod removal now had no collection/holding slot/activation/residency, proving the preceding two-bool native removal repair worked. The remaining failure is the compiled fixture's incomplete original-placement snapshot and per-item cleanup ordering, not a demonstrated mounted casting product defect.

Working193 captures one original cohort before any acquisition, expands exact owner/body/quick-slot containers for originally held shared-inventory usable items, and retains them through cleanup. Every created item first releases its exact native ownership; only then are original placements restored through native equipment APIs and leases disposed. Early rod release does not restore or re-equip other live/consumed cohort items. No original item is deleted or spent; no charges, spell slots, debt, preparation, turn or native process state is written. Production casting/action/relationship/persistence policy, external acceptance rules and the8 native cases remain unchanged.

Three new behavioral regressions prove other-actor original placement restoration, full-cohort release ordering without item resurrection/refunds, and refusal of a stale owner container before equipment mutation. FAST1 PASS6/0;CANDIDATE1 PASS16/0: components710/0, compiled observer26/0, casting151/0, persistence72/0, source145/0, harness271/0, assembly698/0. Gate inputs stayed byte-identical through CANDIDATE. Initial focused build1 version-source mismatch and build2 JToken/JObject compile error are retained; build3 passed. Exact diffs, source hashes and receipts are under lab analysis-cache/chunk6-continuation/20261005-6br/, especially PREVIEW193-COHORT-FIXTURE-REPAIR-20261008.json and preview193-candidate-inputs-terminal.json.

No193 package/suite/purity/native verdict exists at this source checkpoint. Next exact command after coherent commit: invoke C:/Dev/KingmakerMountedCombatLab/codex-policy/Push-KingmakerMountedCombat.ps1 -RepositoryRoot (Get-Location).Path -Confirm:$false; fetch/equality/clean, Package.ps1 -ArtifactQualifier chunk6c-casting-e, new frozen suite, one required purity, then the unchanged reviewed parameterized runner against the same8 stages, failed RT source first. Reuse exact unchanged entry/exit0/7 probe bindings; no executable wrapper redesign. Continue6C then6D-6F while safe.

The optional direct save-archive enumeration previously rejected by automatic approval review was neither executed nor bypassed. Qualification uses the existing approved guarded save/cold-load machinery. No merge/PR/tag/release/permanent install/protected-human-save write/foreign-mod mutation/HUMAN PLAY acceptance occurred.

## 2026-10-07 - preview191 terminal fixture failure; preview192 source-complete

6B is implementation stable, RT supported/default-off Charge and TB delivery DEFER - EVIDENCED with exact cost-free refusal. No188 replay or TB experiment. 6C remains IN PROGRESS;6D-6F incomplete.

Frozen191 source5abca28b80e25c8292b929c95ba9fb22163d06a7/treea1b9bcca1a7d1fc596cb832633ded14652c5fc2e and all package/suite bytes are preserved. Package3df43ff6040f23932b0c520a97f8142b66ca05888c31926d515c7be1fb0b1d34, DLL2ffa6afb3c6d6552ff856da265508dc25881fa122fb530fee7707ff1692538d9/MVID50f6314a-40fe-475b-a6eb-77c3887866d1, suite20261007-chunk6c-casting-c/d19cde77dd6217dbed9dd6c9857de0ec6ea461566f388d326a289dfd5a5d5a71. Purity actual0,2026-10-08T00:29:18.4298301Z-02:03:55.5251912Z (94.62min); receipt6e2eb41471261cc680ff075081deccedff686c3af7d31f77438001aeec19e72b.

Campaign closed0 qualified PASS/1 FAIL/7 BLOCKED-unrun. c6c-casting191-c-settled-rt-save failed7/1 at disposable native item cleanup; worker25000 exited1, game4488/start2026-10-08T02:07:17.5218407Z exited0. Request2c329ccdd6031b942bbf569615bdcef47ea08ee0bbc3acbce259b1022e4a9b6e/token83d6b09f45f8c48ef71d3b401316f90e7df9f7f2bfe14b36ae19f1245eb561f8. Exact human105/275 protected saves/358 Mods/UMM/settings/profile restored, empty restorationErrors, no recovery/game/lock. Failed native source archive remains evidence only, never cold reused.

Closure: lab analysis-cache/chunk6-continuation/20261005-6br/preview191-campaign-closure.json, SHAd5d5b0af2b3cd67ef82228a73222d2162693b9f2ce5f40a570bcbf16202a3615. Save inventory511077a04981fe3657d47eb0fc8727f67ea5602f755575c6b4aae1142974a426; Mods inventoryc4e783ebd7776e3dc298528438ebf5097fc7c61b483881d49cf4a14a965a721c. Before/after transaction digests respectively2e69c4a4b4611327b0acb09d002a996e42980ff23f2ce256ac3d7dd064d9c74a and02b2efc8c35dc0bbd7b041b6fa1f6f9888b51bf0490ab7b9ca34dc0901977dc0, both equal.

The new native facts distinguish the cleanup defect: consumed exact potion item has no collection/holding slot/inventory residency, while its formerly empty slot contains an original potion; removing the still-resident rod through the one-argument wrapper re-equips it. Read-only installed BagOfTricks.dll d03626594ece0f339aeef03ec7259684af7ddeb8b2edf41936f144848059a0e6/MVID4a0cd3b6-d97c-42e6-a380-c438c3ad55aa confirms the exact Harmony12 one-bool removal patch. Its refill searches the native inventory. Foreign bytes/settings are unchanged. No mounted casting product defect is established by this fixture failure.

Working192 uses native private ItemSlot.RemoveItem(bool raiseEvent,bool autoMerge), token06007C7E/pinned native MVID, with true/false: normal unequip/equipment callbacks remain. Exact captured original quick-slot placements are restored through native APIs; unknown occupants, native removal/insertion failures and changed containers retain the owner. All prior emptiness, item residency, activation and rod buff postconditions remain. No original item is deleted or consumed; no charges, spell slots, action debt or preparation are written. Eight component regressions cover the observed replacement and retained retry debt. The actual pinned native empty-slot refusal is also exercised offline.

FAST2 PASS6/0 and CANDIDATE1 PASS16/0: components707/0, compiled observer23/0, casting151/0, persistence72/0, source145/0, harness271/0, assembly698/0, affected preamble215/0/dispatch35/0/profile53/0. FAST1 private-overload compilation failure is immutable; reflection resolves the exact private boundary. No192 package/suite/purity/native credit exists at this source checkpoint. Production action/casting/relationship/cleanup/persistence policy and external acceptance rules are unchanged.

Next exact command after coherent commit: invoke C:/Dev/KingmakerMountedCombatLab/codex-policy/Push-KingmakerMountedCombat.ps1 -RepositoryRoot (Get-Location).Path -Confirm:$false; fetch/equality/clean, Package.ps1 -ArtifactQualifier chunk6c-casting-d, new frozen suite, one required purity, then the unchanged reviewed parameterized runner against8 stages with the failed RT save first. No new wrapper framework. Continue through6C-6F while safe.

The previous optional direct disposable archive-entry inspection remains blocked by automatic approval review (forbidden save-archive enumeration). It was not executed or bypassed and is not needed for this repair. Existing guarded save/cold-load machinery remains the qualification path. No merge/PR/tag/release/permanent install/protected-human-save write/foreign-mod mutation/HUMAN PLAY acceptance occurred.
## 2026-10-07 - 6C preview190 closed; coherent191 diagnostic repair

6B remains IMPLEMENTATION STABLE - RT SUPPORTED; TB DELIVERY DEFERRED;
final qualification is deferred to Chunk6 consolidation. Frozen188 stays closed.
6C IN PROGRESS; 6D-6F incomplete. Published190 source78a20c2338444016f34a86732d42fc1a8db6bcbd,
tree2f33a448d36e3037a13d16bec917497346d01fb0 and all frozen bytes remain preserved.
190 purity PASS, actual exit0. Campaign closed0 qualified PASS / 2 FAIL / 6 BLOCKED-unrun.
Stage1 ran older CM01/02/03 because the6A classifier reused a wider engine allowlist.
Stage5 separately observed quickened Snowball, potion, a native disposable save and
ordinary continuation; its exact item-cleanup postcondition then failed. No6C
stability or cold-load credit. Both workers exited1, both games exited0; exact
human105,275 protected saves and358 Mods restored, empty restorationErrors, no recovery.
No live game/transaction. Closure e689f34c2364aa1f32d7bbd580c2819decb3834e06dcc1734c781a21c68bb62a
under lab analysis-cache/chunk6-continuation/20261005-6br/preview190-campaign-closure.json.

Working191 separates engine routing from the Mount/Dismount behavior family;
its actual compiled regression fails on190 and passes21/0 on the repair. Fixture
unequip disables native auto-merge, retains exact owners and attempts independent
item cleanup; a failed postcondition records its exact collection/slot/residency.
The native cause still requires the next focused run; offline success is not proof.
Casting reader151/0 and settled reader72/0 pass. FAST191-1 retained an orchestration
FAIL solely from an added nonexistent test filename; its actual affected checks passed.
No production casting/action/turn/resource/persistence policy or acceptance change.
No191 package/suite/purity/native verdict yet. Original190 artifacts remain immutable.

CANDIDATE191-1 PASS16/0 (including FAST), components699/0, casting151/0, settled72/0, compiled observer/dispatch21/0, source145/0, harness271/0 and Kingmaker assembly698/0; actual child exit0. Receipt: lab20261005-6br/6c-candidate191-1-process.json.

Next: coherent commit/guarded
push/equality/clean check, then one191 package/suite/proof. Run the failed settled RT
case first, then affected cold and mounted/unmounted RT/TB baselines. Continue6C-6F.
Use the existing parameterized runner/probe; no188 replay, TB experiment or new matrix.
No merge/PR/tag/release/permanent install/protected-human-save write/foreign-mod change/
HUMAN PLAY acceptance. Direct disposable-archive enumeration was rejected by automatic
approval review and was not retried or bypassed; runtime facts and pinned IL are used.
## 2026-10-07 — 6C launcher registration repaired; preview190 ready

6B remains IMPLEMENTATION STABLE — RT SUPPORTED; TB DELIVERY DEFERRED;
final qualification is deferred to Chunk 6 consolidation. Its frozen188 evidence
and all28 passing stages remain closed and unchanged. 6C is IN PROGRESS.

Frozen189 source f8717c53667b296b4031379ea143dcbc501aca56 and package/suite
are immutable. Its first WhatIf child exited1 before launcher entry because the
Scenario ValidateSet omitted the four implemented6C scenarios: ABORTED — NO
PURITY VERDICT; native0, no game/transaction or external-state mutation.
The failed proof, logs, freeze and original parameterized helpers are retained.

Working 0.1.0-chunk6c-preview.190 adds only those four launcher registrations,
actual parameter-binder/registry regressions, and coherent version stamps.
There is no production casting, action, cleanup, fixture or acceptance-policy
change. The same four15-row mounted/unmounted RT/TB baselines and two settled
save/cold pairs remain the ready eight-stage batch. No rod is yet created.

Focused casting151/0 passes, including13 actual launcher-binding checks.
CANDIDATE190-1 PASS16/0: components699/0, source145/0, compiled observer11/0,
settled reader72/0, preamble215/0, harness271/0 and Kingmaker assembly698/0.
All five gate inputs remained byte-identical. Lab receipt:
20261005-6br/6c-candidate190-1-process.json.
No190 package, suite, purity or native verdict exists at this source checkpoint.

Next: coherent guarded publication; exact190 package and suite; reuse the
unchanged18/0 entry probe, run190 WhatIf purity, then the eight native stages.
Do not alter189 or repeat188. Continue6C–6F with stock native behavior first.
One mutating executor; no active game/runtime transaction. No merge/PR/tag/
release/permanent install/protected-human-save write/foreign-mod change/HUMAN PLAY.
Candidate receipt SHA256 9ea40ef54fb30745065042b0edbded99bfb649a78089b67da98cd1f8fbbf5cfb.

## 2026-10-07 — 6C native baseline ready for qualification

6B remains IMPLEMENTATION STABLE — RT SUPPORTED; TB DELIVERY DEFERRED;
final qualification remains deferred to Chunk 6 consolidation. Frozen188 and
all 28 passing stages/29 restored transactions remain immutable.

Working 0.1.0-chunk6c-preview.189 adds bounded normal-input diagnostics and one
external casting/item validator; it changes no production casting, spending,
concentration, charge, movement-budget or turn policy. The original Druid/Mammoth
fixture gains native-created Lesser Quicken rod, potion and scroll only inside
guarded disposable runs. The rod is not yet created. Four RT/TB mounted/unmounted
15-row baselines and two settled spell/item save/cold-load pairs are ready.
The cold process reads the saved rod and spent spell slot; it cannot provision
items, replay casting or Mount, or continue an in-flight process.

Focused PASS: components699/0, casting138/0, settled persistence72/0,
compiled observer11/0, preamble215/0, parent handoff30/0, persistence contracts187/0,
profile protection53/0, source145/0. FAST2 passed11/0; CANDIDATE3 passed16/0
(shared harness271/0, Kingmaker assembly698/0), with exact input bytes unchanged.
Lab receipt: 20261005-6br/6c-candidate3-process.json,
SHA30674368c8545eead5342207b35ebf589680f24c1b3c6a6c1b6956569656fd83.
Earlier build, fixture, reader, FAST and CANDIDATE failures remain retained;
reader variable collisions and stale schema/caller pins were corrected offline.
These counts are not native gameplay qualification. No189 package/suite/purity
or game run exists at this source checkpoint; 6C remains IN PROGRESS.

FAST now runs build/components and explicit affected gates; CANDIDATE adds the
common safety/manifest and affected assembly checks. Historical 6A archives and
ledgers remain under FULL or an explicit dependency-driven check. The existing
native worker/exit observer has a manifest-parameterized successor in the lab;
original helpers are preserved, and focused process/read probes precede use.
No large extraction or acceptance-framework rewrite is required.

Next: coherent guarded publication, exact189 package/suite, required WhatIf
purity, then the eight-stage native baseline/settled persistence batch.
No source mutation while frozen. Native defects are not yet established;
prefer stock behavior and fix only attributable observations. Continue6C–6F.
No merge/PR/tag/release/permanent install/protected-human-save write/foreign-mod
change/HUMAN PLAY acceptance. One mutating executor; no active game/transaction.

## 2026-10-07 - 6B implementation stable; 6C native baseline next

CHUNK 6B IMPLEMENTATION STABLE — RT SUPPORTED; TB DELIVERY DEFERRED;
FINAL QUALIFICATION DEFERRED TO CHUNK 6 CONSOLIDATION. Owner acceptance pending.
Frozen188 source bc98a9cdf0d1e65b30eaabac517b58479efd3520, tree
 def5cf512bae42a27a892d98ccac208ef75998de: all28 required stages PASS1031/0,
zero blocked/unrun. All29 actual guarded transactions restored human105,
protected saves, Mods/foreign Mods, UMM and settings; empty restorationErrors.
The original Stage15 game-exit observation remains UNKNOWN and immutable;
only15 was rerun, with independently observed worker/game exits0. First14 retained.
Native achievement/analytics cache churn at24/25 passed the unchanged Chunk5
policy; the whole profile was not byte-identical. No game/transaction remains.

Charge stays default-off. RT delivery and the agreed cleanup/persistence matrix
are qualified; TB has the exact cost-free refusal, not delivery. No TB experiment
is reopened. No extra confirmation campaign is required. Full final6F and HUMAN
PLAY remain pending. Lab closure SHA256 bb8d505912ef16113c4f9c86151a3aa0c126979923d41854b55817764f6601fb.
Exact ledger, identities, retained failures and receipt-derived costs are in
analysis-cache/chunk6-continuation/CODEX-CHUNK6-CONTINUATION-HANDOFF-2026-10-07-6B-STABLE.md.

After campaign closure, pure schema/reader dispatch moved into
scripts/runtime/ScenarioDispatchEvidence.ps1 under the existing reader identity.
Protected Common remains excluded; launcher, save, restoration, exact identity
and artifact-byte guards are unchanged. Focused PASS: dispatch35/0, charge502/0,
carrier142/0, shared harness271/0. No version bump, product rebuild, purity replay
or native replay was made for this external interpretation separation.

Continue directly6C: normal mounted RT/TB casting/items first, authorized native
Lesser Quicken rod only in the disposable Druid fixture, exact new fixture proof.
No casting production patch without an observed defect. Rod is not yet created.
Then6D/6E/6F under the owner's charter. No merge/PR/tag/release/permanent install,
protected-human-save write, foreign-mod change or HUMAN PLAY acceptance.
## 2026-10-07 - preview187 closed; bounded188 envelope repair

Phase6B IN PROGRESS;6C-6F incomplete. Frozen187 source
3909cf092568ee0670f0db5cea5c697d66026fc6/treea3670dcfd1dbbfd204a99218c6f7165d5b25f32a
closed **24 PASS /1 FAIL /3 BLOCKED-unrun**,25 native transactions.
All actual game exits0;24 workers0/one1. Exact human105, protected saves, Mods,
UMM/settings and recorded profile bytes restored, empty errors, no recovery.
Fetched remote equals HEAD and worktree was clean at closure; no game/transaction.
Closure c05c60967d5fe0478c849e5423c6d8f096f3a8ed960602192dff6cf8927ce257.
Full28-stage ledger, requests, exact product/reader/suite/proof identities and
restoration receipts are in the lab parent handoff
CODEX-CHUNK6-CONTINUATION-HANDOFF-2026-10-07-PREVIEW187-CLOSED.md
(SHA3184fc36e0d6860400fac2ee7c439b2a2abc3adec3f6db3b678469c20609c97f).

All five pending/success/cancelled/failed/drained charge save/cold-load pairs pass,
as do area/session transitions, disable after drain and removal/noDLL readiness.
Disable and unload refuse unresolved debt; this does not claim successful hot unload.
The retained-reference observer closes with no faults in these native traces.
Historical186 identity ambiguity is not retroactively explained or requalified.
Native null-view restoration remains unqualified187 because its cohort was blocked.

Stage2 remains externalFAIL0/1/native70/0. The shared outer envelope still required42,
while compiled evidence and the dedicated reader required43. Stages3/4/5 are explicit
unrun dependencies, not product failures or inherited PASS. The later aggregate
registration error follows the rejected result; native registration rows remain intact.
The closed campaign and its original helpers, artifacts and verdicts are immutable.

Working188 changes the four exact outer schema registrations and adds file-backed
envelope/dispatch coverage for all five charge scenarios, current preamble obligations,
identity/schema refusal, required rows and unchanged mount-cost prohibition.
No production command, action, movement, cleanup or acceptance-policy change.
Focused charge188-1 retained FAIL reproduces the exact native error after441 existing
checks. After repair charge188-2 passes502/0. Immutable native187 offline replay passes
registration and full charge dispatch2/0; all artifact bytes and originalFAIL unchanged.
Build, components699/0 and source145/0 pass. FAST1 passes26/0; CANDIDATE1 passes38/0,
actual child exit0 and all inputs unchanged. Assembly contracts722/0 pass within it.
No188 package/suite/purity/native result.
The existing pure-reader guard excludes Common.ps1, so one new candidate is required;
there is no retroactive187 qualification or guard exception.

Next command: git diff --check
Then coherent commit and guarded publication, Invoke-Artifact188.ps1/Record-Freeze188.ps1,
one freeze,
required purity and complete28-stage native batch. No repeated87-row6A development loop.
FULL remains for consolidation: four charge-only parser conditions changed, with
the full current envelope, shared harness and historical replay gates passing.
TB delivery stays DEFER - EVIDENCED with exact cost-free refusal in source; qualify
that refusal on the successor. No6B stable claim or6C rod yet; continue6B then6C-6F.
No merge/PR/tag/release/permanent install/protected-human-save write, foreign-mod
alteration or HUMAN PLAY acceptance.

## 2026-10-07 - preview186 closed; preview187 observer repairs

Phase 6B IN PROGRESS; 6C-6F incomplete. Frozen186 source
91cf3c3f9a09d2af1e3d1be877beaeba458d99de/tree7418895f867e68353b3aa77bc1b2b213c9af49c1
closed **14 PASS / 2 FAIL / 12 BLOCKED-unrun**, 16 native transactions.
Every actual game exited0; every transaction restored human105, protected saves,
UMM/settings and foreign Mods with empty errors and no recovery. No game or
runtime transaction remains. No blocked entry has a synthetic native verdict.
Closure: lab analysis-cache/chunk6-continuation/20261005-6br/preview186-campaign-closure.json,
SHA e91f460949a6a5dfca70405a1de76957ef5edab7acbb862a1af0263a846232f9.
The full 28-row ledger and exact product/suite/restoration identities are in
CODEX-CHUNK6-CONTINUATION-HANDOFF-2026-10-07-PREVIEW186-CLOSED.md under that lab parent.

Stage4 failed because the view-call observer rejected the native RestoreView(null)
argument. Stage16 failed because two rider commands had one hash label; the shared
observer did not retain references, so exact identity could not be established.
That shared-observer boundary stopped stages17-28. Hash reuse is a hypothesis,
not a proven explanation. Both failed artifacts and actual child exits1 remain
immutable; neither failure establishes a product charge defect. Native186 TB
Mount and pending save/cold load passed, but their evidence is not transferred.

Preview187 retains each exact observed object for a bounded trace, verifies
ReferenceEquals for repeated labels, rejects collisions, and releases references
at closure while retaining evidence of faults. Command, actor, callback and rule
identities use that one registry. The native view probe now distinguishes its
literal null argument from the view created by the engine. Continuation contractv2
requires a closed/drained registry; charge schema43 requires the exact native
view-callv2 facts. Native command/resource/cleanup policy and all acceptance limits
are unchanged. No new library, action write, refund, preparation or TB admission.

Focused PASS: components699/0 (including twelve observer behavior tests), compiled
fixture/identity adapter13/0, continuation50/0, charge reader441/0, persistence159/0,
ownership111/0, assembly722/0, source145/0 and build. FAST1 PASS26/0.
Retained source187-1 FAIL: version.json still named186; corrected before build/FAST.
CANDIDATE1 PASS38/0, actual child exit0; all inputs stayed unchanged. No187 package, suite, purity or native verdict exists yet.
FULL remains for final consolidation: this bounded diagnostics repair preserves
native hooks, hash labels and production semantics; affected shared readers and
compiled adapters are covered by focused/CANDIDATE checks.

Next command: git diff --check
Then coherent guarded publication, Invoke-Artifact187-v2.ps1, Record-Freeze187.ps1,
one mandatory purity and the complete28-stage native campaign. The unexecuted
Invoke-Artifact187.ps1 retains its old log suffix; its reviewed v2 corrects only
that suffix before any artifact operation. Both files and their diff are retained.
TB delivery remains DEFER - EVIDENCED with exact cost-free refusal; no6B stable
claim or6C rod before6B closes. Continue automatically through6F afterward.
No merge/PR/tag/release/permanent installation/protected-human-save write,
foreign-mod mutation or HUMAN PLAY acceptance.

## 2026-10-07 - preview185 closed; coherent preview186 qualification repairs

Phase 6B IN PROGRESS; 6C-6F incomplete. Frozen185 remains immutable:
16 PASS / 4 FAIL / 8 BLOCKED-unrun; all 20 actual transactions restored exactly,
empty restorationErrors, normal game exits, no live game or runtime transaction.
Closure d25e2eea841c3fc356713b3ae34899ee4bd36de47ea85289cb78ccfa8c42e1bc.
Starting HEAD928589f659c2193859f66a4560ddc1e5a9e79dc4 remains fetched and remote-equal;
intentional preview186 source is dirty. No186 package/suite/purity/native verdict.

The coherent tranche corrects per-actor continuation acceptance (native initiative,
mount ordinary attacks, and native cost after first delivery), directly observes
the exact native view-attachment call, and records reciprocal native ground traces
for an origin-edge fixture failure. Existing clearance/arrival/resource thresholds
and production charge/action/cleanup policies remain unchanged. Reverse-trace
planning is a bounded hypothesis pending native proof; it does not move an actor.
Schema42 requires the new observer evidence; no historical result is requalified.
Focused PASS: components687/0, continuation38/0, charge reader434/0, persistence159/0,
ground plan317/0 and joint ground156/0. FAST1 passed26/0 before the compatibility repair.
CANDIDATE1 retained FAIL34/1 at legacy ground evidence. The forward-only legacy path
is restored: focused ledger record checks87/0 and literal assertions7/0 pass.
CANDIDATE2 PASS38/0 includes the final FAST26/0; all inputs stayed unchanged.

Next: coherent commit and direct guarded push, fetch/equality/clean checks, then
Invoke-Artifact186-v2.ps1 / Record-Freeze186-v2.ps1, one new purity and the28-stage native batch.
Do not execute historical Publish-Preview185-v2 commands. Full185 closure handoff:
analysis-cache/chunk6-continuation/CODEX-CHUNK6-CONTINUATION-HANDOFF-2026-10-07-PREVIEW185-CLOSED.md.
TB delivery remains DEFER - EVIDENCED with qualified185 cost-free refusal. No6B
stability claim or rod before6B closes. Continue6B then6C-6F. No merge/PR/tag/release,
permanent install, protected-human-save write, foreign-mod change or HUMAN PLAY.

## 2026-10-07 - preview185 closed; coherent repair tranche in progress

Phase6B remains IN PROGRESS;6C-6F incomplete. Frozen185 product/harness source
928589f659c2193859f66a4560ddc1e5a9e79dc4, tree6b8d252f6ed33e0517e3a4d9b995d81fbefd9d38,
was clean and fetched/remote-equal at closure. Purity PASS, actual child exit0.
**16 PASS /4 FAIL /8 BLOCKED-unrun;20 native transactions.** All28 plan entries
are accounted for; blocked entries have no process exit, request or native verdict.
All20 games exited0, every transaction restored with empty errors, all recorded
profile bytes exact, no live game/transaction and no recovery needed.

Package250c06ca20ee277fa14dea35a6155d000564cf85c69c8f20e54d8ea661733e4d;
manifestf79c51948f4e5be86369e711e7d1c47e1bdba614976ab68fbd42ac3cf2d79d86;
DLL352a5a273330c8bdec06489cd072cfb2b13e33a633975e3b076a0e272166d537,
MVID3e1e2840-0138-4ae0-8a6f-13e01404e49c. Suite20261007-chunk6b-charge-ad /
c3880fc69a133823b9107d309952fc71a0cf42a576ff7bee0fd292d4479ebae9.
Closure lab analysis-cache/chunk6-continuation/20261005-6br/preview185-campaign-closure.json
SHA d25e2eea841c3fc356713b3ae34899ee4bd36de47ea85289cb78ccfa8c42e1bc
binds every artifact, process, stage classification, transaction and restoration.
Protected saves digest511077a04981fe3657d47eb0fc8727f67ea5602f755575c6b4aae1142974a426;
Mods digestc4e783ebd7776e3dc298528438ebf5097fc7c61b483881d49cf4a14a965a721c;
human105 DLL8e231c388540cee50087ae47a2843bff06c69b6bf668b4a35f0ddfc3844f61a2.

RT core70/0 and interruption71/0 passed. TB exact cost-free refusal64/0, carriers62/0
each, stock safety66/0 each, ordinary controls, area/session/disable/removal8/0 each
and no-DLL cold load12/0 passed. TB delivery remains DEFER - EVIDENCED.
Four immutable failures remain: RT lifecycle native70/0 lacks a required observed
view-attachment frame; TB Mount43/2 fails pre-combat route setup; pending/settled
P04 native16/0 each fail external continuation assumptions about mount initiative
and later ordinary attacks. Original child exits1 remain failures. Two own cold
dependencies and six remaining P04 entries are BLOCKED; no unchanged third attempt.
P07 uses a separate reader and completed independently with exact restoration.

The next tranche corrects per-actor native continuation accounting, directly observes
the missing view boundary and repairs measured fixture setup. Production charge
policy has no attributable defect established by these four failures. No new product
or native credit yet; no stable6B claim. Source and focused tests precede CANDIDATE.
No6C rod before6B closes; no merge/PR/tag/release/permanent install/protected-save
write/foreign-mod mutation/HUMAN PLAY acceptance. All historical failures retained.

## 2026-10-07 - preview184 closed; coherent185 fixture and observation repairs

Phase6B IN PROGRESS;6C-6F incomplete. Frozen184 source
`a72b685391a3300b3616434ca59a0827fef6fecd`, tree
`1490b4522f51dd7a08c6b29626da1bd2ab46dad1`, remains preserved. Campaign closure:
**20 PASS /4 FAIL /2 BLOCKED dependencies;24 actual native transactions**.
The blocked settled/drained cold loads were never launched because their source
saves failed. All26 plan entries are reconciled; not all26 native stages completed.

Closure: lab `analysis-cache/chunk6-continuation/20261005-6br/preview184-campaign-closure-with-blocked.json`,
SHA `87d0320fcb7a23250b302a359a537c8eae102df8ef8a1c714975f15ab0f70331`.
Package `83780c77b8db79563e8c9a0a5506baf4dc7de88a9684786467543f874650c850`;
manifest `697620c782d50bd2f952c9cbe6f2b2955e99ac5bc8d5c725601b568e23383a29`;
DLL `9c4bb2ae8ab753a84d19a9ac00bedd8805de79ad1666014911099d68c12f1509`,
MVID `6fc86a6b-8cf5-4776-84e9-d2479da7f5f7`;
suite `20261006-chunk6b-charge-ac` /
`d6f2b13c523e715b3b76138af48a2c1821ddab2b2a4738078b76ea9ca7578a80`.
Purity PASS, actual child exit0; receipt SHA
`1bbfc00cadb4a26a3e2db313556912c901cd4c67d28290d1a7f42c0a78086c28`.
Full26-row result table, process exits, settled receipts and restoration hashes:
lab `analysis-cache/chunk6-continuation/CODEX-CHUNK6-CONTINUATION-HANDOFF-2026-10-07-PREVIEW184-CLOSED.md`.
Its product label is clarified here as **0.1.0-chunk6b-preview.184**; the frozen
product source and all package identities above are separate from this185 worktree.

All24 actual transactions restored human105, protected saves, settings, UMM and
foreign Mods with empty restorationErrors and no recovery.275 protected files/
3180712448 bytes digest `511077a04981fe3657d47eb0fc8727f67ea5602f755575c6b4aae1142974a426`;
358 Mods files/74540634 bytes digest
`c4e783ebd7776e3dc298528438ebf5097fc7c61b483881d49cf4a14a965a721c`.
Human105 DLL `8e231c388540cee50087ae47a2843bff06c69b6bf668b4a35f0ddfc3844f61a2`.
No game or runtime transaction remains. **The whole profile was not byte-identical**:
cancelled-load's native `achievements.dath` rewrite,12288 bytes, passed the unchanged
Chunk5 native-achievement-cache-settled-size policy. Before
`f0c88979911ec7ba23a5fc95caa613b7a178539440331c170fb066946ad9100e`, after
`b46bddd972c27cc84ae409110f1588f94472c76aefcf1e922e5a4150a709921c`.
Exact receipt: `runtime-evidence/c6b-charge184-ac-cancelled-load/profile-cache-changes.json`.
No manual cache repair or protected-state write occurred.

Four retained failures and the coherent repair:

- RT charge exhausted the unchanged300s host budget before child publication;
  native14/1 and external0/1 do not qualify missing rows. Three fixed cohorts now
  contain every required case; repeated request remains attached to the positive
  delivery. Host interruption records the exact case, samples and FAILED prefix
  before cleanup. Root300s/leaf30s bounds are unchanged; no omitted or relabeled row.
- TB stock safety failed its native origin walk before Charge input (62/2).
  The measured path was interrupted near the Horse with static obstruction.185
  records native route/footprint probes and capsule clearance against every live
  occupant before selecting a normal ground input. No warp or resource reset.
- Settled and drained P04 sources each failed a rounded GameTime prediction,16/1.
  The exact pinned engine subtracts float GameDeltaTime from cooldowns but rounds
  GameTime increments to milliseconds; a fixed10ms oracle cannot prove readiness.
 185 records actual cooldown entry/exit, native round crossing, cost callbacks and
  exact ordinary attack rules. One external validator accounts for all mutations,
  requires two lawful later rounds/costs/attacks and rejects refunds/replay/missing
  events. No wider time tolerance, product action change or synthesized debt.

The new RT cohorts reject diagnostic limitations as qualification. Schemas40
(charge) and41(stock safety) distinguish the new raw evidence. Original fullRT and
historical schemas remain available for historical interpretation. The campaign
will contain28 stages: the previous25 independent controls/persistence/lifecycle
stages plus the three cohorts. No185 package, suite, purity or native run yet.
TB delivery remains **DEFER - EVIDENCED**;184 qualified exact refusal64/0. No new
lawful delivery hypothesis or additional TB implementation experiment is asserted.

Offline185 PASS: components687/0; charge reader423/0; continuation26/0;
persistence159/0; stock reader484/0; source145/0; ownership111/0; compiled fixture8/0;
FAST22/0. FAST completed2026-10-07T00:48:07.6771399Z with unchanged inputs,
log SHA `b5a39e0245e93cc975739d381f4cf8652a32413c238746b561c223da52f75347`.
CANDIDATE1 stopped at the harness exact-schema pin,270/1; the producer correctly
emits40/41 and the old test expected39/26. Only those two pinned literals changed;
all9 assertions in the exact affected test body pass. CANDIDATE2 PASS34/0 with
unchanged inputs; harness271/0, assembly717/0. Completed2026-10-07T01:18:16.4536194Z,
log SHA `38f5820ee73857c1138d6b0a24023fb5299bb76eb6d660fe2d5627cd4dc039bb`. Attempt1
and its unchanged-input receipt remain retained. The first source-validator stale array-shape check and first
stock-reader fixture-indexing failure are retained, followed by passing corrections.
No FULL run. Every182-184 failure, helper, frozen package and receipt is preserved.
No new production integration policy or dependency. No merge/PR/tag/release,
permanent install, protected-save write, foreign-mod change, rod or HUMAN PLAY.
Next: coherent guarded publication, one185 package/observer3 suite,
one required purity proof, the complete ready28-stage batch, then continue6B-6F.
## 2026-10-06 - preview183 closed 22 PASS / 4 FAIL; coherent184 fixture/observer repair

Phase6B IN PROGRESS. Frozen183 source672ea70f2cab947651c1906227cd90f1c7a18048,
tree27428f1aaa756401a2327c8f11603a740ca753f8, purity PASS, all26 stages terminal.
All26 transactions restored byte-identically with empty restorationErrors;
human105,275 protected save files and358 Mods unchanged, no game/transaction live.
Five independent P04 save/cold pairs PASS; area, disable and removal PASS. TB exact
cost-free refusal PASS64/0; delivery remains DEFER - EVIDENCED. RT full-stage FAIL
is accumulated fixture geometry; ordinary TB setup prematurely requests Dismount;
session observer reads a disposed actor; no-DLL observer source JSON gains $id.
The coherent184 repair is source-complete: normal native return movement isolates
RT rows; ordinary controls wait for native availability; disposed session actors
are not re-read; observer3 emits the exact source identity without serializer metadata.
Product action/cleanup policy is unchanged. No phase stable or184 native credit yet.
Offline184 PASS: components682/0, charge reader407/0, persistence reader159/0,
fixture reader30/0, compiled fixture contracts8/0, ownership111/0, source144/0,
FAST21/0 and CANDIDATE33/0. Both tier receipts prove inputs unchanged.
Closure e8c65ed6e7f777aa482bc011abb4d1467bdf8b62830eefb73025e86b763f3531;
full receipts: lab analysis-cache/chunk6-continuation/20261005-6br/.
Handoff: CODEX-CHUNK6-CONTINUATION-HANDOFF-2026-10-06-PREVIEW183-CLOSED.md
under lab analysis-cache/chunk6-continuation. Earlier183 freeze gates682/0 components,
404/0 reader,111/0 ownership,FAST18/0,CANDIDATE30/0 are historical exact183 gates.
Next: guarded publication, exact184/observer3 freeze, new suite/purity and the ready
26-stage native batch. Continue6B then6C-6F; no rod before6B closes.
No merge/PR/tag/release/permanent install/protected-save write/HUMAN PLAY acceptance.

## 2026-10-06 - preview182 closed at shared observer failure; coherent183 repair

Phase6B IN PROGRESS. Frozen182 source220ed73b9f17e86e564ae8277bbed8bfa2594ec0,
tree970948a52982a24f9367fba1709e6ca6d8be132c, package
c21d85a316125d76cce0d8d1ab14da08a8f0e9d9d0ccabd7f7aac02975455269,
suite20261006-chunk6b-charge-aa /
8ad7fcc6d45dcf73a13db8c0e73839aa01709e2d684c0590dc424fb677edb4f7,
purity PASS. The owner-approved successor is exactly two Handle captures,
SHAc44331ac2e51cdbb3410278e33a25f7a0ee2fdab470a358e8ae36190a60bf619;
all eight original helpers remain unchanged. Its exact launch/gate fragments
observed0/7 for worker and observer;7 was rejected. The first probe receipt's
PowerShell list-conversion failure and all probe outputs remain retained.

Stage 1 rerun because the prior child exit was unobserved. Original native63/0
and failed outer wrapper remain immutable and receive no process qualification
credit. Newc6b-charge182-aa-unmounted-rt-rerun1 passed63/0 with observed worker,
observer and game exits0. Stage2c6b-charge182-aa-charge-rt failed native64/2,
external0/1; worker1,observer0,game0. Remaining24 stages are NOT RUN. The required
shared-observer stop overrides the instruction to complete the live batch.

The actual NativeMountedControlService.AbilityGuid switch omitted MountedCharge;
its shared activation ledger emitted <none> while the real shell had the correct
GUID. This is a producer defect, not a rider delivery defect or reader defect.
The positive row measured6.799m mount travel, one rider attack and native Standard
max5.96, but cannot be qualified with the missing required identity. Separately,
all seven walkable18m maximum-range placement candidates had blocked straight
navmesh traces, so the fixture threw before that row. No product rejection defect
is inferred. No new TB hypothesis is established; exact cost-free refusal remains.

183 adds the omitted GUID mapping and a compiled regression covering every control
kind (fails on182 for MountedCharge). The range-only fixture keeps the original
3..20m envelope, walkability and landing clearance, requires distance beyond the
actual slowed mount maximum, and records its native trace without demanding a
traversable charge corridor. The unchanged policy checks range before navigation;
the unchanged external reader still demands the exact range reason and no shell,
cost, movement or attack. In-range delivery rows keep their straight-route rule.
Component regression proves range versus navigation and exhausted-action priority;
reader regressions reject the actual missing identity and wrong rejection cause.
No source action, cleanup, speed, movement budget or preparation rule changed.

All three transactions restored byte-identically with no recovery and empty
restorationErrors. Human105 DLL
8e231c388540cee50087ae47a2843bff06c69b6bf668b4a35f0ddfc3844f61a2;
275 saves digest511077a04981fe3657d47eb0fc8727f67ea5602f755575c6b4aae1142974a426;
358 Mods digestc4e783ebd7776e3dc298528438ebf5097fc7c61b483881d49cf4a14a965a721c.
Exact per-file identities, both Stage1 attempts and all26 explicit rows are in
lab analysis-cache/chunk6-continuation/20261005-6br/:
preview182-campaign-process-reconciliation.json
SHA7bbb561a7697f6c46d31584c9ae373cde57b2d36ffcd445e3dea722f1e384233;
original-plan closure028139ec169f5ed64cc950581fa387bf957dcfef6d75168bed3e7acdfdb5b1ba.
The process reconciliation selects the new Stage1 receipt; it does not relabel the
original wrapper or infer its missing OS exits from its internal receipt.

183 build PASS/source144/0, components682/0, compiled ownership111/0.
Charge reader404/0, FAST18/0 and CANDIDATE30/0 pass. Candidate attempt2 completed
2026-10-06T17:41:55.9102171Z, log SHA
74a3c14a08ca367900fffe35f93bc4c3339828745591c770bc11dbbf6d375640.
Build183-1's version-source mismatch and the negative GUID regression are retained.
Candidate attempt1 failed at child launch because the host supplied identical Path
and PATH environment keys. Attempt2 normalized those identical keys in its own
process only; no user/system environment or source guard changed. Both logs remain.
No183 package/suite/purity/native launch. Next: coherent guarded publication,
new183 package/suite, required purity and the full ready26-stage native campaign.
All6B-6F phase exits remain incomplete.
No merge/PR/tag/release/permanent install/protected-save write/foreign-mod change,
rod/6C fixture change or HUMAN PLAY acceptance occurred. One mutating executor.

# Chunk 6 continuation — 2026-10-05

## 2026-10-06 - preview182 exceptional native action cleanup IN PROGRESS

Published181 HEAD beb42edc05e95cb54cadb4cb12e7d8619e200f15/tree
2dbe350450015bae12910e5171001a7feec3c743 is preserved, remote equal at intake.
181 remains immutable UNQUALIFIED: purity ABORTED - NO VERDICT, native0/26.
The actual native/Harmony12 exception probe stranded its action fence. Closure
23e36ae6c81d86a6449a832dd6586276f1514d6aac3ebaa49366adcf529607ce
proved275 saves/358 Mods unchanged, human105 intact, no process/transaction,
empty restorationErrors and no recovery. All prior failed evidence is retained.

Candidate182 source closes the pinned native OnAction scope in finally. The existing
charge owner retains the exact rule, original executor, shell and registered
process through exceptions before native field assignment. Observation failure
remains retryable debt; delivery retires before native completion. No action,
turn, preparation or native process state is fabricated. Real compiled native
exception/return and process-registration fault probes pass ownership105/0;
components681/0, affected assembly693/0 and charge reader401/0 pass. FAST18/0 and CANDIDATE30/0 pass; candidate gate includes affected Both assembly717/0, harness270/0, persistence reader156/0 and source144/0. Final gate completed 2026-10-06T11:39:58.9555535Z. No182 package/suite/purity/run.

The native Tick commits IsActed and charges cooldowns AFTER OnAction returns.
Two new RT fault rows therefore require zero pre-commit cost, exact registered
process ownership where applicable, terminal drain and no later attack. Existing
post-queue failure/P04 save-cold rows retain their separate post-commit contract.
Ready batch:26 stages,29 RT charge rows, exact TB refusal; no extra TB experiment.
Lab detail: analysis-cache/chunk6-continuation/20261005-6br/
PREVIEW182-ACTION-EXIT-REPAIR-20261006.md. Next: coherent
guarded publication, one182 freeze/purity, entire ready6B native batch, then6C-6F.
All phase exits remain incomplete. Authorized native Lesser Quicken rod for the
disposable6C Druid remains uncreated. No merge/PR/tag/release/permanent install,
protected-save write, foreign-mod mutation or HUMAN PLAY acceptance. One mutator.

## Previous checkpoint: preview181 repair tranche, 2026-10-06

Frozen180 campaign closed4 PASS/2 FAIL/20 NOT RUN; all six game processes exited0
and all transactions restored byte-identically with empty errors, human105 and
275 protected saves unchanged. All six passed the former179 startup crash boundary.
The shared6B-R mode hook canceled ordinary commands after a native unchanged-mode
refresh.181 now preserves that no-op; the regression fails on180 and passes on181
(compiled ownership84/0, build PASS). Real transitions retain the ownership barrier.
The coherent tranche also owns the exact native manual attack target through
cleanup, fixes RT blocker observation timing, and pins the loaded Beast Shape
surface (five base components plus eight COTW-added native immunity listeners).
Request-specific admission evidence replaces the stale shared feedback assertion.
No AI lease rewrite was made: the inspected gap was product-owned target residue.
Focused build/components681/0, ownership88/0, surface60/0, assets54/0, charge reader
373/0 and persistence reader156/0 pass. FAST18/0 passes. CANDIDATE attempt1 retained
one stale schema36 source pin (harness269/1); exact schema37 pin corrected.
CANDIDATE attempt2 passes30/0 (harness270/0). No181 freeze exists at this source
checkpoint. Next: guarded publication, one181/charge-z freeze/purity and all26
ready native stages. Record exact identities in the lab freeze receipt.
The immutable180 closure,
source/package/suite identities and exact next steps are in the [resume](../AUTONOMOUS-RESUME.md).
No phase is stable; continue6B then6C-6F. Historical failures remain unchanged.

## Previous preview180 source checkpoint

179 source f1675762ee435125475b12e31f10ccf3fce4725e was published/frozen and proved
WhatIf-pure. Charge RT and ordinary unmounted RT then crashed during Working load,
before rows, with the same Mono access violation. Both guards restored all external
dimensions; fresh audits rehashed275 saves/358 Mods, human105 intact, empty errors,
no recovery, game or mutable transaction. Original FAIL receipts, logs, crash reports
and frozen inputs are retained;24 stages NOTRUN. No third unchanged179 launch.

Native AddBuffInternal/AddEnchantment call their OwnedFactCollection.AddFact base
nonvirtually.179's wrappers instead call virtual overrides that reenter acquisition.
180 retains typed delegates to the exact pinned native base implementation using
nonvirtual IL call semantics. Native gain events and all lease scopes remain intact.
The real wrapper regression fails on179, passes on180 for both collections: exact
arguments, one native base call, zero override reentry, native failure identity and
closed scope. Build PASS; surface55/0, components681/0, assets45/0, charge reader360/0,
persistence155/0, FAST18/0 and CANDIDATE30/0 pass. One freeze/purity/complete26-stage
batch follows. A repaired native load must still confirm crash attribution.
See [newest resume](../AUTONOMOUS-RESUME.md) for all exact identities and failures.
All6B-6F exits remain incomplete. The native Quicken rod is authorized only for the
disposable6C Druid fixture and has not yet been added.

## Previous preview179 loaded-buff checkpoint

Published HEAD `aff3995b228e68b266368b2f3fd03f491b014c55`, tree
`12445990d831e56242f34089c90e83d9cea5b3bc` was guarded-pushed/fetched equal.
Frozen178 purity PASS, native NOTRUN: its three-component guard rejects the
installed enabled COTW augmentation. Its package/suite/receipt are immutable.
See the newest [resume](../AUTONOMOUS-RESUME.md) for exact closure identities.

Working179 extends the existing charge lease with a finite lifetime graph for
the inspected COTW children, two native weapon enchantments and Flaming FX.
It preserves the native actions and target consequences. Exact acquisition,
callback scope and postcondition observations retain debt through partial
activation, removal, fade and pool return. A stale native FX reference cannot
destroy another pool user's generation. No action/turn/resource state is written.
The new RT child-cleanup fixture also exercises the window before parent storage.
Schema36 and the single external validator require the raw native cleanup facts.

Focused build PASS; components681/0, compiled surface47/0, persistence reader155/0.
These are offline evidence only. Full source/affected assembly/asset gates,
FAST18/0 and CANDIDATE179-2 30/0 pass. A final reviewed FX capacity guard now
reserves slots before nested callbacks and refuses acquisition after a fault;
CANDIDATE179-3 passed30/0 on the complete source at2026-10-06T05:11:54.7490982Z
(10.54 minutes). Log SHA256:
e2766212124b1090aa1616cf15bd9d43ec710e3a6c183adad86f8a8a5680c99e.
Next is coherent publication, one freeze/purity and the ready26-stage6B native
batch. RT now27 rows; TB retains the exact defer refusal. Post-commit product and
harness identities will be recorded in lab candidate179-freeze.json and later
immutable receipts, without changing HEAD during qualification.
All failed attempts remain in lab analysis-cache/chunk6-continuation/20261005-6br/,
including179 build2/4/8/9, detached surface4 Unity ECall limitation and source1's
missing shared row registration (repaired). Also retain FAST1 version metadata,
CANDIDATE1 exact source inventory and surface8/10/11 detached test failures.
No179 package exists yet.
No live game/transaction, external write or recovery. Last closure rehashed275
saves/358 Mods unchanged with human105 intact and empty restorationErrors.
No Chunk6 phase is stable; no merge/release/permanent install/HUMAN PLAY claim.

## Previous preview177 retirement and preview178 source checkpoint

Published HEAD `225cf2d932e27d0a2fc3e9e735867959fa548f9d`, tree
`7ba903864b97f4960e709eff5771b2f9e6a686e1`, guarded-pushed and fetched equal on
the integration branch. Its immutable177 package is
`aaad1bbca12643e19bb58370f5e6d79c44e40e5ceb93361bab862068bf824569`;
suite20261005-chunk6b-charge-v is
`41779a4aa048ae2325ba34320e6dc16e14dae16d67ec681901aa924d43185e23`.
**177 is unqualified: native NOT RUN; purity ABORTED, no verdict.** A read-only
review during the proof established that its AddStatBonus-only admission guard
rejects the actual ChargeBuff. The exact owned proof process5076 was stopped
after recording the defect, exit-1; its partial log and process receipt remain.
No native/install/save transaction existed. Retirement verification passed:
275 save files digest511077a04981fe3657d47eb0fc8727f67ea5602f755575c6b4aae1142974a426
and358 Mods digestc4e783ebd7776e3dc298528438ebf5097fc7c61b483881d49cf4a14a965a721c
are unchanged; human105 intact, game/worker absent, restorationErrors empty.
Receipts: lab analysis-cache/chunk6-continuation/20261005-6br/
`candidate177-freeze.json`, `preview177-proof-retirement.json`,
`preview177-retirement-restoration.json`, `purity177-v1.log.process.json`.

Working178 keeps the architecture and ready26-stage batch below. The actual
ChargeBuff f36da144a379d534cad8e21667079066 owns AddStatBonus(AC-2),
AddCondition(StealthForbidden40), and AttackOfOpportunityAttackBonus with its
authored non-opportunity +2 context value. The new bounded installed-asset test
binds their ordered PPtrs, managed MonoScripts and values to both asset hashes.
Compiled admission requires that exact surface. Token-pinned component call-site
wrappers execute native AddCondition/RemoveCondition once, preserving exceptions
and closing observation scopes in finally. A read-only UpdateStatusEffect entry
observes each mutation before callbacks. Expiry remains observed until the exact
owner drains. Foreign contributions may change the shared signed-byte counter;
no counter assignment, restoration-to-intake or blind decrement is permitted.

Fact removal is never replayed while awaiting residue. Exact component/listener,
modifier/list disposal, condition deltas and current native rule stack settlement
are required before release. The buff removal itself refuses a foreign thread.
Rule context is resolved afresh because native outer dispatch replaces it; no
new rule-dispatch hook is required. Schema35 charge artifacts and the persistence
reader require these raw observations, including partial-application cleanup.

Focused progress: components669/0; actual asset36/0; compiled ownership73/0;
charge reader354/0; persistence reader129/0; Kingmaker assembly653/0. These are
offline proofs, not native qualification. Source143/0 and persistence
contracts187/0 pass. FAST178-1 passed17/0 after2.17 minutes, including all
directly affected readers and registration. CANDIDATE178-1 passed29/0 after
10.69 minutes, including harness270/0 and immutable regression checks. Its log
and immutable process receipt are `candidate178-1.log` and `.log.process.json`
in the same lab evidence directory. Retained178 failures:
build1 (IEnumerable Count syntax), ownership1 (Unity fake-null comparison),
ownership2 (detached Game singleton requires Unity), assembly1 (test array
separator). Corrected against native identity; originals remain in the log root.
This source checkpoint precedes package/suite/proof178. Next: coherent commit
and guarded push, immutable178 freeze/proof and complete6B batch. Record exact
freeze/campaign identities in new lab receipts without a documentation-only
product commit during the frozen campaign.
No phase stability, merge/release/install/protected-save write or HUMAN PLAY claim.

## Earlier source177 checkpoint (historical)

**IN PROGRESS — phase 6B-R.** The owner's autonomous Chunk 6 continuation authorizes
repair of the preview.176 persistence blocker, then stable 6B, 6C casting/items,
6D staged actions, bounded 6E reaction feasibility, and final 6F consolidation.
It supersedes the earlier stop-at-persistence and 6B-only scope. No merge, PR,
tag, release, permanent install, protected-save write, foreign-mod change or
HUMAN PLAY acceptance is authorized. One executor owns all mutations; reviewers
are read-only. Development uses FAST/CANDIDATE, with FULL at consolidation or a
proven shared-foundation requirement. No new candidate exists yet.

Intake: clean `codex/mounted-combat-phase3f-playable-core` at
`56d4f1110191a555cb4c592e6cc620117d89afe3`, tree
`dd2ab3d367959abd6f1d4052056300619635bca6`, fetched remote equal. Product source
`40c40e4510c93c7c41d7656a870ccbffa34dd849` remains an ancestor. The independent
read-only intake audit passed 176 checks: all 441 historical artifacts, 275 save
files and 358 Mods files unchanged; no nonterminal transaction. Human preview.105
is unchanged. No game/build/qualification process was active. Evidence directory:
`analysis-cache/chunk6-continuation/20261005-6br/` under the lab root.

The frozen preview.176 package/suite and its 5 PASS / 1 FAIL campaign remain
immutable. See [the exact audit](CHUNK6B-PREVIEW176-CONTINUATION-AUDIT.md).
The TB failure has no new distinguishing implementation hypothesis: every
carrier was interrupted before its first tick, and lease cleanup completed.
The working source restores the exact TB refusal and Acting-only delegation.
**CHUNK 6B TURN-BASED DELIVERY DEFER — EVIDENCED** is the implementation
disposition; native refusal qualification on the new candidate is still TODO.

## Repair contract

One charge owner begins at exact native shell admission and retains the original
rider/mount containers, target, relationship generation, shell/process/context,
command, carrier records and lease. Cleanup first retires delivery, then uses
the existing independent-step compensation runner. Failure retains the owner;
each bounded update and requested lifecycle barrier can retry. Native terminal,
exact queue/slot absence, no scheduler registration, stopped carrier/path,
restored lease and ended shell process are all required before release. Cleanup
does not advance a process or write an action, preparation or turn resource.

Native `UnitCommand.Interrupt` stamps its result before `OnEnded` and will not
retry that callback. Charge termination therefore reaches base termination in a
nested finally even when cleanup fails. Native `InterruptAll(predicate)` removes
only exact commands without promoting unrelated queued work. Applied agents,
views and rider state are retained, rather than rediscovered from a replacement
relationship/view.

The save barrier precedes native enumeration, holds admission closed for the
exact operation, checks again before each native step and before header capture,
and releases only at terminal teardown/worker drain. The pinned native header
block catches exceptions and can continue to its worker, so a header-only throw
is insufficient. Unstarted disposal, readiness failure and timeout must also
release their exact fence. Load, area transfer, disable and unload refuse while
that fence or unresolved cleanup remains.

## Current implementation checkpoint

The coherent next source candidate is preview.177 (not yet frozen). Preview.176
and its failed TB delivery remain immutable. No new native run has occurred.
The charge owner now covers shell admission, native action reentrancy, all
carrier generations, exact containers/slots, scheduler registration, lease,
buff acquisition, process termination and compensation. Failed buff removal
callbacks retain ambiguous debt; unknown buff component shapes fail closed.
The actual native Charge buff component inventory is still to be measured.

Native mode/party-combat notifications and exact pair removal wait for charge
cleanup. One bounded notification queue retains one-shot destroyed-actor
retirement as well. It never replays a partially executed native callback.
Tick and TickTime are fenced while a notification remains pending. Notifications
also wait for active archive serialization; the same already-captured save may
finish while they wait, whereas a new capture cannot cross them. No action,
preparation or turn value is written by these adaptations.

The ready native batch comprises:

- RT Mounted Charge: 26 rows, including maximum range, repeated input, new landing
  obstruction, lease application failure, ownership invalidation, native Dismount,
  mode change, feature disable, genuine native rider view replacement, native mount death/recovery and permanent rider
  death, alongside the prior qualified refusal/interruption/control rows.
- TB: default-off and the exact qualified cost-free refusal; carrier RT/TB and
  stock mounted/unmounted Charge safety RT/TB remain required.
- P04 source and separate cold processes: pending save, settled delivery, native
  cancellation, post-commit failure, and held cleanup fault drained before save.
- P07: held cleanup debt across native area reload, menu/world replacement,
  registered disable/unload refusal and later settled disable, and removal.
- Removal's own cleanup archive in a fresh process with the product DLL absent:
  separate observer records both actors' commands/processes/path/buff/flags and
  attacks from before load through settled ordinary movement. Its external
  reader binds the exact archive, source observation bytes and receipt chain.
- Affected ordinary mounted/unmounted controls and restoration of human105,
  all foreign Mods/settings and protected saves after every guarded transaction.

The lifecycle observer captures native action debt inside the successful retry,
before world replacement can erase that observation. Removal separately proves
that a finished command with retained charge debt still blocks readiness.
Permanent rider death is last in the RT tranche and retains its exact native
life-controller witness through final selection restoration; no resurrection is
performed. Mount recovery belongs to the native encounter policy.

The view row uses installed Kingmaker BeastShapeIBuff
`00d8fbe9cf61dc24298be8d95500c84b`, prefab
`0dc0f602a83a2034ba5842f73c0012c1`, with its five authored native components.
Read-only serialized object93481 (offset158494840, length236) in
Kingmaker_Data/sharedassets1.assets established that identity; file SHA256
`cc779caf2fef21d111856a57d40b510677b0c236ad2723d1849564626445f785`.
Native Polymorph.TryReplaceView (06002A08) attaches the replacement before native
interruption/old-view retirement; RestoreView (06002A09) restores the stock body.
The fixture records exact native attachment stacks, view retirement and native
buff/equipment/stats/facts/voice/bone restoration. It never assigns a view or
calls a lifecycle subscriber. This is source-complete, not native qualification.

Latest focused checks: build25 PASS; components8 662/0; ownership adapters15
58/0; charge reader11 350/0; persistence reader6 117/0; persistence contracts4
187/0; source10 143/0; Kingmaker assembly contracts4 640/0; causal reader1 241/0.
CANDIDATE4 passed all 28 stages, including the current FAST/focused checks,
harness270/0, pinned assembly contracts, affected regressions and immutable replay.
It exited0 after10.4 minutes. CANDIDATE1 retained a missing new-row registration;
CANDIDATE2 retained a stale harness source assertion (269/1). Both were repaired.
CANDIDATE3 passed those gates but failed when a child process inherited duplicate,
identical Path/PATH environment entries. CANDIDATE4 normalized only its process
environment after ordinal equality verification; no system setting or guard changed.
The independently rerun ledger passed9/0. All three failed candidate logs remain.
All attempts remain immutable under analysis-cache/chunk6-continuation/20261005-6br.
Recent failed attempts: source7 (stale source pattern), assembly2 (incorrect
expected native namespace), ownership14 (missing settings in detached fixture).
Each was corrected against source/native evidence and rerun; no native run occurred.

Next: coherent commit and guarded push, then one package/suite/purity cycle and
the entire ready native batch. The actual
native Charge buff component inventory must still be observed on that candidate.
Do not infer 6B stability from offline counts. Continue through 6C-6F once 6B
satisfies its phase exit; no later phase is claimed implemented here.
Voluntary Dismount now carries the existing native relationship command proof,
including its own Move action cost and command/process identity. The charge cost
remains separately constrained; the reader rejects duplicate billing, refunds,
foreign action identity and cost before Dismount. The fixture waits for both
transactions to settle before recording the row. No production cost rule changed.
