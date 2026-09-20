# CODEX AUTONOMOUS MASTER MISSION

## Kingmaker Mounted Combat - Phase 2: Presentation and Private Combat Alpha

## Superseding execution status — 2026-08-24

Automated Phase 2 Round 2 stabilization is `PASS`; focused final human regression is `IN PROGRESS`. The technical implementation closes at `1241222459209aea1e6127bedd7d630df3940b99` with credited RT/TB/door assertions `443/0`, complete final offline gates, and exact external restoration. The final package-bound documentation identity is recorded in `docs/PHASE-2-CLOSURE-AND-HORSE-HANDOFF.md` and its external immutable identity record.

Current stop disposition is `PRIVATE ALPHA STABILIZATION ROUND 2 COMPLETE  MANUAL REGRESSION REQUIRED`. This mission authorizes no further implementation before exact-package human acceptance. The proposed horse work in `docs/PHASE-3-HORSE-MISSION-DRAFT.md` is not execution authority and requires a separate user authorization; the historical mission text below remains evidence of the original scope.

You are the primary implementation, investigation, and qualification agent for the independent `KingmakerMountedCombat` mod.

Work continuously and autonomously through every authorized tranche below. Do not ask the user ordinary implementation questions. Inspect the exact repository, current Phase 1 records, exact installed Kingmaker assemblies, existing runtime harness, native assets and animation metadata, and actual game behavior. Make conservative engineering decisions, preserve evidence, test each contract, commit coherent checkpoints, and continue.

This master mission authorizes Phase 2 through a private melee-combat alpha for one Medium humanoid rider and one Mammoth using the already selected Architecture B. It does not authorize a public release, a merge to `main`, enemy riders, multiple mount species, persistent mounted state, ranged mounted combat, spellcasting while mounted, or mounted-feat production.

There is one mandatory human checkpoint: visual acceptance after the presentation tranche. Complete every safe presentation, UI, persistence-policy, harness, and package task before stopping. After the user explicitly accepts the exact review build, resume this same mission automatically at the combat tranche and continue through the private alpha. Do not ask for approval between internal combat tranches.

Copy this entire mission into:

```text
planning/MOUNTED-COMBAT-PHASE-2-MASTER-MISSION.md
```

before substantive implementation.

---

# 1. Authorized identity and starting point

Exact identity:

```text
Product:             Kingmaker Mounted Combat
Repository:          KingmakerMountedCombat
Assembly:            KingmakerMountedCombat.dll
UMM ID:              KingmakerMountedCombat
Root namespace:      KingmakerMountedCombat
Framework:           .NET Framework 4.7
Language:            C# 7.3
Game target:         Pathfinder: Kingmaker 2.1.7b
Lab root:            C:\Dev\KingmakerMountedCombatLab
Repository root:     C:\Dev\KingmakerMountedCombatLab\repo\KingmakerMountedCombat
Phase 2 branch:      codex/mounted-combat-phase2-alpha
Starting main HEAD:  72dcbeb19d03985509f1ed71d3550dfb74f0ac15
Phase 1 evidence:    d5bd7fa9c434f04c6f8487b61ea49e3cf983c397
```

Frozen qualified Phase 1 artifact:

```text
C:\Dev\KingmakerMountedCombatLab\artifacts\KingmakerMountedCombat-0.0.1-feasibility-diagnostic.zip
SHA-256: 5ce3bd7d98a090ee05405cc4b4725fa58f13f1926958a69905ba478374c75a4d
Manifest SHA-256: 0494f1892b58f00b4e9fa36a0e33a0b16a3030840e6232d0f52503d49e8fa90b
DLL SHA-256: 202701ed9232e4f5a4d5ff65c468c684d1ed1dd53e4a8030be93b960a7cd202a
DLL MVID: a702808c-e8a0-4755-bc24-5ed4e945866a
```

Phase 1 result is authoritative historical evidence:

```text
25 PASS / 0 attributable FAIL / 0 DEFER
Architecture B selected
No K1-K12 criterion fired
Presentation: MECHANICALLY VIABLE, NEW ANIMATION/POSE WORK REQUIRED
```

Do not overwrite, rebuild in place, relabel, or reinterpret the frozen Phase 1 artifact. Phase 2 artifacts must have new names, versions, hashes, manifests, and evidence bindings.

