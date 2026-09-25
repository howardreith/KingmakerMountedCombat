# Chunk 6A: legal combat Mount/Dismount

Single active report for the owner's Chunk 6A mission. Status, identities and
the acceptance ledger live here; the frozen product and action contract is
[planning/CHUNK6-ACTION-CONTRACT.md](../planning/CHUNK6-ACTION-CONTRACT.md) and
the installed-assembly seam map is the Chunk 6A section at the top of
[planning/ASSEMBLY-CONTRACT-MATRIX.md](../planning/ASSEMBLY-CONTRACT-MATRIX.md).

## Status

**IMPLEMENTATION CANDIDATE — NATIVE QUALIFICATION BLOCKED. PARTIAL overall.** The
acceptance ledger's completion gate does **not** pass: all 82 mandatory behaviors
are BLOCKED, so **0 of 82** are demonstrated. The work is built, gated offline and
published, and that is not the same as complete — this milestone is complete only
when `scripts/Test-Chunk6aLedger.ps1 -Completion` reports every mandatory case PASS
on one frozen candidate. This is not a candidate for acceptance and must not be
described as one, and no part of it may be called finished engineering while the
completion ledger stands at 0/82.

### The review remediation: six repairs

An owner review of the published preview.107 candidate found six defects. All six
are repaired on this candidate; none was argued away and none of the earlier
retained failures was removed.

**R1 — a spent partner slot is closed, not reopened.** The retain disposition
recorded a partner whose native initiative slot had already passed as `Granted`
and `Prepared` but **not** `Ended`, and the tests affirmed that the pair could
still address it on the rider's adopted boundary. That is a second action
opportunity for an actor that already took its slot, and it applied equally to a
fully spent, partially spent, surprise-skipped or visibility-skipped companion.
The disposition now records that slot as granted, prepared **and ended**. `Ended`
closes `CanAddress`, and with it partner selection, paired native command
admission and paired movement, while `OwnsRoundEffects` stays true so native
timers due at that boundary still belong to it. The observed Standard, Move and
Swift debt is recorded and never lowered, no private partner `TurnController` is
created, no preparation, refresh or callback replay runs, and eligibility returns
only through Kingmaker's next lawful allocation. The tests that asserted
addressability are replaced rather than loosened, with new behavioral cases for a
fully spent mount, a partially spent mount, residual cooldown debt, nothing
spent, surprise-skipped and visibility-skipped slots in both roster positions,
and recovery at the next allocation.

**R2 — attachment and adoption are one transaction.** The relationship used to
commit and increment its generation, after which `MountedPairActivated` attempted
adoption and merely *logged* a refusal, so a successful native Mount could leave a
mounted relationship with no valid paired activation.
`MidEncounterAdoptionPlan` is now a typed, immutable, generation-bound record of
the exact live encounter: session, round, current turn, roster identities and
indices, initiative disposition, surprise and visibility, liveness, ability to act
and consciousness. `MountRiderOn` plans it before committing, revalidates it field
by field immediately before the commit, and only then commits the adoption,
rebound to the one generation a committed relationship produces. A refusal after
attachment performs exact compensating cleanup — activation, private partner
context, preparation reservation, armed pair and renewal floor removed, the
relationship returned to `Unmounted`, and the transition reported as **failed**
rather than Mounted.

The compensation writes no native resource, calls no preparation or turn end, and
refunds nothing, so a Move that Kingmaker already committed for the approach
stays spent. The generation stays **advanced**, which is what retires every native
shell bound to the old relationship so the failed control cannot be delivered
again. Every refusal path inside the commit precedes the single
`partnerContext.Prepare()`, because that call clears the partner's cooldowns and
cannot be undone; a rollback therefore never has to un-prepare an actor, and a
source contract pins that ordering. The activation announcement no longer attempts
adoption at all: it is the consistency check for the transaction's invariant.

**R3 — turn-based delivery requires an *acting* rider turn.** `Preparing` was
accepted on the assumption that nothing is skipped in that window, and there is no
exact native evidence for it: `TurnController.Prepare` runs at the actor's own
slot and only then advances to Acting, and a player command is delivered by
`UnitCommands.Tick` inside Acting. Admitting a transition while the turn is still
Preparing would settle the transition ledger and the adopted paired grant against
a turn whose native preparation has not finished, so Preparing is now refused —
with its own reason, not the generic wrong-turn one.

