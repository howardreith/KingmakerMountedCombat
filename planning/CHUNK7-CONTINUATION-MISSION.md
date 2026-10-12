# Kingmaker Mounted Combat — sustained Codex mission for Chunk 7B–7D

Prepared for Howie on 12 October 2026. This is the complete continuation assignment after the Chunk 7A foundation review. Suggested repository location: `planning/CHUNK7-CONTINUATION-MISSION.md`.

## 1. Mission and authority

Continue implementation of Kingmaker Mounted Combat through **a Chunk 7B–7D breadth candidate ready for engineering and owner play review**. Complete the review repairs, native multi-pair world integration, targeted native persistence/combat proof, declared profile/progression breadth, and measured hardening described below. This is one sustained assignment with internal checkpoints. Begin the work; a plan alone is not the deliverable.

This new owner assignment explicitly selects the necessary 7B–7D candidate creation and guarded native milestone tests. It supersedes the completed 7A task's instructions in `AUTONOMOUS-RESUME.md`, the original opening prompt, and historical handoff text to stop after source review and perform no candidate/native work. It does not override native action/resource contracts, live safeguards, protected-save rules, evidence requirements, or promotion restrictions. Record this new task selection in the active handoff without deleting the historical 7A evidence or rewriting the general validation policy.

You are authorized to inspect and edit the repository; add necessary behavior tests and bounded native observers/fixture support; compile; perform focused checks; obtain an independent source review; build and freeze private candidates; run the selected guarded native scenarios; and commit and guarded-push coherent changes to the verified review branch. Review can be provided by another agent; it does not require a human permission round at each milestone. Use one integration owner and one live-lab executor. Parallelize independent source analysis and isolated implementation where useful.

This assignment does not select a merge to `main`, tags/releases, permanent installation for human play, or a claim of human acceptance. Full 7E release rehearsal remains a separate selection. Deliver a substantial implementation and a reviewable private candidate, with any remaining mandatory or human gates accurately identified.

**A successful focused pass, source commit, pushed handoff, completed schema adapter, first two-pair test, or completed 7B tranche is a checkpoint within this assignment. Record it and continue through the remaining safely actionable selected work.** Do not ask whether to continue between these steps.

## 2. Establish the current state once

Start from the actual repository and local worktree state. Read `AGENTS.md`, `docs/VALIDATION-POLICY.md`, `CURRENT.md`, `AUTONOMOUS-RESUME.md`, `planning/CHUNK7-DEVELOPMENT-BRIEF.md`, and the foundation review supplied with this mission. Then inspect the relevant production paths and tests. Use the roadmap/oversight material for unresolved product requirements, not as an instruction to replay completed historical campaigns.

| Reference | Verified at preparation of this mission |
| --- | --- |
| Repository | `howardreith/KingmakerMountedCombat` |
| Review branch | `codex/mounted-combat-chunk7-foundation` |
| Foundation source/tests | `21df88347b98532fd59b82d3ae05078f7bac64e9` |
| Foundation handoff / remote HEAD | `01c6a1f1d5047803bd1121d85fcd619efcab2236` |
| Main | `96c7fa1919f371ebd1100aab66f40b5f471644bb` |
| Known Windows lab root | `C:\Dev\KingmakerMountedCombatLab` |

Refresh refs and inspect worktrees before using these identities. They are provenance, not reset targets or installation restoration targets. If the branch has advanced, inspect the new commits and reconcile the mission with actual completed work. Continue on the existing branch when safe; use a separate dedicated `codex/*` continuation worktree only when necessary to preserve occupied or unknown state. Preserve unrelated edits, frozen worktrees, historical artifacts and failed receipts.

The starting foundation is connected to the production one-pair path. It adds registry ownership, captured command/generation bindings, actor lifecycle routing, aggregate readiness and retained custody. Its native capacity is still one; it writes schema 2 and restores schemas 1/2. Schema-3 migration/validation exists offline. Do not spend the opening loop rebuilding an equivalent registry or rediscovering these completed changes.

The handoff reports a 10.56-second focused Fast pass, with compilation, 727 component tests, 147 source checks, 56 historical plus 43 new persistence checks, 121 ownership/Charge checks and 16 preparation contracts. Those are offline results, including detached native adapters. Inspect the local receipts as needed for this work; do not treat those counts or preview.211's historical native PASS as proof for a new candidate.

