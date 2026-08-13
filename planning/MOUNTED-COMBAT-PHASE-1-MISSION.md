# CODEX AUTONOMOUS MISSION

## Kingmaker Mounted Combat  Phase 1: Feasibility, Contract Mapping, and Movement Vertical Slice

You are the primary implementation and research agent for a new, independent Pathfinder: Kingmaker mod project. Work continuously and autonomously until this Phase 1 definition of done is met or a listed critical hard stop is proven.

Do not ask the user ordinary implementation questions. Inspect the exact repository, installed Kingmaker and Wrath assemblies, copied open-source references, runtime-harness patterns, assets/metadata, and game behavior. Make conservative engineering decisions, test them, record evidence, and continue.

This mission is intentionally bounded. It does **not** authorize a full Wrath mounted-combat backport. Its purpose is to determine the correct architecture and prove or disprove the foundational movement/attachment/cleanup assumptions before combat mechanics are attempted.

Copy this entire mission into `planning/MOUNTED-COMBAT-PHASE-1-MISSION.md` before substantive implementation.

---

# 1. Mission identity and mandatory separation

Create and complete Phase 1 for this standalone identity:

```text
Product name:       Kingmaker Mounted Combat
Repository:         KingmakerMountedCombat
Assembly:           KingmakerMountedCombat.dll
UMM ID:             KingmakerMountedCombat
Root namespace:     KingmakerMountedCombat
Initial version:    0.0.1-feasibility
Lab root:           C:\Dev\KingmakerMountedCombatLab
Repository root:    C:\Dev\KingmakerMountedCombatLab\repo\KingmakerMountedCombat
Reference source:   C:\Dev\KingmakerMountedCombatLab\reference-source
Harness reference:  C:\Dev\KingmakerMountedCombatLab\harness-reference
Analysis cache:     C:\Dev\KingmakerMountedCombatLab\analysis-cache
Runtime state:      C:\Dev\KingmakerMountedCombatLab\runtime-state
Runtime staging:    C:\Dev\KingmakerMountedCombatLab\runtime-staging
Runtime evidence:   C:\Dev\KingmakerMountedCombatLab\runtime-evidence
Runtime backups:    C:\Dev\KingmakerMountedCombatLab\runtime-backups
Artifacts:          C:\Dev\KingmakerMountedCombatLab\artifacts
Policy root:        C:\Dev\KingmakerMountedCombatLab\codex-policy
Initial branch:     codex/mounted-combat-feasibility
```

This repository and mod are completely separate from:

```text
Kingmaker Buff Planner
Tabletop Added Rules
Kingmaker Gunslinger
Call of the Wild
Wrath of the Righteous
every copied reference repository
```

You MUST NOT:

- modify, commit, reset, clean, rebase, push, package, deploy, or change branches in another project's worktree;
- build this feature inside another mod assembly;
- reuse another mod's UMM ID, namespace, persistence names, release name, blueprint ownership, runtime locks, or save prefixes;
- create a compile-time or runtime dependency on Wrath or another gameplay mod;
- execute a copied Buff Planner/Gunslinger push or deployment helper;
- deploy a Buff Planner/Gunslinger/Tabletop package;
- launch, patch, or modify Wrath;
- commit or package Kingmaker/Wrath DLLs, decompiled game source, extracted proprietary assets, saves, credentials, or third-party binary payloads.

Copied projects are read-only evidence sources.

---

# 2. User-authoritative product intent

The long-term product goal is mounted combat in Pathfinder: Kingmaker resembling Wrath of the Righteous where technically and aesthetically practical.

The user specifically wants this developed as a standalone mod, not folded into the existing gameplay mods.

Phase 1 must answer, with evidence:

1. What exact runtime responsibilities does Wrath's mounted subsystem perform?
2. Which of those responsibilities already have usable Kingmaker equivalents?
3. Which must be reimplemented, adapted, simplified, or rejected?
4. Can one Kingmaker rider and one larger companion move as a stable pair without dual-agent collision, cumulative drift, selection loss, or unsafe global patches?
5. Can the relationship always cleanly terminate on invalidation?
6. Is one candidate rider/mount presentation visually plausible enough to justify combat implementation?
7. Which architecture should Phase 2 implement?
8. What exact risks and kill criteria remain?

The final Phase 1 result may recommend:

```text
A. True dual-entity mounted relationship
B. Authoritative mount movement plus rider combat proxy
C. Hidden/limited companion plus composite rider/mount proxy
D. Abstract mounted-state mechanics plus cosmetic presentation
E. Stop: no responsible implementation path found
```

Do not bias the investigation toward the most ambitious result.

---

# 3. Exact technical targets

Prove the actual local environment before relying on expected values.

Expected Kingmaker target:

```text
Game:               Pathfinder: Kingmaker Enhanced Plus Edition
Game version:       2.1.7b
Steam app ID:       640820
Target framework:   .NET Framework 4.7
C# language level:  7.3
Unity:               exact game-installed Unity 2018-era assemblies
UMM:                 exact installed known-good 0.32.x baseline
Harmony:             exact installed legacy Harmony12 compatibility surface
Shell:               native Windows PowerShell
```