**R4 — a Dismount delivery revalidates its target identity.**
`DismountTargetIdentityPolicy` decides it, free of engine types so each rejected
condition is directly tested: a missing target, a foreign target, a target that
changed after the command was created, a missing caster identity, a relationship
whose rider changed, and a stale generation. The most specific obstacle is always
the one reported.

**R5 — the save barrier queries the transition state.** The action contract
claimed the ledger deferred an unsettled transition, but `SaveEffectsReady` never
consulted it. It now defers on `HasUnsettledRelationshipTransition`, the ledger's
own admit-to-settle window, and on `OwnsUnsettledRelationshipShell`, a registered
Mount/Dismount shell that has not finished. The second term is strictly more than
`CommandNeedsSettlement` covers, because that predicate requires the command to be
*running* and would let a queued but unstarted relationship shell through. Nothing
is claimed for CM07 from this: all eleven CM07 rows remain mandatory native work.

**R6 — durable status language.** The action contract's legend claimed
`IMPLEMENTED` meaning built **and qualified**. Combat Mount and combat Dismount are
relabelled `IMPLEMENTATION CANDIDATE — NATIVE QUALIFICATION BLOCKED`, and
`IMPLEMENTED` is reserved and claimed by no row. This report and the three active
documents no longer describe the work as complete.

The mandatory list grows from 82 to **85** ids: `CM01-combat-mount-preparing-refused`
for R3's boundary, and `CM02-adoption-plan-invalidated` plus
`CM02-adoption-compensation-releases` for R2's transaction. Nothing was removed or
relabelled.

R2's regression needs a late invalidation that cannot be produced from outside
without writing game state, because the adoption disposition changes only at a turn
boundary and a turn boundary cancels the approach. It is therefore driven by a
**bounded diagnostic adoption fault**, which makes the commit refuse at its first
check exactly as a real late invalidation does. The fault arms only on an idle
unmounted pair with the paired lifecycle enabled, only one at a time, bound to two
exact distinct actor identities; it is consumed once, disarmed on use and on every
cleanup path, and writes nothing. Three source contracts pin those properties and
pin that it is armed exactly once, by this scenario only.

### The starting-payload registration, now authorized

The previous run reported this as its one blocker and declined to change it. The
owner has now explicitly authorized the exact addition, so it is made:

```
Info.json                   917523483b5850ac53ab8bd39ab9a34caaacfa0abfeae64b6d111fcdf7a71476
KingmakerMountedCombat.dll  8e231c388540cee50087ae47a2843bff06c69b6bf668b4a35f0ddfc3844f61a2
```

Its expected entry count is **two**, which is a fact about the tree rather than a
loosened bound: the guarded deployment replaced the DLL and the game has not been
run since, so there is no UMM loader cache, and the count is expressed the way every
other payload expresses it — as the size of its own pin set. Provenance is the
guarded deployment receipt
`runtime-state/deployment-operations/20260925T0200587550503Z-94de251601b24a04a1f5394f57a92f64.json`.

Nothing is widened. Strict byte equality is preserved for every pinned file, and
there is no wildcard, no alternate entry count, no "latest" or newest-wins logic, no
fallback or partial match, and no authority to restore any older or newer intake. A
tree matching no pin set still fails closed. The absence of `Info.json` is now its
own refusal rather than a hashing error, which narrows nothing — every pin set is
keyed on that file, so a tree without it could never have matched.

The negative tests are retained and **extended from 4 assertions to 13**: mutated
bytes per leaf, an extra unregistered file, a missing registered file, a renamed
registered file with the right bytes and the right count, a mixed payload whose DLL
bytes are foreign to its `Info.json`, and a subdirectory. Each is proven rejected,
and the exact live installation is proven accepted before and after, read only.

`scripts/Test-Chunk6aLedger.ps1` has two modes, deliberately separated. Record
consistency validates that each of the 85 entries is internally coherent and,
for a PASS entry, that its named run really executed the frozen payload with the
recorded assertion counts, restored the intake, used the recorded qualification
suite, and contains a single PASS row for every row the entry claims, with the
evidence artifact bound by hash. `-Completion` holds the fixed list of 85
mandatory behaviors and fails on any that is missing, NOT RUN, BLOCKED, FAIL, or
MAPPED / EXCLUDED without the owner's own recorded decision. The record-mode
count is **not** a count of satisfied requirements and is printed with that
warning attached.