## 3. First work: reproduce and repair both source-review findings

The companion review contains pinned source references. Verify each finding against the actual current source, obtain its failing behavior reproduction, fix the cause, and run affected neighbors. If a newer commit has already fixed one, record the evidence and continue.

### R1 — repeated detached cleanup can restart the split-release round

The new retained-owner lookup allows a life-state callback to route an already-detached pair back through `Dismount`. `UnifiedMountedTurnCoordinator.HandleDismounting` calls `SplitPairedActivation`, which unconditionally rewrites `splitReleaseRound`.

Reproduce the boundary order: voluntary dismount in TB round N; native round advances to N+1 before ownership expiry is refreshed; a former pair actor becomes unconscious; the retained owner receives cleanup again. The repeated split must not move the release round to N+1 and suppress the already-unmounted mount for an extra round.

Make repeated detach/split idempotent while preserving cleanup retries, remaining command custody and real participation debt. Distinguish invalidating an active attachment from servicing a detached owner's obligations. Do not fix this by dropping every detached owner from lookup or clearing its expenditure. Verify that one pair's callbacks do not change another pair's participation when multi-pair support is connected.

Relevant files: `GameMountedRelationshipService.Ownership.cs`, `MountedLifecycleSubscriber.cs`, `GameMountedRelationshipService.cs`, `UnifiedMountedTurnCoordinator.cs`, and `PairedActivationLifecycle.cs`.

### R2 — migration rejects a valid mounted relationship with pending-only participation

Historical native removal can retire the activation while the attachment remains mounted: the rider leaves combat, the mount remains in combat, and capture contains mounted R+M plus `Paired` with null rider/mount/activation fields and `PendingSplitId=M`. The historical schema-2 validator accepts this state.

`MountedArchiveCodec.Migrate` only links participation when both saved paired identities equal the mounted pair. It therefore labels this pending-only record detached; `MountedArchiveData.Validate` then rejects the pending mount as overlapping active membership. The existing pending-only migration test first sets `Mounted=false`, so it misses this combination.

Add a regression for the valid historical mounted-plus-pending-only payload. Preserve its native debt, pending actor and release round, original JSON, and absence of an activation. Associate the obligation correctly without inventing missing native participation or discarding the exclusion. Retain negative coverage for genuinely conflicting owners and actors; do not globally permit cross-pair overlap to make this case pass.

Current native schema-2 loading is unaffected because it still uses the historical reader. Repair this delivered migration defect before selecting schema-3 native support. Relevant files: `PairedActivationTransitions.cs`, `PairedActivationPersistence.cs`, `MountedArchiveCodec.cs`, `MountedArchiveData.cs`, `SavedCombatData.cs`, and `tests/Chunk7PersistenceTests.cs`.

## 4. Contracts that govern the implementation

Keep one independent Kingmaker UMM mod using the actual local Kingmaker 2.1.7b / Harmony12 contracts, .NET Framework 4.7 and C# 7.3. No required new mod/library dependency. Verify local engine contracts where behavior depends on them; a Wrath type name is not a Kingmaker contract. Keep patches, event callbacks and UI thin, with behavior in services/domain code and bounded engine adapters.

The native engine remains the sole authority for initiative, action budgets, preparation, reactions, cooldowns and turns. Never clear, refund, synthesize or replay expenditure, nor call native `Prepare` to manufacture entitlement. Preserve exactly-once spending, both initiative arrangements, unrelated actors and ordinary unmounted behavior. Keep `UnifiedMountedTurnCoordinator` while active movement accounting depends on it; do not replace its obligations with a second competing turn authority.

Preserve the accepted feature dispositions: supported RT Charge remains default-off; TB Charge delivery remains `DEFER - EVIDENCED` with cost-free refusal; the defensive Mounted Combat feat remains unimplemented and deferred. Cavalier, Paladin Divine Steed, additional class grants, enemy riders, universal mount support, lances/barding, advanced feat chains, continuous move-and-fire animation and a second rules preset remain expansion scope outside this mission.

Maintain three distinct lifetimes:

- **World/session authority:** one native roster/current turn, clock, shared allocation ledger, save/load operation, archive worker ownership, global subscriptions and shared transition control.
- **Pair authority:** exact session, stable identity, generation, rider/mount objects, relationship/view/movement leases, admitted commands and pair-local cleanup.
- **Actor obligations:** native expenditure and retained split/preparation/cleanup obligations which can outlive an attachment. Dismount/re-pair, selection and mode changes must not erase them.

Selection supplies user intent and presentation context. Resolve execution by the actor/executor or the exact captured request/command/context owner; never redirect admitted work because the selected pair changed. Reject old world, old generation, changed object, foreign actor and consumed-shell work. An actor cannot belong to another pair while incompatible retained custody still owns it.

Cleanup must attempt independent owners even when another fails, retain unresolved obligations, and refuse unsafe shared completion. Pair-local failure must not tear down healthy unrelated pairs. A settled retained participation record need not block every save forever; readiness depends on the actual unsettled operation. Never release the shared operation because just the selected pair is ready.

## 5. Work package 7B — complete native multi-pair world integration

Implement the full dependency chain before lifting the staged native one-pair restriction. Design the narrowest coherent extension of the accepted architecture; do not duplicate global controllers/rosters/workers by instantiating one entire old composition root per pair.

| Boundary | Required behavior |
| --- | --- |
| Composition and ownership | Create/dispose independent pair boundaries under one world service. Route global bridge calls deliberately and actor calls to the exact owner. Keep bounded active and retained capacity, exact object identity, and session invalidation. |
| Controls and admitted work | Resolve availability/targeting by actor. Bind mount/dismount, ground/door movement, rider/mount attacks, Charge, relevant casting and delayed cleanup to the actual pair/generation. Preserve consumed/retired command semantics and native costs. |
| Turns and allocations | Support multiple pair participations within the one native encounter/roster/current turn. Preserve actor allocations across detach/re-pair and mode changes. Handle both initiative arrangements, split survivors, interruptions and complete activation ending without extra preparation. |
| Capture | Capture every active relationship, owned control slot and retained participation, including zero mounted pairs with multiple detached obligations. Capture world combat/clock/roster/allocation state once. Preserve owner/generation links and actual debt. |
| Load and restoration | Discard the old world once; validate and resolve the new world graph and exact native actors; restore shared state once and pair-local state independently at the proper engine phase. Do not loop a legacy restore method whose first operation discards the whole world. No actions or preparation may be granted by restoration. |
| Area transition | Capture/suspend every relevant pair and retained obligation; restore the intended pairs before the affected autosave boundary; settle or retain missing actors and partial restoration explicitly. Do not accidentally serialize only a current UI projection. |
| Save and worker fencing | Aggregate preparation, relationship transition, Charge, command and relevant lease readiness. Fence all participating owners for the actual native enumeration, iterator and asynchronous worker lifetime, including cancellation/fault paths. The operation's own admission fence must not prevent its readiness indefinitely. |
| Lifecycle and teardown | Handle actor death/incapacitation, view replacement, companion removal/replacement, session/load changes, area unload, disable, disposal and removal preparation. Retire only settled records; make retry paths idempotent and attempt independent owners after failures. Preserve mod-owned slot/fact cleanup and unrelated actor data. |

Native schema-3 writing/restoration becomes eligible only after these connected production paths and their focused regressions are complete. Keep explicit codec support versus native adapter selection; do not merely increase a schema constant or capacity value. Use a conservative declared native capacity consistent with implemented bounds and the evidence you obtain. Offline capacity six is not native six-pair qualification.

Decode schemas 1 and 2 using their historical shape, type, bounds and semantic rules, then migrate valid data in memory. Preserve safely read unsupported/invalid original JSON unchanged; if it cannot safely be preserved, retain the existing refusal to write. Check the current character, UTF-8 byte, token, depth, collection and archive-member bounds together against representative maximum captures. Do not relax unrelated limits or validators to admit malformed data.

All schema-3 actor, owner, generation and current-turn claims must agree across the graph. Cover attached and detached participation, pending-only survivors, duplicate/cross-role membership, stale worlds, unresolved actors, contradictory native debt, invalid references and overflow. Unsupported data must remain preserved and clearly non-restoring.

## 6. Work package 7C — declared profile and progression breadth

Continue after the durable two-pair milestone; do not stop there if this work is safely actionable.