Wrath is an exact local read-only reference installation. Record:

```text
storefront
displayed/product/file version when obtainable without launching
Wrath executable identity when present
Assembly-CSharp.dll assembly identity, MVID, length, and SHA-256
relevant Unity assembly identities and SHA-256
Unity/player version evidence
local install path in ignored evidence only
```

Do not use:

- modern Harmony 2 APIs in the Kingmaker production mod;
- Wrath-only Owlcat APIs in production;
- .NET Standard or modern runtime assumptions in production;
- C# syntax unavailable in 7.3;
- a public decompilation as authority when the exact local assembly can be inspected;
- a Wrath blueprint GUID as proof of a Kingmaker asset;
- bulk decompilation as a substitute for a dependency graph;
- game DLLs copied into source control or packages.

Production references must resolve only against the local Kingmaker installation and use Copy Local disabled.

---

# 4. Phase 1 non-goals

Do not implement or claim:

```text
full mounted combat
full Wrath parity
Cavalier class/archetypes
mounted feats
mounted charge
rider full attacks
mount natural attacks
ranged mounted combat
spellcasting while mounted
attacks of opportunity
trip/prone/grapple rules
enemy or AI riders
multiple mount species
Small-rider support
polymorph/size-change support
indoor campaign-wide support
mounted save persistence
public release readiness
```

You may inspect and map the future contracts for those areas, but production behavior in this mission is movement-only, out-of-combat, one pair, one candidate mount.

Do not publish a public GitHub release, Nexus page, or distributable gameplay release.

---

# 5. Durable mission files

Before substantive production code, create and commit:

```text
planning/MOUNTED-COMBAT-PHASE-1-MISSION.md
planning/ASSEMBLY-CONTRACT-MATRIX.md
planning/MOUNTED-SUBSYSTEM-DEPENDENCY-GRAPH.md
planning/KINGMAKER-WRATH-TYPE-MAP.json
planning/ASSET-RIG-ANIMATION-INVENTORY.md
planning/ARCHITECTURE-OPTIONS.md
planning/RISK-AND-KILL-CRITERIA.md
planning/RUNTIME-SCENARIO-MATRIX.md
MOUNTED-COMBAT-JOURNAL.md
AUTONOMOUS-RESUME.md
AUTONOMOUS-BLOCKERS.md
docs/PHASE-1-FEASIBILITY-REPORT.md
docs/PHASE-1-IMPLEMENTATION-REPORT.md
docs/PHASE-1-QUALIFICATION.md
docs/PHASE-2-RECOMMENDATION.md
docs/PHASE-2-MISSION-DRAFT.md
THIRD-PARTY-NOTICES.md
```

## 5.1 Journal rules

After every meaningful investigation, hypothesis change, implementation checkpoint, runtime run, failed strategy, coherent commit, restoration transaction, or publication action, append:

```text
date/time
branch and exact HEAD
active version
work completed
commands/tests run
exact PASS/FAIL counts
runtime evidence IDs and paths
assembly/package/DLL hashes or MVIDs when relevant
rejected theories and why
current uncertainty
external state and restoration result
exact next action
```

The final line of each checkpoint states the next concrete action.

## 5.2 Resume rules

Before context compaction, agent handoff, long runtime transition, or token exhaustion, update `AUTONOMOUS-RESUME.md` with:

```text
exact branch/HEAD/status
active version
last successful gate
current failure/hypothesis
exact files being changed
exact next command
analysis/runtime profile state
unrestored external state, normally none
```

Never ask the user to restate the mission after compaction.

## 5.3 Status vocabulary

Use:

```text
TODO
IN PROGRESS
PASS
FAIL
DEFER  EVIDENCED
UNAVAILABLE-LOCAL-REFERENCE
FEATURE-NOT-PRESENT
PHASE 1 COMPLETE  PROCEED RECOMMENDED
PHASE 1 COMPLETE  PIVOT RECOMMENDED
PHASE 1 COMPLETE  MANUAL VISUAL REVIEW REQUIRED
BLOCKED  CRITICAL
```

A build is not runtime proof. A main-menu load is not movement proof. A cosmetic transform attachment is not a mounted subsystem.

---

# 6. Mandatory intake

Perform and record every step:

1. Confirm the current working directory is the standalone repository.
2. Read global and repository `AGENTS.md`.
3. Confirm the working tree is clean and no Git lock/rebase/merge is unresolved.
4. Confirm `origin` points only to the standalone repository.
5. Record branch, HEAD, status, remotes, and Git author.
6. Create/switch to `codex/mounted-combat-feasibility` without destructive operations.
7. Inventory every lab directory.
8. Read `environment-intake.json`.
9. Hash and record exact Kingmaker, UMM, Harmony, and referenced Unity DLLs.
10. Hash and record exact Wrath reference assemblies and relevant Unity DLLs.
11. Record MVIDs and assembly identities for both `Assembly-CSharp.dll` files.
12. Verify .NET Framework 4.7 targeting pack and C# 7.3 build path.
13. Verify `ilspycmd` and its version.
14. Inventory source-reference repositories, exact commits, licenses, and dirty state.
15. Inventory the harness snapshot, its source commit, and missing optional files.
16. Verify no reference is a linked writable active worktree.
17. Verify no game DLL or proprietary extracted asset is inside Git.
18. Verify Steam/save policy from local evidence without changing account settings autonomously.
19. Verify disposable KMC save availability without loading a valued save.
20. Create the durable mission records and initial matrices.
21. Create an original project-specific guarded push helper under the lab policy root.
22. Commit the intake before implementation.

