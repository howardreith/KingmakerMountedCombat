# Kingmaker Mounted Combat — Chunk 7A foundation review

Review date: 12 October 2026. This is a source-review record and repair input for the sustained 7B–7D mission, not a native qualification or replacement roadmap.

## Verdict

Continue from the delivered branch, with **two P2 correctness fixes at the start of the next mission**. The ownership foundation is connected to the production one-pair path; its deliberate one-pair native capacity and schema-2 native adapter are consistent with the selected 7A assignment. The new migration code and retained lifecycle routing contain the two specific defects below. Neither warrants discarding the foundation or repeating Chunk 6 qualification before starting the repairs.

The defects were established by source/control-flow and validator analysis. This review did not execute the Windows test scripts, launch Kingmaker, read private live receipts, stage a candidate, or write a native save. Their native frequency and runtime reproduction remain to be established in the executor's appropriate tests.

## Reviewed identity and evidence limits

| Item | Review result |
| --- | --- |
| Repository/branch | `howardreith/KingmakerMountedCombat`, `codex/mounted-combat-chunk7-foundation` |
| Source/tests commit | `21df88347b98532fd59b82d3ae05078f7bac64e9` |
| Handoff / verified remote HEAD | `01c6a1f1d5047803bd1121d85fcd619efcab2236` |
| Main at review | `96c7fa1919f371ebd1100aab66f40b5f471644bb` |
| Compared change | Two commits ahead of main; source commit changes 29 files; handoff adds two more changed paths, 31 in the comparison. |
| Local clean worktree | Reported by the executor; not independently established from a remote source read. |
| Test results | Recorded in CURRENT and the owner's handoff; not re-executed by this review. |

The recorded final focused command is:

```powershell
.\scripts\Test-Tiers.ps1 -Tier Fast -Focused 'Test-PersistenceData.ps1,Test-ChargeOwnershipContracts.ps1,Test-MidEncounterNativePreparation.ps1'
```

CURRENT reports PASS in 10.56 seconds: compilation; 727 component tests; 147 source checks; 56 historical and 43 new persistence checks; 121 ownership/Charge checks; 16 preparation contracts. These are different evidence categories, not a count of unique native behaviors. The component movement/view boundary is substituted; compiled adapters use detached native actors/containers. Earlier failed attempts are recorded as retained. No game run, native save write, candidate staging/package or broad qualification is claimed. [Checkpoint][current]

## R1 — P2: repeated cleanup of a detached owner can postpone the mount's independent turn

### Source sequence

1. A pair dismounts in TB round N. Its attachment clears but its owner remains registered while split participation is retained.
2. Native combat advances to round N+1 before the next ownership-expiry refresh.
3. A former pair actor becomes unconscious. The new actor lookup includes the retained detached owner, so the life-state subscriber routes cleanup into `Dismount` again.
4. The relationship invokes its Dismounting subscribers even though this is detached cleanup. The paired-turn subscriber calls `SplitPairedActivation`.
5. That method unconditionally rewrites `splitReleaseRound` to N+1. `SuppressPairedCandidate` can now withhold the unmounted mount for N+1, until N+2.

Before this change the actor event compared against the cleared runtime rider/mount, so it did not re-deliver detach for that retained record. The new routing exposes the existing non-idempotent split operation. This is a source-reachable boundary-order regression; no game reproduction is asserted.

### Correction and regression

Keep detached ownership addressable for remaining custody, but make repeated split/detach idempotent. Distinguish active attachment invalidation from servicing outstanding detached obligations. Preserve retry behavior when other subscribers fail. Test the N → N+1 → detached-life-callback sequence, and later verify pair-local behavior with two owners. Removing all detached records from lookup or clearing their debt is not a valid correction.

### Pinned source

- [Retained owner resolution, Ownership.cs 75–82][owner-lookup].
- [Life-state routing, MountedLifecycleSubscriber.cs 55–70][life-route] and [cleanup dispatch, 279–302][cleanup-route].
- [Dismount subscriber invocation, GameMountedRelationshipService.cs 438–471][dismount].
- [Paired subscriber, UnifiedMountedTurnCoordinator.cs 756–758][split-subscriber].
- [Release-round overwrite, PairedActivationLifecycle.cs 811–818][split-reset] and [candidate suppression, 619–629][suppression].

## R2 — P2: schema-3 migration rejects valid mounted-plus-pending-only schema-2 state

### Source sequence

A mounted principal may leave native combat while its mount remains in combat. `BeforePairedActorRemoval` retains `pendingSplitMount`, finishes/discards the activation and clears the armed pair. It does not dismount the attachment. The leave-combat event cancels commands; mounted invariants do not require identical combat membership.

The historical capture can consequently contain:

```text
Mounted = true; Rider = R; Mount = M
Combat.Paired.RiderId = null
Combat.Paired.MountId = null
Combat.Paired.Activation = null
Combat.Paired.PendingSplitId = M
```