Preserve the existing Medium Horse and Mammoth contracts and implement at least one specifically declared Small-rider body/rig/mount combination. `Integration/SupportedMountedProfile.cs` currently resolves by mount blueprint, while admission/retention and pose also assume Medium. Extend the complete eligibility/profile/pose/persistence path coherently. Actual effective native size, reciprocal companion ownership and the chosen rig remain authoritative.

A native Small humanoid on the existing Large Horse is a reasonable first candidate to investigate; it is not pre-certified. Choose from actual available native bodies/fixtures, measure attachment and pose during representative movement and combat, and publish the precise supported body/equipment/form combinations and exclusions. Do not declare every Small race, transformation or mount supported because one rig worked. Preserve independent health, defenses and effects, with the mount as physical mover and accepted carried-movement costs unchanged.

**Separate new-format profile validation from historical schema-2 validation.** `MountedArchiveData.Validate` currently reuses a legacy DTO validator for each pair. Small profile eligibility belongs in the new-format contract; simply broadening the historical profile allowlist changes the old save contract. Reuse shared invariant helpers where appropriate while keeping version-specific historical acceptance explicit.

Qualify actual horse acquisition through ordinary native choices for a new character and an existing campaign. Keep native AddPet and companion progression ownership. The current Ranger-option append checks seven exact stock GUIDs in order; inspect and preserve that compatibility boundary. Direct diagnostic feature injection alone cannot prove acquisition. Cover relevant leveling/progression transitions, dismissal/replacement, retraining, and cleanup of an owned mounted pair during these operations. Use qualified disposable fixtures; no human campaign edits.

Establish the safely supported six-pair lifecycle/performance stress fixture and exercise it if the engine and guarded fixture path permit. Reuse or extend repository-owned fixture/scenario infrastructure with proper qualification. Do not weaken older scenario guards or fabricate a party by modifying protected saves. If six pairs are genuinely unavailable, record the exact engine/fixture prerequisite as an open mandatory gate and continue the remaining selected work; do not relabel two-pair evidence as six-pair stress.

## 7. Work package 7D — measured hardening and supported configuration

Measure the changed product in comparable scenarios before optimizing. Include unmounted control, one pair, two pairs, and the feasible six-pair stress case. Record actual environment/candidate/fixture identities, sampling conditions and measurement limits. Use repeated mount/unmount/area/load/cleanup cycles to distinguish legitimate retained obligations from subscriptions, views, commands or owners that accumulate after settlement.

Measure frame cost, managed allocations where observable, log volume, subscriptions and retained owners. Existing source targets include horse progression scans/signature construction and runtime view/invariant checks; these are hypotheses, not measured regressions. Repair demonstrated costs or unbounded retention while keeping required validation and cleanup. Establish bounded acceptance criteria from the measurements and existing product policy; do not invent a performance PASS from a quiet main menu or optimize away safety checks to reach an arbitrary number.

Make the accepted CRPG transport preset and turn architecture the normal supported configuration. Keep incompatible historical experimental authorities inaccessible during ordinary play; diagnostics must be opt-in and inert by default. Complete concise normal-player configuration/help and useful refusal feedback. Preserve representative unmounted controls and affected mounted movement, attack, casting, restrictions and reaction behavior.

This mission selects base-game compatibility proof using an isolated test configuration containing KMC and the required UMM/Harmony dependencies, with actual installed versions. Use only a repository-guarded isolation method that preserves the human installation and restores it exactly. Do not modify foreign mod code or describe a run with additional gameplay mods as base-only proof. If the guarded runner cannot provide that isolation, record the specific blocked proof and continue independent selected work. Do not expand into a general mod-compatibility matrix; keep additional unselected mod combinations visible as future scope.

## 8. Selected native evidence campaign

After coherent production changes pass focused checks and independent review, build/freeze the private candidate and run a **finite targeted milestone campaign**. Inspect existing runner/reader/scenario entry points before choosing commands; do not assume a two-pair runner already exists. Extend bounded scenario families where necessary. Instrumentation may observe or arrange a qualified fixture; assertions must establish actual game behavior through the production controls and paths.

Before launching, record the scenario manifest, dependency/risk covered, candidate and harness identities, fixture/configuration, existing stage timeouts, estimated duration, progress checkpoints and restoration plan. Reuse a candidate across relevant cases. The following evidence families are selected; combine compatible observations without concealing skipped behaviors:

| Family | Required observations |
| --- | --- |
| Exploration and dispatch | Two normal mounted pairs, independent movement/selection, affected door/interaction paths, and selection changes while work is admitted. Commands remain with their captured actors/pair. |
| Real-time combat | Concurrent pair intent, distinct rider/mount expenditure, representative attack/casting and supported RT Charge, cancellation or failure local to one pair, and unrelated/unmounted actors remaining functional. |
| Turn-based combat | Both rider-before-mount and mount-before-rider arrangements across the participating pairs; full activation/spending/end-turn observations; independent reactions and movement budgets; unrelated roster actors. Preserve the documented cost-free refusal for unsupported TB Charge. |
| Detach and retained custody | Dismount/re-pair and mode boundaries, the R1 duplicate-cleanup round-order regression, native actor removal/pending survivors, and one failed cleanup without another pair's owner or resources being lost. |
| Durable multi-pair state | Manual, quick and auto saves through their affected actual paths; distinct actor expenditure across native save/cold load in RT and TB; all-pair restoration; zero mounted pairs with retained obligations; no free preparation or duplicate turn grants. |
| Historical migration | Valid schema-1/2 disposable inputs, including R2's mounted-plus-pending-only case, restored through the selected native adapter and saved under the deliberate new format. Retain negative malformed/unknown-data protection. |
| Shared boundaries | Area restoration before autosave, save deferral for an unsettled nonselected owner, worker/iterator completion and fault/cancel cleanup, old-session command rejection, and affected disable/removal behavior. Use fault seams tied to concrete risks, not a combinatorial failure campaign. |
| Profiles and progression | Medium baseline preservation, the declared Small combination, ordinary new/existing-campaign acquisition, relevant leveling, replacement/retraining and pair cleanup. Record visual evidence without granting yourself human visual acceptance. |
| Stress and hardening | Safely feasible six-pair stress, repeated settled lifecycle cycles, measured costs/retained owners, diagnostics-off behavior and representative unmounted controls. |

Where candidate behavior changes during these packages, bind evidence to the bytes that actually ran. Earlier evidence remains useful historical evidence but cannot simply be relabeled PASS on the final DLL. Review the final diff since each proof and select affected reruns for the final candidate, especially when shared command, turn, observer, fixture or persistence code changed. Preserve both the older evidence and the reason for the new selection.

Rollback assurance in this milestone means the retained previous package plus untouched pre-migration saves, through the guarded restoration path. Opaque preservation does not prove that an older binary can resume new-schema gameplay. Do not promise that downgrade without separate evidence. Exercise the affected cleanup/removal path; do not silently select the whole 7E install/upgrade/release rehearsal.

## 9. Validation cadence and live safeguards

The ordinary development validation policy retains its 15-minute planning target and 30-minute ceiling, excluding necessary safe completion/restoration. It is not a cap on implementation time or the whole assignment. This prompt separately selects the targeted native milestone campaign above; declare its finite budget before execution. Do not interpret historical 109-stage/105-row campaigns as mandatory after each correction, and do not hide broader child suites inside a narrow wrapper.

For the opening repairs, the established focused entry point is:

```powershell
.\scripts\Test-Tiers.ps1 -Tier Fast -Focused 'Test-PersistenceData.ps1,Test-ChargeOwnershipContracts.ps1,Test-MidEncounterNativePreparation.ps1'
```

Add/select actual affected neighbors as implementation changes. Reject zero-test selections. Add meaningful behavior regressions at the right seam; do not add assertion counts or source-string checks just to mirror new code. Disclose substitutes and the evidence boundary.

When freezing a candidate, use the actual `Candidate` tier with the Kingmaker assembly target and selected neighbors, following the current script contract. Candidate contains Fast; do not run both for unchanged inputs. Full and the full historical release ledgers are not selected merely by this assignment's length. Broaden only for an identified shared risk or a listed milestone requirement. Review the coherent change before expensive qualification and batch related fixes before rebuilding.

Use only repository-owned guarded staging/launch/qualification/restoration paths, with their identity and WhatIf checks before live use. Transactions must lock, snapshot the actual current installation/settings/caches and protected saves, stage controllably and restore exactly. Never restore to a historical preview pin because it appears in a document. Never hot-replace DLLs, interrupt a human game session, kill unrelated processes, alter foreign mods or bypass a guard.