## What changed

### The action contract, frozen from exact installed evidence

Three measured facts settle the design, and each is recorded with its exact
metadata token in the seam map:

- **Kingmaker's native Move shell is the sole cost owner, and it commits at the
  command's acted transition.** `UnitActionController.TickCommand` calls
  `UpdateCooldowns` only on the `IsActed` false-to-true transition that
  `UnitCommand.Tick` produces after `OnAction` returns. Approach has already
  finished by then, and KMC's custom delivery runs strictly later from the
  `AbilityExecutionProcess`. Cancelling or interrupting before that transition
  costs nothing; after it the cost stands, and KMC performs no refund even when
  a later revalidation legitimately refuses the relationship transition.
- **The out-of-combat transition is free because native code charges nothing
  outside combat**, not because KMC makes an exception for it. The real-time
  branch of `UpdateCooldowns` returns without writing when the executor is not
  in combat.
- **`TurnController.Prepare` is the per-round grant.** It calls
  `Cooldowns.Clear`, resets the reaction allowance, raises `OnNewRound` and
  ticks every per-round fact component. No relationship transition may call it.

### The transition-round participation rule

Kingmaker's turn-based round is a strictly positional walk over the sorted
roster, and no native hook fires when a pair forms mid-encounter. Chunk 6A
therefore adds one explicit typed adoption operation:

1. The relationship, presentation, transport authority and control leases form
   immediately on a successful combat Mount.
2. Paired ownership is created at the same instant, but its per-round grant is
   **adopted, never prepared**: the rider's running native turn becomes the
   activation boundary through pure KMC bookkeeping, because the rider's real
   native preparation already happened at its own initiative slot.
3. The mount's transition-round disposition comes from the positional
   observable. When its own slot is still ahead of the rider it is prepared
   exactly once as the paired partner, through the same private `TurnController`
   the accepted pre-combat path uses, and its own later slot is suppressed. When
   that slot has already been taken, no partner context is created and no native
   preparation runs, and its allocation for this round is recorded as granted,
   prepared **and ended**: the slot really did happen, so it is not re-offered.
   An ended partner cannot be addressed, selected as the pair's acting partner or
   admitted for a paired native command on the rider's boundary, while the native
   timers due at that boundary still belong to it. Its observed native Standard,
   Move and Swift debt is recorded and never lowered, nothing is refreshed or
   replayed, and it becomes eligible again only at Kingmaker's next lawful
   allocation.
4. When the disposition cannot be resolved unambiguously — the mount is outside
   the initiative order, shares the rider's slot, is surprised, is acting in a
   surprise round without proof it will act, or is not visible to the player —
   **the transition is refused before any native commitment**, and availability
   reports that exact reason so no Move shell is ever offered whose delivery
   would have to guess. This is a deliberate, named boundary, not a gap.
5. The activation finalizes at the rider's native turn `End` exactly as an
   ordinary paired round does, and `PairedNativeReadiness` keeps the pair's
   next-round readiness at the greater of both actors' native readiness, so
   adoption cannot pull the following round earlier.

A voluntary combat Mount is also refused while a previously split activation
still governs the pair's participation in the current round; it becomes
available again once that release round has passed.

### Explicit admission, one transition ledger, exact shell binding

`MountedRelationshipAdmission` replaces the restore boolean with three
separately testable modes. Exploration refuses any live encounter; voluntary
combat refuses the *absence* of one, so it can never stand in for the free
transition; saved restore adopts whatever the archive held and can never
authorize a voluntary mount. Every entry point other than the one voluntary
combat path defaults to exploration, so no diagnostic or automation route can
mount during an encounter.

`MountedTransitionLedger` admits and settles each voluntary transition exactly
once, refuses a second while one is in flight, suppresses a repeated delivery of
the same native control identity, and records forced detach as cleanup that
books no voluntary cost and stays idempotent per relationship generation.

