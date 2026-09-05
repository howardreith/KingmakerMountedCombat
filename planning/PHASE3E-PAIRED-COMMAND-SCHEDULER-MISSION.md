# Kingmaker Mounted Combat — Phase 3E Paired-Command Scheduler

Status: `PAIRED SCHEDULER PIVOT — SEPARATE-TURN FALLBACK READY`

## Final bounded disposition — 2026-09-05

The authorized primary scheduler proved its narrow command-lifecycle seam but did not qualify unified turn completion. Immutable dev.4 Mammoth runs A/B passed the minimal vertical slice `70/0` each (`140/0` total): the exact mount-owned Standard command started once, terminated once, charged the mount once, emitted one mount-owned rule chain, left the rider Standard unchanged, and kept the rider as `CurrentTurn.Unit`. The exact native seam is the eligibility result of `UnitActionController.TickCommandTurnBased(UnitCommand)` (`0x0600911D`).

The final bounded dev.12 Horse run `20260905T090000Z-phase3e-dev12-horse-tb-gate2-rerun` then passed an exact Horse Mount Primary action but failed later when `CombatController.ChooseNextUnit()` (`0x06000BD2`) retained the redundant Horse at the next round boundary. KMC entered its fail-closed separate-turn fallback, the relationship was safely cleared, and the comprehensive leaf ended `FAIL 8/1` (`31/1` Horse assertions). This is kill criterion K9 after repair cycle 2/2. The prior same-package process `20260905T082300Z-phase3e-dev12-horse-tb-gate2` stopped earlier on an inherited out-of-combat Mount admission race, so the two fresh processes also did not establish deterministic comprehensive traversal.

The one authorized rider-owned scheduling-shell investigation is closed without implementation. Historical shell commit `2b25bb45556ff62b9f421963ae81dc4d75d63412` placed a Standard wrapper in the rider container and charged rider Standard, which violates the required separate ledgers. A compliant no-cost rider shell could schedule one mount child, but it cannot change the later native `ChooseNextUnit` decision after that child and its lease are already disposed. Repairing that decision now requires broader pending-unit/roster/turn-controller ownership outside this mission.

The bounded fallback therefore ships as `0.1.0-phase3e-fallback.1` with `EnableUnifiedMountedTurn=false` and `EnablePairedCommandScheduler=false` by default. It preserves the accepted Phase 3C separate turns and inherited Phase 3D RT melee/ranged, native abilities, and presentation; it makes no unified-TB claim. Paladin implementation remains unauthorized.

## Authorization and controlling disposition

Phase 3D reached a valid architecture boundary and is closed as:

```text
BLOCKED — CRITICAL for stock-only unified turn-based command scheduling.
```

This mission authorizes the next architecture tranche: an original, purpose-built, pair-local command scheduler that permits one active mount to execute its own commands during its rider-owned turn while preserving separate action, command, weapon, animation, and rule ownership.

This is not authorization for a global Kingmaker turn-controller replacement.

## Repository identity

```text
Repository root: C:\Dev\KingmakerMountedCombatLab\repo\KingmakerMountedCombat
Repository:      howardreith/KingmakerMountedCombat
Phase 3D branch: codex/mounted-combat-phase3d-unified-combat
Phase 3D HEAD:   2f765c15658879236ead17edc40fed6738154ddf
Phase 3E branch: codex/mounted-combat-phase3e-paired-scheduler
Initial version: 0.1.0-phase3e-dev.1
```

Phase 3D production implementation: `9bf83d21b5367e057130b41bf7379512a638c025`.

Final gameplay repair: `e9aee929272ede4b94501e5167d384e3b0d868c3`.

Presentation checkpoint: `63d324d909ec333cd16cde5e942f419759faa664`.

Package-bound diagnostic checkpoint: `ee65550b38ce973db10ad1eb51af8e1f5b8f984c`.

Diagnostic version/package identity:

```text
Version:         0.1.0-phase3d-dev.21
Package:         C:\Dev\KingmakerMountedCombatLab\artifacts\KingmakerMountedCombat-0.1.0-phase3d-dev.21-unified-combat-alpha-diagnostic.zip
Package SHA-256: 067c9b49b8b7767e808a011315769fe996b642c068b9972f300160a1d8a61d26
Manifest SHA-256:5c1360a6368ffe385cee842b918149b46747180d03866960e77562dd63a9d605
DLL SHA-256:     198dfe3cf8b187b6803eb6218e2cca68b2cf71530e4cb6d546e1423fe5e9f6c9
DLL MVID:        fb0bbedb-f63a-417c-9cea-f6ee71d3ed18
```

Discover and reconcile the actual current repository state. Do not reset or overwrite inherited work merely to force these expected values. Preserve Phase 3D and all dev.21 evidence immutably. Do not continue new architecture work on the completed Phase 3D branch. Do not merge to `main`.

## User architecture decision

The user authorizes a purpose-built paired-command scheduler.

The required player-facing model remains:

- one mounted initiative entry;
- rider initiative bonus/result;
- rider portrait in the turn tracker;
- rider selection, action bar, camera, and UI principal;
- one coordinated mounted turn;
- separate rider and mount Standard, Move, five-foot-step, cooldown, weapon, command, animation, and rule-event ownership;
- no shared synthetic action pool;
- no duplicate turn;
- no extra action on Mount or Dismount.

The accepted Phase 3C separate-turn behavior must remain available through `EnableUnifiedMountedTurn=false`. Do not remove or corrupt that fallback.

The new scheduler must initially remain behind a separate experimental gate equivalent to `EnablePairedCommandScheduler=false`. Do not enable it by default until the complete Phase 3E qualification passes.

## Authoritative Phase 3D finding

The decisive dev.21 test established:

- rider remained the real native current-turn principal;
- Mammoth retained its independent Standard ledger;
- Mammoth retained natural-weapon and rule ownership;
- one in-range Mammoth `UnitAttack` entered the Mammoth Standard slot;
- shared admission passed;
- all ordinary readiness conditions passed;
- the command remained unstarted for 30 seconds;
- no attack, rule, damage, resource, movement, relationship, or presentation mutation occurred.

Exact Kingmaker advances a non-AoO turn-based command only when its executor is `CurrentTurn.Unit`.

Exact Wrath solves this with reciprocal rider/mount relationship parts, paired rider/mount command links, a `TurnController` that stores and advances both actors, pair-aware turn completion, and separate rider/mount action and command state. Kingmaker lacks those primitives.

Do not rerun dev.21 merely to rediscover this result.

## Mandatory intake

Before modifying production code:

1. Verify repository root, branch, origin, upstream, local HEAD, remote HEAD, and complete worktree.
2. Preserve all intentional inherited work.
3. Verify no merge, rebase, cherry-pick, revert, bisect, detached HEAD, or Git lock.
4. Verify no Kingmaker, Wrath, Unity Mod Manager, KMC launcher, build, transaction, runtime lock, sentinel, staging, or deployment ambiguity.
5. Read the newest entries in `AGENTS.md`, `AUTONOMOUS-RESUME.md`, `AUTONOMOUS-BLOCKERS.md`, `MOUNTED-COMBAT-JOURNAL.md`, `planning/WOTR-SHARED-TURN-MAP.md`, `planning/UNIFIED-MOUNTED-TURN-CONTRACT.md`, `planning/COMBAT-ACTION-ECONOMY-CONTRACT.md`, `planning/MOUNTED-STOCK-ATTACK-CONTRACT.md`, `planning/MOUNTED-RANGED-COMBAT-CONTRACT.md`, `planning/MOUNTED-FIVE-FOOT-STEP-CONTRACT.md`, `planning/COMBAT-MOUNT-DISMOUNT-CONTRACT.md`, `planning/PHASE3D-RUNTIME-SCENARIO-MATRIX.md`, `docs/PHASE3D-UNIFIED-MOUNTED-COMBAT-IMPLEMENTATION.md`, and `docs/PHASE3D-PLAYTEST.md`.
6. Inspect the exact immutable dev.21 result only after confirming no external-state ambiguity.
7. Preserve the Phase 3D RT and presentation successes. Do not restart those implementations from scratch.

