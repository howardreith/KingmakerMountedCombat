# Phase 3H ordinary combat loop

Status: IN PROGRESS. Separate native turns; unified turn, paired scheduler and diagnostic overlay remain false. Ordinary native full attacks supersede the previous single-only ordinary contract. Explicit Primaries remain single attacks. One active mounted pair remains the boundary.

## Intake and bounded implementation plan

Started 2026-09-06 04:37 UTC on clean `codex/mounted-combat-phase3f-playable-core`, local/upstream/host remote `1b8469845ef32406ca5006290a7b86eeea99b726`. Actual preview.7 source `b8c206331f34a1aa873bfef86a37ab1066c267fc` remains installed with its human-created `.49723.cache`. No game was running. Current saves and foreign settings differ legitimately from installation-time snapshots and are the new preservation baseline.

The seven packet payloads at `handoffs/phase3h-20260906` match their manifest. All three screenshots were inspected; original bytes remain outside Git. The current stable log is preserved read-only at `analysis-cache/runtime-evidence/phase3h-intake-20260906T0444297198055Z/output_log.txt`, SHA-256 `7a839a4bd1ff0a034dce49239d0efdbe5edbcb3fdf90266461e8f8a2ca4d789d`, source last write 04:15:24 UTC. Startup and exception MVID identify preview.7. `intake.json` records full current saves/Mods inventories. Its PowerShell-drive free-space field was unavailable and serialized as zero; independent `System.IO.DriveInfo` reports 254,833,655,808 bytes free.

Human-reported: Horse TB approach and visible Bite fail; manual adjacent Horse attack and rider longbow Standard attack work; ordinary RT melee approach regressed; another rider-turn movement remains possible but its legality needs measurement. Seat improved but needs another 0.08 m down and anatomical centering. Countdown uses question marks. Dismount screenshot shows literal null metadata and automatic-use advice. Saddle aesthetics are explicitly deferred. These are human observations, not a new automated qualification ledger; prior pause, cancellation, party and stationary attack successes remain regression baselines.

1. Repair approach ownership and native/manual driver selection with red regressions. Preserve exact pair/provenance/foreign command guards and native distance/LoS. Reproduce the rider ordinary path independently.
2. Replace partial movement projection with an actor-owned native state adapter at verified refresh boundaries; separate native ordinary attack sequences from single Primary costs. Verify native plans and two turn orders without borrowing rider movement or Standard.
3. Repair the demonstrated Horse animation boundary, named visual seating and narrow native UI metadata; run integrated negative controls, final RT/TB checks and the applicable release gate. Build a distinct clean-source private artifact and publish through the host helper.

At most eight new guarded runtime transactions (zero used at intake), reserving final RT/TB. Each restores the exact current preview.7/cache/settings/saves. No permanent Phase 3H replacement is authorized. Every runtime outcome will identify exact source/package/config and evidence; unresolved movement, visual or pointer gates remain explicit.

## Confirmed first boundaries

The human log repeatedly reaches `MountedPairAttackCommand.BeginDelegatedMove`, then rejects the same mount-owned Standard wrapper because `mount.Commands.Empty` is false (lines 1004 onward). No child is started. The exact native `UnitCommands.Run` also pairs Standard and Move for interruption; admitting a Move must explicitly preserve only its compatible owned parent, and a started wrapper must permit locomotion while approaching. TB manual approach driving must be restricted to rider-current/off-executor movement.

Ordinary attacks currently use a Standard wrapper with an ignored-cooldown `IsSingleAttack` child. Full attacks require one native sequence and one authoritative charge, including eligibility after approach. Merely relaxing the child count is insufficient. Current partial prepayment and two-float projection do not establish complete movement ownership.

## Qualification

First approach checkpoint: the added ownership regression failed against the old empty-container behavior (`341 PASS / 1 FAIL`) and passes after the repair (`342 / 0`). Release/source checks pass (`22 / 0`). The exact private native paired-interruption overload is verified at `0x060026BF`; its thin prefix consults only the active command's exception-safe admission scope. Queue, previous command, group and foreign raw slots are never cleared. The wrapper permits movement only in Approaching, and manual delegated ticks now require rider-current TB; native RT and mount-current TB retain their driver. This is an implemented correction with deterministic/native-member evidence, not yet a runtime PASS.

Phase 3H gameplay, full attacks, resource reconciliation, visible animation, seating and UI: NOT RUN. Historical Phase 3G evidence retains its original build and failures. No HUMAN PASS is claimed.