Relationship actors are captured independently of roster membership. The native actor snapshot permits `InCombat=false`; the historical validator accepts a roster subset and a pending-only paired state. This is supported historical state, not an invented malformed DTO.

The new migration links a participation only when its full rider/mount identity pair matches the attached pair. Here it makes `linked=false` and creates a detached record. New validation rejects the pending mount because it belongs to the still-mounted relationship. The new pending-only test starts by setting `Mounted=false`, so it omits this combination.

### Impact and correction

**Existing native schema-2 loading is unaffected:** `NativeMountedSaveFormat.Decode` still calls `MountedSaveCodec.Decode`. The defect is in the delivered schema-3 migration foundation and must be fixed before native schema-3 enablement.

Add the historical mounted-plus-pending-only regression and associate the pending obligation correctly while preserving the native exclusion, debt, release round and absence of an activation. Do not create missing activation/grant state or discard the pending obligation. Keep rejection of truly unrelated/cross-pair overlap. The new graph model may need an explicit valid representation of this obligation; globally weakening uniqueness is insufficient.

The existing `Historical()` fixture provides a minimal focused reproduction to add and execute in the Windows test environment:

```csharp
var pendingMounted = Historical();
pendingMounted.Combat.Paired = new SavedPairedState
{
    PendingSplitId = pendingMounted.Mount.Id,
    PendingSplitRound = pendingMounted.Combat.Round,
    SplitReleaseRound = -1
};
var source = MountedSaveCodec.Encode(pendingMounted);
var migrated = MountedArchiveCodec.Decode(source);
// Assert Current and preserved mounted + pending-only state after the fix.
```

This snippet was derived from the production validation and existing fixture; it was not executed in this source-review environment.

### Pinned source