## Tranche 1 — Exact Kingmaker command-lifecycle map

Create:

```text
planning/KINGMAKER-TB-COMMAND-LIFECYCLE-MAP.md
planning/PAIRED-COMMAND-SCHEDULER-CONTRACT.md
planning/PHASE3E-RISK-AND-KILL-CRITERIA.md
planning/PHASE3E-RUNTIME-SCENARIO-MATRIX.md
```

Map exact installed Kingmaker contracts for `CombatController`, `TurnController`, `UnitActionController`, `CurrentTurn` and `CurrentTurn.Unit`, `UnitCommands`, `UnitCommand`, `UnitAttack`, `UnitAttackOfOpportunity`, `UnitMoveTo`, `UnitUseAbility`, command slots, command queues, command creation, command admission, command start, command tick/update, animation/action execution, command finish, command interrupt, result assignment, slot removal, resource/cooldown charging, pause and game-mode gates, turn completion, end-turn input, new-round preparation, initiative advancement, and exception cleanup.

Record exact type, method, metadata token, caller, callee, executor identity check, current-turn check, frame/update ordering, side effects, event dispatch, command-container effects, action-resource effects, and AoO special treatment.

Answer before implementation:

> Does stock `UnitActionController` already encounter the exact mount command and reject it only because `executor != CurrentTurn.Unit`, or does it never enumerate/visit the mount command at all?

That answer controls the implementation.

### Preferred implementation hierarchy

Investigate in this order.

#### Option A — exact native eligibility extension

If stock Kingmaker already visits the exact mount command but rejects only on the current-unit equality check:

- patch only that exact predicate;
- return eligible only for one reference-exact scheduler-leased KMC mount command;
- require the active rider to be `CurrentTurn.Unit`;
- require the command executor to be the active paired mount;
- require the command to have been explicitly registered by KMC;
- require one active mounted pair;
- preserve every other native predicate and side effect.

This is preferred if exact control flow supports it.

#### Option B — invoke an exact native per-unit advance seam

If Kingmaker exposes a lower-level function that advances commands for a supplied unit:

- invoke that exact stock function once for the active mount after or alongside the rider's native advancement;
- do not duplicate rider advancement;
- do not advance arbitrary mount commands;
- guard the call with an exact KMC command lease;
- preserve native command lifecycle and result handling.

#### Option C — pair-local explicit scheduler

If no safe native per-unit seam exists, implement an original `MountedPairCommandScheduler` that advances only one exact KMC-leased mount command through the installed command lifecycle. It must not emulate more of `TurnController` than necessary.

#### Option D — rider-owned scheduling shell

This is authorized only if A-C are disproven by exact evidence.

A rider-owned scheduling shell may exist solely because Kingmaker advances `CurrentTurn.Unit` commands. The shell must:

- consume no rider Standard or Move merely for scheduling;
- emit no rider attack or rider weapon rule;
- never claim the rider performed the mount action;
- retain the mount as attack initiator;
- retain the mount as weapon owner;
- retain the mount as target/action actor;
- retain the mount animation;
- debit only the mount's appropriate resource;
- host or advance exactly one mount-owned child action;
- terminate when that child terminates;
- preserve cancellation and failure honestly.

This revises only top-level scheduling ownership, not gameplay-action ownership. Document the distinction precisely.

### Prohibited architecture shortcuts

Do not:

- permanently or globally change `CurrentTurn.Unit` to the mount;
- repeatedly swap `CurrentTurn.Unit` every frame without exact proof and a `try/finally` lease;
- call `StartTurn` on the mount;
- emit another turn event;
- reset the mount's resources more than once;
- advance all commands belonging to the mount;
- advance arbitrary AI commands;
- advance another companion's commands;
- advance AoO commands already handled by stock behavior;
- make the rider the apparent Bite/Hoof attacker;
- debit mount actions from rider cooldowns;
- merge rider and mount action pools;
- replace the complete global Kingmaker turn controller;
- patch all units' `CanActInCombat`;
- bypass native attack, animation, target, line-of-sight, or weapon rules;
- serialize scheduler state into campaign saves.