Each native Mount/Dismount shell records its caster, target and relationship
generation at its own `UnitUseAbility.Init` boundary. At delivery the control
must still own the caster's native Move slot and still match that generation,
so a stale shell cannot transition a relationship it never targeted.

One hazard found and avoided: `UnitUseAbility.OnAction` hard-requires
`AbilityData.IsAvailable`, and `TickCommandTurnBased` interrupts an *unstarted*
command whose `IsAvailableForCast` is false. A KMC availability provider must
therefore stay true for its own running shell, so the in-flight gate is scoped to
the ledger and is suppressed on the execution pass. Adding an "owns a live shell"
gate to availability would have made every committed shell fail its own action.

### Persistence: no schema change, and the barrier now queries the transition

No schema change: a voluntary transition adds no saved field, and the existing
pair, debt and turn-context records carry the new states.

The accepted Chunk 5 save barrier already covers the transient sequence.
`NativeSaveEffectBoundary.CommandNeedsSettlement` holds any running unfinished
command — including the Mount/Dismount shell during approach and execution — and
`HasUnresolvedAbilities` holds the `AbilityExecutionProcess` until it ends, which
is after `Deliver` has performed the relationship transition.

That was the whole argument before the review, and R5 replaced the argument with
two exact queries. `SaveEffectsReady` now also defers on
`NativeMountedControlService.HasUnsettledRelationshipTransition`, the transition
ledger's own admit-to-settle window, and on `OwnsUnsettledRelationshipShell`, a
registered Mount/Dismount shell that has not finished. The second term is strictly
more than `CommandNeedsSettlement` covers, because that predicate requires the
command to be **running** and would let a queued but unstarted relationship shell
through. The ledger term is a guard that fails closed rather than one a frame
boundary is expected to observe, since the admit-to-settle window lies inside one
synchronous game-thread call — it is queried instead of argued away.

None of that is CM07 evidence. All eleven CM07 rows, `CM07-schema-unchanged`
included, remain mandatory native work and are BLOCKED until the campaign runs
them.

## Candidate identity

The remediated candidate opens its own product line so no evidence, ledger entry
or installation can confuse it with the reviewed preview.107.

| Field | Value |
|---|---|
| Branch | `codex/mounted-combat-phase3f-playable-core` |
| Product version | `0.1.0-chunk6a-preview.108` |
| Qualifier | `chunk6a-remediated-r1` |
| Package | `KingmakerMountedCombat-0.1.0-chunk6a-preview.108-chunk6a-remediated-r1-diagnostic.zip` |
| Source commit bound by the manifest | `2c0737eef78a1d60f4e1c6a9ac74dcd85b5560ab` |
| ZIP SHA-256 | `a6a5036c28313303dfb544c59e1340736f9ac69e3fc13e4ed60d9a2cc0f28fc1` |
| Manifest SHA-256 | `e6d303a5edbb8d6815862470ca966084cf39a648bd11038bf5100aee0342c161` |
| DLL SHA-256 | `89766acb62555f22cb2da81badf4f9c5cea92be27cfed517dd7a91eab89d9f3b` |
| DLL MVID | `68c8af22-4d2d-48e3-99d7-18a21f235108` |
| Accepted Chunk 5 payload, untouched | ZIP `35b7c82808ab8ecf264be0d511f24735c070374ca73b8259e544eee0d6200113`, DLL `8e231c388540cee50087ae47a2843bff06c69b6bf668b4a35f0ddfc3844f61a2`, MVID `638259af-9d31-4738-be8a-2784135d4235` |

This is a private engineering candidate. It is not installed, not merged and not
released, and the accepted preview.105 installation is untouched.

`Assert-KmcPackageManifest` requires a manifest to bind the exact current `HEAD`
on a clean worktree, so every documentation commit after a build invalidates that
build for runtime use and the campaign payload must be rebuilt at the then-current
head. That rebuild carries no risk for an unchanged `src/` tree, and on the
reviewed preview.107 line it was **measured rather than claimed**: four qualified
packages built at four different commits all produced the identical DLL
`cac89e2037b898813925782e25d5e1b5c8ef8dc24bead2388abbb01b590c8366` / MVID
`3739324f-ff40-4049-9a82-91667d8dbf24`, because nothing under `src/` changed
between them.

