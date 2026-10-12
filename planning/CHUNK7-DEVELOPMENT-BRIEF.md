# Kingmaker Mounted Combat: Chunk 7 development brief

Prepared for Howie, 12 October 2026. Intended repository location: `planning/CHUNK7-DEVELOPMENT-BRIEF.md`.

This is a durable engineering brief for the existing mod. It preserves the Chunk 7 finish line, source-based ownership constraints, the first source tranche and the later native integration sequence. The companion `KMC_Chunk7_Codex_Start_Prompt.md` is the complete opening assignment. This brief alone does not launch a campaign or authorize a merge, release, permanent installation or protected-save write.

## 1. Authority and current baseline

Use later explicit owner instructions and the current repository policy. Read `AGENTS.md`, `docs/VALIDATION-POLICY.md`, `CURRENT.md` and `AUTONOMOUS-RESUME.md`; historical next-run commands remain historical. Reconcile this brief with an equivalent existing document instead of maintaining competing active plans. Never overwrite a newer accepted decision with this dated snapshot.

### Source snapshot

| Identity | Verified/reference value |
| --- | --- |
| Repository | `howardreith/KingmakerMountedCombat` |
| Main checked on 12 October 2026 | `96c7fa1919f371ebd1100aab66f40b5f471644bb` |
| Merged Chunk 6 closeout | `d8bd2413059ad410772faa715efc320877ab0f68` |
| Released qualified product | preview.211, product source `a0d70fa41f25498aa2f31dfdfebc1ad29750f4a6` |
| Qualification harness/readers | `7e49e0348ee944108af4f8ccec5dda899097f1c1` |
| Final offline test-only correction | `c43f32f897c958021b9b191779707202bf5bd724` |
| Known Windows lab root | `C:\Dev\KingmakerMountedCombatLab` |

Main is the merge of prior main `6f993c226829acd23418b38dd47845da1a924652` and the closeout. Its tree has no file changes relative to the closeout. Refresh remote state and actual local worktrees before beginning; these identities are not reset or restoration targets. [Merge][merge] [Comparison][comparison]

The committed closeout records **Chunk 6 engineering complete, owner acceptance pending**: 109/109 campaign coverage, all 105 mandatory persistence behaviors, all 87 mandatory Chunk 6A behaviors and final FULL success. Restoration is reported for all 211 actual transactions. These measures are different; do not add them as unique features or erase retained failed attempts. Raw private receipts and current DATA installation were not independently audited for this brief. The last recorded human installation was preview.105; any later authorized transaction must snapshot actual current state. [Closeout][closeout]

The released artifact, its native evidence and the current source/documentation revision are separate identities. A new source checkpoint inherits the accepted contract and history, not native PASS on changed bytes.

## 2. Product contract and complete Chunk 7 scope

Keep one independent Kingmaker UMM mod, .NET Framework 4.7, C# 7.3, actual Kingmaker 2.1.7b and installed Harmony12 contracts. Keep patches/UI/callbacks thin, native adapters bounded, and behavior in services/domain code. Retain the accepted architecture and CRPG transport preset: two real combatants with separate health, defenses, effects and native action budgets; the mount is the physical mover. Carried movement alone does not add an unapproved rider cost. [Agent policy][agents]

The scope source is **Kingmaker_Mounted_Combat_Revised_Project_Plan.docx**, Revision 2, 6 September 2026, Chunk 7 on pages 16-17 and F09-F12 on page 19. It was recovered in the supplied oversight package. The following preserves its requirements while applying the later explicit deferrals and current validation policy. Its historical schedules, branch pins and previously repaired defects do not define new work.