The repository `AGENTS.md` still contains Phase 1 branch and scope language. This user mission supersedes those stale Phase 1-only lines. Before substantive implementation, update `AGENTS.md` conservatively so that it:

- records Phase 1 as completed historical evidence;
- authorizes only the Phase 2 branch and scope in this mission;
- preserves the architecture, safety, separation, testing, licensing, and non-publication rules;
- forbids silent expansion beyond this mission.

Commit the governance update before production changes.

---

# 2. Product objective

Deliver the smallest credible private alpha in which one Medium humanoid with an active larger Mammoth companion can:

1. mount and dismount through a player-facing action;
2. move under mount-authoritative Architecture B without regressing Phase 1;
3. display a credible mounted pose at ordinary isometric camera distances;
4. retain coherent selection, portrait, action bar, selection circle, camera follow, and party control;
5. remain save-safe and uninstall-safe through a transient, nonserialized mounted relationship;
6. make one basic rider melee attack while mounted;
7. make one explicit mount natural attack while mounted;
8. obey a documented, nonduplicating action-economy model in real-time and turn-based combat;
9. cleanly handle combat boundaries, death, incapacitation, movement cancellation, area/load/save boundaries, mod disable, and failure recovery;
10. produce a private diagnostic/playtest package and complete evidence.

After the core private alpha passes, this mission also authorizes bounded stretch investigation and implementation of:

- mounted melee reach;
- attacks of opportunity;
- a basic mounted charge.

Those stretch features may proceed automatically only after the core alpha is independently green. A stretch failure must be contained, disabled by default, and documented; it must not invalidate an otherwise qualified core alpha unless it proves Architecture B itself unsafe.

---

# 3. Explicit non-goals

Do not implement, ship, or claim in this mission:

```text
public release
Nexus publication
merge to main
persistent serialized mounted relationship
automatic remount after load/area transition
multiple mount species
Small riders
enemy or AI riders
Cavalier class/archetypes
mounted feats
ranged attacks while mounted
spellcasting while mounted
full Wrath parity
campaign-wide indoor support
custom quest content
Wrath code or asset redistribution
Kingmaker extracted mesh/skeleton redistribution
```

For the private alpha, ranged attacks and spellcasting while mounted must be explicitly rejected or disabled with a clear reason unless a later separately authorized mission proves them. Do not leave ambiguous partial behavior.

---

# 4. Autonomy and stopping policy

Continue automatically through ordinary:

```text
compile failures
test failures
bounded decompilation and signature discovery
failed hooks
failed pose strategies
failed native animation candidates
failed target-spawn candidates
runtime scenario failures when restoration succeeds
telemetry/schema improvements
UI integration defects
turn-based/real-time discrepancies
one rejected optional stretch feature
context compaction
subagent limits
```

Do not ask the user to choose offsets, bone rotations, blueprint IDs, action ownership, thresholds, test enemies, or ordinary architecture details that evidence can resolve.

Mandatory stop 1:

```text
BLOCKED - MANUAL VISUAL ACCEPTANCE REQUIRED
```

Use it only after the complete presentation/UI/persistence tranche is technically qualified, committed, published to the Phase 2 branch, packaged, and accompanied by a guarded manual-review launcher and exact review instructions. Do not begin combat production before explicit visual acceptance.

After the user accepts the exact review build, continue automatically through all combat tranches and private-alpha hardening. Do not request approval between rider attack, mount attack, action economy, lifecycle, reach, attack-of-opportunity, charge, packaging, or documentation gates.

Other legitimate hard stops are limited to:

- protected-save or live Mods state cannot be restored exactly;
- repository/branch/origin identity is ambiguous;
- continuing requires modifying another project;
- safe progress requires proprietary asset redistribution or unlicensed copying;
- every credible pose path is visually or technically unusable;
- core combat causes irreducible duplicate actions, global non-mounted changes, or corrupt state;
- an external credential/account/update/cloud prompt blocks safe runtime work;
- a user-owned fixture or manual visual judgment is genuinely required and cannot be replaced by objective evidence;
- tool or service exhaustion makes further progress impossible.

Before every stop: commit safe work, publish only through the guarded non-force helper when permitted, update journal/resume/blockers, restore all external state, and state the exact next action.

---

# 5. Durable Phase 2 records

Create and maintain:

```text
planning/MOUNTED-COMBAT-PHASE-2-MASTER-MISSION.md
planning/PHASE-2-CONTRACT-MATRIX.md
planning/PHASE-2-RISK-AND-KILL-CRITERIA.md
planning/PHASE-2-RUNTIME-SCENARIO-MATRIX.md
planning/PRESENTATION-POSE-STRATEGY.md
planning/PERSISTENCE-UNINSTALL-POLICY.md
planning/PLAYER-ACTION-UI-CONTRACT.md
planning/COMBAT-ACTION-ECONOMY-CONTRACT.md
planning/TARGETING-REACH-CHARGE-CONTRACT.md
planning/DIAGNOSTIC-TARGET-POLICY.md
docs/PHASE-2A-PRESENTATION-REPORT.md
docs/PHASE-2A-MANUAL-REVIEW.md
docs/PHASE-2-COMBAT-IMPLEMENTATION-REPORT.md
docs/PHASE-2-QUALIFICATION.md
docs/PRIVATE-ALPHA-PLAYTEST.md
docs/PHASE-3-EXPANSION-MISSION-DRAFT.md
```

Continue using:

```text
MOUNTED-COMBAT-JOURNAL.md
AUTONOMOUS-RESUME.md
AUTONOMOUS-BLOCKERS.md
THIRD-PARTY-NOTICES.md
```

Every meaningful checkpoint records:

```text
UTC date/time
branch and exact HEAD
active version
work completed
exact commands/tests
PASS/FAIL counts
runtime evidence IDs and paths
package/DLL hashes and MVIDs
fixture/protected-save authority
external-state restoration
rejected theories
current uncertainty
next exact action
```

Do not rewrite Phase 1 history. Add Phase 2 records and cite Phase 1 evidence by exact commit/hash.

---

# 6. Mandatory intake and governance gate

Before production changes:

1. Confirm current directory is the standalone repository.
2. Confirm branch is `codex/mounted-combat-phase2-alpha` and it descends exactly from main `72dcbeb19d03985509f1ed71d3550dfb74f0ac15`.
3. Confirm origin is exactly `https://github.com/howardreith/KingmakerMountedCombat.git`.
4. Confirm clean worktree, no merge/rebase/lock ambiguity, and no other agent mutating the worktree.
5. Read `AGENTS.md`, Phase 1 qualification, feasibility report, Phase 2 recommendation, mission draft, resume, blockers, architecture options, kill criteria, and runtime scenario matrix.
6. Read `C:\Dev\KingmakerMountedCombatLab\phase2-preflight.json` and the Phase 1 archive manifest produced by the preparation script.
7. Verify the frozen Phase 1 artifact and hashes without modifying it.
8. Verify exact Kingmaker, UMM, Harmony12, Unity, and Wrath-reference identities remain unchanged or record any environmental difference before proceeding.
9. Verify exactly one canonical Baseline and Working, zero KMC near-matches, and the existing current protected-save authority.
10. Verify no game process, runtime lock, sentinel, active transaction, or live KMC deployment exists.
11. Run the existing full source, build, component, visual, harness, assembly-backed, package, and WhatIf gates from clean main-derived Phase 2 state.
12. Update `AGENTS.md` and create the durable Phase 2 files.
13. Commit and publish the intake/governance checkpoint through the existing guarded helper.

Do not autonomously rebaseline protected non-KMC saves. A changed non-KMC save is a hard continuity stop unless explicitly authorized by the user. Only `KMC_AUTOMATION_WORKING` may be mutable inside the guarded transaction contract. `KMC_AUTOMATION_BASELINE` remains immutable.

---

# 7. Architecture B invariants

Preserve the proven architecture:

```text
mount owns stock pathfinding and physical movement
rider remains the principal player-facing logical unit
rider movement agent does not compete with the mount
rider entity/view synchronization is bounded and measured
selection normalizes to the rider
formation does not create duplicate pair slots
cleanup is idempotent and residue-free
non-mounted units remain unaffected
```

Do not replace the proven movement architecture merely to make animation or combat easier.

Production concerns remain separated:

```text
relationship domain
Kingmaker adapters
command routing
movement synchronization
view attachment
pose/animation
player action/UI
combat targeting
action economy
lifecycle cleanup
runtime diagnostics
harness and external-state transaction
```