| preview.107 qualifier | Commit | ZIP SHA-256 |
|---|---|---|
| `-final` | `2d30822` | `fb6a8ab0fe336bddd75084d10e2e64284de6387aab0d5cf0bbefe6fe3a1b91f2` |
| `-final2` | `f0f94fb` | `b4022902836ee1bfb914e3d9555e7f8220cbf86b279149bd6adb8763d337bea7` — the `c6a-smoke-1` payload |
| `-final3` | `9f3a6f2` | `3d9f7c78c9cc894f4911bff992329105a63d97e69d7ef9c6e1b1ec6ce7d111e6` — the `c6a-smoke-2` payload |
| `-head` | `1d60100` | `699d64de0f26f0ef1e37119a00adbeae135fcfb9f418164e5da25d8f06b26e24` |

Those four are the reviewed line and are retained exactly as published. None of
them carries the six repairs, so none may be used for a Chunk 6A campaign.

## Offline gates

All re-measured on the remediated source, not carried forward: source contracts
**62/0**, components **522/0**, persistence assembly/storage contracts 180/0,
persistence data 56/0, profile protection 53/0, owned fixtures 620/0, validation
copies 118/0, harness 262/0, assembly-backed contracts 619/0, patch construction
30/0, package 11/0, Phase 3F contracts 9/0, registered starting installation
**13/0**, Chunk 4 core 375/0, ground 61/0, obstruction 106/0, traversal 149/0,
Chunk 5 ledger 106/0 with its completion gate still **PASS** on all 105 mandatory
behaviors, and Chunk 6A ledger record consistency **85/0** with its completion gate
**FAIL 85/85**, which is the truthful state. The whole `Test.ps1` umbrella exits 0
in 227 s.

No standing gate fails. `Test-Chunk2StartingInstallation.ps1`, which failed on
every run of the reviewed candidate because the owner's installation was
unregistered, now passes 13 of 13 with its negative controls extended rather than
relaxed.

Every accepted protocol envelope is unchanged on this source, which is the
regression signal that matters: ordinary controls 42/0, Chunk 4 play 123/0,
Chunk 4 extended 382/0, Chunk 4 traversal 149/0, Chunk 4 Charge 460/0, actor
allocation 82/0, paired restrictions 41/0, paired condition commands 67/0, paired
death 32/0, Phase 3G 20/0, Phase 3H 42/0 and Mammoth paired 18/0 all match their
pre-change counts exactly.
The eighteen assembly contracts pin the exact seams this design rests on:
`TickCommand`, both `UpdateCooldowns` overloads, `HasMoveAction` with its two
`Used*MoveAction` predicates, `Cooldowns.Clear`, `UnitCommand.get_IsActed` and
`TickApproaching`, `AbilityData.get_IsAvailable` and `get_IsAvailableForCast`, and
the roster observables `FindUnitInfo`, `ChooseNextUnit`, `StartRound`,
`HandleCombatStart` and the three `TBUnitInfo` fields adoption reads.

The source contracts pin, by construction rather than by convention: that the pair
candidate admits combat only through the explicit voluntary mode; that the
relationship service keeps one voluntary combat path and defaults every other entry
to exploration; that neither voluntary transition writes a native resource, forces a
turn end or calls a preparation; that both are admitted and settled exactly once
through the ledger; that the admission mode is decided from live combat state at
execution; that forced detach is recorded as cleanup; that adoption never re-enters
encounter start, candidate selection or a resource write and performs exactly one
native preparation and only for a pending partner slot; that adoption refuses an
unresolvable transition round before changing any state; that the admitted native
shell suppresses only the stale rider Move-resource predicate; and that mounted
Charge safety keeps every boundary unchanged.

The remediation adds thirteen more, one per repaired property: that a spent partner
slot is ended and can never be addressed again on that boundary; that a retained
partner creates no private native context and its closed allocation is observable;
that the adoption plan is immutable, generation-bound and never records an
unavailable disposition; that the lifecycle exposes plan, revalidate, commit and
rollback separately; that the rollback removes only KMC bookkeeping and never writes
or refunds a native resource; that a voluntary combat mount plans, revalidates,
commits and only then adopts, compensating a refusal; that the compensating path
returns the relationship to unmounted, reports failure and never refunds or rewinds
the generation; that the lifecycle is bound exactly once as the single adoption
authority; that the activation announcement never attempts adoption; that a
turn-based transition requires an acting rider turn and names the preparing
boundary; that a dismount delivery revalidates its captured target, its delivery
target, its rider and its generation; that the save barrier defers on an unsettled
relationship shell and on the transition ledger itself; and that the diagnostic
adoption fault is bounded to one idle exact pair, consumed once, refuses before any
state change, writes nothing, and is armed exactly once by this scenario.