| Release requirement | Observable outcome |
| --- | --- |
| Independent pairs | At least two pairs in real combat, with exact pair/generation/actor-allocation command identity and no selected-pair authority over another pair. |
| Stress breadth | Six-pair lifecycle/performance stress where the fixture permits. An unavailable fixture is a named open gate or explicitly agreed limitation, not an invented PASS. |
| Profiles | Existing Medium Horse and Mammoth plus at least one specifically declared Small-rider profile. Publish supported body, equipment and form combinations and exclusions. Native effective size and reciprocal companion ownership remain authoritative. |
| Progression/removal | Existing horse acquisition architecture qualified for new characters and existing campaigns, leveling, dismissal/replacement, retraining and the documented cleanup/removal behavior. |
| Performance | Measure allocations, frame cost, log volume, subscriptions and retained view/command owners through repeated mount/unmount/area cycles; repair demonstrated problems. |
| Supported configuration | One supported rules preset and accepted turn architecture; failed historical experiments inaccessible to ordinary players; diagnostics opt-in and inert. |
| Compatibility | Pinned base-game/loader/Harmony configuration first. At most one explicitly selected, version-pinned high-value mod combination if justified within the selected budget. Read current local versions; never downgrade working components to match stale notes. |
| Exact-package rehearsal | Identified commit/artifact/configuration; fresh install, upgrade, manual/quick/auto saves, cold loads, removal/rollback and representative RT/TB sessions. Evidence must describe delivered bytes. |

### Dispositions that carry forward

Supported baseline includes RT/TB legal combat Mount/Dismount, RT pair-owned Charge (default-off), ordinary mounted casting/items, staged actions, and the agreed persistence/lifecycle contracts. TB Charge delivery remains **DEFER - EVIDENCED** with cost-free refusal. The defensive Mounted Combat feat remains **not implemented and deferred**; feasibility probes are not a reaction, Ride/Mobility check or negated hit. [Current status][current] [Closeout][closeout]

Cavalier, Paladin Divine Steed, additional class grants, enemy riders, universal mount support, lances/barding, advanced feat chains, continuous move-and-fire animation and a second rules preset remain expansion scope. The first source tranche adds none of them.

## 3. Source findings that determine ownership

These findings were inspected at `96c7fa1`. They are constraints of the qualified one-pair design, not freshly reproduced one-pair product defects. Read actual current contiguous source before changing a seam.

### 3.1 One service graph and one static bridge

`CompositionRoot` creates one relationship/combat/control/turn/persistence graph. `GameMountedRelationshipService` owns one runtime, coordinator, generation and cleanup state, and rejects a new mount when not unmounted. `MountedPatchController` assigns these services to static `PatchBridge` fields. Creating another complete graph would overwrite shared routing rather than create independent pair authority. [Root][root] [Relationship][relationship] [Patches][patches]

Retain one bridge and global integration owner. Resolve the appropriate actor, native command, execution context or turn event into the exact pair/retained owner. Shared registration and a bounded membership index may remain global.

### 3.2 Existing exact command binding should survive

`NativeMountedControlService` already binds exact `UnitUseAbility` and `AbilityExecutionContext` instances through conditional weak tables. Shell records retain caster/target, generation, consumed and retired state. Extend that mechanism with correct pair ownership; do not replace it with current selection, current caster slot, latest shell or generation-only lookup. `MountedPlayerActionController.CaptureContext` currently projects the singular relationship and must be considered in routing. [Controls][controls] [Player actions][player-actions]

`MountedCombatController` has singular active command/movement/intent ownership. Current Charge custody is `chargeOwner` in `MountedCombatController.ChargeCleanup.cs`, retaining actors, containers, generation, shell/process/context and cleanup state. Cleanup is finished only when its native postconditions hold. A failed cleanup must retain its owner after dismount. [Combat][combat] [Charge cleanup][charge-cleanup]

### 3.3 Membership, retained obligations and actor history differ

`MountedMovementStateAdapter` already has actor-keyed allocations and a session-aware lifetime helper. Dismount, selection and mode conversion are explicitly not retirement boundaries. `PairedActivationLifecycle` preserves detached/split obligations and retires an old detached activation without clearing actor movement history. `UnifiedMountedTurnCoordinator.Dispose` does clear that history, so disposing a complete coordinator with each pair is incorrect. [Allocations][allocations] [Activation][activation] [Turn coordinator][turn]

Maintain four responsibilities, without requiring four new frameworks:

| Responsibility | Lifetime / authority |
| --- | --- |
| Active membership | Exact rider/mount ownership and reservation. Changes when a relationship is admitted or detached. |
| Retained pair/generation owners | Admitted commands, pending split participation, attachment leases and cleanup. Remain addressable until their established obligations settle. |
| Actor allocations | Expenditure/native grant history. Survives relationship changes; retires only at the established native/session boundary. |
| World operations | Native roster/current turn, transition replay, save/load worker and world replacement. One coherent owner per operation. |