A temporary scoped `CurrentTurn` substitution may be considered only if exact control-flow inspection proves it is a passive equality input within one bounded native call and cannot emit turn/UI/new-round effects. It must use `try/finally`, exact reference restoration, reentrancy protection, and exhaustive controls. It is not the preferred design.

## Tranche 2 — Scheduler lease and state machine

Implement scheduler state outside Harmony patch fields.

Suggested states:

- `Idle`
- `Registered`
- `AwaitingStart`
- `Running`
- `Finishing`
- `Interrupting`
- `Completed`
- `Faulted`
- `Disposed`

Every scheduled command lease must bind:

- rider reference and stable ID;
- mount reference and stable ID;
- mounted relationship generation;
- current rider turn identity;
- command reference;
- executor;
- slot;
- command type;
- action origin;
- target;
- weapon or ability;
- creation frame;
- admission frame;
- last-driven frame;
- started/running/finished/result state;
- expected resource owner;
- expected rule initiator;
- cleanup reason.

Required invariants:

- at most one active pair;
- at most one scheduler lease per exact command;
- at most one scheduler drive per Unity frame;
- command executor is exact paired mount;
- `CurrentTurn.Unit` remains exact rider;
- command is reference-identical to the object in the expected mount slot or queue;
- command was explicitly created/admitted by KMC;
- no foreign or AI command can be adopted;
- no stale command can be driven after slot replacement;
- no command can start twice;
- no command can finish twice;
- no resource can be charged twice;
- cleanup is idempotent;
- relationship invalidation interrupts and disposes the lease;
- turn change disposes or safely interrupts the lease;
- RT/TB mode change disposes safely;
- save/load/area transition leaves no scheduler state;
- no scheduler state is serialized.

Exclude `UnitAttackOfOpportunity` and every other free/out-of-turn command until exact evidence proves scheduling is required and nonduplicative.

## Tranche 3 — Minimal vertical slice

Do not begin broad ranged, five-foot-step, or combat-Mount work until this gate passes.

Implement one exact vertical slice:

- active mounted Mammoth or Horse pair;
- rider is `CurrentTurn.Unit`;
- rider portrait/selection/UI remains principal;
- mount has an unused Standard action;
- hostile target is already in legal mount-primary range;
- KMC creates one mount-owned `UnitAttack` in the mount Standard slot;
- scheduler advances it;
- attack starts;
- one animation/action executes;
- one attack roll occurs;
- at most one damage event occurs;
- command terminates;
- mount Standard is consumed exactly once;
- rider Standard is unchanged;
- rider remains `CurrentTurn.Unit` throughout;
- no native mount turn is emitted;
- no duplicate command or action exists;
- cleanup is exact.

Initial acceptance targets:

- command begins within two actionable game frames after admission, unless exact stock animation staging requires a documented larger bound;
- scheduler drives at most once per frame;
- exactly one command start;
- exactly one terminal result;
- exactly one mount resource charge;
- zero rider action cost;
- zero duplicate attack/roll/damage chains;
- zero turn-order mutation;
- zero non-pair effects.

Run the vertical slice twice in fresh processes from the same immutable package and suite. Do not broaden the architecture until both passes succeed.

## Tranche 4 — Turn completion and separate ledgers

After the vertical slice passes, qualify:

1. mount action first, rider action second;
2. rider action first, mount action second;
3. mount action only;
4. rider action only;
5. one actor has already spent Standard;
6. both actors have spent Standard;
7. target dies after the first action;
8. one action is cancelled;
9. one action is interrupted;
10. player ends the turn.

Requirements:

- rider remains native turn principal;
- mount action remains mount-owned;
- rider action remains rider-owned;
- turn does not advance while an exact pair command is running;
- turn does not remain stuck after both actors are finished or ineligible;
- mount ledger initializes once on a natural rider-turn start;
- rider ledger initializes once through stock behavior;
- no mid-turn reset;
- no action refresh;
- no extra turn;
- next-round initialization occurs once per actor;
- unrelated combatants retain exact order.

Reuse and narrow the existing pair-aware `IsAllActed`/`ContinueActing` work when sound. Do not create a second synthetic turn clock.

## Tranche 5 — Ordinary hostile-click TB sequencing