## Native campaign: not executed

The `chunk6a-combat-mount-rt` and `chunk6a-combat-mount-tb` scenarios are
implemented, registered through every harness allowlist, and carry their own
evidence schema version 28 with a dedicated validator that re-derives the
rider's native Move commitment and both actors' native preparation counts rather
than trusting the game's own arithmetic. They have not been run, because a
save-backed scenario cannot obtain a qualification-suite snapshot — see the
blocker under Status.

### The `-WhatIf` purity proof: one retained failure, then three passes

This project requires a `-WhatIf` purity proof before any live runtime use. Three
were performed against the Chunk 6A packages.

| Attempt | Package | Result |
|---|---|---|
| `c6a-whatif-1` | preview.106 (unqualified) | **FAILED** after 54 min: `WhatIf purity failed: an external tree changed.` |
| `c6a-whatif-3` | preview.107 `-r2` | refused correctly: the worktree was dirty |
| `c6a-whatif-4` | preview.107 `-r2` | **PASS** after ~55 min |
| `c6a-whatif-5` | preview.107 `-final2` | **PASS** |
| `c6a-whatif-6` | preview.107 `-final3`, repaired invoker | **PASS** |

The first failure is retained rather than explained away. It named neither the
root nor the entry, so what it observed is unknown. Measured afterwards:

- The proof manifests `runtime-state`, `runtime-backups`, `runtime-staging`,
  `runtime-evidence` and the live `Mods` root by full SHA-256 before and after.
  Those four lab trees hold **623,407 files and 148,296,003,194 bytes**, so one
  proof hashes about 296 GB and takes roughly 55 minutes; the dominant cost is
  `runtime-staging`'s 472,074 entries at roughly 400 per second, not the byte
  volume.
- No file in any of the four trees had a modification time inside the failing
  window, so a plain concurrent content write is **disproved**.
- Two consecutive manifest passes are byte-stable for `runtime-state`, the live
  `Mods` root, `runtime-evidence` (13,321 files, 6.05 GB, 10 s and 8 s) and
  `runtime-backups` (193,050 files, 49.96 GB, 172 s and 165 s).
  `runtime-staging` was stopped after twenty minutes of its first pass so the
  proof itself could run instead, so its determinism is unmeasured.
- `Test-Harness.ps1`, the whole `Test.ps1` umbrella and every persistence gate
  leave the path-and-length sets of `runtime-evidence`, `runtime-staging` and
  `runtime-backups` **unchanged**, so none of the offline work performed during
  the failing window perturbs those trees.

What is **not** established: which of the five manifests differed, and why. It did
not recur across three later proofs. `runtime-staging` is the one tree whose
determinism is unmeasured, which narrows the search but is not a finding.
Eliminating the causes I could think of does not prove the cause lies outside this
repository's tooling, and it is not called an environmental or engine problem
here. The comparison was not narrowed or bypassed; it was only made
**diagnosable**, so a recurrence now names the root, its before/after file,
directory and byte counts, and up to forty removed, changed or added entries with
their lengths and hashes. Every byte of all five roots is still rehashed and any
difference still fails closed.

### The two live attempts, and the harness defect the first one found

Both are retained; neither launched the game, and both restored external state.

| Run | Package | Outcome |
|---|---|---|
| `c6a-smoke-1` | preview.107 `-final2` | **FAIL** in 4 s. `Cannot validate argument on parameter 'QualificationSuiteId'. The argument "" does not match ...` `modsRestored` true, `saveProtectionPassed` true, `launchIssued` false. |
| `c6a-smoke-2` | preview.107 `-final3` | **FAIL** in 4 s. `Existing KMC tree differs from the exact registered starting payload.` `modsRestored` true, `saveProtectionPassed` true, `launchIssued` false. |

