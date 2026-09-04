# Phase 3E Paired-Command Scheduler Implementation

Status: IN PROGRESS

## Dev.8 finding and dev.9 correction

Clean guarded-published dev.8 commit `35eedf9cf9f092f77c65a20ff9ea580702900032` produced package/manifest/DLL SHA-256 `bb2311b6c1da6e66ba0f22b0b9b68f2169d9e1b57b99eaec216deb6f292bc860` / `31bb9c7b87aa8afe67d83f433f22fb4afb4c09b6c29b5f5fe73e80a731676d1f` / `a7288d4548813951529a3fe835e3a0226fbbd567c1d5083f65d98507a9c172bc`, MVID `b2eee65b-c935-4043-adc5-bdf21eabc610`. Suite `20260904T211500Z-phase3e-dev8-horse-tb-suite7` / `34a702dd9f6684f3a00f0ac4e8c09b1b930e18ee1e53e26c2d7754eff1bc8086` and full-continuity WhatIf passed.

Audited live `20260904T221700Z-phase3e-dev8-horse-tb-gate2` failed `42/2` in the Mount admission frame. Natural rider turn and all recorded readiness predicates passed. The exact native click/cast lifecycle admitted a rider-owned, Horse-targeted, unqueued Move-slot `UnitUseAbility` with `AiAction==null`, but stock left `CreatedByPlayer=false`; the diagnostic incorrectly required true. It stopped before stock could transition `Preparing -> Acting`, enumerate the shell, or reach any paired scheduler lease.

Dev.9 changes no production scheduler code. It pins `OnClick` `0x060093F6`, `CreateCastCommand` `0x06002725`, `UnitCommands.Run` `0x060026B2`, and `CreatedByPlayer` field `0x04001A72`; schema 3 proves native click/cast event origin, requires the rider shell's false flag and null AI action, and rejects contradictory fixtures. The separate KMC-created mount Standard attack remains explicit `CreatedByPlayer=true` and non-AI. Historical schema 1/2 remains accepted.

The complete dev.9 pre-commit gate passes source `22/0`, Release build, component `315/0`, visual/source-order `18/0`, harness/protocol `242/0`, and exact assembly `402/0` (`378` Kingmaker + `24` Wrath), with PowerShell/JSON parsers `26/0` / `7/0` and diff validation passing. Candidate DLL SHA-256/MVID are `5f3af2d7949dc674513cbef8297f8cfd70049d515b1c15873dfb6909b48d9bd2` / `72c99d26-4420-437a-86d2-2b331b7aa69a`.

## Primary architecture checkpoint

Phase 3E uses Option A: an exact native eligibility extension for one reference-identical KMC mount-primary command. The production implementation is anchored at `20d71e5a5b64b85b1815e9ea0c00ff3d7f03dd4e`; the qualified clean dev.4 package is bound to published commit `27e088b4dafe4d449127b5e2920f09b3a0ed4f79` on `codex/mounted-combat-phase3e-paired-scheduler`. Fresh same-package Pass A/B now qualify the minimal vertical slice at `140/0`.

The selected seam is installed Kingmaker `Kingmaker.Controllers.Units.UnitActionController.TickCommandTurnBased(UnitCommand)`, metadata token `0x0600911D`. The existing Harmony12 postfix delegates immediately to `UnifiedMountedTurnCoordinator`, which delegates the reference-exact command to `MountedPairCommandScheduler`. The scheduler may change only that call's returned Boolean from false to true. It does not call `UnitCommand.Start`, `Tick`, `UpdateCooldowns`, or `StartTurn`; it never writes `CurrentTurn`, `CurrentTurn.Unit`, turn status, command result, or a cooldown.

This seam is justified by the immutable dev.1 observation: stock visited the exact awake Mammoth Standard-slot command `2,485` times while the rider remained the exact native current unit in `Preparing`, and returned false every time. The implementation handles those two exact false inputs—mount executor identity and untouched-rider `Preparing`—without creating a second turn or a rider-owned gameplay shell. Every later target, range/LoS, hands, equipment, animation, action, rule, result, resource, and slot-removal stage remains native.