- [Native actor removal, PairedActivationTransitions.cs 127–149][actor-removal].
- [Leave-combat behavior, MountedLifecycleSubscriber.cs 43–46][leave-combat]; [attachment invariants, KingmakerMountedPairRuntime.cs 259–333][invariants].
- [Paired capture, PairedActivationPersistence.cs 20–43][paired-capture]; [capture actor union, MountedCombatPersistenceService.cs 38–48][actor-union].
- [Native actor combat flag, NativeCombatActorPersistence.cs 51–61][actor-capture]; [pending-only historical validation, SavedCombatData.cs 249–265][pending-validation].
- [Migration link choice, MountedArchiveCodec.cs 100–119][migration]; [new membership check, MountedArchiveData.cs 63–79][membership].
- [Existing test's unmounted setup, Chunk7PersistenceTests.cs 95–109][pending-test].

## Confirmed implementation and continuation implications

The registry is used by actual relationship admission before runtime attachment. Commands retain ownership/generation, lifecycle resolves actor ownership, and retained cleanup/participation controls retirement. Actor allocation history remains outside attachment lifetime. The new readiness condition uses the native boundary queue separately from the save's own admission fence; no direct self-blocking save-fence defect was found in that expression. [Implemented design and test boundary][brief]

Native format selection explicitly requires schemas 1/2 normalized to schema 2. Unsupported original data is preserved without invoking native capture; unreadable originals refuse writing. Schema-3 native support has not been accidentally enabled. [Native format adapter][native-format]

The next profile tranche must separate schema-3 profile eligibility from historical schema-2 validation. Current archive validation reuses a legacy DTO's Medium-only profile validation, which is appropriate for the foundation but insufficient for the selected Small profile. This is planned integration work, not an additional current defect. [Archive validation][profile-validation]

The sustained mission should select 7B–7D together: complete native all-pair capture/load/area/lifecycle/removal/worker/turn integration; obtain targeted two-pair RT/TB and save/cold-load proof; continue through declared Small/progression breadth and measured hardening. Its new authority must explicitly supersede completed-7A source-only stop instructions. Preserve live safeguards and leave full 7E release rehearsal and HUMAN PLAY acceptance separately identified. [Delivery sequence][sequence] [Completed-task stop instructions][resume] [Validation policy][validation]

[current]: https://github.com/howardreith/KingmakerMountedCombat/blob/01c6a1f1d5047803bd1121d85fcd619efcab2236/CURRENT.md
[owner-lookup]: https://github.com/howardreith/KingmakerMountedCombat/blob/01c6a1f1d5047803bd1121d85fcd619efcab2236/src/KingmakerMountedCombat/Integration/GameMountedRelationshipService.Ownership.cs#L75-L82
[life-route]: https://github.com/howardreith/KingmakerMountedCombat/blob/01c6a1f1d5047803bd1121d85fcd619efcab2236/src/KingmakerMountedCombat/Integration/MountedLifecycleSubscriber.cs#L55-L70
[cleanup-route]: https://github.com/howardreith/KingmakerMountedCombat/blob/01c6a1f1d5047803bd1121d85fcd619efcab2236/src/KingmakerMountedCombat/Integration/MountedLifecycleSubscriber.cs#L279-L302
[dismount]: https://github.com/howardreith/KingmakerMountedCombat/blob/01c6a1f1d5047803bd1121d85fcd619efcab2236/src/KingmakerMountedCombat/Integration/GameMountedRelationshipService.cs#L438-L471
[split-subscriber]: https://github.com/howardreith/KingmakerMountedCombat/blob/01c6a1f1d5047803bd1121d85fcd619efcab2236/src/KingmakerMountedCombat/Integration/UnifiedMountedTurnCoordinator.cs#L756-L758
[split-reset]: https://github.com/howardreith/KingmakerMountedCombat/blob/01c6a1f1d5047803bd1121d85fcd619efcab2236/src/KingmakerMountedCombat/Integration/PairedActivationLifecycle.cs#L811-L818
[suppression]: https://github.com/howardreith/KingmakerMountedCombat/blob/01c6a1f1d5047803bd1121d85fcd619efcab2236/src/KingmakerMountedCombat/Integration/PairedActivationLifecycle.cs#L619-L629
[actor-removal]: https://github.com/howardreith/KingmakerMountedCombat/blob/01c6a1f1d5047803bd1121d85fcd619efcab2236/src/KingmakerMountedCombat/Integration/PairedActivationTransitions.cs#L127-L149
[leave-combat]: https://github.com/howardreith/KingmakerMountedCombat/blob/01c6a1f1d5047803bd1121d85fcd619efcab2236/src/KingmakerMountedCombat/Integration/MountedLifecycleSubscriber.cs#L43-L46
[invariants]: https://github.com/howardreith/KingmakerMountedCombat/blob/01c6a1f1d5047803bd1121d85fcd619efcab2236/src/KingmakerMountedCombat/Integration/KingmakerMountedPairRuntime.cs#L259-L333
[paired-capture]: https://github.com/howardreith/KingmakerMountedCombat/blob/01c6a1f1d5047803bd1121d85fcd619efcab2236/src/KingmakerMountedCombat/Integration/PairedActivationPersistence.cs#L20-L43
[actor-union]: https://github.com/howardreith/KingmakerMountedCombat/blob/01c6a1f1d5047803bd1121d85fcd619efcab2236/src/KingmakerMountedCombat/Integration/MountedCombatPersistenceService.cs#L38-L48
[actor-capture]: https://github.com/howardreith/KingmakerMountedCombat/blob/01c6a1f1d5047803bd1121d85fcd619efcab2236/src/KingmakerMountedCombat/Integration/NativeCombatActorPersistence.cs#L51-L61
[pending-validation]: https://github.com/howardreith/KingmakerMountedCombat/blob/01c6a1f1d5047803bd1121d85fcd619efcab2236/src/KingmakerMountedCombat/Integration/SavedCombatData.cs#L249-L265
[migration]: https://github.com/howardreith/KingmakerMountedCombat/blob/01c6a1f1d5047803bd1121d85fcd619efcab2236/src/KingmakerMountedCombat/Integration/MountedArchiveCodec.cs#L100-L119
[membership]: https://github.com/howardreith/KingmakerMountedCombat/blob/01c6a1f1d5047803bd1121d85fcd619efcab2236/src/KingmakerMountedCombat/Integration/MountedArchiveData.cs#L63-L79
[pending-test]: https://github.com/howardreith/KingmakerMountedCombat/blob/01c6a1f1d5047803bd1121d85fcd619efcab2236/tests/Chunk7PersistenceTests.cs#L95-L109
[brief]: https://github.com/howardreith/KingmakerMountedCombat/blob/01c6a1f1d5047803bd1121d85fcd619efcab2236/planning/CHUNK7-DEVELOPMENT-BRIEF.md#L127-L184
[native-format]: https://github.com/howardreith/KingmakerMountedCombat/blob/01c6a1f1d5047803bd1121d85fcd619efcab2236/src/KingmakerMountedCombat/Integration/NativeMountedSaveFormat.cs#L9-L29
[profile-validation]: https://github.com/howardreith/KingmakerMountedCombat/blob/01c6a1f1d5047803bd1121d85fcd619efcab2236/src/KingmakerMountedCombat/Integration/MountedArchiveData.cs#L35-L46
[sequence]: https://github.com/howardreith/KingmakerMountedCombat/blob/01c6a1f1d5047803bd1121d85fcd619efcab2236/planning/CHUNK7-DEVELOPMENT-BRIEF.md#L198-L218
[resume]: https://github.com/howardreith/KingmakerMountedCombat/blob/01c6a1f1d5047803bd1121d85fcd619efcab2236/AUTONOMOUS-RESUME.md#L20-L33
[validation]: https://github.com/howardreith/KingmakerMountedCombat/blob/01c6a1f1d5047803bd1121d85fcd619efcab2236/docs/VALIDATION-POLICY.md