If Wrath is unavailable locally, continue every Kingmaker-only and open-source task, mark exact Wrath-local work `UNAVAILABLE-LOCAL-REFERENCE`, and determine whether that prevents a truthful architecture decision. Do not silently substitute an unrelated build.

---

# 7. Reference hierarchy and licensing

Use sources in this order:

1. exact locally installed Kingmaker assemblies and runtime behavior;
2. exact locally installed Wrath assemblies and metadata;
3. locally copied open-source repositories at recorded commits;
4. official documentation;
5. secondary notes only as leads.

The local Wrath assembly is the authority for Wrath member contracts. The local Kingmaker assembly and runtime are the authority for whether a backport seam exists.

Open-source references may include:

```text
Vek17/TabletopTweaks-Base
zephe0n/AutoMount
fl01/pathfinder-wotr-multiplayer
CasDragon/DragonFixes
completed Buff Planner runtime-harness snapshot
```

For every reused open-source fragment, record:

```text
repository
commit
path
license
original or adapted
purpose
substantial-copy/notice obligation
```

Do not copy code until license compatibility is proven. Prefer original implementation informed by contracts.

## 7.1 Proprietary decompilation boundary

Allowed:

```text
bounded type/member listing
bounded method decompilation into ignored analysis-cache
metadata tokens/signatures
call/caller observations
field/property/event inventories
MVID and SHA-256
original pseudocode summaries
original dependency diagrams
small factual excerpts when necessary
```

Forbidden in Git/package:

```text
bulk decompiled Kingmaker or Wrath source trees
large copied method bodies
Wrath assemblies
Kingmaker assemblies
extracted models
animation clips/controllers
textures/materials
asset bundles
serialized proprietary blueprints
```

Keep analysis-cache ignored. Reports should contain original summaries, not a reconstructed Owlcat source distribution.

---

# 8. Wrath mounted-subsystem forensic map

Begin with bounded searches for exact local types and all direct/indirect collaborators. Names below are leads, not assumptions:

```text
UnitPartRider
UnitPartSaddled
SaddledUnitController
mount/dismount ability or action types
IUnitMountHandler or equivalent events
rider/mount conditions and restrictions
UnitMovementAgentBase
avoidance enable/disable state
UnitEntityData or BaseUnitEntity rider helpers
UnitCommands and command collections
UnitCommand
movement commands
attack commands
UnitCombatState action/movement restrictions
selection/click/ground handlers
group commands
followers formation
camera follow
view attach/detach
animation managers
interaction/cutscene/area-transition hooks
entity-part references
JSON/save serialization
death/unconsciousness/state-change handlers
size/footprint/reach helpers
turn-based controller hooks
```

For each relevant Wrath type/member, record in `ASSEMBLY-CONTRACT-MATRIX.md`:

```text
assembly identity/MVID
fully qualified type
member signature
metadata token where useful
responsibility
state owned
entry conditions
exit/cleanup behavior
direct callers
direct callees
events raised/consumed
movement/pathing implications
command/action implications
view/animation implications
selection/UI implications
serialization implications
failure behavior
open-source observations
confidence
evidence path
```

Do not stop at the visible `Mount()` call. Trace enough of the graph to explain how movement, commands, selection, cleanup, and persistence work.

Produce a dependency graph showing at least:

```text
relationship state
command delegation
movement authority
avoidance/collision
entity position
view attachment
selection
formation
combat/action coupling
serialization
lifecycle cleanup
```

---

# 9. Exact Kingmaker equivalence/absence map

For each Wrath responsibility, inspect the exact Kingmaker assembly and classify:

```text
DIRECT EQUIVALENT
OLDER EQUIVALENT
USABLE CONTROL POINT
PARTIAL PRIMITIVE
ABSENT  REIMPLEMENT
ABSENT  SIMPLIFY
UNSAFE GLOBAL SEAM
UNKNOWN  MORE EVIDENCE REQUIRED
```

`planning/KINGMAKER-WRATH-TYPE-MAP.json` must include machine-readable entries such as:

```json
{
  "responsibility": "authoritative movement delegation",
  "wrath": {
    "types": [],
    "members": [],
    "evidence": []
  },
  "kingmaker": {
    "classification": "USABLE CONTROL POINT",
    "types": [],
    "members": [],
    "evidence": []
  },
  "proposedStrategy": "adapter",
  "risks": [],
  "confidence": "medium"
}
```

Investigate exact Kingmaker contracts for:

- movement-agent ownership;
- path request and destination handling;
- collision/avoidance;
- entity versus view position and rotation;
- party/group movement;
- selection;
- active unit/portrait behavior;
- formation slots;
- pause/unpause;
- combat start/end;
- real-time and turn-based controller transitions;
- view attach/detach;
- death/unconsciousness;
- area unload/transition;
- save/load serialization;
- pet/master relationships;
- size and footprint.

Do not implement a global patch because it is convenient. A broad patch must identify every affected non-mounted path and prove it is inert outside a scoped active relationship.

---

# 10. Kingmaker asset, rig, and animation metadata inventory

Inventory candidate native Kingmaker creatures that could serve as the first mount.

Prefer:

1. an existing horse with a usable combat/movement view and ordinary pathing;
2. otherwise one stable Large quadruped already usable as an animal companion;
3. use only one candidate in the vertical slice.

Record, without committing proprietary assets:

```text
blueprint/view identifiers
display/internal name
native ownership
size
footprint/corpulence/collision metadata
movement speed and movement-agent type
view prefab identifier
skeleton/transform hierarchy names
candidate spine/saddle/root anchors
local coordinate conventions
animation controller identifiers
available idle/walk/run/turn/attack/hit/death clip names
rider candidate skeleton/body type
known scale/offset observations
asset-bundle or resource provenance
hashes for locally generated metadata outputs
```

Use original diagnostic tooling or a vetted analysis tool. Extracted objects remain ignored and local.

Evaluate at least:

```text
anchor stability during idle
anchor stability during walk/run
turning behavior
vertical displacement
body clipping risk
weapon clipping risk
Medium humanoid pose compatibility
camera-distance presentation
door/ceiling risk
```

Do not import Wrath mount/rider animations. Determine what Kingmaker-native presentation is possible.

---

# 11. Architecture alternatives and decision rubric

Analyze and score these architectures:

## A. True dual-entity relationship

Both rider and mount remain independent combat entities; mount controls movement; rider remains separately targetable/action-capable.

## B. Authoritative mount plus rider combat proxy

Mount owns pathing and physical footprint; rider remains the principal logical combat entity with carefully scoped synchronization and command delegation.

## C. Composite proxy

One visible/controllable composite represents the pair while the companion is hidden, suspended, or represented through a narrow proxy/resource model.

## D. Abstract mounted state

The rider receives movement/rules benefits and a cosmetic or limited mount presentation; no full two-entity simulation.

Score each from evidence on:

```text
movement/pathfinding stability
selection/formation compatibility
independent targeting potential
future action-economy correctness
turn-based feasibility
real-time feasibility
save/uninstall safety
animation/presentation quality
indoor behavior
implementation complexity
Harmony patch surface
compatibility risk
testability
maintainability
licensing/asset independence
```

Record assumptions and confidence. Do not select an architecture merely because it most closely resembles Wrath.

---

# 12. Pre-code feasibility gate

Do not implement the mounted pair until evidence identifies a plausible, scoped mechanism for all of:

```text
choosing and validating one rider/mount pair
making one entity authoritative for movement
preventing dual-agent self-collision/avoidance conflict
routing or suppressing rider movement commands
maintaining required entity/view alignment
maintaining stable selection/control
leaving non-mounted units unaffected
detaching safely and idempotently
clearing on every observed lifecycle invalidation
avoiding mounted relationship serialization in Phase 1
```

The gate passes only when the contract matrix cites exact Kingmaker members or a bounded, testable adapter seam.

If it does not pass after exhaustive bounded investigation, do not write a speculative global movement patch. Complete the evidence and recommend Architecture C/D or stop.

---

# 13. Minimal standalone mod and build system

After intake and contract gate preparation, create a minimal standalone mod with:

```text
SDK/classic project style compatible with exact target
net47 target
C# 7.3
AnyCPU, Prefer32Bit=false unless exact evidence requires otherwise
UMM Info.json
entry point and structured logging
version source
ignored LocalGamePaths.props
Kingmaker references with Copy Local disabled
build script
package script
package validator
source validator
test runner
diagnostic settings
runtime request/result skeleton
```

Release/diagnostic package must contain only project-owned or legally redistributable files.

Exclude:

```text
game DLLs
Wrath DLLs
UMM/Harmony DLLs
third-party mod DLLs
decompiled source
asset extracts
PDBs unless deliberately allowed
saves
runtime evidence
machine paths
credentials
bin/obj/.vs
```

Gate:

```text
source validation PASS
clean build PASS
package validation PASS
mod-load-smoke PASS twice in fresh Kingmaker processes
live Mods state restored exactly
```

A main-menu load only proves the scaffold, not mounted feasibility.

---

# 14. Guarded runtime harness

Adapt the copied harness patterns into original KMC-owned scripts and types.

The harness must:

```text
use request/result JSON with schemas and validation
bind every run to exact branch/commit/version/DLL hash/MVID
stage the live Mods directory transactionally
take a byte/hash inventory before mutation
hold a lock/sentinel
restore in finally
verify restoration after process exit
reject stale or ambiguous transaction state
support -WhatIf with zero external mutation
protect every non-KMC save
load only KMC_AUTOMATION_WORKING
refuse KMC_AUTOMATION_BASELINE writes
stop on unexpected Steam/account/cloud/update UI
avoid arbitrary process killing
record screenshots when safely available
record structured telemetry independent of screenshots
```