The first was a **latent harness defect, now repaired**, and it meant no no-save
runtime scenario had been runnable since the qualification-suite pin set became
mandatory for save-backed runs. `New-KmcRunTransactionState` was always called
with all three suite arguments; for a no-save run those variables are unbound, so
PowerShell passed empty strings into `ValidatePattern`-guarded parameters, and an
explicitly passed empty string is rejected at binding time — before the
function's own completeness rule (exactly three suite values for a suite mode,
exactly none otherwise) could run. The logic was already correct; only the
argument binding was wrong. The call now splats the three suite arguments only
for a save-backed run. Nothing is relaxed: the parameter patterns, the
completeness rule and the mode-to-schema mapping are untouched, and the
save-backed path still always supplies all three.

The repair is proven by evidence rather than by inspection:
`runtime-state/run-transactions/c6a-smoke-2.json` records `mode=no-save-v1` and
`phase=restored`, which the run could not have reached before the fix.

`c6a-smoke-2` then reached the starting-payload registry, which is how the
blocker above was established to gate *every* live scenario rather than only the
save-backed ones. That is the stronger and more useful statement, and it came
from running the thing rather than from reading it.

## External state

The owner's installed build is untouched: `Mods/KingmakerMountedCombat` still
holds the accepted preview.105 DLL
(`8e231c388540cee50087ae47a2843bff06c69b6bf668b4a35f0ddfc3844f61a2`) and its
`Info.json` (`917523483b5850ac53ab8bd39ab9a34caaacfa0abfeae64b6d111fcdf7a71476`),
alongside the same seven mod directories including the deliberate SkipIntro. No
guarded deployment transaction was opened and no Kingmaker process was started.
Two guarded no-save run transactions were opened and both restored: `c6a-smoke-1`
failed before creating transaction state, and `c6a-smoke-2` recorded
`mode=no-save-v1`, `phase=restored`, `modsRestored` true and
`saveProtectionPassed` true. Verified afterwards: the installed `Info.json`
(`917523483b5850ac53ab8bd39ab9a34caaacfa0abfeae64b6d111fcdf7a71476`) and DLL
(`8e231c388540cee50087ae47a2843bff06c69b6bf668b4a35f0ddfc3844f61a2`) are
byte-identical to intake, the same seven mod directories are present, the 275
protected saves are unchanged, and there is no Kingmaker process and no
active-transaction lock. No protected save, automation fixture, UMM Params, cache
or foreign mod was written.

Checked once more at the close, on the published head: `Assembly-CSharp.dll` is
still `3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`, the
exact installed assembly every contract is pinned against, and UMM
`Kingmaker_Data/Managed/UnityModManager/Params.xml`
(`b4a135f4bc05fc150abf1a4106b4f2edbfad66730583ba8443e87fe69df7fe87`) was last
written at 2026-09-25T01:08:00Z — an hour before the owner's own preview.105
deployment at 02:00:58Z and some six hours before either smoke run, so it was
written by the owner's session and not by this work. The candidate DLL
`cac89e20…` appears nowhere in the installation: the final private candidate is
not installed, as required.

## HUMAN PLAY — owner checklist

Not performed and not inferable from automation. This list is the manual gate
for combat Mount/Dismount once a frozen Chunk 6A candidate exists; it should not
be attempted against the currently installed preview.105, which does not contain
this work.

1. Outside combat, with paired activation enabled while dismounted, mount and
   dismount through the abilities drawer. Confirm both are free and that the
   pose and camera behave as they do today.
2. Start a fight unmounted with the rider and the supported companion adjacent.
   On the rider's turn, confirm Mount Companion is enabled and its tooltip names
   no obstacle.
3. Mount during that turn. Confirm the rider's Move is spent once — one of the
   two move pips, or a single Move charge in the turn tracker — and that nothing
   else changes: the rider's Standard action, the companion's actions, both
   initiative positions and the round number.
4. Confirm the companion does **not** get its own separate turn later in that
   round, and that the next round presents one rider-led turn.
5. Dismount during a later turn. Confirm it spends one Move, that both actors
   remain valid and selectable, and that the companion resumes its own turn only
   from the next round.
6. Try Mount Companion again while already mounted, and with the companion
   selected instead of the rider. Confirm each is refused with a readable reason
   and costs nothing.
