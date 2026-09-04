# Phase 3E Paired-Command Scheduler Implementation

Status: IN PROGRESS

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

## Next gate

Gate 1 is complete except that `exact-turn-completion` intentionally remains open for an explicit advancement observation. Dev.5 implements the bounded diagnostic lease: capture `EnablePairedCommandScheduler`, set it true only for the exact Horse TB suite, restore it in `BestEffortCleanup`, and bind restoration in cleanup evidence. The RT and presentation suites do not enable it; production scheduler and every Phase 3D gameplay seam are unchanged.

Dev.5 complete offline gates pass source/prohibited payload `22/0`, Release, component `315/0`, visual/source-order `18/0`, harness/protocol `241/0`, exact assembly `388/0` (`364` Kingmaker + `24` Wrath), PowerShell parser `26/0`, JSON parser `7/0`, and diff. Dirty DLL SHA-256/MVID are `3b5b75e30ce2380d78a1807f53e5d30379dd7cd08acf4d588e3577f46b5bd0e8` / `4c87923d-2b5a-4232-be7f-d448d377a33f` and are not package identity.

Next: commit/publish/package dev.5, admit a stable suite, pass full-continuity WhatIf, and run one fresh existing Horse TB tranche with independent audit-before-read. Attribute the existing pair-aware `ContinueActing`, mount-ledger preparation, exact movement adapter, hostile-click sequencing, combat Mount/Dismount, and five-foot-step rows before adding missing instrumentation.