## Service and state ownership

`PairedCommandSchedulerLeaseStateMachine` is a Unity/Kingmaker-independent domain object with states `Idle`, `Registered`, `AwaitingStart`, `Running`, `Finishing`, `Interrupting`, `Completed`, `Faulted`, and `Disposed`. `MountedPairCommandScheduler` is an injected runtime-only integration service; no Harmony patch field, fact, `UnitPart`, blueprint, campaign-save object, or serializer state owns a lease.

One lease binds the exact rider and stable ID, exact mount and stable ID, monotonically increasing mounted-relationship generation, exact rider `TurnController` reference and round, exact KMC command reference, mount Standard slot, action origin, target ID, natural-weapon ID, creation/admission/first-grant/last-drive/start frames, expected mount resource and rule ownership, terminal result, resource transition, and cleanup/fault reason.

Admission requires one mounted pair, scheduler and unified gates enabled, TB mode, exact rider `CurrentTurn.Unit`, native `Preparing` or `Acting`, awake mount present in `Game.State.AwakeUnits`, empty mount Standard and queue before `UnitCommands.Run`, one unstarted player-created KMC `MountedPairAttackCommand`, mount executor, Standard type, and exact post-run Standard-slot reference with no queue entry. AoO, Free, AI, foreign, queued, replaced, stale-generation, stale-turn, wrong-rider, and wrong-mount commands are ineligible.

The state machine grants at most once per Unity frame and records one start, terminal result, interrupt, mount Standard charge, and cleanup. A per-frame integration check also catches gate, pair, generation, mode, turn, executor, slot, queue, awake-list, or command-origin drift even if stock stops visiting the command. An unexpected stock-true result for a leased cross-actor command is forced false and faulted rather than trusted. Fault cleanup interrupts only the exact leased KMC command, disposes the lease, retains the reason, and never rewrites the persistent setting.

`EnablePairedCommandScheduler` remains independently default false. `EnableUnifiedMountedTurn=false` remains the accepted Phase 3C separate-turn fallback and never requires a scheduler lease. Phase 3D real-time routes do not require a lease and remain unchanged.

## Diagnostic evidence contract

The exact Mammoth TB row now emits combat evidence schema 56 with a `pairedScheduler` object. It binds command/turn/pair identities, generation, lifecycle frame/count cardinality, retained executor/slot/current-rider invariants, before/after rider and mount Standard state, expected resource/rule owners, native turn statuses observed, rejection/fault/cleanup reason, and final disposal. The scenario alone temporarily enables the experimental gate and restores its prior value in cleanup.

Schema 56 requires one grant per observed frame, one start, one terminal result, one mount Standard charge, zero duplicate-frame drives, zero foreign adoption, unchanged rider Standard, retained mount executor/slot, exact rider current throughout, no fault, and exact cleanup. Historical schema 55 remains accepted with the complete property set the dev.21 runtime actually emitted.

An actionable frame is now anchored at `firstGrantFrame`, the first frame where native `WaitingForUI` and every other hard gate have cleared and the pair-local scheduler returns eligible. Registration/admission may occur earlier without drive. Schema 56 requires `admission <= firstGrant <= start` and `start - firstGrant <= 2`; it continues to reject a start three or more frames after eligibility.

## Audited dev.2 runtime attribution

The dev.2 package is `C:\Dev\KingmakerMountedCombatLab\artifacts\KingmakerMountedCombat-0.1.0-phase3e-dev.2-paired-scheduler-vertical-slice-diagnostic.zip`, commit `20d71e5a5b64b85b1815e9ea0c00ff3d7f03dd4e`. ZIP/manifest/DLL SHA-256 are `70a060cc287fa379de35cb02337ba7e2a3348788db54374e4d3e6f6efd5a852d` / `4dc7fa9ba3ab246b939035f3f6f1bd9fc4ab9eb2f3a52aeeb7d6d0a1d4c6b3d2` / `d71529ba6006bc6fa2c8916953cb773f1c5319e49ce4a0c9d70420e1aee26d87`; MVID is `89de0fcc-ca6f-41cd-944b-097a5860716c`. Suite `20260904T084500Z-phase3e-dev2-paired-scheduler-suite1` / `3349e429905be71e2bde6db01c4788647c890cfea8b947d1c8d527eebaf2920f` and its full-continuity WhatIf passed.