7. Begin Mount targeting and cancel it with a right-click or Escape. Confirm no
   cost and no transition.
8. With the pair mounted, confirm Charge is still unavailable and that an
   unmounted party member can still Charge normally.
9. Save and reload while mounted in combat, then again after dismounting.
   Confirm the pair, both actors' spent actions and the turn order come back
   exactly once, with no repeated Mount animation and no refunded actions.
10. Mount when the companion's initiative slot has **already passed** this round
    (its turn came before the rider's). Confirm the mount succeeds, then confirm
    the companion is not selectable as the pair's actor and the pair cannot move
    for the remainder of that turn: the companion's turn this round is over, and
    the rider has just spent its Move. Confirm ordinary paired movement returns
    at the next round.
11. Watch the moment the rider's turn begins. While the turn panel is still
    settling — before the rider can act — confirm Mount Companion is disabled and
    its tooltip says the transition waits until the rider's turn has finished
    preparing, and that it becomes enabled once the turn is actually acting.
12. Mount, dismount and mount again across several rounds in one encounter, then
    save, quit to the main menu, relaunch and load. Confirm both actors' spent
    actions, the pair and the turn order come back exactly once.

## Remaining scope, with the 6B handoff

None of the following is implemented, partially enabled or simulated here. Each
row of the action contract records the controlling actor, native surface, cost
owner, boundaries and open question; the seam map records the exact metadata
tokens so the next mission starts from evidence rather than discovery.

**6B — mounted Charge.** The whole feature lives behind `AbilityCustomCharge`
(type `0x02000598`). Its `Deliver` `0x06002BB6` dispatches to `RuntimeRoutine`
`0x06002BB8` or `TurnBasesRoutine` `0x06002BB7`, each of which owns a **forced
path** plus a **queued native `UnitAttack`**, and the resulting attack is stamped
`RuleAttackWithWeapon.IsCharge` `0x06007186`. The concrete problem 6B must solve
is a split of ownership the accepted architecture has not yet needed: the mount
must own the forced path while the rider keeps the Standard action that the
enclosing ability shell charges, and `IsEngageUnit` `0x06002BB5` must not strand
the pair mid-path if the target dies or moves. `CanTarget` `0x06002BBD` plus
`GetMinRangeMeters` `0x06002BBA`/`0x06002BBB` and `GetMaxRangeMeters`
`0x06002BBC` are the range surface a mounted variant would have to satisfy with
the mount's geometry rather than the rider's. Until that is done, the exact
multi-boundary rejection in `MountedChargeSafetyPolicy` and
`MountedChargeSafetyService` must stay exactly as it is; Chunk 6A's source
contract pins it, and its `chunk4-charge-safety-rt`/`-tb` rows are mandatory
CM08 ledger ids.

**6C — mounted casting and item use.** Cost stays native per the spell's or item
ability's own `ActionType`; the open questions are concentration
(`MakeConcentrationCheckIfCastingIsDifficult` `0x0600273A`,
`TryCastingDefensively` `0x0600273B`) and hand readiness
(`DontWaitForHands` `0x06002720`) while the mount is the physical mover.

**6D — staged moving actions.** The partner context's `TimeMoved`
`0x06000C13`/`0x06000C14` must carry between legs without a second grant, and
`m_AutoStopAfterFirstMoveAction` `0x04000687` must not end the rider-led boundary
between them.

**6E — the Mounted Combat defensive feat.** The pre-consequence seam is
`RuleAttackWithWeapon.AttackRoll` `0x06007197` inside `OnTrigger` `0x0600719D`.
The unresolved question is whether an immediate action is expressible at all:
`UnitAttackOfOpportunity` `0x02000502` is the only native out-of-turn player
command, so 6E must establish whether a Swift-typed command can be admitted out
of turn, or whether the feat must be modelled as a per-round allowance observed
at the attack rule. Swift debt itself is `Cooldowns.SwiftAction`
`0x0600C3BA`/`0x0600C3BB`, reset by `Cooldowns.Clear` at each `Prepare`.

**6F — consolidation.** Chunk 7 — multiple pairs, additional profiles, content
and public release — remains out of scope, as do the visual, HUD and
physical-input gates that Chunk 4 left as manual items, and the `P08-tb` bounded
known issue that Chunk 5 left open.