Before live use, prove:

```text
request schema tests
result schema tests
source-only validation
transaction preflight
WhatIf purity
package allowlist
save allowlist
restore simulation
stale-lock rejection
```

Do not execute copied project helpers.

---

# 15. Relationship state machine

Implement relationship logic outside patches/UI.

Suggested states, adapted to evidence:

```text
Unmounted
Validating
Mounting
Mounted
Dismounting
Faulted
Disposed
```

Required invariants:

```text
at most one active Phase 1 pair
rider and mount are distinct
both are controllable and alive
mount is larger than rider under exact Kingmaker size rules
mount is the rider's exact active qualifying companion
relationship is out-of-combat only
only the mount owns authoritative pathing while mounted
cleanup is idempotent
partial mount failure rolls back
partial dismount failure continues best-effort cleanup and reports exact residue
no mounted state survives mod disable/process teardown intentionally
no custom mounted relationship is serialized in campaign saves
```

Pure/deterministic tests must cover:

```text
valid mount transition
invalid same-unit pair
invalid dead/incapacitated pair
invalid size relationship
invalid non-companion pair
double mount rejection
dismount idempotence
mount failure rollback
invalidation during mounting
combat-start cleanup
view-detach cleanup
area-unload cleanup
death cleanup
mod-disable cleanup
exception cleanup
```

Do not hide state inside static patch fields without an explicit lifecycle owner.

---

# 16. Movement-only vertical slice

If and only if the pre-code gate passes, implement exactly one controlled vertical slice.

## 16.1 Allowed behavior

```text
one validated Medium humanoid rider
one chosen larger native Kingmaker companion/mount
manual Mount diagnostic action
manual Dismount diagnostic action
out-of-combat only
mount authoritative movement
rider view attached/synchronized to a proven anchor or bounded offset
rider logical state synchronized only as exact engine contracts require
stable player selection
safe party movement
structured telemetry
forced safe cleanup on invalidation
```

## 16.2 Explicit exclusions

The pair must not gain:

```text
mounted combat bonuses
mount attacks
rider attacks while mounted
charge
spellcasting while mounted
full attacks
attacks of opportunity
special feats
enemy AI support
save persistence
automatic remount
multiple mount options
```

If combat starts, the Phase 1 prototype must safely dismount/clear before ordinary combat proceeds.

## 16.3 Movement authority

Prove, rather than assume:

- which entity receives the destination;
- which movement agent is enabled;
- how avoidance is changed and restored;
- whether the rider entity position must track the mount;
- whether only the rider view should attach;
- how group movement sees the pair;
- how selection and camera follow behave;
- how destination cancellation and stop commands propagate.

Do not run two ordinary nav agents toward the same destination and call visual proximity success.

Do not teleport both full entities every frame as the final solution. A temporary diagnostic synchronization experiment may be used only when explicitly measured, isolated, reversible, and rejected or justified in the architecture report.

## 16.4 Attachment evidence

Try candidate anchors/offsets conservatively.

Record:

```text
anchor transform
local position offset
local rotation
rider scale
pose/animation strategy
residual local-position error
residual rotation error
clipping observations
frame/sample count
runtime duration
```

Initial stability target:

```text
no cumulative drift
no visible oscillation
no pair separation after stop/start
residual error relative to expected anchor remains within a calibrated <= 0.10 world-unit target, or a stricter evidence-backed threshold
```

If exact engine/view behavior makes this metric inappropriate, replace it with a documented equivalent before testing; do not silently remove the gate.

---

# 17. Mandatory runtime scenarios

Implement exact or equivalently named scenarios with structured PASS/FAIL results.

## 17.1 Scaffold and forensic exports

```text
mod-load-smoke
export-mounted-contracts
export-candidate-mount-rigs
observe-mount-diagnostic-availability
```

## 17.2 Pair lifecycle

```text
mounted-pair-create-and-clear
mounted-pair-double-mount-rejected
mounted-pair-invalid-pair-rejected
mounted-pair-cleanup-idempotent
mounted-pair-death-cleanup
mounted-pair-combat-start-cleanup
mounted-pair-area-unload-cleanup
mounted-pair-mod-disable-cleanup
```

## 17.3 Movement

```text
mounted-pair-open-ground
mounted-pair-stop-start
mounted-pair-turns-and-corners
mounted-pair-doorway
mounted-pair-selection
mounted-pair-party-formation
mounted-pair-pause-unpause
mounted-pair-destination-cancel
```

## 17.4 Mode and save boundaries

```text
mounted-pair-turn-based-entry-cleanup
mounted-pair-realtime-entry-cleanup
mounted-pair-save-safety
mounted-pair-load-safety
mounted-pair-area-transition-safety
```

Phase 1 may cleanly dismount rather than remain mounted at those boundaries. It must not silently serialize a custom relationship.

## 17.5 Required telemetry

At useful intervals record:

```text
scenario/run/evidence ID
UTC timestamp
branch/commit/version
DLL hash/MVID
rider/mount stable IDs
relationship state
combat and turn-based state
requested destination
authoritative mover
movement-agent enabled flags
avoidance flags
entity positions/rotations
view positions/rotations
expected anchor transform
residual position/rotation error
selection state
active commands
formation state
cleanup trigger
cleanup result/residual state
exceptions
Mods restore result
save protection result
```

A screenshot is supplementary; telemetry must independently explain the state.

Run core lifecycle and movement scenarios twice consecutively in fresh processes from the same clean commit. A flaky pass is a failure until explained and repaired.

---

# 18. Doorway, corners, and formation acceptance

Open-ground success is insufficient.

The selected pair must be tested through:

- a substantial turn;
- repeated direction reversals;
- a corner near collision geometry;
- an ordinary doorway or narrow passage valid for the mount;
- a party group-move command;
- selection switches away and back;
- pause/unpause;
- destination cancellation;
- a stationary wait long enough to expose drift.

Record:

```text
path reached or exact rejection reason
stuck duration
oscillation count
unexpected repath count
maximum anchor residual
selection loss count
non-mounted party interference
cleanup after test
```

Do not label a doorway failure as a mounted-system failure when the unmounted mount itself cannot traverse it. Establish a control run with the same mount unmounted.

---

# 19. Visual and animation review

The first slice may use a crude but stable Kingmaker-native pose; it may not import Wrath animation.

Required evidence:

```text
idle
walk
run when available
turn
stop
selection circle
party movement
doorway/corner
mount and dismount transition or explicit instantaneous diagnostic transition
```

Classify presentation:

```text
PLAUSIBLE FOR PHASE 2
MECHANICALLY VIABLE, NEW ANIMATION/POSE WORK REQUIRED
ONLY ACCEPTABLE FOR ABSTRACT/COMPOSITE ARCHITECTURE
UNUSABLE
```

If safe automated capture is unavailable, do not fabricate visual proof. Complete telemetry and reports, then use:

```text
PHASE 1 COMPLETE  MANUAL VISUAL REVIEW REQUIRED
```

Include exact reproducible manual acceptance steps and the commit/package hash.

---

# 20. Kill criteria

A kill criterion does not mean the whole project failed. It means the current architecture must be abandoned or simplified.

Trigger and document a pivot when, after bounded evidence-driven attempts:

1. rider and mount require two active nav agents that collide, oscillate, or diverge;
2. no scoped control point can make one mover authoritative;
3. the pair cannot traverse valid turns/doorways that the unmounted mount can traverse;
4. selection or formation cannot be restored reliably;
5. dismount/cleanup leaves movement, avoidance, selection, command, or view residue;
6. the implementation requires a broad global movement patch with material non-mounted effects;
7. save/load or area transitions can retain a corrupt/half-mounted state;
8. acceptable presentation requires importing Wrath proprietary assets;
9. one native Kingmaker candidate cannot produce a remotely plausible rider presentation;
10. repeated fresh-process tests remain nondeterministically unstable;
11. the only apparent solution depends on a Wrath runtime assembly;
12. external-state safety cannot be guaranteed.

When a kill criterion triggers:

- preserve exact failing evidence;
- revert only through ordinary forward commits, never destructive resets;
- disable the unsafe experiment behind a default-off diagnostic flag;
- score the remaining architectures;
- implement no broader workaround in this mission;
- produce a pivot recommendation and Phase 2 mission draft.

---

# 21. Test strategy

## 21.1 Pure/component tests

Prefer deterministic tests for:

```text
relationship state transitions
pair validation
command-routing decisions
cleanup trigger prioritization
cleanup idempotence
rollback after partial failure
telemetry calculations
residual-error thresholds
request/result validation
transaction preflight
package allowlist
hash/MVID recording
architecture scoring inputs
```

## 21.2 Assembly-backed tests

Where feasible, load exact local assemblies in a separate analysis/test process for:

```text
type/member presence
signature/token inventories
inheritance/interface relationships
field/property/event contracts
Kingmaker/Wrath map generation
deterministic contract export
```

Never load both same-named game assemblies into one default context in a way that conflates types. Use separate processes, metadata readers, aliases, or isolated load contexts appropriate to the tool.

## 21.3 Runtime tests

Kingmaker runtime evidence is authoritative for:

```text
movement
avoidance
selection
formation
view attachment
cleanup
area/combat boundary behavior
save safety
Mods restoration
```

Do not mock Kingmaker behavior when a controlled disposable runtime scenario can prove it.

## 21.4 Testing style

- Assert player/game-observable behavior and saved evidence, not private implementation details.
- Use real project state machines and serialized request/result fixtures.
- Avoid broad module mocks.
- Stub only a narrow external boundary.
- Do not add a library unless it replaces substantial code and supports net47/tooling constraints.
- Every corrected defect gets a regression test or runtime scenario.

---

# 22. Architecture decision gate

After forensics, asset inventory, and the vertical slice or pivot evidence, update `planning/ARCHITECTURE-OPTIONS.md`.