Live run `20260904T094306Z-phase3e-dev2-mammoth-tb-passA` admitted at frame `3982`, first granted at `4293`, started at `4294`, and last drove at `4527`. It completed one exact Mammoth-owned `Success` chain with one attack, roll, damage event and Standard charge, no rider cost, rider current retained, no native Mammoth turn, no duplicate/foreign drive, exact slot/executor, and fault-free cleanup. Immediate independent audit passed exact suite save/Mods/package/fixture continuity before evidence inspection.

Its one failed assertion subtracted admission from start across native UI staging. Dev.3 changes only the in-game and external diagnostic expressions to subtract first grant, adds a realistic delayed positive fixture and a three-frame rejection mutation, and leaves scheduler production source and schema shape unchanged. This is not a scheduler repair cycle and the dev.2 status is not changed.

## Audited dev.3 runtime attribution

Clean published dev.3 commit `1960bd12acd4976b762185064c058896db3aa376` produced package/manifest/DLL SHA-256 `6f8d8e82e4f1f0b19e6eaa3ee9d6edee763fc91ade1018c28303c03446342e52` / `b93209e4f5d0d4c07c18e8b6dd92e70e1ece87ca42353a257968c2022c01ec05` / `933fc2107a3cd3579b81e6db872139cdff251966fa57aa1dec354918370016bd`, MVID `88dfb6c3-d896-4f71-9dc0-a40f2891925b`. Its stable suite snapshot is `f1edd88a8a86bb89d64e62141bb1dfb3fee209b8e05910cdb56c153a1ab0c086`; full-continuity WhatIf passed.

Run `20260904T113800Z-phase3e-dev3-mammoth-tb-passA` is outer `FAIL`, game `PASS 70/0`. It admitted/granted/started at frames `3987/4309/4310`, drove `234` unique frames through `4542`, and proved the same exact command, rule, weapon, resource, turn, and cleanup ownership as dev.2. Independent audit-before-read passed exact suite/save/Mods/Baseline/Working continuity.

The only outer failure was a latent schema-55/56 validator fixture requiring raw action-actor `CanActInCombat=false`; all three real runs dev.21/dev.2/dev.3 recorded true. Dev.4 requires true at entry and dispatch and reverses that exact synthetic mutation. Direct validation of immutable dev.3 evidence and the complete harness pass. No production file, schema field, threshold, scheduler behavior, or repair-cycle count changes.

## Qualified dev.4 runtime checkpoint

Clean published commit `27e088b4dafe4d449127b5e2920f09b3a0ed4f79`, version `0.1.0-phase3e-dev.4`, produced ZIP/manifest/DLL SHA-256 `c6636c54eaee15bc1ab7c1c72a867dd0d0bc9ff62ae14d3a62dbd61672da3d7a` / `de999807ffa2114a5b9468c1679b73c5da757329105464a606ef8eb5ce1945aa` / `7f17fbc50ad282eef797be74e807cb6b89d769e3e924d359d4d746939080a13c`, MVID `59008275-8bb0-4763-804a-b4175d917a99`. Stable suite `20260904T123300Z-phase3e-dev4-paired-scheduler-suite3` / `686f131a580377ca0b77ffc28bdd3d04eb12bfc0f6d24d8f59ad5ceb1963ce7b` and its full-continuity WhatIf passed.