An unrelated pair's participation must not prevent another pair's lawful admission. An actor's own unresolved obligations must not disappear merely to permit re-pairing. Exact generation and command identity prevent delayed work from attaching to a replacement relationship.

### 3.4 Save cardinality exists at two levels

`MountedSaveData` schema 2 holds one `Mounted/ProfileId/Rider/Mount` relationship. `SavedCombatData.Paired` is also singular; it includes split and pending-split state, not just presentation. Actors, roster, current/next turn, clocks, engagements and allocations are already one shared combat snapshot. [Save model][save-model] [Combat model][combat-model]

`PairedActivationPersistence.RestorePersistence` first calls `DiscardPersistenceWorld` and restores allocations before restoring one paired state. Do not call that whole method once per pair. Discard and restore shared world state once, then bind per-pair state into it. [Paired persistence][paired-persistence]

### 3.5 World saves and transitions include every outstanding owner

The native header barrier snapshots one relationship and suspends one set of controls/AI leases. The save worker subsequently serializes the live Player/area graphs. Global projectile/command scans do not replace the singular pair preparation, transition and Charge-fence checks. All relevant owners must remain fenced until the exact worker and iterator settle. Aggregate the existing readiness predicates while preserving lawful preparation and combat-save behavior; a new blanket refusal rule is not part of the expansion. [Persistence][persistence] [Deferred saves][deferred-saves]

Area transfer has one pair record, and removal readiness examines one relationship/controller. Actor-specific events should resolve affected owners; world save/load/area/mode/disable/removal boundaries aggregate them. Never restore one pair by discarding the previous pair's world state. Attempt individual cleanup releases independently so one throwing release does not skip later owners, and retain failed cleanup responsibilities. [Area][area] [Lifecycle][lifecycle] [Removal][removal]

## 4. Initial selected source tranche

The opening assignment selects **production ownership/routing plus the versioned codec/migration foundation**. Its finish line is an implemented, compiled, focused-tested source checkpoint with a coherent commit and guarded review-branch push. Native tests, candidate packaging, releases and full Chunk 7 acceptance are later selected work.

### Required source result

- Bounded session-aware membership/retained-owner lookup supports two disjoint pairs in real production components.
- Existing one-pair Mount/Dismount, command/context dispatch and cleanup use the new seam; no unused demonstration registry.
- Actor history and pending split/cleanup remain valid through detach and re-pair.
- Aggregate readiness accounts for every outstanding owner without duplicating native world authority.
- Versioned DTO/codec migration and cross-record validation are implemented with existing infrastructure.
- Normal native runtime still admits only one pair and continues its existing native save/restore contract.

Use focused behavior groups covering production adoption; two-pair registry isolation and duplicate/cross-role refusal; exact delayed delivery/stale generation; local failure and retained history; aggregate readiness; old-schema migration including unmounted retained combat; new-schema round-trip/invalid graph, including conflicting current-turn claims; and native capacity/format preservation. Exercise them through production decisions with minimal substitutes at unavailable native boundaries. They do not require enabling a second pair in the live runtime during 7A. These are scenarios to cover efficiently, not a requirement for a new parallel acceptance engine.

### Explicit format and capacity boundaries

During this source tranche:

| Capability | Boundary |
| --- | --- |
| Production registry/domain ownership | Multiple records supported and tested. |
| Native relationship admission | One pair maximum; second admission refuses before mutation. |
| Native save writer | Continues selecting schema 2. |
| Native restore | Existing schema-1/schema-2 adapter retained. |
| Version-3 codec/migration | Implemented and tested in memory; not automatically activated by the native save/load path. |
| Version-3 archive encountered by current runtime | No native restoration through the one-pair adapter; preserve original metadata through the established unsupported/future path. |

Check retained participation as well as mounted relationship count. A document with zero mounted pairs can still contain multiple outstanding native participation records. No truncation, "first pair wins," partial restore or silent rewrite is acceptable.

This adapter is an explicit staging boundary, not the eventual Chunk 7 product limit. The next native tranche removes it only after all-pair world integration is coherent.

### Implemented 7A ownership and migration note (12 October 2026)

The review branch starts at the refreshed main above. The following describes
this implementation; section 3 preserves the inspected baseline rationale.