For every architecture include:

```text
summary
exact Kingmaker seams
required new subsystems
Harmony patch surface
movement result
selection result
cleanup result
visual result
future combat feasibility
turn-based feasibility
save strategy
known incompatibilities
licensing/asset posture
estimated complexity
fatal risks
confidence
```

A proceed recommendation requires:

```text
contract map substantially complete
scoped movement authority proven
lifecycle cleanup proven
non-mounted isolation proven
two-pass fresh-process movement/lifecycle suite
no save corruption/residue
at least one visually plausible or explicitly reviewable presentation
future combat seams identified
```

A pivot recommendation is correct when a simpler architecture clearly dominates on stability and maintainability.

Do not equate sunk implementation effort with evidence for proceeding.

---

# 23. Phase 2 recommendation and mission draft

Do not execute Phase 2.

Create `docs/PHASE-2-RECOMMENDATION.md` containing:

```text
recommended architecture
evidence and confidence
contracts proven
contracts still unknown
required player-facing compromises
future scope order
risk controls
manual decisions, if any
estimated engineering bands
explicit non-goals
```

Create `docs/PHASE-2-MISSION-DRAFT.md` tailored to the actual result.

If proceed is recommended, the draft should ordinarily stage future work in this order:

```text
basic mounted pair persistence policy
rider basic melee attack
mount basic natural attack
movement/action economy
combat start/end behavior
death/unconsciousness/prone/forced movement
turn-based and real-time parity
charge
reach and attacks of opportunity
ranged/spellcasting restrictions
mounted feats
additional mounts/rider sizes
UI and polish
compatibility/hardening
```

If pivot is recommended, draft the simplified architecture mission instead.

The draft must preserve evidence gates and must not assume unproven contracts.

---

# 24. Git and publication discipline

Commit after coherent checkpoints.

Suggested messages:

```text
chore: establish mounted combat feasibility mission
docs: map Wrath mounted subsystem contracts
docs: classify Kingmaker mounted control points
build: add standalone Kingmaker diagnostic scaffold
test: add guarded KMC runtime transaction
feat: add mounted relationship state machine
experiment: add single-pair movement authority
test: qualify mounted movement and cleanup
docs: publish Phase 1 architecture recommendation
```

Never use destructive history/worktree operations.

The guarded push helper must verify:

```text
exact repository root
permitted branch prefix
not detached HEAD
no unresolved merge/rebase
expected origin
no prohibited payloads or secrets
clean/intentional state
current branch only
local and remote SHA output
```

Direct `git push` is prohibited by policy. Use the helper.

A remote outage does not justify discarding local work. Continue local commits and record the blocker.

Do not publish a public release.

---

# 25. Autonomy rules

Continue without user input through:

```text
ordinary compile failures
test failures
bounded decompilation/tool syntax investigation
API-signature discovery
failed candidate hooks
failed candidate anchors
runtime scenario failures with successful restoration
telemetry improvements
UI diagnostics
context compaction
subagent rate limits
missing optional open-source files
one rejected architecture
```

When one path fails, preserve evidence and investigate the next bounded hypothesis.

Use subagents only for independent read-only work such as:

```text
Wrath dependency-map subset
Kingmaker equivalence-map subset
open-source license/source review
asset metadata inventory subset
test/report consistency review
Harmony patch-surface audit
```

The primary agent owns integration, Git, versioning, external state, runtime transactions, and final claims. Never allow two agents to mutate the same files or operate the runtime deployment transaction concurrently.

Do not wait passively for the user. Do not ask the user to choose names, offsets, thresholds, or ordinary architecture details that evidence can resolve.

---

# 26. Critical hard stops

Stop only after a durable checkpoint, exact evidence, safe external-state restoration, and a precise next command.

## 26.1 Identity/repository ambiguity

- current directory is not the standalone repository;
- origin points to another project;
- another process/agent is actively mutating the same worktree;
- branch ancestry cannot be identified without overwrite/reset;
- reference sources are linked writable active worktrees and cannot be isolated.

## 26.2 External-state safety

- live Kingmaker `Mods` state cannot be restored exactly;
- a runtime transaction lock is ambiguous;
- a protected save was or may have been modified;
- baseline and working fixture identity cannot be distinguished;
- an unexpected game/Steam process cannot be safely attributed;
- continuing requires deletion/overwrite of unknown external files.

## 26.3 Steam/account boundary

- credential, Steam Guard, purchase, cloud conflict, update, account, or Remote Play prompt blocks launch;
- Offline Mode cannot be used safely;
- simultaneous account use creates an invalid session.

Do not automate credentials or click through these prompts.

## 26.4 Exact platform unavailable

- required Kingmaker/UMM/Harmony files are corrupt or missing;
- net47 reference assemblies or MSBuild cannot be located through ordinary setup;
- exact assembly inspection cannot establish a coherent Kingmaker target;
- required analysis tooling cannot safely inspect the assemblies and no bounded alternative exists.

Missing Wrath local reference is not automatically critical; classify whether it prevents the final architecture claim.

## 26.5 Licensing/proprietary blocker