Fresh runs `20260904T133300Z-phase3e-dev4-mammoth-tb-passA` and `20260904T140400Z-phase3e-dev4-mammoth-tb-passB` each pass `70/0`. In A/B respectively, the exact lease admitted at `3984/3968`, first granted at `4295/4280`, started one actionable frame later at `4296/4281`, and drove through `4529/4514`; each had `235` distinct drive frames. Each completed one mount-owned `Success` command, attack, roll, damage event, and Standard charge; left rider Standard unchanged; retained rider current identity and mount executor/weapon/rule/resource ownership; emitted no native mount turn; and cleaned the lease, command, target, relationship, presentation, Mods, and protected fixture with no residue. No primary scheduler repair cycle or K1-K12 criterion fired.

## Dev.5-dev.7 Horse TB intake attribution and diagnostic isolation

Dev.5 clean package/suite/WhatIf identities are recorded in the playtest report. Immutable run `20260904T155035Z-phase3e-dev5-horse-tb-gate2` failed before Mount admission. For `2,520` sampled frames the rider was exact actionable `CurrentTurn.Unit`, while the Horse held one unstarted foreign Standard `UnitAttack` created by `BlueprintAiAttack`. No KMC lease existed and the scheduler correctly refused to adopt or advance the AI command. This confirms the foreign-command exclusion at runtime; it does not exercise sequencing or consume a scheduler repair cycle.

Dev.6 adds no production code. After either pre-combat adjacency path completes, `Phase3dHorseScenarioTranche` enters `AwaitCombatMountHorseAiIsolation`, acquires and validates its existing reversible `ScopedDiagnosticAiLease<UnitEntityData>` for two stable frames, records the lease, and only then creates the disposable hostile. The lease remains active through direct diagnostic inputs and is restored by the existing cleanup predicate. Source-order coverage proves two convergence calls, one target-creation call, validation-before-target ordering, and no `InterruptAll` in the new state.

The complete dev.6 offline gate passes source/prohibited payload `22/0`, Release, component `315/0`, visual/source-order `18/0`, harness/protocol `241/0`, exact assembly `388/0` (`364` Kingmaker + `24` Wrath), PowerShell/JSON parsers `26/0` / `7/0`, and diff. Clean published commit is `90d0616ea8120496cdaf4397c069287f9338c985`. Package `C:\Dev\KingmakerMountedCombatLab\artifacts\KingmakerMountedCombat-0.1.0-phase3e-dev.6-paired-scheduler-horse-tb-gate2-diagnostic.zip` has ZIP/manifest/DLL SHA-256 `e97043c698db1b8860c9c485ffa39668ef36542b4f848151d626601f1f93ec75` / `0ae39630fe1d1cd248067a6b8313463284ba754668cc92795dd78b260da67a9f` / `8e7761a8cb8382e94ef4aaf6b9be1ada5c0fe1a83aace87fee3fa5fe89ba1c0b`; DLL MVID is `69d4c696-4a4b-4008-804b-6571ef0bca28`. Stable suite `20260904T164231Z-phase3e-dev6-horse-tb-suite5` / `0882d416094f857ca10da2c81371227cd0448ee3c0d59de70d0dc294dc11e44f` and full-continuity WhatIf `20260904T164720Z-phase3e-dev6-horse-tb-whatif` passed exact purity.

Immutable dev.6 live run `20260904T174752Z-phase3e-dev6-horse-tb-gate2` is outer/game `FAIL 42/2` at `AwaitCombatMount`. Its independent audit passed before evidence inspection and re-proved exact suite/save/Mods/Baseline/Working continuity with no process, runtime lock, sentinel, or live KMC residue. Dev.6's Horse lease boundary passed completely. A distinct rider AI Standard attack existed before diagnostic `StartTurn(rider)`; native preparation removed it but left rider hands busy. The genuine rider-owned Mount `UnitUseAbility` entered the rider Move slot while `Preparing`, with exact executor/target/type, legal proximity, and `CanStart=true`, but `executorHandsBusy=true`. It remained unstarted for 30 seconds and completed only after cleanup restored RT; KMC then correctly refused Mount because the mode had changed. No mounted relationship, scheduler lease, mount action, attack/rule/damage, resource transition, or completion path ran.