| Authority | Production owner and behavior |
| --- | --- |
| Membership and retained custody | `Domain/MountedPairRegistry.cs` indexes both exact actor objects by canonical native ID and session/campaign. Reservation precedes attachment; active, detached and retired registrations differ. Six active pairs and twelve total owners are bounded. A conflicting actor cannot be reused while its cleanup or participation remains owned. |
| Pair runtime and generation | `GameMountedRelationshipService.Ownership.cs` binds each registration to its relationship service, runtime, coordinator and generation. The ordinary composition root creates one such boundary. Its explicit capacity of one also fences retained native participation before another pair can reserve. Mount and legacy saved-pair restoration reserve before `runtime.Prepare`. |
| Admitted commands | Existing native shell and execution-context weak tables retain a captured request and exact boundary. Selection supplies request context only. Ordinary attack, door, ground movement and Charge execution retain the registration; generation/registration replacement refuses stale work. The existing consumed/retired shell mechanism and Charge cleanup postconditions remain. |
| Cleanup and lifecycle | Actor events resolve the actor's owner, including detached custody. Destruction continuations capture that registration. World cleanup visits every retained owner and attempts independent releases even after another throws. Finished command sweeps, failed callbacks, native Charge residue and pending split state prevent premature retirement. |
| Actor expenditure and participation | One `UnifiedMountedTurnCoordinator` and `MountedMovementStateAdapter` keep native grant/allocation history outside relationship lifetime. The ownership callback uses the existing split/adoption retirement predicate; no allocation clearing, preparation, refund or turn replay is added. Legacy unmounted combat restoration registers retained participation, including a pending-only survivor. |
| Shared world operation | One root, patch bridge, blueprint/control registration, native roster/turn integration, transition replay owner and persistence operation remain. Save readiness aggregates the existing preparation, relationship-transition and Charge settlement predicates; teardown aggregates the existing Charge fence. World scans and worker fences still run once. Native all-pair capture/restore/area/removal and participation integration are the next tranche. |

`MountedArchiveData` schema 3 contains a mounted-record collection, a separate
retained-participation collection (which can exist with zero mounted records),
owned native control slots, and exactly one shared `SavedCombatData` snapshot.
The old singular `Combat.Paired` field must be empty in this envelope. Pair and
participation identities/generations, cross-role uniqueness, references, shared
native debt and current-turn claims are validated. Mounted and detached owners
together cannot exceed twelve. Slots remain bounded native actor references;
resolving those native actors is a future restore-adapter responsibility, not
permission inferred from successful decoding.

`MountedArchiveCodec` first applies the unchanged historical schema-1/2
validation, then clones valid data and migrates it in memory. Original JSON,
native debt, actor/activation identities, allocation history, split state and
renewal boundaries remain unchanged. Formats without a stored pair identity get
a deterministic synthetic custody ID and generation 1; those values do not grant
native actions. This adds no save-file migration and no downgrade guarantee.

| Bound | Schema 1/2 historical reader | Schema 3 foundation |
| --- | --- | --- |
| Characters / tokens / depth | 32,768 / 4,096 / 12 | 131,072 / 32,768 / 12 |
| UTF-8/archive-member bytes | Existing 131,072 storage bound | 131,072 on encode and decode |
| Mounted / retained / total owners | Singular historical fields | 6 / 12 / 12 |
| Shared combat actors / owned slots | 64 / 128 | 64 / 128 |

All limits apply together; cardinality alone does not guarantee that arbitrary
record contents fit the wire bounds. Tests include six representative mounted
records, two detached records without membership, pending-only migration,
collection and wire overflows, malformed historical input and contradictory
cross-record claims.

`MountedSaveData.CurrentSchema` and `NativeMountedSaveFormat.WriterSchema`
remain 2. Native storage deliberately uses the historical reader; restoration
requires source schema 1 or 2 and the existing schema-2 native DTO. The new codec's
`Current` result is not accepted as native restoration authority. Schema 3,
including a zero-pair/multiple-participation archive, remains non-restoring and
preserves its original JSON. An unreadable original still refuses writing.