- a core path requires copying code/assets without permission;
- required attribution/license cannot be established;
- acceptable presentation requires redistributing Wrath assets.

Choose an original or simplified design before stopping.

## 26.6 Irreducible safety/architecture ambiguity

- no conservative default exists for a decision with major save or global movement consequences;
- every candidate architecture requires unsafe global behavior;
- a truthful recommendation cannot be made from obtainable evidence.

## 26.7 Tool/service exhaustion

- the active Codex environment cannot continue due to a hard quota or unrecoverable tool failure.

Before stopping, commit safe work, update journal/resume/blockers, restore external state, and state the exact next command.

---

# 27. Phase 1 definition of done

Every applicable core row must be proven.

## 27.1 Standalone identity

```text
[ ] independent repository, assembly, UMM ID, namespace, package, save prefix
[ ] no mutation/dependency on Buff Planner/Tabletop/Gunslinger/Wrath
[ ] no game/third-party DLL payloads
[ ] no proprietary source/assets committed
```

## 27.2 Environment and provenance

```text
[ ] exact Kingmaker/UMM/Harmony/Unity identities and hashes
[ ] exact Wrath reference identities/hashes or explicit unavailability
[ ] exact source-reference commits/licenses
[ ] exact harness-reference provenance
[ ] environment fingerprint generated
```

## 27.3 Contract mapping

```text
[ ] Wrath mounted subsystem dependency graph
[ ] Kingmaker equivalence/absence map
[ ] movement/pathing authority mapped
[ ] command routing mapped
[ ] selection/formation mapped
[ ] view/animation lifecycle mapped
[ ] serialization/cleanup risks mapped
[ ] claims cite exact evidence
```

## 27.4 Asset/rig inventory

```text
[ ] candidate native Kingmaker mounts inventoried
[ ] one selected with evidence
[ ] skeleton/anchor metadata recorded
[ ] movement/animation metadata recorded
[ ] no proprietary asset committed/shipped
```

## 27.5 Scaffold and harness

```text
[ ] clean net47/C# 7.3 build
[ ] package validation
[ ] loadable standalone diagnostic mod
[ ] request/result runtime runner
[ ] transactional Mods staging/restoration
[ ] KMC save protection
[ ] mod-load-smoke twice in fresh processes
```

## 27.6 Relationship domain

```text
[ ] explicit state machine
[ ] pair validation
[ ] partial-failure rollback
[ ] cleanup idempotence
[ ] lifecycle cleanup triggers
[ ] no intended save serialization
[ ] pure/component tests pass
```

## 27.7 Movement vertical slice or evidence-backed pivot

Proceed path:

```text
[ ] one rider/one mount
[ ] one authoritative mover
[ ] no dual-agent collision/oscillation
[ ] open ground
[ ] stop/start
[ ] corners
[ ] doorway with unmounted control
[ ] selection
[ ] party formation
[ ] pause/unpause
[ ] destination cancellation
[ ] cleanup boundaries
[ ] two fresh-process passes
```

Pivot path:

```text
[ ] pre-code or runtime kill criterion proven
[ ] unsafe experiment disabled
[ ] exact evidence retained
[ ] alternatives rescored
[ ] simplified architecture recommended
```

## 27.8 Safety

```text
[ ] no protected save accessed/mutated
[ ] baseline immutable
[ ] every runtime transaction restored
[ ] no stale movement/avoidance/selection/view state after cleanup
[ ] no Wrath launch/modification
[ ] no credentials/account prompts automated
```

## 27.9 Reports

```text
[ ] feasibility report
[ ] implementation report
[ ] qualification report
[ ] architecture recommendation
[ ] known limitations
[ ] exact manual visual steps when needed
[ ] tailored Phase 2 mission draft
[ ] clean working tree
[ ] coherent commits
[ ] local/remote equality when publication is available
```

Do not downgrade a core row merely because it is difficult.

---

# 28. Final response contract

When complete or critically stopped, provide one concise, evidence-rich report containing:

```text
Status:
  PHASE 1 COMPLETE  PROCEED RECOMMENDED
  PHASE 1 COMPLETE  PIVOT RECOMMENDED
  PHASE 1 COMPLETE  MANUAL VISUAL REVIEW REQUIRED
  BLOCKED  CRITICAL

Repository and branch
Final local and remote SHA
Version
Kingmaker/UMM/Harmony identities and hashes
Wrath reference identity and hash, or exact unavailability
Source-reference commits/licenses
Architecture options and final recommendation
Selected rider/mount and why
Contract-map coverage counts
Pure/component test totals
Assembly-backed test totals
Runtime scenario totals
Two-pass fresh-process summary
Maximum measured residual/drift and movement findings
Doorway/control findings
Selection/formation findings
Cleanup/save-safety findings
Mods restoration result
Visual classification
Package/DLL paths, hashes, and MVID
Known limitations
Phase 2 mission-draft path
Exact blocker and next command only when blocked
```

Do not claim tests that were not run. Do not describe a planned feature as delivered. Do not call a cosmetic attachment mounted combat. Summarize the load-bearing evidence rather than asking the user to infer it from logs.