Dev.7 remains diagnostic-only. It adds a separate exact reversible rider AI lease after the already-validated Horse lease and before hostile creation; requires two stable frames with both command surfaces empty and both raw/effective AI states disabled; and waits for native rider hands/equipment readiness before Mount admission while preserving `Preparing` as the exact player-input boundary. Cleanup requires exact restoration of both leases. Source-order tests bind Horse lease, rider lease, both observations, sole target creation, no pre-target command interruption, hands/equipment guards, and dual restoration. Production scheduler, turn, ledger, combat, RT, and presentation code are unchanged. Complete dev.7 offline gates pass source/prohibited payload `22/0`, Release, component `315/0`, visual/source-order `18/0`, harness/protocol `241/0`, exact assembly `388/0` (`364` Kingmaker + `24` Wrath), PowerShell/JSON parsers `26/0` / `7/0`, and diff. Candidate DLL SHA-256/MVID are `c5d7cf5780ed02a0fb941090e74cd9a9f23642b4d94a067fbe47b2e9bf27a5d0` / `7b485e8c-d192-4f77-a0f9-1b983250f4f1`; they are not immutable package identity yet.

Dev.7 is now immutable clean/published commit `412fa949be558718200781df8221bd4b6f22af3c`. Its package ZIP/manifest/DLL identities are `4e6249211a496574a660935205711276319b80a3a819656acde3effc833f32fa` / `73eb397c5dc4ef3a9131da4088cbadd5e7dd7ec1b768dc28b35e3b8b612f99f2` / `c5d7cf5780ed02a0fb941090e74cd9a9f23642b4d94a067fbe47b2e9bf27a5d0`, MVID `7b485e8c-d192-4f77-a0f9-1b983250f4f1`; suite `20260904T185200Z-phase3e-dev7-horse-tb-suite6` is `bb7ee7fe14bf3a9e8711b51234dc0f1f2e3cd24b653ba290c49114e22f9ea5d3`. Full-continuity WhatIf passed.

Live `20260904T195400Z-phase3e-dev7-horse-tb-gate2` is immutable outer/game `FAIL 42/2`. Audit-before-read passed. Both AI leases, empty containers, rider hands/equipment, adjacency, selection, combat memory, current rider turn, unused ledgers, and native ability admission passed. The exact native click-created rider Mount shell entered the Move slot with the exact Horse target, legal proximity, `CanStart=true`, no approach, and no cooldown, but did not start during the 30-second TB leaf. Earlier prose incorrectly equated native click origin with `CreatedByPlayer=true`; dev.7 did not establish that field. It completed only after cleanup restored RT. No relationship or scheduler-owned command was reached, so this consumed no scheduler repair and fired no kill criterion.

## Dev.8 natural-turn and exact native-start telemetry

Exact `CombatController.TickTime` owns pending initiative advancement: it calls `StartTurn(m_NextUnit)` and then clears `m_NextUnit`. The old direct diagnostic `StartTurn(rider)` did not clear that pending field and could produce an artificial rider turn whose UI guard never converged. Dev.8 changes the diagnostic only:

- the combat-Mount entry path makes no direct `StartTurn` call and waits for the natural rider turn with `m_NextUnit == null`;
- admission additionally requires `WaitingForUI.Value == false`, `GuardCount == 0`, Default/unpaused mode, rider awake and in `AwakeUnits`, no rigidbody/get-up unit-tick exclusion, no nausea, exact rider selection, empty pair commands, and idle rider hands/equipment for two frames;
- the existing exact-token `TickCommandTurnBased` postfix observes the rider Mount shell's stock result before delegating to paired-scheduler policy; the observer is inert outside the active Phase 3D Horse tranche and never writes `__result`;
- the exact shell is bound by reference, native target-selection/cast-request origin, `CreatedByPlayer=false`, `AiAction==null`, rider executor, Horse target, Move slot, no queue entry, admission/start/terminal frame, eligibility true/false counts, duplicate-frame count, UI guard, current turn/status, and all `ShouldStartCommand` inputs;
- Horse evidence advances to schema 2; the external validator retains schema-1 compatibility and strictly validates both natural-turn admission failures and post-admission native lifecycle failures. PASS semantics require a natural turn, stock eligibility, start within two actionable frames, no duplicate visit, successful terminal cleanup, exact rider Move charge, and unchanged Horse ledger.

