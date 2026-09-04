# Phase 3E Paired-Command Scheduler Implementation

Status: IN PROGRESS

## Primary architecture checkpoint

Phase 3E uses Option A: an exact native eligibility extension for one reference-identical KMC mount-primary command. The clean published implementation is `20d71e5a5b64b85b1815e9ea0c00ff3d7f03dd4e` on `codex/mounted-combat-phase3e-paired-scheduler`; the current diagnostic correction candidate is version `0.1.0-phase3e-dev.3`. One dev.2 runtime attempt exercised the complete gameplay lifecycle but remains immutable `FAIL 69/1`, so this document makes no Gate 1 PASS claim.

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

## Offline verification

The complete pre-commit candidate gate on 2026-09-04 passed:

- source and prohibited-payload validation: `22 PASS / 0 FAIL`;
- Release build: PASS, .NET Framework 4.7 / C# 7.3;
- component tests: `315 PASS / 0 FAIL`;
- visual/source-order tests: `18 PASS / 0 FAIL`;
- harness/protocol tests: `241 PASS / 0 FAIL`;
- exact assembly contracts: `388 PASS / 0 FAIL` (`364` Kingmaker, `24` Wrath);
- `git diff --check`: PASS.

Those dev.2 DLL values became the clean package identities listed above. The complete dev.3 correction gate passes source/prohibited payload `22/0`, Release, component `315/0`, visual/source-order `18/0`, harness/protocol `241/0`, exact assembly `388/0` (`364` Kingmaker + `24` Wrath), PowerShell parser `26/0`, JSON parser `7/0`, and diff. Its dirty DLL SHA-256/MVID are `933fc2107a3cd3579b81e6db872139cdff251966fa57aa1dec354918370016bd` / `88dfb6c3-d896-4f71-9dc0-a40f2891925b`; clean package identity remains pending.

## Next gate

Complete all dev.3 offline gates, commit and guarded-publish the diagnostic correction, build one immutable clean-HEAD dev.3 package, admit one stable suite, pass focused WhatIf purity, then run `shared-rider-turn-mount-primary-passA` and `passB` in fresh processes from that same package. Audit restoration immediately after each process and before reading gameplay evidence. No sequencing, ordinary TB melee/ranged, combat Mount/Dismount, or five-foot-step expansion begins until both vertical-slice passes succeed.