`KMC_AUTOMATION_BASELINE` and all human saves remain protected. This mission authorizes the qualified disposable Working fixture and properly qualified, run-scoped isolated fixture/save copies needed for the selected cases. Preserve the older fixtures' strict guards. Mod-absent controls require an appropriate fixture without permanent KMC references; do not strip those references from a human save. Stop the affected live operation on Steam authentication, purchase, update, cloud, entitlement or unexpected dialogs. Preserve safe transaction settlement/restoration even if a check reaches its budget.

Treat repeated equivalent failures as a reason to inspect the missing causal boundary. Preserve the first evidence, change the hypothesis or implementation, then rerun the affected case. Do not repeatedly rebuild/relaunch unchanged inputs or weaken assertions, thresholds, completion validators, package allowlists or evidence identities to manufacture PASS.

## 10. Persistence across checkpoints and sessions

Maintain one active implementation plan linked to this mission. Update the existing development brief only where architecture or supported behavior actually changes; do not create competing roadmaps or replace it with an older attachment. Keep `CURRENT.md` concise and factual, and make `AUTONOMOUS-RESUME.md` identify the next concrete implementation/evidence action within this assignment.

At coherent checkpoints record: completed behavior; source and documentation commits; branch/remote relation; candidate DLL/package hash and MVID where applicable; harness/reader/fixture/configuration identities; actual commands, durations and counts; substitutions; preserved failed attempts; exact native restoration disposition; open gates; and the next action. Keep private raw receipts out of public Git payloads as required by the repository. Preserve source, product, reader, fixture and documentation identities separately.

Give concise progress updates during active work: what behavior is now implemented or proven, what remains uncertain, and what the next action will resolve. Report checkpoints without turning each update into a request for permission or a final handoff.

Use the project-owned guarded push helper when available. If none exists, follow `AGENTS.md`'s audited fallback: inspect tracked payload, origin, clean staged/checkpoint state, ancestry and fast-forward relation before an ordinary push, then verify remote HEAD. Do not reset/clean unknown work or force-push. A successful publication receipt is a resumption point, not a reason to end this mission early.

If a native row is blocked by a concrete environment/fixture prerequisite, settle the transaction, record the missing requirement, and continue independent selected source/tests/profile/hardening work. Ask the owner only when a material product choice cannot be resolved from the accepted scope, access genuinely requires owner action, or proceeding would cross a protected boundary. Ordinary architecture choices, focused test selection, private candidate creation and the selected guarded native cases already have authorization here.

If a tool, runtime or context limit forces a pause, leave a precise resumable handoff: current worktree/commit/artifact identity, settled transaction state, remaining work and the next command or code boundary to inspect. Do not claim the milestone complete or imply that work will continue after the session stops. On resumption, refresh the actual state and continue from that checkpoint without replaying completed setup and unchanged evidence.

## 11. Completion and final report

The intended endpoint is **“Chunk 7B–7D breadth candidate ready for engineering and owner play review.”** Reach it when the review defects are repaired, the connected multi-pair implementation and declared profile/progression/hardening work are delivered, required targeted evidence is collected on identified candidate inputs, and all selected live transactions are settled with exact restoration receipts.

Keep engineering behavior, fixture limitations and human judgment distinct. Missing required native evidence or an unavailable mandatory stress fixture prevents an unqualified completion claim. A human visual/play decision remains pending even when all automated observations pass. If a genuine blocker remains after independent safe work is exhausted, deliver the substantial completed work with a precise blocked disposition and prerequisites; do not rename the blocked row optional or report full Chunk 7 acceptance.

The final response must lead with the resulting behavior and milestone status, then give:

1. What changed and why, including the disposition of R1 and R2.
2. Supported pair capacity, profiles/configuration, native writer schema and accepted load schemas.
3. Focused and native evidence, exact candidate/source/harness identities, meaningful counts and actual durations, with limits and retained failures.
4. Current branch/commits/remote verification, private candidate/package location and concise owner-play instructions.
5. Exact installation/save restoration status and any live transaction still requiring recovery.
6. Remaining required/human gates, explicit deferrals, and the next review or separately selected 7E action.

Do not stop after writing this plan. Start with the actual state and the two review reproductions, then carry this selected mission forward.