Component tests execute the production registry, relationship coordinator and
actor-allocation lifetime with an external movement/view lease substitute.
The existing Charge contract script additionally exercises the compiled
relationship services, lookup, aggregate readiness, capacity and independent
cleanup retry with detached native actors. These are offline component/adapter
checks, not two-pair Unity gameplay. Exact commands, counts, artifact identity and
publication state are recorded in [CURRENT.md](../CURRENT.md).

## 5. Persistence design direction and compatibility

Schema 3 is the opening assignment's starting design, subject to newer actual source. Prefer one world envelope with campaign/area/clock/rules, a bounded mounted-pair collection, owned control slots and one combat snapshot containing bounded paired-participation records. Retained detached obligations do not require a current mounted entry. Keep existing pair/combat debt validation unless a narrower change demonstrably improves the model; avoid rewriting every DTO for aesthetics.

Decode schema 1 and 2 under their actual historical rules, then migrate valid values in memory. Preserve native expenditure, activation/split state and original input bytes. The old schema-unchanged qualification remains historical evidence; a deliberate future schema migration does not erase it.

Current code retains original JSON for safely read future/invalid data and carries it forward instead of saving empty metadata. When original bytes cannot safely be retained, the existing path refuses the write. Preserve both behaviors. Current limits include 32,768 JSON characters, depth 12, 4,096 tokens and a 131,072-byte archive-member bound; collection bounds also apply. Select and verify new capacity across all limits rather than increasing only the pair count. [Save model][save-model] [Archive storage][archive-storage]

Do not change the emitted runtime format merely by changing a shared constant. Code must distinguish codec support from native restoration support and select the supported native adapter explicitly; successful Decode or a codec's `Current` status cannot authorize native restoration. Version-3 native writing becomes eligible only after complete world integration and selected native proof.

Recommended rollback qualification uses untouched pre-migration saves and the retained previous package. Opaque preservation does not establish that an older binary can resume new gameplay semantics. Any broader downgrade promise must be explicitly chosen and demonstrated before publication.

## 6. Later delivery sequence and finite gates

The labels below organize future assignments; they do not automatically select execution or change the original release requirements.

| Tranche | Work and exit |
| --- | --- |
| **7A: source foundation** | The selected initial ownership/dispatch/migration implementation. Focused source evidence, one-pair live adapter retained. |
| **7B: native multi-pair world integration** | Adapt native activation/commands, all-pair snapshot/restore, area/lifecycle/disable/removal and aggregate worker ownership. Enable two pairs coherently. Selected native proof: normal controls, selection changes, concurrent RT intent, both TB initiative arrangements, unrelated actors, local failure, distinct expenditure through manual save/cold load, and area restoration before autosave. Cover affected quick/auto/removal paths with existing scenario families. |
| **7C: profiles and progression** | Certify Medium baselines and one declared Small body/rig/mount combination. Verify real new/existing campaign acquisition, leveling, replacement and retraining. Establish the feasible six-pair stress fixture. |
| **7D: measured hardening** | Measure lifecycle subscriptions/retained owners, hot-path allocation/frame cost/logging, repeated cycles and unmounted controls. Repair observed problems. Complete normal-player configuration/help and the selected compatibility scope. |
| **7E: exact candidate rehearsal** | At the owner-selected stable milestone, identify/freeze one candidate and harness, perform Full plus the agreed native/release checks, migration/install/upgrade/removal/rollback and both-mode sessions. Record remaining human gates separately. |

Do not automatically replay every historical Chunk 6 transaction at 7A or after every correction. Changes to shared native action or persistence behavior do require appropriately selected affected regression when the native tranche is reached. No new mandatory failure becomes optional merely to declare completion.

### Profile, acquisition and performance specifics

The actual profile declaration is `Integration/SupportedMountedProfile.cs`. Only Medium Horse/Mammoth exist; its resolver takes the mount blueprint, and admission/retention separately require Medium size. Small support must include rider-aware selection, native eligibility, profile identity, pose/rig evidence and persistence. A minimal candidate to investigate is one native Small humanoid body on the existing Large Horse; it is not pre-certified. [Profiles][profiles] [Runtime][runtime]

Retain horse companion registration and native AddPet/progression ownership. The exact Ranger-option append checks seven stock GUIDs in order, creating a concrete compatibility boundary for mods that change that selection. Qualification must use ordinary acquisition paths, not only direct feature injection. [Horse service][horse]

