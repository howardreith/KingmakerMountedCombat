# PR-1 round 7 working record (owner instruction 2026-11-26b section 7)

Enrollment kmc-codex-mmr-attack-orders-20261125103400-6cef, resumed to ceiling 8 (6 used). R1-F11, R4-F1, R4-F5, R4-F6 have been
open three consecutive reviews: any of them still open after round 7 stops the child (no-progress). All seven blockers must close.

## Production corrections (applied, uncommitted in worktrees/mmr-attack-orders)

- R1-F11: installed ClickUnitHandler.OnClick (0x060093ED) per selected unit either skips (IL_0207 -> MoveNext) or calls
  unit.Commands.Run(cmd) (IL_0341) and then sets unit.CombatState.ManualTarget = clicked (IL_0363). Controller.ObserveClickRun
  (called from the UnitCommands.Run prefix, MountedPatchController.UnitCommandRunPrefix) marks the actor the click reached; Begin no
  longer infers from selection. EndClickObservation unchanged otherwise.
- R6-F1: MountedPairAttackCommand.RecordTargetInvalidation (shared TargetInvalidationReason) records TargetInvalid + bounded reason +
  transaction.Cancel("Expected target invalidation: <reason>") before RetireOrderOnTargetInvalidation's generic retirement (first
  recorded cause stands). Released strikes untouched.
- R6-F2: ResolveEqualRankTie: under restricted travel a melee candidate needs StationaryStrikeAdmissible(unit), not rider.HasLOS.
- R4-F6: MountedPairAttackCommand.AttachActorCharges(outcome) (ActorId, RiderStandardCharged, ActionStandardCharged from
  DescribeStandardCharge) runs in ReportTerminalOnce on the outcome actually delivered (detailed or minimal); DescribeOutcome no longer
  sets the two charge flags.

## Harness plan (tests/Chunk7Diagnostics/AttackPathHarnessTemplate.cs + scripts/Test-MountedAlpha2AttackPathHarness.ps1)

1. Native UnitAttack facade (installed 2.1.7b UnitAttack.OnAction 0x06002681): trigger rule for AllAttacks[m_AttackIndex], index++,
   return None while index < count, Success after the last; GetAttackIndex = m_AttackIndex; repeated acts while attacks remain.
   Horse plan of N attacks (World option). Existing single-attack cases: count 1 => Success at the act (native); check fallout.
2. R4-F1: horse with 2+ attacks: first delivery, rider replaces order; remaining attack(s) on A through the native acts; production
   OnEnded completed-plan branch (transaction Completed), one Standard spend; then one dispatch for valid B / zero for invalid B;
   hard-cancellation control.
3. R4-F6: splice ReportTerminalOnce + AttachActorCharges; harness DescribeOutcome stand-in (detailed) or throwing (minimal); capture
   the outcome delivered to the terminal callback for unstarted rider force-finish, delivered rider attack, delivered horse attack.
4. R4-F5: splice HandleCommandTerminal (recorded native terminal branch and actual progress) and TryDriveStockAttackIntent +
   TryExecuteAction (CaptureContext facade over the world; production evaluator); fixture order bound to the fixture turn.
5. R5-F3 / R6-F2: splice ObserveAutomaticProposal + ResolveEqualRankTie (NativeBrainRankingReader facade over world candidates with
   cached HasLOS vs command sight).
6. R1-F11: click loop model (skip or Run+write) calling controller.ObserveClickRun as the Run prefix does.
7. R6-F1: controller-first vs wrapper-first loss before delivery: compare cause, terminal reason, rejection code; zero spend/move.