Preserve the already qualified Phase 3D RT behavior. Use the scheduler to complete the TB contract.

For rider melee:

- one ordinary hostile click establishes pair intent;
- mount approaches through mount-owned movement when needed;
- rider attacks with rider Standard;
- mount attacks with mount Standard when legally available;
- deterministic default order is rider first, mount second unless exact evidence supports a safer order;
- target death after rider attack cancels mount attack;
- explicit Rider Primary spends only rider action;
- explicit Mount Primary spends only mount action.

For rider ranged:

- mount moves only until rider-native range and LoS admission;
- rider fires through native weapon/ammunition/reload rules;
- mount does not close to melee automatically;
- Mount Primary remains separately available when mount has a legal target;
- movement consumes mount movement resources;
- rider attack consumes rider attack resources;
- mount attack consumes mount attack resources.

Require exact command, weapon, target, rule-event, animation, resource, and cancellation telemetry.

## Tranche 6 — Mount movement commands

Phase 3D already has rider-turn delegated movement. Do not rewrite it without evidence.

Determine whether the paired scheduler must also drive mount approach for melee, mount approach-to-range for ranged, mount five-foot-step, door approach, and combat Mount positioning.

If the existing `ExactTurnMovementAdapter` already advances movement safely, reuse it and do not duplicate movement scheduling.

If movement also requires the paired scheduler:

- lease exact KMC-created `UnitMoveTo` only;
- preserve mount as physical mover;
- charge the correct mount or rider movement ledger according to the documented contract;
- never drive arbitrary AI movement.

## Tranche 7 — Mount/Dismount during combat

After scheduler and turn completion pass:

- Mount during combat costs rider one Move action;
- rider and mount must be adjacent and eligible;
- preserve all resources already spent by both actors;
- suppress the redundant mount turn without granting another action;
- do not reset either ledger;
- do not skip an unrelated unit;
- Dismount costs rider one Move action;
- do not create an immediate second rider or mount turn;
- restore separate-turn participation at the next safe round boundary;
- preserve exact current-round costs.

Run cases where neither has acted, rider has acted, mount has acted, both have partially moved, mount had an upcoming native initiative slot, mount had already passed its native slot, and rider or mount becomes invalid during the transition.

## Tranche 8 — Five-foot step and AoO

Complete the previously deferred six-row contract only after scheduler stability.

Mounted five-foot step must:

- use the exact native five-foot-step mode;
- physically move the mount;
- keep the rider attached;
- not provoke solely due to that step;
- consume the correct five-foot-step opportunity;
- remain limited to legal distance;
- prevent illegal use after ordinary movement;
- avoid global AoO suppression.

Controls:

- mounted ordinary movement provokes when stock rules require;
- mounted five-foot step does not;
- unmounted five-foot step remains stock;
- another party member's movement/AoO remains stock;
- hostile movement/AoO remains stock.

Do not alter broad engagement or opportunity logic.

## Tranche 9 — Fallback and runtime failure safety

Keep `EnableUnifiedMountedTurn=false` as the accepted separate-turn fallback. Add a separately controlled scheduler gate.

If a scheduler invariant fails at runtime:

- do not silently grant or lose an action;
- interrupt only scheduler-owned commands;
- restore exact command slots and leases;
- preserve rider/mount resources as far as exact native state permits;
- cleanly dismount or fall back at a safe turn boundary;
- log the exact invariant failure;
- leave unrelated combatants untouched.

Do not automatically flip persistent user settings after a transient error.

The final manual-review package may enable the paired scheduler by default only after complete qualification. The fallback setting must remain available.

## Bounded architecture budget

This mission authorizes:

- one exact command-lifecycle observation checkpoint;
- one primary paired-scheduler implementation;
- at most two narrowly attributable scheduler repair cycles;
- one controlled rider-owned scheduling-shell fallback investigation if the primary scheduler reaches a documented kill criterion.

Do not create twenty sequential packages that each add one observation field.

A repair cycle may address incorrect native method ordering, one command-start predicate, one terminal/slot-restoration defect, or one exact turn-completion defect. A fundamentally new global controller architecture requires another user decision.