Existing performance candidates include per-update Horse progression scans/signature construction and pair-runtime view/invariant checks. They are source-based measurement targets, not measured regressions. Measure first and keep necessary ownership checks while optimizing demonstrated costs.

## 7. Workflow, autonomy and reporting

Ordinary development uses focused checks under the current policy, with a 15-minute planning target and 30-minute ceiling for validation, excluding safe settlement/restoration already underway. This is not an implementation time limit. Candidate includes Fast; Full/native/purity/release qualification requires selected scope. Verify real test selectors and nested calls; reject empty selections. Reuse unchanged candidate inputs and keep product, compiled observer/fixture, external reader and documentation provenance distinct. [Validation policy][validation]

Use realistic production components and stable behavior contracts. Batch related corrections. At repeated equivalent failures, inspect the missing causal boundary instead of increasing retries. Preserve failed evidence and guard/threshold semantics. One engineer integrates shared routing/turn/save edits; parallel read-only reviews or genuinely independent work can assist. Keep one live executor for later authorized lab work.

Preserve the actual human installation and protected saves. Future transactions use existing locks, qualified disposable fixtures, controlled staging and exact restoration. Protected baseline saves, human saves, foreign mods, frozen worktrees and historical artifacts are not scratch data. A lack of human/visual evidence does not prevent safe source work, and source tests do not supply that evidence.

Keep this brief for stable scope/architecture. Keep actual branch/commit, results, blockers and the next step in the active `CURRENT.md` checkpoint. Use the repository's guarded publication route. A blocked push leaves the local committed work intact and is reported accurately; it does not justify bypassing a guard or abandoning independent source work.

Resolve routine design and test choices from evidence. Decisions genuinely requiring later owner selection include changed feature scope, waived release gates, unsupported fixture authority, a broader compatibility/rollback promise, human acceptance, and promotion beyond the current assignment.

The final Chunk 7 engineering disposition is **ENGINEERING COMPLETE - OWNER ACCEPTANCE PENDING** only when the agreed engineering breadth/release requirements have evidence on the selected artifact. A source foundation remains a source foundation. An accepted/public release additionally requires the actual owner acceptance and promotion decisions.

## Source register

The online references below are pinned to the reviewed main snapshot, except the explicit comparison. Use current repository paths for execution and these pins to understand provenance. No proprietary game implementation, save bytes or private raw logs are included.