This is not a production scheduler repair. `MountedPairCommandScheduler`, pair ledgers, command routing, movement, attacks, RT behavior, presentation, settings defaults, and separate-turn fallback are unchanged.

## Offline verification

The complete pre-commit candidate gate on 2026-09-04 passed:

- source and prohibited-payload validation: `22 PASS / 0 FAIL`;
- Release build: PASS, .NET Framework 4.7 / C# 7.3;
- component tests: `315 PASS / 0 FAIL`;
- visual/source-order tests: `18 PASS / 0 FAIL`;
- harness/protocol tests: `241 PASS / 0 FAIL`;
- exact assembly contracts: `388 PASS / 0 FAIL` (`364` Kingmaker, `24` Wrath);
- `git diff --check`: PASS.

Those dev.2 DLL values became the clean package identities listed above. The complete dev.3 correction gate passed source/prohibited payload `22/0`, Release, component `315/0`, visual/source-order `18/0`, harness/protocol `241/0`, exact assembly `388/0` (`364` Kingmaker + `24` Wrath), PowerShell parser `26/0`, JSON parser `7/0`, and diff. Dev.4 passes the same complete totals; its clean package DLL SHA-256/MVID are `7f17fbc50ad282eef797be74e807cb6b89d769e3e924d359d4d746939080a13c` / `59008275-8bb0-4763-804a-b4175d917a99`.

The complete dev.8 pre-commit gate passes source/prohibited payload `22/0`, clean Release, component `315/0`, visual/source-order `18/0`, harness/protocol `242/0`, exact assembly `398/0` (`374` Kingmaker + `24` Wrath), PowerShell parser `26/0`, JSON parser `7/0`, and diff. Candidate DLL SHA-256/MVID are `a7288d4548813951529a3fe835e3a0226fbbd567c1d5083f65d98507a9c172bc` / `b2eee65b-c935-4043-adc5-bdf21eabc610`; clean packaging must establish the immutable artifact identity.

## Next gate

Gate 1 is complete except that `exact-turn-completion` intentionally remains open for an explicit advancement observation. Dev.5 implements the bounded diagnostic lease: capture `EnablePairedCommandScheduler`, set it true only for the exact Horse TB suite, restore it in `BestEffortCleanup`, and bind restoration in cleanup evidence. The RT and presentation suites do not enable it; production scheduler and every Phase 3D gameplay seam are unchanged.

Dev.5 complete offline gates pass source/prohibited payload `22/0`, Release, component `315/0`, visual/source-order `18/0`, harness/protocol `241/0`, exact assembly `388/0` (`364` Kingmaker + `24` Wrath), PowerShell parser `26/0`, JSON parser `7/0`, and diff. Dirty DLL SHA-256/MVID are `3b5b75e30ce2380d78a1807f53e5d30379dd7cd08acf4d588e3577f46b5bd0e8` / `4c87923d-2b5a-4232-be7f-d448d377a33f` and are not package identity.

Dev.5 through dev.8 live Horse TB processes remain immutable uncredited failures at distinct pre-scheduler diagnostic boundaries, each with exact independent restoration. Dev.9 preserves the natural-turn trace and corrects only the exact stock rider-shell provenance contract. Next: coherent guarded publication, clean package/suite/WhatIf, and one fresh existing Horse TB tranche with independent audit-before-read. Attribute the existing pair-aware `ContinueActing`, mount-ledger preparation, exact movement adapter, hostile-click sequencing, combat Mount/Dismount semantics, and five-foot-step rows before adding missing instrumentation. `exact-turn-completion` remains TODO until explicit native next-combatant advancement is observed.
