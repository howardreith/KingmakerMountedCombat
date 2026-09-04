# Phase 3E Paired-Command Scheduler Implementation

Status: IN PROGRESS

## Primary architecture checkpoint

Phase 3E uses Option A: an exact native eligibility extension for one reference-identical KMC mount-primary command. The clean published input is `80a75ee6b3011cb4ec52d1b296776db25f6b0f15` on `codex/mounted-combat-phase3e-paired-scheduler`; the implementation candidate is version `0.1.0-phase3e-dev.2`. Runtime qualification has not yet run, so this document makes no gameplay PASS claim.

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

## Offline verification

The complete pre-commit candidate gate on 2026-09-04 passed:

- source and prohibited-payload validation: `22 PASS / 0 FAIL`;
- Release build: PASS, .NET Framework 4.7 / C# 7.3;
- component tests: `315 PASS / 0 FAIL`;
- visual/source-order tests: `18 PASS / 0 FAIL`;
- harness/protocol tests: `241 PASS / 0 FAIL`;
- exact assembly contracts: `388 PASS / 0 FAIL` (`364` Kingmaker, `24` Wrath);
- `git diff --check`: PASS.

The dirty-build DLL SHA-256 is `d71529ba6006bc6fa2c8916953cb773f1c5319e49ce4a0c9d70420e1aee26d87`; MVID is `89de0fcc-ca6f-41cd-944b-097a5860716c`. These are not package identities. No dev.2 package, stable suite, WhatIf, or live process exists yet.

## Next gate

Commit and guarded-publish this coherent candidate, build one immutable clean-HEAD dev.2 diagnostic package, admit one stable suite, pass focused WhatIf purity, then run `shared-rider-turn-mount-primary-passA` and `passB` in fresh processes from that same package. Audit restoration immediately after each process and before reading gameplay evidence. No sequencing, ordinary TB melee/ranged, combat Mount/Dismount, or five-foot-step expansion begins until both vertical-slice passes succeed.