[merge]: https://github.com/howardreith/KingmakerMountedCombat/commit/96c7fa1919f371ebd1100aab66f40b5f471644bb
[comparison]: https://github.com/howardreith/KingmakerMountedCombat/compare/d8bd2413059ad410772faa715efc320877ab0f68...96c7fa1919f371ebd1100aab66f40b5f471644bb
[agents]: https://github.com/howardreith/KingmakerMountedCombat/blob/96c7fa1919f371ebd1100aab66f40b5f471644bb/AGENTS.md
[validation]: https://github.com/howardreith/KingmakerMountedCombat/blob/96c7fa1919f371ebd1100aab66f40b5f471644bb/docs/VALIDATION-POLICY.md
[current]: https://github.com/howardreith/KingmakerMountedCombat/blob/96c7fa1919f371ebd1100aab66f40b5f471644bb/CURRENT.md
[closeout]: https://github.com/howardreith/KingmakerMountedCombat/blob/96c7fa1919f371ebd1100aab66f40b5f471644bb/docs/CHUNK6-ENGINEERING-CLOSEOUT-2026-10-11.md
[root]: https://github.com/howardreith/KingmakerMountedCombat/blob/96c7fa1919f371ebd1100aab66f40b5f471644bb/src/KingmakerMountedCombat/CompositionRoot.cs
[relationship]: https://github.com/howardreith/KingmakerMountedCombat/blob/96c7fa1919f371ebd1100aab66f40b5f471644bb/src/KingmakerMountedCombat/Integration/GameMountedRelationshipService.cs
[patches]: https://github.com/howardreith/KingmakerMountedCombat/blob/96c7fa1919f371ebd1100aab66f40b5f471644bb/src/KingmakerMountedCombat/Integration/MountedPatchController.cs
[controls]: https://github.com/howardreith/KingmakerMountedCombat/blob/96c7fa1919f371ebd1100aab66f40b5f471644bb/src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs
[player-actions]: https://github.com/howardreith/KingmakerMountedCombat/blob/96c7fa1919f371ebd1100aab66f40b5f471644bb/src/KingmakerMountedCombat/Integration/MountedPlayerActionController.cs
[combat]: https://github.com/howardreith/KingmakerMountedCombat/blob/96c7fa1919f371ebd1100aab66f40b5f471644bb/src/KingmakerMountedCombat/Integration/MountedCombatController.cs
[charge-cleanup]: https://github.com/howardreith/KingmakerMountedCombat/blob/96c7fa1919f371ebd1100aab66f40b5f471644bb/src/KingmakerMountedCombat/Integration/MountedCombatController.ChargeCleanup.cs
[allocations]: https://github.com/howardreith/KingmakerMountedCombat/blob/96c7fa1919f371ebd1100aab66f40b5f471644bb/src/KingmakerMountedCombat/Integration/MountedMovementStateAdapter.cs
[activation]: https://github.com/howardreith/KingmakerMountedCombat/blob/96c7fa1919f371ebd1100aab66f40b5f471644bb/src/KingmakerMountedCombat/Integration/PairedActivationLifecycle.cs
[turn]: https://github.com/howardreith/KingmakerMountedCombat/blob/96c7fa1919f371ebd1100aab66f40b5f471644bb/src/KingmakerMountedCombat/Integration/UnifiedMountedTurnCoordinator.cs
[save-model]: https://github.com/howardreith/KingmakerMountedCombat/blob/96c7fa1919f371ebd1100aab66f40b5f471644bb/src/KingmakerMountedCombat/Integration/MountedSaveData.cs
[combat-model]: https://github.com/howardreith/KingmakerMountedCombat/blob/96c7fa1919f371ebd1100aab66f40b5f471644bb/src/KingmakerMountedCombat/Integration/SavedCombatData.cs
[paired-persistence]: https://github.com/howardreith/KingmakerMountedCombat/blob/96c7fa1919f371ebd1100aab66f40b5f471644bb/src/KingmakerMountedCombat/Integration/PairedActivationPersistence.cs
[persistence]: https://github.com/howardreith/KingmakerMountedCombat/blob/96c7fa1919f371ebd1100aab66f40b5f471644bb/src/KingmakerMountedCombat/Integration/MountedPersistenceService.cs
[deferred-saves]: https://github.com/howardreith/KingmakerMountedCombat/blob/96c7fa1919f371ebd1100aab66f40b5f471644bb/src/KingmakerMountedCombat/Integration/MountedDeferredSave.cs
[area]: https://github.com/howardreith/KingmakerMountedCombat/blob/96c7fa1919f371ebd1100aab66f40b5f471644bb/src/KingmakerMountedCombat/Integration/MountedAreaPersistence.cs
[lifecycle]: https://github.com/howardreith/KingmakerMountedCombat/blob/96c7fa1919f371ebd1100aab66f40b5f471644bb/src/KingmakerMountedCombat/Integration/MountedLifecycleSubscriber.cs
[removal]: https://github.com/howardreith/KingmakerMountedCombat/blob/96c7fa1919f371ebd1100aab66f40b5f471644bb/src/KingmakerMountedCombat/Integration/MountedRemovalPreparation.cs
[archive-storage]: https://github.com/howardreith/KingmakerMountedCombat/blob/96c7fa1919f371ebd1100aab66f40b5f471644bb/src/KingmakerMountedCombat/Integration/NativeMountedSaveStorage.cs
[profiles]: https://github.com/howardreith/KingmakerMountedCombat/blob/96c7fa1919f371ebd1100aab66f40b5f471644bb/src/KingmakerMountedCombat/Integration/SupportedMountedProfile.cs
[runtime]: https://github.com/howardreith/KingmakerMountedCombat/blob/96c7fa1919f371ebd1100aab66f40b5f471644bb/src/KingmakerMountedCombat/Integration/KingmakerMountedPairRuntime.cs
[horse]: https://github.com/howardreith/KingmakerMountedCombat/blob/96c7fa1919f371ebd1100aab66f40b5f471644bb/src/KingmakerMountedCombat/Integration/HorseCompanionBlueprintService.cs