Harmony patches, UMM callbacks, UI handlers, animation callbacks, and runtime scenario dispatchers must delegate to services. Do not accumulate business logic in `Main`, a patch class, a `MonoBehaviour`, or a diagnostic window.

---

# 8. Tranche A - Persistence, uninstall, native lifecycle, and player action

Complete this tranche before pose work.

## 8.1 Persistence policy

The private alpha mounted relationship is transient and must not be intentionally serialized.

Required policy:

```text
save begins only after clean dismount/relationship cleanup
load begins unmounted
area transition begins only after clean dismount
mod disable removes all runtime state
uninstall leaves no required custom mounted relationship in saves
no automatic remount
no orphaned rider/mount references
```

Do not add a persistent fact, buff, activatable ability state, custom unit part, or hotbar reference to a campaign save until its uninstall/orphan behavior is mapped and tested. Prefer a runtime-injected, removable player action during the private alpha.

If a blueprint-backed player action is necessary, prove:

- exact registration lifecycle;
- whether facts or hotbar slots serialize;
- cleanup before save;
- load without the mod or with the feature disabled;
- no orphan exception or broken save;
- a repair path for stale references.

If that cannot be proven, use a project-owned UMM action/hotkey or another transient UI surface for the private alpha and document the compromise. Do not trade save safety for prettier UI.

## 8.2 Native lifecycle delivery

Phase 1 retained claim limits for some directly invoked handlers. Phase 2 must investigate and, where safely possible, qualify actual native delivery for:

```text
save request
load start/end
area begin unload / new area attach
turn-based mode state change
real-time mode state change
combat start/end
view attach/detach
party removal
unit death/incapacitation
mod disable
```

Direct service tests remain valuable, but do not silently promote them to native event proof.

## 8.3 Player-facing Mount/Dismount action

Deliver a single clear player action with exact eligibility feedback.

Eligibility must include at least:

```text
Medium supported rider body type
active owned Mammoth companion
mount currently larger than rider
both alive, conscious, controllable, and in valid area state
no conflicting mounted relationship
no unsupported polymorph/size state
no transition, cutscene, or blocked lifecycle boundary
```

The action must:

- explain why mounting is unavailable;
- never act on an inferred or nearby non-owned creature;
- normalize selection to the rider;
- become Dismount while mounted or expose an equally clear paired action;
- fail closed and rollback on partial failure;
- leave no saved relationship.

Component and runtime tests must cover action visibility, availability, invalid reasons, mount, dismount, double activation, lifecycle interruption, and restoration.

---

# 9. Tranche B - Pose, animation, presentation, UI, and camera

Phase 1 proved mechanical attachment but not acceptable presentation. This tranche must solve or truthfully reject presentation before combat production.

## 9.1 Investigation order

Use this order:

1. inspect exact Kingmaker-native humanoid animation sets, body rigs, seated/riding/crouched poses, layered animation facilities, IK helpers, and post-animator hooks;
2. inspect exact Mammoth animation and anchor behavior already identified in Phase 1;
3. evaluate legally usable native references without extracting or redistributing game assets;
4. if no suitable native pose exists, implement an original code-driven procedural pose layer;
5. consider original project-authored animation/pose data only when it can be created and shipped without copied Kingmaker/Wrath meshes, skeletons, clips, controllers, or other proprietary payloads.

Wrath remains read-only evidence. Do not import or ship Wrath code or assets.

## 9.2 Preferred procedural fallback

A procedural fallback should, when technically appropriate:

- apply after ordinary animation evaluation through a narrow view-owned adapter;
- retain upper-body animation freedom for later attacks;
- control pelvis, thighs, knees, lower legs, and feet through a typed mount/body pose profile;
- avoid cumulative transform mutation by deriving every frame from a captured/restorable baseline;
- support deterministic position, rotation, and scale values;
- restore every affected transform and animator/IK state on dismount and all cleanup boundaries;
- avoid a global animator patch affecting non-mounted units;
- measure jitter, residual, restoration, and frame cost.

A development-only calibration UI may expose anchor, pelvis, leg, foot, rotation, and scale adjustments. Final accepted profiles must be deterministic data, validated, and usable with the calibration UI closed.

## 9.3 Presentation requirements

Qualify at minimum:

```text
idle
walk
run
substantial turn
repeated direction reversal
stop/start
stationary wait
mount transition
dismount transition
doorway traversal
party formation
selection away/back
ordinary one-handed weapon
shield when available
ordinary two-handed weapon when safely available
```

Acceptance targets:

- no cumulative drift or visible oscillation;
- no gross pelvis/lower-body burial at ordinary isometric camera angles;
- no feet visibly hanging beneath or far outside the Mammoth body at ordinary angles;
- no persistent weapon/shield displacement after dismount;
- no animation or bone residue after cleanup;
- upper body remains viable for future attack animation;
- no material non-mounted animation impact;
- frame-time overhead is measured and bounded.

Do not optimize only for one screenshot or camera angle.

## 9.4 UI and camera observation

Directly observe and record:

```text
portrait highlight
selected unit identity
click-selection on rider and mount
selection circle ownership/placement
action bar ownership
Mount/Dismount action state
camera follow while moving
camera follow after selection switches
party group movement
cursor/ground command behavior
```

Camera-only world screenshots do not prove UI state. Capture the actual UI when needed.

## 9.5 Presentation runtime matrix

Create and run exact or equivalent scenarios twice in fresh processes:

```text
player-action-availability
mount-dismount-user-flow
pose-idle
pose-walk-run
pose-turn-stop
pose-doorway-formation
pose-equipment-variants
ui-selection-portrait-actionbar
camera-follow-and-command-routing
native-save-clean-dismount
native-area-clean-dismount
native-mode-transition-cleanup
presentation-residue-and-uninstall-safety
```

Every run must bind exact branch/commit/version/package/DLL hash/MVID, exact fixture authority, telemetry, screenshots, and restoration results.

---

# 10. Mandatory manual visual checkpoint

After Tranches A and B are technically green:

1. Create a private review package with a new Phase 2A version and manifest.
2. Create a guarded manual-review launcher that:
   - performs the same repository/package/fixture/protected-save/Mods preflight as the harness;
   - stages transactionally;
   - loads only the exact Working fixture;
   - does not authorize save writes;
   - leaves the user in a controlled review state;
   - waits for the user to exit Kingmaker;
   - restores Mods, Working, protected saves, process/lock/sentinel state exactly;
   - supports `-WhatIf` with zero mutation.
3. Create `docs/PHASE-2A-MANUAL-REVIEW.md` with one command and a concise review checklist.
4. Capture usable screenshots/video-equivalent frame sequences where supported.
5. Run all automated gates and two fresh-process presentation suites.
6. Commit and publish the exact review checkpoint through the guarded non-force helper.
7. Stop with:

```text
BLOCKED - MANUAL VISUAL ACCEPTANCE REQUIRED
```

The stop report must include:

```text
branch/HEAD
version
package path and SHA-256
DLL SHA-256/MVID
manual launcher command
fixture/protected-save authority
technical test totals
known visual compromises
exact review checklist
external restoration state
```

Do not begin combat implementation before explicit user acceptance of that exact build.

---

# 11. Authorization after visual acceptance

When the user explicitly accepts the exact presentation review build, resume this same master mission at Tranche C. Reverify that the reviewed commit/package/hash is the accepted one and that no unauthorized external state changed.

Do not repeat broad Phase 1 or presentation work except for narrow regression gates.

---

# 12. Diagnostic combat target policy

Prefer a real suitable hostile already present in the disposable Working fixture when it can be used without quest/script contamination.

If no controlled target exists, this mission authorizes a runtime-only diagnostic hostile created by project-owned code under all of these conditions:

```text
stock native Kingmaker unit blueprint
simple melee-capable non-quest creature
spawned only after exact Working load
no quest, dialogue, loot, faction-story, or area-script dependency
no XP or reward retained
not serialized
stable runtime-only identity recorded
placed in validated open test space
removed during every cleanup path
zero entity/faction/command residue after run
non-KMC saves unchanged
Working restored exactly by transaction
```

Do not modify the fixture permanently to add an enemy. Do not spawn into Baseline. Do not use a boss, named NPC, quest creature, summon with special rules, or another mod's blueprint.

If temporary equipment is needed for deterministic tests, use a runtime-only transaction that records and restores the rider's exact equipment and inventory state. Prefer an already equipped ordinary melee weapon.

---

# 13. Tranche C - Stationary rider basic melee attack

