# Mission: investigate and fix the post-combat save and door lock (owner assignment, 2026-11-22)

Recorded verbatim as assigned by the owner (Howie) on 2026-11-22. Execution record:
[the handoff](SAVE-AND-DOOR-LOCK-HANDOFF.md).

**Execution direction given with the assignment (owner, same message):** "Please develop in this thread and use our old
review procedure with you developing and me sharing outputs with Pro. Do not use the new approach." The work therefore
follows the manual procedure of [the PR #57 continuation](MOUNTED-COMBAT-CITY-AND-PRONE-MANUAL-CONTINUATION-MISSION.md):
Claude implements and runs focused checks, then hands a concrete commit and handoff to Howie, who obtains Pro's review.
Section 2's agent-review controller (enrollment, `push`/`verify`/`review`/`finalize`, Codex reviewer) is not used; the draft
PR is opened through the ordinary audited push route instead of by enrollment. Everything else in the mission applies.

---

Mission: investigate and fix the post-combat save and door lock
Owner: Howie Reith
Date: November 22, 2026
Primary repository: howardreith/KingmakerMountedCombat
Execution: interactive Claude development with independent Codex review; final owner review

## 1. Objective

Investigate and fix the reported Kingmaker state in which projectiles appear to remain active after combat, saving
repeatedly waits or fails, and doors cannot be opened. Establish the actual cause, implement the smallest coherent repair,
and demonstrate that ordinary saving and door interaction work afterward without losing or duplicating combat effects.

Carry this through diagnosis, implementation, focused validation, independent review, and a reviewable draft PR.
Instrumentation is an investigation step. A patch that only improves the warning or releases a lock while the underlying
work remains unsafe is not a completed repair.

Use the findings below as evidence and starting hypotheses. The immediate save-wait mechanism is established; the
particular action, effect, or ownership condition preventing readiness is still unknown.

## 2. Work in the main Claude thread

Keep implementation and corrections in the current interactive Claude session. Show meaningful progress: what you found,
what you changed, the relevant check result, and the current reviewer finding being addressed. During reviewer execution,
expose its supported progress output or a readable log tail where available; summarize activity and elapsed time without
presenting an unchanging "waiting" message as progress.

Use the existing agent-review controller's foreground path. The current source supports an interactive developer
submitting a report for a separate Codex review round. The controller's generic README examples use headless mode, so do
not copy those examples unchanged.

| Setting | Required value |
|---|---|
| Installed controller entry point | C:/Dev/agent-review/agent_review.py; verify the actual installed version and help |
| Project | kingmaker-mounted-combat |
| Plan | single |
| Mode | foreground, explicitly selected |
| Developer | Claude Opus, high effort, in the genuine interactive session |
| Reviewer | Codex gpt-6.2-astra, max reasoning effort |
| Gate | The project's required focused gate, bound to the candidate commit |
| Final handoff | Current independent review and draft PR for Howie; AWAITING_OWNER_REVIEW when the controller finalizes successfully |

Read the installed guide, project configuration, report schema, and CLI help before enrollment. Preserve the real session
provenance required by the foreground path, including its genuine CLAUDE_CODE_SESSION_ID; do not invent a session ID or
fabricate a transcript.

Create an isolated codex/* worktree from the current primary target branch, preserve this mission as a tracked file, and
publish the clean starting branch through the approved route. A suggested branch name is codex/mounted-save-and-door-lock;
use another compliant name if it already exists. Enrollment creates the draft PR, so do not create a duplicate PR first.

The intended command sequence, after resolving real paths and checking installed support, is:

```
$ar = 'C:/Dev/agent-review/agent_review.py'
python $ar check-project --project kingmaker-mounted-combat --repo $worktree
python $ar enroll --project kingmaker-mounted-combat --worktree $worktree --plan single --mission $mission --mode foreground --title '[draft] Fix post-combat save and door lock'
# Implement and commit in this interactive session.
python $ar push --enrollment <returned-id>
python $ar verify --enrollment <returned-id> --gate focused -- -Tier Fast -Focused <actual-focused-script-name.ps1>
# Produce the real developer report using the installed schema and verified candidate identity.
python $ar review --enrollment <returned-id> --report <actual-report-path>
# Address findings here and repeat the supported report/review cycle.
# Once the controller reaches its finalize phase:
python $ar finalize --enrollment <returned-id>
python $ar status --enrollment <returned-id> --remote
```

Replace placeholders with returned IDs and real paths. Select an actual focused script for the changed behavior; the
placeholder is not an existing script. The current single plan's checkpoint ID is final; use the installed report schema
and genuine validation/disposition records. The reviewed project configuration allows five completed review rounds per
checkpoint and has a three-round no-progress limit; verify and respect the installed limits.

There is no dedicated controller tail command in the inspected version. Discover the active reviewer log from the real
enrollment state; its event stream is under rounds/rNN/events-N.jsonl, with a corresponding stderr file. A read-only tail
in a visible terminal can show that review while the interactive Claude session remains the writer. Do not invent a path
or run a second reviewer to produce visible output.

Do not invoke the headless run loop, start a second writer, silently change models, edit the installed controller, or
re-enroll to reset findings or budgets. If foreground review is unavailable on the installed version, continue useful
investigation, implementation, and focused checks interactively; report the specific review integration limitation at the
handoff instead of turning this into a controller-development project or substituting a headless writer.

This is a new incident assignment. Leave unrelated PRs, enrollments, branches, and worktrees alone. Routine investigation
and correction rounds do not require an owner checkpoint. This mission includes scoped source changes, tests, guarded
branch publication, a draft PR, and the established automated review exchange. Merge, release, tags, permanent
installation, human reviewer requests, and interruption of an active human game session remain outside this assignment.

## 3. Incident evidence and starting identities

### Primary input

Original file: output_log(20261122-180838).txt
Size: 2,326,054 bytes
SHA-256: 8b0e422fd84d59cb9b700ea7fa3db039cfdb8d75afe72affbbd76773730edd30

Preserve that original as private diagnostic evidence. Do not commit the entire personal game log or any human save to a
public repository. The line references here use LF-delimited numbering, as rg -n or nl does; the last line is 10797. There
are stray carriage returns, so other line-splitting methods can differ by three lines.

The embedded excerpts below are enough to begin source investigation. Read the complete original when available. If it is
missing from your session, do not pretend it was supplied or block all useful work on retrieving it. Identify any concrete
evidence gap that later requires it.

### Reported environment

| Component | Evidence |
|---|---|
| Game | Pathfinder: Kingmaker 2.1.7b |
| Unity Mod Manager | 0.33.0 |
| Mounted Combat | 0.2.14, successfully loaded |
| Tabletop Rules Expansion / Gunslinger | 0.0.194; log build label 0.0.194-bag-of-tricks-no-item-weight |
| Waking Rune | 0.12.3 |
| Last Azlanti Preserver | 0.1.3; the failing requests report onlyOneSave=False |

Mounted Combat source inspected for the diagnosis was tag v0.2.14, commit 6151903116d418d5c96d4862a3fb9a68c843b377. The
corresponding TRE source reference is howardreith/KingmakerGunslingerAndOtherTabletopRules, tag v0.0.194, commit
805f9d59a14733170a07ce1d016e1df6647c79c1.

These are incident baselines, not instructions to reset newer work. Verify the actual checkout and current target branch.
Record installed DLL hashes, relevant settings, and fixture identity before claiming a matched reproduction. Version text
alone does not establish binary identity. Use the original log's full mod list when narrowing a compatibility
reproduction.

### Owner's symptoms

> Seem to be stuck in a state where projectiles are firing and save is permanently forbidden.
>
> Also FYI, I can't open doors when in this state.

The owner reported continuing projectile activity. The attached log does not independently establish repeated new shots;
its only logged firearm round consumption is at line 10761.

### Relevant chronology

| Log location | Observation |
|---|---|
| 10730–10735 | Mounted restoration succeeds. |
| 10750–10753 | Automatic mounted ranged action is canceled by a ground command before a child attack starts. |
| 10759–10764 | A mounted ranged action reloads a Reliable Pistol +1, consumes one round, and terminates when its target dies. |
| 10765–10768 | Another automatic mounted ranged action begins, reload completes, and the action terminates with an unrecorded interruption. |
| 10769–10776 | Party combat ends. |
| 10777–10782 | Out-of-combat mounted movement continues. |
| 10783 onward | Repeated quicksave attempts enter the deferred wait; several abort; another wait begins. |

Selected fields from the second interruption and first save request, wrapped for readability:

```
Mounted combat command terminal: action=RiderRanged; result=Interrupt;
cause=Unrecorded; childAttacks=1; rejectionCodes=CommandAdmissionFailure;
transaction=Cancelled; terminal=Failed

Mounted save request observed: type=Quick; name=Quicksave1; nativeOutcome=queued;
onlyOneSave=False; nativeAllowed=True; kmcCombatGate=open; inCombat=False;
turnBased=False; mode=Default; saveScope=False; saveFence=False;
saveDraining=False; restoreFailure=False; loading=False.

Saving after current actions and projectiles finish; new actions are briefly held.
```

Later requests report saveFence=True; one reports queuedBehind=DeferredSaveEnumerator followed by the generic type suffix.
Three failures say:

```
Save did not complete; the previous complete save is unchanged.
Save was not written: the native world changed while its effects were finishing.
```

The next request shows saveFence=False, followed by the waiting message again. There are no post-load runtime exception
traces identifying the blocker.

### Established behavior and remaining uncertainty

1. Mounted Combat's deferred-save path is active even though the native game reports saving allowed and combat ended.
2. SaveCommandStartPrefix prevents ordinary UnitCommand.Start calls while that wait is active, across actors. Door commands
   have no ordinary exception. This provides a direct mechanism for the door symptom, although no door attempt is recorded
   in the supplied log.
3. SaveEffectsReady checks relationship ownership, preparation, mount/dismount transitions, qualifying commands across all
   world units, unresolved projectiles, and non-ended ability processes. Its generic message does not identify which test
   failed.
4. A 30-second wall-clock timeout already exists for each activated save request. Its pre-serialization cleanup releases
   that request's fence. Queued requests can re-enter the hold sequentially. The log proves repeated deferral/failure, not
   a single permanently leaked fence.
5. "The native world changed" also covers a manual loading screen or a mode other than Default/`Pause`. The logged message
   does not identify the branch taken and does not prove an area transition.
6. The interrupted mounted attack/reload sequence is a useful lead, not a proven cause. A terminal attack log does not prove
   that a projectile, ability, related command, or ownership obligation was left alive.

## 4. Investigate the actual blocking lifecycle

Read AGENTS.md, docs/VALIDATION-POLICY.md, the current portions of CURRENT.md and AUTONOMOUS-RESUME.md, and the relevant
persistence/command contracts before choosing checks. Keep business logic in services or existing domain components; keep
patches as narrow adapters. Verify the actual Kingmaker 2.1.7b and installed Harmony12 contracts.

### A. Capture the reason for waiting

Add the minimum bounded observation needed to name the failed readiness condition. Cover the admission fence as well as
SaveEffectsReady: AcquireWorldSaveFence(scope) can prevent readiness before the effect checks run.

For each deferred save, record its identity, activation time, elapsed wall-clock wait, current mode, and the first blocking
condition. When the blocker changes or the request ends, report that transition. Include enough identity and state to
follow the relevant owner:

- Command: actor identity, command type, raw/queue ownership, started/running/finished state, target, and the native
  interruption/completion path.
- Projectile: blueprint, launcher/target where available, active/pending membership, cleared state, and whether KMC
  observed completion.
- Ability process: caster, ability/context identity, ended state, and delivery/cleanup stage where available.
- Mounted state: exact pair/generation and unsettled relationship, preparation, transition, or charge ownership.
- Failure: distinguish timeout, mode change, manual loading screen, real world replacement, cancellation, and
  serialization/cleanup failure.

Keep observations read-only, bounded, and contained if logging fails. Do not enumerate through an API that mutates native
iteration state, retain strong references that create the leak under investigation, emit per-frame floods, or add a second
authority for readiness. Reuse the actual production predicate or shared observation result so diagnostics describe the
decision that really blocked saving.

### B. Reproduce and distinguish causes

Begin with the smallest faithful sequence suggested by the log: mounted ranged attack, target death or interruption,
automatic reload/retarget, combat ending, then a quicksave and door interaction. Use the reported pistol and mod
combination when accessible. Record differences if a substitute fixture is necessary.

Inspect whether the blocker is a genuinely live effect, an already completed object misclassified as active, an incomplete
cleanup path, or a continuation that the save hold prevents from executing. Examine the precise lifecycle before deciding
which component owns the defect.

Useful controls are a native ranged weapon, an unmounted actor, and a projectile spell owned by another unit. Select
controls to answer a concrete question; do not launch a whole compatibility matrix. The global readiness scan means the
culprit can be an enemy or another caster.

Trace the second attack's Unrecorded interruption and the reload continuation if the evidence implicates them. Inspect
actual projectile/ability completion, not just the fact that the enclosing attack ended. Conversely, do not assume a
separate blocked child command: the ordinary mounted attack's ChildAttack currently refers to itself and its native
sequence starts through base.OnStart().

Produce a behavioral regression that fails on the relevant baseline before applying the fix. A faithful
component/integration reproduction can prove a mechanism, but label it separately from reproducing the owner's incident in
the game.

### C. Fix at the responsible boundary

Primary scope is Mounted Combat's deferred saving, action admission, and implicated command/effect lifecycle. Inspect
TRE's projectile and reload paths as needed. If the demonstrated cause requires a narrow TRE correction, that related
correction is in scope in a separate branch/PR under that repository's own instructions. Preserve each mod's independent
operation; do not add a required cross-mod dependency.

Restore correct completion or cancellation and correct save-readiness classification. A new timeout, a larger timeout,
suppressing the warning, bypassing the barrier, or deleting all live projectiles does not establish a repair. The existing
timeout is a fallback to preserve, not evidence that this incident has been solved.

If supported by the diagnosed failure, make any wait failure accurately name its reason and leave normal play usable once
the pending request has retired. Preserve native request ordering and the semantics of Manual, Quick, and Auto saves.
Coalescing or canceling queued requests needs a demonstrated reason and explicit tests for those semantics; it is not a
default redesign.

## 5. Required behavior and focused verification

Reuse existing behavioral and integration-style test infrastructure. Exercise production logic and real engine-facing
contracts where possible; fake only unavailable game boundaries. Assert player-visible results, actual lifecycle state,
resources, archive validity, and cleanup. Do not replace the production behavior with a duplicate test implementation or
rely on source-string assertions as proof of the fix.

| Case | Required result |
|---|---|
| Discovered triggering sequence | Baseline shows the defect; candidate completes or cancels correctly and permits a normal save and door interaction. |
| Legitimate in-flight projectile or ability | Effects resolve exactly once before serialization. No lost/duplicated damage, spell delivery, reload spending, ammunition, or action entitlement. |
| Unmounted or unrelated world actor | Its genuinely unfinished work remains protected; rider-specific cleanup cannot authorize an unsafe world snapshot. |
| Stop during a wait | Stop remains usable; selected actions cancel normally; mounted automatic proposals stay suppressed until deliberate input; no stranded ownership. |
| Several queued save attempts | Each request retains its proper outcome and cleanup; after the final request retires, ordinary interactions are admitted and no wait restarts without a new request. |
| Existing timeout and pre-write cancellation | The previous complete save is preserved, no success is reported, and that request's action hold/fence releases. |
| Mode change or loading transition during a wait | Correct continuation or safe failure according to verified native contracts; report the actual reason and leave no stale hold. |
| Pause state if activation, draining, or cleanup changes | Initially paused and initially running requests preserve the intended pause state after completion, timeout, cancellation, and queued retries; draining leaves neither unexpected running nor permanent pause. |
| Mounted transition or native preparation affected by the change | Previously permitted continuations still finish; no new save-versus-transition deadlock. |
| Idle state after combat | Saving completes promptly and doors work; there is no blanket 30-second delay for normal saves. |
| Save then cold load after the fixed sequence | The written save loads with expected party, mount relationships, position, relevant resources, and effects. |

Cover timeout, failure, and queue behavior in focused component/integration checks wherever that gives a faithful test.
Select only the native scenarios needed to establish the incident mechanism and player-visible repair. The primary native
acceptance is the formerly failing sequence followed by a successful save, working door, and successful reload. If shared
command admission changes, include the relevant turn-based neighbor as well as the reported real-time path. Do not broaden
to every historical scenario without a concrete shared risk.

Keep protection for a native archive worker that has actually begun writing until it settles. A pre-write timeout and a
post-commit cleanup failure are different outcomes. Do not mark the latter "previous save unchanged" merely because an
exception occurred.

Follow the repository's validation policy: focused Fast checks during development, Candidate when required for a frozen
native candidate, and no automatic Full/history campaign. Candidate already includes Fast. Reuse an unchanged verified
artifact. Follow the existing cumulative budgets; never restart a run or reset counters just to obtain a passing result.

## 6. Runtime and save safety

Use only the repository-owned guarded runtime workflow and qualified disposable fixtures. Preserve the actual installed mod
tree, settings, caches, and protected saves through the existing snapshot, lock, staging, and restoration controls. Use the
owner's current installation as the restoration target, not a historical fixture's versions. Never hot-replace a DLL or
interrupt a human game session.

A human save may be read or copied only through the applicable authorized workflow; never overwrite, strip, or repurpose
the original. Do not clear/refund actions, reconstruct an attack, replay delivery, call native preparation to manufacture
entitlement, or discard unrelated effects to make a save pass. Preserve stable blueprint, actor, pair, and archive
identities. Avoid a save-format change unless the established cause genuinely requires it.

If the exact save or a qualified native fixture is unavailable, finish the source investigation, faithful regression,
implementation, and focused checks that are possible. Supply a concrete next reproduction and the precise missing input.
Distinguish an implemented repair awaiting native verification from a diagnostic-only checkpoint. Do not claim the
reported gameplay incident fixed without the required evidence.

## 7. Independent review and completion

Submit the full feature diff, this mission, baseline/candidate identities, causal trace, failing-then-passing regression,
focused results, and any native evidence to the independent reviewer. The review must challenge whether the correction
fixes the real blocking lifecycle or merely makes a warning or lock disappear. Include previous findings and their
dispositions on subsequent rounds; do not feed the reviewer only selected favorable hunks.

Address supported findings in the same interactive Claude thread. Explain disagreements using code and evidence. Continue
within the installed limits without making Howie relay routine findings. Every material correction needs current
validation and a review of the actual candidate. Do not label an old review as approval of newer source. Use the
controller to publish its own review/status records; do not impersonate either role or edit controller-owned records.

The final handoff must contain:

1. Root cause, exact blocking owner and lifecycle, and why saving and doors were both affected.
2. What changed, why it fixes that cause, and the compatibility boundaries preserved.
3. Baseline and reviewed source commits, final PR head, PR link, enrollment identity, and any transcript-only successor
   commit identified separately.
4. Focused commands/results and the failing regression before the fix; distinguish component, native gameplay, and
   cold-load evidence.
5. Original log and fixture identities; candidate DLL identity when used; exact restoration result for any runtime
   transaction.
6. Each remaining limitation or required input, with an honest status: verified repair, implemented repair awaiting native
   acceptance, or diagnostic checkpoint.
7. A short owner playtest sequence covering the triggering attack/interruption, save, door interaction, and reload.

Completion means a supported causal explanation, a meaningful regression, a focused correction, independent review of the
final source, and demonstrated save/door recovery with safe persistence. The draft PR remains for Howie's final review.

## 8. Source map

Use the incident revisions for comparison and the current branch for implementation. Names below identify investigation
entry points, not a mandate to edit every file.

### Mounted Combat

- MountedDeferredSave.cs: readiness conditions, mode/world checks, 30-second wait, failure notification.
- DeferredSaveEnumerator.cs: per-request activation, timeout, disposal, serialization boundary.
- NativeDeferredSave.cs: native loading queue and retirement of failed owned saves.
- NativeSaveEffectBoundary.cs: projectile/ability membership and command-settlement rules.
- MountedPatchController.cs: SaveCommandStartPrefix, projectile completion hook, Stop handling.
- MountedPairAttackCommand.cs: native attack start, interruption, and termination.
- Nearby services as implicated: MountedPersistenceService.World.cs, MountedAreaPersistence.cs, MountedCombatController.cs,
  MountedDoorInteractionCommand.cs, NativeSaveCommitOutcome.cs, and NativeSaveWorkerBoundary.cs.

### Tabletop Rules Expansion

- EmptyFirearmAttackCommandPatch.cs: reload continuation and resumed attack admission.
- ReloadTestMusketAbilityLogic.cs: the reload delivery that emits ability.loaded.
- FirearmProjectileBlueprints.cs and FirearmProjectileVisualPatch.cs: projectile construction and launch visuals, if the
  blocker points there.

### Workflow

- Controller operating guide.
- Mounted Combat project configuration.
- Review protocol.

The installed supported commands and guards remain authoritative. The explicit foreground requirement in this mission takes
precedence over generic headless examples in the operating guide.