### Scheduler kill criteria

Trigger a pivot if:

1. the command can advance only by permanently changing `CurrentTurn.Unit`;
2. temporary `CurrentTurn` substitution emits duplicate turn, UI, initiative, or new-round effects that cannot be isolated;
3. exact mount commands are double-ticked;
4. arbitrary AI or non-KMC commands must be advanced;
5. native command lifecycle cannot be completed without bypassing rule/weapon/animation ownership;
6. mount resources cannot remain exact;
7. rider resources are consumed or refreshed incorrectly;
8. unrelated units' command processing changes;
9. turn completion skips, duplicates, or deadlocks combatants;
10. repeated fresh-process tests remain nondeterministic;
11. cleanup leaves command/slot/turn residue;
12. the only solution is a broad global `TurnController` replacement.

### Authorized fallback ownership revision

If the primary scheduler reaches a kill criterion, the rider-owned scheduling shell is authorized with these nonnegotiable semantics:

- the shell is only a scheduler;
- it consumes no rider combat action;
- it emits no rider attack rule;
- the mount remains the child action initiator;
- mount weapon, animation, target, damage, and resource ownership remain exact;
- shell and child terminate together;
- one child only;
- no duplicate;
- exact cancellation;
- exact cleanup.

If even this cannot satisfy the contract safely, preserve the accepted separate-turn fallback, package the qualified Phase 3D RT improvements with unified mode disabled, and stop at the fallback disposition rather than continuing an unbounded architecture loop.

## Required tests

### Scheduler unit/component tests

- exact leased command accepted;
- foreign command rejected;
- AI command rejected;
- AoO command excluded;
- wrong rider rejected;
- wrong mount rejected;
- stale relationship generation rejected;
- stale turn rejected;
- slot replacement rejected;
- start exactly once;
- tick at most once per frame;
- finish exactly once;
- interrupt exactly once;
- cleanup idempotent;
- exception cleanup;
- no serialized state;
- fallback disabled is inert.

### Vertical-slice runtime

- `shared-rider-turn-mount-primary-passA`;
- `shared-rider-turn-mount-primary-passB`;
- `rider-remains-current`;
- `mount-standard-only`;
- `one-chain-cardinality`;
- `no-native-mount-turn`;
- `exact-turn-completion`.

### Sequencing runtime

- mount-first-rider-second;
- rider-first-mount-second;
- target-dies-after-first;
- explicit-rider-only;
- explicit-mount-only;
- one-ledger-already-spent;
- cancellation;
- interruption;
- end-turn;
- next-round-reset-once.

### Movement and ranged

- TB melee approach;
- TB ranged approach-to-range;
- TB ranged no-forced-melee;
- TB Shortbow;
- TB Crossbow/reload;
- TB Sling;
- LoS recovery;
- target movement bounded repath;
- movement cancellation.

### Combat lifecycle

- combat Mount before either acted;
- combat Mount after rider acted;
- combat Mount after mount acted;
- combat Dismount;
- no extra turn;
- RT-to-TB;
- TB-to-RT;
- rider unconsciousness;
- mount unconsciousness;
- target invalidation;
- area/save cleanup.

### Five-foot step

- mounted five-foot-step no AoO;
- mounted ordinary move AoO control;
- distance bound;
- one-step limit;
- post-movement rejection;
- unmounted control.

### Isolation

- separate-turn fallback;
- non-mounted Horse;
- non-mounted Mammoth;
- unrelated companion;
- unrelated combatant initiative;
- unmounted melee;
- unmounted ranged;
- no foreign-mod dependency.

## Qualification order

Do not run the entire Phase 3D suite after every observation. Use this order:

1. exact contract map;
2. component tests;
3. minimal mount-primary vertical slice;
4. two fresh-process vertical-slice passes;
5. turn completion and sequencing;
6. ordinary TB melee;
7. TB ranged;
8. combat Mount/Dismount;
9. five-foot step;
10. RT regression only for production seams actually changed;
11. targeted Horse and Mammoth regressions;
12. final package and manual review.

After every live process, audit complete restoration before reading gameplay evidence, preserve failed evidence immutably, use fresh run IDs, and do not relabel historical failures.