Implement exactly one basic mounted rider melee attack before broader combat.

Initial contract:

```text
pair already mounted
rider and target stationary
ordinary melee weapon
one target
one attack command
mount natural attacks suppressed
no movement-to-target
no full attack
no charge
no spell or ranged attack
```

Prove:

- the rider owns the attack rule and combat log identity;
- target validity and range use a documented mounted spatial contract;
- exactly one attack roll and at most one damage application occur;
- the mount does not attack automatically;
- facing and animation remain coherent enough for the private alpha;
- one and only one intended action cost occurs;
- misses, hits, criticals when deterministically obtainable, invalid target, target death, rider death, and cleanup are safe;
- non-mounted rider attacks are unchanged;
- real-time and turn-based behavior are separately qualified.

Do not solve range by globally changing every rider or animal-companion reach calculation. Use scoped pair-aware adapters with explicit non-mounted isolation.

Required scenarios, twice in fresh processes where applicable:

```text
mounted-rider-melee-hit-rt
mounted-rider-melee-miss-rt
mounted-rider-melee-hit-tb
mounted-rider-melee-invalid-target
mounted-rider-melee-target-death
mounted-rider-melee-cleanup
non-mounted-melee-control
```

---

# 14. Tranche D - Explicit mount natural attack

Add one explicit mount primary natural attack while the rider remains mounted.

The player must be able to distinguish rider attack from mount attack. Do not rely on uncontrolled companion AI.

Prove:

- attack ownership is the Mammoth;
- combat log, attack roll, damage, reach, facing, and target identity are correct;
- rider does not also attack unless an explicitly tested later action model permits it;
- no duplicate standard/full-round action is spent;
- one command cannot execute both attack paths accidentally;
- mount attack animation does not break rider attachment/pose beyond the accepted limits;
- target/rider/mount death and dismount cleanup are safe;
- non-mounted Mammoth attacks are unchanged.

Required scenarios include real-time, turn-based, invalid target, target death, rider interruption, and non-mounted control, with repeatable fresh-process evidence.

---

# 15. Tranche E - Action economy, movement-to-attack, targeting, and combat lifecycle

After isolated attacks pass, define and implement the pair action model.

## 15.1 Contract first

Update `planning/COMBAT-ACTION-ECONOMY-CONTRACT.md` with exact Kingmaker and Wrath-reference evidence for:

```text
initiative ownership
standard/move/swift/full-round action ownership
mount movement cost to rider
rider movement restrictions after mount movement
mount attack cost
rider attack cost
full-attack eligibility
five-foot-step behavior
command cancellation
turn end
automatic AI suppression or delegation
```

Choose the safest product policy supported by exact contracts. Do not accidentally grant two complete turns to the pair. No automatic rider-plus-mount attack routine is allowed until independently specified and tested.

## 15.2 Movement-to-attack

Implement bounded movement-to-target for one selected attack path at a time.

Prove:

- mount remains the only pathfinding authority;
- stopping distance is derived from the correct attacker/reach contract;
- command delegation cannot oscillate or replace unrelated commands;
- action costs match the chosen model;
- cancellation stops both effective representations;
- rider/mount do not attack twice;
- obstacles, corners, and doorway geometry remain stable;
- selection/camera/UI remain coherent.

## 15.3 Combat lifecycle

Qualify:

```text
enter combat while mounted
leave combat while mounted
turn-based entry and exit through native delivery
real-time entry and exit through native delivery
rider death
mount death
rider unconsciousness
mount incapacitation
prone/trip when applicable
forced movement/teleport when safely testable
party removal
view detach/reattach
save request
load
area transition
mod disable
exception recovery
```

Unsupported disruptive states may force immediate safe dismount. Silent half-mounted continuation is forbidden.

Run each core lifecycle/action scenario twice in fresh processes, with strict validators and exact external restoration.

---

# 16. Tranche F - Bounded stretch: reach, attacks of opportunity, and charge

Proceed only after the core private alpha through Tranche E is green.

## 16.1 Reach

Map and implement the narrowest mounted melee reach contract. Prove rider and mount reach independently, targetability of rider versus mount, and non-mounted isolation.

## 16.2 Attacks of opportunity

Implement only after exact event/action ownership is understood. Prove:

- correct attacker identity;
- one opportunity per qualifying trigger under ordinary rules;
- no rider/mount duplicate opportunity;
- movement authority remains stable;
- turn-based and real-time behavior;
- cleanup and non-mounted isolation.

## 16.3 Basic charge

Implement one basic charge path only if no broad global movement/action patch is required.

Prove:

```text
valid straight/path contract
mount-authoritative movement
correct attacker
correct attack count
correct action cost
no duplicate mount/rider strike
interruption and cancellation
obstacle rejection
turn-based and real-time behavior
residue-free cleanup
```

If AoO or charge cannot qualify without unsafe global behavior, leave that feature default-off, preserve the core alpha, document exact evidence, and generate a focused future mission. Do not weaken gates or abandon the qualified core alpha.

---

# 17. Private-alpha restrictions and user communication

Until separately authorized:

```text
ranged attacks while mounted: disabled with clear reason
spellcasting while mounted: disabled with clear reason
mounted feats: not published
additional mounts: not published
persistent mounted state: not published
enemy riders: not published
```

The UI must communicate unsupported actions clearly rather than failing silently or executing partially.

The private alpha may remain limited to one known Medium humanoid body profile and the Mammoth. Eligibility checks must reject unsupported bodies/mounts safely.

---

# 18. Testing and evidence requirements

## 18.1 Deterministic tests

Expand project-owned tests for:

```text
pose baseline capture/restoration
procedural pose calculations
player-action eligibility and reasons
transient UI lifecycle
save/uninstall policy
combat attacker ownership
targeting and range decisions
action-cost decisions
command delegation
AI suppression/delegation
duplicate-attack prevention
runtime target lifecycle
cleanup prioritization
telemetry and validators
package allowlists
```

Prefer behavior and state outcomes, not implementation details. Avoid broad mocks. Use exact assembly-backed checks and real runtime scenarios when they provide stronger evidence.

## 18.2 Runtime evidence

Every live run records at least:

```text
scenario/run/evidence ID
UTC timestamp
branch/commit/version/package/DLL hash/MVID
fixture and protected-save authority
rider/mount/target stable IDs
relationship state
combat and turn-based state
selection/portrait/action-bar state where applicable
commands and action resources before/after
attacker/target/rule-event identity
attack and damage counts
entity/view/anchor/pose state
movement authority and destination
cleanup trigger/result/residue
exceptions
Mods/save/process/lock/sentinel restoration
```

Screenshots supplement telemetry; they do not replace it.

## 18.3 Fresh-process repeatability

Core runtime claims require two consecutive fresh-process passes from the same clean commit and package unless a report explicitly identifies a narrower one-run exploratory result. A flaky pass is a failure until explained and repaired.

## 18.4 Existing gates

Preserve and extend:

```text
source validation
Release build
component tests
visual-capture tests
harness tests
assembly-backed tests
request/game/final result validators
WhatIf purity
package allowlist
protected-save continuity
Mods transaction restoration
```

Never weaken existing thresholds to admit Phase 2.

---

# 19. Phase 2 kill and pivot criteria

Create numbered Phase 2 criteria. At minimum, a core pivot or stop is required when evidence proves:

1. acceptable pose requires proprietary redistribution;
2. every pose strategy requires unsafe global animator mutation;
3. presentation cannot be restored residue-free;
4. player action or UI necessarily corrupts or orphans save state;
5. mounted attacks duplicate rule events, damage, or action costs irreducibly;
6. combat requires two competing movement agents;
7. non-mounted units are materially affected by scoped combat patches;
8. rider/mount target identity cannot be made coherent;
9. native lifecycle leaves a half-mounted state after save/load/area/death/mod disable;
10. runtime-only target or temporary equipment cannot be removed/restored exactly;
11. external Mods/save restoration cannot be guaranteed;
12. repeatable real-time/turn-based behavior cannot be achieved without unsafe broad patches.

A stretch-only AoO or charge failure does not automatically invalidate core Architecture B. Disable and defer the stretch unless the failure proves a core invariant false.

Architecture C and D remain documented pivots. Do not pivot because they are easier; pivot only when a numbered criterion fires with evidence.

---

# 20. Git, packaging, and publication discipline

Work only on:

```text
codex/mounted-combat-phase2-alpha
```

Commit coherent checkpoints. Suggested messages:

```text
chore: establish Phase 2 governance and contracts
feat: add transient mounted player action
feat: add mounted rider pose profile
fix: restore pose and UI state on cleanup
test: qualify presentation and native lifecycle
feat: add stationary mounted rider melee
feat: add explicit mount natural attack
feat: enforce mounted action economy
feat: add mounted movement-to-attack
experiment: qualify mounted reach and opportunity attacks
experiment: qualify basic mounted charge
test: qualify private mounted combat alpha
docs: publish Phase 2 private alpha evidence
```

Never reset, clean, rebase, force-push, or discard unknown state. Use only the project-owned guarded non-force push helper after its gates pass.

Do not merge to `main`.

Do not create a GitHub release, Nexus package, public announcement, or public-ready compatibility claim. Branch pushes and private local artifacts are authorized.

Packages must exclude game DLLs, Wrath DLLs, decompiled source, extracted proprietary assets, saves, credentials, runtime evidence, machine paths, PDBs unless deliberately allowed, and third-party binary payloads.

---

# 21. Private-alpha definition of done

The mission may finish as `PHASE 2 PRIVATE ALPHA COMPLETE` only when every applicable core row is proven.

## 21.1 Governance and safety

```text
[ ] Phase 2 branch descends from exact Phase 1 main
[ ] AGENTS and durable mission records updated
[ ] frozen Phase 1 artifact preserved
[ ] protected-save authority exact
[ ] Baseline immutable
[ ] Working-only write authorization
[ ] no public release or main merge
```

## 21.2 Persistence and player action

```text
[ ] mounted relationship remains nonserialized
[ ] save/load/area/mod-disable cleanup policy proven
[ ] uninstall/orphan policy documented and tested
[ ] clear Mount/Dismount player action
[ ] exact eligibility/rejection reasons
[ ] no non-mounted or unsupported-unit side effects
```

## 21.3 Presentation

```text
[ ] accepted pose strategy
[ ] idle/walk/run/turn/stop
[ ] mount/dismount transition
[ ] equipment variants
[ ] UI/portrait/action-bar/selection-circle observation
[ ] camera-follow observation
[ ] no pose/animation residue
[ ] manual visual acceptance of exact package
```

## 21.4 Core combat

```text
[ ] stationary rider melee RT/TB
[ ] explicit mount primary attack RT/TB
[ ] exact attacker/target/rule-event identity
[ ] no duplicate attack or damage
[ ] documented action economy
[ ] movement-to-attack
[ ] command cancellation
[ ] combat start/end
[ ] death/incapacitation cleanup
[ ] save/load/area/mod-disable safety
[ ] non-mounted controls
[ ] two fresh-process passes for core claims
```

## 21.5 Stretch disposition

```text
[ ] mounted reach qualified or explicitly deferred with evidence
[ ] attacks of opportunity qualified or disabled/deferred with evidence
[ ] basic charge qualified or disabled/deferred with evidence
[ ] stretch failure did not silently weaken core gates
```

## 21.6 Qualification and artifact

```text
[ ] full source/build/component/visual/harness/assembly gates green
[ ] strict runtime validators green
[ ] exact external restoration after every run
[ ] private alpha package and manifest
[ ] package/DLL hashes and MVID
[ ] implementation and qualification reports
[ ] private playtest instructions
[ ] Phase 3 expansion mission draft
[ ] clean worktree and local/remote branch equality
```

---

# 22. Final response contract

At the manual visual stop, report only the exact accepted-review decision inputs described in Section 10.

After visual acceptance and completion of the private alpha, provide one evidence-rich report containing:

```text
Status
Repository/branch/final local and remote SHA
Version
Accepted visual-review commit/package/hash
Architecture and pose strategy
Persistence/uninstall policy
Player action/UI behavior
Core combat contracts delivered
Action-economy policy
Runtime target policy
Reach/AoO/charge disposition
Source/component/visual/harness/assembly totals
Runtime scenario totals and fresh-process summary
Attack/damage/action-duplication findings
Real-time/turn-based findings
Lifecycle and cleanup findings
Protected-save/Mods restoration
Package path/SHA-256
DLL SHA-256/MVID
Known limitations
Private alpha playtest path
Phase 3 mission-draft path
```

Do not claim unrun tests, unqualified native delivery, presentation the user did not accept, or unsupported features. Do not create a public release or begin Phase 3.