## Gates

For production checkpoints:

- source validation;
- clean Release build;
- component tests;
- visual/source-order tests;
- harness/protocol tests;
- assembly-contract tests;
- PowerShell parser;
- JSON parser;
- diff validation;
- prohibited-payload validation;
- coherent commit;
- guarded feature-branch publication;
- fresh package;
- stable suite;
- WhatIf;
- independent audit;
- applicable runtime evidence.

Documentation-only edits do not require gameplay requalification. Observation-only edits require targeted evidence, not a complete suite restart.

## Git publication

For the non-main branch, use only the authorized generic helper outside the Windows sandbox:

```powershell
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File C:\Dev\CodexPolicy\Push-KingmakerFeatureBranch.ps1 -RepositoryRoot C:\Dev\KingmakerMountedCombatLab\repo\KingmakerMountedCombat -Confirm:$false
```

Do not first retry inside the credential-isolated sandbox. Do not use raw `git push`. Do not merge to `main` or `master`.

## Deliverables

Create or update:

```text
planning/PHASE3E-PAIRED-COMMAND-SCHEDULER-MISSION.md
planning/KINGMAKER-TB-COMMAND-LIFECYCLE-MAP.md
planning/PAIRED-COMMAND-SCHEDULER-CONTRACT.md
planning/PHASE3E-RISK-AND-KILL-CRITERIA.md
planning/PHASE3E-RUNTIME-SCENARIO-MATRIX.md
planning/WOTR-SHARED-TURN-MAP.md
planning/COMBAT-ACTION-ECONOMY-CONTRACT.md
planning/MOUNTED-STOCK-ATTACK-CONTRACT.md
planning/MOUNTED-RANGED-COMBAT-CONTRACT.md
planning/MOUNTED-FIVE-FOOT-STEP-CONTRACT.md
planning/COMBAT-MOUNT-DISMOUNT-CONTRACT.md
docs/PHASE3E-PAIRED-SCHEDULER-IMPLEMENTATION.md
docs/PHASE3E-PLAYTEST.md
docs/PALADIN-DIVINE-STEED-DESIGN.md
AUTONOMOUS-RESUME.md
AUTONOMOUS-BLOCKERS.md
MOUNTED-COMBAT-JOURNAL.md
```

Paladin design may be updated. Paladin implementation remains unauthorized.

## Completion states

### Successful scheduler

Stop at:

```text
PAIRED COMMAND SCHEDULER ALPHA — MANUAL REVIEW REQUIRED
```

Provide branch and exact local/upstream/remote SHA, implementation commit, version, package path and hashes, DLL MVID, exact native command seam used, scheduler state and lease model, proof `CurrentTurn.Unit` remained rider, proof mount command/resource/rule ownership remained exact, vertical-slice A/B totals, sequencing totals, TB melee/ranged totals, combat Mount/Dismount totals, five-foot-step/AoO totals, RT regression, Horse regression, Mammoth regression, fallback result, restoration result, known limitations, install/uninstall commands, and a focused manual checklist.

### Bounded fallback disposition

If the authorized scheduler and scheduling-shell paths reach kill criteria, stop at:

```text
PAIRED SCHEDULER PIVOT — SEPARATE-TURN FALLBACK READY
```

Produce an installable, truthful fallback package with `EnableUnifiedMountedTurn=false` by default, accepted Phase 3C separate turns, qualified Phase 3D RT hostile-click melee, qualified Phase 3D RT ranged behavior, accepted native abilities and presentation, no claim of unified TB behavior, exact manual checklist, and exact future architecture blocker.

Do not leave only a diagnostic package when a safe, already accepted fallback can be packaged.

## Still unauthorized

Do not implement Paladin Divine Steed, a merged rider/mount action pool, broad mounted spellcasting rules, mounted feats, mounted charge, broad mounted AoO changes beyond exact five-foot-step behavior, persistent mounted state, automatic remount, additional mount species, Small riders, enemy riders, a public release, or a merge to `main`.

Continue autonomously through ordinary compile, test, package, and restored runtime failures within the bounded architecture budget.
